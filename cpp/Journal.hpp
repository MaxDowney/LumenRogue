#pragma once

bool Game::questAvailable(const Quest&q)const{
    return q.prerequisite.empty()||any_of(quests.begin(),quests.end(),[&](const Quest&parent){return parent.id==q.prerequisite&&parent.done;});
}

string Game::giverLabel(const string&id)const{
    for(auto&m:maps)for(auto&n:m.npcs)if(n.id==id)return n.name+" · "+(id=="elin"?"Bramblewick":id=="dara"?"Stonecross":id=="nella"?"Reedwater":id=="roen"?"Castle Valeward":"Emberfall");
    return "Emberfall";
}

string Game::campaignBrief()const{
    if(flags.contains("story_complete"))return "THE COMMON LIGHT · The threat has ended. The villages have their water, warmth, and roads back.";
    if(cleared.contains("Valeward Undercroft")&&cleared.contains("The Unsurveyed Sixth · Depth 6"))return "THE FINAL ACCOUNT · Both threats are defeated. Report to Sable in Emberfall to close the investigation.";
    if(!flags.contains("circuit_traced"))return "THE BLUE CIRCUIT · Emberfall's lamps are warning of trouble below. Mara Vey needs the first survey station checked.";
    if(!flags.contains("valeward_commissioned"))return "TWO FRONTS · Find the broken seal on Depth 6. Help the three villages so Roen can open Valeward's undercroft.";
    return "BREAK THE REGENT'S HOLD · Defeat the Warden on Depth 6 and the Regent under Valeward. Either can be faced first.";
}

string Game::questNextStep(const Quest&q)const{
    if(q.done)return questCompletionNote(q);
    if(!questAvailable(q)){auto parent=find_if(quests.begin(),quests.end(),[&](auto&entry){return entry.id==q.prerequisite;});return parent==quests.end()?"Follow the investigation in Emberfall.":"First finish and report '"+parent->title+"' to "+giverLabel(parent->giver)+".";}
    if(!q.accepted)return "Speak to "+giverLabel(q.giver)+" and ask about this work.";
    if(objectiveProgress(q)>=q.need)return "Return to "+giverLabel(q.giver)+". Choose this quest, then report it complete.";
    if(q.id=="blue_circuit")return "Enter Emberfall's dungeon stair. Visit the Lampwright Muster on Depth 1; then return to Mara.";
    if(q.id=="sixth_record")return "Follow the downward stairs through Depths 1–6. Defeat the Sixth Warden beside the Foundation Seal.";
    if(q.id=="bramble_scouts")return "Hunt goblin scouts in the March. "+to_string(max(0,q.need-objectiveProgress(q)))+" more needed; then report to Elin in Bramblewick.";
    if(q.id=="stones")return "Visit the Whispering Stones in the March yourself. A mark on the map is only a direction, not evidence.";
    if(q.id=="river")return "Reach the Moonwell Cavern entrance in the March. Nella needs the intake located before her next request.";
    if(q.id=="compact"){
        string missing;for(auto pair:array<pair<string,string>,3>{{{"bramble_safe","Elin / Bramblewick"},{"stonecross_safe","Dara / Stonecross"},{"reedwater_safe","Nella / Reedwater"}}})if(!flags.contains(pair.first)){if(!missing.empty())missing+="; ";missing+=pair.second;}
        return "Finish and report the village work: "+missing+". Their pledges unlock Roen's commission.";
    }
    if(q.id=="common_light")return "Still needed: "+string(cleared.contains("The Unsurveyed Sixth · Depth 6")?"":"Sixth Warden on Depth 6. ")+(cleared.contains("Valeward Undercroft")?"":"Oathless Regent under Valeward. ")+"Then report to Sable.";
    return "Enter "+q.target+" from the March and defeat its guardian. Return to "+giverLabel(q.giver)+".";
}

string Game::objectiveTracker()const{
    auto chosen=find_if(quests.begin(),quests.end(),[&](auto&q){return q.id==trackedQuest&&!q.done;});
    if(chosen==quests.end())chosen=find_if(quests.begin(),quests.end(),[](auto&q){return q.accepted&&!q.done;});
    if(chosen==quests.end())chosen=find_if(quests.begin(),quests.end(),[&](auto&q){return !q.done&&questAvailable(q);});
    if(chosen==quests.end())return "JOURNAL · Chapter complete · Tab review";
    return "TRACKED · "+chosen->title+" · "+questNextStep(*chosen);
}

void Game::gainXp(int amount){
    xp+=max(0,amount);bool gained=false;while(xp>=level*45){xp-=level*45;level++;skillPoints+=2;maxHp+=2;hp=maxHp;mana=maxMana;gained=true;}
    if(gained){message+=" LEVEL "+to_string(level)+" · 2 skill points per level; press T to invest.";playCue(SoundCue::Level);}
}

