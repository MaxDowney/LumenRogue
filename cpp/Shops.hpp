#pragma once

bool Game::buyFromShop(const ShopDefinition&shop,size_t index,int count){
    if(minutes()<420||minutes()>=1080){message="The counter is closed. Return between 07:00 and 18:00.";return false;}
    if(index>=shop.stock.size()||count<1||count>99)return false;
    const auto&offer=shop.stock[index];int64_t total=int64_t(offer.price)*count;
    if(offer.price<=0||total>gold){message="Not enough gold. This purchase costs "+to_string(total)+"; you have "+to_string(gold)+".";return false;}
    Item purchase=offer.item;purchase.quantity=count;purchase.equipped=false;addItem(purchase);gold-=int(total);
    message="BOUGHT · "+to_string(count)+" × "+purchase.name+" · paid "+to_string(total)+" gold. Added to your pack.";playCue(SoundCue::Quest);return true;
}

bool Game::sellToShop(const ShopDefinition&shop,size_t index,int count){
    if(minutes()<420||minutes()>=1080){message="The counter is closed. Return between 07:00 and 18:00.";return false;}
    if(index>=inventory.size()||count<1||count>inventory[index].quantity)return false;
    const Item sale=inventory[index];if(sale.equipped){message="Still equipped. Remove this item in your pack before selling it.";return false;}
    int price=resaleValue(sale);int64_t total=int64_t(price)*count;
    if(price<=0||int64_t(gold)+total>2147483647){message="The merchant cannot complete that trade.";return false;}
    if(count==sale.quantity)inventory.erase(inventory.begin()+index);else inventory[index].quantity-=count;
    gold+=int(total);message="SOLD · "+to_string(count)+" × "+sale.name+" · received "+to_string(total)+" gold.";playCue(SoundCue::Quest);return true;
}

