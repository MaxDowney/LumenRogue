#define NOMINMAX
#include <windows.h>
#include <conio.h>
#include <algorithm>
#include <array>
#include <cmath>
#include <chrono>
#include <cstdint>
#include <cstdlib>
#include <filesystem>
#include <fstream>
#include <iomanip>
#include <iostream>
#include <map>
#include <optional>
#include <queue>
#include <random>
#include <set>
#include <sstream>
#include <string>
#include <unordered_set>
#include <utility>
#include <vector>

using namespace std;
#include "TerminalSession.hpp"

struct Point {
    int x{}, y{};
    friend bool operator==(Point a, Point b) { return a.x == b.x && a.y == b.y; }
    friend bool operator!=(Point a, Point b) { return !(a == b); }
    friend bool operator<(Point a, Point b) { return a.y == b.y ? a.x < b.x : a.y < b.y; }
    Point operator+(Point b) const { return {x + b.x, y + b.y}; }
    int dist2(Point b) const { int dx=x-b.x,dy=y-b.y; return dx*dx+dy*dy; }
};

struct PointHash { size_t operator()(Point p) const { return (uint64_t(uint32_t(p.x)) << 32) ^ uint32_t(p.y); } };
struct Color { int r{},g{},b{}; Color scaled(double n) const { return {clamp(int(r*n),0,255),clamp(int(g*n),0,255),clamp(int(b*n),0,255)}; } };
struct Tile { char glyph{' '}; Color color{}; bool walk{}, opaque{}, solid{}; string name; int toughness{}; double reflectivity{.15}; };
struct Light { Point p; Color color; double radius{}, intensity{}, flicker{.12}, phase{}, softness{.28}, diffraction{.12}, bounce{.08}; };
struct LinearLight { double r{},g{},b{}; };
#include "Parity.hpp"
#include "GeneratedPortraits.hpp"
#include "Sound.hpp"
enum class Intent { Idle, Roam, Advance, Strike, Shoot, Retreat, Guard, Sleep };
enum class Reaction { None, Block, Dodge, Ward };
enum class InventorySort { Type, Power, Newest, Name };
enum class Faction { Beast, Goblin, Undead, Construct, Cult };

struct Monster {
    Point p{}, home{};
    string name;
    char glyph{'m'};
    Color color{190,100,80};
    int hp{10}, maxHp{10}, attack{4}, defense{1}, territory{10}, alert{};
    Faction faction{Faction::Beast};
    Intent intent{Intent::Idle};
    bool boss{}, ranged{}, nocturnal{}, rewardClaimed{};
};

struct Npc {
    string id, name, role, greeting;
    Point work{}, home{}, evening{}, p{};
    Color color{220,190,120};
    string schedule{"working"};
};

struct Landmark { Point p; string name, lore; };
struct Travel { Point p; int targetMap{}; Point arrival{}; string label; bool castleLocked{}; };
struct Quest {
    string id,title,giver,description,objective,type,target,flag,prerequisite;
    int need{1}, progress{};
    bool accepted{},done{};
};

struct DialogueChoice { string label,next,action; };

struct MapData {
    int w{},h{};
    string name;
    bool outdoor{}, town{};
    BiomeKind biome{BiomeKind::Emberfall};
    vector<Tile> tiles;
    vector<uint8_t> explored;
    vector<Light> lights;
    vector<Monster> monsters;
    vector<Npc> npcs;
    vector<Landmark> landmarks;
    vector<Travel> travels;
    vector<GroundItem> groundItems;
    vector<ProvisionSource> provisions;
    vector<WallTorchFeature> wallTorches;
    vector<TrapFeature> traps;
    vector<MachineFeature> machines;
    vector<ElementCell> elements;
    vector<Point> critical;
    Point start{}, up{-1,-1}, down{-1,-1};
    Tile& at(Point p) { return tiles[size_t(p.y)*w+p.x]; }
    const Tile& at(Point p) const { return tiles[size_t(p.y)*w+p.x]; }
    bool contains(Point p) const { return p.x>=0&&p.y>=0&&p.x<w&&p.y<h; }
    ElementCell& element(Point p) { return elements[size_t(p.y)*w+p.x]; }
    const ElementCell& element(Point p) const { return elements[size_t(p.y)*w+p.x]; }
};

// Preserve the four legacy site maps at indices 7-10 so existing saves still
// load into the same places. The newly charted sixth Delve floor is appended.
static constexpr int MAIN_DEPTHS=6;
static constexpr int MAIN_FIRST_MAP=2;
static constexpr int MAIN_FIFTH_MAP=6;
static constexpr int SITE_FIRST_MAP=7;
static constexpr int SITE_COUNT=4;
static constexpr int MAIN_SIXTH_MAP=11;
static constexpr int mainMapForDepth(int depth){return depth<MAIN_DEPTHS?MAIN_FIRST_MAP+depth-1:MAIN_SIXTH_MAP;}
static constexpr int mainDepthForMap(int mapIndex){return mapIndex>=MAIN_FIRST_MAP&&mapIndex<=MAIN_FIFTH_MAP?mapIndex-MAIN_FIRST_MAP+1:mapIndex==MAIN_SIXTH_MAP?MAIN_DEPTHS:0;}

static constexpr array<Point,4> DIRS{{{1,0},{-1,0},{0,1},{0,-1}}};
static Tile Grass{'.',{76,130,67},true,false,false,"grass"};
static Tile Road{'.',{145,127,91},true,false,false,"road"};
static Tile Floor{'.',{126,120,108},true,false,false,"stone floor"};
static Tile Wall{'#',{105,108,118},false,true,true,"wall",20,.22};
static Tile Tree{'T',{40,112,48},false,true,true,"ashpine"};
static Tile Boulder{'O',{112,112,102},false,false,true,"boulder"};
static Tile Water{'~',{55,125,180},false,false,false,"deep water"};
static Tile Bridge{'=',{165,118,70},true,false,false,"bridge"};
static Tile Door{'+',{175,120,68},false,true,true,"closed door",10,.18};
static Tile Rubble{':',{105,95,86},true,false,false,"rubble",0,.18};
static Tile WallTorchTile{'i',{255,132,32},false,true,true,"mounted wall torch",12,.2};
static Tile FoodTile{'%',{214,168,82},true,false,false,"provision cache"};
static Tile WaterTile{'w',{80,188,245},true,false,false,"fresh water source",0,.42};
static Tile EmptySupply{'x',{88,82,72},true,false,false,"empty supply cache"};
static Tile MachineTile{'&',{180,185,110},true,false,false,"ancient machinery"};
static Tile StreetLamp{'t',{220,162,74},true,false,false,"street lamp",0,.18};
static Tile TownCobble{'.',{142,132,116},true,false,false,"town cobble",0,.28};
static Tile TownFloor{'.',{142,104,68},true,false,false,"timber interior",0,.16};
static Tile TownWall{'#',{126,108,94},false,true,true,"timber-and-stone wall",18,.2};
static Tile TownWindow{'o',{245,176,82},false,true,true,"lit townhouse window",12,.34};
static Tile TownSign{'?',{245,194,92},true,false,false,"building plaque",0,.2};
static bool potableWaterTile(const Tile&tile){return tile.name=="deep water"||tile.name=="fresh water source";}

static uint32_t mixSeed(uint32_t seed, uint32_t salt) {
    uint32_t v=seed ^ salt*0x9E3779B9u; v^=v>>16; v*=0x7FEB352Du; v^=v>>15; v*=0x846CA68Bu; return v^(v>>16);
}
static int cheb(Point a,Point b){return max(abs(a.x-b.x),abs(a.y-b.y));}
static string intentName(Intent i){ static const char* n[]={"IDLE","ROAM","ADVANCE","STRIKE","SHOOT","RETREAT","GUARD","SLEEP"}; return n[int(i)]; }
static string reactionName(Reaction r){ static const char* n[]={"NONE","BLOCK","DODGE","WARD"}; return n[int(r)]; }
static string raceName(RaceKind r){static const char*n[]={"Human","Dwarf","Elf","Ashborn"};return n[int(r)];}
static string className(ClassKind c){static const char*n[]={"Vanguard","Arcanist","Ranger","Lightbearer"};return n[int(c)];}
static string slotDisplayName(Slot slot){switch(slot){case Slot::MainHand:return"MAIN HAND";case Slot::OffHand:return"OFF HAND";case Slot::Head:return"HEAD";case Slot::Body:return"BODY";case Slot::Hands:return"HANDS";case Slot::Feet:return"FEET";case Slot::Ring:return"RING";case Slot::Amulet:return"AMULET";default:return"ITEM";}}
static string slotName(Slot slot){return slotDisplayName(slot);}

struct CharacterProfile {
    int strength{10},dexterity{10},intellect{10},constitution{10},wisdom{10};
    int attack{5},defense{1},maxMana{10},maxHp{};
    double lanternRadius{9},lanternIntensity{1};
    string weapon,playstyle;
};

static CharacterProfile characterProfile(RaceKind race,ClassKind calling){
    CharacterProfile p;if(race==RaceKind::Human){p.strength++;p.dexterity++;p.intellect++;p.constitution++;p.wisdom++;}else if(race==RaceKind::Dwarf){p.strength+=2;p.constitution+=4;p.dexterity--;}else if(race==RaceKind::Elf){p.dexterity+=3;p.intellect+=3;p.constitution--;}else{p.intellect+=2;p.wisdom+=2;p.constitution+=2;p.lanternIntensity+=.15;}
    if(calling==ClassKind::Vanguard){p.strength+=4;p.constitution+=3;p.attack=7;p.defense=3;p.maxMana=7;p.weapon="militia broadsword";p.playstyle="Hold ground, punish advances, and rupture stone with earthfire.";}else if(calling==ClassKind::Arcanist){p.intellect+=5;p.wisdom+=2;p.attack=3;p.defense=0;p.maxMana=18;p.weapon="apprentice emberstaff";p.playstyle="Shape corridors with volatile magic; positioning is your armour.";}else if(calling==ClassKind::Ranger){p.dexterity+=5;p.wisdom+=2;p.attack=6;p.defense=1;p.maxMana=11;p.lanternRadius++;p.weapon="ashwood shortbow";p.playstyle="Control distance, read terrain, and hunt through dim approaches.";}else{p.wisdom+=5;p.constitution+=2;p.attack=5;p.defense=2;p.maxMana=14;p.lanternIntensity+=.18;p.weapon="dawnwatch mace";p.playstyle="Sustain the expedition with radiant wards, healing, and searing light.";}p.maxHp=20+p.constitution+(calling==ClassKind::Vanguard?8:0);return p;
}

static vector<string> raceLore(RaceKind race){
    if(race==RaceKind::Human)return{"HEARTHBORN","Raised among maintained roads and settlements where shared work is a form of law.","The Lantern Compact feels familiar: survival made from ordinary promises.","Legacy: diplomacy, resolve, and no inherited weakness."};
    if(race==RaceKind::Dwarf)return{"DEEP-KIN","Their oath-smiths once taught Valeward to make iron remember an agreement.","The crown stamped over those old forge marks is both insult and evidence.","Legacy: endurance enough to turn attrition into advantage."};
    if(race==RaceKind::Elf)return{"TWILIGHT SCION","Their eyes distinguish reflected light from the sky that cast it.","Moonwell's false stars are a dangerous corruption of an old twilight art.","Legacy: perception, finesse, and dangerous spellcraft."};
    return{"EMBER-SOULED","Ashborn carry a coal descended from fires freely shared, never owned.","The Regent's attempt to crown the common flame is felt as a wound behind the heart.","Legacy: a naturally warmer and stronger personal light."};
}

static vector<string> generatedPortrait(int index,int columns=32){
    index=clamp(index,0,lumen_portraits::count-1);columns=clamp(columns,8,32);int rows=max(3,int(lround(columns*12.0/32)));vector<string> result;
    auto sample=[&](int x,int y){auto&p=lumen_portraits::bitmaps[index][(y/2)*32+x];return y%2?Color{p.br,p.bg,p.bb}:Color{p.tr,p.tg,p.tb};};
    auto pixel=[&](int x,int y){int x0=x*32/columns,x1=max(x0+1,(x+1)*32/columns),y0=y*24/(rows*2),y1=max(y0+1,(y+1)*24/(rows*2)),r=0,g=0,b=0,count=0;for(int sy=y0;sy<y1;sy++)for(int sx=x0;sx<x1;sx++){auto c=sample(sx,sy);r+=c.r;g+=c.g;b+=c.b;count++;}return Color{r/count,g/count,b/count};};
    for(int y=0;y<rows;y++){ostringstream line;for(int x=0;x<columns;x++){auto top=pixel(x,y*2),bottom=pixel(x,y*2+1);if(max({top.r,top.g,top.b,bottom.r,bottom.g,bottom.b})<4)line<<"\x1b[48;2;9;12;18m ";else line<<"\x1b[38;2;"<<top.r<<';'<<top.g<<';'<<top.b<<"m\x1b[48;2;"<<bottom.r<<';'<<bottom.g<<';'<<bottom.b<<"m▀";}line<<"\x1b[0m\x1b[48;2;9;12;18m\x1b[38;2;208;217;228m";result.push_back(line.str());}return result;
}

static vector<string> resizePortrait(const vector<string>&source,int columns){
    if(columns==32)return source;static const vector<vector<string>> originals=[](){vector<vector<string>> values;for(int i=0;i<lumen_portraits::count;i++)values.push_back(generatedPortrait(i));return values;}();
    for(int i=0;i<int(originals.size());i++)if(source==originals[size_t(i)])return generatedPortrait(i,columns);return source;
}

static int generatedPortraitIndex(const string&id){
    for(int i=0;i<lumen_portraits::count;i++)if(id==lumen_portraits::ids[i])return i;
    return 0;
}

static vector<string> generatedPortrait(const string&id){return generatedPortrait(generatedPortraitIndex(id));}

static vector<string> racePortrait(RaceKind race){return generatedPortrait(int(race));}

static vector<string> classLore(ClassKind calling){
    if(calling==ClassKind::Vanguard)return{"FRONT-LINE WARDEN","Steel · Bulwark · Earthfire · Command","Begins with Stonebreak and Shield Flare.","Break the Regent's fortifications without becoming another instrument of command."};
    if(calling==ClassKind::Arcanist)return{"AETHER CARTOGRAPHER","Arcana · Runes · Elements · Aether","Begins with Arcane Beam and Fireball.","Read the Compact as a working machine, then rewrite the instruction that corrupted it."};
    if(calling==ClassKind::Ranger)return{"MARCH STALKER","Hunt · Survival · Gloom · Wild","Begins with Thorn Volley and Shadow Bolt.","Follow physical traces between three wounded villages and the power drawing them inward."};
    return{"KEEPER OF THE DAWN","Devotion · Aegis · Miracles · Pilgrim","Begins with Sun Lance and Healing Light.","Prove by action that light can protect a community without belonging to a crown."};
}

static vector<string> classPortrait(ClassKind calling){return generatedPortrait(4+int(calling));}

static vector<string> npcPortrait(const Npc&npc){
    if(npc.id=="mara")return classPortrait(ClassKind::Lightbearer);
    if(npc.id=="orra"||npc.id=="roen")return classPortrait(ClassKind::Vanguard);
    if(npc.id=="sable")return classPortrait(ClassKind::Arcanist);
    if(npc.id=="neris"||npc.id=="nella")return classPortrait(ClassKind::Ranger);
    if(npc.id=="dara")return racePortrait(RaceKind::Dwarf);
    if(npc.id=="elin")return racePortrait(RaceKind::Human);
    return generatedPortrait("inspect-npc");
}

static vector<string> itemPortrait(const Item&item){
    if(item.equippable()){if(item.slot==Slot::Head||item.slot==Slot::Ring||item.slot==Slot::Amulet)return generatedPortrait("item-jewellery");if(item.slot==Slot::Body||item.slot==Slot::OffHand||item.slot==Slot::Hands||item.slot==Slot::Feet)return generatedPortrait("item-armour");return generatedPortrait("inspect-item");}
    if(item.kind==ItemKind::Ration||item.kind==ItemKind::Ambrosia)return generatedPortrait("inspect-food");
    if(item.kind==ItemKind::Flask||item.kind==ItemKind::EmptyFlask)return generatedPortrait("inspect-water-source");
    if(item.kind==ItemKind::WallTorch)return generatedPortrait("inspect-torch");
    if(item.kind==ItemKind::Bomb||item.kind==ItemKind::SmokeBomb)return generatedPortrait("item-bomb");
    if(item.kind==ItemKind::Waystone)return generatedPortrait("inspect-landmark");
    return generatedPortrait("item-potion");
}

static string itemCategory(const Item&item){
    if(item.equippable())return slotDisplayName(item.slot)+" EQUIPMENT";
    switch(item.kind){case ItemKind::Potion:case ItemKind::GreaterPotion:return"HEALING";case ItemKind::Ration:return"PROVISION";case ItemKind::Flask:return"WATER";case ItemKind::EmptyFlask:return"EMPTY CONTAINER";case ItemKind::ManaTonic:return"ARCANE TONIC";case ItemKind::Bomb:return"EXPLOSIVE";case ItemKind::SmokeBomb:return"TACTICAL";case ItemKind::WallTorch:return"LIGHT SOURCE";case ItemKind::Waystone:return"TRAVEL RELIC";case ItemKind::Ambrosia:return"RESTORATIVE";default:return"COMBAT ELIXIR";}
}

static string itemQuality(const Item&item){
    if(!item.equippable())return item.kind==ItemKind::Ambrosia||item.kind==ItemKind::Waystone?"RARE":item.kind==ItemKind::GreaterPotion?"REFINED":"FIELD";
    int score=abs(item.attack)+abs(item.defense)+abs(item.spell)+int(lround(item.light*5));return score>=7?"RELIC":score>=4?"ENCHANTED":score>=2?"TEMPERED":"COMMON";
}

static string inventorySortName(InventorySort sort){
    switch(sort){case InventorySort::Type:return"TYPE";case InventorySort::Power:return"POWER";case InventorySort::Newest:return"NEWEST";default:return"NAME";}
}

static int itemPower(const Item&item){
    if(item.equippable())return max(0,item.attack)*4+max(0,item.defense)*3+max(0,item.spell)*4+int(lround(max(0.0,item.light)*6));
    switch(item.kind){case ItemKind::Ambrosia:return 40;case ItemKind::Waystone:return 36;case ItemKind::Bomb:return 32;case ItemKind::GreaterPotion:return 28;case ItemKind::SmokeBomb:return 24;case ItemKind::MightElixir:case ItemKind::GuardElixir:case ItemKind::FocusElixir:return 22;case ItemKind::ManaTonic:return 18;case ItemKind::Potion:return 16;case ItemKind::WallTorch:return 14;case ItemKind::Ration:case ItemKind::Flask:return 12;default:return 0;}
}

static bool sameStackIdentity(const Item&a,const Item&b){
    return !a.equipped&&!b.equipped&&a.kind==b.kind&&a.name==b.name&&a.description==b.description&&a.glyph==b.glyph&&a.color.r==b.color.r&&a.color.g==b.color.g&&a.color.b==b.color.b&&a.slot==b.slot&&a.attack==b.attack&&a.defense==b.defense&&a.spell==b.spell&&abs(a.light-b.light)<.000001;
}

static string signedStat(int value){return string(value>=0?"+":"")+to_string(value);}
static string signedLightStat(double value){ostringstream out;out<<(value>=0?"+":"")<<fixed<<setprecision(1)<<value;return out.str();}
static string lightStat(double value){ostringstream out;out<<fixed<<setprecision(1)<<value;return out.str();}
static string equipmentStatLine(const Item&item){return "ATK "+signedStat(item.attack)+"   DEF "+signedStat(item.defense)+"   SPELL "+signedStat(item.spell)+"   LIGHT "+signedLightStat(item.light);}

static string acquiredLabel(const Item&item){
    int total=max(0,420+item.acquiredTurn*6),day=total/1440+1,minute=total%1440;ostringstream out;out<<"DAY "<<day<<" · "<<setfill('0')<<setw(2)<<minute/60<<':'<<setw(2)<<minute%60;return out.str();
}

static string itemStudy(const Item&item){
    if(item.name.find("emberstaff")!=string::npos)return"Copper bindings warm when a spell is held ready.";
    if(item.name.find("broadsword")!=string::npos)return"A broad, honest blade balanced for a shield line.";
    if(item.name.find("shortbow")!=string::npos)return"Ashwood limbs flex silently even in cold air.";
    if(item.name.find("dawnwatch")!=string::npos)return"Sunburst flanges catch lantern light before impact.";
    if(item.name.find("glassfang")!=string::npos)return"Translucent chitin still carries a web's faint vibration.";
    if(item.name.find("gargoyle")!=string::npos)return"Jointed stone plates settle against the body like masonry.";
    if(item.name.find("lich-light")!=string::npos)return"Cold points of light orbit the metal without casting shadows.";
    if(item.name.find("furnace-heart")!=string::npos)return"Heat pulses along the fuller like a second heartbeat.";
    if(item.name.find("Iven")!=string::npos)return"The guard bears a lamplighter's circuit mark and six fresh tally cuts.";
    if(item.name.find("Cal Flint")!=string::npos)return"Orra's maker mark survives beside black oath-iron scales fused to the break.";
    if(item.name.find("Pell")!=string::npos)return"Survey coordinates cover the grip; every route converges on the sixth depth.";
    if(item.name.find("oath")!=string::npos||item.name.find("compact")!=string::npos)return"Four joined hands remain visible beneath a later crown stamp.";
    if(item.name.find("vault-forged")!=string::npos)return"Old balance, little ornament, and a compact-era forge mark beneath the corrosion.";
    if(item.kind==ItemKind::Potion||item.kind==ItemKind::GreaterPotion)return"A red meniscus clings to the glass before settling.";
    if(item.kind==ItemKind::ManaTonic||item.kind==ItemKind::FocusElixir)return"Violet motes align whenever the stopper turns.";
    if(item.kind==ItemKind::Ration)return"Waxed cloth keeps salt, grain, and dried fruit expedition-ready.";
    if(item.kind==ItemKind::Flask)return"The stopper is tied down; drinking leaves the flask ready to refill.";
    if(item.kind==ItemKind::EmptyFlask)return"River water, wells, springs, and cisterns can make this useful again.";
    if(item.kind==ItemKind::Bomb)return"A scored shell directs the blast through stone and armour.";
    if(item.kind==ItemKind::SmokeBomb)return"Silver powder shifts inside without making a sound.";
    if(item.kind==ItemKind::WallTorch)return"Iron teeth bite mortar while the wrapped head holds a steady flame.";
    if(item.kind==ItemKind::Waystone)return"Its carved line always points toward the nearest upward stair.";
    if(item.kind==ItemKind::Ambrosia)return"The fruit gives off the colour of late afternoon even underground.";
    if(item.equippable())return"Monster-work and field repairs tell competing stories across its surface.";
    return"A practical object made valuable by the distance from home.";
}

static string fixtureStory(const string&name){
    if(name.find("Iven's labelled wick cutter")!=string::npos)return"The brass handle is warm. Iven scratched SIX IS OPEN beside his name before leaving on the final survey.";
    if(name.find("blue-flame burner")!=string::npos)return"Three burners contain different oil but hold the same blue flame, proving the colour comes from the old circuit below.";
    if(name.find("Lamp circuit model")!=string::npos)return"Four channels run into a central flame. Blue beads advance from ROOT, ROAD, and RIVER toward a socket labelled DEPTH SIX.";
    if(name.find("compact shelf")!=string::npos)return"Dust outlines a missing volume exactly the width of the first Lantern Compact. The catalogue card remains; the borrower's name was scraped away.";
    if(name.find("missing Survey Six")!=string::npos)return"Volumes one through five lean around a clean gap. A hidden index lists Survey Six as FOUNDATION AND REFUSAL MECHANISM.";
    if(name.find("four-seat witness table")!=string::npos)return"Three village emblems and Emberfall's lamp surround an empty centre. A crown was added later, then cut out so deeply the table split.";
    if(name.find("date press set ninety")!=string::npos)return"The press is locked to the night the last Regent vanished. Someone has used it recently; the ink pad is still damp.";
    if(name.find("oath-iron")!=string::npos)return"Hammer scars form four overlapping rhythms. The fourth strike stops whenever the metal is turned toward Valeward.";
    if(name.find("black tide-ring")!=string::npos)return"The waterline is too high for the trough's volume. Orra's chalk note reads: METAL RANG; WATER ANSWERED.";
    if(name.find("unreturned blades")!=string::npos)return"Hooks are labelled for Iven Vey, Cal Flint, and Surveyor Pell. Only Cal's scabbard remains.";
    if(name.find("Hedge Bell Memorial")!=string::npos)return"Each bell-rope belongs to a household. One new Valeward wire bypasses every hand and runs directly toward Hollowroot.";
    if(name.find("Roadwarden's Measure")!=string::npos)return"Thirty-seven dated chalk lines record steady subsidence toward the Sunken Barrow. Dara has signed every measurement.";
    if(name.find("Reedwater Flood Board")!=string::npos)return"The newest marks descend against rainfall and season. A child's drawing beside them shows a crown beneath the water.";
    if(name.find("child's bed")!=string::npos)return"A blue curtain has been folded beneath the pillow, the town's quiet sign that someone from this house is missing.";
    if(name.find("family table")!=string::npos||name.find("supper table")!=string::npos)return"One place remains set with a clean bowl. Nobody has admitted they are ready to put it away.";
    if(name.find("cooking hearth")!=string::npos)return"Three covered bowls warm at the edge of the fire, labelled IVEN, CAL, and PELL in Tovan's blunt handwriting.";
    return"Wear, repairs, stains, and careful placement show how ordinary work has carried the Compact long after its rulers failed it.";
}

static string npcPresence(const Npc&npc){
    if(npc.id=="mara")return"Soot marks every finger except the one tracing routes on her circuit map. She looks tired, exact, and unwilling to confuse either condition with defeat.";
    if(npc.id=="orra")return"She keeps speaking while her hands inspect your buckles and blade edge. The empty hook marked CAL is always within her line of sight.";
    if(npc.id=="neris")return"Knotted cords, chalk, and a brass rule hang from their belt. Every alarming claim is met with a measurement rather than disbelief.";
    if(npc.id=="tovan")return"He jokes while counting bowls, boots, and occupied beds. Nothing about his welcome is careless, least of all the warmth.";
    if(npc.id=="sable")return"Red ink stains one cuff. She listens without interrupting, then asks for the single detail most stories leave conveniently vague.";
    if(npc.id=="elin")return"Seed husks cling to her coat beside a strung bow. She speaks as ‘we’ even when admitting a private grief.";
    if(npc.id=="dara")return"A plumb line hangs beside a mourning cord. Her sentences are built like culverts: spare, load-bearing, and meant to outlast weather.";
    if(npc.id=="nella")return"River mud dries on her boots in successive bands. Her eyes check every reflective surface before they settle on you.";
    if(npc.id=="roen")return"His coat is carefully mended and carries no crown. The three village seals on his ledger remain blank spaces he refuses to fill himself.";
    return"Their work has left visible habits in posture, clothing, and attention.";
}

static string npcWorkDialogue(const Npc&npc){
    if(npc.id=="mara")return"I keep Emberfall's public flames in one circuit: sixteen street lamps, the guild beacons, and the Delve gate. When one lamp changes, I test them all. Three turned blue together. That is a signal, not bad oil.";
    if(npc.id=="orra")return"I repair what comes back from the Delve. Lately every broken blade carries the same black scale, stamped with Valeward's old crown. Someone forged the trouble before we were born.";
    if(npc.id=="neris")return"I sound the well at dawn, noon, and moonrise. Since the blue lamps began, its level falls exactly when the Delve exhales. Water is tracing a connection the town forgot.";
    if(npc.id=="tovan")return"I keep people fed and remember who has not come home. Sable calls that an unofficial census. I call it knowing when to leave a bowl warm and when to send Mara looking.";
    if(npc.id=="sable")return"I compare the first Delve survey with the public copy. The first records a sixth depth and a Lantern Compact. The copy omits both. Somebody edited our history to conceal what Emberfall was built to guard.";
    if(npc.id=="elin")return"We tend hedge, seed, bells, and one another. Hollowroot once fed our gardens. Now its roots drink heat from below and the goblins follow them like a road.";
    if(npc.id=="dara")return"I maintain the road and the oath-stones beneath it. Both carry weight. Someone has lifted stones from the old compact and mortared them into the Sunken Barrow. The road is settling toward the theft.";
    if(npc.id=="nella")return"I measure the river against flood marks cut by my mother and hers. Moonwell is drawing water backward beneath the bank. Reflections are only the part of the theft we can see.";
    if(npc.id=="roen")return"I hold Valeward with eleven guards and three villages that no longer owe this keep anything. My work is to earn their trust before I ask them to face the failure buried under my command.";
    return"I do the work that will become an emergency if I leave it until tomorrow.";
}

static string npcRumourDialogue(const Npc&npc){
    if(npc.id=="mara")return"The blue lamps flare in a sequence: west road, well, guild, Delve. The old circuit is pointing underground. I need someone to follow it to the Lampwright Muster and find who rang the signal.";
    if(npc.id=="orra")return"The black scale in those blades is oath-iron. Valeward mixed it only when sealing a sworn bargain. Heat it, and you hear three voices answer a fourth that never speaks.";
    if(npc.id=="neris")return"Two knocks rise through the well after each blue flare. Old plans mark two buried channels: one to Moonwell, one to the Delve. Both meet beneath Valeward.";
    if(npc.id=="tovan")return"My impossible guest knew the names of three village founders and asked whether Emberfall still shared its light. He was relieved when I said yes. Then he cast no reflection in his washbasin.";
    if(npc.id=="sable")return"The omitted clause is legible under the scraping: ‘No crown may own the common light.’ The last regent signed beneath it, then ordered the sixth depth erased.";
    if(npc.id=="elin")return"The hedge bells are old mine alarms. Their wires run through Hollowroot and into Valeward stone. Something below is plucking them before it sends the goblins.";
    if(npc.id=="dara")return"The Whispering Stones once read ROOT, ROAD, RIVER—THREE WITNESSES, ONE LIGHT. Someone recut WITNESSES as DEBTORS. That is not weather. It is policy.";
    if(npc.id=="nella")return"Moonwell shows Valeward's undercroft at midnight. A crowned figure stands there pulling three bright threads through the water. One thread has already reached Emberfall.";
    if(npc.id=="roen")return"The figure below calls itself the rightful Regent. The record calls it Oathless. My predecessor sealed the undercroft and died before explaining which title was the warning.";
    return"Roadside fires have been seen moving against the wind. Sensible travellers are arriving before dusk.";
}

static string npcPrompt(const Npc&npc,const string&topic){
    if(topic=="town"){
        if(npc.id=="mara")return"Which streets are on your lighting circuit?";if(npc.id=="orra")return"What is worth knowing around here?";if(npc.id=="neris")return"What does the well tell you about this place?";if(npc.id=="tovan")return"Give me the innkeeper's tour.";if(npc.id=="sable")return"What do the records say about this settlement?";if(npc.id=="elin")return"How is Bramblewick holding together?";if(npc.id=="dara")return"How does Stonecross keep the road open?";if(npc.id=="nella")return"Tell me about Reedwater.";if(npc.id=="roen")return"Tell me about Valeward.";
    }else if(topic=="work"){
        if(npc.id=="mara")return"What changed in the lamp circuit?";if(npc.id=="orra")return"What are the damaged weapons telling you?";if(npc.id=="neris")return"What are you measuring at the well?";if(npc.id=="tovan")return"How do you keep track of everyone?";if(npc.id=="sable")return"What was removed from the archive?";if(npc.id=="elin")return"What is Hollowroot doing to Bramblewick?";if(npc.id=="dara")return"What is happening beneath the road?";if(npc.id=="nella")return"What is Moonwell taking from the river?";if(npc.id=="roen")return"Why does Valeward need the villages?";
    }else if(topic=="rumour"){
        if(npc.id=="mara")return"Where does the blue-light signal lead?";if(npc.id=="orra")return"What is oath-iron?";if(npc.id=="neris")return"Where do the buried channels meet?";if(npc.id=="tovan")return"Who was the impossible guest?";if(npc.id=="sable")return"What did the missing clause say?";if(npc.id=="elin")return"Why do the bells ring before an attack?";if(npc.id=="dara")return"What did the Whispering Stones originally say?";if(npc.id=="nella")return"What have you seen in Moonwell?";if(npc.id=="roen")return"What is beneath the undercroft?";
    }
    return topic=="town"?"Tell me about this settlement.":topic=="work"?"What work do you do here?":"What are people whispering about?";
}

static string npcTopicDialogue(const Npc&npc,const string&topic,bool emberfall){
    if(npc.id=="mara"){
        if(topic=="town")return"Emberfall began as a lamp station above the Delve. The square distributes oil, the guild keeps the circuit, and every ward contributes a night watch. We do not decorate the dark; we organize against it.";
        if(topic=="places")return"Start at the guild: my circuit map marks every blue flare. Sable has the first survey in Town Hall. Orra kept the black scale from the last expedition. The Kettle has food, beds, and witnesses.";
        if(topic=="delve")return"The first depth contains the old Lampwright Muster. If the blue circuit is a warning, its source will be recorded there. Carry a wall torch; the old builders left brackets because they expected people to return.";
        if(topic=="work_more")return"I trained Iven to trim lamp nine. He vanished on the last survey, and his hand marked the final maintenance sheet: SIX IS OPEN. This is not curiosity for me. I want my apprentice home or honestly mourned.";
        if(topic=="rumour_more")return"A signal has a sender and a recipient. The circuit points from the March toward the Delve, which means something outside is pressing inward and the old ward is asking Emberfall to answer.";
    }else if(npc.id=="orra"){
        if(topic=="town")return"Mara keeps the lamps. Neris keeps the water. Tovan keeps people talking. Sable keeps the proof. I keep their tools from failing. That division of labour is the only magic I trust.";
        if(topic=="places")return"My forge for repairs. The Badger for standard kit. Town Hall for the old compact. Before the Delve, inspect the memorial rack by my quench trough. Those broken blades belonged to people, not loot tables.";
        if(topic=="delve")return"The deeper walls were poured around oath-iron ribs. Break ordinary stone if you must; leave any black metal bearing four joined hands alone. It carries the load—and perhaps the promise.";
        if(topic=="work_more")return"My brother Cal was on Iven's survey. Sable wants evidence; Mara wants hope. I make myself useful because neither will repair a shield. Bring me Cal's blade if you find it. Do not bring me comfort shaped like a guess.";
        if(topic=="rumour_more")return"Three voices in the metal are Bramblewick, Stonecross, and Reedwater. The absent fourth is Valeward. The iron remembers that the keep stopped answering first.";
    }else if(npc.id=="neris"){
        if(topic=="town")return"Every roof drains toward this well, but the drinking channel is sealed from street runoff. The first settlers understood that shared water survives only when everyone protects the source. Their compact used the same design.";
        if(topic=="places")return"Fill your flasks here. Read my depth board beside the well. Then compare it with the flood marks at Reedwater and the cistern gauges below. The same handspan is missing from all three.";
        if(topic=="delve")return"The Drowned Reliquary was a balance tank, not a tomb. It joined Moonwell to Emberfall's well. If its floodgates still work, their maker's marks may show who reversed the flow.";
        if(topic=="work_more")return"One handspan lost. Two knocks. One handspan returned colder than before. I write each event down because fear becomes smaller when it has units.";
        if(topic=="rumour_more")return"Both buried channels meet at Valeward's undercroft. The crowned thing Nella sees is drawing water through one and heat through the other. It is rebuilding a circuit around itself.";
    }else if(npc.id=="tovan"){
        if(topic=="town")return"Emberfall is a lamp station that accidentally grew kitchens, families, and tax disputes. The Compact began here as a supper agreement: every settlement brought something, and nobody left the table in darkness.";
        if(topic=="places")return"Eat here, fill water at Neris's well, buy steel east or south, then speak to Mara and Sable. One tells you what changed tonight; the other tells you why it changed ninety years ago.";
        if(topic=="delve")return"Iven, Cal, and Surveyor Pell took my back table before the sixth stair opened. Iven joked, Cal did not, Pell drew four linked circles on a napkin. I kept it. Innkeepers are archivists with soup.";
        if(topic=="work_more")return"People reveal themselves in what they order when frightened. Mara forgets to eat. Orra orders two bowls and claims one is for later. Sable asks who was sitting where. I make sure there are bowls when they come back.";
        if(topic=="rumour_more")return"The guest used a room key retired before my grandmother was born. Sable matched its teeth to the private stair below Valeward. Whatever spoke to me knew the keep before it became a grave.";
    }else if(npc.id=="sable"){
        if(topic=="town")return"Emberfall's charter calls it ‘the eastern lamp of a light held in common.’ Later clerks reduced that to ‘lamp station.’ Small edits are where large betrayals hide.";
        if(topic=="places")return"Town Hall holds the altered survey. The guild holds the working circuit. Flint Forge holds oath-iron. The Kettle holds Pell's last sketch. Taken separately, they are curiosities. Together, they are a case.";
        if(topic=="delve")return"Depths one through five housed the Compact's machinery: flame, water, prism, witness, crown. The sixth housed the Foundation Seal. The last Regent erased it after trying to make the common light answer only to him.";
        if(topic=="work_more")return"Surveyor Pell was my mentor. She believed a record should make lies expensive. Her final note says: THE REGENT IS NOT DEAD; HE IS AN INSTRUCTION. I intend to determine what executes him.";
        if(topic=="rumour_more")return"The compact was not metaphor. Roots carried heat, roads carried oath-iron, river water cooled the seal, and Emberfall's lamps measured it. Valeward coordinated the work. When its Regent claimed ownership, every part became a wound.";
    }else if(npc.id=="elin"){
        if(topic=="town")return"Bramblewick grows food on thin ash because Hollowroot once warmed the soil. Every household tends one hedge section and every child learns the bell code. We survive by making private chores public promises.";
        if(topic=="places")return"Supplies at the post. Names of the missing on the bell frame. Hollowroot northeast of the hedge. Read the names before you go below; the creatures there used to be neighbours to someone.";
        if(topic=="delve")return"Forewoman Maud Bramble found Valeward pipes beneath the mine. She refused an order to open them, then vanished in a root surge. The thing wearing her survey badge now nests at the bottom.";
        if(topic=="work_more")return"I can order a watch. I cannot order trust. We earned that by sharing harvests through three bad winters. If you help us, do it as a neighbour, not Valeward's collector.";
        if(topic=="rumour_more")return"The bells are connected to the old heat pipes. The Oathless Regent plucks that line before each attack. He is testing whether fear can make us accept his protection again.";
    }else if(npc.id=="dara"){
        if(topic=="town")return"Stonecross keeps the only all-season road between the villages. Every doorstep contributes one day a month to drainage and repair. A road is an agreement you can walk on.";
        if(topic=="places")return"Supplies in the post. Original oath at the Whispering Stones. Missing compact blocks in the Sunken Barrow, southeast of here. The cracks in our square point the same way.";
        if(topic=="delve")return"The barrow was built for roadwardens, not kings. The Drowned Castellan has stacked stolen oath-stones around himself like a throne. Return those stones to silence by removing the occupant.";
        if(topic=="work_more")return"My wife died when last spring's culvert failed. Bad masonry killed her; not fate, not weather. I do not romanticize neglected work. That includes promises.";
        if(topic=="rumour_more")return"The changed inscription makes the villages owe Valeward instead of witness one another. The Regent is trying to rewrite a compact into a tax, then use the false debt as power.";
    }else if(npc.id=="nella"){
        if(topic=="town")return"Reedwater stands on pilings and collective memory. Every flood mark carries a date and a name. When the river rises, nobody asks whose boat is whose until everyone is ashore.";
        if(topic=="places")return"Fill at the rain cistern, trade on the high platform, read the flood board, then follow the upstream bank to Moonwell. If the stars in the water do not match the sky, do not step between them.";
        if(topic=="delve")return"Moonwell was the Compact's cooling lens. Now a wyrm-shaped reflection coils around the intake and sends our water toward Valeward. Kill the reflection and the river should remember its direction.";
        if(topic=="work_more")return"My son Tam sees pictures in any puddle. I used to encourage it. Yesterday his reflection asked him to come closer. I have boarded every low window and slept badly beside his door.";
        if(topic=="rumour_more")return"The crowned figure is pulling root-heat, oath-iron, and river water into the undercroft. He is rebuilding the old seal as a throne. Emberfall's blue lamps mean the fourth line has begun to move.";
    }else if(npc.id=="roen"){
        if(topic=="town")return"Valeward was built to coordinate the Lantern Compact, not rule it. The last Regent forgot the distinction. The villages remember; the empty barracks are their verdict.";
        if(topic=="places")return"The great hall contains three unused village chairs. The gatehouse holds my commission ledger. The undercroft holds the man who made both symbols necessary. I will not open it without renewed consent from all three roads.";
        if(topic=="delve")return"The Oathless Regent turned the Compact's maintenance rite into a coronation. The rite rejected him but preserved his command in the machinery. He has spent ninety years trying to finish the sentence.";
        if(topic=="work_more")return"My family kept this gate after the betrayal. We called obedience honour because shame was harder to inherit. I will not repeat that mistake. The villages choose whether Valeward acts.";
        if(topic=="rumour_more")return"‘Regent’ is not his name now; it is the command he forced into the circuit. Destroy the body in the undercroft and the command loses its speaker. Secure the sixth seal and it loses its work to perform.";
    }
    if(topic=="town")return emberfall?"Emberfall gathers around its roads, lamps, and the people stubborn enough to maintain both.":"This settlement survives because the road still reaches it.";
    if(topic=="places")return"Start with the road, the well, and the trading post. Everything else is either a reason to leave or a reason to return.";
    if(topic=="delve")return"Old places are patient. They can wait longer than your caution.";
    if(topic=="work_more")return"The details change. The responsibility remains.";
    return"I would rather verify it than make the story prettier.";
}