bool Game::acceptQuest(Quest&q){
    if(q.accepted||q.done||!questAvailable(q))return false;q.accepted=true;trackedQuest=q.id;refreshQuestProgress();
    message="QUEST ACCEPTED · "+q.title+" · "+questNextStep(q);playCue(SoundCue::Quest);return true;
}

void Game::applyQuestConsequences(const Quest&q){
    if(q.flag=="stonecross_safe")for(auto&n:maps[1].npcs)if(n.id=="dara")for(auto&light:maps[1].lights)if(light.p.dist2(n.work)<400){light.radius=max(light.radius,7.0);light.intensity=max(light.intensity,.9);}
    if(q.flag=="reedwater_safe")for(auto&n:maps[1].npcs)if(n.id=="nella")for(auto&source:maps[1].provisions)if(source.kind==ProvisionKind::Water&&source.p.dist2(n.work)<400)source.charges=max(source.charges,12);
    if(q.flag=="story_complete")for(auto&light:maps[0].lights)if(light.color.b>light.color.r){light.color={255,176,76};light.intensity=1.25;}
}

bool Game::turnInQuest(Quest&q){
    refreshQuestProgress();if(!q.accepted||q.done||!questAvailable(q)||q.progress<q.need)return false;
    q.done=true;if(!q.flag.empty())flags.insert(q.flag);applyQuestConsequences(q);gold+=35;
    string note=questCompletionNote(q);lore.push_back("Completed — "+q.title+": "+note);
    int oldLevel=level;message="QUEST COMPLETE · "+q.title+" · +35 gold · +30 XP. "+note;gainXp(30);refreshQuestProgress();
    auto next=find_if(quests.begin(),quests.end(),[&](auto&entry){return !entry.done&&entry.prerequisite==q.id;});
    if(next!=quests.end()){trackedQuest=next->id;message+=" NEXT: "+giverLabel(next->giver)+" — "+next->title+".";}else if(trackedQuest==q.id)trackedQuest.clear();
    if(level==oldLevel)playCue(SoundCue::Quest);return true;
}

string Game::npcGreeting(const Npc&npc)const{
    const string&id=npc.id;
    if(flags.contains("story_complete")){
        if(id=="mara")return "All three lamps held gold through the night. I checked them twice. For once, there is nothing urgent I need you to do.";
        if(id=="sable")return "The full account is in the public archive, including our part in concealing it. A useful history has to name its mistakes.";
        if(id=="orra")return "A whole supper without somebody listening for the alarm. You should have heard the argument over whose turn it was to wash up. Beautiful.";
        if(id=="neris")return "The riverside plants are putting out clean new leaves. I am keeping the old samples; next time, I want us to recognise the warning sooner.";
        if(id=="tovan")return "The forge holds its heat again. Three apprentices have each explained to me how they knew it would. None of them did.";
        if(id=="elin")return "The hedge bells are quiet, and the root cellar is warm. We buried the forewoman by the road. People can go and speak to her now.";
        if(id=="dara")return "I have put the original oath where everyone can read it. If Valeward ever sends another collector, they can begin by reading it aloud.";
        if(id=="nella")return "The boats are riding level again. Tam says the river sounds bored. I told him that is the nicest thing anyone has said about it in weeks.";
        if(id=="roen")return "I sent copies of the confession to all three wardens. No royal seal. My own name will have to answer for it.";
    }
    if(id=="elin"&&flags.contains("bramble_safe"))return "The cellar holds its warmth again. Bramblewick has given its pledge to Roen. Tell him we expect him to keep his part of it.";
    if(id=="dara"&&flags.contains("stonecross_safe"))return "The lamps along our road are steady again. Stonecross has given its pledge. Take it to Roen at Valeward, and see that he puts it to use.";
    if(id=="nella"&&flags.contains("reedwater_safe"))return "The current is steady, and Tam can play beside a puddle again. Reedwater's pledge is ready for Roen. Our cisterns are filled for the journey.";
    if(id=="mara"&&flags.contains("circuit_traced"))return "You brought back the first answer. Sable is examining the account at Town Hall. Ask her about the erased depth; I will keep watch over the lamps.";
    if(id=="roen"&&flags.contains("valeward_commissioned"))return "The three wardens have given their consent. The undercroft is open. This time, Valeward answers to the roads, not the other way round.";
    return npc.greeting;
}

void Game::recordTestimony(const Npc&npc,bool conclusion){
    string key=(conclusion?"conclusion_":"rumour_")+npc.id;
    if(!flags.insert(key).second){message="ALREADY RECORDED · These notes are in your lore journal (F4).";return;}
    string entry=conclusion?npcTopicDialogue(npc,"rumour_more",current==0):npcRumourDialogue(npc);
    lore.push_back(string(conclusion?"Interpretation from ":"Testimony from ")+npc.name+": "+entry);
    message="NOTES UPDATED · "+npc.name+" · +2 XP · F4 to read.";gainXp(2);
}

