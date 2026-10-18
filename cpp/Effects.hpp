#pragma once

static Color effectColour(EffectStyle style){
    switch(style){case EffectStyle::Frost:return{92,192,255};case EffectStyle::Lightning:return{143,219,255};case EffectStyle::Arcane:return{193,116,255};case EffectStyle::Radiant:return{255,212,100};case EffectStyle::Poison:return{124,218,83};case EffectStyle::Stone:return{215,159,105};case EffectStyle::Healing:return{112,237,175};case EffectStyle::Steel:return{233,203,148};default:return{255,99,27};}
}

static EffectStyle spellStyle(SpellKind spell){
    switch(spell){case SpellKind::FrostLance:case SpellKind::IceStorm:return EffectStyle::Frost;case SpellKind::ChainLightning:return EffectStyle::Lightning;case SpellKind::ArcaneBeam:case SpellKind::PrismaticOrb:case SpellKind::ShadowBolt:case SpellKind::Blink:return EffectStyle::Arcane;case SpellKind::HealingLight:case SpellKind::Sanctuary:return EffectStyle::Healing;case SpellKind::SunLance:case SpellKind::SolarRay:case SpellKind::Smite:case SpellKind::LuminousNova:case SpellKind::ShieldFlare:return EffectStyle::Radiant;case SpellKind::PoisonCloud:case SpellKind::ThornVolley:return EffectStyle::Poison;case SpellKind::Stonebreak:case SpellKind::SeismicWave:case SpellKind::GroundQuake:case SpellKind::BattleCry:return EffectStyle::Stone;case SpellKind::Multishot:case SpellKind::MarkedShot:return EffectStyle::Steel;default:return EffectStyle::Fire;}
}

void Game::emitImpact(Point p,EffectStyle style,int radius,double delay){
    Color colour=effectColour(style);effects.push_back({{},p,lerpColor(colour,{255,255,245},.78),colour,visualSeconds()+delay,style==EffectStyle::Steel?.32:style==EffectStyle::Healing?1.15:1.05,radius,false,style});
}

static void effectCell(const VisualEffect&e,Point p,double now,char&glyph,Color&fg,Color&bg){
    double age=now-e.started;if(age<0||age>e.duration)return;double t=age/e.duration;int frame=int(age*24),seed=e.center.x*731+e.center.y*197;
    if(e.projectile&&!e.path.empty()){
        size_t head=min(e.path.size()-1,size_t(t*e.path.size()));bool beam=e.style==EffectStyle::Lightning;int tail=beam?int(head):5;
        for(int trail=0;trail<=tail&&int(head)-trail>=0;trail++){Point point=e.path[size_t(int(head)-trail)];if(point!=p)continue;glyph=trail==0?'*':e.style==EffectStyle::Frost?'-':beam?(frame%2?'~':'='):'.';fg=trail==0?e.hot:e.cool.scaled(beam?.9:1-trail*.13);bg=e.cool.scaled(trail==0?.24:.08);return;}
        if(beam&&head>0)for(size_t i=0;i<=head;i++){Point point=e.path[i];if(abs(p.x-point.x)==1&&abs(p.y-point.y)==1&&((int(i)+frame)%3)==0){glyph='/';fg=e.cool;return;}}
        return;
    }
    double x=p.x-e.center.x,y=(p.y-e.center.y)*1.8,d=sqrt(x*x+y*y),radius=max(1,e.radius)*1.7;
    if(e.style==EffectStyle::Healing||e.style==EffectStyle::Radiant){
        double ring=.7+t*radius;if(abs(d-ring)<.55&&t<.85){glyph=e.style==EffectStyle::Healing?'+':'*';fg=lerpColor(e.hot,e.cool,t);bg=e.cool.scaled(.12*(1-t));}else if(d<radius&&((p.x*7+p.y*3+frame/3)%11==0)){glyph='+';fg=e.cool.scaled(1-t*.45);}return;
    }
    if(e.style==EffectStyle::Steel){if(d<1.8&&t<.75){glyph=(p.x+p.y+frame)%2?'/':'\\';fg=lerpColor(e.hot,e.cool,t);bg=e.cool.scaled(.09);}return;}
    if(e.style==EffectStyle::Frost){double shell=t*radius*1.4;if(d<=shell&&d>shell-1.1&&t<.85){glyph=(p.x+p.y)%3?'*':'+';fg=e.hot;bg=e.cool.scaled(.16);}else if(d<shell&&d<radius&&((p.x*3+p.y*7)%5==0)){glyph=':';fg=e.cool.scaled(1-t*.65);}return;}
    if(e.style==EffectStyle::Poison){double spread=radius*(.4+.6*t);if(d<spread&&(p.x*13+p.y*7+frame/3)%4!=0){glyph=(p.x+p.y+frame/4)%3?';':'~';fg=e.cool.scaled(1-t*.5);bg=e.cool.scaled(.08);}return;}
    if(e.style==EffectStyle::Arcane||e.style==EffectStyle::Lightning){double angle=atan2(y,x);if(abs(d-radius*t)<.6||((int((angle+3.15)*5+frame)%7==0)&&d<radius*(1-t*.3))){glyph=e.style==EffectStyle::Lightning?'~':'+';fg=lerpColor(e.hot,e.cool,t);bg=e.cool.scaled(.16*(1-t));}return;}
    // Fire and stone: a hot core, one expanding pressure ring, then ballistic debris.
    double shock=t*(radius+5),core=radius*pow(1-t,.65);if(t<.55&&abs(d-shock)<.65){glyph=e.style==EffectStyle::Stone?':':'o';fg=lerpColor(e.hot,e.cool,t);bg=e.cool.scaled(.19*(1-t));}
    if(d<core&&((p.x*11+p.y*17+frame)%4!=0)){glyph=e.style==EffectStyle::Stone?'%':(t<.18?'#':t<.46?'*':':');fg=lerpColor(e.hot,e.cool,min(1.0,t*1.6));bg=e.cool.scaled(.34*(1-t));}
    for(int spark=0;spark<20;spark++){double angle=spark*2.399+seed*.01,speed=radius+2+(spark%5);Point point{e.center.x+int(lround(cos(angle)*speed*t)),e.center.y+int(lround((sin(angle)*speed*t+3*t*t)/1.8))};if(point==p&&t<.9){glyph=t<.45?'*':'.';fg=lerpColor(e.hot,e.cool,t).scaled(1-t*.45);return;}}
}

void Game::renderQaEffects(){
    configureCharacter(RaceKind::Ashborn,ClassKind::Arcanist);current=0;auto&m=mapNow();player={m.w/2,m.h/2};for(int y=player.y-8;y<=player.y+8;y++)for(int x=player.x-27;x<=player.x+27;x++)if(m.contains({x,y}))m.at({x,y})=Floor;
    fill(m.explored.begin(),m.explored.end(),1);double now=visualSeconds();for(int i=0;i<5;i++){EffectStyle style=array<EffectStyle,5>{{EffectStyle::Fire,EffectStyle::Frost,EffectStyle::Lightning,EffectStyle::Arcane,EffectStyle::Healing}}[size_t(i)];Point p=player+Point{(i-2)*8,0};emitImpact(p,style,3);effects.back().started=now-.3;}
    message="EFFECT STUDY · Fire / Frost / Lightning / Arcane / Healing · Turn-based actors, continuous effects";cout<<renderWorldFrame(now);
}