static string npcFarewellDialogue(const Npc&npc){
    if(npc.id=="mara")return"Follow the circuit, not the glow. A warning points somewhere; bait only shines.";if(npc.id=="orra")return"Bring back facts, broken steel, or both. I can work with either.";if(npc.id=="neris")return"Record the level before you disturb the water. Change is evidence.";if(npc.id=="tovan")return"Come back for the bowl I am keeping warm. That is an instruction, not optimism.";if(npc.id=="sable")return"Names, dates, maker's marks. Bring me what happened, not what would make the better ballad.";if(npc.id=="elin")return"If the bells ring, come toward the hedge. Bramblewick answers its neighbours.";if(npc.id=="dara")return"Test each stone before trusting your weight to it. Promises included.";if(npc.id=="nella")return"Trust the current you can feel, not the sky you can see in it.";if(npc.id=="roen")return"Act by consent, report plainly, and leave no order behind for a ghost to obey.";return"Safe roads.";
}

static string npcQuestDialogue(const Npc&npc,const Quest&quest,const string&phase){
    if(npc.id=="mara"){
        if(phase=="offer")return"Three lamps turned blue in sequence and Iven's last note says SIX IS OPEN. Follow the circuit into the Ashen Vaults. Find the old Lampwright Muster and bring back the message it was built to send.";
        if(phase=="progress")return"The lamps are still repeating the route. West road, well, guild, Delve. The Muster is on the first depth; do not let the deeper dark distract you from the first answer.";
        return"The Muster named the sixth seal. Good. I can stop treating sixteen symptoms and start treating one wound. Take the thirty-five gold from the guild fund. Show this account to Sable at Town Hall; she can read the survey marks.";
    }if(npc.id=="sable"){
        if(phase=="offer")return quest.id=="sixth_record"?"The Muster confirms the erased depth housed the Foundation Seal. Descend, identify what is executing the Regent's command, and stop it. I need an ending the archive cannot misfile.":"The Regent and the Sixth Warden are two halves of the same surviving order. Secure the seal and end the Oathless Regent; then the Compact can belong to its people again.";
        if(phase=="progress")return quest.id=="sixth_record"?"Five depths explain the machine. The sixth explains the crime. Continue until the record and the architecture agree.":"One remnant still holds: the speaker beneath Valeward or the command at the Foundation Seal. Remove both, in either order.";
        return quest.id=="sixth_record"?"You found the erased depth and silenced its Warden. I am restoring page six to the public survey—with your account attached and no heroic adjectives.":"Then it is finished. Not because a crown changed hands, but because the light has none. I will enter every village as co-author of the restored Compact.";
    }
    if(npc.id=="elin"){
        if(phase=="offer")return quest.id=="bramble_scouts"?"The Regent is using goblins to count our gates and test our alarm line. Stop three scouts before their count becomes an attack. Then we can reach Hollowroot without leaving the hedge bare.":"The scouts carried Valeward wire from Hollowroot. Go below, sever the old heat line, and put Maud Bramble's stolen body—or whatever wears it—to rest.";
        if(phase=="progress")return quest.id=="bramble_scouts"?"They are still counting. Break the count at three and the rest will scatter.":"The roots still twitch when the bells sound. Follow them down; the Matriarch sits where the old heat pipe enters the mine.";
        return quest.id=="bramble_scouts"?"The remaining scouts fled before finishing their map. We can spare people for Hollowroot now.":"The soil cooled the moment the Matriarch fell. Maud is gone, but the village can say where she died. That matters.";
    }if(npc.id=="dara"){
        if(phase=="offer")return quest.id=="stones"?"Read the Whispering Stones before we touch the barrow. I need the original wording proved, not remembered, before I accuse Valeward of rewriting our oath.":"The Stones confirm the theft. Enter the Sunken Barrow, destroy the Drowned Castellan, and stop him using our road blocks as a throne.";
        if(phase=="progress")return quest.id=="stones"?"The marked face is southeast of the main road. Copy every word; one altered verb caused ninety years of obedience.":"The road is still dropping. Find the chamber built from cleaner stone than its surroundings. That is ours.";
        return quest.id=="stones"?"ROOT, ROAD, RIVER—witnesses, not debtors. Good. Now we can act on proof.":"The square settled by half an inch when he fell. I measured twice. Stonecross considers the account closed.";
    }if(npc.id=="nella"){
        if(phase=="offer")return quest.id=="river"?"Follow the backward current to Moonwell and mark its entrance. Do not enter yet. First learn where the false sky touches our water.":"Now we know the intake. Enter Moonwell, kill the Wyrm in the reflection chamber, and reverse the flow before the Regent draws the river dry.";
        if(phase=="progress")return quest.id=="river"?"Watch floating leaves, not reflected stars. Leaves obey the real current.":"The flood board keeps falling. The Wyrm coils around the deepest light; that is where the false current begins.";
        return quest.id=="river"?"You found the contact point. I can keep children away while you deal with what lives beyond it.":"The river turned before you reached the bank. Tam's reflection copies him again. Reedwater will stand with you.";
    }if(npc.id=="roen"){
        if(phase=="offer")return quest.id=="compact"?"Help Bramblewick, Stonecross, and Reedwater on their own terms. Bring me their freely given support. I will not open the undercroft by repeating the coercion that created it.":"The three roads consent. Enter the undercroft and end the Oathless Regent—the speaker still issuing commands to the Compact.";
        if(phase=="progress")return quest.id=="compact"?"The ledger records support, not submission. Speak to each warden, finish the work they name, and return their answer unchanged.":"The undercroft is open. Destroy the Regent's body, but read the compact table first. We need to understand the failure we are ending.";
        return quest.id=="compact"?"All three seals are entered, and every village signed in its own hand. You have a commission. The undercroft will open.":"The command has lost its speaker. Valeward's first useful act in ninety years will be publishing the confession beneath us.";
    }
    if(phase=="offer")return quest.description+" Objective: "+quest.objective+".";if(phase=="progress")return"The work remains unfinished: "+quest.objective+".";return"You have completed the work. Give me the plain account; embellishment can wait for the archive.";
}

static string questCompletionNote(const Quest&q){
    if(q.flag=="circuit_traced")return"The guild circuit now identifies the erased sixth depth.";
    if(q.flag=="foundation_secured")return"The Foundation Seal is free of the Regent's command.";
    if(q.flag=="story_complete")return"The restored Lantern Compact has been entered as a public agreement.";
    if(q.flag=="bramble_safe")return"Bramblewick freely pledges its support.";
    if(q.flag=="stones_found")return"The original road oath is restored to Stonecross's record.";
    if(q.flag=="stonecross_safe")return"Stonecross freely pledges its support.";
    if(q.flag=="river_traced")return"Reedwater has marked the true Moonwell intake.";
    if(q.flag=="reedwater_safe")return"Reedwater freely pledges its support.";
    if(q.flag=="valeward_commissioned")return"All three village seals open Valeward's undercroft.";
    if(q.flag=="valeward_safe")return"The Oathless Regent can issue no further command.";
    return"The result has been entered in the public record.";
}

static vector<string> artBlock(const string&block){
    vector<string> lines;istringstream input(block);string line;while(getline(input,line)){if(!line.empty()&&line.back()=='\r')line.pop_back();if(lines.empty()&&line.empty())continue;lines.push_back(line);}while(!lines.empty()&&lines.back().empty())lines.pop_back();return lines;
}

