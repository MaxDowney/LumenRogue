# LumenRogue — earlier implementation notes

These notes preserve the earlier C# edition and design history. For the current
Windows C++ release, see the [project overview](../README.md) and
[native edition guide](../cpp/README.md).

A true-color terminal roguelike built around dynamic light. The world is strictly turn-based, while its renderer runs continuously: flames flicker, spells cast moving colored light down corridors, shockwaves throw sparks and smoke, and explosions can fracture or destroy vault walls. A synchronized 24-hour world clock drives outdoor dawn, daylight, dusk, and night. Biomes carry their own ash, rain, spores, glints, embers, and heat shimmer; creatures idle and shed motes; combat leaves blood, soot, frost, footprints, cracks, and crystal fragments behind.

## Native C++ edition

A dependency-free native C++20 rewrite is the current release. It is a stripped, statically linked 64-bit Windows executable and can be distributed by itself—no .NET runtime, DLLs, or asset folder are required. Native build and distribution details are in [cpp/README.md](../cpp/README.md).

## Run

Requires the .NET 8 SDK and a terminal with ANSI true-color support (Windows Terminal, modern PowerShell, iTerm2, most Linux terminals).

```powershell
dotnet run
```

Generate a reproducible world with `dotnet run -- --seed 94117`. The seed is shown in the HUD. Start from a save with `dotnet run -- --load lumenrogue-save.json`.

For a standalone release build:

```powershell
dotnet publish -c Release
```

Run the deterministic gameplay/map smoke tests with:

```powershell
dotnet run -c Release -- --self-test
```

The game needs a terminal of at least 60×20; 100×35 or larger is recommended. UTF-8/CP437-style block glyphs are used for walls, cracks, rubble, smoke, and dense effects.

## Controls

- Move: arrow keys, WASD, or HJKL
- Diagonals: Y/U/B/N or Home/PageUp/End/PageDown
- Quick-cast first learned spell: F
- Open spellbook: Z; select a spell, move the targeting cursor, and press Enter
- Pack and equipment: I; arrows select, Tab cycles sorting, 1–4 sort by type/power/newest/name, Enter equips/uses, D drops
- Pick up ground items: G
- Rest: R (advances up to six safe turns, healing HP and mana)
- Character statistics: C
- Explored area map: M (full-floor overview; does not consume a turn)
- Fast travel: F2 (discovered outdoor landmarks; costs one hour, food, and water)
- Settlement crafting: F3 (turn creature materials into supplies and equipment)
- Lore journal: F4
- Quick-save / quick-load: F5 / F9
- Prepared reaction: F6 cycles Block, Dodge, Ward
- Quest journal: Tab (accepted work, progress, giver, and rewards; does not consume a turn)
- Shutter or uncover your lantern: O (consumes one turn)
- Search for nearby secret doors and traps: V (consumes one turn)
- Class skill tree: T; arrows move through linked branches and Enter invests a rank
- Inspect terrain, creatures, and loot: X; move cursor or press Tab
- Drink potion: P (quick use)
- Talk to residents: bump into them or press E/Enter while nearby
- Enter shops/gather provisions/use stairs/take wall torches/operate machinery/close doors: E or Enter
- Wait one turn: Space or period
- Help: F1 or `?`
- Quit: Q or Escape opens a safe-default confirmation dialog

Character creation offers Human, Dwarf, Elf, and Ashborn races plus Vanguard, Arcanist, Ranger, and Lightbearer classes. Each combination changes attributes, combat values, mana, light, starting supplies, and spell access.

The HUD keeps health, mana, hunger, and thirst bars fixed at the top of the screen, together with experience and current dungeon depth. Messages and contextual controls remain at the bottom.

Walk onto a `$` shop entrance in Emberfall and press E. The dungeon entrance is marked `>`. Monsters only act after you walk, attack, wait, rest, eat, drink, gather supplies, pick up an item, or cast; animated light never advances the simulation. Hunger and thirst are calibrated to the world clock: a full meter represents roughly 24 hours, so one ration and one filled flask cover about one day. Reaching empty imposes combat weakness; slow attrition then exhausts a full health pool over approximately three further days. Drinking keeps the empty flask. Press E beside River Sere or another potable river, well, spring, cistern, or dungeon water source to drink directly or refill every empty flask the source can supply. Emberfall has a community pantry and town well, while every procedural dungeon floor guarantees both a biome-themed food cache and water source. Safe rest consumes only the time shown by the clock—there is no additional hidden provision tax.

