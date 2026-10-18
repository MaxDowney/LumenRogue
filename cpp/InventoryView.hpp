#pragma once

vector<string> Game::inventoryLines(int selected,bool compact,double elapsed,InventorySort sort)const{
    auto[sw,sh]=consoleViewport();int width=min(180,sw-8);bool wide=width>=96&&sh>=32,tiny=sh<24;vector<string> lines;
    string sortRow=" SORT  ";for(int i=0;i<4;i++)sortRow+="["+to_string(i+1)+(int(sort)==i?" ◆ ":" ")+inventorySortName(InventorySort(i))+"] ";
    lines.push_back("\4"+string(tiny?" TOTALS · ATK ":" YOUR TOTALS · ATK ")+to_string(totalAttack())+"  DEF "+to_string(totalDefense())+"  SPELL "+to_string(totalSpellPower())+"  LIGHT "+lightStat(totalLightRadius())+(tiny?"":"  GOLD "+to_string(gold)));
    for(auto&part:wrapText(tiny?sortRow:sortRow+" [TAB] NEXT",width))lines.push_back("\4"+part);
    if(inventory.empty()){lines.push_back("Your pack is empty. Pick up an item with G.");lines.push_back("\5 I/Esc Close");return lines;}
    auto order=inventoryOrder(sort);selected=clamp(selected,0,int(order.size())-1);auto&chosen=inventory[order[size_t(selected)]];
    int artWidth=wide?32:0,left=width-(wide?35:0),visible=wide?min(12,max(5,sh-24)):min(5,max(2,sh-23)),start=clamp(selected-visible/2,0,max(0,int(order.size())-visible));
    lines.push_back("\4"+fitPad(" PACK · "+to_string(selected+1)+"/"+to_string(order.size())+" · SORTED BY "+inventorySortName(sort),left)+(wide?"│  ITEM STUDY":""));
    auto art=itemPortrait(chosen);int statSpace=wide&&left>=90?27:0,nameWidth=left-23-statSpace;
    for(int row=0;row<visible;row++){
        string label;if(start+row<int(order.size())){int i=start+row;auto&item=inventory[order[size_t(i)]];string name=i==selected?marqueeText(item.name,nameWidth,elapsed):fitPad(item.name,nameWidth);string state=item.equipped?"WORN":("×"+to_string(item.quantity));label=string(i==selected?"◆ ":"  ")+name+" "+fitPad(state,6)+fitPad(item.equippable()?slotDisplayName(item.slot):itemCategory(item),12);if(statSpace)label+=item.equippable()?" A"+signedStat(item.attack)+" D"+signedStat(item.defense)+" S"+signedStat(item.spell):"";}
        string combined=fitPad(label,left)+(wide?"│  "+fitPad(art[size_t(row*12/visible)],artWidth):"");lines.push_back(string(start+row==selected?(wide?"\3":"\1"):"")+combined);
    }
    lines.push_back("\4 "+marqueeText(chosen.name+" · "+slotDisplayName(chosen.slot)+" · "+itemQuality(chosen)+" · ×"+to_string(chosen.quantity),width-1,elapsed));
    if(chosen.equippable()){
        auto worn=find_if(inventory.begin(),inventory.end(),[&](auto&i){return i.equipped&&i.slot==chosen.slot;});Item empty;const Item&currentItem=worn==inventory.end()?empty:*worn;
        if(wide)lines.push_back(" WORN NOW · "+(worn==inventory.end()?string("empty slot"):currentItem.name));
        array<double,4> itemValues{{double(chosen.attack),double(chosen.defense),double(chosen.spell),chosen.light}},oldValues{{double(currentItem.attack),double(currentItem.defense),double(currentItem.spell),currentItem.light}};
        array<double,4> before{{double(totalAttack()),double(totalDefense()),double(totalSpellPower()),totalLightRadius()}};
        array<double,4> after{{double(attack+(mightTurns?4:0)-(food==0?2:0)-(water==0?1:0)),double(defense+(wardTurns?4:0)+(guardTurns?4:0)-(water==0?1:0)),double(spellPower+(focusTurns?4:0)-(water==0?2:0)),lanternRadius}};
        for(auto&i:inventory)if(i.equipped&&i.slot!=chosen.slot){after[0]+=i.attack;after[1]+=i.defense;after[2]+=i.spell;after[3]+=i.light;}for(int i=0;i<4;i++)after[size_t(i)]+=itemValues[size_t(i)];after[0]=max(1.0,after[0]);after[1]=max(0.0,after[1]);after[2]=max(0.0,after[2]);
        int labelWidth=wide?23:16;lines.push_back("\4"+fitPad(" ITEM BONUSES",labelWidth)+fitPad("WORN",7)+fitPad("ITEM",7)+fitPad("CHANGE",9)+"YOUR TOTAL");
        array<string,4>names=wide?array<string,4>{{" ATK · attack strength"," DEF · protection"," SPELL · magic power"," LIGHT · lamp radius"}}:array<string,4>{{" ATK"," DEF"," SPELL"," LIGHT"}};
        for(int i=0;i<4;i++){auto value=[&](double v){return i==3?lightStat(v):to_string(int(v));};double difference=itemValues[size_t(i)]-oldValues[size_t(i)];string delta=i==3?signedLightStat(difference):signedStat(int(difference));string colour=difference>.0001?"\x1b[38;2;116;226;170m":difference<-.0001?"\x1b[38;2;250;140;118m":"\x1b[38;2;154;174;193m";
            lines.push_back(fitPad(names[size_t(i)],labelWidth)+fitPad(value(oldValues[size_t(i)]),7)+fitPad(value(itemValues[size_t(i)]),7)+colour+fitPad(delta,9)+"\x1b[38;2;208;217;228m"+value(before[size_t(i)])+" → "+value(after[size_t(i)]));}
        if(!tiny)lines.push_back("\5 "+string(chosen.equipped?"Already equipped. Enter removes this copy.":"Green + gains · Red - losses · Enter equips one copy."));
    }else{
        for(auto&part:wrapText("USE · "+chosen.description,width-2))lines.push_back(" "+part);
        lines.push_back(" Enter uses one item. Identical copies share this stack.");
    }
    if(wide){lines.push_back("\5 LAST ACQUIRED "+acquiredLabel(chosen)+" · POWER SCORE "+to_string(itemPower(chosen))+" (sort estimate) · Newest = latest pickup");for(auto&part:wrapText(itemStudy(chosen),width-2))lines.push_back(" "+part);}
    lines.push_back(tiny?"\5 ↑↓ · 1–4/Tab Sort · Enter Use · D Drop · Esc Close":"\5 ↑↓ Select · 1–4/Tab Sort · Enter Use/Equip · D Drop One · I/Esc Close");return lines;
}