static vector<string> monsterPortrait(const string&name){
    auto has=[&](const string&word){return name.find(word)!=string::npos;};
    if(has("kobold"))return generatedPortrait("monster-kobold");
    if(has("spider"))return generatedPortrait("monster-spider");
    if(has("slime"))return generatedPortrait("monster-slime");
    if(has("myconid"))return generatedPortrait("monster-myconid");
    if(has("mimic"))return generatedPortrait("monster-mimic");
    if(has("skeleton")||has("lich"))return generatedPortrait("monster-undead");
    if(has("cultist")||has("mage")||has("whisper"))return generatedPortrait("monster-cultist");
    if(has("gargoyle")||has("warden"))return generatedPortrait("monster-gargoyle");
    if(has("bat"))return generatedPortrait("monster-bat");
    if(has("goblin")||has("orc")||has("troll"))return generatedPortrait("monster-goblinoid");
    if(has("demon")||has("salamander"))return generatedPortrait("monster-demon");
    return generatedPortrait("monster-beast");
#if 0
    if(has("kobold"))return artBlock(R"ART(
               (
              ( )
               ║
           ╭───╨───╮
        ╭──┤ ◉   ◉ ├──╮
       ╱   ╰───▴───╯   ╲
      ╱  ╭╴  ╲_╱  ╶╮    ╲
      ╲__╰═╦═════╦═╯____╱
          ╱║  │  ║╲
         ╱_╨╲   ╱╨_╲
)ART");
    if(has("spider"))return artBlock(R"ART(
       ╲          │          ╱
        ╲         │         ╱
    ─────╲    ╭───┴───╮    ╱─────
          ╲╭──┤ ◉   ◉ ├──╮╱
    ───────┤   ╰──▴──╯   ├───────
          ╱╰──╮ ╲___╱ ╭──╯╲
    ─────╱    ╰───┬───╯    ╲─────
        ╱         │         ╲
       ╱          │          ╲
)ART");
    if(has("slime"))return artBlock(R"ART(
              __..---..__
          _.-'           '-._
        .'    ◉         ◉    '.
       ╱    ·      ≋      ·    ╲
      │   ·    ╭─────╮    ·     │
      │        ╰─www─╯          │
       ╲  %   %   %   %   %   ╱
        '._               _.-'
           '---..___..---'
)ART");
    if(has("myconid"))return artBlock(R"ART(
             .-════════-.
          .-'  ·  ·  ·   '-.
        .' ·      ◉      ·   '.
       ╰═══════╦════╦════════╯
               ║ ◉◉ ║
             ╭─╨─▴──╨─╮
             ║ ╱╲ ╱╲  ║
             ╰╮  ║   ╭╯
             ╱╨╲ ║  ╱╨╲
)ART");
    if(has("mimic"))return artBlock(R"ART(
         ╔══════════════════╗
         ║  ◉          ◉    ║
         ╠══╦══════════╦════╣
         ║ ╲╱╲╱╲╱╲╱╲╱╲╱    ║
         ║ ╱╲╱╲╱╲╱╲╱╲╱╲    ║
         ╚══╩══════════╩════╝
          ╲  ╲        ╱  ╱
           ╲__╲      ╱__╱
)ART");
    if(has("skeleton")||has("lich"))return artBlock(R"ART(
              .-══════-.
            .'  ◉    ◉  '.
           ╱      ▴       ╲
          │   ╲  ▂▂▂  ╱    │
          │    ║╫╫╫╫║      │
           ╲___╨╨╨╨╨╨_____╱
             ╱╲  ║  ╱╲
          ══╪══╲═╬═╱══╪══
             ╲  ║  ╱
             ╱_╱ ╲_╲
)ART");
    if(has("cultist")||has("mage")||has("whisper"))return artBlock(R"ART(
                 ▲
               ╱   ╲
             ╱  ╭─╮  ╲
            │   │◉│   │
            │   ╰▴╯   │
           ╱╲  ╱██╲  ╱╲
          ╱  ╲╱████╲╱  ╲
         ╱  ╱████████╲  ╲
        ╱__╱██╱────╲██╲__╲
            ╱      ╲
)ART");
    if(has("gargoyle")||has("warden"))return artBlock(R"ART(
        ╲╲      ╱╲      ╱╱
         ╲╲  ╭──┴──╮  ╱╱
          ╲╲─┤ ◉ ◉ ├─╱╱
          ╭──╯  ▴  ╰──╮
       ╭──┤  ╲═════╱  ├──╮
       │  ╰╦══╦═╦══╦══╯  │
       ╰╮  ║  ║ ║  ║   ╭╯
        ╰══╩══╩═╩══╩═══╯
           ╱╨╲   ╱╨╲
)ART");
    if(has("bat"))return artBlock(R"ART(
       ╲╲                   ╱╱
        ╲╲__             __╱╱
        ╱   '──╮     ╭──'   ╲
       ╱  ╭────╲╲___╱╱────╮  ╲
      ╱__╱      │◉ ◉│      ╲__╲
                 ╲▴╱
                ╱││╲
                 ╨╨
)ART");
    if(has("goblin")||has("orc")||has("troll"))return artBlock(R"ART(
          ╭╲___________╱╮
       ╭──╯  ◉       ◉  ╰──╮
      <          ▴          >
       ╰╮    ╲═════╱     ╭╯
        ╰╦═════════════╦╯
       ╱ ║  ╱╲     ╱╲ ║ ╲
      ╱  ║ ╱  ╲___╱  ╲║  ╲
      ╲__╩╯    ║ ║    ╰╩__╱
              ╱╨ ╨╲
)ART");
    if(has("demon")||has("salamander"))return artBlock(R"ART(
          ▲╲             ╱▲
           ╲╲  ╭─────╮  ╱╱
            ╲╲─┤◉   ◉├─╱╱
             ╲╮╰──▴──╯╭╱
           ╭──╯ ╲███╱ ╰──╮
        *──┤   ╲███████╱  ├──*
           ╰╦══╦══╦══╦══╦╯
            ║ ╱╨╲ ║ ╱╨╲ ║
          * ╨     ╨     ╨ *
)ART");
    return artBlock(R"ART(
             ╭╲_______╱╮
          ╭──╯  ◉   ◉  ╰──╮
         ╱        ▴        ╲
        │     ╲_______╱     │
        ╰╮   ╱╲  ║  ╱╲    ╭╯
         ╰══╦══╲═╬═╱══╦═══╯
            ║   ║ ║   ║
           ╱╨╲ ╱   ╲ ╱╨╲
)ART");
#endif
}

static string monsterFlavour(const string&name){
    auto has=[&](const string&word){return name.find(word)!=string::npos;};
    if(has("Root-Crowned Matriarch"))return"Maud Bramble's survey badge is embedded above its heart. Valeward wire works the roots like puppet strings whenever the hedge bells sound.";
    if(has("Drowned Castellan"))return"Stonecross household marks cover its armour, all turned inward. It carries a community's road as if collective labour were royal tribute.";
    if(has("Moonwell Wyrm"))return"It exists only in reflected water. A crown-shaped siphon gives the image teeth and anchors it to the real river.";
    if(has("Oathless Regent"))return"Cables have replaced veins. Every breath repeats the same unfinished coronation while four stolen currents feed the throne.";
    if(has("Sixth Warden"))return"Four hands are carved across its chest beneath a bolted crown. With every step, the older carving strains against the newer command.";
    if(has("kobold"))return"Its stolen candle is both crown and courage; every nervous breath makes the flame bow.";
    if(has("spider"))return"Each leg reads a separate vibration. The web has already told it your weight and heartbeat.";
    if(has("slime"))return"Light enters its body in wavering bands, illuminating things that have not finished dissolving.";
    if(has("myconid"))return"Spores orbit its cap like dust around a private moon, blooming where the lantern warms them.";
    if(has("mimic"))return"The hinges flex like jaw muscles. Its treasure is bait arranged with a predator's patience.";
    if(has("skeleton")||has("lich"))return"Old magic keeps the bones in argument with gravity; cold light leaks through every joint.";
    if(has("cultist")||has("mage"))return"The robe is scorched from the inside. A rehearsed incantation moves silently behind its teeth.";
    if(has("whisper"))return"Its outline is not dark; it is an argument against light, shaped like something almost remembered.";
    if(has("gargoyle")||has("warden"))return"Chisel marks become tendons when it moves. Reflected light crawls across a face built never to blink.";
    if(has("bat"))return"Its wings redraw the corridor with every beat, navigating by echoes too small for you to hear.";
    if(has("goblin scout"))return"A charcoal map of Bramblewick hangs from its belt beside measured lengths of Valeward bell-wire.";
    if(has("goblin"))return"Scavenged iron bears a fresh Valeward crown stamp. Whoever armed it expected the mark to command obedience.";
    if(has("orc")||has("troll"))return"Its armour is a history of impacts. None of the dents appear to have taught caution.";
    if(has("demon")||has("salamander")||has("cinder"))return"Heat moves beneath its skin like a second, impatient skeleton.";
    return"It watches the lantern rather than your weapon, measuring what the light reveals and what it conceals.";
}

#include "ShopCatalog.hpp"

class Game {
public:
    uint32_t seed{};
    mt19937 rng;
    vector<MapData> maps;
    int current{};
    Point player{};
    int hp{34},maxHp{34},attack{7},defense{3},gold{32},level{1},xp{},turns{},food{100},water{100};
    int mana{8},maxMana{8},skillPoints{},strength{10},dexterity{10},intellect{10},constitution{10},wisdom{10},spellPower{};
    int wardTurns{},mightTurns{},guardTurns{},focusTurns{};
    double lanternRadius{9.0},lanternIntensity{1.0};
    RaceKind race{RaceKind::Human}; ClassKind calling{ClassKind::Vanguard};
    bool lantern{true},quit{};
    Reaction reaction{Reaction::None};
    map<string,int> materials{{"hide",0},{"venom",0},{"scrap",0},{"shard",0},{"relic",0},{"potion",2},{"ration",2},{"flask",2},{"bomb",1}};
    vector<Quest> quests;
    set<string> flags,discovered,cleared;
    vector<string> lore;
    vector<Item> inventory;
    uint64_t nextItemOrder{1};
    InventorySort preferredInventorySort{InventorySort::Type};
    vector<SpellKind> learnedSpells;
    vector<SkillNode> skills;
    vector<VisualEffect> effects;
    vector<CombatText> combatText;
    string trackedQuest;
    int panelScroll{},dialoguePage{};
    vector<pair<double,SoundCue>> scheduledSounds;
    bool audioActive{}, reducedMotion{};
    optional<Point> cursor;
    string message{"Your light is your own."};
    string messageMarqueeKey,panelMarqueeKey;
    double messageMarqueeStarted{},panelMarqueeStarted{};

    explicit Game(uint32_t worldSeed):seed(worldSeed),rng(mixSeed(seed,1)){generate();initQuests();discoverNearby();lore.insert(lore.begin(),"The Blue Circuit: Three Emberfall lamps changed from gold to blue as the sealed sixth stair opened. Chief Lamplighter Mara Vey has called for a wayfarer to trace the warning into the Delve.");message="ARRIVAL · Three lamps burn blue, three surveyors are missing, and Emberfall's erased sixth depth has opened.";}
    MapData& mapNow(){return maps[current];} const MapData& mapNow()const{return maps[current];}
    int minutes()const{return (420+turns*6)%1440;}
    double daylight()const{ double phase=minutes()/1440.0; return clamp((sin((phase-.25)*6.283185307)+.1)/1.1,0.0,1.0); }
    string clockText()const{ char b[16]; snprintf(b,sizeof b,"%02d:%02d",minutes()/60,minutes()%60); return b; }
    string period()const{int m=minutes();return m<300?"deep night":m<420?"dawn":m<660?"morning":m<1020?"day":m<1140?"dusk":"night";}
    static double visualSeconds(){using C=chrono::steady_clock;return chrono::duration<double>(C::now().time_since_epoch()).count();}
    int totalAttack()const; int totalDefense()const; int totalSpellPower()const; double totalLightRadius()const;

    void generate();
    MapData buildTown(uint32_t);
    MapData buildWild(uint32_t);
    MapData buildDungeon(uint32_t,int,const string&,bool);
    void connectWorld();
    void initQuests();
    void run();
    void draw();
    string renderWorldFrame(double=-1);
    void renderQaShowcase(bool);
    void renderQaTown();
    string renderSplashFrame(double)const;
    void splashScreen();
    void renderQaSplash();
    void renderQaCreation();
    void renderQaAncestry();
    void renderQaHelp();
    void renderQaInspect(bool);
    void renderQaDialogue();
    void renderQaSkillTree(double);
    void handle(int);
    void movePlayer(Point);
    void travel(const Travel&);
    void passTurn();
    void advanceSurvival();
    void advanceMonsters();
    void interact();
    void shopMenu(const string&);
    bool buyFromShop(const ShopDefinition&,size_t,int);
    bool sellToShop(const ShopDefinition&,size_t,int);
    vector<string> shopLines(const ShopDefinition&,bool,int,int,double,const string&)const;
    void renderQaShop(int);
    void rest();
    void inventoryMenu();
    void characterMenu();
    void spellbookMenu();
    void skillTreeMenu();
    void inspectMenu();
    void searchNearby();
    optional<Point> targetCursor(const string&,int);
    void castSpell(SpellKind,Point);
    void addItem(Item,bool=false);
    void coalesceInventory();
    int fillEmptyFlasks(int);
    void useItem(size_t);
    void gatherProvision(ProvisionSource&);
    void takeWallTorch(WallTorchFeature&);
    void mountWallTorch(size_t);
    bool damageTerrain(MapData&,Point,int);
    void triggerTrap(Point);
    void operateMachine(MachineFeature&);
    void advanceEnvironment();
    void configureCharacter(RaceKind,ClassKind);
    void createCharacterInteractive();
    int spellInvestment(SpellKind)const;
    int spellCost(SpellKind)const;
    void talk(Npc&);
    void attackMonster(Monster&);
    void killMonster(Monster&);
    void discoverNearby();
    void updateSchedules();
    void fastTravelMenu();
    void craftingMenu();
    void loreMenu();
    void questMenu();
    vector<string> questLines(int,double)const;
    string questNextStep(const Quest&)const;
    string campaignBrief()const;
    string npcGreeting(const Npc&)const;
    string objectiveTracker()const;
    bool questAvailable(const Quest&)const;
    string giverLabel(const string&)const;
    bool acceptQuest(Quest&);
    bool turnInQuest(Quest&);
    void gainXp(int);
    void emitImpact(Point,EffectStyle,int=2,double=0);
    void playCue(SoundCue cue){if(audioActive)soundBank().emit(cue);}
    void scheduleCue(SoundCue cue,double delay){if(audioActive)scheduledSounds.push_back({visualSeconds()+delay,cue});}
    void renderQaJournal();
    void renderQaDossier(int);
    vector<string> characterLines(int,double)const;
    vector<string> spellbookLines(int,double)const;
    vector<string> loreLines(int,double)const;
    void renderQaEffects();
    void mapMenu();
    void helpMenu();
    bool save(const filesystem::path&);
    static optional<Game> load(const filesystem::path&);
    bool lineOfSight(Point,Point)const;
    bool clearLight(Point,Point)const;
    double sunVisibility(Point)const;
    double lightAt(Point)const;
    LinearLight illuminationAt(Point,double)const;
    pair<double,double> sampleLightVisibility(Point,Point,double)const;
    bool traceSubCellRay(double,double,double,double)const;
    bool reachable(const MapData&,Point,Point)const;
    void guarantee(MapData&,Point,const vector<Point>&,Tile);
    static uint64_t fingerprint(const MapData&);
    static bool selfTest();
private:
    void panel(const string&,const vector<string>&,Color={245,184,80},bool=false);
    int menu(const string&,const vector<string>&,Color={245,184,80});
    void renderPanel(const string&,const vector<string>&,Color,int=-1,bool=false,int=0);
    int creationChoice(const string&,const vector<string>&,const vector<vector<string>>&,const vector<vector<string>>&,Color);
    vector<string> skillTreeLines(int,bool,double)const;
    string inspectSummary(Point)const;
    vector<string> inspectDetails(Point)const;
    vector<string> conversationLines(const Npc&,const string&,const string&,const vector<DialogueChoice>&,int)const;
    vector<size_t> inventoryOrder(InventorySort)const;
    vector<string> inventoryLines(int,bool,double,InventorySort=InventorySort::Type)const;
    void carve(MapData&,Point,Point,Tile,int=0);
    Point openNear(MapData&,Point);
    void addVillage(MapData&,Point,const string&,const string&,const string&,Color);
    vector<string> markLocalMap(const Npc&);
    int objectiveProgress(const Quest&)const;
    void refreshQuestProgress();
    void recordTestimony(const Npc&,bool);
    void applyQuestConsequences(const Quest&);
    void completeObjective(const string&,const string& = "");
    Quest* questFor(const string&);
};

static int readKey(){
    int c=_getch();
    if(c==0||c==224){int e=_getch();if(e==72)return 1001;if(e==80)return 1002;if(e==75)return 1003;if(e==77)return 1004;if(e==71)return 1005;if(e==73)return 1006;if(e==79)return 1007;if(e==81)return 1008;if(e>=59&&e<=68)return 2000+(e-58);return 0;}
    if(c>='A'&&c<='Z')c=c-'A'+'a';
    return c;
}
static optional<Point> keyDirection(int key,bool diagonals=true){if(key==1001||key=='w'||key=='k'||key=='8')return Point{0,-1};if(key==1002||key=='s'||key=='j'||key=='2')return Point{0,1};if(key==1003||key=='a'||key=='h'||key=='4')return Point{-1,0};if(key==1004||key=='d'||key=='l'||key=='6')return Point{1,0};if(!diagonals)return nullopt;if(key==1005||key=='y'||key=='7')return Point{-1,-1};if(key==1006||key=='u'||key=='9')return Point{1,-1};if(key==1007||key=='b'||key=='1')return Point{-1,1};if(key==1008||key=='n'||key=='3')return Point{1,1};return nullopt;}
static void ansiColor(ostringstream& out,Color c){out<<"\x1b[38;2;"<<c.r<<';'<<c.g<<';'<<c.b<<'m';}
static void ansiBg(ostringstream& out,Color c){out<<"\x1b[48;2;"<<c.r<<';'<<c.g<<';'<<c.b<<'m';}
static string statBar(int value,int maximum,int width){int fill=maximum?clamp(value*width/maximum,0,width):0;return "["+string(fill,'#')+string(width-fill,'-')+"]";}
static int utf8Width(const string&);
static string fitPad(string,int,char=' ');
static size_t utf8Offset(const string&,int);
static string marqueeText(const string&,int,double,char=' ');
static vector<string> wrapText(string,int);

void Game::carve(MapData& m,Point a,Point b,Tile tile,int half){
    auto paint=[&](Point p){for(int oy=-half;oy<=half;oy++)for(int ox=-half;ox<=half;ox++){Point q{p.x+ox,p.y+oy};if(!m.contains(q))continue;Tile t=tile;if(m.at(q).name=="deep water")t=Bridge;m.at(q)=t;}};
    bool horizontal=(rng()%2)==0;
    if(horizontal){int sx=a.x<=b.x?1:-1;for(int x=a.x;;x+=sx){paint({x,a.y});if(x==b.x)break;}int sy=a.y<=b.y?1:-1;for(int y=a.y;;y+=sy){paint({b.x,y});if(y==b.y)break;}}
    else{int sy=a.y<=b.y?1:-1;for(int y=a.y;;y+=sy){paint({a.x,y});if(y==b.y)break;}int sx=a.x<=b.x?1:-1;for(int x=a.x;;x+=sx){paint({x,b.y});if(x==b.x)break;}}
}

bool Game::reachable(const MapData& m,Point start,Point goal)const{
    auto passable=[&](Point p){return m.at(p).walk||m.at(p).name=="closed door";};if(!m.contains(start)||!m.contains(goal)||!passable(start)||!passable(goal))return false;
    vector<uint8_t> seen(size_t(m.w)*m.h);queue<Point> q;q.push(start);seen[size_t(start.y)*m.w+start.x]=1;
    while(!q.empty()){auto p=q.front();q.pop();if(p==goal)return true;for(auto d:DIRS){auto n=p+d;if(!m.contains(n)||!passable(n))continue;auto i=size_t(n.y)*m.w+n.x;if(!seen[i]){seen[i]=1;q.push(n);}}}return false;
}

void Game::guarantee(MapData& m,Point start,const vector<Point>& points,Tile path){
    m.critical=points;for(auto p:points)if(!reachable(m,start,p))carve(m,start,p,path,m.outdoor?1:0);
    for(auto p:points)if(!reachable(m,start,p))throw runtime_error("connectivity repair failed in "+m.name);
}

Point Game::openNear(MapData& m,Point p){
    for(int r=0;r<12;r++)for(int y=-r;y<=r;y++)for(int x=-r;x<=r;x++){Point q{p.x+x,p.y+y};if(m.contains(q)&&m.at(q).walk)return q;}return m.start;
}

void Game::addVillage(MapData& m,Point c,const string& village,const string& npcId,const string& npcName,Color color){
    for(int y=c.y-5;y<=c.y+5;y++)for(int x=c.x-8;x<=c.x+8;x++){Point p{x,y};if(m.contains(p))m.at(p)=Grass;}
    for(int x=c.x-5;x<=c.x+5;x++){m.at({x,c.y-4})=Wall;m.at({x,c.y+4})=Wall;}
    for(int y=c.y-4;y<=c.y+4;y++){m.at({c.x-5,y})=Wall;m.at({c.x+5,y})=Wall;}
    m.at({c.x,c.y+4})=Road;m.at(c)=Tile{'$',color,true,false,false,"trading post"};
    string greeting=npcId=="elin"?"You answered the bells. Good. Bramblewick has no spare people, only people who make room.":npcId=="dara"?"Mind the crack beside your boot. It points to the Barrow, like every other failure in my road.":npcId=="nella"?"Look at the river, then at its reflection. If they disagree, trust the one that can drown you.":"The March remembers every road that people keep open.";
    string role=npcId=="elin"?"hedgewarden":npcId=="dara"?"roadwarden":npcId=="nella"?"riverkeeper":"village elder";Npc n{npcId,npcName,role,greeting};n.work={c.x-2,c.y+2};n.home={c.x+3,c.y-1};n.evening={c.x,c.y+1};n.p=n.work;n.color=color;m.npcs.push_back(n);
    string villageLore=npcId=="elin"?"Every gate bears one household's bell-rope. The ropes join beneath the hedge before running toward Hollowroot.":npcId=="dara"?"Every doorstep is cut from the same road-stone; hairline fractures lean southeast toward the Sunken Barrow.":"Flood marks name the people who measured them. The newest line is dry while the river below it runs backward.";
    string featureName=npcId=="elin"?"Hedge Bell Memorial":npcId=="dara"?"Roadwarden's Measure":"Reedwater Flood Board";
    string featureLore=npcId=="elin"?"Nine bells hang above nine scratched names. Forewoman Maud Bramble's bell has a fresh Valeward wire tied to its clapper.":npcId=="dara"?"A brass straightedge spans a widening crack. Chalk dates show Dara has measured it every dawn for thirty-seven days.":"Generations of flood heights climb the board. This week's marks descend, one handspan at a time, against the weather.";
    Point feature{c.x-3,c.y-2};m.at(feature)=Tile{'!',color,true,false,false,"fixture: "+featureName};Tile villageSign=TownSign;villageSign.name="plaque: "+village+" — THREE ROADS, ONE LIGHT";m.at({c.x+2,c.y+4})=villageSign;m.landmarks.push_back({c,village,villageLore});m.landmarks.push_back({feature,featureName,featureLore});
    m.lights.push_back({c,{255,160,70},8,1.0});
}

MapData Game::buildTown(uint32_t s){
    mt19937 r(s);MapData m;m.w=100;m.h=48;m.name="Emberfall";m.outdoor=m.town=true;m.tiles.assign(size_t(m.w)*m.h,Grass);m.explored.assign(m.tiles.size(),0);m.elements.resize(m.tiles.size());
    for(int x=0;x<m.w;x++){m.at({x,0})=Tree;m.at({x,m.h-1})=Tree;}for(int y=0;y<m.h;y++){m.at({0,y})=Tree;m.at({m.w-1,y})=Tree;}
    int roadY=23+int(r()%3),roadX=49+int(r()%3);m.start={roadX,roadY};
    carve(m,{2,roadY},{m.w-3,roadY},Road,1);carve(m,{roadX,3},{roadX,m.h-4},Road,1);carve(m,{3,4},{m.w-4,4},Road,0);carve(m,{3,m.h-5},{m.w-4,m.h-5},Road,0);carve(m,{22,4},{22,m.h-5},Road,0);carve(m,{77,4},{77,m.h-5},Road,0);
    for(int y=roadY-5;y<=roadY+5;y++)for(int x=roadX-8;x<=roadX+8;x++)if(m.contains({x,y}))m.at({x,y})=TownCobble;
    for(int i=0;i<320;i++){Point p{2+int(r()%(m.w-4)),2+int(r()%(m.h-4))};if(m.at(p).name=="grass"&&p.dist2(m.start)>100)m.at(p)=(r()%6?Tile{',',{90,140,70},true,false,false,"garden verge"}:Tree);}
    array<pair<string,Color>,3> shops{{{"Copper Kettle",{245,150,60}},{"Cinder and Sigil",{185,95,235}},{"Brass Badger",{235,105,45}}}};
    struct Building{Point door,inside,feature,sign;};
    auto placeBuilding=[&](int x,int y,int w,int h,const string&name,const string&plaque,bool doorSouth,bool shop,Color accent){Tile floor=TownFloor,wall=TownWall,window=TownWindow;floor.name=name+" interior";wall.name=name+" wall";window.name=name+" window";window.color=accent;for(int py=y;py<y+h;py++)for(int px=x;px<x+w;px++){bool edge=px==x||px==x+w-1||py==y||py==y+h-1;m.at({px,py})=edge?wall:floor;}Point door{x+w/2,doorSouth?y+h-1:y};m.at(door)=Door;Point outside=door+Point{0,doorSouth?1:-1};Point roadEdge{outside.x,doorSouth?roadY-2:roadY+2};carve(m,outside,roadEdge,Road,0);Point sign=outside+Point{1,0};Tile signTile=TownSign;signTile.name="plaque: "+name+" — "+plaque;m.at(sign)=signTile;Point leftWindow{clamp(door.x-3,x+1,x+w-2),door.y},rightWindow{clamp(door.x+3,x+1,x+w-2),door.y};if(leftWindow!=door){m.at(leftWindow)=window;m.lights.push_back({leftWindow,accent,5.5,.72,.08,double(x+y)});}if(rightWindow!=door&&rightWindow!=leftWindow){m.at(rightWindow)=window;m.lights.push_back({rightWindow,accent,5.5,.72,.08,double(x+y+7)});}Point inside{x+w/2,doorSouth?y+h-2:y+1},feature{x+w/2,y+h/2};if(shop){m.at(feature)=Tile{'$',accent,true,false,false,name};m.lights.push_back({feature,accent,7.5,1.0,.12,double(x)});}return Building{door,inside,feature,sign};};
    auto kettle=placeBuilding(6,7,14,10,shops[0].first,"A BOWL KEPT WARM FOR EVERY RETURN",true,true,shops[0].second);auto westHomes=placeBuilding(26,7,13,10,"Hearthside Row","ONE LAMP PER WINDOW · ONE WATCH PER HOUSE",true,false,{238,170,92});auto townHall=placeBuilding(59,7,15,10,"Emberfall Town Hall","PUBLIC RECORDS · THE COMPACT BELONGS TO ALL",true,false,{235,188,105});auto sigil=placeBuilding(80,7,14,10,shops[1].first,"TONICS · RUNES · NO CROWNED CONTRACTS",true,true,shops[1].second);
    auto forge=placeBuilding(6,32,15,10,"Flint Forge","OATH-IRON REPAIRED · OWNERSHIP REFUSED",false,false,{245,110,45});auto southHomes=placeBuilding(27,32,13,10,"Cooper Lane","BARRELS FOR WELL, RIVER, AND ROAD",false,false,{224,154,86});auto guild=placeBuilding(59,32,15,10,"Lamplighters Guild","NO FLAME BURNS ALONE · REPORT BLUE FIRE",false,false,{255,192,72});auto badger=placeBuilding(80,32,14,10,shops[2].first,"ARMS · ARMOUR · DELVER'S GOODS",false,true,shops[2].second);
    auto fixture=[&](Point p,char glyph,Color color,const string&name,bool walk=false){m.at(p)=Tile{glyph,color,walk,false,false,"fixture: "+name,0,.18};};
    fixture({8,8},'*',{255,108,35},"Copper Kettle cooking hearth");fixture({10,12},'=',{186,126,72},"Copper Kettle oak table");fixture({16,12},'=',{186,126,72},"Copper Kettle dice table");fixture({18,9},'0',{132,92,55},"Copper Kettle ale barrel");m.lights.push_back({{8,8},{255,116,38},6.5,1.0,.16,3.1});
    fixture({28,9},'H',{192,145,92},"Hearthside patchwork bed");fixture({36,9},'H',{192,145,92},"Hearthside child's bed");fixture({32,12},'=',{180,125,75},"Hearthside family table");fixture({28,14},'C',{155,104,62},"Hearthside cedar chest");
    fixture({61,9},'B',{150,112,80},"Town Hall compact shelf with one scraped spine");fixture({71,9},'B',{150,112,80},"Town Hall expedition archive missing Survey Six");fixture({66,10},'=',{190,145,85},"Town Hall four-seat witness table");fixture({61,14},'&',{215,184,112},"Town Hall brass date press set ninety years back");
    fixture({82,9},'B',{145,75,165},"Cinder and Sigil reagent shelf");fixture({91,9},'B',{145,75,165},"Cinder and Sigil sealed grimoires");fixture({87,10},'o',{190,105,240},"Cinder and Sigil scrying bowl");fixture({91,14},'&',{205,105,230},"Cinder and Sigil rune press");m.lights.push_back({{87,10},{180,82,255},6,.78,.05,6.2});
    fixture({8,39},'*',{255,92,28},"Flint Forge coal hearth burning above a blue ember");fixture({10,37},'A',{190,195,205},"Flint Forge anvil scarred by oath-iron");fixture({17,38},'~',{72,145,185},"Flint Forge quench trough with a black tide-ring");fixture({18,34},'/',{175,155,125},"Flint Forge memorial rack for unreturned blades");m.lights.push_back({{8,39},{255,88,22},7.5,1.18,.2,9.4});
    fixture({29,39},'H',{188,138,86},"Cooper Lane narrow bed");fixture({37,39},'H',{188,138,86},"Cooper Lane loft bed");fixture({33,36},'=',{178,122,74},"Cooper Lane supper table");fixture({38,34},'0',{145,100,60},"Cooper Lane unfinished barrel");
    fixture({61,39},'0',{180,125,48},"Lamplighters Guild oil rack with three untouched cans");fixture({71,39},'0',{180,125,48},"Lamplighters Guild blue-flame burner samples");fixture({63,35},'&',{230,172,65},"Iven's labelled wick cutter");fixture({69,35},'t',{255,190,72},"Lamp circuit model pointing toward the Delve");m.lights.push_back({{69,35},{255,170,55},6.5,.95,.19,12.7});
    fixture({82,39},'/',{205,112,62},"Brass Badger weapon rack");fixture({91,39},']',{205,112,62},"Brass Badger shield rack");fixture({83,34},'H',{178,92,52},"Brass Badger armour stand");fixture({91,34},'C',{170,110,62},"Brass Badger locked stock chest");
    array<string,6> civicLore{{"Tovan keeps a bowl warming beside three empty surveyors' places.","Protective runes around the shop have been amended from a crown to four joined hands.","The armourer has pinned a free repair notice over Valeward's expired levy order.","One archive shelf is cleaner than the rest where the sixth survey was removed.","A brass model links every town lamp; three blue glass beads point toward the Delve.","Broken expedition blades hang by owner, including Cal Flint's empty hook."}};size_t civicIndex=0;for(auto entry:array<pair<Point,string>,6>{{{kettle.feature,shops[0].first},{sigil.feature,shops[1].first},{badger.feature,shops[2].first},{townHall.feature,"Emberfall Town Hall"},{guild.feature,"Lamplighters Guild"},{forge.feature,"Flint Forge"}}})m.landmarks.push_back({entry.first,entry.second,civicLore[civicIndex++]});
    Point pantry{roadX-5,roadY+3},well{roadX+5,roadY+3};m.provisions.push_back({pantry,ProvisionKind::Food,"Emberfall community pantry",4});m.at(pantry)=FoodTile;m.provisions.push_back({well,ProvisionKind::Water,"old town well",6});m.at(well)=WaterTile;m.landmarks.push_back({pantry,"Community Pantry","Shelves are labelled by donor: Bramblewick grain, Stonecross salt, Reedwater fish, Emberfall oil—the Lantern Compact reduced to useful daily practice."});m.landmarks.push_back({well,"Old Town Well","Four capped channels meet beneath the coping. Neris has marked the western channel VALEWARD—BACKFLOW with an arrow pointing down."});
    struct ResidentSeed{string id,name,role,greeting;Point work,home,evening;Color color;};vector<ResidentSeed> residents{{"mara","Mara Vey","chief lamplighter","Three lamps burned blue together. I have ruled out oil, weather, and incompetence. That leaves intent.",guild.feature,westHomes.inside,{roadX-3,roadY+1},{255,190,72}},{"orra","Orra Flint","smith","If you came about the blue fire, speak to Mara. If you came back from it, put your weapon on the bench.",forge.feature,southHomes.inside,{roadX-1,roadY+2},{235,112,55}},{"neris","Neris Vale","wellkeeper","The well lost one handspan when the lamps changed. Tell me where you have been, and I will tell you what moved beneath you.",openNear(m,well+Point{-1,0}),townHall.inside,{roadX+2,roadY+2},{80,188,235}},{"tovan","Tovan Reed","innkeeper","You look like someone about to miss a meal for a mystery. Sit first. Mysteries are patient; stew is not.",kettle.feature,kettle.inside,{roadX-4,roadY-1},{245,160,72}},{"sable","Sable Orr","town archivist","Before you descend: the official survey is missing a floor, and omissions are rarely load-bearing by accident.",townHall.feature,sigil.inside,{roadX+4,roadY-1},{176,132,235}}};for(auto&seed:residents){Npc n{seed.id,seed.name,seed.role,seed.greeting};n.work=seed.work;n.home=seed.home;n.evening=seed.evening;n.p=n.work;n.color=seed.color;m.npcs.push_back(n);}
    array<Point,16> lamps{{{roadX-36,roadY},{roadX-27,roadY},{roadX-18,roadY},{roadX-9,roadY},{roadX+9,roadY},{roadX+18,roadY},{roadX+27,roadY},{roadX+36,roadY},{roadX,roadY-16},{roadX,roadY-10},{roadX,roadY+10},{roadX,roadY+16},{22,roadY-5},{22,roadY+5},{77,roadY-5},{77,roadY+5}}};for(size_t i=0;i<lamps.size();i++){Point p{clamp(lamps[i].x,2,m.w-3),clamp(lamps[i].y,2,m.h-3)};if(!m.at(p).walk)p=openNear(m,p);m.at(p)=StreetLamp;m.lights.push_back({p,{255,154,55},9,1.25,.18,i*1.73,.5,.2,.12});}
    Point gate{2,roadY};m.at(gate)=Tile{'=',{225,180,90},true,false,false,"west gate"};Point stairs=openNear(m,{m.w-12,m.h-9});m.at(stairs)=Tile{'>',{230,210,150},true,false,false,"Emberfall Delve"};m.down=stairs;
    m.landmarks.push_back({m.start,"Civic Lantern Square","Four iron channels meet beneath the central lamp. Three are stamped ROOT, ROAD, and RIVER; the fourth, CROWN, has been chiselled to COMMON."});m.landmarks.push_back({{18,roadY},"Hearthside Ward","Blue curtains hang in three windows awaiting Iven, Cal, and Pell—the surveyors who did not return."});m.landmarks.push_back({{82,roadY},"Cindersmith Ward","Forge soot settles around clean outlines where old Valeward levy plaques were recently removed."});m.landmarks.push_back({gate,"Emberfall West Gate","The road marker names Bramblewick, Stonecross, and Reedwater at equal distance beneath the words THREE ROADS, ONE LIGHT."});m.landmarks.push_back({stairs,"Emberfall Delve","Blue wax beads on the reopened gate. Its lintel bears four joined hands around an unclaimed flame."});
    vector<Point> critical{gate,stairs};for(auto& l:m.landmarks)critical.push_back(l.p);for(auto& n:m.npcs)critical.push_back(n.p);guarantee(m,m.start,critical,Road);return m;
}

MapData Game::buildWild(uint32_t s){
    mt19937 r(s);MapData m;m.w=200;m.h=100;m.name="Ashwind March";m.outdoor=true;m.tiles.assign(size_t(m.w)*m.h,Grass);m.explored.assign(m.tiles.size(),0);m.elements.resize(m.tiles.size());m.start={m.w-4,50+int(r()%9)-4};
    for(int x=0;x<m.w;x++){m.at({x,0})=Tree;m.at({x,m.h-1})=Tree;}for(int y=0;y<m.h;y++){m.at({0,y})=Tree;m.at({m.w-1,y})=Tree;}
    for(int i=0;i<3900;i++){Point p{2+int(r()%(m.w-4)),2+int(r()%(m.h-4))};if(p.dist2(m.start)<150)continue;int v=r()%100;m.at(p)=v<53?Tile{',',{90,140,68},true,false,false,"tall grass"}:v<78?Tree:v<92?Boulder:Grass;}
    int river=95+int(r()%14)-7;for(int y=1;y<m.h-1;y++){int cx=river+int(sin(y*.16)*4);for(int x=cx-1;x<=cx+1;x++)m.at({x,y})=Water;}
    int roadY=m.start.y;carve(m,{2,roadY},m.start,Road,2);
    Point bramble{155+int(r()%20),20+int(r()%15)},stone{105+int(r()%25),70+int(r()%16)},reed{35+int(r()%28),45+int(r()%15)},castle{35+int(r()%22),82+int(r()%8)};
    carve(m,{bramble.x,roadY},bramble,Road,1);carve(m,{stone.x,roadY},stone,Road,1);carve(m,{reed.x,roadY},reed,Road,1);carve(m,{castle.x,roadY},castle,Road,1);
    addVillage(m,bramble,"Bramblewick","elin","Elin Bramble",{220,158,76});addVillage(m,stone,"Stonecross","dara","Dara Stone",{192,126,74});addVillage(m,reed,"Reedwater","nella","Nella Reed",{84,180,174});
    for(int y=castle.y-8;y<=castle.y+7;y++)for(int x=castle.x-12;x<=castle.x+12;x++){Point p{x,y};if(!m.contains(p))continue;bool edge=x==castle.x-12||x==castle.x+12||y==castle.y-8||y==castle.y+7;m.at(p)=edge?Wall:Floor;}m.at({castle.x,castle.y+7})=Road;
    Npc roen{"roen","Captain Roen","acting castellan","The gate stays shut until the three villages choose to open it. Valeward has mistaken obedience for consent before."};roen.work={castle.x-4,castle.y+4};roen.home={castle.x+5,castle.y-3};roen.evening={castle.x,castle.y+2};roen.p=roen.work;m.npcs.push_back(roen);
    m.landmarks.push_back({castle,"Castle Valeward","Three roads enter through separate arches and meet at a round table with three village chairs. The raised regent's seat has been split for firewood."});m.lights.push_back({castle,{255,190,90},11,1.0});
    Point watch{20+int(r()%18),14+int(r()%15)},stones{83+int(r()%20),75+int(r()%14)},camp{145+int(r()%28),72+int(r()%17)};
    for(int y=watch.y-3;y<=watch.y+3;y++)for(int x=watch.x-4;x<=watch.x+4;x++)m.at({x,y})=((x==watch.x-4||x==watch.x+4||y==watch.y-3||y==watch.y+3)?Wall:Floor);m.at({watch.x,watch.y+3})=Floor;
    m.landmarks.push_back({watch,"Broken Watchtower","Its three signal mirrors face the villages; a later iron arm has forced all three reflections toward Valeward."});m.landmarks.push_back({stones,"Whispering Stones","Beneath fresh recutting, the original oath reads: ROOT, ROAD, RIVER—THREE WITNESSES, ONE LIGHT. NO CROWN MAY CLAIM IT."});m.landmarks.push_back({camp,"Pell's Last Camp","Surveyor Pell's cold firepit surrounds a scorched map. Four circles are linked around a sixth mark beneath Emberfall."});m.lights.push_back({camp,{255,130,45},9,1.0});
    vector<Point> sites{{bramble.x+15,bramble.y-10},{stone.x+17,stone.y+15},{reed.x+14,reed.y+24},{castle.x,castle.y-2}};array<string,4> names{{"Hollowroot Mine","Sunken Barrow","Moonwell Cavern","Valeward Undercroft"}};
    array<string,4> siteLore{{"Warm roots knot around a Valeward heat pipe. Nine broken bell-wires disappear into the mine.","Road-stones bearing household marks have been pried from Stonecross and stacked below the flooded entrance.","The water reflects Valeward's undercroft instead of the sky. A bright current runs uphill through the image.","Three unlit village seals surround the door. The regent's crown above them has been hammered into the word OATHLESS."}};for(size_t i=0;i<sites.size();i++){sites[i]=openNear(m,sites[i]);carve(m,i==0?bramble:i==1?stone:i==2?reed:castle,sites[i],Road,0);m.at(sites[i])=Tile{'>',{220,205,150},true,false,false,names[i]};m.landmarks.push_back({sites[i],names[i],siteLore[i]});}
    Point gate{m.w-2,m.start.y};m.at(gate)=Tile{'=',{225,180,90},true,false,false,"Emberfall road"};m.landmarks.push_back({gate,"Emberfall Road","Blue lamp-glass has been nailed to the waymarker as a warning: the old circuit is awake."});
    vector<string> mn{"thorn wolf","briar boar","goblin scout","road brigand","marsh spider","mossback troll"};vector<char> mg{'w','b','g','r','s','T'};
    for(int i=0;i<50;i++){Point p;do{p={3+int(r()%(m.w-6)),3+int(r()%(m.h-6))};}while(!m.at(p).walk||m.at(p).name=="road"||p.dist2(m.start)<300||any_of(m.landmarks.begin(),m.landmarks.end(),[&](auto&l){return p.dist2(l.p)<150;}));int k=r()%mn.size();Monster z;z.p=z.home=p;z.name=mn[k];z.glyph=mg[k];z.color=k==2?Color{100,180,75}:Color{150,125,95};z.hp=z.maxHp=10+k*3;z.attack=4+k/2;z.defense=k/2;z.faction=k==2?Faction::Goblin:Faction::Beast;z.nocturnal=k==0||k==4;z.territory=8+int(r()%8);m.monsters.push_back(z);}
    vector<Point> critical{gate,castle};for(auto& l:m.landmarks)critical.push_back(l.p);for(auto& n:m.npcs)critical.push_back(n.p);guarantee(m,m.start,critical,Road);return m;
}

MapData Game::buildDungeon(uint32_t s,int depth,const string& name,bool site){
    mt19937 r(s);MapData m;m.w=82;m.h=40;m.name=name;m.biome=BiomeKind(clamp(depth,1,5));m.tiles.assign(size_t(m.w)*m.h,Wall);m.explored.assign(m.tiles.size(),0);m.elements.resize(m.tiles.size());
    Tile dWall=Wall,dFloor=Floor;if(m.biome==BiomeKind::AshenVaults){dWall.color={105,100,108};dWall.name="fractured ash-vault wall";dFloor.color={135,112,96};dFloor.name="ash flagstone";}
    else if(m.biome==BiomeKind::DrownedReliquary){dWall.color={65,98,118};dWall.name="waterlogged reliquary wall";dWall.toughness=22;dFloor.color={65,125,150};dFloor.name="flooded reliquary floor";dFloor.reflectivity=.32;}
    else if(m.biome==BiomeKind::PrismaticFoundry){dWall.color={105,122,185};dWall.name="prismatic foundry wall";dWall.toughness=26;dWall.reflectivity=.72;dFloor.color={116,125,190};dFloor.name="crystal-veined floor";dFloor.reflectivity=.58;}
    else if(m.biome==BiomeKind::LightlessCourt){dWall.color={68,63,82};dWall.name="lightless court wall";dWall.toughness=24;dFloor.color={74,70,91};dFloor.name="lightless court stone";dFloor.reflectivity=.06;}
    else{dWall.color={145,66,42};dWall.name="furnace basalt";dWall.toughness=30;dFloor.color={154,83,48};dFloor.name="hot cinderstone";dFloor.reflectivity=.2;}fill(m.tiles.begin(),m.tiles.end(),dWall);
    struct Room{int x,y,w,h;Point c()const{return{x+w/2,y+h/2};}};vector<Room> rooms;
    for(int attempts=0;attempts<220&&rooms.size()<14;attempts++){int rw=6+int(r()%7),rh=5+int(r()%5);Room q{2+int(r()%(m.w-rw-4)),2+int(r()%(m.h-rh-4)),rw,rh};bool overlap=any_of(rooms.begin(),rooms.end(),[&](auto&a){return q.x<a.x+a.w+2&&q.x+q.w+2>a.x&&q.y<a.y+a.h+2&&q.y+q.h+2>a.y;});if(overlap)continue;for(int y=q.y;y<q.y+q.h;y++)for(int x=q.x;x<q.x+q.w;x++)m.at({x,y})=dFloor;rooms.push_back(q);}
    if(rooms.size()<4)throw runtime_error("dungeon room generation failed");for(size_t i=1;i<rooms.size();i++)carve(m,rooms[i-1].c(),rooms[i].c(),dFloor);for(size_t i=2;i<rooms.size();i+=3)carve(m,rooms[i].c(),rooms[i-2].c(),dFloor);
    m.start=m.up=rooms.front().c();m.down=rooms.back().c();m.at(m.up)=Tile{'<',{230,210,150},true,false,false,"stairs up"};m.at(m.down)=Tile{'>',{230,210,150},true,false,false,"stairs down"};
    for(size_t i=1;i<rooms.size();i+=2){auto c=rooms[i].c();array<Point,4> mounts{{{rooms[i].x-1,c.y},{rooms[i].x+rooms[i].w,c.y},{c.x,rooms[i].y-1},{c.x,rooms[i].y+rooms[i].h}}};for(auto p:mounts)if(m.contains(p)&&!m.at(p).walk){if(m.biome==BiomeKind::LightlessCourt&&i%4!=1)break;WallTorchFeature f{p,m.at(p),true};m.wallTorches.push_back(f);m.at(p)=WallTorchTile;Color col=m.biome==BiomeKind::DrownedReliquary?Color{70,175,235}:m.biome==BiomeKind::PrismaticFoundry?Color{135,105,255}:Color{255,103,25};m.at(p).color=col;m.lights.push_back({p,col,8+double(r()%4),1.3,.2,double(r()%20)});break;}}
    if(rooms.size()>4){auto fp=rooms[2].c(),wp=rooms[3].c();m.provisions.push_back({fp,ProvisionKind::Food,m.biome==BiomeKind::LightlessCourt?"edible duskcap colony":"sealed dungeon larder",1+int(r()%3)});m.at(fp)=FoodTile;m.provisions.push_back({wp,ProvisionKind::Water,m.biome==BiomeKind::DrownedReliquary?"reliquary cistern":"condensation basin",2+int(r()%3)});m.at(wp)=WaterTile;m.groundItems.push_back({openNear(m,fp+Point{1,0}),makeItem(ItemKind::Ration)});m.groundItems.push_back({openNear(m,wp+Point{-1,0}),makeItem(ItemKind::Flask)});}
    for(int i=0;i<10;i++){Point p;do{auto&rm=rooms[1+r()%(rooms.size()-1)];p={rm.x+1+int(r()%max(1,rm.w-2)),rm.y+1+int(r()%max(1,rm.h-2))};}while(p==m.down||any_of(m.provisions.begin(),m.provisions.end(),[&](auto&s){return s.p==p;}));m.traps.push_back({p,TrapKind(r()%5),false,true});}
    for(size_t i=4;i<rooms.size();i+=5){Point p=rooms[i].c();m.machines.push_back({p,m.biome==BiomeKind::DrownedReliquary?MachineKind::Floodgate:m.biome==BiomeKind::PrismaticFoundry?MachineKind::Prism:m.biome==BiomeKind::LightlessCourt?MachineKind::Generator:MachineKind::Brazier,true});m.at(p)=MachineTile;m.lights.push_back({p,{230,145,55},8,1.0,.1,double(i)});}
    for(int i=0;i<32;i++){Point p;do{auto&rm=rooms[r()%rooms.size()];p={rm.x+int(r()%rm.w),rm.y+int(r()%rm.h)};}while(!m.at(p).walk);auto&e=m.element(p);if(m.biome==BiomeKind::AshenVaults)e.oil=20;else if(m.biome==BiomeKind::DrownedReliquary)e.wet=60;else if(m.biome==BiomeKind::PrismaticFoundry)e.electricity=2;else if(m.biome==BiomeKind::LightlessCourt)(i%2?e.web:e.poison)=20;else e.fire=5;}
    vector<Point> choke;for(int y=2;y<m.h-2;y++)for(int x=2;x<m.w-2;x++){Point p{x,y};if(!m.at(p).walk||p==m.up||p==m.down)continue;bool h=!m.at({x,y-1}).walk&&!m.at({x,y+1}).walk&&m.at({x-1,y}).walk&&m.at({x+1,y}).walk,v=!m.at({x-1,y}).walk&&!m.at({x+1,y}).walk&&m.at({x,y-1}).walk&&m.at({x,y+1}).walk;if((h||v)&&none_of(m.provisions.begin(),m.provisions.end(),[&](auto&s){return s.p==p;}))choke.push_back(p);}shuffle(choke.begin(),choke.end(),r);for(size_t i=0;i<min<size_t>(7,choke.size());i++)m.at(choke[i])=Door;
    if(rooms.size()>10){string firstName,firstLore,secondName,secondLore,itemName;
        if(name=="Hollowroot Mine"){firstName="Forewoman Maud's Station";firstLore="A survey badge, nine bell-wire spools, and an unsigned Valeward order lie fused into living roots. Maud marked the order REFUSED.";secondName="The Heat Manifold";secondLore="A crown-shaped valve forces Delve heat into the roots. Handprints show miners tried to turn it back together.";itemName="Maud Bramble's root-hook";}
        else if(name=="Sunken Barrow"){firstName="Roadwardens' Table";firstLore="Household marks on the stolen blocks match Stonecross doorsteps. Their original mortar outlines three equal circles, never a throne.";secondName="The False Toll";secondLore="A ledger charges every dead traveller a debt to Valeward. The same entries are carved into the Drowned Castellan's armour.";itemName="roadwarden's oath-hammer";}
        else if(name=="Moonwell Cavern"){firstName="Inverted Observatory";firstLore="The lens points down into reflected Valeward. Three bright threads enter the undercroft; a fourth crawls east toward Emberfall.";secondName="Tam's Paper Boat";secondLore="A child's folded boat sits perfectly dry beneath the false sky. Nella's handwriting inside says FOLLOW THE REAL CURRENT.";itemName="moonwell lens-knife";}
        else if(name=="Valeward Undercroft"){firstName="The Compact Table";firstLore="Root, road, and river seals surround an empty flame. The Regent's added crown splits the circle and turns every promise toward his chair.";secondName="The Regent's Confession";secondLore="The final line is gouged into the wall: IF THEY WILL NOT GIVE ME THE LIGHT, I WILL MAKE THEIR DUTY FEED IT.";itemName="broken compact sword";}
        else if(depth==1){firstName="Lampwright Muster";firstLore="A brass circuit names ROOT, ROAD, RIVER, and COMMON. Three blue needles point to a lower plate stamped: FOUNDATION SEAL · DEPTH SIX.";secondName="Iven's Last Station";secondLore="Fresh wick ash surrounds a maintenance slate: BLUE IS A DISTRESS COLOUR. THE REGENT IS PULLING FROM OUTSIDE. PELL AND CAL WENT LOWER.";itemName="Iven's survey blade";}
        else if(depth==2){firstName="Compact Cistern";firstLore="Channels from Moonwell and Emberfall meet without mixing. A Valeward bypass has been opened recently, stealing both flows westward.";secondName="Cal's Stand";secondLore="Black scales and a snapped sword mark where Cal Flint held a floodgate long enough for two companions to descend.";itemName="Cal Flint's broken sword";}
        else if(depth==3){firstName="Prism Arbitration Hall";firstLore="Four lenses once balanced one flame between four recipients. The crown lens was later rotated inward, concentrating every colour on an empty throne.";secondName="Pell's Working Proof";secondLore="Equations scratched across crystal show the Oathless Regent is a command repeated by the machinery, not a living succession.";itemName="Pell's prism stylus";}
        else if(depth==4){firstName="Gallery of Witnesses";firstLore="Village names cover the walls at equal height. Every Valeward name has been scraped away except one title: OATHLESS.";secondName="The Refusal Door";secondLore="The lock opens from either side and bears no keyhole—proof that the old Compact depended on consent, not custody.";itemName="witness-iron sabre";}
        else if(depth==5){firstName="The Crown Engine";firstLore="A later machine clamps itself over the Compact's older channels. Its crown-shaped teeth are worn from ninety years of trying to close.";secondName="Surveyor Pell's Rest";secondLore="Pell's pack faces the sixth stair. Her final page reads: A COMMAND WITHOUT A SPEAKER STILL NEEDS A WARDEN. FREE THE WARDEN OR END IT.";itemName="Pell's field blade";}
        else{firstName="Instruction Chamber";firstLore="The same order repeats across every wall: GATHER ROOT, ROAD, RIVER, LIGHT; CROWN THE REGENT. A second hand has carved NO beneath each line.";secondName="Warden's Vigil";secondLore="Three bedrolls lie before the seal. Iven, Cal, and Pell reached this room; drag marks continue toward the Warden, but none return.";itemName="sixth-depth wardblade";}
        m.landmarks.push_back({rooms[5].c(),firstName,firstLore});m.landmarks.push_back({rooms[9].c(),secondName,secondLore});m.groundItems.push_back({rooms[9].c(),equipment(itemName,Slot::MainHand,'/',2+depth)});
    }
    vector<string> roster=m.biome==BiomeKind::AshenVaults?vector<string>{"cave rat","candle kobold","gutter goblin","ember skeleton","ash cultist","slag orc","cinder salamander"}:m.biome==BiomeKind::DrownedReliquary?vector<string>{"ochre slime","glassfang spider","crystal myconid","tunnel troll","void whisper"}:m.biome==BiomeKind::PrismaticFoundry?vector<string>{"glassfang spider","crystal myconid","vault gargoyle","cinder mage","hungry mimic","vault warden"}:m.biome==BiomeKind::LightlessCourt?vector<string>{"blind cave bat","glassfang spider","vault gargoyle","umbra hound","void whisper","crown lich"}:vector<string>{"ember skeleton","slag orc","cinder salamander","cinder mage","vault warden","crown lich","furnace demon"};
    for(int i=0;i<12+depth*2;i++){auto& room=rooms[1+r()%(rooms.size()-1)];Point p;do{p={room.x+1+int(r()%max(1,room.w-2)),room.y+1+int(r()%max(1,room.h-2))};}while(p==m.down||any_of(m.monsters.begin(),m.monsters.end(),[&](auto&z){return z.p==p;}));int k=r()%roster.size();Monster z;z.p=z.home=p;z.name=roster[k];static string glyphs="rkgscomaGvLD";z.glyph=glyphs[k%glyphs.size()];z.hp=z.maxHp=8+depth*5+k*2;z.attack=3+depth+k/2;z.defense=depth/2;z.faction=z.name.find("goblin")!=string::npos||z.name.find("orc")!=string::npos?Faction::Goblin:z.name.find("skeleton")!=string::npos||z.name.find("lich")!=string::npos?Faction::Undead:z.name.find("gargoyle")!=string::npos||z.name.find("warden")!=string::npos?Faction::Construct:Faction::Cult;z.ranged=z.name.find("mage")!=string::npos||z.name.find("cultist")!=string::npos||z.name.find("lich")!=string::npos;z.nocturnal=false;m.monsters.push_back(z);}
    if(site){auto& boss=m.monsters.back();boss.p=boss.home=m.down;boss.boss=true;boss.hp=boss.maxHp=45+depth*12;boss.attack=8+depth;boss.defense=3+depth/2;boss.glyph='B';boss.name=name=="Hollowroot Mine"?"Root-Crowned Matriarch":name=="Sunken Barrow"?"Drowned Castellan":name=="Moonwell Cavern"?"Moonwell Wyrm":"Oathless Regent";string heartLore=name=="Hollowroot Mine"?"Maud Bramble's survey badge hangs inside the root-crown. Valeward wire enters the creature where a spine should be.":name=="Sunken Barrow"?"The Castellan's throne is built from Stonecross doorsteps, each household mark turned inward where nobody can witness it.":name=="Moonwell Cavern"?"The Wyrm has no body outside the water. A crown-shaped siphon gives the reflection teeth and draws the real river west.":"The last Regent sits wired into the Compact table. Root, road, river, and lamp-light enter his throne; none leave.";m.landmarks.push_back({m.down,"Heart of "+name,heartLore});}
    m.landmarks.push_back({m.up,name+" Entrance","The only dependable route back to the surface."});vector<Point> critical{m.up,m.down};guarantee(m,m.up,critical,dFloor);m.at(m.up)=Tile{'<',{230,210,150},true,false,false,"stairs up"};m.at(m.down)=site?Tile{'*',{245,165,72},true,false,false,"dungeon heart"}:Tile{'>',{230,210,150},true,false,false,"stairs down"};return m;
}

void Game::generate(){
    maps.clear();maps.push_back(buildTown(mixSeed(seed,11)));maps.push_back(buildWild(mixSeed(seed,23)));
    array<string,5> biomes{{"Ashen Vaults","Drowned Reliquary","Prismatic Foundry","Lightless Court","Crown of Cinders"}};for(int d=1;d<=5;d++)maps.push_back(buildDungeon(mixSeed(seed,700+d*37),d,biomes[d-1]+" · Depth "+to_string(d),false));
    array<string,4> sites{{"Hollowroot Mine","Sunken Barrow","Moonwell Cavern","Valeward Undercroft"}};for(int i=0;i<4;i++)maps.push_back(buildDungeon(mixSeed(seed,1100+i*97),i+1,sites[i],true));
    auto sixth=buildDungeon(mixSeed(seed,700+MAIN_DEPTHS*37),MAIN_DEPTHS,"The Unsurveyed Sixth · Depth 6",false);sixth.at(sixth.down)=Tile{'*',{255,188,72},true,false,false,"foundation seal"};auto&warden=sixth.monsters.back();warden.p=warden.home=sixth.down;warden.boss=true;warden.hp=warden.maxHp=125;warden.attack=16;warden.defense=7;warden.glyph='W';warden.name="Sixth Warden";sixth.landmarks.push_back({sixth.down,"The Foundation Seal","Black glass holds one steady flame with four equal wicks. Crown-shaped machinery grips its rim, forcing the Warden to repeat an order the seal was built to refuse."});maps.push_back(std::move(sixth));
    connectWorld();current=0;player=maps[0].start;
}

void Game::connectWorld(){
    auto findLand=[&](MapData&m,const string& n){return find_if(m.landmarks.begin(),m.landmarks.end(),[&](auto&l){return l.name==n;})->p;};
    Point townGate=findLand(maps[0],"Emberfall West Gate"), wildGate=findLand(maps[1],"Emberfall Road");maps[0].travels.push_back({townGate,1,maps[1].start,"Ashwind March"});maps[1].travels.push_back({wildGate,0,{townGate.x+2,townGate.y},"Emberfall"});
    maps[0].travels.push_back({maps[0].down,mainMapForDepth(1),maps[mainMapForDepth(1)].up,"Main Delve"});
    for(int depth=1;depth<=MAIN_DEPTHS;depth++){int here=mainMapForDepth(depth),previous=depth==1?0:mainMapForDepth(depth-1);maps[here].travels.push_back({maps[here].up,previous,depth==1?maps[0].down:maps[previous].down,"up"});if(depth<MAIN_DEPTHS){int next=mainMapForDepth(depth+1);maps[here].travels.push_back({maps[here].down,next,maps[next].up,"down"});}}
    array<string,4> names{{"Hollowroot Mine","Sunken Barrow","Moonwell Cavern","Valeward Undercroft"}};for(int i=0;i<SITE_COUNT;i++){Point entrance=findLand(maps[1],names[i]);int site=SITE_FIRST_MAP+i;maps[1].travels.push_back({entrance,site,maps[site].up,names[i],i==3});maps[site].travels.push_back({maps[site].up,1,entrance,"surface"});}
}

void Game::initQuests(){
    quests={
        {"blue_circuit","The Blue Circuit","mara","Emberfall's blue lamps trace an old warning route into the Delve.","Discover the Lampwright Muster on Depth 1","discover","Lampwright Muster","circuit_traced",""},
        {"sixth_record","The Erased Depth","sable","The missing survey identifies a Foundation Seal beneath the five public depths.","Defeat the Sixth Warden at the Foundation Seal","clear","The Unsurveyed Sixth · Depth 6","foundation_secured","blue_circuit"},
        {"common_light","A Light Held in Common","sable","The Oathless Regent's speaker and command must both be ended to restore the Lantern Compact.","Secure the Foundation Seal and end the Oathless Regent","flags","ending","story_complete","sixth_record",2},
        {"bramble_scouts","Bells Beyond the Hedge","elin","The Regent is using goblin scouts to test Bramblewick's alarm line.","Defeat three goblin scouts","kill","goblin","","",3},
        {"hollowroot","The Root Below","elin","Hollowroot's stolen heat line has twisted its roots and its missing forewoman.","Clear Hollowroot Mine","clear","Hollowroot Mine","bramble_safe","bramble_scouts"},
        {"stones","Witnesses, Not Debtors","dara","The original road oath can prove that Valeward rewrote mutual duty as village debt.","Discover Whispering Stones","discover","Whispering Stones","stones_found",""},
        {"barrow","The Stolen Road","dara","The Drowned Castellan is building a throne from Stonecross's missing oath-stones.","Clear Sunken Barrow","clear","Sunken Barrow","stonecross_safe","stones"},
        {"river","The Backward Current","nella","Moonwell is drawing Reedwater's river through the Compact's buried channels.","Discover Moonwell Cavern","discover","Moonwell Cavern","river_traced",""},
        {"moonwell","The False Moon","nella","A reflected Wyrm is holding open the intake that feeds Valeward's undercroft.","Clear Moonwell Cavern","clear","Moonwell Cavern","reedwater_safe","river"},
        {"compact","The Western Compact","roen","Valeward may act only with the freely given trust of all three village roads.","Secure Bramblewick, Stonecross, and Reedwater","flags","regional","valeward_commissioned","",3},
        {"regent","The Oathless Regent","roen","The last Regent survives as a command embedded in the Compact's machinery.","Clear Valeward Undercroft","clear","Valeward Undercroft","valeward_safe","compact"}
    };
}

bool Game::lineOfSight(Point a,Point b)const{
    auto&m=mapNow();int x=a.x,y=a.y,dx=abs(b.x-a.x),sx=a.x<b.x?1:-1,dy=-abs(b.y-a.y),sy=a.y<b.y?1:-1,err=dx+dy;
    while(true){if(x==b.x&&y==b.y)return true;if(!(x==a.x&&y==a.y)&&m.at({x,y}).opaque)return false;int e=2*err;if(e>=dy){err+=dy;x+=sx;}if(e<=dx){err+=dx;y+=sy;}if(!m.contains({x,y}))return false;}
}

bool Game::clearLight(Point a,Point b)const{
    auto&m=mapNow();int x=a.x,y=a.y,dx=abs(b.x-a.x),sx=a.x<b.x?1:-1,dy=-abs(b.y-a.y),sy=a.y<b.y?1:-1,err=dx+dy;
    while(true){if(x==b.x&&y==b.y)return true;int e=2*err;if(e>=dy){err+=dy;x+=sx;}if(e<=dx){err+=dx;y+=sy;}Point p{x,y};if(p==b)return true;if(!m.contains(p)||m.at(p).solid)return false;if(p==player)return false;if(any_of(m.monsters.begin(),m.monsters.end(),[&](auto&z){return z.hp>0&&z.p==p;}))return false;}
}

double Game::sunVisibility(Point p)const{
    if(!mapNow().outdoor||daylight()<.01)return 1;double progress=clamp((minutes()-360)/720.0,0.0,1.0),sx=cos(progress*3.14159265),sy=-.58-sin(progress*3.14159265)*.34;int clear=0;
    for(double off:{-.42,0.0,.42}){bool ok=true;for(int step=1;step<=14;step++){Point q{int(lround(p.x+sx*step-sy*off)),int(lround(p.y+sy*step+sx*off))};if(!mapNow().contains(q))break;if(mapNow().at(q).solid||q==player||any_of(mapNow().monsters.begin(),mapNow().monsters.end(),[&](auto&z){return z.hp>0&&z.p==q;})){ok=false;break;}}if(ok)clear++;}return clear/3.0;
}

bool Game::traceSubCellRay(double sx,double sy,double tx,double ty)const{
    auto&m=mapNow();double dx=tx-sx,dy=ty-sy;Point sourceCell{int(floor(sx)),int(floor(sy))},targetCell{int(floor(tx)),int(floor(ty))};int steps=max(2,int(ceil(max(abs(dx),abs(dy))*2.5)));
    for(int i=1;i<steps;i++){double t=i/double(steps);Point p{int(floor(sx+dx*t)),int(floor(sy+dy*t))};if(!m.contains(p))return false;if(p==sourceCell||p==targetCell)continue;bool actor=p==player||any_of(m.monsters.begin(),m.monsters.end(),[&](auto&z){return z.hp>0&&z.p==p;});if(m.at(p).opaque||!m.at(p).walk||actor||m.element(p).smoke>0)return false;}return true;
}

pair<double,double> Game::sampleLightVisibility(Point source,Point target,double softness)const{
    if(source==target)return{1,0};static constexpr array<pair<double,double>,5> offsets{{{0,0},{-.28,0},{.28,0},{0,-.28},{0,.28}}};int passed=0;for(auto[ox0,oy0]:offsets){double scale=softness/.28,ox=ox0*scale,oy=oy0*scale;if(traceSubCellRay(source.x+.5+ox,source.y+.5+oy,target.x+.5-ox*.4,target.y+.5-oy*.4))passed++;}double visibility=passed/double(offsets.size()),fringe=4*visibility*(1-visibility);
    if(visibility==0){static constexpr array<Point,8> around{{{1,0},{-1,0},{0,1},{0,-1},{1,1},{-1,1},{1,-1},{-1,-1}}};int edges=0;for(auto d:around){Point n=target+d;if(mapNow().contains(n)&&mapNow().at(n).walk&&traceSubCellRay(source.x+.5,source.y+.5,n.x+.5,n.y+.5))edges++;}if(edges)fringe=min(.42,edges*.075);}return{visibility,fringe};
}

static Color lerpColor(Color a,Color b,double t){t=clamp(t,0.0,1.0);return{int(lround(a.r+(b.r-a.r)*t)),int(lround(a.g+(b.g-a.g)*t)),int(lround(a.b+(b.b-a.b)*t))};}

#include "Effects.hpp"

LinearLight Game::illuminationAt(Point p,double now)const{
    auto&m=mapNow();LinearLight result;if(m.outdoor){double sun=daylight(),rise=max(0.0,1-abs(minutes()-360)/105.0),set=max(0.0,1-abs(minutes()-1080)/115.0),horizon=max(rise,set);Color night{7,12,29},day{190,202,176},base=lerpColor(night,day,pow(sun,.68));base.r=min(225,base.r+int(166*horizon*.32));base.g=min(215,base.g+int(86*horizon*.15));base.b=min(195,base.b+int(52*horizon*.05));double directional=.55+.45*sunVisibility(p);result={base.r/255.0*directional,base.g/255.0*directional,base.b/255.0*directional};}else result={7/255.0,8/255.0,14/255.0};
    auto add=[&](const Light&l){double dx=p.x-l.p.x,dy=(p.y-l.p.y)*1.8,d=sqrt(dx*dx+dy*dy),flame=0,slow=0;if(l.flicker>0){flame=sin(now*17.3+l.phase)*.55+sin(now*31.7+l.phase*2.1)*.3+sin(now*7.1+l.phase*.37)*.15;slow=sin(now*2.65+l.phase*.61);}double noise=max(.68,1+flame*l.flicker),effectiveRadius=l.radius*(1+(flame*.22+slow*.08)*l.flicker);if(d>effectiveRadius)return;auto[visibility,fringe]=sampleLightVisibility(l.p,p,l.softness);if(visibility<=0&&fringe<=0)return;double fall=pow(max(0.0,1-d/effectiveRadius),1.72)*l.intensity*noise,bounce=m.at(p).reflectivity*l.bounce*(1-visibility)*fall,diffracted=fringe*l.diffraction*fall,warmth=flame*l.flicker;double redScale=1+warmth*.08,greenScale=1+warmth*.025,blueScale=1-warmth*.13;result.r+=l.color.r/255.0*redScale*(fall*visibility+bounce+diffracted*1.35);result.g+=l.color.g/255.0*greenScale*(fall*visibility+bounce+diffracted*.72);result.b+=l.color.b/255.0*blueScale*(fall*visibility+bounce+diffracted*1.12);};
    for(auto&l:m.lights)add(l);if(lantern)add({player,race==RaceKind::Ashborn?Color{255,116,65}:Color{255,176,82},totalLightRadius(),lanternIntensity,.075,13.37,.48,.2,.14});for(auto&z:m.monsters)if(z.hp>0){optional<Color>glow;if(z.name.find("cinder")!=string::npos||z.name.find("demon")!=string::npos)glow=Color{255,72,18};else if(z.name.find("lich")!=string::npos||z.name.find("void")!=string::npos)glow=Color{142,82,235};else if(z.name.find("crystal")!=string::npos)glow=Color{95,205,255};if(glow)add({z.p,*glow,5.5,.7,.12,double(z.p.x),.4,.2,.1});}
    for(int y=max(0,p.y-7);y<=min(m.h-1,p.y+7);y++)for(int x=max(0,p.x-7);x<=min(m.w-1,p.x+7);x++)if(m.element({x,y}).fire>0)add({{x,y},{255,92,18},7,1,.2,x*.9+y,.45,.18,.12});
    for(auto&e:effects){
        double age=now-e.started;if(age<0||age>e.duration)continue;double t=age/e.duration;double gentle=reducedMotion?.45:1;
        if(e.projectile&&!e.path.empty()){Point source=e.path[min(e.path.size()-1,size_t(t*e.path.size()))];add({source,e.cool,8,2*gentle,0,0,.42,.2,.12});add({source,e.hot,2.8,2.5*gentle,0,0,.5,.18,.1});}
        else{bool healing=e.style==EffectStyle::Healing||e.style==EffectStyle::Radiant,steel=e.style==EffectStyle::Steel;double energy=healing?sin(t*3.14159265)*1.5:pow(1-t,2)*(steel?.85:4.4);add({e.center,lerpColor(e.hot,e.cool,t),steel?3.0:4+e.radius*2+t*5,energy*gentle,0,0,.55,.24,.14});if(!reducedMotion&&!healing&&!steel&&t<.14)add({e.center,e.hot,9.0+e.radius,2.7*(1-t/.14),0,0,.6,.25,.15});}
    }
    if(m.element(p).smoke>0){result.r*=.42;result.g*=.42;result.b*=.42;}result.r=min(1.8,result.r);result.g=min(1.8,result.g);result.b=min(1.8,result.b);return result;
}

double Game::lightAt(Point p)const{auto light=illuminationAt(p,visualSeconds());return max({light.r,light.g,light.b});}

int Game::totalAttack()const{int v=attack+(mightTurns?4:0);for(auto&i:inventory)if(i.equipped)v+=i.attack;return max(1,v-(food==0?2:0)-(water==0?1:0));}
int Game::totalDefense()const{int v=defense+(wardTurns?4:0)+(guardTurns?4:0);for(auto&i:inventory)if(i.equipped)v+=i.defense;return max(0,v-(water==0?1:0));}
int Game::totalSpellPower()const{int v=spellPower+(focusTurns?4:0);for(auto&i:inventory)if(i.equipped)v+=i.spell;return max(0,v-(water==0?2:0));}
double Game::totalLightRadius()const{double v=lanternRadius;for(auto&i:inventory)if(i.equipped)v+=i.light;return v;}
int Game::spellInvestment(SpellKind spell)const{int rank=0;for(auto&n:skills)if(n.spell&&*n.spell==spell)rank+=n.rank;return rank;}
int Game::spellCost(SpellKind spell)const{int cost=spellInfo(spell).cost-(calling==ClassKind::Arcanist?1:0)-(spellInvestment(spell)>=3?1:0);return max(1,cost);}

uint64_t Game::fingerprint(const MapData&m){uint64_t h=1469598103934665603ull;for(auto&t:m.tiles){h^=uint8_t(t.glyph);h*=1099511628211ull;}return h;}



#include "Interface.hpp"

int Game::menu(const string&title,const vector<string>&items,Color accent){
    if(items.empty())return -1;int selected=0;while(true){vector<string>lines;lines.push_back("\4  SELECT AN OPTION");for(size_t i=0;i<items.size();i++)lines.push_back(string(int(i)==selected?"\1\2 ◆ ":"\2   ")+to_string(i+1)+"  "+items[i]);lines.push_back("");lines.push_back("\5  ↑↓ Navigate · Enter Confirm · 1–9 Quick Select · Esc Back");renderPanel(title,lines,accent,selected+1,items.size()<=4);for(int i=0;i<4&&!_kbhit();i++)Sleep(8);if(!_kbhit())continue;int k=readKey();if(k==1001||k=='w'||k=='k')selected=(selected-1+int(items.size()))%int(items.size());else if(k==1002||k=='s'||k=='j')selected=(selected+1)%int(items.size());else if(k>='1'&&k<='9'&&k-'1'<int(items.size()))return k-'1';else if(k==13||k==' ')return selected;else if(k==27||k=='q')return -1;}
}

static size_t ansiSequenceEnd(const string&s,size_t at){if(at+1>=s.size()||s[at]!='\x1b'||s[at+1]!='[')return at;size_t i=at+2;while(i<s.size()){unsigned char c=s[i++];if(c>=0x40&&c<=0x7e)break;}return i;}
static int utf8Width(const string&s){int count=0;for(size_t i=0;i<s.size();){size_t ansiEnd=ansiSequenceEnd(s,i);if(ansiEnd!=i){i=ansiEnd;continue;}unsigned char c=s[i];size_t bytes=c<0x80?1:(c&0xe0)==0xc0?2:(c&0xf0)==0xe0?3:4;i=min(s.size(),i+bytes);count++;}return count;}
static size_t utf8Offset(const string&s,int columns){int count=0;size_t i=0;while(i<s.size()&&count<columns){size_t ansiEnd=ansiSequenceEnd(s,i);if(ansiEnd!=i){i=ansiEnd;continue;}unsigned char c=s[i];size_t bytes=c<0x80?1:(c&0xe0)==0xc0?2:(c&0xf0)==0xe0?3:4;i=min(s.size(),i+bytes);count++;}while(i<s.size()){size_t ansiEnd=ansiSequenceEnd(s,i);if(ansiEnd==i)break;i=ansiEnd;}return i;}
static string fitPad(string s,int width,char fill){int used=utf8Width(s);if(used>width){s=s.substr(0,utf8Offset(s,width));used=width;}return s+string(max(0,width-used),fill);}
static string centerPad(string s,int width){return string(max(0,(width-utf8Width(s))/2),' ')+s;}
static vector<string> wrapText(string text,int width){vector<string> lines;if(width<=0)return{""};while(utf8Width(text)>width){size_t boundary=utf8Offset(text,width),cut=boundary,space=text.rfind(' ',boundary);if(space!=string::npos&&space>0)cut=space;lines.push_back(text.substr(0,cut));text=text.substr(min(text.size(),cut));while(!text.empty()&&text.front()==' ')text.erase(text.begin());}lines.push_back(text);return lines;}
static string marqueeText(const string&s,int width,double now,char fill){if(width<=0)return"";if(utf8Width(s)<=width)return fitPad(s,width,fill);vector<string>glyphs;for(size_t i=0;i<s.size();){unsigned char c=s[i];size_t bytes=c<0x80?1:(c&0xe0)==0xc0?2:(c&0xf0)==0xe0?3:4;bytes=min(bytes,s.size()-i);glyphs.push_back(s.substr(i,bytes));i+=bytes;}for(string spacer:{" "," "," ","◇"," "," "," "})glyphs.push_back(spacer);int pause=8,tick=int(floor(now*7.5)),phase=((tick%(int(glyphs.size())+pause))+int(glyphs.size())+pause)%(int(glyphs.size())+pause),offset=phase<pause?0:phase-pause;string out;for(int i=0;i<width;i++)out+=glyphs[size_t((offset+i)%int(glyphs.size()))];return out;}
static string richBar(int value,int maximum,int width){int fill=maximum?clamp(int(lround(value/double(maximum)*width)),0,width):0;string s;for(int i=0;i<fill;i++)s+="█";for(int i=fill;i<width;i++)s+="░";return s;}
static Color quantizeColor(Color c){auto q=[](int v){return clamp(((v+2)/4)*4,0,255);};return{q(c.r),q(c.g),q(c.b)};}
static Color applyIllumination(Color source,LinearLight light){auto channel=[](int value,double lit){double level=lit*255,base=value*(level/150.0),bloom=max(0.0,level-255)*.28;return clamp(int(lround(base+bloom)),0,255);};return{channel(source.r,light.r),channel(source.g,light.g),channel(source.b,light.b)};}
static Color tileBackground(const Tile&tile,const MapData&m){if(tile.name=="street lamp"||tile.glyph=='$'||tile.name.find("window")!=string::npos)return{20,12,3};if(tile.name.rfind("plaque:",0)==0)return{22,15,4};if(tile.name.rfind("fixture:",0)==0)return{16,10,6};if(tile.name.find("road")!=string::npos)return{12,10,8};if(tile.name.find("cobble")!=string::npos)return{11,10,10};if(tile.name.find("interior")!=string::npos)return{13,8,5};if(tile.name.find("wall")!=string::npos&&m.town)return{8,7,7};if(tile.name.find("grass")!=string::npos||tile.name.find("garden")!=string::npos)return{5,12,7};if(tile.name.find("ashpine")!=string::npos)return{3,10,5};if(tile.name.find("water")!=string::npos)return{2,8,18};if(tile.name.find("closed door")!=string::npos)return{14,7,3};if(tile.name.find("open door")!=string::npos)return{8,6,5};if(tile.name.find("mounted wall torch")!=string::npos)return{24,8,1};if(tile.name.find("provision")!=string::npos)return{18,11,3};if(tile.name.find("machinery")!=string::npos)return{15,10,2};return m.outdoor?Color{4,9,6}:m.biome==BiomeKind::DrownedReliquary?Color{3,10,15}:m.biome==BiomeKind::PrismaticFoundry?Color{8,5,18}:m.biome==BiomeKind::LightlessCourt?Color{3,2,8}:m.biome==BiomeKind::CrownOfCinders?Color{15,3,1}:Color{5,5,8};}
static int visualHash(int x,int y,int seed){uint32_t v=uint32_t(x*374761393u+y*668265263u+seed*1442695041u);v=(v^(v>>13))*1274126177u;return int((v^(v>>16))&0x7fffffff);}

string Game::renderWorldFrame(double nowOverride){
    if(audioActive){double audioNow=visualSeconds();for(auto&entry:scheduledSounds)if(entry.first<=audioNow)playCue(entry.second);scheduledSounds.erase(remove_if(scheduledSounds.begin(),scheduledSounds.end(),[&](auto&entry){return entry.first<=audioNow;}),scheduledSounds.end());}
    ViewportFrame viewportFrame;auto&m=mapNow();auto[sw,sh]=ViewportFrame::size();int vh=sh-6,vw=sw,drawWidth=min(vw,m.w),drawHeight=min(vh,m.h),gutterX=(vw-drawWidth)/2,gutterY=(vh-drawHeight)/2;double now=nowOverride>=0?nowOverride:visualSeconds();
    Point shake{};if(!reducedMotion)for(auto&e:effects)if(!e.projectile&&e.radius>=3&&(e.style==EffectStyle::Fire||e.style==EffectStyle::Stone)){double age=now-e.started;if(age>=0&&age<.13){int frame=int(age*25);shake={frame%2?1:-1,0};break;}}
    int ox=clamp(player.x-drawWidth/2+shake.x,0,max(0,m.w-drawWidth)),oy=clamp(player.y-drawHeight/2+shake.y,0,max(0,m.h-drawHeight));ostringstream out;out<<frameReset(sw,sh);auto row=[&](int y){out<<"\x1b["<<y<<";1H";};
    string state=clockText()+"  LV "+to_string(level)+"  SP "+to_string(skillPoints)+"  T "+to_string(turns);
    if(sw>=116)state=raceName(race)+" "+className(calling)+"  ·  "+state;
    int areaWidth=max(12,sw-utf8Width(state)-4);row(1);out<<"\x1b[48;2;12;18;26m\x1b[38;2;235;194;112m "<<marqueeText(m.name,areaWidth,now/2)<<"  "<<state<<" \x1b[0m";
    string vitals;int column=sw/4;
    auto meter=[&](string label,int value,int maximum,string suffix,Color colour){string fraction=suffix=="%"?to_string(value)+"%":to_string(value)+"/"+to_string(maximum);int barWidth=clamp(column-utf8Width(label)-utf8Width(fraction)-3,2,16);ostringstream segment;ansiColor(segment,colour);segment<<fitPad(" "+label+" "+richBar(value,maximum,barWidth)+" "+fraction,column);return segment.str();};
    vitals+=meter(sw>=100?"HEALTH":"HP",hp,maxHp,"",{115,226,161});vitals+=meter(sw>=100?"MANA":"MP",mana,maxMana,"",{137,170,255});vitals+=meter(sw>=100?"HUNGER":"FOOD",food,100,"%",food==0?Color{255,135,102}:Color{224,191,125});vitals+=meter(sw>=100?"THIRST":"WATER",water,100,"%",water==0?Color{255,135,102}:Color{110,201,222});
    row(2);out<<"\x1b[48;2;7;11;17m"<<fitPad(vitals,sw)<<"\x1b[0m";
    string stats=" XP "+(sw>=104?richBar(xp,max(1,level*45),10)+" ":string{})+to_string(xp)+"/"+to_string(level*45)+"  ATK "+to_string(totalAttack())+" DEF "+to_string(totalDefense())+" MAG "+to_string(totalSpellPower())+"  "+(sw<76?"G ":"GOLD ")+to_string(gold)+"  LAMP "+(lantern?"ON":"OFF")+"  "+(sw<76?"R:":"REACT ")+reactionName(reaction);
    if(sw>=144)stats+="  F7 SOUND / F8 MOTION";
    row(3);out<<"\x1b[48;2;7;11;17m\x1b[38;2;173;190;208m"<<fitPad(stats,sw)<<"\x1b[0m";
    optional<Color> lastFg,lastBg;auto cellOut=[&](const string&glyph,Color fg,Color bg){fg=quantizeColor(fg);bg=quantizeColor(bg);if(!lastFg||lastFg->r!=fg.r||lastFg->g!=fg.g||lastFg->b!=fg.b){ansiColor(out,fg);lastFg=fg;}if(!lastBg||lastBg->r!=bg.r||lastBg->g!=bg.g||lastBg->b!=bg.b){ansiBg(out,bg);lastBg=bg;}out<<glyph;};
    double solar=pow(daylight(),.62),inner=m.outdoor?9+solar*10:10.5,outer=m.outdoor?24+solar*26:27;
    for(int sy=0;sy<vh;sy++){row(4+sy);for(int sx=0;sx<vw;sx++){if(sx<gutterX||sx>=gutterX+drawWidth||sy<gutterY||sy>=gutterY+drawHeight){cellOut(" ",{0,0,0},{2,3,5});continue;}Point p{ox+sx-gutterX,oy+sy-gutterY};double dx=p.x-player.x,dy=(p.y-player.y)*1.65,dist=sqrt(dx*dx+dy*dy),perception;if(dist<=inner)perception=1;else if(dist>=outer)perception=0;else{double t=(dist-inner)/(outer-inner),smooth=t*t*t*(t*(t*6-15)+10);perception=1-smooth;}bool visible=perception>0&&lineOfSight(player,p);auto idx=size_t(p.y)*m.w+p.x;if(visible&&perception>.035)m.explored[idx]=1;if(!m.explored[idx]){cellOut(" ",{0,0,0},{0,0,0});continue;}auto&tile=m.at(p);char glyph=tile.glyph;Color fg=tile.color,bg=tileBackground(tile,m);
            if(visible){auto&el=m.element(p);int frame=int(now*8),slow=int(now*2.5);
                if(el.fire>0){glyph=(frame+p.x+p.y)%3==0?'^':(frame+p.x)%3==0?'*':'A';fg={255,112+(frame%3)*25,22};bg={55,9,1};}
                else if(el.electricity>0){glyph=(frame+p.x)%2?'z':'*';fg={100,205,255};bg={2,18,36};}
                else if(el.frost>0){glyph='*';fg={165,225,255};bg={8,22,32};}
                else if(el.poison>0){glyph=';';fg={105,215,72};bg={8,24,4};}
                else if(el.smoke>0){glyph=(frame+p.x+p.y)%3==0?';':':';fg={105,100,112};bg={12,11,15};}
                else if(el.web>0){glyph='~';fg={195,200,205};}
                else if(el.wet>0){glyph=(slow+p.x)%2?'~':'-';fg={70,145,195};bg={3,14,25};}
                else if(tile.walk&&m.biome==BiomeKind::AshenVaults&&visualHash(p.x+frame/2,p.y-frame,211)%83==0){glyph='.';fg={142,125,116};}
                else if(tile.walk&&m.biome==BiomeKind::PrismaticFoundry&&tile.reflectivity>.25&&visualHash(p.x,p.y,slow)%79==0){glyph='*';fg=(slow%3==0?Color{100,235,255}:slow%3==1?Color{255,100,225}:Color{225,245,145});}
                else if(tile.walk&&m.biome==BiomeKind::LightlessCourt&&visualHash(p.x-slow,p.y+frame/2,419)%101==0){glyph='.';fg={100,88,135};}
                else if(tile.walk&&m.biome==BiomeKind::CrownOfCinders&&visualHash(p.x+slow,p.y-frame,557)%67==0){glyph=frame%3?'\'':'!';fg={245,92,28};}
                if(auto trap=find_if(m.traps.begin(),m.traps.end(),[&](auto&t){return t.p==p&&t.revealed;});trap!=m.traps.end()){glyph='^';fg=trap->armed?Color{255,95,62}:Color{115,105,102};}if(auto item=find_if(m.groundItems.begin(),m.groundItems.end(),[&](auto&i){return i.p==p;});item!=m.groundItems.end()){glyph=item->item.glyph;fg=item->item.color;}if(auto n=find_if(m.npcs.begin(),m.npcs.end(),[&](auto&a){return a.p==p;});n!=m.npcs.end()){glyph='n';fg=n->color;}if(auto z=find_if(m.monsters.begin(),m.monsters.end(),[&](auto&a){return a.hp>0&&a.p==p;});z!=m.monsters.end()){glyph=z->glyph;double pulse=.88+sin(now*(z->intent==Intent::Sleep?2.4:4.2)+p.x*.43+p.y*.71)*.1;fg=fg=z->color.scaled(pulse);if(z->name.find("slime")!=string::npos&&(int(now*3+p.x)%4==0))glyph='~';else if(z->name.find("spider")!=string::npos&&(int(now*7+p.y)%5==0))glyph='x';else if(z->name.find("whisper")!=string::npos&&(int(now*5+p.x)%6==0))glyph='?';}if(p==player){glyph='@';fg={248,240,210};}
                auto illumination=illuminationAt(p,now);fg=applyIllumination(fg,illumination);bg=applyIllumination(bg,{illumination.r*.72,illumination.g*.72,illumination.b*.72});double shimmer=.5+.5*sin(now*4.2+p.x*.61+p.y*1.17);if(el.wet>0||tile.name.find("water")!=string::npos){Color reflected{int(min(255.0,illumination.b*127)),int(min(255.0,illumination.g*127)),int(min(255.0,illumination.r*85))};bg=lerpColor(bg,reflected,.08+shimmer*.12);}if(tile.walk&&m.biome==BiomeKind::PrismaticFoundry)fg=lerpColor(fg,{int(min(255.0,illumination.r*255)),int(min(255.0,illumination.g*255)),int(min(255.0,illumination.b*255))},min(.22,tile.reflectivity*(.1+shimmer*.25)));for(auto&e:effects)effectCell(e,p,now,glyph,fg,bg);if(p==player){glyph='@';fg={248,240,210};}double fade=.025+perception*.975;fg=fg.scaled(fade);bg=bg.scaled(fade);if(cursor&&*cursor==p){bg={82,22,18};fg={255,255,255};}}
            else{double memory=m.town?.22:.025;fg=fg.scaled(memory);bg=bg.scaled(memory*.72);}string symbol(1,glyph);if(glyph==tile.glyph){if(tile.name.find("wall")!=string::npos||tile.name.find("basalt")!=string::npos)symbol=m.biome==BiomeKind::PrismaticFoundry||m.biome==BiomeKind::CrownOfCinders?"█":"▓";else if(tile.name.find("rubble")!=string::npos)symbol="░";else if(tile.name.find("water")!=string::npos)symbol="≈";else if(tile.name.find("road")!=string::npos)symbol="·";else if(tile.name.find("street lamp")!=string::npos)symbol="†";else if(tile.name.find("mounted wall torch")!=string::npos)symbol="╫";else if(tile.name.find("machinery")!=string::npos)symbol="¤";else if(tile.name.find("ashpine")!=string::npos)symbol="♣";else if(tile.name.find("boulder")!=string::npos)symbol="▲";}cellOut(symbol,fg,bg);}}
    for(auto&label:combatText){double age=now-label.started;if(age<0||age>label.duration||!lineOfSight(player,label.p))continue;int x=label.p.x-ox+gutterX,y=label.p.y-oy+gutterY-(reducedMotion?0:int(age*1.5));if(x<0||x+int(label.text.size())>sw||y<0||y>=vh)continue;out<<"\x1b["<<4+y<<';'<<x+1<<"H\x1b[48;2;9;12;18m";ansiColor(out,label.color);out<<label.text;}
    row(sh-2);out<<"\x1b[0m\x1b[48;2;12;18;26m\x1b[38;2;225;187;110m"<<marqueeText(" "+objectiveTracker(),sw,now/2)<<"\x1b[0m";
    out<<"\x1b[0m";string msg=" "+message;if(msg!=messageMarqueeKey){messageMarqueeKey=msg;messageMarqueeStarted=now;}row(sh-1);out<<"\x1b[48;2;7;8;12m\x1b[38;2;238;225;195m"<<marqueeText(msg,sw,now-messageMarqueeStarted)<<"\x1b[0m";string keys=cursor?" Move: Arrows/Numpad · Enter Examine · Tab Next · Esc Close":sw<100?" E Use · I Pack · Z Spells · T Skills · Tab Journal · ? Help":" Move: Arrows/Numpad/HJKL · E Use · I Pack · C Character · Z Spells · T Skills · Tab Journal · M Map · ? Help · Q Quit";row(sh);out<<"\x1b[48;2;7;8;12m\x1b[38;2;154;174;193m"<<fitPad(keys,sw)<<"\x1b[0m";return out.str();
}

void Game::draw(){cout<<"\x1b[?2026h"<<renderWorldFrame()<<"\x1b[?2026l"<<flush;}

void Game::renderQaShowcase(bool showMenu){
    current=2;auto&m=mapNow();player=m.wallTorches.empty()?m.start:openNear(m,m.wallTorches.front().p);fill(m.explored.begin(),m.explored.end(),1);double now=visualSeconds();Point impact=openNear(m,player+Point{8,2});vector<Point> path;int dx=impact.x-player.x,dy=impact.y-player.y,steps=max(abs(dx),abs(dy));for(int i=0;i<=steps;i++)path.push_back({player.x+int(lround(dx*i/double(max(1,steps)))),player.y+int(lround(dy*i/double(max(1,steps))))});effects.push_back({path,impact,{255,255,235},{255,93,18},now-.18,1.1,3,true});effects.push_back({{},impact,{255,255,255},{255,65,12},now-.12,1.35,4,false});if(!showMenu){cout<<"\x1b[?2026h"<<renderWorldFrame()<<"\x1b[?2026l";return;}addItem(makeItem(ItemKind::Bomb));addItem(equipment("lich-light diadem",Slot::Head,']',0,1,3,.4));auto order=inventoryOrder(InventorySort::Type);size_t actual=inventory.size()-1;int selected=int(find(order.begin(),order.end(),actual)-order.begin());auto[sw,sh]=consoleViewport();bool compact=sw<104||sh<34;renderPanel("PACK & EQUIPMENT · SORT TYPE",inventoryLines(selected,compact,1.4,InventorySort::Type),inventory[actual].color,-1,compact);
}

void Game::renderQaTown(){current=0;turns=80;auto&m=mapNow();player=m.start;fill(m.explored.begin(),m.explored.end(),1);cout<<"\x1b[?2026h"<<renderWorldFrame()<<"\x1b[?2026l";}

string Game::renderSplashFrame(double elapsed)const{
    ViewportFrame viewportFrame;auto[sw,sh]=ViewportFrame::size();struct Cell{string glyph{" "};Color fg{70,75,86},bg{2,3,7};bool solid{};};vector<Cell>canvas(size_t(sw)*sh);auto at=[&](int x,int y)->Cell&{return canvas[size_t(y)*sw+x];};
    for(int y=0;y<sh;y++)for(int x=0;x<sw;x++)if(visualHash(x,y,913)%173==0){at(x,y).glyph="·";at(x,y).fg={38,46,63};}
    static const map<char,array<string,5>> font{
        {'L',{{"#    ","#    ","#    ","#    ","#####"}}},{'U',{{"#   #","#   #","#   #","#   #","#####"}}},{'M',{{"#   #","## ##","# # #","#   #","#   #"}}},{'E',{{"#####","#    ","#### ","#    ","#####"}}},{'N',{{"#   #","##  #","# # #","#  ##","#   #"}}},{'R',{{"#### ","#   #","#### ","#  # ","#   #"}}},{'O',{{" ### ","#   #","#   #","#   #"," ### "}}},{'G',{{" ####","#    ","#  ##","#   #"," ### "}}}
    };string word="LUMENROGUE";int letterGap=sw>=86?2:1,wordWidth=int(word.size())*5+(int(word.size())-1)*letterGap,logoX=max(0,(sw-wordWidth)/2),logoY=1;for(int row=0;row<5;row++){int x=logoX;for(size_t index=0;index<word.size();index++){auto pattern=font.at(word[index])[row];for(char ch:pattern){if(ch!=' '){at(x,logoY+row).glyph="█";at(x,logoY+row).fg={230,188,122};}x++;}if(index+1<word.size())x+=letterGap;}}
    int top=max(9,sh/2),bottom=max(top+5,sh-4),mid=(top+bottom)/2;for(int x=1;x<sw-1;x++){at(x,top).glyph=visualHash(x,top,31)%4?"▓":"▒";at(x,top).fg={82,82,94};at(x,top).solid=true;at(x,bottom).glyph=visualHash(x,bottom,47)%4?"▓":"▒";at(x,bottom).fg={72,72,82};at(x,bottom).solid=true;for(int y=top+1;y<bottom;y++){at(x,y).glyph=visualHash(x,y,59)%9?"·":"∙";at(x,y).fg={70,67,65};at(x,y).bg={5,5,8};}}
    int pillar=sw*3/5;for(int y=top+1;y<bottom;y++)if(y!=mid){at(pillar,y).glyph="█";at(pillar,y).fg={88,86,98};at(pillar,y).solid=true;}Point torch{max(4,sw/6),mid};at(torch.x,torch.y).glyph="╫";at(torch.x,torch.y).fg={255,164,58};
    struct SLight{double x,y,radius,intensity;Color color;};vector<SLight>lights{{double(torch.x),double(torch.y),13,1.35,{255,106,24}},{sw*.34,3.0,max(16.0,sw*.22),.48,{255,112,35}},{sw*.69,3.0,max(16.0,sw*.23),.42,{96,116,255}}};double phase=fmod(max(0.0,elapsed),5.4),flame=sin(elapsed*17.3)*.55+sin(elapsed*31.7)*.3+sin(elapsed*7.1)*.15;lights[0].intensity*=1+flame*.2;lights[0].radius*=1+flame*.045;Point impact{max(torch.x+12,sw*5/6),mid};if(phase>=.55&&phase<3.1){double t=(phase-.55)/2.55,ease=t*t*(3-2*t);int bx=int(lround(torch.x+3+(impact.x-torch.x-3)*ease));at(bx,mid).glyph="●";at(bx,mid).fg={255,250,215};if(bx>1){at(bx-1,mid).glyph="✦";at(bx-1,mid).fg={255,133,35};}lights.push_back({double(bx),double(mid),10,2.35,{255,92,18}});lights.push_back({double(bx),double(mid),3.2,3.2,{255,248,210}});}else if(phase>=3.1&&phase<4.55){double t=(phase-3.1)/1.45,flash=pow(max(0.0,1-t),2.0),radius=2+t*15;lights.push_back({double(impact.x),double(impact.y),radius,6.5*flash+.22,{255,69,12}});if(t<.16)lights.push_back({double(impact.x),double(impact.y),22,8*(1-t/.16),{255,255,245}});for(int y=top+1;y<bottom;y++)for(int x=max(1,impact.x-int(radius));x<min(sw-1,impact.x+int(radius));x++){double dx=x-impact.x,dy=(y-impact.y)*1.8,d=sqrt(dx*dx+dy*dy);if(abs(d-radius*.62)<1.15){at(x,y).glyph=visualHash(x,y,int(elapsed*30))%2?"✦":"*";at(x,y).fg={255,145,36};}}}
    auto clear=[&](const SLight&light,int tx,int ty){int x=int(lround(light.x)),y=int(lround(light.y)),dx=abs(tx-x),sx=x<tx?1:-1,dy=-abs(ty-y),sy=y<ty?1:-1,err=dx+dy;while(true){if(x==tx&&y==ty)return true;int e=2*err;if(e>=dy){err+=dy;x+=sx;}if(e<=dx){err+=dx;y+=sy;}if(x==tx&&y==ty)return true;if(x<0||y<0||x>=sw||y>=sh||at(x,y).solid)return false;}};
    for(int y=0;y<sh;y++)for(int x=0;x<sw;x++){LinearLight lit{.018,.022,.042};for(auto&light:lights){double dx=x-light.x,dy=(y-light.y)*1.75,d=sqrt(dx*dx+dy*dy);if(d>light.radius)continue;bool visible=clear(light,x,y);double edge=0;if(!visible)for(int oy=-1;oy<=1;oy++)if(clear(light,clamp(x,0,sw-1),clamp(y+oy,0,sh-1))){edge=.075;break;}double fall=pow(max(0.0,1-d/light.radius),1.65)*light.intensity*(visible?1:edge);lit.r+=light.color.r/255.0*fall;lit.g+=light.color.g/255.0*fall;lit.b+=light.color.b/255.0*fall;}auto&cell=at(x,y);cell.fg=applyIllumination(cell.fg,lit);cell.bg=applyIllumination(cell.bg,{lit.r*.68,lit.g*.68,lit.b*.68});if(y>=logoY&&y<logoY+5&&cell.glyph=="█"){double blend=wordWidth<=1?0:(x-logoX)/double(wordWidth-1);cell.fg=lerpColor({255,170,78},{142,170,255},clamp(blend,0.0,1.0));cell.bg={3,4,9};}}
    auto centered=[&](int y,const string&text,Color fg){for(int x=0;x<sw;x++){auto&cell=at(x,y);cell.glyph=" ";cell.bg={3,4,9};}int start=max(0,(sw-utf8Width(text))/2);for(size_t i=0;i<text.size()&&start+int(i)<sw;i++){auto&cell=at(start+int(i),y);cell.glyph=string(1,text[i]);cell.fg=fg;}};
    centered(6,"L U M E N   R O G U E",{246,224,180});string subtitle=sw>=72?"A TRUE-COLOUR ROGUELIKE OF SHARED LIGHT AND BROKEN OATHS":"SHARED LIGHT / BROKEN OATHS";centered(7,subtitle,{170,178,196});
    ostringstream out;out<<frameReset(sw,sh);optional<Color>lastFg,lastBg;auto differs=[](const Color&a,const Color&b){return a.r!=b.r||a.g!=b.g||a.b!=b.b;};for(int y=0;y<sh;y++){out<<"\x1b["<<y+1<<";1H";for(int x=0;x<sw;x++){auto cell=at(x,y);cell.fg=quantizeColor(cell.fg);cell.bg=quantizeColor(cell.bg);if(!lastFg||differs(*lastFg,cell.fg)){ansiColor(out,cell.fg);lastFg=cell.fg;}if(!lastBg||differs(*lastBg,cell.bg)){ansiBg(out,cell.bg);lastBg=cell.bg;}out<<cell.glyph;}}
    string prompt="[ PRESS ANY KEY TO CARRY THE LIGHT ]";Color promptColor=lerpColor({128,113,91},{255,220,145},.5+.5*sin(elapsed*3.2));int px=max(1,(sw-utf8Width(prompt))/2+1);out<<"\x1b["<<sh-1<<';'<<px<<"H\x1b[48;2;3;4;9m\x1b[38;2;"<<promptColor.r<<';'<<promptColor.g<<';'<<promptColor.b<<'m'<<prompt<<"\x1b[0m";return out.str();
}

void Game::splashScreen(){double started=visualSeconds();while(!_kbhit()){cout<<"\x1b[?2026h"<<renderSplashFrame(visualSeconds()-started)<<"\x1b[?2026l"<<flush;Sleep(32);}readKey();cout<<"\x1b[2J";}
void Game::renderQaSplash(){cout<<"\x1b[?2026h"<<renderSplashFrame(3.23)<<"\x1b[?2026l";}

#include "CreationView.hpp"

void Game::renderQaCreation(){
    configureCharacter(RaceKind::Ashborn,ClassKind::Arcanist);current=0;auto profile=characterProfile(RaceKind::Ashborn,ClassKind::Arcanist);auto details=classLore(ClassKind::Arcanist),art=classPortrait(ClassKind::Arcanist);details.push_back("VITALS  HP "+to_string(profile.maxHp)+"  MANA "+to_string(profile.maxMana)+"  ATK "+to_string(profile.attack)+"  DEF "+to_string(profile.defense));details.push_back("ATTRIBUTES  STR "+to_string(profile.strength)+"  DEX "+to_string(profile.dexterity)+"  INT "+to_string(profile.intellect)+"  CON "+to_string(profile.constitution)+"  WIS "+to_string(profile.wisdom));details.push_back("EQUIPMENT  "+profile.weapon);details.push_back("TACTICS  "+profile.playstyle);vector<string>options{"VANGUARD · Front-line warden","ARCANIST · Aether cartographer","RANGER · March stalker","LIGHTBEARER · Keeper of the dawn"};renderPanel("II · CHOOSE A CALLING · Ashborn",creationPresentation(options,details,art,1),{118,184,255},-1,false);
}

void Game::renderQaAncestry(){
    configureCharacter(RaceKind::Elf,ClassKind::Ranger);current=0;auto details=raceLore(RaceKind::Elf),art=racePortrait(RaceKind::Elf);details.push_back("ATTRIBUTES  DEX +3  INT +3  CON -1");vector<string>options{"HUMAN · Hearthborn","DWARF · Deep-kin","ELF · Twilight Scion","ASHBORN · Ember-souled"};renderPanel("I · CHOOSE AN ANCESTRY",creationPresentation(options,details,art,2),{255,174,66},-1,false);
}

void Game::renderQaInspect(bool detailed){
    configureCharacter(RaceKind::Elf,ClassKind::Ranger);current=2;auto&m=mapNow();fill(m.explored.begin(),m.explored.end(),1);auto monster=find_if(m.monsters.begin(),m.monsters.end(),[](auto&entry){return entry.hp>0&&entry.name=="candle kobold";});if(monster==m.monsters.end())monster=find_if(m.monsters.begin(),m.monsters.end(),[](auto&entry){return entry.hp>0;});if(monster!=m.monsters.end())monster->name="candle kobold";Point focus=monster==m.monsters.end()?m.start:monster->p;player=openNear(m,focus+Point{-4,0});cursor=focus;message=inspectSummary(focus)+(detailed?"":" · Enter examines");if(detailed){string title=monster==m.monsters.end()?m.at(focus).name:monster->name;renderPanel("EXAMINE · "+title,inspectDetails(focus),{236,178,92},-1,false);}else cout<<"\x1b[?2026h"<<renderWorldFrame(18.25)<<"\x1b[?2026l";
}

void Game::renderQaDialogue(){configureCharacter(RaceKind::Elf,ClassKind::Ranger);current=0;turns=80;auto&m=mapNow();fill(m.explored.begin(),m.explored.end(),1);auto npc=find_if(m.npcs.begin(),m.npcs.end(),[](auto&entry){return entry.id=="mara";});if(npc==m.npcs.end())return;player=openNear(m,npc->p+Point{-2,0});vector<DialogueChoice>choices{{"Put those places on my map.","root","map"},{"And the Delve?","delve",""},{"Back.","town",""}};renderPanel("DIALOGUE · "+npc->name,conversationLines(*npc,npc->name+" · "+npc->role,npcTopicDialogue(*npc,"places",true),choices,0),npc->color,-1,false);}

void Game::renderQaSkillTree(double elapsed){configureCharacter(RaceKind::Human,ClassKind::Vanguard);current=2;level=7;skillPoints=3;fill(mapNow().explored.begin(),mapNow().explored.end(),1);skills[0].rank=3;skills[1].rank=2;skills[6].rank=2;skills[7].rank=1;skills[12].rank=2;skills[13].rank=1;skills[18].rank=1;auto[width,height]=consoleViewport();bool compact=width<116||height<34;renderPanel("SKILL CONSTELLATION · VANGUARD · LEVEL 7 · 3 POINTS",skillTreeLines(1,compact,elapsed),{225,178,92},-1,compact);}

void Game::run(){
    coalesceInventory();audioActive=true;
    while(!quit&&hp>0){double now=visualSeconds();effects.erase(remove_if(effects.begin(),effects.end(),[&](auto&e){return now-e.started>e.duration;}),effects.end());combatText.erase(remove_if(combatText.begin(),combatText.end(),[&](auto&entry){return now-entry.started>entry.duration;}),combatText.end());draw();for(int i=0;i<4&&!_kbhit();i++)Sleep(8);if(_kbhit())handle(readKey());}if(hp<=0)panel("YOUR LIGHT IS GONE",{"The March remembers the spark you carried.","Your seed remains reproducible: "+to_string(seed)});
}

void Game::handle(int k){
    if(k==2007){auto&audio=soundBank();audio.enabled=!audio.enabled;if(!audio.enabled)audio.stop();if(!audio.available()){message="Audio is unavailable on this system; visual effects remain active.";return;}message=audio.enabled?"SOUND ON · F7 mutes effects.":"SOUND MUTED · F7 enables effects.";return;}
    if(k==2008){reducedMotion=!reducedMotion;message=reducedMotion?"REDUCED MOTION · Camera shake off; gentler flashes. F8 restores full effects.":"FULL EFFECTS · F8 enables reduced motion.";return;}
    if(auto movement=keyDirection(k)){movePlayer(*movement);return;}
    if(k=='e'||k==13)interact();else if(k==' '||k=='5')passTurn();else if(k=='o'){lantern=!lantern;message=lantern?"You uncover the lantern.":"You shutter the lantern.";passTurn();}else if(k=='i')inventoryMenu();else if(k=='c')characterMenu();else if(k=='z')spellbookMenu();else if(k=='t')skillTreeMenu();else if(k=='x')inspectMenu();else if(k=='v')searchNearby();else if(k=='r')rest();else if(k=='g'){auto&m=mapNow();auto it=find_if(m.groundItems.begin(),m.groundItems.end(),[&](auto&i){return i.p==player;});if(it==m.groundItems.end())message="There is nothing here to pick up.";else{string name=it->item.name;addItem(it->item);m.groundItems.erase(it);message="PICKED UP · "+name+".";passTurn();}}else if(k=='p'){auto it=find_if(inventory.begin(),inventory.end(),[](auto&i){return i.kind==ItemKind::Potion;});if(it==inventory.end())message="You have no healing potion.";else useItem(size_t(it-inventory.begin()));}else if(k=='f'&&!learnedSpells.empty()){auto s=learnedSpells.front();auto info=spellInfo(s);Point target=player;if(info.range){auto p=targetCursor("TARGET · "+info.name,info.range);if(!p)return;target=*p;}castSpell(s,target);}else if(k=='m')mapMenu();else if(k==9)questMenu();else if(k==2002)fastTravelMenu();else if(k==2003)craftingMenu();else if(k==2004)loreMenu();else if(k==2005){save("lumenrogue-cpp.save");}else if(k==2006){reaction=Reaction((int(reaction)+1)%4);message="REACTION PREPARED · "+reactionName(reaction)+".";}else if(k==2009){auto g=load("lumenrogue-cpp.save");if(g){bool playingAudio=audioActive;*this=std::move(*g);audioActive=playingAudio;message="QUICK-SAVE LOADED.";}else message="No valid quick-save was found.";}else if(k=='?'||k==2001)helpMenu();else if(k=='q'||k==27){int v=menu("QUIT GAME",{"CONTINUE PLAYING","QUIT TO DESKTOP"},{255,105,62});if(v==1)quit=true;}
}

void Game::movePlayer(Point d){
    auto&m=mapNow();Point target=player+d;if(m.contains(target)&&m.at(target).name=="closed door"){m.at(target)=Tile{'/',{145,105,68},true,false,false,"open door"};message="You open the door, cutting a new line through the light.";passTurn();return;}if(!m.contains(target)||!m.at(target).walk){message="The "+(m.contains(target)?m.at(target).name:string("edge of the world"))+" bars your way.";return;}
    if(auto n=find_if(m.npcs.begin(),m.npcs.end(),[&](auto&a){return a.p==target;});n!=m.npcs.end()){talk(*n);return;}
    if(auto z=find_if(m.monsters.begin(),m.monsters.end(),[&](auto&a){return a.hp>0&&a.p==target;});z!=m.monsters.end()){attackMonster(*z);passTurn();return;}
    player=target;triggerTrap(player);if(auto t=find_if(m.travels.begin(),m.travels.end(),[&](auto&a){return a.p==player;});t!=m.travels.end()){travel(*t);return;}passTurn();if(any_of(m.groundItems.begin(),m.groundItems.end(),[&](auto&i){return i.p==player;}))message="An item lies here. Press G to pick it up.";else for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++){Point p=player+Point{dx,dy};if(m.contains(p)&&m.at(p).name.rfind("plaque: ",0)==0){message="SIGN: "+m.at(p).name.substr(8)+". Press E to read.";return;}}
}

