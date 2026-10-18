# LumenRogue — Windows quick start

Put LumenRogue-CPP.exe in a writable folder and run it in Windows Terminal.
Use at least 60 columns by 20 rows; 120 by 40 or larger gives the menus more room.
All art and lighting are terminal characters, not a graphical window.

From PowerShell in that folder:

```powershell
.\LumenRogue-CPP.exe
.\LumenRogue-CPP.exe --seed 94117
.\LumenRogue-CPP.exe --load .\lumenrogue-cpp.save
```

F5 writes lumenrogue-cpp.save in the current working directory. F9 loads it.
Back up this file before changing versions. Quitting does not automatically save.

Movement: arrows/WASD/HJKL; Y/U/B/N or numpad for diagonals. Space waits.
E interacts; G picks up; R rests; I opens inventory; C shows your character;
Z opens spells; T opens the skill tree; X inspects; M shows the explored map;
Tab opens quests; ? shows help; Q opens quit confirmation.

Inventory: 1 Type, 2 Power, 3 Newest, 4 Name; Tab cycles sorting. Enter uses/equips.
Shops: B Buy, S Sell, Tab switches; arrows select; +/- changes quantity; Enter trades.
Equipped items cannot be sold. Shop hours are 07:00 to 18:00 in game time.

F7 toggles sound. F8 enables reduced motion and gentler flashes.
The world only advances when you take a turn; light and visual effects keep moving.

Requires 64-bit Windows 10/11. No extra runtime or asset folder is needed.
The executable is unsigned; SHA256SUMS.txt records the published file checksums.
