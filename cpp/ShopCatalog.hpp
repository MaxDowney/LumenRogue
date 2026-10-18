#pragma once

struct ShopOffer { Item item; int price{}; };
struct ShopDefinition { string name,keeper,subtitle,greeting,portrait; Color accent; vector<ShopOffer> stock; };

static int retailValue(const Item&item){
    if(item.equippable()){
        if(item.name=="watchman buckler"&&item.defense==2&&item.attack==0&&item.spell==0&&item.light==0)return 38;
        double value=8+max(0,item.attack)*8.0+max(0,item.defense)*10.0+max(0,item.spell)*12.0+max(0.0,item.light)*20;
        return int(clamp(value,6.0,100000.0));
    }
    switch(item.kind){case ItemKind::Potion:return 12;case ItemKind::GreaterPotion:return 28;case ItemKind::Ration:return 8;case ItemKind::Flask:return 7;case ItemKind::EmptyFlask:return 4;case ItemKind::WallTorch:return 10;case ItemKind::ManaTonic:return 20;case ItemKind::Bomb:return 24;case ItemKind::SmokeBomb:return 22;case ItemKind::Waystone:return 45;case ItemKind::Ambrosia:return 40;case ItemKind::MightElixir:case ItemKind::GuardElixir:case ItemKind::FocusElixir:return 26;default:return 0;}
}
static int resaleValue(const Item&item){int value=retailValue(item);return value>0?max(1,value*2/5):0;}

static ShopDefinition shopDefinition(const string&name){
    ShopDefinition shop;
    if(name=="Copper Kettle")shop={name,"TOVAN REED","PROVISIONS · REMEDIES · A WARM WELCOME","Eat before the mystery. The mystery can wait.","race-human",{234,181,104},{}};
    else if(name=="Cinder and Sigil")shop={name,"THE SIGILKEEPER","TONICS · FOCI · LIGHT FOR THE ROAD","Every bottle is labelled. Please keep it that way.","class-arcanist",{181,150,244},{}};
    else shop={name,"THE BADGER ARMOURER","ARMS · ARMOUR · DELVER'S GOODS","Bring back the blade. More importantly, bring back the hand.","class-vanguard",{236,180,101},{}};
    auto supply=[&](ItemKind kind){Item item=makeItem(kind);shop.stock.push_back({item,retailValue(item)});};
    auto gear=[&](string label,Slot slot,char glyph,int atk,int def,int magic=0,double light=0){Item item=equipment(label,slot,glyph,atk,def,magic,light);item.description=magic>0?"A carefully cut sigil holds a steady charge. The maker has signed the setting.":"Workshop-made in Emberfall, checked and fitted for the long road into the Delve.";shop.stock.push_back({item,retailValue(item)});};
    if(name=="Copper Kettle"){
        for(auto kind:{ItemKind::Ration,ItemKind::Flask,ItemKind::EmptyFlask,ItemKind::Potion,ItemKind::GreaterPotion,ItemKind::Ambrosia,ItemKind::WallTorch,ItemKind::GuardElixir})supply(kind);
    }else if(name=="Cinder and Sigil"){
        for(auto kind:{ItemKind::ManaTonic,ItemKind::FocusElixir,ItemKind::Potion,ItemKind::GreaterPotion,ItemKind::Waystone,ItemKind::Ambrosia,ItemKind::SmokeBomb,ItemKind::WallTorch})supply(kind);
        gear("sigil-carved ash staff",Slot::MainHand,'/',0,0,3,.3);gear("lamplighter's amber pendant",Slot::Amulet,'"',0,0,1,.8);gear("silver focus ring",Slot::Ring,'=',0,0,2);gear("rune-stitched mantle",Slot::Body,']',0,1,1);
    }else{
        gear("iron longsword",Slot::MainHand,'/',3,0);gear("watchman buckler",Slot::OffHand,']',0,2);gear("riveted road coat",Slot::Body,']',0,3);gear("iron browguard",Slot::Head,']',0,2);gear("delver's leather gloves",Slot::Hands,']',1,1);gear("hobnailed road boots",Slot::Feet,']',0,1);gear("balanced hunting bow",Slot::MainHand,'}',4,0);gear("hearthguard mace",Slot::MainHand,'/',2,1);
        for(auto kind:{ItemKind::Bomb,ItemKind::SmokeBomb,ItemKind::WallTorch,ItemKind::Potion,ItemKind::Ration,ItemKind::Flask,ItemKind::MightElixir})supply(kind);
    }
    return shop;
}