void Game::travel(const Travel&t){
    if(t.castleLocked&&!flags.contains("valeward_commissioned")){refreshQuestProgress();vector<string>missing;if(!flags.contains("bramble_safe"))missing.push_back("Bramblewick");if(!flags.contains("stonecross_safe"))missing.push_back("Stonecross");if(!flags.contains("reedwater_safe"))missing.push_back("Reedwater");if(missing.empty())message="UNDERCROFT SEALED · All three villages support you. Report the Western Compact to Captain Roen for a formal commission.";else{string needed;for(size_t i=0;i<missing.size();i++){if(i)needed+=i+1==missing.size()?" and ":", ";needed+=missing[i];}message="UNDERCROFT SEALED · Earn the trust of "+needed+". Captain Roen and your quest journal track the compact.";}return;}
    string origin=mapNow().name;current=t.targetMap;player=t.arrival;int depth=mainDepthForMap(current);if(depth==1)message="THE BLUE CIRCUIT · You descend from Emberfall into the Ashen Vaults, following cold blue wax toward the Lampwright Muster.";else if(depth>1)message="DESCENT · "+origin+" gives way to "+maps[current].name+". Compact-era channels continue through the walls.";else if(maps[current].name=="Hollowroot Mine")message="HOLLOWROOT · Warm air pulses through the roots in time with distant hedge bells.";else if(maps[current].name=="Sunken Barrow")message="SUNKEN BARROW · Stolen road-stones shift under black water, each bearing a Stonecross household mark.";else if(maps[current].name=="Moonwell Cavern")message="MOONWELL · The ceiling is dark, but a false sky shines beneath your feet and its current runs toward Valeward.";else if(maps[current].name=="Valeward Undercroft")message="THE UNDERCROFT · Three village seals open together. Somewhere below, a dead command welcomes its witnesses.";else message="TRAVEL · You follow the maintained route to "+maps[current].name+".";discoverNearby();
}

