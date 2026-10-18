#pragma once

enum class RaceKind { Human, Dwarf, Elf, Ashborn };
enum class ClassKind { Vanguard, Arcanist, Ranger, Lightbearer };
enum class BiomeKind { Emberfall, AshenVaults, DrownedReliquary, PrismaticFoundry, LightlessCourt, CrownOfCinders };
enum class Slot { None, MainHand, OffHand, Head, Body, Hands, Feet, Ring, Amulet };
enum class ItemKind { Potion, GreaterPotion, Ration, Flask, ManaTonic, Bomb, SmokeBomb, WallTorch, Waystone, Ambrosia, MightElixir, GuardElixir, FocusElixir, Equipment, EmptyFlask };
enum class ProvisionKind { Food, Water };
enum class MachineKind { Brazier, Floodgate, Prism, Generator };
enum class TrapKind { FlameJet, ShockPlate, PoisonVent, Collapse, Alarm };
enum class SpellKind {
    Fireball, FrostLance, ChainLightning, LuminousNova, Meteor, ArcaneBeam, Stonebreak,
    SeismicWave, ShieldFlare, ThornVolley, ShadowBolt, SunLance, HealingLight, FlameCleave,
    GroundQuake, BattleCry, IceStorm, Blink, PrismaticOrb, Multishot, PoisonCloud,
    MarkedShot, SolarRay, Sanctuary, Smite, Count
};

struct Item {
    ItemKind kind{ItemKind::Ration};
    string name, description;
    char glyph{'?'};
    Color color{210,190,140};
    int quantity{1};
    Slot slot{Slot::None};
    int attack{}, defense{}, spell{};
    double light{};
    bool equipped{};
    int acquiredTurn{};
    uint64_t acquiredOrder{};
    bool equippable() const { return slot != Slot::None; }
};

struct GroundItem { Point p{}; Item item; };
struct ProvisionSource { Point p{}; ProvisionKind kind{}; string name; int charges{}; };
struct WallTorchFeature { Point p{}; Tile underlying; bool lit{true}; };
struct TrapFeature { Point p{}; TrapKind kind{}; bool revealed{}, armed{true}; };
struct MachineFeature { Point p{}; MachineKind kind{}; bool active{true}; };
struct ElementCell { int wet{},oil{},fire{},frost{},electricity{},poison{},smoke{},web{}; };

struct SkillNode {
    string id, branch, name, description;
    int branchIndex{}, tier{}, requiredLevel{}, rank{}, maxRank{3};
    string prerequisite;
    enum class Reward { Vitality, Might, Guard, Arcana, Radiance, Spell } reward{Reward::Might};
    optional<SpellKind> spell;
    bool purchased() const { return rank > 0; }
    bool maxed() const { return rank >= maxRank; }
};

enum class EffectStyle { Fire, Frost, Lightning, Arcane, Radiant, Poison, Stone, Healing, Steel };
struct VisualEffect {
    vector<Point> path;
    Point center{};
    Color hot{255,245,210}, cool{255,90,20};
    double started{}, duration{.65};
    int radius{2};
    bool projectile{};
    EffectStyle style{EffectStyle::Fire};
};

struct CombatText { Point p; string text; Color color; double started{},duration{.9}; };

struct SpellInfo { string name, description; int cost{}, range{}; };

