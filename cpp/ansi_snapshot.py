import re
import sys
from PIL import Image, ImageDraw, ImageFont

source, target = sys.argv[1], sys.argv[2]
width = int(sys.argv[3]) if len(sys.argv) > 3 else 60
height = int(sys.argv[4]) if len(sys.argv) > 4 else 20
cells = [[(" ", (220, 220, 220), (0, 0, 0)) for _ in range(width)] for _ in range(height)]
payload = sys.stdin.buffer.read() if source == "-" else open(source, "rb").read()
text = payload.decode("utf-8", "replace")
font = ImageFont.truetype(r"C:\Windows\Fonts\CascadiaMono.ttf", 18)
x = y = 0
fg, bg = (220, 220, 220), (0, 0, 0)
pattern = re.compile(r"\x1b\[([0-9;?]*)([A-Za-z])")
i = 0
while i < len(text):
    if text[i] == "\x1b":
        match = pattern.match(text, i)
        if not match:
            i += 1
            continue
        params, command = match.group(1), match.group(2)
        if command == "H":
            values = [int(value) for value in params.split(";") if value and value != "?"]
            y = (values[0] - 1) if values else 0
            x = (values[1] - 1) if len(values) > 1 else 0
        elif command == "J" and params == "2":
            cells = [[(" ", (220, 220, 220), (0, 0, 0)) for _ in range(width)] for _ in range(height)]
        elif command == "m":
            values = [int(value) for value in params.split(";") if value] or [0]
            cursor = 0
            while cursor < len(values):
                value = values[cursor]
                if value == 0:
                    fg, bg = (220, 220, 220), (0, 0, 0)
                elif value == 39:
                    fg = (220, 220, 220)
                elif value == 49:
                    bg = (0, 0, 0)
                elif value in (38, 48) and cursor + 4 < len(values) and values[cursor + 1] == 2:
                    color = tuple(values[cursor + 2:cursor + 5])
                    if value == 38:
                        fg = color
                    else:
                        bg = color
                    cursor += 4
                cursor += 1
        i = match.end()
        continue
    char = text[i]
    if char == "\n":
        y += 1
        x = 0
    elif char != "\r":
        if 0 <= x < width and 0 <= y < height:
            cells[y][x] = (char, fg, bg)
        x += 1
    i += 1

cell_width, cell_height = 11, 22
image = Image.new("RGB", (width * cell_width, height * cell_height), "black")
draw = ImageDraw.Draw(image)
for row in range(height):
    for column in range(width):
        char, foreground, background = cells[row][column]
        left, top = column * cell_width, row * cell_height
        draw.rectangle((left, top, left + cell_width, top + cell_height), fill=background)
        draw.text((left, top - 1), char, font=font, fill=foreground)
image.save(target)
with open(str(target) + '.txt', 'w', encoding='utf-8') as readable:
    readable.write('\n'.join(''.join(cell[0] for cell in row) for row in cells))