void Game::advanceSurvival(){
    int previousFood=food,previousWater=water;if(turns%12==0){food=max(0,food-5);water=max(0,water-5);}int attritionInterval=max(1,720/max(1,maxHp));if((food==0||water==0)&&turns%attritionInterval==0)hp=max(0,hp-1);bool hungerBegan=previousFood>0&&food==0,thirstBegan=previousWater>0&&water==0;if(hungerBegan&&thirstBegan)message="A full day without food or water leaves you dangerously weak.";else if(hungerBegan)message="A full day without food leaves you weak. Eat before attrition sets in.";else if(thirstBegan)message="A full day without water leaves you weak. Drink before attrition sets in.";
}

void Game::passTurn(){
    turns++;advanceSurvival();if(turns%9==0)mana=min(maxMana,mana+1);
    advanceEnvironment();updateSchedules();discoverNearby();advanceMonsters();
}

void Game::advanceMonsters(){
    auto&m=mapNow();if(m.town)return;for(auto&z:m.monsters){if(z.hp<=0)continue;bool active=!m.outdoor||!z.nocturnal||daylight()<.35;if(!active&&z.alert==0){z.intent=Intent::Sleep;continue;}int dist=cheb(z.p,player);bool sees=dist<11&&lineOfSight(z.p,player);if(sees){z.alert=8;for(auto&a:m.monsters)if(a.hp>0&&a.faction==z.faction&&a.p.dist2(z.p)<65)a.alert=max(a.alert,4);}else if(z.alert>0)z.alert--;
        if(z.intent==Intent::Strike&&(!sees||dist>1))z.intent=Intent::Advance;if(z.intent==Intent::Shoot&&(!sees||dist<3||dist>7))z.intent=Intent::Advance;
        if(z.ranged&&sees&&dist>=3&&dist<=7){if(z.intent!=Intent::Shoot){z.intent=Intent::Shoot;message="The "+z.name+" telegraphs a ranged attack · Move, interrupt it, or press F6 to prepare a reaction.";continue;}int dmg=max(1,z.attack-totalDefense()/2);if(reaction==Reaction::Dodge&&rng()%100<55){dmg=0;message="You read the intent and dodge cleanly.";reaction=Reaction::None;}else if(reaction==Reaction::Ward){dmg/=2;reaction=Reaction::None;message="Your ward folds part of the attack away.";}hp-=dmg;if(dmg){combatText.push_back({player,"-"+to_string(dmg),{255,129,113},visualSeconds()});emitImpact(player,EffectStyle::Steel,1);playCue(SoundCue::Hurt);}if(dmg)message="The "+z.name+" strikes at range for "+to_string(dmg)+" damage.";z.intent=Intent::Guard;continue;}
        if(sees&&dist<=1){if(z.intent!=Intent::Strike){z.intent=Intent::Strike;message="The "+z.name+" draws back to strike · Move away or press F6 to prepare a reaction.";continue;}int dmg=max(1,z.attack-totalDefense());if(reaction==Reaction::Dodge&&rng()%100<55){dmg=0;message="You dodge the telegraphed blow.";reaction=Reaction::None;}else if(reaction==Reaction::Block){dmg=max(0,dmg-3-totalDefense()/2);reaction=Reaction::None;message="Your block absorbs the worst of the blow.";}hp-=dmg;if(dmg){combatText.push_back({player,"-"+to_string(dmg),{255,129,113},visualSeconds()});emitImpact(player,EffectStyle::Steel,1);playCue(SoundCue::Hurt);}if(dmg)message="The "+z.name+" hits you for "+to_string(dmg)+" damage.";z.intent=Intent::Guard;continue;}
        Point destination=(z.alert>0?player:z.home),d{};if(z.alert==0&&z.p.dist2(z.home)<=z.territory*z.territory){if(rng()%100>22){z.intent=Intent::Idle;continue;}d=DIRS[rng()%4];z.intent=Intent::Roam;}else{int dx=destination.x-z.p.x,dy=destination.y-z.p.y;d=abs(dx)>abs(dy)?Point{dx>0?1:-1,0}:Point{0,dy>0?1:-1};z.intent=z.alert?Intent::Advance:Intent::Retreat;}Point q=z.p+d;if(m.contains(q)&&m.at(q).walk&&q!=player&&none_of(m.monsters.begin(),m.monsters.end(),[&](auto&a){return &a!=&z&&a.hp>0&&a.p==q;}))z.p=q;
    }
}

void Game::attackMonster(Monster&z){int dmg=max(1,totalAttack()+int(rng()%4)-1-z.defense);z.hp-=dmg;emitImpact(z.p,EffectStyle::Steel,1);combatText.push_back({z.p,"-"+to_string(dmg),{255,213,133},visualSeconds()});playCue(SoundCue::Hit);message="You strike the "+z.name+" for "+to_string(dmg)+" damage.";if(z.hp<=0)killMonster(z);}

void Game::killMonster(Monster&z){
    if(z.rewardClaimed)return;z.rewardClaimed=true;gold+=z.boss?60:5;string material=z.faction==Faction::Goblin?"scrap":z.faction==Faction::Beast?(z.name.find("spider")!=string::npos?"venom":"hide"):"shard";materials[material]++;completeObjective("kill",z.name.find("goblin")!=string::npos?"goblin":"other");
    if(rng()%100<62){Item drop;if(z.name.find("spider")!=string::npos)drop=equipment("glassfang bracers",Slot::Hands,']',1,1);else if(z.name.find("gargoyle")!=string::npos)drop=equipment("gargoyle plate",Slot::Body,']',0,3);else if(z.name.find("lich")!=string::npos)drop=equipment("lich-light diadem",Slot::Head,']',0,1,3,.4);else if(z.name.find("demon")!=string::npos)drop=equipment("furnace-heart greatblade",Slot::MainHand,'/',6,0,2);else if(z.name.find("goblin")!=string::npos||z.name.find("kobold")!=string::npos)drop=equipment("scavenger's cleaver",Slot::MainHand,'/',1+level/2);else if(z.name.find("orc")!=string::npos||z.name.find("troll")!=string::npos)drop=equipment("notched raider axe",Slot::MainHand,'/',2+level/2);else if(z.name.find("skeleton")!=string::npos||z.name.find("cultist")!=string::npos)drop=equipment("grave-iron ritual blade",Slot::MainHand,'/',1+level/2,0,1);else drop=equipment("salvaged delver's weapon",Slot::MainHand,'/',1+level/2);mapNow().groundItems.push_back({z.p,drop});message+=" It drops the "+drop.name+".";}
    if(z.boss){string defeatedMap=mapNow().name;cleared.insert(defeatedMap);materials["relic"]++;completeObjective("clear",defeatedMap);string revelation;
        if(defeatedMap=="Hollowroot Mine")revelation="The root-crown opens around Maud Bramble's badge. Its Valeward wire goes cold, and the distant hedge bells fall silent.";
        else if(defeatedMap=="Sunken Barrow")revelation="Water drains between stolen doorstep stones. Their household marks face outward again, witnesses instead of debtors.";
        else if(defeatedMap=="Moonwell Cavern")revelation="The Wyrm collapses into ordinary reflection. The false stars go dark and the current turns back toward Reedwater.";
        else if(defeatedMap=="Valeward Undercroft")revelation="The Regent unthreads from his throne, leaving only a command with no speaker. Across the March, three buried channels release their grip.";
        else if(defeatedMap=="The Unsurveyed Sixth · Depth 6")revelation="The Warden's crown-command breaks. Four equal flames settle behind the black glass, and the Foundation Seal begins answering to no throne.";
        else revelation="The force holding this place together releases its claim.";
        lore.push_back("After "+z.name+": "+revelation);message="DUNGEON CLEARED · "+defeatedMap+" · "+revelation;
        if(cleared.contains("Valeward Undercroft")&&cleared.contains("The Unsurveyed Sixth · Depth 6")){string ending="With the Regent silenced and the Foundation Seal free, the Lantern Compact belongs to its four communities again. The blue lamps of Emberfall return to gold.";if(none_of(lore.begin(),lore.end(),[&](auto&entry){return entry.rfind("The Lantern Compact Restored:",0)==0;}))lore.push_back("The Lantern Compact Restored: "+ending);Quest restored;restored.flag="story_complete";applyQuestConsequences(restored);message="CHAPTER COMPLETE · "+ending+" Report the full account to Sable Orr.";}
    }gainXp(z.boss?80:12);
}

void Game::updateSchedules(){
    string schedule=minutes()<420?"sleeping":minutes()<1080?"working":minutes()<1260?"at supper":"sleeping";for(auto&m:maps)for(auto&n:m.npcs){n.schedule=schedule;Point p=schedule=="working"?n.work:schedule=="at supper"?n.evening:n.home;if(m.contains(p)&&m.at(p).walk)n.p=p;}
}

void Game::discoverNearby(){
    auto&m=mapNow();for(auto&l:m.landmarks)if(l.p.dist2(player)<=20&&discovered.insert(l.name).second){lore.push_back(l.name+": "+l.lore);message="LANDMARK DISCOVERED · "+l.name+". Added to the lore journal; +3 XP.";completeObjective("discover",l.name);gainXp(3);}
}

Quest* Game::questFor(const string&id){
    refreshQuestProgress();
    for(auto&q:quests)if(q.giver==id&&!q.done&&(q.prerequisite.empty()||any_of(quests.begin(),quests.end(),[&](auto&a){return a.id==q.prerequisite&&a.done;})))return &q;return nullptr;
}

vector<string> Game::conversationLines(const Npc&npc,const string&speaker,const string&text,const vector<DialogueChoice>&choices,int selected)const{
    auto[sw,sh]=consoleViewport();int width=min(180,sw-8),portraitWidth=width/2-2,imageWidth=sh<24?8:sh<28?12:sh<36?20:32;
    auto playerArt=resizePortrait(racePortrait(race),imageWidth),otherArt=resizePortrait(npcPortrait(npc),imageWidth);
    auto cell=[&](const string&label){return fitPad(centerPad(label,portraitWidth),portraitWidth);};
    string footer=sw<76?"↑↓ Choose · Enter Reply · PgUp/Dn Read · Esc Leave":"↑↓ Choose · Enter/1–9 Reply · PgUp/Dn Read · Esc Leave";
    int footerRows=int(wrapText(footer,width).size());vector<string>lines{"\4"+cell(raceName(race)+" · "+className(calling))+"    "+cell(npc.name)};
    for(size_t row=0;row<playerArt.size();row++)lines.push_back(cell(playerArt[row])+"    "+cell(otherArt[row]));
    auto speakerRows=wrapText(speaker,width-2);for(auto&part:speakerRows)lines.push_back("\4 "+part);
    auto words=wrapText("“"+text+"”",width-2);int room=max(1,sh-7-footerRows-int(lines.size())-int(choices.size())-1),pages=max(1,(int(words.size())+room-1)/room),page=clamp(dialoguePage,0,pages-1);
    for(int i=page*room;i<min(int(words.size()),(page+1)*room);i++)lines.push_back(" "+words[size_t(i)]);
    lines.push_back("\5 "+string(pages>1?"ACCOUNT "+to_string(page+1)+"/"+to_string(pages)+" · PgUp/PgDn to continue":"YOUR RESPONSE"));
    for(size_t i=0;i<choices.size();i++)lines.push_back(string(int(i)==selected?"\1\2 ◆ ":"\2   ")+to_string(i+1)+"  "+choices[i].label);
    lines.push_back("\5"+footer);return lines;
}

vector<string> Game::markLocalMap(const Npc&npc){
    auto&m=mapNow();vector<string>marked;bool townGuide=m.town;
    for(auto&landmark:m.landmarks){if(!townGuide&&landmark.p.dist2(npc.p)>=2025)continue;bool changed=flags.insert("charted_"+landmark.name).second;for(int dy=-2;dy<=2;dy++)for(int dx=-2;dx<=2;dx++){Point p=landmark.p+Point{dx,dy};if(m.contains(p)&&dx*dx+dy*dy<=5){auto index=size_t(p.y)*m.w+p.x;if(!m.explored[index]){m.explored[index]=1;changed=true;}}}if(changed){marked.push_back(landmark.name);}}
    refreshQuestProgress();return marked;
}

void Game::talk(Npc&n){
    string node="root";int selected=0;dialoguePage=0;while(true){Quest*q=questFor(n.id);string speaker=n.name+" · "+n.role,text;vector<DialogueChoice>choices;if(node=="root"){text=npcGreeting(n);if(n.schedule=="sleeping")text="I am off duty, but not beyond conversation. "+text;choices={{npcPrompt(n,"town"),"town",""},{npcPrompt(n,"work"),"work",""},{npcPrompt(n,"rumour"),"rumour",""}};if(q){string questLabel=!q->accepted?"Do you have work for me?":q->progress>=q->need?"I've completed “"+q->title+"”.":"About “"+q->title+"”…";choices.push_back({questLabel,"quest",""});}choices.push_back({"That's all. Farewell.","","leave"});}
        else if(node=="town"){text=npcTopicDialogue(n,"town",current==0);choices={{"Which places should I visit?","places",""},{current==0?"What do you know about the Delve?":"What danger lies nearby?","delve",""},{"Back to something else.","root",""}};}
        else if(node=="places"){text=npcTopicDialogue(n,"places",current==0);choices={{"Put those places on my map.","root","map"},{current==0?"And the Delve?":"And the danger you mentioned?","delve",""},{"Back.","town",""}};}
        else if(node=="delve"){text=npcTopicDialogue(n,"delve",current==0);choices={{npcPrompt(n,"rumour"),"rumour",""},{"Back.","town",""}};}
        else if(node=="work"){text=npcWorkDialogue(n);choices={{"What does this mean for you personally?","work_more",""},{"I wanted to ask something else.","root",""}};}
        else if(node=="work_more"){text=npcTopicDialogue(n,"work_more",current==0);choices={{"I understand.","root",""},{npcPrompt(n,"rumour"),"rumour",""}};}
        else if(node=="rumour"){text=npcRumourDialogue(n);choices={{"Record that testimony.","root","rumour"},{"What does that evidence mean?","rumour_more",""},{"Ask about something else.","root",""}};}
        else if(node=="rumour_more"){text=npcTopicDialogue(n,"rumour_more",current==0);choices={{"Add your conclusion to my notes.","root","conclusion"},{"Back.","root",""}};}
        else if(node=="reward"&&q){text=n.id=="mara"?"Thirty-five gold from the emergency oil fund. Bring back a clear account; Sable needs facts she can check.":n.id=="sable"?"Thirty-five gold and your account entered in the public archive without alteration. Accuracy is the rarer part of that offer.":n.id=="elin"?"Thirty-five gold. Finish the Hollowroot work and report back, and Bramblewick will give Roen its pledge.":n.id=="dara"?"Thirty-five gold. Roads are paid in stone and sweat; heroes apparently insist on coin.":n.id=="nella"?"Thirty-five gold. After Moonwell is safe, I can refill our public cisterns and send Reedwater's pledge to Roen.":n.id=="roen"?"Thirty-five gold under the old watch tariff, and Valeward's formal recognition. The latter once opened more doors than it does now.":"Thirty-five gold, entered honestly in the account. No exposure, no commemorative plaque, and no mysterious deductions.";choices={{"I'll take the work.","root","accept"},{"I need time to think.","root",""}};}
        else if(node=="quest"&&q){if(!q->accepted){text=npcQuestDialogue(n,*q,"offer");choices={{"I'll do it.","root","accept"},{"What is the reward?","reward",""},{"Not yet.","root",""}};}else if(q->progress>=q->need){text=npcQuestDialogue(n,*q,"complete");choices={{"Report the task complete.","root","complete"},{"Not just yet.","root",""}};}else{text=npcQuestDialogue(n,*q,"progress");choices={{"Remind me why this matters.","quest_why",""},{"I'll return when it is done.","root",""}};}}
        else if(node=="quest_why"&&q){text=npcTopicDialogue(n,"work_more",current==0)+" "+q->description;choices={{"Understood.","root",""},{"Repeat the objective.","quest",""}};}
        else{node="root";selected=0;continue;}
        selected=clamp(selected,0,max(0,int(choices.size())-1));
        renderPanel("DIALOGUE · "+n.name,conversationLines(n,speaker,text,choices,selected),n.color,-1,false);
        for(int i=0;i<4&&!_kbhit();i++)Sleep(8);
        if(!_kbhit())continue;
        int key=readKey();bool activate=false;
        if(key==1001||key=='w'||key=='k')selected=(selected-1+int(choices.size()))%int(choices.size());
        else if(key==1002||key=='s'||key=='j')selected=(selected+1)%int(choices.size());
        else if(key>='1'&&key<='9'&&key-'1'<int(choices.size())){selected=key-'1';activate=true;}
        else if(key==1006)dialoguePage=max(0,dialoguePage-1);
        else if(key==1008)dialoguePage=min(50,dialoguePage+1);
        else if(key==27||key=='q')return;
        else if(key==13||key==' ')activate=true;
        if(activate){
            auto choice=choices[size_t(selected)];
            if(choice.action=="leave"){message=n.name+": “"+npcFarewellDialogue(n)+"”";return;}
            if(choice.action=="accept"&&q){acceptQuest(*q);panel("JOURNAL UPDATED",{"\4 "+q->title,message,questNextStep(*q)},n.color,true);}
            else if(choice.action=="complete"&&q){turnInQuest(*q);panel("REPORT RECEIVED",{"\4 "+q->title,message},n.color,true);}
            else if(choice.action=="rumour"||choice.action=="conclusion"){recordTestimony(n,choice.action=="conclusion");panel("NOTES RECORDED",{message,"Read the account in your lore journal (F4)."},n.color,true);}
            else if(choice.action=="map"){auto marked=markLocalMap(n);message=marked.empty()?"Your map already shows those places.":"MAP UPDATED · "+to_string(marked.size())+" places charted. Visit them to gather evidence.";panel("MAP ANNOTATED",{message,"Locations are marked; unvisited quest sites still need to be reached."},n.color,true);}
            node=choice.next.empty()?"root":choice.next;selected=0;dialoguePage=0;
        }
    }
}

int Game::objectiveProgress(const Quest&q)const{
    if(q.type=="discover")return discovered.contains(q.target)?q.need:0;
    if(q.type=="clear")return cleared.contains(q.target)?q.need:0;
    if(q.type=="flags"&&q.target=="regional")return int(flags.contains("bramble_safe"))+int(flags.contains("stonecross_safe"))+int(flags.contains("reedwater_safe"));
    if(q.type=="flags"&&q.target=="ending")return int(cleared.contains("The Unsurveyed Sixth · Depth 6")||flags.contains("foundation_secured"))+int(cleared.contains("Valeward Undercroft")||flags.contains("valeward_safe"));
    if(q.type=="kill"&&q.target=="goblin"){int slain=0;for(auto&m:maps)for(auto&z:m.monsters)if(z.name.find("goblin")!=string::npos&&(z.rewardClaimed||z.hp<=0))slain++;return min(q.need,slain);}
    return q.progress;
}

void Game::refreshQuestProgress(){for(auto&q:quests)q.progress=clamp(max(q.progress,objectiveProgress(q)),0,q.need);}

void Game::completeObjective(const string&type,const string&target){
    // Progress comes from durable world evidence, including work before acceptance.
    refreshQuestProgress();
}

void Game::interact(){
    auto&m=mapNow();
    if(auto n=find_if(m.npcs.begin(),m.npcs.end(),[&](auto&a){return cheb(a.p,player)<=1;});n!=m.npcs.end()){talk(*n);return;}
    if(auto t=find_if(m.travels.begin(),m.travels.end(),[&](auto&a){return a.p==player;});t!=m.travels.end()){travel(*t);return;}
    if(auto torch=find_if(m.wallTorches.begin(),m.wallTorches.end(),[&](auto&t){return cheb(t.p,player)<=1;});torch!=m.wallTorches.end()){takeWallTorch(*torch);return;}
    if(auto supply=find_if(m.provisions.begin(),m.provisions.end(),[&](auto&s){return cheb(s.p,player)<=1;});supply!=m.provisions.end()){gatherProvision(*supply);return;}
    if(auto machine=find_if(m.machines.begin(),m.machines.end(),[&](auto&s){return cheb(s.p,player)<=1;});machine!=m.machines.end()){operateMachine(*machine);return;}
    array<Point,5>nearbyTiles{{player,player+Point{1,0},player+Point{-1,0},player+Point{0,1},player+Point{0,-1}}};
    for(auto p:nearbyTiles)if(m.contains(p)&&m.at(p).name.rfind("plaque: ",0)==0){message="THE SIGN READS: "+m.at(p).name.substr(8)+".";return;}
    for(auto p:nearbyTiles)if(m.contains(p)&&m.at(p).name.rfind("fixture: ",0)==0){message="You examine the "+m.at(p).name.substr(9)+". Press X and Enter for its full description.";return;}
    if(auto source=find_if(nearbyTiles.begin(),nearbyTiles.end(),[&](Point p){return m.contains(p)&&potableWaterTile(m.at(p));});source!=nearbyTiles.end()){
        int filled=fillEmptyFlasks(1000000);if(filled>0)message="You refill "+to_string(filled)+" flask"+(filled==1?"":"s")+" with cold running water.";else if(water<100){water=100;message="You drink directly from the water until your thirst is gone.";}else{message="The water is drinkable, but you have no empty flasks to refill.";return;}passTurn();return;
    }
    if(m.at(player).glyph=='$'){
        if(minutes()<420||minutes()>=1080){message="The shop is shuttered. Trading hours are 07:00-18:00.";return;}
        shopMenu(m.at(player).name);return;
    }
    for(auto d:DIRS){Point p=player+d;if(m.contains(p)&&m.at(p).name=="open door"){m.at(p)=Door;message="You close the door, reshaping the light and line of sight.";passTurn();return;}}
    message="There is nothing here to use.";
}

void Game::fastTravelMenu(){
    if(!mapNow().outdoor){message="Fast travel is only possible under the open sky.";return;}if(any_of(mapNow().monsters.begin(),mapNow().monsters.end(),[&](auto&z){return z.hp>0&&z.alert>0&&z.p.dist2(player)<225;})){message="Hostile pursuit makes the road unsafe.";return;}
    struct Dest{int map;Point p;string name;};vector<Dest> d;for(int mi:{0,1})for(auto&l:maps[mi].landmarks)if(discovered.contains(l.name))d.push_back({mi,l.p,l.name});vector<string> names;for(auto&a:d)names.push_back(a.name+"  ·  "+maps[a.map].name);int pick=menu("FAST TRAVEL · 1 HOUR",names);if(pick>=0){current=d[pick].map;player=d[pick].p;for(int elapsed=0;elapsed<10;elapsed++){turns++;advanceSurvival();if(turns%9==0)mana=min(maxMana,mana+1);}message="FAST TRAVEL · You follow known roads to "+d[pick].name+".";updateSchedules();}
}

void Game::craftingMenu(){
    if(!mapNow().outdoor||( !mapNow().town&&none_of(mapNow().npcs.begin(),mapNow().npcs.end(),[&](auto&n){return n.p.dist2(player)<225;}))){message="Crafting requires a settlement workbench.";return;}
    vector<string> recipes{"FIELD MEDICINE  ·  1 hide + 1 venom  →  healing potion","MARCH BOMB  ·  2 scrap  →  alchemist bomb","RELIC WARD  ·  3 shards  →  +1 base DEF","WAYFARER KIT  ·  1 hide + 1 scrap  →  2 trail rations","OATHBOUND LANTERN  ·  1 relic + 4 shards  →  stronger lantern"};string supplies="HIDE "+to_string(materials["hide"])+" · VENOM "+to_string(materials["venom"])+" · SCRAP "+to_string(materials["scrap"])+" · SHARDS "+to_string(materials["shard"])+" · RELICS "+to_string(materials["relic"]);int p=menu("SETTLEMENT WORKBENCH · "+supplies,recipes);bool ok=false;string result;if(p==0&&materials["hide"]&&materials["venom"]){materials["hide"]--;materials["venom"]--;addItem(makeItem(ItemKind::Potion));result="healing potion added to your pack";ok=true;}else if(p==1&&materials["scrap"]>=2){materials["scrap"]-=2;addItem(makeItem(ItemKind::Bomb));result="alchemist bomb added to your pack";ok=true;}else if(p==2&&materials["shard"]>=3){materials["shard"]-=3;defense++;result="relic ward fitted; base DEF increased by 1";ok=true;}else if(p==3&&materials["hide"]&&materials["scrap"]){materials["hide"]--;materials["scrap"]--;Item rations=makeItem(ItemKind::Ration);rations.quantity=2;addItem(rations);result="2 trail rations added to your pack";ok=true;}else if(p==4&&materials["relic"]&&materials["shard"]>=4){materials["relic"]--;materials["shard"]-=4;lantern=true;lanternRadius+=1;lanternIntensity+=.1;result="lantern radius and intensity increased";ok=true;}if(p>=0){message=ok?"CRAFTED · "+result+".":"MISSING MATERIALS · Review the recipe requirements and your supplies.";if(ok)passTurn();}
}

int Game::creationChoice(const string&title,const vector<string>&options,const vector<vector<string>>&details,const vector<vector<string>>&art,Color accent){
    if(options.empty())return -1;int selected=0,offset=0;double started=visualSeconds();
    while(true){
        auto content=creationPresentation(options,details[size_t(selected)],art[size_t(selected)],selected,visualSeconds()-started,offset);
        renderPanel(title,content,accent,-1,false);for(int i=0;i<4&&!_kbhit();i++)Sleep(8);if(!_kbhit())continue;int key=readKey();
        if(key==1001||key=='w'||key=='k'){selected=(selected-1+int(options.size()))%int(options.size());offset=0;started=visualSeconds();}
        else if(key==1002||key=='s'||key=='j'){selected=(selected+1)%int(options.size());offset=0;started=visualSeconds();}
        else if(key==1006)offset=max(0,offset-8);else if(key==1008)offset=min(64,offset+8);
        else if(key>='1'&&key<='9'&&key-'1'<int(options.size()))return key-'1';
        else if(key==13||key==' ')return selected;else if(key==27||key=='q')return -1;
    }
}

void Game::configureCharacter(RaceKind chosenRace,ClassKind chosenClass){
    race=chosenRace;calling=chosenClass;auto profile=characterProfile(race,calling);strength=profile.strength;dexterity=profile.dexterity;intellect=profile.intellect;constitution=profile.constitution;wisdom=profile.wisdom;attack=profile.attack;defense=profile.defense;maxMana=profile.maxMana;lanternRadius=profile.lanternRadius;lanternIntensity=profile.lanternIntensity;maxHp=profile.maxHp;hp=maxHp;mana=maxMana;inventory.clear();nextItemOrder=1;preferredInventorySort=InventorySort::Type;learnedSpells=startingSpells(calling);skills=buildSkillTree(calling);skillPoints=0;
    addItem(makeItem(ItemKind::Potion));addItem(makeItem(ItemKind::Potion));addItem(makeItem(ItemKind::Ration));addItem(makeItem(ItemKind::Ration));addItem(makeItem(ItemKind::Flask));addItem(makeItem(ItemKind::Flask));
    Item weapon=calling==ClassKind::Vanguard?equipment("militia broadsword",Slot::MainHand,'/',2):calling==ClassKind::Arcanist?equipment("apprentice emberstaff",Slot::MainHand,'/',0,0,2,.2):calling==ClassKind::Ranger?equipment("ashwood shortbow",Slot::MainHand,'}',2):equipment("dawnwatch mace",Slot::MainHand,'/',1,0,1,.25);weapon.equipped=true;addItem(weapon);
    if(calling==ClassKind::Vanguard)addItem(makeItem(ItemKind::Bomb));if(calling==ClassKind::Arcanist)addItem(makeItem(ItemKind::ManaTonic));
    message="WAYFARER READY · Mara Vey waits at the Lamplighters Guild with Emberfall's blue circuit map.";
}

