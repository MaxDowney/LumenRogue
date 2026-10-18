// Included inside Game::selfTest; no player files or live game are touched.
{
    auto require=[](bool ok,const string&why){if(!ok)throw runtime_error("shops: "+why);};
    auto arms=shopDefinition("Brass Badger"),foodShop=shopDefinition("Copper Kettle"),magic=shopDefinition("Cinder and Sigil");
    require(arms.stock.size()>=15&&foodShop.stock.size()>=8&&magic.stock.size()>=12,"shop variety");
    require(arms.stock.front().item.equippable()&&foodShop.stock.front().item.kind==ItemKind::Ration&&magic.stock.front().item.kind==ItemKind::ManaTonic,"shop specialities");
    for(auto&shop:{arms,foodShop,magic})for(auto&offer:shop.stock)require(offer.price>resaleValue(offer.item)&&resaleValue(offer.item)>0,"buy/sell arbitrage or unsellable stock");
    Game customer(94117);customer.configureCharacter(RaceKind::Human,ClassKind::Vanguard);customer.inventory.clear();customer.gold=1000;
    require(customer.buyFromShop(arms,0,3)&&customer.gold==904&&customer.inventory.size()==1&&customer.inventory[0].quantity==3&&!customer.inventory[0].equipped,"bulk purchase");
    require(customer.buyFromShop(arms,0,2)&&customer.inventory.size()==1&&customer.inventory[0].quantity==5,"repeat purchases do not stack");
    auto acquired=customer.inventory[0].acquiredOrder;
    require(customer.sellToShop(arms,0,2)&&customer.inventory[0].quantity==3&&customer.gold==864&&customer.inventory[0].acquiredOrder==acquired,"partial sale changed identity or money");
    int coins=customer.gold;require(!customer.buyFromShop(arms,0,99)&&customer.gold==coins&&customer.inventory[0].quantity==3,"unaffordable purchase mutated inventory");
    require(!customer.buyFromShop(arms,999,1)&&!customer.buyFromShop(arms,0,0)&&!customer.buyFromShop(arms,0,-1)&&!customer.buyFromShop(arms,0,100),"invalid purchase accepted");
    require(!customer.sellToShop(arms,999,1)&&!customer.sellToShop(arms,0,0)&&!customer.sellToShop(arms,0,4),"invalid sale accepted");
    customer.inventory[0].equipped=true;require(!customer.sellToShop(arms,0,1)&&customer.gold==coins&&customer.inventory[0].quantity==3,"equipped item sold");customer.inventory[0].equipped=false;
    require(customer.sellToShop(arms,0,3)&&customer.inventory.empty()&&customer.gold==900,"full stack sale");require(customer.turns==0,"trading moves actors or clock");
    customer.turns=110;coins=customer.gold;require(!customer.buyFromShop(foodShop,0,1)&&customer.gold==coins,"closed shop traded");customer.turns=0;
    require(customer.buyFromShop(foodShop,0,4)&&customer.buyFromShop(magic,0,2),"consumable purchase");
    auto path=filesystem::temp_directory_path()/("lumen-shop-test-"+to_string(GetCurrentProcessId())+".save");require(customer.save(path),"test save failed");auto reloaded=load(path);filesystem::remove(path);
    require(reloaded&&reloaded->gold==customer.gold&&reloaded->inventory.size()==2&&reloaded->inventory[0].quantity==4&&reloaded->inventory[1].quantity==2,"trades do not survive save/load");
    customer.inventory.clear();auto empty=customer.shopLines(arms,true,500,1,0,"");require(any_of(empty.begin(),empty.end(),[](auto&line){return line.find("YOUR PACK IS EMPTY")!=string::npos;}),"empty sell tab");
    string oldWidth=getenv("LUMEN_QA_WIDTH")?getenv("LUMEN_QA_WIDTH"):"",oldHeight=getenv("LUMEN_QA_HEIGHT")?getenv("LUMEN_QA_HEIGHT"):"";
    _putenv_s("LUMEN_QA_WIDTH","120");_putenv_s("LUMEN_QA_HEIGHT","30");
    {ViewportFrame frame;require(ViewportFrame::size()==pair{120,30},"initial frame size");_putenv_s("LUMEN_QA_WIDTH","220");_putenv_s("LUMEN_QA_HEIGHT","85");{ViewportFrame overlay;require(ViewportFrame::size()==pair{120,30},"resize split world and overlay geometry");}}
    require(ViewportFrame::size()==pair{220,85},"next frame ignores resize");_putenv_s("LUMEN_QA_WIDTH",oldWidth.c_str());_putenv_s("LUMEN_QA_HEIGHT",oldHeight.c_str());
    require(frameReset(221,86).find("?7l")!=string::npos&&frameReset(221,86).empty()&&!frameReset(120,30).empty(),"resize redraw and no-wrap contract");
}