The west gate on Emberfall's main road is marked `=`. Walk through it to enter the Ashwind March, a persistent 240×120 wilderness region under the same outdoor day/night sky as town. The old west road, branch roads, and River Sere bridges provide dependable navigation across almost 29,000 terrain cells. Exploration away from the road reveals a broken watchtower, whispering standing stones, a wayfarer's camp, hillside spring, abandoned berry orchard, supplies, and hidden loot. Thorn wolves, briar boars, goblin scouts, brigands, marsh spiders, and mossback trolls inhabit the region. The eastern `=` road returns directly to Emberfall; wilderness travel never alters the main dungeon's depth.

Three inhabited villages are built directly into the overworld: hedge-ringed Bramblewick in the northeast, the roadworks settlement of Stonecross south of River Sere, and waterside Reedwater in the west. Each has named residents with local dialogue, a distinct working shop, lamplight, a village well, and roads connecting it to the wider March. Residents follow work, supper, and sleep schedules; shops trade from 07:00 to 18:00. Castle Valeward occupies the southwest: its curtain wall, gate, courtyard, great hall, guards, castellan, cistern, quest commission, and sealed undercroft are physically traversable rather than represented by a menu.

Four independent dungeon sites complement Emberfall's original five-depth expedition: Hollowroot Mine, the Sunken Barrow, Moonwell Cavern, and Castle Valeward's undercroft. Their `>` entrances are found in different parts of the wilderness and each opens a persistent generated dungeon with its own biome, creatures, provisions, mechanisms, loot, and explored map. Returning by the entry stair places you beside the exact overworld entrance you used. These sites do not splice into or alter the main dungeon's depth sequence.

Emberfall is now arranged as a readable settlement around Civic Lantern Square: eight enterable buildings, named wards, shops, Town Hall, the Lamplighters Guild, homes, gardens, a pantry, a well, doors, lit windows, and sixteen street lamps all connect to a proper road and lane network. Gold `?` plaques identify every building; approaching one prints its name and services, while E or Inspect reads it in full. Interiors contain building-specific furniture and working details—hearths, beds, tables, archives, an anvil and quench trough, rune apparatus, lamp tools, weapon racks, and armour stands—instead of undifferentiated floor. Five named residents move between distinct work, evening, and home positions.

Bump into a resident or press E nearby to open a branching conversation. The dialogue screen keeps your ancestry/calling portrait and the speaker's role-specific portrait visible together, while response trees cover local places, danger, work, rumours, maps, and any available quest. Every named resident has distinct prompts, diction, local knowledge, humour, conclusions, quest explanations, reward language, and farewell. Recorded rumours become lore entries. When a resident promises to put places on the map, those landmarks are now both labelled and visibly revealed at their real coordinates. Quest acceptance and reporting are explicit choices rather than one-shot text panels. Arrow keys select a response, Enter speaks it, and number keys 1–9 choose and speak immediately. New work is indicated by a restrained gold pulse; a green pulse means the resident is holding a completed quest reward. Objectives update only after a quest is accepted, and rewards are claimed by returning to its giver. Tab opens the journal without passing a turn.

The dungeon contains crystal grottoes, flooded moon shrines, collapsed armories, and luminous fungal gardens. Its twenty-creature bestiary ranges from candle kobolds, glassfang spiders, slimes, cultists, gargoyles, salamanders, umbra hounds, and mimics to crown liches and furnace demons. Bombs and Stonebreak can demolish vault terrain, doors, and traps; ordinary fireballs weaken walls into visible cracks.

## Light, detection, and monster ecology

The HUD clock begins at 07:00. Every action advances world time by six minutes, making one complete day 240 turns; resting advances six turns and therefore 36 minutes. The clock continues underground, but dungeon ambient light remains independent of the sky. Outdoors, sunrise and sunset blend through dedicated sky palettes, midday supplies strong ambient illumination, and night falls to cool moonlit darkness. Dawn and sunset generate clear messages when crossed.

Outdoor time is mechanically meaningful. Natural illumination feeds the same deterministic exposure calculation used by monster senses: daylight makes long-range sight easier, while darkness makes an extinguished lantern genuinely useful for stealth. Outdoor vision expands from a 24-tile night horizon to roughly 50 tiles at midday, with the fully clear central radius growing from 9 to 19 tiles. Day terrain is deliberately brighter than its base material colors instead of merely being less dark. Player lanterns, street lamps, village lamps, castle lights, campfires, and creature light still contribute locally at every hour, becoming comparatively important after sunset. Rendering and detection use the same turn-based solar curve, so the clock, visible brightness, `HIDDEN`/`DIM`/`EXPOSED` state, and enemy sight never disagree.

The HUD reports `HIDDEN`, `DIM`, `EXPOSED`, or `HUNTED`, together with current noise. Gameplay illumination is deterministic and updates only on turns; the continuous visual flicker never changes whether a creature detects you while you stand still. Shuttering the lantern reduces exposure but also removes its useful light.