vector<string> Game::shopLines(const ShopDefinition&shop,bool selling,int selected,int count,double elapsed,const string&receipt)const{
    auto[sw,sh]=ViewportFrame::size();int width=min(180,sw-8),inner=min(sh-6,32);bool wide=width>=96&&sh>=30,tiny=sh<25;
    auto order=inventoryOrder(InventorySort::Type);int entries=selling?int(order.size()):int(shop.stock.size());
    selected=clamp(selected,0,max(0,entries-1));const Item*chosen=entries?(selling?&inventory[order[size_t(selected)]]:&shop.stock[size_t(selected)].item):nullptr;
    int price=chosen?(selling?resaleValue(*chosen):shop.stock[size_t(selected)].price):0;
    count=clamp(count,1,chosen&&selling?max(1,chosen->quantity):99);int64_t total=int64_t(price)*count;
    bool blocked=chosen&&(selling?chosen->equipped:total>gold);string tabs=selling?" [B] BUY WARES    [S ◆] SELL FROM PACK":" [B ◆] BUY WARES    [S] SELL FROM PACK";
    vector<string> lines{"\4"+tabs+"  ·  GOLD "+to_string(gold)};
    if(tiny)lines[0]="\4 "+string(selling?"[B] Buy  [S ◆] Sell":"[B ◆] Buy  [S] Sell")+" · GOLD "+to_string(gold);
    if(wide){lines.push_back("\5 "+shop.subtitle);lines.push_back(" "+shop.keeper+": “"+shop.greeting+"”");}
    if(!chosen){lines.push_back("\4 YOUR PACK IS EMPTY");if(!tiny){auto portrait=generatedPortrait(shop.portrait);for(auto&row:resizePortrait(portrait,wide?32:16))lines.push_back(centerPad(row,width));}lines.push_back(" Nothing to sell. Press B or Tab to browse the shop.");lines.push_back("\5 Tab Buy/Sell · Esc Leave");return lines;}
    int left=wide?clamp(width/3,38,58):width,right=width-left-3;
    int visible=wide?min(entries,min(15,inner-10)):min(entries,tiny?2:3),start=clamp(selected-visible/2,0,max(0,entries-visible));
    vector<string> shelf;for(int i=start;i<start+visible;i++){
        const Item&item=selling?inventory[order[size_t(i)]]:shop.stock[size_t(i)].item;int unit=selling?resaleValue(item):shop.stock[size_t(i)].price;
        string tag=item.equipped&&selling?"WORN":selling?"×"+to_string(item.quantity):"";
        shelf.push_back(string(i==selected?"◆ ":"  ")+(i==selected?marqueeText(item.name,left-14,elapsed):fitPad(item.name,left-14))+fitPad(tag,6)+fitPad(to_string(unit)+"g",6));
    }
    string chosenTitle=chosen->name+" · "+(chosen->equippable()?slotDisplayName(chosen->slot):itemCategory(*chosen));
    vector<string> comparison;
    if(chosen->equippable()){
        auto worn=find_if(inventory.begin(),inventory.end(),[&](auto&i){return i.equipped&&i.slot==chosen->slot;});Item empty;auto&currentItem=worn==inventory.end()?empty:*worn;
        comparison.push_back("STAT       WORN   ITEM   CHANGE");
        array<double,4> base{{double(currentItem.attack),double(currentItem.defense),double(currentItem.spell),currentItem.light}},value{{double(chosen->attack),double(chosen->defense),double(chosen->spell),chosen->light}};
        array<string,4> labels{{"ATK","DEF","SPELL","LIGHT"}};
        for(int i=0;i<4;i++){double delta=value[size_t(i)]-base[size_t(i)];string colour=delta>0?"\x1b[38;2;116;226;170m":delta<0?"\x1b[38;2;250;140;118m":"\x1b[38;2;173;190;208m";
            comparison.push_back(fitPad(labels[size_t(i)],11)+fitPad(i==3?lightStat(base[size_t(i)]):to_string(int(base[size_t(i)])),7)+fitPad(i==3?lightStat(value[size_t(i)]):to_string(int(value[size_t(i)])),7)+colour+(i==3?signedLightStat(delta):signedStat(int(delta)))+"\x1b[38;2;208;217;228m");}
        if(!tiny)comparison.push_back("Change compares this item with your worn slot.");
    }else for(auto&part:wrapText(chosen->description,wide?max(30,right-2):width-2))comparison.push_back(part);
    lines.push_back("\4"+fitPad(string(selling?" YOUR PACK":" WARES")+" · "+to_string(selected+1)+"/"+to_string(entries)+" · UNIT PRICE",left)+(wide?"│  ITEM ON THE COUNTER":""));
    if(wide){
        vector<string> detail;detail.push_back(marqueeText(chosenTitle,right,elapsed));detail.push_back(itemQuality(*chosen)+" · "+to_string(price)+" gold each"+(selling?" · merchant offer":""));
        auto portrait=itemPortrait(*chosen);
        if(right>=82){for(int row=0;row<12;row++)detail.push_back(fitPad(portrait[size_t(row)],32)+"   "+fitPad(row<int(comparison.size())?comparison[size_t(row)]:"",right-35));}
        else{auto small=resizePortrait(portrait,inner<30?16:24);for(auto&row:small)detail.push_back(fitPad(centerPad(row,right),right));for(auto&row:comparison)detail.push_back(row);}
        for(auto&row:wrapText(!selling&&chosen->equippable()?chosen->description:itemStudy(*chosen),right))detail.push_back(row);
        // The merchant occupies the lower shelf column instead of an empty ocean of rows.
        if(inner>=30){shelf.push_back(shop.keeper);auto keeper=resizePortrait(generatedPortrait(shop.portrait),16);for(auto&row:keeper)shelf.push_back(centerPad(row,left));}
        int contentRows=min(inner-int(lines.size())-6,max(int(shelf.size()),int(detail.size())));
        for(int row=0;row<contentRows;row++)lines.push_back(string(start+row==selected?"\3":"")+fitPad(row<int(shelf.size())?shelf[size_t(row)]:"",left)+"│  "+fitPad(row<int(detail.size())?detail[size_t(row)]:"",right));
    }else{
        for(int row=0;row<int(shelf.size());row++)lines.push_back(string(start+row==selected?"\1":"")+shelf[size_t(row)]);
        lines.push_back("\4 "+marqueeText(chosenTitle,width-1,elapsed));
        if(!tiny){auto portrait=resizePortrait(itemPortrait(*chosen),16);for(int row=0;row<max(int(portrait.size()),int(comparison.size()));row++)lines.push_back(fitPad(row<int(portrait.size())?portrait[size_t(row)]:"",16)+"  "+fitPad(row<int(comparison.size())?comparison[size_t(row)]:"",width-18));}
        else for(auto&row:comparison)lines.push_back(row);
    }
    string checkout=" QTY "+to_string(count)+" × "+to_string(price)+"g = "+to_string(total)+"g"+(blocked?string(selling?" · REMOVE FIRST":" · NEED ")+ (selling?"":to_string(total-gold)+"g"):" · GOLD AFTER "+to_string(selling?gold+total:gold-total));
    lines.push_back("\4"+checkout);
    if(!tiny)lines.push_back("\5 "+marqueeText(receipt.empty()?(selling?"Offers: 40% of retail. Remove equipped items in your pack before selling.":"Purchases stack in your pack. Buying does not equip."):receipt,width-1,elapsed));
    lines.push_back(tiny?"\5 Tab Buy/Sell · ↑↓ Item · +/- Qty · Enter · Esc":"\5 Tab Buy/Sell · ↑↓ Item · +/- Qty · Enter Trade · I Pack · Esc Leave");return lines;
}