inline SpellInfo spellInfo(SpellKind spell) {
    switch(spell) {
        case SpellKind::Fireball:return{"Fireball","Hurls a piercing fireball that scorches its path, cracks nearby stone, and ignites the point of impact.",3,15};
        case SpellKind::FrostLance:return{"Frost Lance","Drives a piercing shard of ice down a clear line and disrupts creatures caught in its path.",2,18};
        case SpellKind::ChainLightning:return{"Chain Lightning","Sends a violent electrical arc down a clear line through every creature it reaches.",5,16};
        case SpellKind::LuminousNova:return{"Luminous Nova","Releases a radiant blast around the caster, striking everything within two tiles.",6,0};
        case SpellKind::Meteor:return{"Meteor","Calls down a vast explosion that devastates creatures and demolishes terrain around the target.",9,14};
        case SpellKind::ArcaneBeam:return{"Arcane Beam","Projects a piercing violet beam through every creature in a clear line.",3,15};
        case SpellKind::Stonebreak:return{"Stonebreak","Ruptures creatures in a short line and fractures the masonry around the target.",3,10};
        case SpellKind::SeismicWave:return{"Seismic Wave","Drives a fault through a clear line of creatures and breaks stone around its endpoint.",6,10};
        case SpellKind::ShieldFlare:return{"Shield Flare","Wraps the caster in a temporary defensive ward of hardened light.",4,0};
        case SpellKind::ThornVolley:return{"Thorn Volley","Launches spectral thorns through every creature in a clear line.",3,15};
        case SpellKind::ShadowBolt:return{"Shadow Bolt","Fires an efficient umbral projectile through a clear line.",2,15};
        case SpellKind::SunLance:return{"Sun Lance","Projects a piercing lance of radiance that burns through a clear line.",3,15};
        case SpellKind::HealingLight:return{"Healing Light","Restores health with a concentrated pulse of living light.",5,0};
        case SpellKind::FlameCleave:return{"Flame Cleave","Sweeps weapon-fed flame through a short line in front of the caster.",4,4};
        case SpellKind::GroundQuake:return{"Ground Quake","Detonates a tectonic burst that damages a broad area and pulverises surrounding terrain.",7,10};
        case SpellKind::BattleCry:return{"Battle Cry","Temporarily increases both attack and defence.",5,0};
        case SpellKind::IceStorm:return{"Ice Storm","Conjures a freezing blizzard across a wide area around the target.",7,12};
        case SpellKind::Blink:return{"Blink","Teleports the caster to a visible, unoccupied tile within range.",4,9};
        case SpellKind::PrismaticOrb:return{"Prismatic Orb","Fires a powerful chromatic projectile through a clear line.",5,15};
        case SpellKind::Multishot:return{"Multishot","Sends a spectral barrage through every creature in a clear line.",4,15};
        case SpellKind::PoisonCloud:return{"Poison Cloud","Fills a wide area around the target with lingering venom and obscuring smoke.",5,12};
        case SpellKind::MarkedShot:return{"Marked Shot","Looses a heavy, precisely aimed projectile through a clear line.",5,15};
        case SpellKind::SolarRay:return{"Solar Ray","Carves a corridor of concentrated daylight through every creature in its path.",6,15};
        case SpellKind::Sanctuary:return{"Sanctuary","Restores health and surrounds the caster with a long-lasting ward.",7,0};
        default:return{"Smite","Detonates focused judgement across a small area around the target.",7,12};
    }
}

inline Item makeItem(ItemKind kind) {
    switch(kind) {
        case ItemKind::Potion:return{kind,"healing potion","Restores 14 HP.",'!',{225,65,78}};
        case ItemKind::GreaterPotion:return{kind,"greater healing elixir","Restores 30 HP.",'!',{255,42,62}};
        case ItemKind::Ration:return{kind,"trail ration","Restores a full day of food.",'%',{196,151,82}};
        case ItemKind::Flask:return{kind,"water flask","Restores a full day of water and leaves an empty flask.",'!',{72,169,245}};
        case ItemKind::ManaTonic:return{kind,"mana tonic","Restores 8 mana.",'!',{156,95,255}};
        case ItemKind::Bomb:return{kind,"alchemist bomb","Explodes across a small area and shatters nearby walls.",'o',{255,105,32}};
        case ItemKind::SmokeBomb:return{kind,"smoke bomb","Fills the surrounding area with concealing smoke.",'o',{145,145,155}};
        case ItemKind::WallTorch:return{kind,"wall torch","Can be mounted on an adjacent bare wall.",'/',{255,148,45}};
        case ItemKind::Waystone:return{kind,"waystone","Returns you to the current floor's upward stair.",'*',{105,160,255}};
        case ItemKind::Ambrosia:return{kind,"sun-fruit ambrosia","Fully restores food, water, and mana.",'%',{255,210,75}};
        case ItemKind::MightElixir:return{kind,"dragonroot elixir","Temporarily increases attack.",'!',{235,85,40}};
        case ItemKind::GuardElixir:return{kind,"ironhide elixir","Temporarily increases defence.",'!',{125,145,165}};
        case ItemKind::FocusElixir:return{kind,"focus elixir","Temporarily increases spell power.",'!',{185,95,255}};
        case ItemKind::EmptyFlask:return{kind,"empty water flask","Can be refilled at a river, well, spring, or cistern.",'!',{105,132,148}};
        default:return{kind,"equipment","Wearable equipment.",']',{200,190,160}};
    }
}