void Game::createCharacterInteractive(){
    splashScreen();vector<string>raceOptions{"HUMAN · Hearthborn","DWARF · Deep-kin","ELF · Twilight Scion","ASHBORN · Ember-souled"},classOptions{"VANGUARD · Front-line warden","ARCANIST · Aether cartographer","RANGER · March stalker","LIGHTBEARER · Keeper of the dawn"};vector<vector<string>>raceDetails,raceArt;for(int i=0;i<4;i++){auto chosen=RaceKind(i);auto detail=raceLore(chosen);detail.push_back(i==0?"ATTRIBUTES  STR +1  DEX +1  INT +1  CON +1  WIS +1":i==1?"ATTRIBUTES  STR +2  DEX -1  CON +4":i==2?"ATTRIBUTES  DEX +3  INT +3  CON -1":"ATTRIBUTES  INT +2  CON +2  WIS +2  FLAME +15%");raceDetails.push_back(detail);raceArt.push_back(racePortrait(chosen));}
    int chosenRace=-1,chosenClass=-1;while(true){if(chosenRace<0){chosenRace=creationChoice("I · CHOOSE AN ANCESTRY",raceOptions,raceDetails,raceArt,{255,174,66});if(chosenRace<0)continue;}vector<vector<string>>classDetails,classArt;for(int i=0;i<4;i++){auto chosen=ClassKind(i);auto profile=characterProfile(RaceKind(chosenRace),chosen);auto detail=classLore(chosen);auto spells=startingSpells(chosen);detail.push_back("VITALS  HP "+to_string(profile.maxHp)+"  MANA "+to_string(profile.maxMana)+"  ATK "+to_string(profile.attack)+"  DEF "+to_string(profile.defense));detail.push_back("ATTRIBUTES  STR "+to_string(profile.strength)+"  DEX "+to_string(profile.dexterity)+"  INT "+to_string(profile.intellect)+"  CON "+to_string(profile.constitution)+"  WIS "+to_string(profile.wisdom));detail.push_back("EQUIPMENT  "+profile.weapon);detail.push_back("SPELLS  "+spellInfo(spells[0]).name+" · "+spellInfo(spells[1]).name);detail.push_back("TACTICS  "+profile.playstyle);classDetails.push_back(detail);classArt.push_back(classPortrait(chosen));}chosenClass=creationChoice("II · CHOOSE A CALLING · "+raceName(RaceKind(chosenRace)),classOptions,classDetails,classArt,{118,184,255});if(chosenClass<0){chosenRace=-1;continue;}auto profile=characterProfile(RaceKind(chosenRace),ClassKind(chosenClass));vector<vector<string>>confirmDetails(3),confirmArt(3,classPortrait(ClassKind(chosenClass)));vector<string>summary{raceName(RaceKind(chosenRace))+" "+className(ClassKind(chosenClass)),"VITALS  HP "+to_string(profile.maxHp)+" · MANA "+to_string(profile.maxMana)+" · ATK "+to_string(profile.attack)+" · DEF "+to_string(profile.defense),"ATTRIBUTES  STR "+to_string(profile.strength)+" · DEX "+to_string(profile.dexterity)+" · INT "+to_string(profile.intellect)+" · CON "+to_string(profile.constitution)+" · WIS "+to_string(profile.wisdom),"EQUIPMENT  "+profile.weapon,"TACTICS  "+profile.playstyle};confirmDetails[0]=confirmDetails[1]=confirmDetails[2]=summary;int confirmation=creationChoice("III · CONFIRM YOUR WAYFARER",{"BEGIN IN EMBERFALL","CHANGE CALLING","CHANGE ANCESTRY"},confirmDetails,confirmArt,{245,205,105});if(confirmation==0){configureCharacter(RaceKind(chosenRace),ClassKind(chosenClass));return;}if(confirmation==2)chosenRace=-1;chosenClass=-1;}
}

void Game::addItem(Item item,bool announce){
    if(item.quantity<=0)return;item.acquiredTurn=turns;item.acquiredOrder=nextItemOrder++;auto existing=find_if(inventory.begin(),inventory.end(),[&](const Item&carried){return sameStackIdentity(carried,item);});if(existing!=inventory.end()){existing->quantity+=item.quantity;existing->acquiredTurn=item.acquiredTurn;existing->acquiredOrder=item.acquiredOrder;}else{inventory.push_back(item);coalesceInventory();}if(announce)message="Added "+item.name+(item.quantity>1?" ×"+to_string(item.quantity):"")+" to your pack.";
}

void Game::coalesceInventory(){
    for(size_t i=0;i<inventory.size();i++)if(inventory[i].equipped&&inventory[i].quantity>1){Item remainder=inventory[i];remainder.quantity=inventory[i].quantity-1;remainder.equipped=false;inventory[i].quantity=1;inventory.push_back(remainder);}for(size_t i=0;i<inventory.size();i++){if(inventory[i].quantity<=0){inventory.erase(inventory.begin()+i--);continue;}for(size_t j=i+1;j<inventory.size();){if(!sameStackIdentity(inventory[i],inventory[j])){j++;continue;}if(inventory[j].acquiredOrder>inventory[i].acquiredOrder){inventory[i].acquiredOrder=inventory[j].acquiredOrder;inventory[i].acquiredTurn=inventory[j].acquiredTurn;}inventory[i].quantity+=inventory[j].quantity;inventory.erase(inventory.begin()+j);}}
}

int Game::fillEmptyFlasks(int limit){
    if(limit<=0)return 0;auto empty=find_if(inventory.begin(),inventory.end(),[](auto&item){return item.kind==ItemKind::EmptyFlask;});if(empty==inventory.end())return 0;size_t index=size_t(empty-inventory.begin());int filled=min(limit,inventory[index].quantity);inventory[index].quantity-=filled;if(inventory[index].quantity<=0)inventory.erase(inventory.begin()+index);Item waterFlask=makeItem(ItemKind::Flask);waterFlask.quantity=filled;addItem(waterFlask);return filled;
}

void Game::useItem(size_t index){
    if(index>=inventory.size())return;Item &item=inventory[index];if(item.equippable()){string itemName=item.name;if(item.equipped){item.equipped=false;coalesceInventory();message="You remove the "+itemName+".";}else{Slot targetSlot=item.slot;Item equippedCopy=item;equippedCopy.quantity=1;equippedCopy.equipped=true;for(auto&carried:inventory)if(carried.equipped&&carried.slot==targetSlot)carried.equipped=false;if(inventory[index].quantity>1){inventory[index].quantity--;inventory.push_back(equippedCopy);}else inventory[index].equipped=true;coalesceInventory();message="You equip the "+itemName+".";}passTurn();return;}
    bool consume=true,returnEmptyFlask=false;if(item.kind==ItemKind::Potion){if(hp==maxHp){message="You are already unhurt.";return;}hp=min(maxHp,hp+14);message="The potion seals your wounds.";}else if(item.kind==ItemKind::GreaterPotion){hp=min(maxHp,hp+30);message="The crimson elixir restores you.";}else if(item.kind==ItemKind::Ration){if(food==100){message="You are already well fed.";return;}food=100;message="You eat a dense trail ration: enough for roughly one day.";}else if(item.kind==ItemKind::Flask){if(water==100){message="You are not thirsty.";return;}water=100;returnEmptyFlask=true;message="You drink deeply and keep the empty flask.";}else if(item.kind==ItemKind::EmptyFlask){message="Refill the flask beside a river, well, spring, or cistern.";return;}else if(item.kind==ItemKind::ManaTonic){mana=min(maxMana,mana+8);message="Violet current refills your spellwork.";}else if(item.kind==ItemKind::Ambrosia){food=water=100;mana=maxMana;message="Sun-fruit restores every reserve.";}else if(item.kind==ItemKind::MightElixir){mightTurns=20;message="Dragonroot heat floods your limbs.";}else if(item.kind==ItemKind::GuardElixir){guardTurns=20;message="Your skin hardens like iron.";}else if(item.kind==ItemKind::FocusElixir){focusTurns=20;message="Every mote snaps into focus.";}else if(item.kind==ItemKind::Waystone){player=mapNow().up.x>=0?mapNow().up:mapNow().start;message="The waystone collapses distance.";}else if(item.kind==ItemKind::WallTorch){mountWallTorch(index);return;}else if(item.kind==ItemKind::SmokeBomb){for(int y=-3;y<=3;y++)for(int x=-3;x<=3;x++){Point p=player+Point{x,y};if(mapNow().contains(p)&&cheb(p,player)<=3)mapNow().element(p).smoke=12;}message="Silver smoke floods the passage.";}else if(item.kind==ItemKind::Bomb){auto target=targetCursor("THROW BOMB",10);if(!target){message="Bomb throw cancelled.";return;}for(auto&z:mapNow().monsters)if(z.hp>0&&z.p.dist2(*target)<=9){z.hp-=max(2,14+strength/2-z.defense);if(z.hp<=0)killMonster(z);}for(int y=-4;y<=4;y++)for(int x=-4;x<=4;x++){Point p=*target+Point{x,y};if(p.dist2(*target)<=16)damageTerrain(mapNow(),p,32);}emitImpact(*target,EffectStyle::Fire,3);playCue(SoundCue::Fire);message="The bomb ruptures stone in a boiling sphere of fire!";}
    if(consume){item.quantity--;if(item.quantity<=0)inventory.erase(inventory.begin()+index);}passTurn();
    if(returnEmptyFlask)addItem(makeItem(ItemKind::EmptyFlask));
}

vector<size_t> Game::inventoryOrder(InventorySort sort)const{
    vector<size_t> order(inventory.size());for(size_t i=0;i<order.size();i++)order[i]=i;
    stable_sort(order.begin(),order.end(),[&](size_t a,size_t b){auto&A=inventory[a];auto&B=inventory[b];if(sort==InventorySort::Power){if(itemPower(A)!=itemPower(B))return itemPower(A)>itemPower(B);if(A.equipped!=B.equipped)return A.equipped>B.equipped;}else if(sort==InventorySort::Newest){if(A.acquiredOrder!=B.acquiredOrder)return A.acquiredOrder>B.acquiredOrder;}else if(sort==InventorySort::Name){if(A.name!=B.name)return A.name<B.name;}else{string ac=itemCategory(A),bc=itemCategory(B);if(ac!=bc)return ac<bc;if(A.equippable()!=B.equippable())return A.equippable()>B.equippable();if(A.slot!=B.slot)return int(A.slot)<int(B.slot);}if(A.name!=B.name)return A.name<B.name;return A.acquiredOrder>B.acquiredOrder;});return order;
}

#include "InventoryView.hpp"
#include "Shops.hpp"

void Game::inventoryMenu(){
    coalesceInventory();if(inventory.empty()){panel("PACK & EQUIPMENT",inventoryLines(0,true,0,preferredInventorySort),{110,205,170},true);return;}auto[sw,sh]=consoleViewport();bool compact=sw<104||sh<34;int selected=0;InventorySort sort=preferredInventorySort;double opened=visualSeconds();while(true){auto order=inventoryOrder(sort);selected=clamp(selected,0,int(order.size())-1);size_t actual=order[size_t(selected)];auto accent=inventory[actual].color;renderPanel("PACK & EQUIPMENT · SORTED BY "+inventorySortName(sort),inventoryLines(selected,compact,visualSeconds()-opened,sort),accent,-1,compact);for(int i=0;i<4&&!_kbhit();i++)Sleep(8);if(!_kbhit())continue;int k=readKey();if(k==1001||k=='w'||k=='k')selected=(selected-1+int(order.size()))%int(order.size());else if(k==1002||k=='s'||k=='j')selected=(selected+1)%int(order.size());else if(k==9||(k>='1'&&k<='4')){size_t anchored=actual;sort=k==9?InventorySort((int(sort)+1)%4):InventorySort(k-'1');preferredInventorySort=sort;auto reordered=inventoryOrder(sort);selected=int(find(reordered.begin(),reordered.end(),anchored)-reordered.begin());opened=visualSeconds();}else if(k==13||k==' '){useItem(actual);return;}else if(k=='d'){Item dropped=inventory[actual];dropped.quantity=1;dropped.equipped=false;mapNow().groundItems.push_back({player,dropped});message="You drop one "+dropped.name+".";if(inventory[actual].quantity<=1)inventory.erase(inventory.begin()+actual);else inventory[actual].quantity--;passTurn();if(inventory.empty())return;selected=min(selected,int(inventory.size())-1);}else if(k==27||k=='i'||k=='q')return;}
}

#include "DossierViews.hpp"
void Game::characterMenu(){
    int selected=0;double started=visualSeconds();while(true){renderPanel("CHARACTER & LOADOUT",characterLines(selected,visualSeconds()-started),{226,194,105});
        for(int i=0;i<4&&!_kbhit();i++)Sleep(8);if(!_kbhit())continue;int key=readKey(),previous=selected;
        if(key==1001||key==1003||key=='w'||key=='a'||key=='k'||key=='h')selected=(selected+7)%8;
        else if(key==1002||key==1004||key=='s'||key=='d'||key=='j'||key=='l')selected=(selected+1)%8;
        else if(key==13||key==' '){auto chosen=find_if(inventory.begin(),inventory.end(),[&](auto&i){return i.equipped&&i.slot==loadoutSlots[size_t(selected)];});if(chosen!=inventory.end()){message="You remove the "+chosen->name+".";chosen->equipped=false;coalesceInventory();passTurn();}}
        else if(key=='i')inventoryMenu();else if(key==27||key=='c'||key=='q')return;if(previous!=selected)started=visualSeconds();
    }
}

optional<Point> Game::targetCursor(const string& title,int range){
    auto&m=mapNow();auto nearest=min_element(m.monsters.begin(),m.monsters.end(),[&](auto&a,auto&b){return a.p.dist2(player)<b.p.dist2(player);});cursor=nearest!=m.monsters.end()&&nearest->hp>0?nearest->p:player;message=title+" · Move cursor · Tab cycles monsters · Enter confirms · Esc cancels";size_t cycle=0;
    while(true){draw();for(int i=0;i<4&&!_kbhit();i++)Sleep(8);if(!_kbhit())continue;int k=readKey();if(auto d=keyDirection(k)){Point q=*cursor+*d;if(m.contains(q)&&cheb(player,q)<=range)cursor=q;}else if(k==9){vector<Point> seen;for(auto&z:m.monsters)if(z.hp>0&&lineOfSight(player,z.p)&&cheb(player,z.p)<=range)seen.push_back(z.p);if(!seen.empty()){cursor=seen[cycle%seen.size()];cycle++;}}else if(k==13||k==' '){Point result=*cursor;cursor.reset();return result;}else if(k==27||k=='q'){cursor.reset();return nullopt;}}
}

void Game::castSpell(SpellKind spell,Point target){
    auto info=spellInfo(spell);int cost=spellCost(spell),bonus=spellInvestment(spell)*2+totalSpellPower();auto&m=mapNow();
    if(mana<cost){message="You need "+to_string(cost)+" mana for "+info.name+".";return;}
    if(!m.contains(target)||(info.range>0&&cheb(player,target)>info.range)){message="That target is beyond range.";return;}
    if(info.range>0&&!lineOfSight(player,target)&&spell!=SpellKind::Stonebreak&&spell!=SpellKind::SeismicWave){message="No clear line of effect reaches that target.";return;}
    if(spell==SpellKind::Blink&&(!m.at(target).walk||any_of(m.monsters.begin(),m.monsters.end(),[&](auto&z){return z.hp>0&&z.p==target;})||any_of(m.npcs.begin(),m.npcs.end(),[&](auto&n){return n.p==target;}))){message="Blink needs an unoccupied floor tile.";return;}
    mana-=cost;double now=visualSeconds();EffectStyle style=spellStyle(spell);Color colour=effectColour(style);
    playCue(style==EffectStyle::Fire?SoundCue::Fire:style==EffectStyle::Frost?SoundCue::Frost:style==EffectStyle::Lightning?SoundCue::Lightning:style==EffectStyle::Healing||style==EffectStyle::Radiant?SoundCue::Heal:SoundCue::Arcane);
    if(spell==SpellKind::HealingLight||spell==SpellKind::Sanctuary){int heal=min(maxHp-hp,(spell==SpellKind::HealingLight?12+wisdom/2:8+wisdom/3)+bonus);hp+=heal;if(spell==SpellKind::Sanctuary)wardTurns=12+bonus/2;emitImpact(player,EffectStyle::Healing,3);combatText.push_back({player,"+"+to_string(heal),colour,now});message=info.name+" restores "+to_string(heal)+" HP"+(spell==SpellKind::Sanctuary?" and grants a ward.":".");}
    else if(spell==SpellKind::ShieldFlare){wardTurns=7+bonus/2;emitImpact(player,EffectStyle::Radiant,2);message="Shield Flare · +4 defence for "+to_string(wardTurns)+" turns.";}
    else if(spell==SpellKind::BattleCry){mightTurns=guardTurns=10+bonus/2;emitImpact(player,EffectStyle::Stone,3);message="Battle Cry · +4 attack and defence for "+to_string(mightTurns)+" turns.";}
    else if(spell==SpellKind::Blink){emitImpact(player,EffectStyle::Arcane,2);player=target;emitImpact(player,EffectStyle::Arcane,2,.1);message="Blink folds the distance into a violet seam.";}
    else{
        int radius=spell==SpellKind::Meteor?4:spell==SpellKind::GroundQuake||spell==SpellKind::IceStorm||spell==SpellKind::PoisonCloud?3:spell==SpellKind::Fireball||spell==SpellKind::LuminousNova||spell==SpellKind::Smite?2:0;
        vector<Point> path;int dx=target.x-player.x,dy=target.y-player.y,steps=max(abs(dx),abs(dy));for(int i=1;i<=steps;i++)path.push_back({player.x+int(lround(dx*i/double(steps))),player.y+int(lround(dy*i/double(steps)))});
        bool travelling=!path.empty()&&(radius==0||spell==SpellKind::Fireball||spell==SpellKind::Meteor);double flight=travelling?clamp(path.size()*.032,.14,.48):0;
        if(travelling)effects.push_back({path,target,{255,250,235},colour,now,flight,max(1,radius),true,style});
        emitImpact(target,style,max(1,radius),flight);if(flight>0)scheduleCue(style==EffectStyle::Fire||style==EffectStyle::Stone?SoundCue::Fire:style==EffectStyle::Frost?SoundCue::Frost:style==EffectStyle::Lightning?SoundCue::Lightning:style==EffectStyle::Steel?SoundCue::Hit:SoundCue::Arcane,flight);
        int base=8+bonus+(spell==SpellKind::Meteor?12:spell==SpellKind::Smite?10:0),hitCount=0,totalDamage=0;string bossReport;
        for(auto&z:m.monsters)if(z.hp>0&&((radius&&z.p.dist2(target)<=radius*radius)||((!radius||spell==SpellKind::Fireball)&&find(path.begin(),path.end(),z.p)!=path.end()))){
            int damage=max(2,base-z.defense);z.hp-=damage;hitCount++;totalDamage+=damage;combatText.push_back({z.p,"-"+to_string(damage),colour,now+flight});if(style==EffectStyle::Frost)z.intent=Intent::Guard;if(z.hp<=0){killMonster(z);if(z.boss)bossReport=message;}
        }
        if(spell==SpellKind::Stonebreak||spell==SpellKind::SeismicWave||spell==SpellKind::GroundQuake||spell==SpellKind::Meteor||spell==SpellKind::Fireball)for(int y=-radius-1;y<=radius+1;y++)for(int x=-radius-1;x<=radius+1;x++)damageTerrain(m,target+Point{x,y},spell==SpellKind::Meteor?30:spell==SpellKind::GroundQuake?24:14);
        for(int y=-radius;y<=radius;y++)for(int x=-radius;x<=radius;x++){Point p=target+Point{x,y};if(!m.contains(p)||!m.at(p).walk||p.dist2(target)>radius*radius)continue;if(style==EffectStyle::Fire)m.element(p).fire=8;else if(style==EffectStyle::Frost)m.element(p).frost=8;else if(spell==SpellKind::PoisonCloud){m.element(p).poison=12;m.element(p).smoke=12;}}
        message=info.name+" · "+to_string(hitCount)+" hit · "+to_string(totalDamage)+" damage"+(radius?" · blast radius "+to_string(radius):"")+".";
        if(!bossReport.empty())message+=" "+bossReport;
    }
    passTurn();
}

void Game::spellbookMenu(){
    if(learnedSpells.empty()){panel("SPELLBOOK",spellbookLines(0,0),{166,125,238},false);return;}int selected=0;double started=visualSeconds();
    while(true){renderPanel("SPELLBOOK · MANA "+to_string(mana)+"/"+to_string(maxMana),spellbookLines(selected,visualSeconds()-started),{166,125,238});
        for(int i=0;i<4&&!_kbhit();i++)Sleep(8);if(!_kbhit())continue;int key=readKey(),previous=selected;bool cast=false;
        if(key==1001||key=='w'||key=='k')selected=(selected+int(learnedSpells.size())-1)%int(learnedSpells.size());
        else if(key==1002||key=='s'||key=='j')selected=(selected+1)%int(learnedSpells.size());
        else if(key>='1'&&key<='9'&&key-'1'<int(learnedSpells.size())){selected=key-'1';cast=true;}
        else if(key==13||key==' ')cast=true;else if(key==27||key=='z'||key=='q')return;if(previous!=selected)started=visualSeconds();
        if(cast){auto spell=learnedSpells[size_t(selected)];auto info=spellInfo(spell);Point target=player;if(info.range>0){auto point=targetCursor("TARGET · "+info.name,info.range);if(!point){message="Casting cancelled.";continue;}target=*point;}castSpell(spell,target);return;}
    }
}

static Color skillBranchColor(int branch){
    static constexpr array<Color,4> palette{{{225,178,92},{105,178,228},{244,105,48},{180,132,232}}};
    return palette[size_t(clamp(branch,0,3))];
}

static string skillDiscipline(const SkillNode&node){
    switch(node.reward){
        case SkillNode::Reward::Might:return"OFFENCE";
        case SkillNode::Reward::Guard:return"DEFENCE";
        case SkillNode::Reward::Vitality:return"VITALITY";
        case SkillNode::Reward::Arcana:return"ARCANA";
        case SkillNode::Reward::Radiance:return"RADIANCE";
        default:return"SPELL";
    }
}

static string skillRankPips(const SkillNode&node){
    string result;for(int rank=0;rank<node.maxRank;rank++)result+=rank<node.rank?"●":"○";return result;
}

vector<string> Game::skillTreeLines(int selected,bool compact,double marqueeElapsed)const{
    selected=clamp(selected,0,int(skills.size())-1);int branch=skills[selected].branchIndex;vector<string>lines;auto&node=skills[selected];
    auto[screenWidth,screenHeight]=consoleViewport();int cellWidth=max(25,min(43,(min(180,screenWidth-8)-3)/4)),treeWidth=cellWidth*4+3,nameWidth=cellWidth-10;
    auto prerequisiteMet=[&](const SkillNode&entry){return entry.prerequisite.empty()||any_of(skills.begin(),skills.end(),[&](auto&parent){return parent.id==entry.prerequisite&&parent.purchased();});};
    auto structurallyReady=[&](const SkillNode&entry){return !entry.maxed()&&level>=entry.requiredLevel&&prerequisiteMet(entry);};
    auto label=[&](const SkillNode&entry,int index,int width){return index==selected&&utf8Width(entry.name)>width?marqueeText(entry.name,width,marqueeElapsed):fitPad(entry.name,width);};
    auto investedInBranch=[&](int value){int invested=0;for(auto&entry:skills)if(entry.branchIndex==value)invested+=entry.rank;return invested;};
    int invested=0,nextGate=0;for(auto&entry:skills){invested+=entry.rank;if(entry.requiredLevel>level&&(nextGate==0||entry.requiredLevel<nextGate))nextGate=entry.requiredLevel;}
    const string reset="\x1b[0m\x1b[48;2;7;8;13m\x1b[38;2;205;207;214m";
    auto tint=[&](string text,Color color,bool bold=false){ostringstream out;out<<"\x1b["<<(bold?"1;":"")<<"38;2;"<<color.r<<';'<<color.g<<';'<<color.b<<'m'<<text<<reset;return out.str();};
    auto selectedCell=[&](string text,Color color){ostringstream out;out<<"\x1b[1m\x1b[48;2;"<<max(7,color.r/7)<<';'<<max(7,color.g/7)<<';'<<max(10,color.b/7)<<"m\x1b[38;2;252;247;232m"<<text<<reset;return out.str();};
    auto statusGlyph=[&](const SkillNode&entry){return entry.maxed()?string("◆"):entry.purchased()?string("●"):structurallyReady(entry)?string("◇"):string("×");};
    auto statusColor=[&](const SkillNode&entry){return entry.maxed()?Color{255,224,118}:entry.purchased()?skillBranchColor(entry.branchIndex):structurallyReady(entry)?Color{218,220,205}:Color{91,95,108};};
    auto divider=[&](int width){string result;for(int column=0;column<width;column++)result+="─";return result;};
    string gate=nextGate?"NEXT GATE LV "+to_string(nextGate):"ALL TIERS OPEN";

    if(compact){
        lines.push_back(" "+className(calling)+" PATHS · "+to_string(invested)+"/72 RANKS · "+to_string(skillPoints)+" POINT"+(skillPoints==1?"":"S"));
        string tabs=" ";for(int b=0;b<4;b++){string name=fitPad(skills[size_t(b*6)].branch,8);tabs+=(b==branch?tint("["+name+"]",skillBranchColor(b),true):" "+name+" ")+(b<3?" ":"");}lines.push_back(tabs);
        lines.push_back(screenHeight<24?" ◆ Maxed · ● Invested · ◇ Open · × Locked":" TREE KEY  ◆ MASTERED  ● INVESTED  ◇ AVAILABLE  × LOCKED");
        int firstTier=screenHeight<24?clamp(node.tier-1,0,3):0,lastTier=screenHeight<24?firstTier+3:6;
        for(int tier=firstTier;tier<lastTier;tier++){
            int index=branch*6+tier;auto&entry=skills[size_t(index)];string arm=tier==0?"╭─":tier==5?"╰─":"├─";
            string row=" "+arm+" "+statusGlyph(entry)+" T"+to_string(tier+1)+" "+label(entry,index,20)+" "+skillRankPips(entry)+"  LV"+to_string(entry.requiredLevel)+"  "+skillDiscipline(entry);
            lines.push_back(string(index==selected?"\1":"")+row);
        }
        if(screenHeight>=24)lines.push_back(" "+divider(48));
        lines.push_back(screenHeight<24?" "+node.name+" · "+skillRankPips(node):" "+node.name+" · "+node.branch+" / TIER "+to_string(node.tier+1)+" · RANK "+skillRankPips(node));
        for(auto&part:wrapText("EFFECT  "+node.description,48))lines.push_back(" "+part);
    }else{
        lines.push_back(" "+className(calling)+" DISCIPLINES   "+richBar(invested,72,12)+"  "+to_string(invested)+"/72 RANKS   "+to_string(skillPoints)+" POINT"+(skillPoints==1?"":"S")+" BANKED   "+gate);
        lines.push_back(" TREE KEY  "+tint("◆",{255,224,118},true)+" MASTERED   "+tint("●",skillBranchColor(branch),true)+" INVESTED   ◇ AVAILABLE   "+tint("×",{91,95,108})+" LOCKED");
        auto plaque=[&](string text){int fill=max(0,cellWidth-2-utf8Width(text));return divider(fill/2)+" "+text+" "+divider(fill-fill/2);};
        string headers;for(int b=0;b<4;b++){headers+=tint(plaque(skills[size_t(b*6)].branch+" · "+to_string(investedInBranch(b))+"/18"),skillBranchColor(b),b==branch);if(b<3)headers+=' ';}lines.push_back(headers);
        for(int tier=0;tier<6;tier++){
            string names,metadata;
            for(int b=0;b<4;b++){
                int index=b*6+tier;auto&entry=skills[size_t(index)];Color color=statusColor(entry);
                string nameCell="│ "+statusGlyph(entry)+" "+label(entry,index,nameWidth)+" "+skillRankPips(entry)+" │";
                string metaCell="╰ "+fitPad("T"+to_string(tier+1)+" · LV"+to_string(entry.requiredLevel)+" · "+skillDiscipline(entry),cellWidth-4)+" ╯";
                names+=(index==selected?selectedCell(nameCell,skillBranchColor(b)):tint(nameCell,color,entry.purchased()));
                metadata+=(index==selected?selectedCell(metaCell,skillBranchColor(b)):tint(metaCell,color,false));
                if(b<3){names+=' ';metadata+=' ';}
            }
            lines.push_back(names);lines.push_back(metadata);
            if(tier<5){string connectors;for(int b=0;b<4;b++){auto&parent=skills[size_t(b*6+tier)];string stem=fitPad(centerPad(parent.purchased()?"┃":"┊",cellWidth),cellWidth);connectors+=tint(stem,parent.purchased()?skillBranchColor(b):Color{68,72,84});if(b<3)connectors+=' ';}lines.push_back(connectors);}
        }
        lines.push_back(" "+divider(treeWidth));
        lines.push_back(" SELECTED NODE  "+tint(node.name,skillBranchColor(branch),true)+"  ·  "+node.branch+" / TIER "+to_string(node.tier+1)+"  ·  "+skillDiscipline(node)+"  ·  RANK "+skillRankPips(node));
        for(auto&part:wrapText("EFFECT PER RANK  "+node.description,treeWidth-3))lines.push_back(" "+part);
        string parent="ROOT",child="CAPSTONE";if(!node.prerequisite.empty())if(auto found=find_if(skills.begin(),skills.end(),[&](auto&entry){return entry.id==node.prerequisite;});found!=skills.end())parent=found->name;if(node.tier<5)child=skills[size_t(branch*6+node.tier+1)].name;
        lines.push_back(" PATH  "+parent+"  →  "+node.name+"  →  "+child);
    }
    bool prerequisite=prerequisiteMet(node);string status=node.maxed()?"MASTERED":skillPoints<=0?"NO SKILL POINTS":level<node.requiredLevel?"REQUIRES LEVEL "+to_string(node.requiredLevel):!prerequisite?"INVEST IN THE CONNECTED PARENT":"READY TO INVEST";
    string spellNote;if(node.spell){bool learned=find(learnedSpells.begin(),learnedSpells.end(),*node.spell)!=learnedSpells.end();spellNote=" · "+string(learned?"SPELL LEARNED":"UNLEARNED SPELL")+" · MASTERY +"+to_string(node.rank*2);}
    lines.push_back(" STATUS  "+tint(status,status=="READY TO INVEST"?skillBranchColor(branch):status=="MASTERED"?Color{255,224,118}:Color{150,154,166},true)+spellNote);
    lines.push_back(screenHeight<24?"\5 ↑↓ Tier · ←→ Branch · Enter Invest · Esc Close":"\5 ↑↓ Tier · ←→ Branch · 1–4 Jump Branch · Enter Invest · T/Esc Close");
    return lines;
}

void Game::skillTreeMenu(){
    if(skills.empty())return;
    auto[screenWidth,screenHeight]=consoleViewport();bool compact=screenWidth<116||screenHeight<34;int selected=0;string marqueeId;double marqueeStarted=visualSeconds();
    while(true){
        if(marqueeId!=skills[selected].id){marqueeId=skills[selected].id;marqueeStarted=visualSeconds();}
        int branch=skills[selected].branchIndex,tier=skills[selected].tier;auto lines=skillTreeLines(selected,compact,visualSeconds()-marqueeStarted);auto&node=skills[selected];
        bool prerequisite=node.prerequisite.empty()||any_of(skills.begin(),skills.end(),[&](auto&parent){return parent.id==node.prerequisite&&parent.purchased();});
        renderPanel("SKILL CONSTELLATION · "+className(calling)+" · LEVEL "+to_string(level)+" · "+to_string(skillPoints)+" POINT"+(skillPoints==1?"":"S"),lines,skillBranchColor(branch),-1,compact);
        for(int i=0;i<4&&!_kbhit();i++)Sleep(8);if(!_kbhit())continue;int key=readKey();
        if(key==27||key=='t'||key=='q')return;
        if(key==1001||key=='w'||key=='k')tier=max(0,tier-1);
        else if(key==1002||key=='s'||key=='j')tier=min(5,tier+1);
        else if(key==1003||key=='a'||key=='h')branch=(branch+3)%4;
        else if(key==1004||key=='d'||key=='l')branch=(branch+1)%4;
        else if(key>='1'&&key<='4')branch=key-'1';
        else if(key==13||key==' '){
            if(node.maxed())message="That skill is already at maximum rank.";
            else if(skillPoints<=0)message="You have no skill points.";
            else if(level<node.requiredLevel)message="That tier requires level "+to_string(node.requiredLevel)+".";
            else if(!prerequisite)message="Invest in the connected parent first.";
            else{
                skillPoints--;node.rank++;
                if(node.reward==SkillNode::Reward::Vitality){maxHp+=4;constitution++;}
                else if(node.reward==SkillNode::Reward::Might){attack++;strength++;}
                else if(node.reward==SkillNode::Reward::Guard)defense++;
                else if(node.reward==SkillNode::Reward::Arcana){maxMana+=3;intellect++;}
                else if(node.reward==SkillNode::Reward::Radiance){spellPower++;lanternRadius+=.25;}
                else if(node.spell&&find(learnedSpells.begin(),learnedSpells.end(),*node.spell)==learnedSpells.end())learnedSpells.push_back(*node.spell);
                hp=maxHp;mana=maxMana;effects.push_back({{},player,{255,255,255},{125,155,255},visualSeconds(),1.15,7,false});message=node.name+" reaches rank "+to_string(node.rank)+".";
            }
            continue;
        }else continue;
        selected=branch*6+tier;
    }
}
void Game::loreMenu(){
    if(lore.empty()){panel("LORE JOURNAL",loreLines(0,0),{118,176,218});return;}int selected=0;double started=visualSeconds();
    while(true){renderPanel("LORE JOURNAL",loreLines(selected,visualSeconds()-started),{118,176,218});
        for(int i=0;i<4&&!_kbhit();i++)Sleep(8);if(!_kbhit())continue;int key=readKey(),previous=selected;
        if(key==1001||key=='w'||key=='k')selected=(selected+int(lore.size())-1)%int(lore.size());else if(key==1002||key=='s'||key=='j')selected=(selected+1)%int(lore.size());
        else if(key==13)panel("FULL ACCOUNT",{lore[size_t(selected)]},{118,176,218});else if(key==27||key=='l'||key=='q'||key==2004)return;if(previous!=selected)started=visualSeconds();
    }
}

// Journal presentation and quest transactions.
#include "Journal.hpp"

void Game::mapMenu(){
    auto&m=mapNow();auto[sw,sh]=consoleViewport();int cols=min(128,sw-12),rows=min(29,max(12,sh-13));vector<string> lines;for(int oy=0;oy<rows;oy++){string row;for(int ox=0;ox<cols;ox++){int x=ox*m.w/cols,y=oy*m.h/rows;Point p{x,y};char g=' ';if(p==player)g='@';else if(m.explored[size_t(y)*m.w+x]){g=m.at(p).walk?'.':'#';if(any_of(m.landmarks.begin(),m.landmarks.end(),[&](auto&l){return l.p.dist2(p)<4&&(discovered.contains(l.name)||flags.contains("charted_"+l.name));}))g='*';if(any_of(m.travels.begin(),m.travels.end(),[&](auto&t){return t.p.dist2(p)<4;}))g='=';if(any_of(m.provisions.begin(),m.provisions.end(),[&](auto&s){return s.charges>0&&s.p.dist2(p)<4;}))g='%';if(any_of(m.wallTorches.begin(),m.wallTorches.end(),[&](auto&t){return t.lit&&t.p.dist2(p)<4;}))g='i';if(any_of(m.machines.begin(),m.machines.end(),[&](auto&x){return x.p.dist2(p)<4;}))g='&';if(any_of(m.groundItems.begin(),m.groundItems.end(),[&](auto&i){return i.p.dist2(p)<4;}))g='!';if(any_of(m.traps.begin(),m.traps.end(),[&](auto&t){return t.revealed&&t.p.dist2(p)<4;}))g='^';}row+=g;}lines.push_back(centerPad(row,min(180,sw-8)));}lines.push_back("");lines.push_back("\5 @ YOU   * LANDMARK   = TRAVEL   % PROVISIONS   i TORCH   & MACHINE");lines.push_back("\5 ! ITEM  ^ TRAP       # OBSTACLE  . TRAVERSABLE   BLANK UNEXPLORED");lines.push_back("\5 Press any key to return.");panel("EXPLORATION MAP · "+m.name,lines,{105,175,225});
}

bool Game::damageTerrain(MapData&m,Point p,int damage){if(!m.contains(p)||m.at(p).toughness<=0)return false;m.at(p).toughness-=damage;if(m.at(p).toughness>0){m.at(p).glyph=';';return false;}m.at(p)=Rubble;m.lights.erase(remove_if(m.lights.begin(),m.lights.end(),[&](auto&l){return l.p==p;}),m.lights.end());m.wallTorches.erase(remove_if(m.wallTorches.begin(),m.wallTorches.end(),[&](auto&t){return t.p==p;}),m.wallTorches.end());return true;}

