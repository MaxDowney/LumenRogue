#pragma once

// The live creator and QA snapshots share this layout, so a preview cannot
// quietly test a different, roomier screen than the one players actually use.
static vector<string> creationPresentation(const vector<string>&options,const vector<string>&details,const vector<string>&art,int selected,double elapsed=0,int detailOffset=0){
    auto[sw,sh]=consoleViewport();int width=min(180,sw-8),left=clamp(width/3,24,44),right=width-left-3,available=sh-9;
    vector<string> dossier;for(auto&detail:details){auto wrapped=wrapText(detail,right);dossier.insert(dossier.end(),wrapped.begin(),wrapped.end());}
    int start=clamp(detailOffset,0,max(0,int(dossier.size())-available));
    vector<string> candidate;for(size_t i=0;i<options.size();i++){string name=options[i].substr(0,options[i].find(" · "));candidate.push_back(string(int(i)==selected?"◆ ":"  ")+to_string(i+1)+"  "+name);}
    candidate.push_back("");int imageWidth=min(32,min(left-2,(available-int(candidate.size()))*8/3));if(imageWidth>=8){auto portrait=resizePortrait(art,imageWidth);for(auto&line:portrait)candidate.push_back(centerPad(line,left));}
    string detailLabel="DOSSIER";if(int(dossier.size())>available)detailLabel+=" · "+to_string(start+1)+"/"+to_string(dossier.size());
    vector<string> lines{"\4"+fitPad("  CANDIDATES",left)+"│  "+detailLabel};
    for(int row=0;row<min(available,max(int(candidate.size()),int(dossier.size())-start));row++){
        string label=row<int(candidate.size())?candidate[size_t(row)]:"",detail=start+row<int(dossier.size())?dossier[size_t(start+row)]:"";
        lines.push_back(string(row==selected?"\3":"")+fitPad(label,left)+"│  "+fitPad(detail,right));
    }
    lines.push_back("\5 ↑↓ Choose · Enter Confirm · PgUp/Dn Read · Esc Back");return lines;
}
