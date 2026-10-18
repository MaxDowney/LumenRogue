# Generated portrait sources

These 35 square PNGs are source art for LumenRogue's character, monster, and
inspection portraits. They are never displayed as graphics by the game.
`portrait_bitmap.py` downsamples them into 32×24 colour samples, packs each
vertical pair into a true-colour ANSI `▀` character, and generates
`GeneratedPortraits.hpp`. The released game remains 100% terminal/text output
and does not need the PNG files at runtime.

Regenerate the embedded bitmaps from `cpp/` with:

```powershell
python portrait_bitmap.py --preview assets/portrait-preview.png
```

The sources were created in separate calls with the built-in image-generation
tool. Character prompts used this shared specification:

> Source portrait for a true-colour terminal text-bitmap in LumenRogue; pure
> black background; centered square head-and-shoulders framing; high-contrast
> dark-fantasy scratchboard engraving with restrained painterly concept art;
> large readable tonal masses; warm amber torch key light; faint cool blue rim
> light; no text, border, UI, scenery, logo, or watermark; exactly one subject.

The eight subjects were Human Hearthborn, Dwarf Deep-kin, Elf Twilight Scion,
Ashborn Ember-souled, Vanguard front-line warden, Arcanist aether cartographer,
Ranger march stalker, and Lightbearer keeper of the dawn.

Monster and inspection prompts deliberately used a stricter source-icon mode:

> Source icon for a 32×12 true-colour terminal text-bitmap; pure black seamless
> background; exactly one centered subject; only two or three large forms;
> broad flat colour regions; thick high-contrast contours; limited black,
> furnace-amber, midnight-blue, and one accent palette; warm lower-left key and
> cool upper-right rim; instantly recognizable after reduction to 32×24 colour
> samples; no scenery, texture, tiny details, thin isolated lines, particles,
> text, border, UI, logo, or watermark.

The twelve monster sources cover kobold, spider, slime, myconid, mimic, undead,
cultist, gargoyle, bat, goblinoid, demon, and fallback beast silhouettes. The
fifteen inspection sources cover unmapped dark, residents, items, water and food
supplies, torches, machines, traps, passages, landmarks, water terrain, doors,
walls, ashpines, and generic floors.
