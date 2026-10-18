#pragma once

void Game::renderPanel(const string&title,const vector<string>&source,Color accent,int highlighted,bool compact,int maximumHeight){
    ViewportFrame viewportFrame;auto[sw,sh]=ViewportFrame::size();bool workspace=!compact||source.size()>10||title.rfind("PACK",0)==0||title.rfind("SKILL",0)==0;int pw=min(sw-4,workspace?184:76),width=pw-4;double now=visualSeconds();
    struct Row {string text;bool selected{},marquee{},prefix{};int tone{};};vector<Row> rows,footer;
    auto parse=[&](string text,int index){Row row;row.selected=index==highlighted;while(!text.empty()&&text.front()>0&&text.front()<=5){char c=text.front();text.erase(text.begin());row.selected|=c==1||c==3;row.marquee|=c==2;row.prefix|=c==3;if(c==4)row.tone=1;if(c==5)row.tone=2;}row.text=text;return row;};
    for(size_t i=0;i<source.size();i++){
        Row row=parse(source[i],int(i));bool last=i+1==source.size()&&row.tone==2;
        auto parts=row.marquee?vector<string>{row.text}:wrapText(row.text,width);
        for(auto&part:parts){Row fragment=row;fragment.text=part;(last?footer:rows).push_back(fragment);}
    }
    if(footer.empty())footer.push_back({"Enter/Esc Close",false,false,false,2});
    string focus=title;for(auto&row:rows)if(row.selected){focus+="\n"+row.text;break;}
    if(focus!=panelMarqueeKey){panelMarqueeKey=focus;panelMarqueeStarted=now;}
    int inner=workspace?sh-6:min(sh-6,max(4,int(rows.size()+footer.size()+1)));if(maximumHeight>0)inner=min(inner,maximumHeight);int footCount=min(3,int(footer.size())),body=max(1,inner-footCount-1);
    vector<Row> visible;int clipped=0;
    if(int(rows.size())>body){
        int pinned=min(2,body/3),room=body-pinned,selected=0;for(size_t i=0;i<rows.size();i++)if(rows[i].selected){selected=int(i);break;}
        int begin=clamp(max(pinned,selected-room/2)+panelScroll,pinned,max(pinned,int(rows.size())-room));
        visible.insert(visible.end(),rows.begin(),rows.begin()+pinned);visible.insert(visible.end(),rows.begin()+begin,rows.begin()+begin+room);clipped=int(rows.size())-body;
    }else visible=rows;
    while(int(visible.size())<body)visible.push_back({""});
    string separator;for(int i=0;i<width;i++)separator+="─";
    if(clipped)separator=fitPad(" More text · PgUp/PgDn in reading panels ",width);
    visible.push_back({separator,false,false,false,2});for(int i=0;i<footCount;i++)visible.push_back(footer[size_t(i)]);
    int left=max(1,(sw-pw)/2+1),top=max(1,(sh-inner-2)/2+1);ostringstream out;out<<renderWorldFrame();
    Color border=lerpColor(accent,{92,112,136},.55);auto cursorAt=[&](int row){out<<"\x1b["<<row<<';'<<left<<"H\x1b[0m\x1b[48;2;9;12;18m";ansiColor(out,border);};
    cursorAt(top);string heading="╭─ "+fitPad(title,width-2)+" ─";out<<fitPad(heading,pw-1)<<"╮";
    for(int i=0;i<inner;i++){
        cursorAt(top+i+1);out<<"│ ";Row row=visible[size_t(i)];string text=row.marquee&&row.selected?marqueeText(row.text,width,now-panelMarqueeStarted):fitPad(row.text,width);
        if(row.selected){out<<"\x1b[48;2;30;44;59m\x1b[38;2;250;240;215m";size_t divider=row.prefix?text.find("│"):string::npos;if(divider!=string::npos){out<<text.substr(0,divider)<<"\x1b[48;2;9;12;18m\x1b[38;2;205;214;225m"<<text.substr(divider);}else out<<text;}
        else{ansiColor(out,row.tone==1?accent:row.tone==2?Color{154,174,193}:Color{208,217,228});out<<text;}
        out<<"\x1b[0m\x1b[48;2;9;12;18m";ansiColor(out,border);out<<" │";
    }
    cursorAt(top+inner+1);out<<"╰";for(int i=0;i<pw-2;i++)out<<"─";out<<"╯\x1b[0m";
    cout<<"\x1b[?2026h"<<out.str()<<"\x1b[?2026l"<<flush;
}

void Game::panel(const string&title,const vector<string>&source,Color accent,bool compact){
    vector<string> lines=source;if(!lines.empty()&&!lines.back().empty()&&lines.back().front()==5)lines.pop_back();lines.push_back("\5 PgUp/PgDn Read More · Enter/Esc Close");panelScroll=0;
    while(true){renderPanel(title,lines,accent,-1,compact);for(int i=0;i<4&&!_kbhit();i++)Sleep(8);if(!_kbhit())continue;int key=readKey();if(key==1006)panelScroll=max(0,panelScroll-8);else if(key==1008)panelScroll=min(int(lines.size())*4,panelScroll+8);else{panelScroll=0;return;}}
}
