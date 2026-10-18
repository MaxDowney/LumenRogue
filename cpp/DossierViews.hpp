#pragma once

static constexpr array<Slot,8> loadoutSlots{{Slot::Head,Slot::Amulet,Slot::MainHand,Slot::OffHand,Slot::Body,Slot::Hands,Slot::Ring,Slot::Feet}};

vector<string> Game::characterLines(int selected,double elapsed)const{
    auto[sw,sh]=consoleViewport();int width=min(180,sw-8);bool wide=sw>=108&&sh>=32;selected=clamp(selected,0,7);
    auto itemFor=[&](Slot slot){return find_if(inventory.begin(),inventory.end(),[&](auto&i){return i.equipped&&i.slot==slot;});};auto chosen=itemFor(loadoutSlots[size_t(selected)]);
    vector<string>lines{"\4 "+raceName(race)+" · "+className(calling)+" · LEVEL "+to_string(level)+" · XP "+to_string(xp)+"/"+to_string(level*45)," STR "+to_string(strength)+"  DEX "+to_string(dexterity)+"  INT "+to_string(intellect)+"  CON "+to_string(constitution)+"  WIS "+to_string(wisdom)};
    int left=wide?width-35:width,visible=wide?8:min(8,max(3,sh-15)),start=clamp(selected-visible/2,0,8-visible);auto art=classPortrait(calling);
    lines.push_back("\4"+fitPad(" EQUIPPED LOADOUT",left)+(wide?"│  WAYFARER":""));
    for(int row=0;row<(wide?12:visible);row++){string label;if(row<visible){int index=start+row;auto item=itemFor(loadoutSlots[size_t(index)]);string name=item==inventory.end()?"— empty —":item->name;label=string(index==selected?"◆ ":"  ")+fitPad(slotDisplayName(loadoutSlots[size_t(index)]),12)+" "+(index==selected?marqueeText(name,left-16,elapsed):fitPad(name,left-16));}lines.push_back(string(start+row==selected?(wide?"\3":"\1"):"")+fitPad(label,left)+(wide?"│  "+art[size_t(row)]:""));}
    lines.push_back("\4 "+marqueeText(chosen==inventory.end()?"EMPTY SLOT":chosen->name,width-1,elapsed));
    if(chosen!=inventory.end())lines.push_back(" ITEM · ATK "+signedStat(chosen->attack)+"  DEF "+signedStat(chosen->defense)+"  SPELL "+signedStat(chosen->spell)+"  LIGHT "+signedLightStat(chosen->light));
    else lines.push_back(" Open your pack to equip an item in this slot.");
    if(wide){lines.push_back(" YOUR TOTALS · ATK "+to_string(totalAttack())+"  DEF "+to_string(totalDefense())+"  SPELL "+to_string(totalSpellPower())+"  LIGHT "+lightStat(totalLightRadius()));if(chosen!=inventory.end())for(auto&part:wrapText(itemStudy(*chosen),width-2))lines.push_back(" "+part);lines.push_back("\5 Seed "+to_string(seed)+" · Day "+to_string(turns/240+1)+" · "+to_string(skillPoints)+" unspent skill points");}
    lines.push_back("\5 ↑↓ Select Slot · Enter Remove · I Pack · C/Esc Close");return lines;
}

