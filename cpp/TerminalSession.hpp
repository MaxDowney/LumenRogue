#pragma once

// One alternate-screen lifetime for splash, creation and gameplay. Re-entering
// it at the town transition can reset the host's buffer while it is resizing.
class TerminalSession {
    HANDLE output{GetStdHandle(STD_OUTPUT_HANDLE)};
    DWORD originalMode{};
    UINT originalCodePage{};
    bool active{};
public:
    TerminalSession(){
        if(!GetConsoleMode(output,&originalMode))return;
        originalCodePage=GetConsoleOutputCP();
        DWORD mode=originalMode|ENABLE_PROCESSED_OUTPUT|ENABLE_VIRTUAL_TERMINAL_PROCESSING|DISABLE_NEWLINE_AUTO_RETURN;
        if(!SetConsoleMode(output,mode)&&!SetConsoleMode(output,mode&~DISABLE_NEWLINE_AUTO_RETURN))throw runtime_error("This terminal cannot enable ANSI rendering.");
        SetConsoleOutputCP(CP_UTF8);active=true;
        cout<<"\x1b[?1049h\x1b[?7l\x1b[?25l\x1b[r\x1b[2J\x1b[H"<<flush;
    }
    ~TerminalSession(){
        if(!active)return;
        cout<<"\x1b[?2026l\x1b[0m\x1b[?7h\x1b[?25h\x1b[?1049l"<<flush;
        SetConsoleMode(output,originalMode);SetConsoleOutputCP(originalCodePage);
    }
    TerminalSession(const TerminalSession&)=delete;
    TerminalSession&operator=(const TerminalSession&)=delete;
};

// Read the *visible window*, not the scrollback buffer. Keep the last valid
// measurement during a host resize instead of drawing a transient 60x20 frame.
static pair<int,int> consoleViewport(){
    static pair<int,int> last{60,20};CONSOLE_SCREEN_BUFFER_INFO info{};
    if(GetConsoleScreenBufferInfo(GetStdHandle(STD_OUTPUT_HANDLE),&info)){
        int width=info.srWindow.Right-info.srWindow.Left+1,height=info.srWindow.Bottom-info.srWindow.Top+1;
        if(width>=2&&height>=2)last={max(60,width),max(20,height)};
    }
    auto size=last;
    if(auto value=getenv("LUMEN_QA_WIDTH"))if(atoi(value)>0)size.first=atoi(value);
    if(auto value=getenv("LUMEN_QA_HEIGHT"))if(atoi(value)>0)size.second=atoi(value);
    return size;
}

// Freeze a viewport for the complete composed frame. A resize between the
// world pass and its overlay must not give the two passes different geometry.
static optional<pair<int,int>> frameViewport;
struct ViewportFrame {
    bool owns{!frameViewport};
    ViewportFrame(){if(owns)frameViewport=consoleViewport();}
    ~ViewportFrame(){if(owns)frameViewport.reset();}
    static pair<int,int> size(){return frameViewport?*frameViewport:consoleViewport();}
};

static string frameReset(int width,int height){
    static pair<int,int> previous{};if(previous==pair{width,height})return "";
    previous={width,height};return "\x1b[?7l\x1b[r\x1b[0m\x1b[2J\x1b[H";
}