vector<string> Game::questLines(int selected,double elapsed)const{
    auto[sw,sh]=consoleViewport();int width=min(180,sw-8);selected=clamp(selected,0,int(quests.size())-1);auto&q=quests[size_t(selected)];
    auto status=[&](const Quest&entry){return entry.done?string("DONE"):!questAvailable(entry)?string("LOCKED"):!entry.accepted?string("LEAD"):objectiveProgress(entry)>=entry.need?string("REPORT"):string("ACTIVE");};
    vector<string> lines;bool tiny=sh<24;if(tiny)lines.push_back("\4 THE LANTERN COMPACT · "+status(q)+" · "+to_string(selected+1)+"/"+to_string(quests.size()));else{for(auto&part:wrapText(campaignBrief(),width-2))lines.push_back("\4 "+part);
    lines.push_back("\5 LEAD = ask the giver · ACTIVE = work in progress · REPORT = collect reward · LOCKED = finish earlier work");}
    vector<string> detail{"NEXT STEP",questNextStep(q),"",q.title,q.description,"","OBJECTIVE · "+q.objective,"PROGRESS · "+to_string(objectiveProgress(q))+" / "+to_string(q.need)+"   "+status(q),"CONTACT · "+giverLabel(q.giver),"REWARD · 35 gold + 30 XP on reporting"};
    if(width>=104){int left=min(46,width/3),right=width-left-3;vector<string> wrapped;for(auto&entry:detail){auto parts=wrapText(entry,right);wrapped.insert(wrapped.end(),parts.begin(),parts.end());}
        lines.push_back("\4"+fitPad("  INVESTIGATION",left)+"│  CURRENT LEAD");
        for(int row=0;row<max(int(quests.size()),int(wrapped.size()));row++){string label;if(row<int(quests.size())){auto&entry=quests[size_t(row)];string name=(trackedQuest==entry.id?"* ":"  ")+entry.title;label=string(row==selected?"◆ ":"  ")+(row==selected?marqueeText(name,left-12,elapsed):fitPad(name,left-12))+fitPad(status(entry),8);}string combined=fitPad(label,left)+"│  "+fitPad(row<int(wrapped.size())?wrapped[size_t(row)]:"",right);lines.push_back(string(row==selected?"\3":"")+combined);}
    }else{
        int count=tiny?1:3,start=clamp(selected-(tiny?0:1),0,max(0,int(quests.size())-count));for(int i=start;i<min(start+count,int(quests.size()));i++)lines.push_back(string(i==selected?"\1 ◆ ":"   ")+marqueeText(quests[size_t(i)].title,width-16,i==selected?elapsed:0)+" "+status(quests[size_t(i)]));
        for(auto&entry:vector<string>{"NEXT STEP",questNextStep(q),"CONTACT · "+giverLabel(q.giver),"PROGRESS · "+to_string(objectiveProgress(q))+" / "+to_string(q.need),"REWARD · 35 gold + 30 XP on reporting"})for(auto&part:wrapText(entry,width-2))lines.push_back(" "+part);
    }
    lines.push_back("\5 ↑↓ Select · Enter Track · Tab/Esc Close");return lines;
}

void Game::questMenu(){
    refreshQuestProgress();int selected=0;auto tracked=find_if(quests.begin(),quests.end(),[&](auto&q){return q.id==trackedQuest;});if(tracked!=quests.end())selected=int(tracked-quests.begin());double started=visualSeconds();
    while(true){renderPanel("JOURNAL · THE LANTERN COMPACT",questLines(selected,visualSeconds()-started),{225,187,110},-1,false);for(int i=0;i<4&&!_kbhit();i++)Sleep(8);if(!_kbhit())continue;int key=readKey();if(key==1001||key=='w'||key=='k'){selected=(selected+int(quests.size())-1)%int(quests.size());started=visualSeconds();}else if(key==1002||key=='s'||key=='j'){selected=(selected+1)%int(quests.size());started=visualSeconds();}else if(key==13){trackedQuest=quests[size_t(selected)].id;message="TRACKING · "+quests[size_t(selected)].title+" · "+questNextStep(quests[size_t(selected)]);return;}else if(key==9||key==27||key=='q')return;}
}

void Game::renderQaJournal(){configureCharacter(RaceKind::Human,ClassKind::Vanguard);acceptQuest(quests[0]);renderPanel("JOURNAL · THE LANTERN COMPACT",questLines(0,0),{225,187,110},-1,false);}
