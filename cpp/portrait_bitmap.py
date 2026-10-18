"""Convert generated portrait source images into embedded terminal half-block bitmaps.

Each terminal cell stores two square colour samples: the upper sample is the
foreground of a Unicode upper-half block and the lower sample is its background.
With the game's 1:2 terminal cell aspect ratio this preserves the source image's
proportions while remaining entirely text/ANSI output at runtime.
"""

from __future__ import annotations

import argparse
from pathlib import Path

from PIL import Image, ImageEnhance, ImageOps


PORTRAITS = (
    ("race-human", "race-human.png"),
    ("race-dwarf", "race-dwarf.png"),
    ("race-elf", "race-elf.png"),
    ("race-ashborn", "race-ashborn.png"),
    ("class-vanguard", "class-vanguard.png"),
    ("class-arcanist", "class-arcanist.png"),
    ("class-ranger", "class-ranger.png"),
    ("class-lightbearer", "class-lightbearer.png"),
    ("monster-kobold", "monster-kobold.png"),
    ("monster-spider", "monster-spider.png"),
    ("monster-slime", "monster-slime.png"),
    ("monster-myconid", "monster-myconid.png"),
    ("monster-mimic", "monster-mimic.png"),
    ("monster-undead", "monster-undead.png"),
    ("monster-cultist", "monster-cultist.png"),
    ("monster-gargoyle", "monster-gargoyle.png"),
    ("monster-bat", "monster-bat.png"),
    ("monster-goblinoid", "monster-goblinoid.png"),
    ("monster-demon", "monster-demon.png"),
    ("monster-beast", "monster-beast.png"),
    ("inspect-unmapped", "inspect-unmapped.png"),
    ("inspect-npc", "inspect-npc.png"),
    ("inspect-item", "inspect-item.png"),
    ("item-armour", "item-armour.png"),
    ("item-jewellery", "item-jewellery.png"),
    ("item-potion", "item-potion.png"),
    ("item-bomb", "item-bomb.png"),
    ("inspect-water-source", "inspect-water-source.png"),
    ("inspect-food", "inspect-food.png"),
    ("inspect-torch", "inspect-torch.png"),
    ("inspect-machine", "inspect-machine.png"),
    ("inspect-trap", "inspect-trap.png"),
    ("inspect-passage", "inspect-passage.png"),
    ("inspect-landmark", "inspect-landmark.png"),
    ("inspect-water", "inspect-water.png"),
    ("inspect-door", "inspect-door.png"),
    ("inspect-wall", "inspect-wall.png"),
    ("inspect-ashpine", "inspect-ashpine.png"),
    ("inspect-floor", "inspect-floor.png"),
)


def quantize(channel: int) -> int:
    return min(255, ((channel + 4) // 8) * 8)


def prepare(path: Path, columns: int, rows: int) -> list[tuple[int, int, int]]:
    image = Image.open(path).convert("RGB")
    image = ImageOps.fit(image, (columns, rows * 2), Image.Resampling.LANCZOS)
    image = ImageEnhance.Contrast(image).enhance(1.20)
    image = ImageEnhance.Color(image).enhance(1.12)
    pixels: list[tuple[int, int, int]] = []
    for y in range(image.height):
        for x in range(image.width):
            red, green, blue = image.getpixel((x, y))
            luminance = red * 0.2126 + green * 0.7152 + blue * 0.0722
            if luminance < 9:
                pixels.append((0, 0, 0))
            else:
                pixels.append((quantize(red), quantize(green), quantize(blue)))
    return pixels


def write_header(
    target: Path,
    sources: list[tuple[str, list[tuple[int, int, int]]]],
    columns: int,
    rows: int,
) -> None:
    lines = [
        "#pragma once",
        "#include <cstdint>",
        "",
        "namespace lumen_portraits {",
        "struct PixelPair { std::uint8_t tr,tg,tb,br,bg,bb; };",
        f"inline constexpr int columns={columns};",
        f"inline constexpr int rows={rows};",
        f"inline constexpr int count={len(sources)};",
        "inline constexpr const char* ids[count]={" + ",".join(f'\"{name}\"' for name, _ in sources) + "};",
        "inline constexpr PixelPair bitmaps[count][columns*rows]={",
    ]
    for name, pixels in sources:
        lines.append(f"    {{ // {name}")
        for row in range(rows):
            cells = []
            for column in range(columns):
                top = pixels[(row * 2) * columns + column]
                bottom = pixels[(row * 2 + 1) * columns + column]
                cells.append("{" + ",".join(map(str, (*top, *bottom))) + "}")
            lines.append("        " + ",".join(cells) + ("," if row + 1 < rows else ""))
        lines.append("    },")
    lines.extend(("};", "}", ""))
    target.write_text("\n".join(lines), encoding="utf-8")


def write_preview(
    target: Path,
    sources: list[tuple[str, list[tuple[int, int, int]]]],
    columns: int,
    rows: int,
) -> None:
    cell_width, cell_height = 11, 22
    gap, label_height = 22, 28
    portrait_width, portrait_height = columns * cell_width, rows * cell_height
    grid_columns = 4
    grid_rows = (len(sources) + grid_columns - 1) // grid_columns
    canvas = Image.new(
        "RGB",
        (
            portrait_width * grid_columns + gap * (grid_columns + 1),
            (portrait_height + label_height) * grid_rows + gap * (grid_rows + 1),
        ),
        (4, 5, 9),
    )
    from PIL import ImageDraw, ImageFont

    draw = ImageDraw.Draw(canvas)
    font = ImageFont.truetype(r"C:\Windows\Fonts\CascadiaMono.ttf", 17)
    for index, (name, pixels) in enumerate(sources):
        grid_x, grid_y = index % grid_columns, index // grid_columns
        left = gap + grid_x * (portrait_width + gap)
        top = gap + grid_y * (portrait_height + label_height + gap)
        for row in range(rows):
            for column in range(columns):
                upper = pixels[(row * 2) * columns + column]
                lower = pixels[(row * 2 + 1) * columns + column]
                x = left + column * cell_width
                y = top + row * cell_height
                draw.rectangle((x, y, x + cell_width, y + cell_height // 2), fill=upper)
                draw.rectangle((x, y + cell_height // 2, x + cell_width, y + cell_height), fill=lower)
        draw.text((left, top + portrait_height + 4), name.upper(), font=font, fill=(218, 205, 180))
    target.parent.mkdir(parents=True, exist_ok=True)
    canvas.save(target)


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--source-dir", type=Path, default=Path("assets/portrait_sources"))
    parser.add_argument("--header", type=Path, default=Path("GeneratedPortraits.hpp"))
    parser.add_argument("--preview", type=Path)
    parser.add_argument("--columns", type=int, default=32)
    parser.add_argument("--rows", type=int, default=12)
    args = parser.parse_args()

    sources = [(name, prepare(args.source_dir / filename, args.columns, args.rows)) for name, filename in PORTRAITS]
    write_header(args.header, sources, args.columns, args.rows)
    if args.preview:
        write_preview(args.preview, sources, args.columns, args.rows)


if __name__ == "__main__":
    main()