void Game::mountWallTorch(size_t index){if(index>=inventory.size())return;message="Choose an adjacent bare wall for the torch (arrow/WASD/numpad, Esc cancels).";draw();int k=readKey();auto direction=keyDirection(k,false);if(!direction){message="Torch placement cancelled.";return;}Point p=player+*direction;auto&m=mapNow();if(!m.contains(p)||m.at(p).walk||any_of(m.wallTorches.begin(),m.wallTorches.end(),[&](auto&t){return t.p==p;})){message="A wall torch needs a bare adjacent wall.";return;}m.wallTorches.push_back({p,m.at(p),true});m.at(p)=WallTorchTile;m.lights.push_back({p,{255,103,25},9,1.3,.2,double(p.x+p.y)});inventory[index].quantity--;if(inventory[index].quantity<=0)inventory.erase(inventory.begin()+index);message="You mount and light the wall torch.";passTurn();}

void Game::takeWallTorch(WallTorchFeature&torch){auto&m=mapNow();Point p=torch.p;Tile underlying=torch.underlying;m.lights.erase(remove_if(m.lights.begin(),m.lights.end(),[&](auto&l){return l.p==p;}),m.lights.end());m.at(p)=underlying;m.wallTorches.erase(remove_if(m.wallTorches.begin(),m.wallTorches.end(),[&](auto&t){return t.p==p;}),m.wallTorches.end());addItem(makeItem(ItemKind::WallTorch));message="You lift the wall torch from its bracket and stow it.";passTurn();}

void Game::gatherProvision(ProvisionSource&source){
    if(source.charges<=0){message="The "+source.name+" has been picked clean.";return;}
    if(source.kind==ProvisionKind::Food){source.charges--;addItem(makeItem(ItemKind::Ration));message="You recover a full day's trail ration from the "+source.name+".";}
    else{int filled=fillEmptyFlasks(source.charges);if(filled>0){source.charges-=filled;message="You refill "+to_string(filled)+" flask"+(filled==1?"":"s")+" at the "+source.name+".";}else if(water<100){source.charges--;water=100;message="You drink from the "+source.name+" until your thirst is gone.";}else{message="You need an empty flask before drawing more water from the "+source.name+".";return;}}
    if(source.charges==0)mapNow().at(source.p)=EmptySupply;passTurn();
}

void Game::triggerTrap(Point p){auto&m=mapNow();auto it=find_if(m.traps.begin(),m.traps.end(),[&](auto&t){return t.p==p&&t.armed;});if(it==m.traps.end())return;it->revealed=true;it->armed=false;if(it->kind==TrapKind::FlameJet){hp-=7;m.element(p).fire=8;message="A hidden nozzle erupts in flame!";}else if(it->kind==TrapKind::ShockPlate){hp-=6;m.element(p).electricity=4;message="Blue current snaps through the plate!";}else if(it->kind==TrapKind::PoisonVent){hp-=3;m.element(p).poison=12;message="A poison vent sighs beneath your boot.";}else if(it->kind==TrapKind::Collapse){hp-=8;for(auto d:DIRS)damageTerrain(m,p+d,9);message="The ceiling collapses in a crushing roar!";}else{for(auto&z:m.monsters)z.alert=max(z.alert,10);message="An alarm rune screams through the floor.";}}

void Game::operateMachine(MachineFeature&machine){machine.active=!machine.active;auto&m=mapNow();if(machine.kind==MachineKind::Brazier){if(machine.active){m.lights.push_back({machine.p,{255,78,18},9,1.2,.18,double(machine.p.x)});m.element(machine.p).fire=8;}else m.lights.erase(remove_if(m.lights.begin(),m.lights.end(),[&](auto&l){return l.p==machine.p;}),m.lights.end());message=machine.active?"The brazier catches, throwing predatory shadows.":"You smother the brazier.";}else if(machine.kind==MachineKind::Floodgate){for(int y=-5;y<=5;y++)for(int x=-5;x<=5;x++){Point p=machine.p+Point{x,y};if(m.contains(p)&&m.at(p).walk)m.element(p).wet=machine.active?60:0;}message=machine.active?"Black water spreads from the floodgate.":"Hidden drains drink the water.";}else if(machine.kind==MachineKind::Prism){if(machine.active)m.lights.push_back({machine.p,{105,155,255},11,1.35,.04,2});else m.lights.erase(remove_if(m.lights.begin(),m.lights.end(),[&](auto&l){return l.p==machine.p;}),m.lights.end());message=machine.active?"The prism splits a charged rainbow.":"The prism folds into dull glass.";}else{for(auto&z:m.monsters)z.alert=max(0,z.alert-2);message=machine.active?"The generator wakes with an iron hymn.":"The generator winds down.";}passTurn();}

void Game::advanceEnvironment(){auto&m=mapNow();for(int y=0;y<m.h;y++)for(int x=0;x<m.w;x++){Point p{x,y};auto&e=m.element(p);if(e.fire>0){e.fire--;e.smoke=max(e.smoke,3);if(e.wet)e.fire=0;if(player==p)hp-=3;for(auto&z:m.monsters)if(z.hp>0&&z.p==p){z.hp-=3;if(z.hp<=0)killMonster(z);}}if(e.electricity>0){e.electricity--;if(player==p)hp-=e.wet?6:3;}if(e.poison>0){e.poison--;if(player==p)hp-=2;}if(e.smoke>0)e.smoke--;if(e.web>0)e.web--;if(e.frost>0)e.frost--;if(e.oil>0&&e.fire>0)e.fire=max(e.fire,8);}if(wardTurns)wardTurns--;if(mightTurns)mightTurns--;if(guardTurns)guardTurns--;if(focusTurns)focusTurns--;}

void Game::rest(){if(any_of(mapNow().monsters.begin(),mapNow().monsters.end(),[&](auto&z){return z.hp>0&&z.alert>0&&z.p.dist2(player)<=144;})){message="You cannot rest while hostile eyes hunt you.";return;}int healed=0,magic=0;for(int i=0;i<6&&hp>0;i++){int h=min(1+wisdom/8,maxHp-hp),m=min(1+intellect/12,maxMana-mana);hp+=h;mana+=m;healed+=h;magic+=m;passTurn();if(hp==maxHp&&mana==maxMana)break;}message="You rest beneath the lantern: +"+to_string(healed)+" HP, +"+to_string(magic)+" mana.";}

void Game::searchNearby(){int found=0;for(auto&t:mapNow().traps)if(!t.revealed&&cheb(t.p,player)<=3){t.revealed=true;found++;}message=found?"You reveal "+to_string(found)+" concealed trap"+(found==1?".":"s."):"You study every seam and shadow, finding nothing.";passTurn();}

string Game::inspectSummary(Point p)const{auto&m=mapNow();if(!m.contains(p))return"INSPECT · Beyond the mapped world";int distance=cheb(player,p),light=int(lround(min(1.0,lightAt(p))*100));if(p==player)return"INSPECT · You · "+raceName(race)+" "+className(calling)+" · HP "+to_string(hp)+"/"+to_string(maxHp)+" · Light "+to_string(light)+"%";if(auto z=find_if(m.monsters.begin(),m.monsters.end(),[&](auto&monster){return monster.hp>0&&monster.p==p;});z!=m.monsters.end())return"INSPECT · "+z->name+" · HP "+to_string(z->hp)+"/"+to_string(z->maxHp)+" · "+intentName(z->intent)+" · Range "+to_string(distance)+" · Light "+to_string(light)+"%";if(auto n=find_if(m.npcs.begin(),m.npcs.end(),[&](auto&npc){return npc.p==p;});n!=m.npcs.end())return"INSPECT · "+n->name+" · "+n->role+" · "+n->schedule+" · Range "+to_string(distance);if(auto item=find_if(m.groundItems.begin(),m.groundItems.end(),[&](auto&entry){return entry.p==p;});item!=m.groundItems.end())return"INSPECT · "+item->item.name+" · "+item->item.description+" · Stand here and press G to pick up";if(auto supply=find_if(m.provisions.begin(),m.provisions.end(),[&](auto&entry){return entry.p==p;});supply!=m.provisions.end())return"INSPECT · "+supply->name+" · "+to_string(supply->charges)+" portion"+(supply->charges==1?"":"s")+" remain";if(auto torch=find_if(m.wallTorches.begin(),m.wallTorches.end(),[&](auto&entry){return entry.p==p;});torch!=m.wallTorches.end())return"INSPECT · Wall torch · "+string(torch->lit?"burning":"cold")+" · Range "+to_string(distance)+" · Light "+to_string(light)+"%";if(auto machine=find_if(m.machines.begin(),m.machines.end(),[&](auto&entry){return entry.p==p;});machine!=m.machines.end())return"INSPECT · Ancient machinery · "+string(machine->active?"active":"dormant")+" · Range "+to_string(distance);if(auto trap=find_if(m.traps.begin(),m.traps.end(),[&](auto&entry){return entry.p==p&&entry.revealed;});trap!=m.traps.end())return"INSPECT · Revealed trap · "+string(trap->armed?"armed":"spent")+" · Range "+to_string(distance);if(auto landmark=find_if(m.landmarks.begin(),m.landmarks.end(),[&](auto&entry){return entry.p==p;});landmark!=m.landmarks.end())return"INSPECT · "+landmark->name+" · Landmark · Range "+to_string(distance);auto&tile=m.at(p);if(potableWaterTile(tile))return"INSPECT · "+tile.name+" · Potable · Press E nearby to drink or refill empty flasks";if(tile.name.rfind("plaque: ",0)==0)return"INSPECT · Sign · "+tile.name.substr(8)+" · Press E nearby to read";if(tile.name.rfind("fixture: ",0)==0)return"INSPECT · "+tile.name.substr(9)+" · Interior detail · Range "+to_string(distance);return"INSPECT · "+tile.name+" · "+string(tile.walk?"passable":"blocks movement")+" · "+string(tile.opaque?"opaque":"transparent")+" · Light "+to_string(light)+"%";}

vector<string> Game::inspectDetails(Point p)const{
    auto&m=mapNow();
    auto card=[](vector<string>art,const vector<string>&facts,const string&flavour){int width=56;for(auto&line:art)width=max(width,utf8Width(line));vector<string>lines;for(auto&line:art)lines.push_back(centerPad(line,width));string divider;for(int i=0;i<width;i++)divider+="─";lines.push_back(divider);for(size_t i=0;i<facts.size();i++){auto wrapped=wrapText(facts[i],width);for(size_t row=0;row<wrapped.size();row++)lines.push_back(string(i==0&&row==0?"\4":"")+wrapped[row]);}lines.push_back("");for(auto&part:wrapText(flavour,width))lines.push_back(part);return lines;};
    if(!m.contains(p))return card(generatedPortrait("inspect-unmapped"),{"UNMAPPED DARK","No reliable distance or illumination reading."},"The lantern finds no purchase there; the world ends in an unfinished thought.");
    if(p==player){
        auto ancestry=racePortrait(race),profession=classPortrait(calling);vector<string>art;
        for(size_t i=0;i<max(ancestry.size(),profession.size());i++)art.push_back(fitPad(i<ancestry.size()?ancestry[i]:"",lumen_portraits::columns)+"    "+(i<profession.size()?profession[i]:""));
        return card(move(art),{"ANCESTRY  "+raceName(race)+"          CALLING  "+className(calling),"HP "+to_string(hp)+"/"+to_string(maxHp)+" · MANA "+to_string(mana)+"/"+to_string(maxMana)+" · LEVEL "+to_string(level),"STR "+to_string(strength)+" · DEX "+to_string(dexterity)+" · INT "+to_string(intellect)+" · CON "+to_string(constitution)+" · WIS "+to_string(wisdom)},"Every expedition is a conversation between the flame you carry and the dark that answers.");
    }
    if(auto monster=find_if(m.monsters.begin(),m.monsters.end(),[&](auto&entry){return entry.hp>0&&entry.p==p;});monster!=m.monsters.end())
        return card(monsterPortrait(monster->name),{"CREATURE  "+monster->name,"HP "+to_string(monster->hp)+"/"+to_string(monster->maxHp)+" · ATK "+to_string(monster->attack)+" · DEF "+to_string(monster->defense)+" · INTENT "+intentName(monster->intent),"RANGE "+to_string(cheb(player,p))+" · ILLUMINATION "+to_string(int(lround(min(1.0,lightAt(p))*100)))+"%"},monsterFlavour(monster->name));
    if(auto npc=find_if(m.npcs.begin(),m.npcs.end(),[&](auto&entry){return entry.p==p;});npc!=m.npcs.end())
        return card(npcPortrait(*npc),{"RESIDENT  "+npc->name+" · "+npc->role,"SCHEDULE  "+npc->schedule,"“"+npc->greeting+"”"},npcPresence(*npc));
    if(auto item=find_if(m.groundItems.begin(),m.groundItems.end(),[&](auto&entry){return entry.p==p;});item!=m.groundItems.end())
        return card(itemPortrait(item->item),{"ITEM  "+item->item.name,item->item.description,item->item.equippable()?"EQUIPMENT · "+slotName(item->item.slot):"CONSUMABLE OR TOOL"},"Someone carried this far enough for it to acquire a story, then not far enough to finish it.");
    if(auto supply=find_if(m.provisions.begin(),m.provisions.end(),[&](auto&entry){return entry.p==p;});supply!=m.provisions.end()){
        auto art=generatedPortrait(supply->kind==ProvisionKind::Water?"inspect-water-source":"inspect-food");
        return card(move(art),{"PROVISION  "+supply->name,to_string(supply->charges)+" portions remain · E gathers while adjacent",supply->kind==ProvisionKind::Water?"FRESH WATER SOURCE":"FOOD SOURCE"},"In a place ruled by attrition, this modest cache is as valuable as any relic.");
    }
    if(auto torch=find_if(m.wallTorches.begin(),m.wallTorches.end(),[&](auto&entry){return entry.p==p;});torch!=m.wallTorches.end())
        return card(generatedPortrait("inspect-torch"),{"WALL TORCH  "+string(torch->lit?"BURNING":"COLD"),"MOUNTED ON STONE · E takes while adjacent","ILLUMINATION "+to_string(int(lround(min(1.0,lightAt(p))*100)))+"%"},"The flame shortens, blooms, and leans into currents too small for skin to feel.");
    if(auto machine=find_if(m.machines.begin(),m.machines.end(),[&](auto&entry){return entry.p==p;});machine!=m.machines.end()){
        static const char*names[]={"COMPACT HEAT BRAZIER","RELIQUARY FLOODGATE","ARBITRATION PRISM","WITNESS GENERATOR"};static const char*lores[]={"Four soot-black vanes once divided one fire equally. A crown clamp forces every vane toward Valeward.","ROOT, RIVER, WELL, and SEAL are cut beside four valves. The SEAL valve has been wired open from elsewhere.","The original lens balances four equal beams; a later crown lens concentrates them into one hungry colour.","Iron coils repeat fragments of an order in four pulses. The fourth always arrives as a command instead of an answer."};
        return card(generatedPortrait("inspect-machine"),{string(names[int(machine->kind)]),string(machine->active?"STATE  ACTIVE":"STATE  DORMANT"),"E operates while adjacent"},lores[int(machine->kind)]);
    }
    if(auto trap=find_if(m.traps.begin(),m.traps.end(),[&](auto&entry){return entry.p==p&&entry.revealed;});trap!=m.traps.end()){
        static const char*names[]={"FLAME-JET NOZZLE","SHOCK PLATE","POISON VENT","COLLAPSE TRIGGER","ALARM RUNE"};static const char*lores[]={"A hair-thin fuel line disappears beneath the flagstones.","Copper veins converge beneath the plate like frozen lightning.","The grille is wet with something the light turns green.","Load-bearing cracks radiate upward from one innocent stone.","The carved eye seems to widen whenever the corridor grows quiet."};
        return card(generatedPortrait("inspect-trap"),{string(names[int(trap->kind)]),string(trap->armed?"STATUS  ARMED":"STATUS  SPENT"),"REVEALED HAZARD · RANGE "+to_string(cheb(player,p))},lores[int(trap->kind)]);
    }
    if(auto travel=find_if(m.travels.begin(),m.travels.end(),[&](auto&entry){return entry.p==p;});travel!=m.travels.end())
        return card(generatedPortrait("inspect-passage"),{"PASSAGE  "+travel->label,"LEADS TO  "+maps[travel->targetMap].name,"STEP ONTO THE PASSAGE TO TRAVEL"},"Air from another place moves through the opening, carrying dust with a different history.");
    if(auto landmark=find_if(m.landmarks.begin(),m.landmarks.end(),[&](auto&entry){return entry.p==p;});landmark!=m.landmarks.end())
        return card(generatedPortrait("inspect-landmark"),{"LANDMARK  "+landmark->name,landmark->lore,"RECORDED ON THE EXPLORATION MAP"},"The map remembers this place now, but memory and understanding are not the same thing.");
    auto&tile=m.at(p);if(tile.name.rfind("plaque: ",0)==0)return card(generatedPortrait("inspect-landmark"),{"BUILDING SIGN",tile.name.substr(8),"E reads this plaque while standing beside it"},"Fresh paint fills old chisel cuts; several civic mottos deliberately replace the language of crowns with shared duties.");if(tile.name.rfind("fixture: ",0)==0){string fixtureName=tile.name.substr(9);return card(generatedPortrait("inspect-item"),{"INTERIOR  "+fixtureName,string(tile.walk?"CAN BE STEPPED OVER":"OCCUPIES THIS SPACE"),"PART OF A LIVED-IN BUILDING"},fixtureStory(fixtureName));}vector<string>art;string flavour="The lantern reveals texture, wear, and the patient accumulation of dust.";
    if(tile.name.find("torch")!=string::npos||tile.name.find("lamp")!=string::npos){art=generatedPortrait("inspect-torch");flavour="The flame shortens, blooms, and leans into currents too small for skin to feel.";}
    else if(tile.name.find("water")!=string::npos){art=generatedPortrait("inspect-water");flavour="Reflected light arrives broken into long, trembling pieces.";}
    else if(tile.name.find("door")!=string::npos){art=generatedPortrait("inspect-door");flavour="A door promises that two spaces may become one—and warns that they have not yet.";}
    else if(tile.name.find("wall")!=string::npos||tile.solid){art=generatedPortrait("inspect-wall");flavour=tile.toughness>0?"Fractures catch the light. Enough force could turn certainty into a passage.":"Its surface drinks the weaker colours and returns only a dull edge.";}
    else if(tile.name.find("ashpine")!=string::npos){art=generatedPortrait("inspect-ashpine");flavour="Its needles whisper after the wind has stopped, shedding ash that was never burned.";}
    else art=generatedPortrait("inspect-floor");
    vector<string>facts{"TERRAIN  "+tile.name,string(tile.walk?"PASSABLE":"BLOCKS MOVEMENT")+" · "+(tile.opaque?"OPAQUE":"TRANSPARENT")+(tile.toughness>0?" · TOUGHNESS "+to_string(tile.toughness):""),"ILLUMINATION "+to_string(int(lround(min(1.0,lightAt(p))*100)))+"%"};if(potableWaterTile(tile))facts.push_back("POTABLE · E DRINKS OR REFILLS EMPTY FLASKS WHILE ADJACENT");return card(move(art),facts,flavour);
}

void Game::inspectMenu(){cursor=player;size_t cycle=0;while(true){message=inspectSummary(*cursor)+" · Enter Opens Dossier · Tab Cycles Creatures · X/Esc Closes";draw();for(int i=0;i<4&&!_kbhit();i++)Sleep(8);if(!_kbhit())continue;int k=readKey();if(auto d=keyDirection(k)){Point q=*cursor+*d;if(mapNow().contains(q))cursor=q;}else if(k==9){vector<Point>seen;for(auto&monster:mapNow().monsters)if(monster.hp>0&&lineOfSight(player,monster.p))seen.push_back(monster.p);if(!seen.empty()){cursor=seen[cycle%seen.size()];cycle++;}}else if(k==13||k==' '){Point p=*cursor;string title=inspectSummary(p),prefix="INSPECT · ";if(title.rfind(prefix,0)==0)title=title.substr(prefix.size());auto divider=title.find(" · ");if(divider!=string::npos)title=title.substr(0,divider);panel("EXAMINE · "+title,inspectDetails(p),{236,178,92});}else if(k==27||k=='x'||k=='q'){cursor.reset();message="Inspection closed.";return;}}}

static vector<string> helpLines(){return{
    "\4  MOVEMENT & TIME",
    "    [ARROWS / WASD / HJKL]  Move cardinally   ·   [Y U B N / NUMPAD 1–4, 6–9]  Move diagonally",
    "    [SPACE / NUMPAD 5]      Wait one turn     ·   [R] Rest until danger interrupts you",
    "",
    "\4  EXPLORATION",
    "    [E] Interact / close a door   ·   [G] Pick up   ·   [V] Search   ·   [P] Use a healing potion",
    "    [E] Near a river, well, spring, or cistern: drink or refill every empty flask",
    "    [X] Inspect: move the cursor for an immediate description; [ENTER] opens the full dossier",
    "    [TAB] While inspecting: cycle visible creatures   ·   [M] Open the explored-area map",
    "",
    "\4  CHARACTER & MAGIC",
    "    [I] Pack & equipment   ·   [C] Character & loadout   ·   [Z] Spellbook   ·   [T] Skill constellation",
    "    Pack sorting: [TAB] Cycle   ·   [1] Type   ·   [2] Power   ·   [3] Newest   ·   [4] Name",
    "    [F] Quick-cast your first spell; targeting uses movement, [TAB], [ENTER], and [ESC]",
    "",
    "\4  JOURNEY & SYSTEMS",
    "    [TAB] Quests   ·   [F2] Fast travel   ·   [F3] Crafting   ·   [F4] Lore   ·   [O] Lantern",
    "    [F5] Quick-save   ·   [F9] Quick-load   ·   [F6] Reaction   ·   [F7] Sound   ·   [F8] Reduced motion",
    "    [TAB] Journal: Enter tracks a lead on the HUD. [Q / ESC] Quit dialog",
    "",
    "\4  WORLD RULES",
    "    Actors wait for your turn. Lighting, flames, projectiles, and explosions do not.",
    "    One ration and one flask each last roughly 24 hours; empty reserves make you weak.",
    "    Going roughly four days without food or water is fatal. Empty flasks are reusable.",
    "    Terrain, traps, machinery, dropped gear, and wall torches persist.",
    "",
    "\5                       PRESS ANY KEY TO RETURN"
};}

static vector<string> compactHelpLines(int page){
    if(page==0)return{
        "\4  MOVEMENT & TIME",
        "    [ARROWS / WASD / HJKL] Cardinal movement",
        "    [Y U B N / NUMPAD 1–4, 6–9] Diagonal movement",
        "    [SPACE / NUMPAD 5] Wait one turn   ·   [R] Rest until interrupted",
        "",
        "\4  EXPLORATION",
        "    [E] Interact / close door   ·   [G] Pick up   ·   [V] Search",
        "    [P] Use healing potion   ·   [M] Explored-area map",
        "    [X] Inspect tiles live   ·   [ENTER] Open the selected dossier",
        "    [TAB] While inspecting: cycle visible creatures",
        "",
        "\4  CHARACTER & MAGIC",
        "    [I] Pack   ·   [C] Character   ·   [Z] Spellbook   ·   [T] Skills",
        "    Pack sort: [1] Type  [2] Power  [3] Newest  [4] Name  [TAB] Next",
        "    [F] Quick-cast first spell; targeting uses movement, Tab, Enter, and Esc",
        "",
        "\5 PAGE 1/2 · ←→ Page · PgUp/Dn Scroll · Esc Close"
    };
    return{
        "\4  JOURNEY & SYSTEMS",
        "    [TAB] Quests   ·   [F2] Travel   ·   [F3] Craft   ·   [F4] Lore",
        "    [F5] Save   ·   [F9] Load   ·   [F6] Reaction   ·   [O] Lantern",
        "    [F7] Sound on/off   ·   [F8] Reduced motion   ·   [Q / ESC] Quit",
        "",
        "\4  WATER & PROVISIONS",
        "    One ration and one water flask each last roughly 24 hours.",
        "    Press [E] near potable water to drink or refill every empty flask.",
        "    Empty reserves weaken you; four untreated days are fatal.",
        "",
        "\4  WORLD RULES",
        "    Actors wait for your turn; lighting and visual effects animate continuously.",
        "    Terrain, traps, machinery, dropped gear, and wall torches persist.",
        "    New spells are learned through spell nodes in the skill constellation.",
        "",
        "\5 PAGE 2/2 · ←→ Page · PgUp/Dn Scroll · Esc Close"
    };
}

void Game::helpMenu(){auto[sw,sh]=consoleViewport();if(sw>=118&&sh>=34){panel("FIELD GUIDE · CONTROLS",helpLines(),{245,184,80});return;}int page=0;panelScroll=0;while(true){renderPanel("FIELD GUIDE · "+to_string(page+1)+"/2",compactHelpLines(page),{245,184,80},-1,false);for(int i=0;i<4&&!_kbhit();i++)Sleep(8);if(!_kbhit())continue;int key=readKey();if(key==1003||key==1004||key=='a'||key=='d'||key=='h'||key=='l'||key==9){page=1-page;panelScroll=0;}else if(key==1006)panelScroll=max(0,panelScroll-8);else if(key==1008)panelScroll=min(64,panelScroll+8);else if(key==13||key==27||key=='q'||key=='?'){panelScroll=0;return;}}}
void Game::renderQaHelp(){configureCharacter(RaceKind::Human,ClassKind::Vanguard);current=0;auto[sw,sh]=consoleViewport();bool compact=sw<118||sh<34;renderPanel(compact?"FIELD GUIDE · 1/2":"FIELD GUIDE · CONTROLS",compact?compactHelpLines(0):helpLines(),{245,184,80},-1,false);}

bool Game::save(const filesystem::path& path){
    ofstream o(path,ios::binary);if(!o){message="Save failed: cannot create "+path.string();return false;}o<<"LRCPP3\n"<<seed<<' '<<current<<' '<<player.x<<' '<<player.y<<' '<<hp<<' '<<maxHp<<' '<<attack<<' '<<defense<<' '<<gold<<' '<<level<<' '<<xp<<' '<<turns<<' '<<food<<' '<<water<<' '<<lantern<<' '<<int(reaction)<<'\n';
    o<<materials.size()<<'\n';for(auto&[k,v]:materials)o<<quoted(k)<<' '<<v<<'\n';
    o<<flags.size()<<'\n';for(auto&s:flags)o<<quoted(s)<<'\n';o<<discovered.size()<<'\n';for(auto&s:discovered)o<<quoted(s)<<'\n';o<<cleared.size()<<'\n';for(auto&s:cleared)o<<quoted(s)<<'\n';o<<lore.size()<<'\n';for(auto&s:lore)o<<quoted(s)<<'\n';
    o<<quests.size()<<'\n';for(auto&q:quests)o<<quoted(q.id)<<' '<<q.progress<<' '<<q.accepted<<' '<<q.done<<'\n';o<<maps.size()<<'\n';for(auto&m:maps){vector<size_t> ex;for(size_t i=0;i<m.explored.size();i++)if(m.explored[i])ex.push_back(i);o<<quoted(m.name)<<' '<<ex.size();for(auto i:ex)o<<' '<<i;o<<'\n'<<m.monsters.size()<<'\n';for(auto&z:m.monsters)o<<quoted(z.name)<<' '<<z.p.x<<' '<<z.p.y<<' '<<z.home.x<<' '<<z.home.y<<' '<<z.hp<<' '<<z.maxHp<<' '<<z.attack<<' '<<z.defense<<' '<<z.alert<<' '<<int(z.intent)<<' '<<z.rewardClaimed<<'\n';}
    auto writeItem=[&](const Item&i){o<<int(i.kind)<<' '<<quoted(i.name)<<' '<<quoted(i.description)<<' '<<int(uint8_t(i.glyph))<<' '<<i.color.r<<' '<<i.color.g<<' '<<i.color.b<<' '<<i.quantity<<' '<<int(i.slot)<<' '<<i.attack<<' '<<i.defense<<' '<<i.spell<<' '<<i.light<<' '<<i.equipped<<' '<<i.acquiredTurn<<' '<<i.acquiredOrder<<'\n';};
    o<<"PARITY3\n"<<mana<<' '<<maxMana<<' '<<skillPoints<<' '<<strength<<' '<<dexterity<<' '<<intellect<<' '<<constitution<<' '<<wisdom<<' '<<spellPower<<' '<<lanternRadius<<' '<<lanternIntensity<<' '<<int(race)<<' '<<int(calling)<<'\n';o<<inventory.size()<<'\n';for(auto&i:inventory)writeItem(i);o<<learnedSpells.size()<<'\n';for(auto s:learnedSpells)o<<int(s)<<' ';o<<'\n'<<skills.size()<<'\n';for(auto&s:skills)o<<s.rank<<' ';o<<'\n'<<maps.size()<<'\n';for(auto&m:maps){o<<m.provisions.size()<<'\n';for(auto&s:m.provisions)o<<s.charges<<' ';o<<'\n'<<m.wallTorches.size()<<'\n';for(auto&t:m.wallTorches)o<<t.p.x<<' '<<t.p.y<<' '<<t.lit<<'\n';o<<m.traps.size()<<'\n';for(auto&t:m.traps)o<<t.revealed<<' '<<t.armed<<'\n';o<<m.machines.size()<<'\n';for(auto&x:m.machines)o<<x.active<<' ';o<<'\n'<<m.groundItems.size()<<'\n';for(auto&i:m.groundItems){o<<i.p.x<<' '<<i.p.y<<' ';writeItem(i.item);}vector<pair<size_t,ElementCell>> active;for(size_t i=0;i<m.elements.size();i++){auto&e=m.elements[i];if(e.wet||e.oil||e.fire||e.frost||e.electricity||e.poison||e.smoke||e.web)active.push_back({i,e});}o<<active.size()<<'\n';for(auto&[i,e]:active)o<<i<<' '<<e.wet<<' '<<e.oil<<' '<<e.fire<<' '<<e.frost<<' '<<e.electricity<<' '<<e.poison<<' '<<e.smoke<<' '<<e.web<<'\n';}
    o<<"PRESENTATION1\n"<<quoted(trackedQuest)<<' '<<int(preferredInventorySort)<<' '<<reducedMotion<<'\n';
    message="Expedition saved to "+path.string()+".";return bool(o);
}

optional<Game> Game::load(const filesystem::path& path){
    ifstream in(path,ios::binary);string magic;if(!(in>>magic)||(magic!="LRCPP2"&&magic!="LRCPP3"))return nullopt;bool itemMetadata=magic=="LRCPP3";uint32_t savedSeed;int mapIndex,px,py,reactionValue;in>>savedSeed>>mapIndex>>px>>py;Game g(savedSeed);in>>g.hp>>g.maxHp>>g.attack>>g.defense>>g.gold>>g.level>>g.xp>>g.turns>>g.food>>g.water>>g.lantern>>reactionValue;g.current=clamp(mapIndex,0,int(g.maps.size()-1));g.player={px,py};g.reaction=Reaction(clamp(reactionValue,0,3));
    size_t n;string text;int value;in>>n;g.materials.clear();for(size_t i=0;i<n;i++){in>>quoted(text)>>value;g.materials[text]=value;}in>>n;g.flags.clear();for(size_t i=0;i<n;i++){in>>quoted(text);g.flags.insert(text);}in>>n;g.discovered.clear();for(size_t i=0;i<n;i++){in>>quoted(text);g.discovered.insert(text);}in>>n;g.cleared.clear();for(size_t i=0;i<n;i++){in>>quoted(text);g.cleared.insert(text);}in>>n;g.lore.clear();for(size_t i=0;i<n;i++){in>>quoted(text);g.lore.push_back(text);}
    in>>n;for(size_t i=0;i<n;i++){int progress;bool accepted,done;in>>quoted(text)>>progress>>accepted>>done;if(auto q=find_if(g.quests.begin(),g.quests.end(),[&](auto&a){return a.id==text;});q!=g.quests.end()){q->progress=progress;q->accepted=accepted;q->done=done;}}
    size_t mapCount;in>>mapCount;for(size_t mi=0;mi<mapCount;mi++){size_t exCount;in>>quoted(text)>>exCount;vector<size_t> ex(exCount);for(auto&i:ex)in>>i;size_t monsterCount;in>>monsterCount;if(mi>=g.maps.size())return nullopt;auto&m=g.maps[mi];fill(m.explored.begin(),m.explored.end(),0);for(auto i:ex)if(i<m.explored.size())m.explored[i]=1;for(size_t zi=0;zi<monsterCount;zi++){string name;int x,y,hx,hy,zhp,zmax,atk,def,alert,intent;bool claimed;in>>quoted(name)>>x>>y>>hx>>hy>>zhp>>zmax>>atk>>def>>alert>>intent>>claimed;if(zi<m.monsters.size()){auto&z=m.monsters[zi];z.name=name;z.p={x,y};z.home={hx,hy};z.hp=zhp;z.maxHp=zmax;z.attack=atk;z.defense=def;z.alert=alert;z.intent=Intent(clamp(intent,0,7));z.rewardClaimed=claimed;}}}
    in>>ws;if(in.peek()!=char_traits<char>::eof()){string extension;in>>extension;if(extension!="PARITY3")return nullopt;int raceValue,classValue;in>>g.mana>>g.maxMana>>g.skillPoints>>g.strength>>g.dexterity>>g.intellect>>g.constitution>>g.wisdom>>g.spellPower>>g.lanternRadius>>g.lanternIntensity>>raceValue>>classValue;g.race=RaceKind(clamp(raceValue,0,3));g.calling=ClassKind(clamp(classValue,0,3));g.nextItemOrder=1;auto readItem=[&](){Item item;int kind,glyph,slot;in>>kind>>quoted(item.name)>>quoted(item.description)>>glyph>>item.color.r>>item.color.g>>item.color.b>>item.quantity>>slot>>item.attack>>item.defense>>item.spell>>item.light>>item.equipped;if(itemMetadata)in>>item.acquiredTurn>>item.acquiredOrder;else{item.acquiredTurn=0;item.acquiredOrder=g.nextItemOrder++;}item.kind=ItemKind(clamp(kind,0,int(ItemKind::EmptyFlask)));item.glyph=char(glyph);item.slot=Slot(clamp(slot,0,int(Slot::Amulet)));g.nextItemOrder=max(g.nextItemOrder,item.acquiredOrder+1);return item;};in>>n;g.inventory.clear();for(size_t i=0;i<n;i++)g.inventory.push_back(readItem());in>>n;g.learnedSpells.clear();for(size_t i=0;i<n;i++){int s;in>>s;g.learnedSpells.push_back(SpellKind(clamp(s,0,int(SpellKind::Count)-1)));}g.skills=buildSkillTree(g.calling);in>>n;for(size_t i=0;i<n;i++){int rank;in>>rank;if(i<g.skills.size())g.skills[i].rank=rank;}in>>mapCount;for(size_t mi=0;mi<mapCount&&mi<g.maps.size();mi++){auto&m=g.maps[mi];in>>n;for(size_t i=0;i<n;i++){int charges;in>>charges;if(i<m.provisions.size())m.provisions[i].charges=charges;}for(auto&t:m.wallTorches){m.at(t.p)=t.underlying;m.lights.erase(remove_if(m.lights.begin(),m.lights.end(),[&](auto&l){return l.p==t.p;}),m.lights.end());}map<Point,Tile>bases;for(auto&t:m.wallTorches)bases[t.p]=t.underlying;m.wallTorches.clear();in>>n;for(size_t i=0;i<n;i++){int x,y;bool lit;in>>x>>y>>lit;Point p{x,y};if(!m.contains(p))continue;Tile base=bases.contains(p)?bases[p]:m.at(p);m.wallTorches.push_back({p,base,lit});m.at(p)=WallTorchTile;if(lit)m.lights.push_back({p,{255,103,25},9,1.3,.2,double(x+y)});}in>>n;for(size_t i=0;i<n;i++){bool revealed,armed;in>>revealed>>armed;if(i<m.traps.size()){m.traps[i].revealed=revealed; m.traps[i].armed=armed;}}in>>n;for(size_t i=0;i<n;i++){bool active;in>>active;if(i<m.machines.size())m.machines[i].active=active;}in>>n;m.groundItems.clear();for(size_t i=0;i<n;i++){int x,y;in>>x>>y;m.groundItems.push_back({{x,y},readItem()});}fill(m.elements.begin(),m.elements.end(),ElementCell{});in>>n;for(size_t i=0;i<n;i++){size_t index;ElementCell e;in>>index>>e.wet>>e.oil>>e.fire>>e.frost>>e.electricity>>e.poison>>e.smoke>>e.web;if(index<m.elements.size())m.elements[index]=e;}}}
    if(!in.fail()&&!in.eof()){in>>ws;if(in.peek()!=char_traits<char>::eof()){string tag;int sort;in>>tag;if(tag!="PRESENTATION1"||!(in>>quoted(g.trackedQuest)>>sort>>g.reducedMotion))return nullopt;g.preferredInventorySort=InventorySort(clamp(sort,0,3));}}
    if(in.eof())in.clear();if(!in||!g.mapNow().contains(g.player))return nullopt;for(auto&q:g.quests)if((q.done&&q.flag=="stonecross_safe")||(q.flag=="story_complete"&&(q.done||(g.cleared.contains("Valeward Undercroft")&&g.cleared.contains("The Unsurveyed Sixth · Depth 6")))))g.applyQuestConsequences(q);g.coalesceInventory();g.refreshQuestProgress();g.updateSchedules();g.message="Expedition loaded from "+path.string()+".";return g;
}