inline Item equipment(string name, Slot slot, char glyph, int attack=0, int defense=0, int spell=0, double light=0) {
    string description;
    if(name.find("militia")!=string::npos||name.find("apprentice")!=string::npos||name.find("ashwood")!=string::npos||name.find("dawnwatch")!=string::npos)description="Dependable Emberfall issue, prepared for a first expedition.";
    else if(name.find("iron longsword")!=string::npos||name.find("watchman")!=string::npos)description="Practical Emberfall workmanship built for hard use on the road.";
    else if(name.find("vault-forged")!=string::npos)description="Recovered from a lost armoury; old forge marks remain beneath the corrosion.";
    else description="Recovered from a fallen foe; its balance still remembers a previous owner.";
    Item item{ItemKind::Equipment,std::move(name),std::move(description),glyph,{210,190,150}};
    item.slot=slot;item.attack=attack;item.defense=defense;item.spell=spell;item.light=light;return item;
}

inline vector<SpellKind> startingSpells(ClassKind c) {
    switch(c) {
        case ClassKind::Vanguard:return{SpellKind::Stonebreak,SpellKind::ShieldFlare};
        case ClassKind::Arcanist:return{SpellKind::ArcaneBeam,SpellKind::Fireball};
        case ClassKind::Ranger:return{SpellKind::ThornVolley,SpellKind::ShadowBolt};
        default:return{SpellKind::SunLance,SpellKind::HealingLight};
    }
}

inline string rewardDescription(SkillNode::Reward reward, optional<SpellKind> spell) {
    switch(reward) {
        case SkillNode::Reward::Vitality:return "Each rank grants +4 maximum HP and +1 Constitution.";
        case SkillNode::Reward::Might:return "Each rank grants +1 base attack and +1 Strength.";
        case SkillNode::Reward::Guard:return "Each rank grants +1 base defence.";
        case SkillNode::Reward::Arcana:return "Each rank grants +3 maximum mana and +1 Intellect.";
        case SkillNode::Reward::Radiance:return "Each rank grants +1 spell power and +0.25 lantern radius.";
        default:return spellInfo(*spell).description+" Each rank increases its power; rank 3 also reduces its mana cost.";
    }
}

