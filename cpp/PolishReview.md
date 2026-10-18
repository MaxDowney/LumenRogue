# Presentation and adventure review

## Findings

- Overlay clipping can remove keyboard instructions and the selected item's details. Compact inventory spends too many rows on artwork before explaining equipment.
- The journal lists accepted quests only. It hides the available lead, the person to report to, and the relationship between the village work and the two campaign threats.
- NPCs repeat their original alarm after their problem is resolved. A conclusion-recording option saves testimony instead, and several reward conversations promise objects or services that are not awarded.
- Quest and discovery XP can sit above the level threshold until the next monster kill. Map annotations also count as visiting a quest landmark.
- Most spells use the same orange effect; fireballs never show a separate impact, healing looks like an explosion, and ranged attacks have little feedback.

## Implementation direction

Use stable frames, a reserved footer, height-aware content, and consistent selection colours. Make the journal show leads, dependencies, an explicit next step, and a tracked objective. Centralize quest acceptance/turn-in and apply visible consequences; record testimony and interpretations separately. Give spells distinct visual families, timed travel/impact phases, combat numbers, and optional synthesized sound, with reduced-motion and mute controls. Preserve old saves and verify complete campaign progression as well as small-terminal layouts.

## Delivered

- Fixed-position health/mana/food/water bars and combat information. Only descriptive text marquees; buttons and meters stay put.
- Shared stable frames, reserved control rows and 180-column wide layouts. Inventory, character loadout, creation, skill tree, spellbook, lore, help and conversations adapt to available height. Live screens and QA captures share the same layout functions.
- Explicit equipment comparison columns, visible sort shortcuts, and proportional portrait thumbnails. Short-terminal layouts retain the selected item's four stat rows. Long dialogue and dossiers can be paged; lore entries have a full-account reader.
- A journal covering all eleven campaign quests, including leads and dependencies. Contacts and next actions are explicit. Tracking and sort preference survive save/load.
- Quest acceptance and turn-in are centralized and checked. Duplicate turn-ins cannot farm rewards; every XP source can level the character immediately. Both guardian orders complete the same campaign. Actual visits, remote map annotations, testimony and interpretations are separate concepts.
- Removed reward promises that were not implemented; resolved NPC greetings respond to events in nine distinct voices. Village pledges unlock Roen's commission. Stonecross's lamps improve, Reedwater's nearby cisterns refill once, and Emberfall's blue warning lamps return to gold when both major threats are defeated. Light changes also survive reload.
- Distinct elemental visual families, delayed fireball impacts, weapon-hit effects and damage/healing numbers. Emissive effect glyphs keep their own colour instead of being washed white by their light. Nine synthesized PCM cues provide optional combat/magic/reward sound. F7 mutes; F8 reduces shake and flash lighting.
- Build script tests a candidate before updating both release locations and refuses to overwrite a running game. Failed explicit loads no longer silently start a new expedition.

## Verification — 5 September 2026

- Optimized, stripped, statically linked x64 Windows build with Zig 0.15.2: passed.
- Expanded native regression suite: passed. Covers the full eleven-quest transaction flow in both boss orders, finite scout population, real gate traversal after the commission, repeat-reward prevention, immediate quest levelling, independent testimony/conclusions, restored lamp state, travel/impact sequencing, invalid-target resource preservation, animation without actor turns, PCM integrity/amplitude/distinctness, and save preference round-trips. Existing generation, stairs, input, survival, equipment, lighting and persistence tests also pass.
- Shipped executable: 48 terminal captures across twelve screens at 60×20, 80×25, 120×30 and 160×40. Automated checks confirm important controls and selected-item details remain visible; representative captures were visually inspected. Captures are in `qa-polish/`.
- Existing `cpp/lumenrogue-cpp.save` loads successfully. Its SHA-256 remained `F10BA0E47CDA18825589D76D3F48A539AF9CAA394702D2FDDC20B3820358625E`; no player save was edited.
- Headless world-frame construction: about 1.42 ms/frame at 160×40 and 1.25 ms/frame at 200×50 in the tested scenes. These are CPU construction times, not measured terminal display rates.
- Both `cpp/LumenRogue-CPP.exe` and `bin/cpp/LumenRogue-CPP.exe` are identical, 1,576,448 bytes. SHA-256: `83C786E276D96B7D2B7601106B5DA2590D8DDDF131047FC99CC9FF9758D936E5`.

## Scope and limits

All game visuals remain terminal characters and ANSI colour; there is no graphical window or raster renderer. Visual checks render the executable's ANSI output with Cascadia Mono, rather than automating a live Windows Terminal window. Speaker playback and subjective audio quality have not been auditioned on the user's hardware. Sound is a small single-channel procedural system, not a layered audio mixer; closely spaced cues may interrupt each other. Reduced motion softens the presentation but is not a photosensitivity guarantee.
