// Included inside Game::selfTest so the tests exercise the real private APIs.
{
    auto require=[](bool condition,const string&what){if(!condition)throw runtime_error("polish: "+what);};
    for(bool regentFirst:{false,true}){
        Game campaign(94117);campaign.configureCharacter(RaceKind::Human,ClassKind::Vanguard);
        auto quest=[&](const string&id)->Quest&{auto found=find_if(campaign.quests.begin(),campaign.quests.end(),[&](auto&q){return q.id==id;});require(found!=campaign.quests.end(),"unknown campaign quest");return *found;};
        require(!campaign.acceptQuest(quest("sixth_record")),"locked quest accepted");
        auto&first=quest("blue_circuit");require(campaign.acceptQuest(first),"first quest unavailable");
        require(!campaign.turnInQuest(first),"unfinished quest awarded a reward");
        campaign.xp=44;campaign.discovered.insert("Lampwright Muster");int coins=campaign.gold;
        require(campaign.turnInQuest(first),"completed first quest not reportable");
        require(campaign.gold==coins+35&&campaign.level==2&&campaign.skillPoints==2&&campaign.xp==29,"quest XP did not level immediately");
        int xpNow=campaign.xp;require(!campaign.turnInQuest(first)&&campaign.gold==coins+35&&campaign.xp==xpNow,"repeat reward exploit");
        require(campaign.trackedQuest=="sixth_record"&&campaign.questNextStep(quest("sixth_record")).find("Sable")!=string::npos,"next chapter not signposted");
        require(campaign.acceptQuest(quest("sixth_record")),"second chapter unavailable");
        require(campaign.acceptQuest(quest("compact")),"regional commission unavailable");
        require(campaign.questNextStep(quest("compact")).find("Elin")!=string::npos,"missing wardens not named");
        // Annotation is genuinely distinct from reaching a site; neither XP nor evidence is granted remotely.
        campaign.current=1;auto dara=find_if(campaign.mapNow().npcs.begin(),campaign.mapNow().npcs.end(),[](auto&n){return n.id=="dara";});require(dara!=campaign.mapNow().npcs.end(),"missing Dara");
        campaign.discovered.erase("Whispering Stones");campaign.markLocalMap(*dara);
        require(!campaign.discovered.contains("Whispering Stones")&&campaign.objectiveProgress(quest("stones"))==0,"map note completed site visit");
        auto reach=[&](const string&name){auto&m=campaign.mapNow();auto l=find_if(m.landmarks.begin(),m.landmarks.end(),[&](auto&entry){return entry.name==name;});require(l!=m.landmarks.end(),"missing objective landmark");campaign.player=l->p;campaign.discoverNearby();};
        require(campaign.acceptQuest(quest("stones")),"Stonecross lead unavailable");reach("Whispering Stones");require(campaign.turnInQuest(quest("stones")),"visited stone oath not reportable");
        require(campaign.acceptQuest(quest("river")),"Reedwater lead unavailable");reach("Moonwell Cavern");require(campaign.turnInQuest(quest("river")),"visited intake not reportable");
        require(campaign.acceptQuest(quest("bramble_scouts")),"scout lead unavailable");int scouts=0;
        for(auto&monster:campaign.mapNow().monsters)if(monster.name.find("goblin")!=string::npos&&scouts<3){monster.hp=0;campaign.killMonster(monster);scouts++;}
        require(scouts==3&&campaign.turnInQuest(quest("bramble_scouts")),"finite scout hunt cannot be finished");
        auto defeat=[&](int mapIndex){campaign.current=mapIndex;auto&m=campaign.mapNow();auto boss=find_if(m.monsters.begin(),m.monsters.end(),[](auto&z){return z.boss;});require(boss!=m.monsters.end(),"missing campaign guardian");boss->hp=0;campaign.killMonster(*boss);};
        for(auto entry:array<pair<string,int>,3>{{{"hollowroot",SITE_FIRST_MAP},{"barrow",SITE_FIRST_MAP+1},{"moonwell",SITE_FIRST_MAP+2}}}){require(campaign.acceptQuest(quest(entry.first)),"village follow-up unavailable");defeat(entry.second);require(campaign.turnInQuest(quest(entry.first)),"village guardian report failed");}
        require(campaign.objectiveProgress(quest("compact"))==3&&campaign.turnInQuest(quest("compact")),"village pledges did not unlock commission");
        campaign.current=1;auto gate=find_if(campaign.mapNow().travels.begin(),campaign.mapNow().travels.end(),[](auto&t){return t.castleLocked;});require(gate!=campaign.mapNow().travels.end(),"no undercroft gate");auto destination=*gate;campaign.player=destination.p;campaign.travel(destination);require(campaign.current==SITE_FIRST_MAP+3,"earned commission did not open gate");
        require(campaign.acceptQuest(quest("regent")),"Regent quest unavailable");
        if(regentFirst){defeat(SITE_FIRST_MAP+3);require(campaign.turnInQuest(quest("regent")),"early Regent report failed");}
        defeat(MAIN_SIXTH_MAP);require(campaign.turnInQuest(quest("sixth_record")),"Foundation report failed");
        require(campaign.acceptQuest(quest("common_light")),"ending unavailable after Foundation report");
        if(!regentFirst){require(!campaign.turnInQuest(quest("common_light")),"ending awarded before both threats defeated");defeat(SITE_FIRST_MAP+3);require(campaign.turnInQuest(quest("regent")),"late Regent report failed");}
        require(campaign.turnInQuest(quest("common_light"))&&campaign.flags.contains("story_complete"),"campaign cannot be completed in both boss orders");
        require(all_of(campaign.quests.begin(),campaign.quests.end(),[](auto&q){return q.done;}),"campaign leaves a stranded quest");
        set<string> endings;for(auto&m:campaign.maps)for(auto&npc:m.npcs)endings.insert(campaign.npcGreeting(npc));require(endings.size()==9,"resolved NPC voices are not distinct");
        require(none_of(campaign.maps[0].lights.begin(),campaign.maps[0].lights.end(),[](auto&light){return light.color.b>light.color.r;}),"promised gold lamps not restored");
    }
    Game notes(94117);auto npc=notes.maps[0].npcs.front();size_t oldNotes=notes.lore.size();int oldXp=notes.xp;notes.recordTestimony(npc,false);notes.recordTestimony(npc,true);notes.recordTestimony(npc,false);
    require(notes.lore.size()==oldNotes+2&&notes.xp==oldXp+4&&notes.flags.contains("conclusion_"+npc.id)&&notes.flags.contains("rumour_"+npc.id),"testimony and conclusion are duplicate or repeatable");
    Game fx(94117);fx.configureCharacter(RaceKind::Ashborn,ClassKind::Arcanist);fx.current=0;fx.player={50,25};auto&arena=fx.mapNow();arena.monsters.clear();arena.npcs.clear();
    for(int y=16;y<34;y++)for(int x=40;x<65;x++)arena.at({x,y})=Floor;
    int beforeTurn=fx.turns,beforeMana=fx.mana;fx.castSpell(SpellKind::Blink,{-1,-1});require(fx.turns==beforeTurn&&fx.mana==beforeMana,"invalid target consumes resources");
    Monster target;target.name="test target";target.hp=target.maxHp=500;target.p=target.home=fx.player+Point{6,0};arena.monsters.push_back(target);
    fx.mana=99;fx.castSpell(SpellKind::Fireball,target.p);require(fx.turns==beforeTurn+1&&arena.monsters.front().hp<500,"spell did not spend exactly one turn and deal damage");
    require(fx.effects.size()>=2&&fx.effects[0].projectile&&!fx.effects[1].projectile&&fx.effects[1].radius==2&&fx.effects[1].started>=fx.effects[0].started+fx.effects[0].duration-.01,"fireball travel and impact are not sequenced");
    Point frozenMonster=arena.monsters.front().p;int frozenTurn=fx.turns;double began=fx.effects[0].started;string early=fx.renderWorldFrame(began+.05),late=fx.renderWorldFrame(began+.55);
    require(early!=late&&fx.turns==frozenTurn&&arena.monsters.front().p==frozenMonster,"real-time effects advance actors or do not animate");
    fx.effects.clear();fx.combatText.clear();fx.hp=1;fx.mana=99;fx.castSpell(SpellKind::HealingLight,fx.player);require(fx.hp>1&&!fx.effects.empty()&&fx.effects.back().style==EffectStyle::Healing,"healing lacks separate visual family");
    fx.emitImpact(fx.player,EffectStyle::Fire,4,.5);char glyph='.';Color fg{1,2,3},bg{4,5,6};auto delayed=fx.effects.back();effectCell(delayed,fx.player,delayed.started-.1,glyph,fg,bg);require(glyph=='.'&&bg.r==4,"future explosion is visible early");
    fx.reducedMotion=true;auto reduced=fx.renderWorldFrame(delayed.started+.05);require(!reduced.empty(),"reduced-motion rendering failed");
    set<vector<char>> sounds;
    for(int i=0;i<int(SoundCue::Count);i++){auto clip=SoundBank::synthesize(SoundCue(i));require(clip.size()>14000&&string(clip.data(),4)=="RIFF"&&string(clip.data()+8,4)=="WAVE","invalid PCM cue");int peak=0;long long energy=0;for(size_t p=44;p+1<clip.size();p+=2){int16_t sample=int16_t(uint16_t(uint8_t(clip[p]))|(uint16_t(uint8_t(clip[p+1]))<<8));peak=max(peak,abs(int(sample)));energy+=int64_t(sample)*sample;}require(peak>1500&&peak<28000&&energy>1000000,"silent or clipped synthesized cue");sounds.insert(clip);}
    require(sounds.size()==size_t(SoundCue::Count),"sound families are identical");
    auto thumbnail=generatedPortrait(int(RaceKind::Elf),16);require(thumbnail.size()==6&&all_of(thumbnail.begin(),thumbnail.end(),[](auto&row){return utf8Width(row)==16;}),"portrait thumbnail aspect ratio");
}