inline vector<SkillNode> buildSkillTree(ClassKind calling) {
    using R=SkillNode::Reward; vector<SkillNode> nodes; const int gates[6]{2,3,4,5,7,9};
    auto passive=[](string name,R reward){return tuple<string,R,optional<SpellKind>>{name,reward,nullopt};};
    auto magic=[](string name,SpellKind spell){return tuple<string,R,optional<SpellKind>>{name,R::Spell,spell};};
    auto add=[&](string branch,int bi,initializer_list<tuple<string,R,optional<SpellKind>>> defs){int tier=0;for(auto&[name,reward,spell]:defs){SkillNode n;n.id=to_string(bi)+":"+to_string(tier);n.branch=branch;n.branchIndex=bi;n.tier=tier;n.requiredLevel=gates[tier];n.prerequisite=tier?to_string(bi)+":"+to_string(tier-1):"";n.name=name;n.reward=reward;n.spell=spell;n.description=rewardDescription(reward,spell);nodes.push_back(n);tier++;}};
    if(calling==ClassKind::Vanguard){
        add("STEEL",0,{passive("Tempered Edge",R::Might),passive("Measured Riposte",R::Guard),magic("Flame Cleave",SpellKind::FlameCleave),passive("Relentless",R::Might),passive("Executioner",R::Might),passive("War Incarnate",R::Vitality)});
        add("BULWARK",1,{passive("Iron Skin",R::Guard),passive("Deep Reserves",R::Vitality),magic("Shield Flare",SpellKind::ShieldFlare),passive("Immovable",R::Guard),magic("Battle Cry",SpellKind::BattleCry),passive("Unbroken",R::Vitality)});
        add("EARTHFIRE",2,{magic("Stonebreak",SpellKind::Stonebreak),passive("Emberblood",R::Radiance),magic("Seismic Wave",SpellKind::SeismicWave),magic("Fireball",SpellKind::Fireball),magic("Luminous Nova",SpellKind::LuminousNova),magic("Ground Quake",SpellKind::GroundQuake)});
        add("COMMAND",3,{passive("Battle Rhythm",R::Arcana),passive("Hard March",R::Vitality),passive("Weapon Master",R::Might),passive("Hold the Line",R::Guard),passive("Beacon Banner",R::Radiance),passive("Warlord",R::Might)});
    } else if(calling==ClassKind::Arcanist){
        add("ARCANA",0,{passive("Expanded Mind",R::Arcana),magic("Arcane Beam",SpellKind::ArcaneBeam),magic("Prismatic Orb",SpellKind::PrismaticOrb),passive("Deep Current",R::Arcana),magic("Blink",SpellKind::Blink),passive("Archmagus",R::Radiance)});
        add("RUNES",1,{passive("Runic Blood",R::Vitality),passive("Mana Ward",R::Guard),passive("Sigil Memory",R::Arcana),passive("Reactive Glyph",R::Guard),passive("Spell Mirror",R::Radiance),passive("Perfect Ward",R::Guard)});
        add("ELEMENTS",2,{magic("Fireball",SpellKind::Fireball),magic("Frost Lance",SpellKind::FrostLance),magic("Chain Lightning",SpellKind::ChainLightning),magic("Meteor",SpellKind::Meteor),magic("Ice Storm",SpellKind::IceStorm),passive("Elemental Crown",R::Radiance)});
        add("AETHER",3,{passive("Aether Sense",R::Radiance),passive("Reservoir",R::Arcana),passive("Glass Soul",R::Vitality),passive("Overchannel",R::Radiance),passive("Endless Formula",R::Arcana),passive("Aether Ascendant",R::Radiance)});
    } else if(calling==ClassKind::Ranger){
        add("HUNT",0,{passive("Deadeye",R::Might),magic("Thorn Volley",SpellKind::ThornVolley),magic("Multishot",SpellKind::Multishot),passive("Heartseeker",R::Might),magic("Marked Shot",SpellKind::MarkedShot),passive("Apex Hunter",R::Might)});
        add("SURVIVAL",1,{passive("Trail Hardened",R::Vitality),passive("Barkskin",R::Guard),passive("Forager",R::Vitality),magic("Poison Cloud",SpellKind::PoisonCloud),passive("Ghost Step",R::Guard),passive("Never Lost",R::Vitality)});
        add("GLOOM",2,{magic("Shadow Bolt",SpellKind::ShadowBolt),magic("Frost Lance",SpellKind::FrostLance),magic("Chain Lightning",SpellKind::ChainLightning),magic("Fireball",SpellKind::Fireball),passive("Night Eyes",R::Radiance),passive("Gloom Walker",R::Guard)});
        add("WILD",3,{passive("Wild Instinct",R::Might),passive("Deep Breath",R::Arcana),passive("Thick Hide",R::Guard),passive("Predator Light",R::Radiance),passive("Pack Hunter",R::Might),passive("Primal Legend",R::Vitality)});
    } else {
        add("DEVOTION",0,{passive("Living Radiance",R::Radiance),magic("Sun Lance",SpellKind::SunLance),magic("Solar Ray",SpellKind::SolarRay),passive("Consecration",R::Radiance),magic("Luminous Nova",SpellKind::LuminousNova),passive("Beacon Soul",R::Arcana)});
        add("AEGIS",1,{passive("Sacred Ward",R::Guard),passive("Martyr Heart",R::Vitality),magic("Shield Flare",SpellKind::ShieldFlare),passive("Golden Bulwark",R::Guard),magic("Sanctuary",SpellKind::Sanctuary),passive("Undying Flame",R::Vitality)});
        add("MIRACLES",2,{magic("Healing Light",SpellKind::HealingLight),passive("Font of Grace",R::Arcana),magic("Chain Lightning",SpellKind::ChainLightning),passive("Wrathful Light",R::Radiance),magic("Smite",SpellKind::Smite),passive("Living Miracle",R::Radiance)});
        add("PILGRIM",3,{passive("Pilgrim Vigour",R::Vitality),passive("Steady Flame",R::Guard),passive("Deep Faith",R::Arcana),passive("Dawn March",R::Might),passive("Halo",R::Radiance),passive("Saint of Embers",R::Vitality)});
    } return nodes;
}
