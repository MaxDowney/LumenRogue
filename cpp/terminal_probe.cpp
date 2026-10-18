// A separate QA executable: exercise the real creator/renderer in Windows
// ConPTY, then inspect the native console cells, not a simplified ANSI parser.
#define main lumenApplicationMain
#include "LumenRogue.cpp"
#undef main
#include <io.h>

int main(int argc,char**argv){
    if(argc!=2)return 2;
    // Codex/Python can inherit redirected standard handles even with a ConPTY
    // attribute. Bind this test child explicitly to its own console device.
    if(!freopen("CONOUT$","w+",stdout)||!freopen("CONIN$","r+",stdin))return 3;
    SetStdHandle(STD_OUTPUT_HANDLE,reinterpret_cast<HANDLE>(_get_osfhandle(_fileno(stdout))));
    SetStdHandle(STD_INPUT_HANDLE,reinterpret_cast<HANDLE>(_get_osfhandle(_fileno(stdin))));
    ofstream report(argv[1]);HANDLE output=GetStdHandle(STD_OUTPUT_HANDLE);DWORD original{};if(!GetConsoleMode(output,&original)){report<<"mode_error "<<GetLastError()<<'\n'<<flush;return 4;}
    auto capture=[&](const string&phase){
        CONSOLE_SCREEN_BUFFER_INFO info{};GetConsoleScreenBufferInfo(output,&info);int w=info.srWindow.Right-info.srWindow.Left+1,h=info.srWindow.Bottom-info.srWindow.Top+1;
        vector<wchar_t> cells(size_t(w)*h);DWORD count{};ReadConsoleOutputCharacterW(output,cells.data(),DWORD(cells.size()),{info.srWindow.Left,info.srWindow.Top},&count);
        wstring all(cells.begin(),cells.end()),first(cells.begin(),cells.begin()+w),bottom(cells.end()-w,cells.end());
        bool hud=first.find(L"Emberfall")!=wstring::npos,player=all.find(L'@')!=wstring::npos,footer=bottom.find(w>=100?L"Quit":L"Help")!=wstring::npos;
        report<<phase<<' '<<w<<' '<<h<<' '<<hud<<' '<<player<<' '<<footer<<'\n'<<flush;
    };
    {TerminalSession session;Game game(94117);game.createCharacterInteractive();for(int i=0;i<3;i++)game.draw();capture("town");
        readKey();for(int i=0;i<3;i++)game.draw();capture("small");
        readKey();for(int i=0;i<3;i++)game.draw();capture("large");
        // Open the real interactive counter, switch tabs and make both trades.
        game.gold=200;game.inventory.clear();game.shopMenu("Brass Badger");game.draw();capture("shop_closed");
        report<<"trades "<<game.gold<<' '<<game.turns<<'\n'<<flush;
    }
    DWORD restored{};GetConsoleMode(output,&restored);report<<"restored "<<(original==restored)<<'\n'<<flush;
    return 0;
}