Creatures combine sight, light sensitivity, noise, scent, and vibration. Once alerted, they pursue the last place they saw or heard you rather than receiving your position for free. Wilderness creatures have territories, day/night activity, pack alerts, and predator/rival relationships. Strike and ranged intentions are telegraphed for a full player turn; inspect reports intent and F6 prepares Block, Dodge, or Ward. Intelligent creatures open doors, brutes smash locks, kobolds extinguish flames, spiders leave webs, infernal creatures leave fire, liches raise nearby corpses, and rival factions can attack one another. Mimics and gargoyles remain disguised until they act or are inspected.

Every map is derived from the chosen world seed. After terrain generation, a connectivity audit flood-fills from the arrival point to every shop, settlement, castle gate, dungeon entrance, stair, mechanism, and provision source. Any failed critical route is repaired with a walkable road, bridge, or biome floor before monsters are placed. The self-test suite repeats these guarantees across a bank of unrelated seeds and verifies same-seed fingerprints.

Player control effects follow a strict anti-chain rule: hard control can consume at most one action, after which the player receives two turns of control immunity. Tearing free of a web clears the restraint and grants the same recovery window, guaranteeing an opportunity to move away. Electrified water deals damage in separated pulses with two safe actions between them and never removes player input; shock plates likewise cannot add a second terrain shock on their triggering turn.

Smoke blocks sight. Loud melee, spells, broken doors, explosions, collapsing ceilings, and alarms can draw enemies from outside the visible area. Rest is allowed only when nearby hunters have lost your trail.

## Elemental terrain and dungeon mechanisms

Each cell can carry persistent water, oil, fire, frost, electricity, poison, smoke, or webs. These states interact consistently: water extinguishes flame and conducts electricity; oil feeds and spreads fire; frost suppresses fire and freezes wet ground; poison and smoke linger; radiant magic clears corrupt clouds and webs. Actors standing in hazardous cells take damage on simulation turns.

Procedural floors contain closed, locked, broken, and secret doors plus concealed flame jets, shock plates, poison vents, collapse triggers, and alarm runes. Search with V to reveal nearby secrets. Biome machinery is operated with E: braziers alter illumination and fire, floodgates wet or drain rooms, prisms project charged light, and generators power light and release nearby locks.

Dungeon torches are mounted directly into solid biome walls. Press E while adjacent to take one, then use the wall torch from your inventory and choose an adjacent bare wall to mount and light it elsewhere. Cancelling placement returns the torch to your pack.

## Equipment and interface

Equipment uses eight slots: main hand, off hand, head, body, hands, feet, ring, and amulet. Gear can modify attack, defense, spell power, and lantern radius. The responsive pack browser places contents beside a live 32x12 true-colour item study on wide terminals and stacks the artwork above the list on compact terminals. Tab cycles stable views sorted by type, power, newest pickup, or name; keys 1–4 select those modes directly. Pickup time and order are saved with each item, so “newest” remains correct after saving and loading without physically rearranging game state. Scrolling changes the artwork, accent colour, category, quality, quantity, field notes, and exact comparison against the equipped slot without another keypress. Equipping, removing, and dropping gear are turn-based actions.

Monsters drop themed equipment with common, uncommon, rare, and epic variants. Every major monster family has signature gear, including spider bracers, salamander boots, gargoyle plate, mimic rings, lich diadems, and epic furnace-heart greatblades. The broader consumable pool adds greater healing, attack, defense, and focus elixirs, smoke bombs, escape waystones, and restorative ambrosia.

Menus use one of two deliberate geometries: compact dialogs for short decisions and a stable workspace frame for browsers, journals, conversations, creation, maps, help, equipment, and progression. On a 160-column fullscreen terminal the workspace exposes a 140-column interior and holds the same width and height for the lifetime of the screen; only the contents scroll. Moving the selection never resizes a menu, descriptions wrap inside their assigned column, and the map, skill constellation, dual-portrait dialogue, character dossier, and inventory study expand into the additional space. Arrow-key selection and highlighted rows are supported throughout, with number keys retained as shortcuts. The HUD uses compact health, mana, experience, food, and water bars and always displays equipment-adjusted combat values.

The character screen is an equipment-first loadout dossier: eight selectable slots share a wide, aligned equipment column with the calling portrait, live item power, full descriptions, attributes, and derived combat totals. Selected names marquee only when their assigned field is genuinely too narrow. Arrow keys move between slots, Enter removes the selected item, and I opens the pack.

## Progression, spells, and targeting