bool Game::selfTest(){
    try{
#include "PolishTests.hpp"
#include "ShopTests.hpp"
        Game dialogue(94117);dialogue.configureCharacter(RaceKind::Elf,ClassKind::Ranger);auto&builtTown=dialogue.maps[0];auto interiors=count_if(builtTown.tiles.begin(),builtTown.tiles.end(),[](auto&tile){return tile.name.find("interior")!=string::npos;}),windows=count_if(builtTown.tiles.begin(),builtTown.tiles.end(),[](auto&tile){return tile.name.find("window")!=string::npos;}),doors=count_if(builtTown.tiles.begin(),builtTown.tiles.end(),[](auto&tile){return tile.name=="closed door";}),plaques=count_if(builtTown.tiles.begin(),builtTown.tiles.end(),[](auto&tile){return tile.name.rfind("plaque: ",0)==0;}),fixtures=count_if(builtTown.tiles.begin(),builtTown.tiles.end(),[](auto&tile){return tile.name.rfind("fixture: ",0)==0;});if(interiors<220||windows<12||doors<8||plaques!=8||fixtures<30||builtTown.npcs.size()<5||builtTown.landmarks.size()<12)throw runtime_error("structured town generation contract");vector<DialogueChoice>qaChoices{{"Tell me about Emberfall.","town",""},{"What do you do here?","work",""},{"Farewell.","","leave"}};auto dialogueLines=dialogue.conversationLines(builtTown.npcs.front(),builtTown.npcs.front().name,builtTown.npcs.front().greeting,qaChoices,0);if(dialogueLines.size()<10||npcPortrait(builtTown.npcs.front())==racePortrait(dialogue.race)||none_of(dialogueLines.begin(),dialogueLines.end(),[](auto&line){return utf8Width(line)>=48&&line.find("▀")!=string::npos;})||none_of(dialogueLines.begin(),dialogueLines.end(),[](auto&line){return !line.empty()&&line[0]=='\1';}))throw runtime_error("two-portrait dialogue tree presentation contract");auto mara=find_if(builtTown.npcs.begin(),builtTown.npcs.end(),[](auto&npc){return npc.id=="mara";}),orra=find_if(builtTown.npcs.begin(),builtTown.npcs.end(),[](auto&npc){return npc.id=="orra";});if(mara==builtTown.npcs.end()||orra==builtTown.npcs.end()||npcPrompt(*mara,"town")==npcPrompt(*orra,"town")||npcWorkDialogue(*mara)==npcWorkDialogue(*orra)||npcTopicDialogue(*mara,"rumour_more",true)==npcTopicDialogue(*orra,"rumour_more",true)||npcFarewellDialogue(*mara)==npcFarewellDialogue(*orra))throw runtime_error("character voice diversity contract");fill(builtTown.explored.begin(),builtTown.explored.end(),0);dialogue.discovered.clear();dialogue.lore.clear();auto marked=dialogue.markLocalMap(*mara);if(marked.size()!=builtTown.landmarks.size()||any_of(builtTown.landmarks.begin(),builtTown.landmarks.end(),[&](auto&landmark){return dialogue.discovered.contains(landmark.name)||!dialogue.flags.contains("charted_"+landmark.name)||!builtTown.explored[size_t(landmark.p.y)*builtTown.w+landmark.p.x];}))throw runtime_error("conversation map action contract");
        vector<Npc*>cast;for(auto&worldMap:dialogue.maps)for(auto&npc:worldMap.npcs)cast.push_back(&npc);
        set<string>greetings,workVoices,evidenceVoices,conclusions,presences;for(auto*n:cast){greetings.insert(n->greeting);workVoices.insert(npcWorkDialogue(*n));evidenceVoices.insert(npcRumourDialogue(*n));conclusions.insert(npcTopicDialogue(*n,"rumour_more",n->id=="mara"||n->id=="orra"||n->id=="neris"||n->id=="tovan"||n->id=="sable"));presences.insert(npcPresence(*n));if(npcWorkDialogue(*n).size()<90||npcRumourDialogue(*n).size()<90||npcPresence(*n).size()<80)throw runtime_error("underwritten character voice for "+n->id);}
        if(cast.size()!=9||greetings.size()!=9||workVoices.size()!=9||evidenceVoices.size()!=9||conclusions.size()!=9||presences.size()!=9)throw runtime_error("distinct nine-character narrative voice contract");auto sable=find_if(builtTown.npcs.begin(),builtTown.npcs.end(),[](auto&npc){return npc.id=="sable";});if(sable==builtTown.npcs.end()||npcTopicDialogue(*mara,"delve",true).find("Lampwright Muster")==string::npos||npcTopicDialogue(*orra,"rumour_more",true).find("Bramblewick")==string::npos||npcTopicDialogue(*sable,"delve",true).find("Foundation Seal")==string::npos)throw runtime_error("Emberfall clue-chain contract");
        if(fixtureStory("Iven's labelled wick cutter").find("SIX IS OPEN")==string::npos||fixtureStory("Town Hall expedition archive missing Survey Six").find("FOUNDATION")==string::npos||monsterFlavour("Oathless Regent").find("unfinished coronation")==string::npos)throw runtime_error("environmental plot evidence contract");auto wildLandmark=[&](const string&name)->Point{auto found=find_if(dialogue.maps[1].landmarks.begin(),dialogue.maps[1].landmarks.end(),[&](auto&landmark){return landmark.name==name;});if(found==dialogue.maps[1].landmarks.end())throw runtime_error("missing wilderness landmark "+name);return found->p;};Point bramblePoint=wildLandmark("Bramblewick"),stonePoint=wildLandmark("Stonecross"),reedPoint=wildLandmark("Reedwater"),hollowPoint=wildLandmark("Hollowroot Mine"),barrowPoint=wildLandmark("Sunken Barrow"),moonwellPoint=wildLandmark("Moonwell Cavern");if(hollowPoint.dist2(bramblePoint)>=min(hollowPoint.dist2(stonePoint),hollowPoint.dist2(reedPoint))||barrowPoint.dist2(stonePoint)>=min(barrowPoint.dist2(bramblePoint),barrowPoint.dist2(reedPoint))||moonwellPoint.dist2(reedPoint)>=min(moonwellPoint.dist2(bramblePoint),moonwellPoint.dist2(stonePoint)))throw runtime_error("quest geography and settlement causality contract");
        if(none_of(dialogue.maps[mainMapForDepth(1)].landmarks.begin(),dialogue.maps[mainMapForDepth(1)].landmarks.end(),[](auto&landmark){return landmark.name=="Lampwright Muster"&&landmark.lore.find("DEPTH SIX")!=string::npos;})||none_of(dialogue.maps[mainMapForDepth(5)].landmarks.begin(),dialogue.maps[mainMapForDepth(5)].landmarks.end(),[](auto&landmark){return landmark.name=="Surveyor Pell's Rest";}))throw runtime_error("main Delve environmental story sequence");
        Game presentation(94117);presentation.configureCharacter(RaceKind::Human,ClassKind::Vanguard);auto skillStart=presentation.skillTreeLines(1,false,0.0),skillCompact=presentation.skillTreeLines(1,true,0.0);if(any_of(skillStart.begin(),skillStart.end(),[](auto&line){return line.find('~')!=string::npos;})||none_of(skillStart.begin(),skillStart.end(),[](auto&line){return line.find("SELECTED NODE")!=string::npos;})||none_of(skillStart.begin(),skillStart.end(),[](auto&line){return line.find("TREE KEY")!=string::npos;})||none_of(skillCompact.begin(),skillCompact.end(),[](auto&line){return !line.empty()&&line[0]=='\1';}))throw runtime_error("skill constellation presentation contract");auto humanArt=racePortrait(RaceKind::Human),elfArt=racePortrait(RaceKind::Elf);if(elfArt.size()!=lumen_portraits::rows||humanArt==elfArt||any_of(elfArt.begin(),elfArt.end(),[](auto&line){return utf8Width(line)!=lumen_portraits::columns;})||none_of(elfArt.begin(),elfArt.end(),[](auto&line){return line.find("▀")!=string::npos&&line.find("\x1b[38;2;")!=string::npos;})||fitPad("AB◇CD",3)!="AB◇")throw runtime_error("generated terminal portrait and UTF-8 layout contract");auto wrappedDossier=wrapText("A deliberately long dossier sentence that must remain inside its own column.",24);if(wrappedDossier.size()<2||any_of(wrappedDossier.begin(),wrappedDossier.end(),[](auto&line){return utf8Width(line)>24;}))throw runtime_error("dossier column wrapping contract");auto koboldArt=monsterPortrait("candle kobold"),spiderArt=monsterPortrait("glassfang spider"),cultistArt=monsterPortrait("ash cultist");if(koboldArt.size()<9||spiderArt.size()<8||cultistArt.size()<9||koboldArt==spiderArt||spiderArt==cultistArt||none_of(koboldArt.begin(),koboldArt.end(),[](auto&line){return utf8Width(line)>=24;}))throw runtime_error("species portrait artwork contract");
        auto wallArt=generatedPortrait("inspect-wall"),voidArt=generatedPortrait("inspect-unmapped");if(lumen_portraits::count!=39||generatedPortraitIndex("monster-mimic")<8||generatedPortraitIndex("item-jewellery")<20||generatedPortraitIndex("inspect-wall")<20||wallArt==voidArt||wallArt.size()!=lumen_portraits::rows||any_of(wallArt.begin(),wallArt.end(),[](auto&line){return utf8Width(line)!=lumen_portraits::columns;})||none_of(wallArt.begin(),wallArt.end(),[](auto&line){return line.find("▀")!=string::npos&&line.find("\x1b[48;2;")!=string::npos;}))throw runtime_error("generated monster and inspection bitmap contract");
        const array<pair<int,Point>,16> movementKeys{{{1001,{0,-1}},{'8',{0,-1}},{1002,{0,1}},{'2',{0,1}},{1003,{-1,0}},{'4',{-1,0}},{1004,{1,0}},{'6',{1,0}},{1005,{-1,-1}},{'7',{-1,-1}},{1006,{1,-1}},{'9',{1,-1}},{1007,{-1,1}},{'1',{-1,1}},{1008,{1,1}},{'3',{1,1}}}};for(auto&[key,expected]:movementKeys){auto actual=keyDirection(key);if(!actual||*actual!=expected)throw runtime_error("movement input mapping");}if(keyDirection(1005,false)||keyDirection('7',false))throw runtime_error("cardinal-only input mapping");
        Game a(73451),b(73451),c(73452);if(fingerprint(a.maps[1])!=fingerprint(b.maps[1]))throw runtime_error("same seed did not reproduce wilderness");if(fingerprint(a.maps[1])==fingerprint(c.maps[1]))throw runtime_error("different seeds did not vary wilderness");auto&town=a.maps[0];auto lampCount=count_if(town.tiles.begin(),town.tiles.end(),[](auto&tile){return tile.name=="street lamp";});if(lampCount<8||town.lights.size()<11)throw runtime_error("town street-lamp population");a.current=0;a.lantern=false;auto lamp=find_if(town.tiles.begin(),town.tiles.end(),[](auto&tile){return tile.name=="street lamp";});Point lampPoint{int((lamp-town.tiles.begin())%town.w),int((lamp-town.tiles.begin())/town.w)};double dim=10,bright=0;for(int i=0;i<64;i++){auto sample=a.illuminationAt(lampPoint,12.0+i*.03125);double value=max({sample.r,sample.g,sample.b});dim=min(dim,value);bright=max(bright,value);}if(bright-dim<.08)throw runtime_error("real-time lamp flicker amplitude");a.player=a.openNear(town,lampPoint+Point{2,0});fill(town.explored.begin(),town.explored.end(),1);string lampFrameA=a.renderWorldFrame(12.0),lampFrameB=a.renderWorldFrame(12.181);if(lampFrameA==lampFrameB)throw runtime_error("quantized rendered lamp flicker");if(lampFrameA.find("\x1b[4;1H")==string::npos||lampFrameA.find("\x1b[19;1H")==string::npos)throw runtime_error("absolute-row frame composition");
        for(uint32_t s:{1u,7u,42u,94117u,128003u,761239u}){Game g(s);if(g.maps.size()!=MAIN_SIXTH_MAP+1)throw runtime_error("world map count and legacy index contract");for(size_t mi=0;mi<g.maps.size();mi++){auto&m=g.maps[mi];for(auto p:m.critical)if(!g.reachable(m,m.start,p))throw runtime_error("unreachable critical point for seed "+to_string(s)+" in "+m.name);for(auto&t:m.travels){if(!m.contains(t.p)||!m.at(t.p).walk||t.targetMap<0||t.targetMap>=int(g.maps.size())||!g.maps[t.targetMap].contains(t.arrival)||!g.maps[t.targetMap].at(t.arrival).walk||!g.reachable(m,m.start,t.p))throw runtime_error("invalid or unreachable travel link in "+m.name);}for(int y=0;y<m.h;y++)for(int x=0;x<m.w;x++){Point p{x,y};if(m.at(p).name=="stairs up"||m.at(p).name=="stairs down")if(none_of(m.travels.begin(),m.travels.end(),[&](auto&t){return t.p==p;}))throw runtime_error("visible stair has no destination in "+m.name);}}for(int i=SITE_FIRST_MAP;i<SITE_FIRST_MAP+SITE_COUNT;i++){int bosses=count_if(g.maps[i].monsters.begin(),g.maps[i].monsters.end(),[](auto&z){return z.boss;});if(bosses!=1||!g.reachable(g.maps[i],g.maps[i].up,g.maps[i].monsters.back().p)||g.maps[i].at(g.maps[i].down).name!="dungeon heart")throw runtime_error("site endpoint and boss reachability");}auto&fifth=g.maps[mainMapForDepth(5)];auto sixthLink=find_if(fifth.travels.begin(),fifth.travels.end(),[&](auto&t){return t.p==fifth.down;});auto&sixth=g.maps[mainMapForDepth(6)];if(sixthLink==fifth.travels.end()||sixthLink->targetMap!=mainMapForDepth(6)||mainDepthForMap(mainMapForDepth(6))!=6||sixth.at(sixth.down).name!="foundation seal"||count_if(sixth.monsters.begin(),sixth.monsters.end(),[](auto&z){return z.boss;})!=1)throw runtime_error("sixth Delve floor and terminal seal contract");}
        Game progression(94117);auto questById=[&](const string&id)->Quest&{auto q=find_if(progression.quests.begin(),progression.quests.end(),[&](auto&entry){return entry.id==id;});if(q==progression.quests.end())throw runtime_error("missing quest "+id);return *q;};for(auto&q:progression.quests){bool giverExists=any_of(progression.maps.begin(),progression.maps.end(),[&](auto&m){return any_of(m.npcs.begin(),m.npcs.end(),[&](auto&npc){return npc.id==q.giver;});});if(!giverExists)throw runtime_error("quest giver missing for "+q.id);if(!q.prerequisite.empty()&&none_of(progression.quests.begin(),progression.quests.end(),[&](auto&candidate){return candidate.id==q.prerequisite;}))throw runtime_error("quest prerequisite missing for "+q.id);if(q.type=="discover"&&none_of(progression.maps.begin(),progression.maps.end(),[&](auto&m){return any_of(m.landmarks.begin(),m.landmarks.end(),[&](auto&landmark){return landmark.name==q.target;});}))throw runtime_error("quest landmark missing for "+q.id);if(q.type=="clear"){auto targetMap=find_if(progression.maps.begin(),progression.maps.end(),[&](auto&m){return m.name==q.target;});if(targetMap==progression.maps.end()||none_of(targetMap->monsters.begin(),targetMap->monsters.end(),[](auto&z){return z.boss;}))throw runtime_error("quest dungeon or boss missing for "+q.id);}}
        if(auto maraQuest=progression.questFor("mara");!maraQuest||maraQuest->id!="blue_circuit")throw runtime_error("central plot quest is not available at arrival");if(auto roen=progression.questFor("roen");!roen||roen->id!="compact"||roen->need!=3||roen->progress!=0)throw runtime_error("Western Compact is not available before village work");progression.discovered.insert("Whispering Stones");auto&stonesQuest=questById("stones");stonesQuest.accepted=true;progression.cleared.insert("Moonwell Cavern");auto&moonwellQuest=questById("moonwell");moonwellQuest.accepted=true;int markedGoblins=0;for(auto&m:progression.maps)for(auto&z:m.monsters)if(markedGoblins<3&&z.name.find("goblin")!=string::npos){z.hp=0;z.rewardClaimed=true;markedGoblins++;}if(markedGoblins<3)throw runtime_error("finite goblin scout quest population");auto&scoutQuest=questById("bramble_scouts");scoutQuest.accepted=true;progression.flags.insert("bramble_safe");progression.flags.insert("stonecross_safe");progression.flags.insert("reedwater_safe");auto&compactQuest=questById("compact");compactQuest.accepted=true;progression.flags.insert("foundation_secured");progression.flags.insert("valeward_safe");auto&endingQuest=questById("common_light");endingQuest.accepted=true;progression.refreshQuestProgress();if(stonesQuest.progress!=stonesQuest.need||moonwellQuest.progress!=moonwellQuest.need||scoutQuest.progress!=scoutQuest.need||compactQuest.progress!=3||endingQuest.progress!=2)throw runtime_error("order-independent quest and ending progress reconciliation");
        auto locked=find_if(progression.maps[1].travels.begin(),progression.maps[1].travels.end(),[](auto&t){return t.castleLocked;});if(locked==progression.maps[1].travels.end())throw runtime_error("Valeward lock travel missing");progression.current=1;progression.player=locked->p;progression.travel(*locked);if(progression.current!=1||progression.message.find("Captain Roen")==string::npos)throw runtime_error("actionable completed-compact gate guidance");progression.flags.insert("valeward_commissioned");progression.travel(*locked);if(progression.current!=locked->targetMap)throw runtime_error("Valeward commission did not unlock travel");
        Game ending(94117);ending.current=MAIN_SIXTH_MAP;auto&sixthBoss=ending.maps[MAIN_SIXTH_MAP].monsters.back();sixthBoss.hp=0;ending.killMonster(sixthBoss);ending.current=SITE_FIRST_MAP+3;auto&regentBoss=ending.maps[SITE_FIRST_MAP+3].monsters.back();regentBoss.hp=0;ending.killMonster(regentBoss);if(ending.message.find("CHAPTER COMPLETE")==string::npos||none_of(ending.lore.begin(),ending.lore.end(),[](auto&entry){return entry.rfind("The Lantern Compact Restored:",0)==0;}))throw runtime_error("order-independent narrative climax contract");
        if(!Boulder.solid||Boulder.opaque)throw runtime_error("transparent obstacle shadow contract");Game shadow(90421);shadow.current=2;auto&m=shadow.mapNow();Point source{},mid{},target{};bool found=false;for(int y=2;y<m.h-2&&!found;y++)for(int x=2;x<m.w-2&&!found;x++)for(auto d:DIRS){source={x,y};mid=source+d;target=mid+d;if(m.at(source).walk&&m.at(mid).walk&&m.at(target).walk){found=true;break;}}if(!found)throw runtime_error("shadow test corridor");shadow.player=m.start;Monster blocker;blocker.p=blocker.home=mid;blocker.hp=blocker.maxHp=1;m.monsters.push_back(blocker);if(!shadow.lineOfSight(source,target)||shadow.clearLight(source,target))throw runtime_error("monster shadows altered line of sight contract");
        Game lighting(42190);lighting.configureCharacter(RaceKind::Ashborn,ClassKind::Arcanist);lighting.current=2;auto&litMap=lighting.mapNow();if(litMap.wallTorches.empty())throw runtime_error("lighting fixture generation");Point fixture=litMap.wallTorches.front().p,litPoint=lighting.openNear(litMap,fixture);lighting.player=litPoint;auto[softVisibility,fringe]=lighting.sampleLightVisibility(fixture,litPoint,.28);auto chroma=lighting.illuminationAt(litPoint,visualSeconds());if(softVisibility<=0||softVisibility>1||fringe<0||chroma.r<=chroma.b)throw runtime_error("sampled RGB lighting contract");string frame=lighting.renderWorldFrame();if(frame.find("\x1b[48;2;")==string::npos||frame.find("█")==string::npos)throw runtime_error("true-color composited frame contract");
        Game survival(94117);survival.configureCharacter(RaceKind::Human,ClassKind::Vanguard);int fedAttack=survival.totalAttack();for(int elapsed=0;elapsed<239;elapsed++){survival.turns++;survival.advanceSurvival();}if(survival.food!=5||survival.water!=5||survival.hp!=survival.maxHp)throw runtime_error("daily survival reserve pacing");survival.turns++;survival.advanceSurvival();if(survival.food!=0||survival.water!=0||survival.totalAttack()>=fedAttack)throw runtime_error("one-day deprivation weakness");for(int elapsed=0;elapsed<480;elapsed++){survival.turns++;survival.advanceSurvival();}if(survival.hp<=0)throw runtime_error("deprivation killed before roughly four days");for(int elapsed=0;elapsed<240;elapsed++){survival.turns++;survival.advanceSurvival();}if(survival.hp>0)throw runtime_error("deprivation did not become fatal around day four");Game canteen(94117);canteen.configureCharacter(RaceKind::Human,ClassKind::Vanguard);canteen.inventory.clear();Item empties=makeItem(ItemKind::EmptyFlask);empties.quantity=2;canteen.addItem(empties);if(canteen.fillEmptyFlasks(9)!=2||count_if(canteen.inventory.begin(),canteen.inventory.end(),[](auto&item){return item.kind==ItemKind::Flask&&item.quantity==2;})!=1||!potableWaterTile(Water)||none_of(canteen.maps[1].tiles.begin(),canteen.maps[1].tiles.end(),[](auto&tile){return potableWaterTile(tile);}))throw runtime_error("natural-water flask refill contract");Game riverCanteen(94117);riverCanteen.configureCharacter(RaceKind::Human,ClassKind::Vanguard);riverCanteen.current=1;riverCanteen.inventory.clear();riverCanteen.addItem(makeItem(ItemKind::EmptyFlask));Point riverBank{-1,-1};for(int y=2;y<riverCanteen.mapNow().h-2&&riverBank.x<0;y++)for(int x=2;x<riverCanteen.mapNow().w-2&&riverBank.x<0;x++){Point waterPoint{x,y};if(!potableWaterTile(riverCanteen.mapNow().at(waterPoint)))continue;for(auto direction:DIRS){Point bank=waterPoint+direction;if(riverCanteen.mapNow().at(bank).walk){riverBank=bank;break;}}}if(riverBank.x<0)throw runtime_error("reachable river bank contract");riverCanteen.player=riverBank;int refillTurn=riverCanteen.turns;riverCanteen.interact();if(riverCanteen.turns!=refillTurn+1||none_of(riverCanteen.inventory.begin(),riverCanteen.inventory.end(),[](auto&item){return item.kind==ItemKind::Flask;}))throw runtime_error("river interaction refill contract");canteen.water=0;canteen.inventory.clear();canteen.addItem(makeItem(ItemKind::Flask));canteen.useItem(0);if(canteen.water!=100||none_of(canteen.inventory.begin(),canteen.inventory.end(),[](auto&item){return item.kind==ItemKind::EmptyFlask;}))throw runtime_error("reusable water flask contract");
        Game parity(94117);parity.configureCharacter(RaceKind::Elf,ClassKind::Arcanist);if(parity.inventory.size()<5||parity.learnedSpells.size()!=2||parity.skills.size()!=24)throw runtime_error("character/inventory/skill initialization");auto profile=characterProfile(RaceKind::Ashborn,ClassKind::Arcanist);if(profile.maxHp!=32||profile.maxMana!=18||profile.intellect!=17||profile.weapon.empty()||raceLore(RaceKind::Ashborn).size()<4||classLore(ClassKind::Arcanist).size()<4)throw runtime_error("character creation dossier contract");string marqueeA=marqueeText("A deliberately long single-line presentation label",18,0),marqueeB=marqueeText("A deliberately long single-line presentation label",18,2.0);if(utf8Width(marqueeA)!=18||utf8Width(marqueeB)!=18||marqueeA==marqueeB)throw runtime_error("marquee presentation contract");string splashA=parity.renderSplashFrame(1.15),splashB=parity.renderSplashFrame(3.23);if(splashA==splashB||splashA.find("\x1b[48;2;")==string::npos||splashB.find("█")==string::npos||splashA.find("L U M E N   R O G U E")==string::npos)throw runtime_error("animated splash presentation contract");set<string> branches;for(auto&node:parity.skills){branches.insert(node.branch);if(node.maxRank!=3)throw runtime_error("multi-rank skill contract");}if(branches.size()!=4)throw runtime_error("skill branches");parity.skills[12].rank=3;if(parity.spellInvestment(SpellKind::Fireball)!=3||parity.spellCost(SpellKind::Fireball)>=spellInfo(SpellKind::Fireball).cost)throw runtime_error("spell mastery");
        Game gearStacks(94117);gearStacks.configureCharacter(RaceKind::Human,ClassKind::Vanguard);gearStacks.inventory.clear();gearStacks.nextItemOrder=1;
        Item stackBlade=equipment("test iron blade",Slot::MainHand,'/',3,1);stackBlade.quantity=2;gearStacks.addItem(stackBlade);gearStacks.turns++;stackBlade.quantity=1;gearStacks.addItem(stackBlade);
        Item strongerBlade=stackBlade;strongerBlade.attack=4;gearStacks.addItem(strongerBlade);
        if(gearStacks.inventory.size()!=2||count_if(gearStacks.inventory.begin(),gearStacks.inventory.end(),[](auto&item){return item.name=="test iron blade"&&item.attack==3&&item.quantity==3;})!=1)throw runtime_error("identical equipment stacking contract");
        auto stackIndex=find_if(gearStacks.inventory.begin(),gearStacks.inventory.end(),[](auto&item){return item.attack==3;});gearStacks.useItem(size_t(stackIndex-gearStacks.inventory.begin()));
        if(count_if(gearStacks.inventory.begin(),gearStacks.inventory.end(),[](auto&item){return item.attack==3&&item.equipped&&item.quantity==1;})!=1||count_if(gearStacks.inventory.begin(),gearStacks.inventory.end(),[](auto&item){return item.attack==3&&!item.equipped&&item.quantity==2;})!=1||count_if(gearStacks.inventory.begin(),gearStacks.inventory.end(),[](auto&item){return item.attack==4;})!=1)throw runtime_error("equipment stack split and rolled-stat identity contract");
        auto comparisonOrder=gearStacks.inventoryOrder(InventorySort::Power);size_t strongerIndex=size_t(find_if(gearStacks.inventory.begin(),gearStacks.inventory.end(),[](auto&item){return item.attack==4;})-gearStacks.inventory.begin());int comparisonSelected=int(find(comparisonOrder.begin(),comparisonOrder.end(),strongerIndex)-comparisonOrder.begin());auto comparisonLines=gearStacks.inventoryLines(comparisonSelected,false,0.0,InventorySort::Power);
        if(none_of(comparisonLines.begin(),comparisonLines.end(),[](auto&line){return line.find("[2 ◆ POWER]")!=string::npos;})||none_of(comparisonLines.begin(),comparisonLines.end(),[](auto&line){return line.find("ITEM BONUSES")!=string::npos&&line.find("WORN")!=string::npos&&line.find("YOUR TOTAL")!=string::npos;})||none_of(comparisonLines.begin(),comparisonLines.end(),[](auto&line){return line.find(" ATK")!=string::npos&&line.find("+1")!=string::npos&&line.find("→")!=string::npos;})||none_of(comparisonLines.begin(),comparisonLines.end(),[](auto&line){return line.find("1–4/Tab Sort")!=string::npos;}))throw runtime_error("readable equipment comparison contract");
        auto wornIndex=find_if(gearStacks.inventory.begin(),gearStacks.inventory.end(),[](auto&item){return item.equipped;});gearStacks.useItem(size_t(wornIndex-gearStacks.inventory.begin()));if(gearStacks.inventory.size()!=2||count_if(gearStacks.inventory.begin(),gearStacks.inventory.end(),[](auto&item){return item.attack==3&&!item.equipped&&item.quantity==3;})!=1)throw runtime_error("equipment stack recombination contract");
        string oldQaWidth=getenv("LUMEN_QA_WIDTH")?getenv("LUMEN_QA_WIDTH"):"",oldQaHeight=getenv("LUMEN_QA_HEIGHT")?getenv("LUMEN_QA_HEIGHT"):"";_putenv_s("LUMEN_QA_WIDTH","160");_putenv_s("LUMEN_QA_HEIGHT","40");auto inventoryPotion=parity.inventoryLines(0,false,0.0);auto inventoryFood=parity.inventoryLines(1,false,0.0);auto powerOrder=parity.inventoryOrder(InventorySort::Power);auto newestOrder=parity.inventoryOrder(InventorySort::Newest);auto nameOrder=parity.inventoryOrder(InventorySort::Name);_putenv_s("LUMEN_QA_WIDTH",oldQaWidth.c_str());_putenv_s("LUMEN_QA_HEIGHT",oldQaHeight.c_str());auto compactInventory=parity.inventoryLines(1,true,0.0);if(inventoryPotion==inventoryFood||none_of(inventoryPotion.begin(),inventoryPotion.end(),[](auto&line){return line.find("ITEM STUDY")!=string::npos;})||none_of(inventoryPotion.begin(),inventoryPotion.end(),[](auto&line){return line.find("SORTED BY TYPE")!=string::npos;})||none_of(inventoryPotion.begin(),inventoryPotion.end(),[](auto&line){return line.find("ACQUIRED DAY")!=string::npos;})||none_of(inventoryPotion.begin(),inventoryPotion.end(),[](auto&line){return line.find("▀")!=string::npos;})||none_of(inventoryPotion.begin(),inventoryPotion.end(),[](auto&line){return !line.empty()&&line[0]=='\3';})||none_of(compactInventory.begin(),compactInventory.end(),[](auto&line){return line.find("1–4/Tab Sort")!=string::npos;})||itemPortrait(parity.inventory[0])==itemPortrait(parity.inventory[1])||powerOrder.empty()||itemPower(parity.inventory[powerOrder.front()])<itemPower(parity.inventory[powerOrder.back()])||newestOrder.empty()||parity.inventory[newestOrder.front()].acquiredOrder<parity.inventory[newestOrder.back()].acquiredOrder||!is_sorted(nameOrder.begin(),nameOrder.end(),[&](size_t left,size_t right){return parity.inventory[left].name<parity.inventory[right].name;}))throw runtime_error("scrolling and sortable inventory presentation contract");
        set<string> monsters;for(int i=2;i<=6;i++){auto&d=parity.maps[i];if(int(d.biome)!=i-1||d.provisions.size()<2||d.wallTorches.empty()||d.traps.size()<8||d.machines.empty()||d.groundItems.size()<2)throw runtime_error("biome feature generation in "+d.name);for(auto&t:d.wallTorches)if(!d.contains(t.p)||d.at(t.p).walk)throw runtime_error("wall torch mounting contract");for(auto&z:d.monsters)monsters.insert(z.name);}if(monsters.size()<14)throw runtime_error("monster variety");parity.current=2;auto&inspectMap=parity.mapNow();auto subject=find_if(inspectMap.monsters.begin(),inspectMap.monsters.end(),[](auto&monster){return monster.hp>0;});if(subject==inspectMap.monsters.end())throw runtime_error("inspect creature fixture");parity.player=parity.openNear(inspectMap,subject->p+Point{-3,0});if(parity.inspectSummary(subject->p).find(subject->name)==string::npos||parity.inspectDetails(subject->p).size()<8)throw runtime_error("live inspect and examination presentation contract");auto&terrain=parity.maps[2];Point breakable{-1,-1};for(int y=1;y<terrain.h-1&&breakable.x<0;y++)for(int x=1;x<terrain.w-1;x++)if(terrain.at({x,y}).toughness>0&&none_of(terrain.wallTorches.begin(),terrain.wallTorches.end(),[&](auto&t){return t.p==Point{x,y};})){breakable={x,y};break;}if(breakable.x<0||!parity.damageTerrain(terrain,breakable,999)||!terrain.at(breakable).walk)throw runtime_error("destructible terrain");
        auto path=filesystem::temp_directory_path()/"lumenrogue-cpp-selftest.save";a.configureCharacter(RaceKind::Ashborn,ClassKind::Lightbearer);a.skills[0].rank=2;a.skillPoints=3;a.mana=4;a.player={a.player.x-1,a.player.y};a.gold=417;a.turns=37;a.addItem(makeItem(ItemKind::EmptyFlask));uint64_t savedItemOrder=a.inventory.back().acquiredOrder;a.discovered.insert("test landmark");a.trackedQuest="stones";a.reducedMotion=true;a.preferredInventorySort=InventorySort::Newest;if(!a.save(path))throw runtime_error("save creation");auto loaded=load(path);filesystem::remove(path);if(!loaded||loaded->seed!=a.seed||loaded->gold!=417||loaded->trackedQuest!="stones"||!loaded->reducedMotion||loaded->preferredInventorySort!=InventorySort::Newest||loaded->player!=a.player||!loaded->discovered.contains("test landmark")||loaded->race!=RaceKind::Ashborn||loaded->calling!=ClassKind::Lightbearer||loaded->mana!=4||loaded->skillPoints!=3||loaded->skills.size()!=24||loaded->skills[0].rank!=2||loaded->inventory.size()!=a.inventory.size()||none_of(loaded->inventory.begin(),loaded->inventory.end(),[&](auto&item){return item.kind==ItemKind::EmptyFlask&&item.acquiredTurn==37&&item.acquiredOrder==savedItemOrder;})||loaded->nextItemOrder<=savedItemOrder||loaded->maps[2].wallTorches.size()!=a.maps[2].wallTorches.size())throw runtime_error("extended save/load round trip");
        cout<<"LumenRogue C++ self-test: campaign turn-ins, narrative, equipment, elemental effects, PCM audio, input, world, lighting, and save checks passed.\n";return true;
    }catch(const exception&e){cerr<<"LumenRogue C++ self-test failed: "<<e.what()<<'\n';return false;}
}

int main(int argc,char**argv){
    try{
        uint32_t seed=uint32_t(chrono::high_resolution_clock::now().time_since_epoch().count());optional<filesystem::path> loadPath;
        int snapshotShop=-1;int snapshotDossier=-1;bool test=false,snapshotWorld=false,snapshotMenu=false,snapshotTown=false,snapshotSplash=false,snapshotCreation=false,snapshotAncestry=false,snapshotHelp=false,snapshotInspect=false,snapshotExamine=false,snapshotDialogue=false,snapshotSkill=false,snapshotSkillScroll=false,snapshotJournal=false,snapshotEffects=false,benchmark=false;
        for(int i=1;i<argc;i++){string a=argv[i];if(a=="--snapshot-shop")snapshotShop=0;else if(a=="--snapshot-shop-sell")snapshotShop=1;else if(a=="--snapshot-shop-kettle")snapshotShop=2;else if(a=="--snapshot-shop-sigil")snapshotShop=3;else if(a=="--snapshot-character")snapshotDossier=0;else if(a=="--snapshot-spells")snapshotDossier=1;else if(a=="--snapshot-lore")snapshotDossier=2;else if(a=="--snapshot-journal")snapshotJournal=true;else if(a=="--snapshot-effects")snapshotEffects=true;else if(a=="--self-test")test=true;else if(a=="--snapshot-world")snapshotWorld=true;else if(a=="--snapshot-menu")snapshotMenu=true;else if(a=="--snapshot-town")snapshotTown=true;else if(a=="--snapshot-splash")snapshotSplash=true;else if(a=="--snapshot-creation")snapshotCreation=true;else if(a=="--snapshot-ancestry")snapshotAncestry=true;else if(a=="--snapshot-help")snapshotHelp=true;else if(a=="--snapshot-inspect")snapshotInspect=true;else if(a=="--snapshot-examine")snapshotExamine=true;else if(a=="--snapshot-dialogue")snapshotDialogue=true;else if(a=="--snapshot-skill-tree")snapshotSkill=true;else if(a=="--snapshot-skill-tree-scroll")snapshotSkillScroll=true;else if(a=="--benchmark-render")benchmark=true;else if(a=="--seed"&&i+1<argc)seed=uint32_t(stoul(argv[++i]));else if(a=="--load"&&i+1<argc)loadPath=argv[++i];}
        if(test)return Game::selfTest()?0:1;optional<Game> loaded=loadPath?Game::load(*loadPath):nullopt;if(loadPath&&!loaded){cerr<<"Could not load the requested save. No new expedition was started.\n";return 1;}Game game=loaded?std::move(*loaded):Game(seed);
        if(snapshotShop>=0){game.renderQaShop(snapshotShop);return 0;}
        if(snapshotDossier>=0){game.renderQaDossier(snapshotDossier);return 0;}
        if(snapshotJournal){game.renderQaJournal();return 0;}if(snapshotEffects){game.renderQaEffects();return 0;}
        if(snapshotWorld||snapshotMenu||snapshotTown||snapshotSplash||snapshotCreation||snapshotAncestry||snapshotHelp||snapshotInspect||snapshotExamine||snapshotDialogue||snapshotSkill||snapshotSkillScroll){if(snapshotSplash)game.renderQaSplash();else if(snapshotCreation)game.renderQaCreation();else if(snapshotAncestry)game.renderQaAncestry();else if(snapshotHelp)game.renderQaHelp();else if(snapshotInspect||snapshotExamine)game.renderQaInspect(snapshotExamine);else if(snapshotDialogue)game.renderQaDialogue();else if(snapshotSkill||snapshotSkillScroll)game.renderQaSkillTree(snapshotSkillScroll?2.1:0.0);else{game.configureCharacter(RaceKind::Ashborn,ClassKind::Arcanist);if(snapshotTown)game.renderQaTown();else game.renderQaShowcase(snapshotMenu);}return 0;}
        if(benchmark){game.configureCharacter(RaceKind::Ashborn,ClassKind::Arcanist);game.current=2;game.player=game.maps[2].start;auto begin=chrono::steady_clock::now();size_t bytes=0;for(int i=0;i<60;i++)bytes+=game.renderWorldFrame().size();double ms=chrono::duration<double,milli>(chrono::steady_clock::now()-begin).count();cout<<"60 frames: "<<ms<<" ms ("<<ms/60<<" ms/frame, "<<bytes<<" bytes)\n";return ms/60>33.3?1:0;}
        TerminalSession terminal;if(!loaded)game.createCharacterInteractive();game.run();return 0;
    }catch(const exception&e){cerr<<"Fatal error: "<<e.what()<<'\n';return 2;}
}