void Game::shopMenu(const string&name){
    auto shop=shopDefinition(name);bool selling=false;int selected=0,count=1;double started=visualSeconds();string receipt;
    while(true){
        {ViewportFrame frame;renderPanel(shop.name+" · TRADING COUNTER",shopLines(shop,selling,selected,count,visualSeconds()-started,receipt),shop.accent,-1,false,32);}
        for(int i=0;i<4&&!_kbhit();i++)Sleep(8);if(!_kbhit())continue;int key=readKey();auto order=inventoryOrder(InventorySort::Type);int entries=selling?int(order.size()):int(shop.stock.size());
        selected=clamp(selected,0,max(0,entries-1));bool moved=false;
        if(key==27||key=='q')return;
        if(key==9||key=='b'||key=='s'){selling=key==9?!selling:key=='s';selected=0;count=1;receipt.clear();moved=true;}
        else if(key==1001||key=='k'){selected=entries?(selected+entries-1)%entries:0;count=1;moved=true;}
        else if(key==1002||key=='j'){selected=entries?(selected+1)%entries:0;count=1;moved=true;}
        else if(key=='+'||key=='='||key==1004){int limit=selling&&entries?inventory[order[size_t(selected)]].quantity:99;count=min(limit,count+1);moved=true;}
        else if(key=='-'||key==1003){count=max(1,count-1);moved=true;}
        else if(key=='i'){inventoryMenu();count=1;receipt.clear();moved=true;}
        else if((key==13||key==' ')&&entries){if(selling)sellToShop(shop,order[size_t(selected)],count);else buyFromShop(shop,size_t(selected),count);receipt=message;count=1;moved=true;}
        if(moved)started=visualSeconds();
    }
}

void Game::renderQaShop(int variant){
    configureCharacter(RaceKind::Human,ClassKind::Vanguard);current=0;turns=80;gold=87;
    auto shop=shopDefinition(variant==2?"Copper Kettle":variant==3?"Cinder and Sigil":"Brass Badger");
    auto tile=find_if(mapNow().tiles.begin(),mapNow().tiles.end(),[&](auto&t){return t.glyph=='$'&&t.name==shop.name;});if(tile!=mapNow().tiles.end())player={int((tile-mapNow().tiles.begin())%mapNow().w),int((tile-mapNow().tiles.begin())/mapNow().w)};
    Item loot=equipment("glassfang bracers",Slot::Hands,']',1,1);loot.quantity=4;addItem(loot);
    int selected=0;if(variant==1){auto order=inventoryOrder(InventorySort::Type);for(int i=0;i<int(order.size());i++)if(inventory[order[size_t(i)]].name==loot.name)selected=i;}
    ViewportFrame frame;renderPanel(shop.name+" · TRADING COUNTER",shopLines(shop,variant==1,selected,variant==1?2:1,0,""),shop.accent,-1,false,32);
}