Every level grants two skill points and automatically opens a persistent class skill constellation. Each class has four branch-coloured, visually connected six-tier disciplines—24 nodes—with active/dormant path lines, prerequisites, level gates, rank pips, and live investment states. The selected-node dossier shows its parent and child, per-rank effect, spell status, and exact lock condition. Every node supports three ranks: passive ranks repeatedly improve attack, defense, vitality, mana, light, or spell power, while spell ranks increase that spell's effect and rank three reduces its mana cost. One rank in a parent opens the next linked tier, so a character can specialize deeply or spread points across branches. Points can be saved and the tree can be reopened at any time with T; number keys 1-4 jump directly between disciplines.

Each class now has an eight-spell progression path drawn from 25 complete abilities. Vanguard gains Flame Cleave, Battle Cry, and Ground Quake; Arcanist gains Prismatic Orb, Blink, and Ice Storm; Ranger gains Multishot, Poison Cloud, and Marked Shot; Lightbearer gains Solar Ray, Sanctuary, and Smite. Characters begin knowing two class-defining spells and learn further abilities through level choices.

Targeted spells open a world cursor. Move it with the normal movement keys, press Tab to cycle visible monsters, Enter to cast, or Escape to cancel. Range and line of effect are validated before mana or a turn is spent. X opens a non-turn-consuming inspection cursor that describes monster health and behaviour, equipment on the ground, and terrain properties.

## Biome-generated dungeon depths

The expedition spans five connected procedural depths. Every depth is generated from a dedicated biome definition rather than sharing one dungeon template:

- **Ashen Vaults:** ash flagstones, oil, smoke, braziers, fire traps, goblinoids, undead, and cinder creatures.
- **Drowned Reliquary:** flooded reflective floors, conductive water, floodgates, shock plates, slimes, myconids, trolls, and void creatures.
- **Prismatic Foundry:** highly reflective crystal materials, charged floors, prisms, generators, constructs, mages, mimics, and wardens.
- **Lightless Court:** minimal fixed light, webs, poison, smoke, secret architecture, spiders, gargoyles, umbra hounds, whispers, and liches.
- **Crown of Cinders:** furnace basalt, burning oil, braziers, collapse and flame traps, salamanders, cultists, wardens, liches, and demons.

Room placement and connections remain procedural inside each biome. Stairs travel both directions, while monster health, attacks, rewards, population, and equipment quality scale with depth.

## Lighting design

Every rendered cell receives additive RGB light from visible sources. Each source has color, radius, intensity, flicker, phase, softness, diffraction, and bounce properties. Five sub-cell rays sample each source to create penumbrae instead of binary shadows. Partially occluded rays receive wavelength-split edge fringes; light leaking around a visible corner produces a restrained diffraction halo; each terrain material contributes reflected color.

Light occlusion is independent from line of sight. Walls, trees, closed doors, boulders and other solid obstacles cast soft sampled shadows; living monsters, residents, and the player cast moving shadows as they change position. Transparent solid objects can therefore interrupt illumination without hiding the tile behind them. Outdoors, a three-sample directional sun ray changes direction from morning through evening, allowing creatures and scenery to cast daylight shadows as well as shadows from lanterns, fire, spells, and lamps. These shadows alter deterministic gameplay exposure but never make a geometrically visible cell invisible.

Player vision uses an aspect-correct smootherstep curve: nearby cells retain full clarity, then terrain, creatures, items, and effects fade together across a broad outer band before blending into extremely dim explored memory. There is no binary radius edge.

Frames are enclosed in DEC synchronized-output transactions so compatible terminals present each redraw atomically instead of visibly scanning it. RGB channels are quantized to four-value steps—still 262,144 possible colors—to suppress imperceptible color churn and substantially reduce ANSI traffic.

The player's lantern, street lamps, dungeon torches, luminous monsters, crystals, projectile cores, lightning arcs, and explosion volumes all use this pipeline. Explosions have a white flash, expanding shock front, turbulent fire volume, deterministic radial sparks, lingering drifting smoke, and terrain interaction. Explored-but-unseen terrain remains as a very dim visual memory.

Wet and deliberately crystalline floor materials respond to passing colored light, webs sparkle when illuminated, and fire and smoke animate independently of the turn clock. Melee impacts use a restrained local light response, with a small flash reserved for genuinely heavy blows and an edge vignette when the player is hurt; camera motion is reserved for explosions. Element contacts create steam, spreading frost cracks, branching current, curling poison, and collapsing embers. Spells have distinct trails and afterimages; stairs reveal each biome outward in its palette, while level and skill unlocks bloom like short-lived constellations. HUD health retains a brief damage trail, mana shimmers, new messages flash, and danger subtly animates the frame.
