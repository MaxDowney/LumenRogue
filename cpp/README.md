# LumenRogue native C++ edition

This directory contains the dependency-free C++20 port of LumenRogue. It is a native implementation rather than a .NET launcher or wrapper.

## Distribute

Download the standalone executable from the project's
[GitHub Releases](https://github.com/MaxDowney/LumenRogue/releases).
The `bin/cpp/` path below is created by a local build, not checked into Git.

Distribute only `bin/cpp/LumenRogue-CPP.exe`. The release build statically links its C++ runtime and does not require .NET, Zig, Visual C++ redistributables, or external assets. It targets 64-bit Windows 10/11 and uses the built-in Windows console and sound APIs plus ANSI true-color output. No additional DLLs need to be shipped.

The optional `lumenrogue-cpp.save` file is created in the player's current working directory when F5 is pressed. It does not need to be shipped.

## Run

```powershell
.\bin\cpp\LumenRogue-CPP.exe
.\bin\cpp\LumenRogue-CPP.exe --seed 94117
.\bin\cpp\LumenRogue-CPP.exe --load .\lumenrogue-cpp.save
.\bin\cpp\LumenRogue-CPP.exe --self-test
```

Movement supports the arrow keys, WASD, HJKL, YUBN diagonals, and the full numpad. With Num Lock on, use 7/9/1/3 for diagonals; with Num Lock off, Home/PgUp/End/PgDn work as the same four directions. Numpad 5 or Space waits for one turn. These direction keys also work while targeting spells and inspecting the map.

## Build

Use Zig 0.15.2 (the tested compiler) on `PATH`, or the local `.zig-full/zig-x86_64-windows-0.15.2` compiler, then run:

```powershell
.\cpp\build.ps1
```

You can pass an explicit compiler location:

```powershell
.\cpp\build.ps1 -Compiler "C:\Tools\zig\zig.exe"
```

The build uses Zig's Clang C++ frontend and MinGW Windows headers to create a stripped, statically linked x64 executable. It compiles a candidate, runs the regression suite, and updates both `cpp/LumenRogue-CPP.exe` and `bin/cpp/LumenRogue-CPP.exe`. If a release is running, it keeps the tested candidate and asks you to close the game before installation. It never closes the game or edits a save.

Character, monster, and inspection portraits originate as generated PNG source icons under `assets/portrait_sources`, but the game never displays raster graphics. `portrait_bitmap.py` converts each source into a 32×12 true-colour terminal bitmap made entirely from ANSI-styled Unicode half-block characters and writes the embedded `GeneratedPortraits.hpp`. Monster and inspection sources use deliberately broad silhouettes and sparse detail so their subject survives the tiny terminal conversion. Run the converter from `cpp/` when changing a source portrait; Pillow is only a development dependency and is not required by the game or release build.

## Renderer QA

The executable includes `--snapshot-world`, `--snapshot-town`, `--snapshot-menu`, `--snapshot-splash`, `--snapshot-creation`, `--snapshot-ancestry`, `--snapshot-help`, `--snapshot-inspect`, `--snapshot-examine`, `--snapshot-dialogue`, `--snapshot-skill-tree`, `--snapshot-skill-tree-scroll`, `--snapshot-journal`, `--snapshot-character`, `--snapshot-spells`, `--snapshot-lore`, `--snapshot-effects`, and `--benchmark-render` modes. `ansi_snapshot.py` turns terminal output into PNG and text captures. `presentation_qa.py <exe>` checks 12 screens at 60×20, 80×25, 120×30, and 160×40, including visible controls and selected-item details. Python and Pillow are development dependencies only.

## Presentation and campaign polish

### Shops and terminal resizing

- Shops have **Buy** and **Sell** tabs. **B/S** selects a tab; **Tab** switches between them. **Up/Down** selects an item, **+/-** changes quantity, **Enter** trades, **I** opens your pack, and **Esc** leaves. Trades keep the counter open.
- The Brass Badger specializes in arms and armour; the Copper Kettle sells provisions and remedies; Cinder and Sigil carries magical supplies and equipment. All three buy unequipped inventory items at 40% of retail, rounded down to whole gold (minimum one for goods with a value). Equipped items must be removed in your pack before selling.
- The selected item gets a terminal-bitmap illustration, unit price, quantity, checkout total and remaining-gold preview. Equipment compares **Worn / Item / Change** for attack, defence, spell power and light. Identical purchases stack. Trading itself does not advance time; using/equipping something from your pack still does. Counters are open 07:00–18:00.
- The trading panel has a stable, height-capped layout, so a large terminal shows the town around a composed counter rather than stretching nine lines over the entire screen. Wide screens include a merchant portrait; shorter screens prioritize prices and all four stat rows. Selected long item names and receipts marquee.
- One alternate-screen session now covers splash, creation and play. Each composed frame uses one viewport measurement, redraws cleanly after size changes, and disables automatic wrapping at the right edge to prevent bottom-row output from scrolling the HUD away. Original terminal settings are restored on exit.

`shop_qa.py` checks four shop views at seven sizes, from 60×20 to 220×85, including wide/short windows. Snapshot flags: `--snapshot-shop`, `--snapshot-shop-sell`, `--snapshot-shop-kettle`, and `--snapshot-shop-sigil`.

For native Windows resize/input QA, compile `terminal_probe.cpp` with the same options and compiler as `LumenRogue.cpp`, naming the result `cpp/terminal_probe.exe`, then run `terminal_resize_qa.py`. The probe includes the real game code and runs in its own hidden Windows pseudoconsole. Tests exercise full-screen startup, resizing during creation and gameplay, interactive buying/selling, native HUD/map/footer cell positions, and restoration of console settings. They never attach to a player's game or write a player save.

### Other presentation and campaign improvements

- Health, mana, food and water bars stay fixed; essential HUD controls no longer scroll away. Longer names and descriptions still use marquees.
- Stable workspace frames, reserved control footers, larger 180-column interiors, and shared layouts for the live game and its QA captures. Small portraits are proportionally resampled from the embedded bitmaps rather than flattened.
- Inventory sorting: **1 Type, 2 Power, 3 Newest, 4 Name**, or **Tab** to cycle. Equipment compares **Worn / Item / Change / Your Total** in four aligned stat rows. Identical unequipped gear stacks; equipping takes one copy.
- **Tab** opens the investigation journal. It lists available leads, active work, reports and prerequisites; **Enter** tracks a quest on the HUD. Each quest names the next action and the person to report to. Rewards are paid once, and all XP sources can trigger levelling immediately.
- Map annotations mark locations without counting as visits. Testimony and interpretation are separate records. Village reports grant their actual pledges, restore Stonecross lighting or replenish Reedwater's cisterns where appropriate, and open the undercroft through Roen's commission. Resolved NPC conversations acknowledge the outcome in distinct voices.
- **PgUp/PgDn** pages long dialogue and character dossiers. In the lore browser, **Enter** opens the full account, which can be scrolled. Help also supports paging and scrolling on short terminals.
- Element-specific real-time visuals: fire pressure rings and embers, frost crystals, lightning arcs, arcane patterns, poison clouds, healing rings, weapon strikes, and floating damage/healing numbers. Fireballs have a separate travel phase and a two-tile impact blast. Actors still advance only on game turns.
- **F7** toggles synthesized sound effects. **F8** toggles reduced motion, removing camera shake, keeping damage labels stationary, and softening flash lighting. Nine short PCM cues cover combat, elemental magic, healing, quest rewards and levelling. Playback uses one asynchronous Windows sound channel; cues can interrupt each other. No recordings or external sound assets are required.
- Native saves retain the tracked quest, inventory sort and reduced-motion preference. Older saves still load; a failed explicit load now reports an error instead of silently starting a new character.

## Native parity systems

- Reproducible `--seed` world generation for town, expansive wilderness, six main dungeon depths, four villages/castle sites, and site bosses.
- A real-time, true-colour lighting showcase splash screen, followed by three-stage character creation with ancestry lore, class dossiers, portraits, final attributes, starting equipment, spell previews, and confirmation.
- Four ancestries and four classes with distinct attributes, starting equipment, mana profiles, class spell lists, and playstyle guidance.
- A four-branch, 24-node class skill constellation with branch-coloured node cards, active and dormant connectors, rank pips, level gates, linked prerequisites, spell unlocks, and mastery bonuses. Its selected-node dossier shows the full path, per-rank effect, investment state, and next requirement at a glance.
- A slot-based pack and equipment system, consumables, monster-themed weapon and armour drops, shops, pickup, character sheet, and spellbook. The responsive pack browser pairs its scrolling selection with embedded 32x12 true-colour item studies, live category/quality/quantity notes, equipment stat comparisons, persistent acquisition dates, and non-destructive Type/Power/Newest/Name sort views.
- Twenty-five targetable spells spanning projectiles, beams, explosions, healing, wards, teleportation, terrain destruction, and persistent elemental surfaces.
- Five procedurally distinct dungeon biomes, each with its own masonry, palette, reflectivity, monsters, hazards, special rooms, provisions, traps, and machinery.
- Clock-calibrated survival: one ration and one filled flask sustain roughly 24 in-game hours, empty reserves impose weakness, and untreated hunger or thirst becomes fatal around day four. Drinking leaves reusable empty flasks that can be refilled from rivers, wells, springs, cisterns, and dungeon water sources.
- Wall-mounted torches that can be taken, carried, and remounted only on walls; closed doors can be opened and closed to reshape light.
- Real-time true-color lighting animation over a turn-based simulation: independently phased town lamps and wall torches, flame-driven intensity/radius/colour-temperature flicker, five-ray sub-cell visibility sampling, soft penumbrae, RGB ambient light, directional sunlight, chromatic diffraction fringes, material bounce, smoke attenuation, traveling spell cores and trails, layered explosions, sparks, and camera shake.
- Responsive Unicode/extended-glyph terrain, animated biome and surface details, terrain-specific coloured cell backgrounds, explored-area map, live cursor inspection and targeting, generated true-colour text-bitmap ancestry/calling portraits, large species-specific creature portraits, feature artwork and flavour-text examination cards, trap search, absolute-row frame composition, and synchronized terminal rendering.
- Long skill names marquee inside the selected fixed-width node card without shifting branches, rank pips, or connector lines; wide terminals show all four disciplines while compact terminals focus on one branch with quick 1-4 switching.
- A consistent two-tier overlay system: compact decision dialogs and stable screen-relative workspace frames. Fullscreen workspaces expand to a 180-column interior, remain fixed while selections change, and give their extra width to inventory, character creation/loadout, dialogue, skill-tree, map, journal, and help layouts; lighting continues animating behind every interface.
- Post-generation flood-fill audits and automatic route repair for every critical landmark, entrance, stair, NPC, and objective.
- Day/night clock, outdoor visibility, local RGB illumination, directional sunlight, and separate light-only occlusion by monsters and solid obstacles.
- Emberfall is generated as a recognizable town plan with a civic lantern square, named wards, connected streets, enterable homes and civic buildings, shop interiors, doors, lit windows, gardens, a guild, forge, town hall, pantry, well, and independently flickering street lamps.
- Two-portrait conversations keep the player and NPC visible together and use navigable dialogue trees for local knowledge, work, rumours, quest negotiation, explicit acceptance, progress reports, rewards, completion, map discoveries, and persistent lore outcomes.
- A cohesive central campaign follows Emberfall's blue-light warning circuit into an erased sixth depth. The root, road, river, and lamp branches of the broken Lantern Compact connect the three village quest lines, Valeward's Oathless Regent, and the Foundation Seal; either major threat can be confronted first without breaking the conclusion.
- Nine principal NPCs have distinct professional perspectives, speech patterns, personal stakes, evidence, interpretations, quest responses, and examination text. Environmental clues in homes, civic buildings, wilderness landmarks, special dungeon rooms, machinery, equipment, and boss chambers corroborate their accounts.
- Towns, villages, Castle Valeward, NPC schedules, shop hours, order-independent regional quest chains, consequences, lore, landmarks, and fast travel. Objectives completed before accepting their quest are reconciled automatically, and locked routes identify the exact outstanding requirement.
- Territorial and nocturnal creatures, pack alerts, readable Strike/Shoot intentions, and Block/Dodge/Ward reactions.
- Faction materials, settlement crafting, supplies, equipment upgrades, boss relics, and persistent dungeon clears.
- Versioned native save/load that regenerates the world from its seed before restoring exploration, monsters, quests, discoveries, character progression, inventory/equipment and pickup chronology, dungeon features, items, and elemental state; older LRCPP2 saves remain loadable.