vector<string> Game::spellbookLines(int selected,double elapsed)const{
    auto[sw,sh]=consoleViewport();int width=min(180,sw-8);vector<string>lines;if(learnedSpells.empty())return{"No spells learned. Invest in spell nodes with T.","\5 Z/Esc Close"};
    selected=clamp(selected,0,int(learnedSpells.size())-1);auto spell=learnedSpells[size_t(selected)];auto info=spellInfo(spell);bool wide=width>=96&&sh>=28;
    int visible=min(int(learnedSpells.size()),wide?sh-11:max(2,min(5,sh-17))),start=clamp(selected-visible/2,0,max(0,int(learnedSpells.size())-visible)),left=wide?min(46,width/3):width,right=width-left-3;
    vector<string> detail{info.name,info.description,"COST · "+to_string(spellCost(spell))+" mana","RANGE · "+(info.range?to_string(info.range)+" tiles":string("self")),"MASTERY · "+to_string(spellInvestment(spell))+"/3",info.range?"Choose a target; Tab cycles visible enemies.":"Casts immediately on yourself.","Learn more spells by investing in T / Skills."},wrapped;
    if(sh<24)detail.pop_back();for(auto&entry:detail)for(auto&part:wrapText(entry,wide?right:width-2))wrapped.push_back(part);
    lines.push_back("\4"+fitPad(" LEARNED SPELLS · "+to_string(selected+1)+"/"+to_string(learnedSpells.size()),left)+(wide?"│  SPELL PROFILE":""));
    for(int row=0;row<(wide?max(visible,int(wrapped.size())):visible);row++){string label;if(row<visible){int i=start+row;auto candidate=spellInfo(learnedSpells[size_t(i)]);string prefix=string(i==selected?"◆ ":"  ")+to_string(i+1)+" ";label=prefix+(i==selected?marqueeText(candidate.name,left-14,elapsed):fitPad(candidate.name,left-14))+" "+to_string(spellCost(learnedSpells[size_t(i)]))+" MP";}lines.push_back(string(start+row==selected?(wide?"\3":"\1"):"")+fitPad(label,left)+(wide?"│  "+fitPad(row<int(wrapped.size())?wrapped[size_t(row)]:"",right):""));}
    if(!wide){for(auto&part:wrapped)lines.push_back(" "+part);}
    lines.push_back(sh<24?"\5 ↑↓ Select · Enter/1–9 Cast · Esc Close":"\5 ↑↓ Select · Enter Cast · 1–9 Quick Cast · Z/Esc Close");return lines;
}

vector<string> Game::loreLines(int selected,double elapsed)const{
    auto[sw,sh]=consoleViewport();int width=min(180,sw-8);if(lore.empty())return{"No notes yet. Visit landmarks or record an NPC's testimony.","\5 F4/Esc Close"};selected=clamp(selected,0,int(lore.size())-1);
    auto fields=[&](int index){auto split=lore[size_t(index)].find(": ");return pair{split==string::npos?lore[size_t(index)]:lore[size_t(index)].substr(0,split),split==string::npos?string("No additional notes."):lore[size_t(index)].substr(split+2)};};auto[name,body]=fields(selected);
    bool wide=width>=96;int left=wide?min(46,width/3):width,right=width-left-3,visible=min(int(lore.size()),wide?sh-11:max(2,min(5,sh-17))),start=clamp(selected-visible/2,0,max(0,int(lore.size())-visible));
    vector<string> detail;for(auto&part:wrapText(name,wide?right:width-2))detail.push_back(part);detail.push_back("");for(auto&part:wrapText(body,wide?right:width-2))detail.push_back(part);
    vector<string>lines{"\4"+fitPad(" NOTES · "+to_string(selected+1)+"/"+to_string(lore.size()),left)+(wide?"│  SELECTED ACCOUNT":"")};
    int budget=wide?sh-11:max(1,sh-10-visible);for(int row=0;row<(wide?max(visible,min(budget,int(detail.size()))):visible);row++){string label;if(row<visible){int i=start+row;auto entry=fields(i).first;label=string(i==selected?"◆ ":"  ")+(i==selected?marqueeText(entry,left-3,elapsed):fitPad(entry,left-3));}lines.push_back(string(start+row==selected?(wide?"\3":"\1"):"")+fitPad(label,left)+(wide?"│  "+fitPad(row<int(detail.size())?detail[size_t(row)]:"",right):""));}
    if(!wide)for(int row=0;row<min(budget,int(detail.size()));row++)lines.push_back(" "+detail[size_t(row)]);
    lines.push_back("\5 ↑↓ Select · Enter Read Full Account · F4/Esc Close");return lines;
}

void Game::renderQaDossier(int kind){
    configureCharacter(RaceKind::Elf,ClassKind::Arcanist);
    if(kind==0){addItem(equipment("hearthguard's emberwoven crown",Slot::Head,']',0,2,1,.5));inventory.back().equipped=true;renderPanel("CHARACTER & LOADOUT",characterLines(0,0),{226,194,105});}
    else if(kind==1){for(int i=0;i<int(SpellKind::Count);i++)if(find(learnedSpells.begin(),learnedSpells.end(),SpellKind(i))==learnedSpells.end())learnedSpells.push_back(SpellKind(i));renderPanel("SPELLBOOK · MANA "+to_string(mana)+"/"+to_string(maxMana),spellbookLines(12,0),{166,125,238});}
    else{for(auto&m:maps)for(auto&npc:m.npcs){recordTestimony(npc,false);recordTestimony(npc,true);}renderPanel("LORE JOURNAL",loreLines(int(lore.size())-1,0),{118,176,218});}
}
