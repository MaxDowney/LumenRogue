#pragma once

// Synthesized PCM clips stay alive for asynchronous Win32 playback.
// No sound files, background threads, or third-party runtime are needed.
enum class SoundCue { Hit, Hurt, Fire, Frost, Lightning, Arcane, Heal, Quest, Level, Count };
class SoundBank {
    using Play = BOOL (WINAPI*)(LPCSTR, HMODULE, DWORD);
    HMODULE module{};
    Play play{};
    array<vector<char>,size_t(SoundCue::Count)> clips;
public:
    bool enabled{true};
    static vector<char> synthesize(SoundCue cue){
        constexpr int rate=22050;double duration=cue==SoundCue::Fire?.8:cue==SoundCue::Level?1.0:cue==SoundCue::Quest?.6:cue==SoundCue::Heal?.65:.32;
        int samples=int(rate*duration);vector<char> wav;wav.reserve(44+samples*2);
        auto bytes=[&](const char*s){for(int i=0;i<4;i++)wav.push_back(s[i]);};
        auto word=[&](uint32_t n,int count){for(int i=0;i<count;i++)wav.push_back(char((n>>(8*i))&255));};
        bytes("RIFF");word(36+samples*2,4);bytes("WAVE");bytes("fmt ");word(16,4);word(1,2);word(1,2);word(rate,4);word(rate*2,4);word(2,2);word(16,2);bytes("data");word(samples*2,4);
        uint32_t noiseState=0xA17C9u+uint32_t(cue);double smooth=0;
        for(int i=0;i<samples;i++){
            double t=i/double(rate),u=t/duration,attack=min(1.0,t/.006),tail=pow(1-u,2),value=0;noiseState=noiseState*1664525u+1013904223u;double noise=(noiseState>>8)/8388607.5-1;smooth=smooth*.87+noise*.13;
            auto sine=[&](double hz){return sin(6.283185307*hz*t);};
            switch(cue){
                case SoundCue::Hit:value=.45*noise*exp(-t*38)+.45*sine(180)*exp(-t*18)+.2*sine(1900)*exp(-t*28);break;
                case SoundCue::Hurt:value=.55*smooth+.45*sin(6.283185307*(115*t-95*t*t));break;
                case SoundCue::Fire:value=.75*smooth+.4*sin(6.283185307*(90*t-42*t*t))+.12*noise*exp(-t*17);break;
                case SoundCue::Frost:value=.22*noise+.35*sine(1320)+.22*sine(2087)*exp(-t*7)+.16*sine(2960);break;
                case SoundCue::Lightning:value=noise*(.25+.55*pow(max(0.0,sine(43)),8))+.25*sine(70);break;
                case SoundCue::Arcane:value=.36*sin(6.283185307*(330*t+580*t*t))+.3*sine(660)+.2*sine(993);break;
                case SoundCue::Heal:value=.28*sine(523.25)+.25*sine(659.25)+.22*sine(783.99);break;
                case SoundCue::Quest:case SoundCue::Level:{int note=min(3,int(u*4));double hz=note==0?523.25:note==1?659.25:note==2?783.99:1046.5,phase=fmod(u*4,1.0),ramp=min(1.0,phase*14)*min(1.0,(1-phase)*10);value=(.5*sine(hz)+.14*sine(hz*2))*ramp;break;}
                default:break;
            }
            // Headroom and short ramps prevent clipping and harsh clicks.
            int16_t pcm=int16_t(clamp(value*attack*tail*.24,-.8,.8)*32767);word(uint16_t(pcm),2);
        }
        return wav;
    }
    SoundBank(){module=LoadLibraryExW(L"winmm.dll",nullptr,LOAD_LIBRARY_SEARCH_SYSTEM32);if(module)play=reinterpret_cast<Play>(GetProcAddress(module,"PlaySoundA"));for(size_t i=0;i<clips.size();i++)clips[i]=synthesize(SoundCue(i));}
    ~SoundBank(){if(play)play(nullptr,nullptr,0);if(module)FreeLibrary(module);}
    void stop(){if(play)play(nullptr,nullptr,0);}
    void emit(SoundCue cue){if(enabled&&play)play(clips[size_t(cue)].data(),nullptr,0x0001|0x0002|0x0004);}
    bool available()const{return play!=nullptr;}
};
static SoundBank& soundBank(){static SoundBank bank;return bank;}
