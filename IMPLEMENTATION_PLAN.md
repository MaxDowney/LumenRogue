# Professional systems pass — implementation plan

Item 12 (sound and music) is intentionally deferred. All world and dungeon construction is derived from a user-selectable integer seed; the HUD and save file retain that seed.

## 1. Save and load

- Serialize the seed, character build, clock, resources, skills, inventory/equipment, quests, flags, discoveries, lore, monster state, explored cells, ground loot, provisions, doors, and mechanisms.
- Regenerate the exact base world from its seed before applying persistent changes, keeping saves compact and resilient.
- Expose F5/F9 quick-save/load and `--load <path>` startup loading. Validate a save version before applying it.

## 2. Regional quest chains

- Give Bramblewick, Stonecross, and Reedwater two-stage local stories with prerequisite handling.
- Join their outcomes into Captain Roen's Western Compact commission and Lady Sera's castle-undercroft finale.
- Reconcile objectives already completed before acceptance so exploration is never invalidated.

## 3. Dungeon identity and bosses

- Give Hollowroot, Sunken Barrow, Moonwell, and Valeward different environmental rules and explanatory entry text.
- Place exactly one seeded named boss at each site's reachable heart.
- Persist clears, award unique epic gear and a boss relic, and connect each clear to regional quests.

## 4. Navigation

- Record named landmarks when the player comes within a clear discovery radius; show them as `◆` on the explored map.
- Unlock F2 travel only for discovered outdoor destinations, block it during pursuit, and charge explicit time/food/water costs.
- Preserve ordinary roads and exact dungeon return positions so fast travel complements walking rather than replacing geography.

## 5. NPC schedules

- Give every resident deterministic work, supper, and home positions.
- Move schedules from the same world clock that drives daylight, and display the current schedule in interaction text.
- Close shops outside 07:00–18:00 with a clear explanation rather than silently denying interaction.

## 6. Castle progression

- Keep Valeward physically present and explorable from the beginning.
- Require the three village outcomes for a formal castle commission.
- Seal the undercroft until commissioned, then finish with its unique boss and a visible secured-castle consequence.

## 7. Wilderness ecology

- Seed home territories and diurnal/nocturnal behavior for outdoor creatures.
- Let nearby faction allies share alerts, rivals fight, and idle creatures return toward their territories.
- Keep villages, roads, castle approaches, supplies, loot, and dungeon entrances free from spawn obstruction.

## 8. Quest/world consequences

- Apply stable world flags at turn-in: safer Bramblewick territory, relit Stonecross roads, renewed Reedwater supplies, and castle lights.
- Make flags prerequisites for later quests and persist them in saves.
- Keep changes functional and legible instead of relying on ambient particles.

## 9. Combat readability and tactics

- Give enemies explicit Idle/Roam/Advance/Retreat/Sleep/Strike/Shoot/Guard intentions.
- Telegraph melee and ranged attacks for one full response turn and show intent through inspection.
- Let F6 prepare Block, Dodge, or Ward; consume the reaction on the next relevant attack. Retain the existing anti-stunlock grace rule.

## 10. Economy and crafting

- Add faction-specific hide, venom, scrap, and relic drops plus boss relics.
- Provide five settlement recipes spanning medicine, bombs, provisions, uncommon gear, and an epic lantern amulet.
- Show exact ingredients, workshop fees, purpose, and result in a stable F3 menu.

## 11. Discovery rewards

- Award a small, explicit XP reward once per named landmark.
- Attach authored lore only to places with something meaningful to say, and collect it in F4.
- Track discoveries independently from visible-map cells so rewards cannot repeat.

## Procedural safety and verification

- Derive town, wilderness, main depths, and site dungeons from salted hashes of the selected seed.
- Maintain a critical-point list for every generated map. Flood-fill from the arrival point after generation; carve a guaranteed road, bridge, or biome floor to any unreachable point.
- Run deterministic fingerprint tests, different-seed variance tests, and reachability checks across eight disparate seeds and all five dungeon depths.
- Round-trip a generated expedition through JSON and assert seed, time, inventory, discoveries, and character state survive.
