"""Render real executable output at multiple terminal sizes; never open a game/save."""
import os
import subprocess
import sys
from pathlib import Path

root = Path(__file__).resolve().parent
exe = Path(sys.argv[1]).resolve() if len(sys.argv) > 1 else root / 'LumenRogue-CPP.new.exe'
output = root / 'qa-polish'
output.mkdir(exist_ok=True)
screens = ['menu', 'journal', 'dialogue', 'creation', 'ancestry', 'help', 'skill-tree', 'character', 'spells', 'lore', 'effects', 'splash']
sizes = [(60, 20), (80, 25), (120, 30), (160, 40)]
for width, height in sizes:
    for screen in screens:
        env = dict(os.environ, LUMEN_QA_WIDTH=str(width), LUMEN_QA_HEIGHT=str(height))
        ansi = subprocess.check_output([str(exe), '--snapshot-' + screen, '--seed', '94117'], env=env, timeout=30)
        assert b'\x1b[' in ansi, (screen, 'missing terminal output')
        target = output / f'{screen}-{width}x{height}.png'
        subprocess.run([sys.executable, str(root / 'ansi_snapshot.py'), '-', str(target), str(width), str(height)], input=ansi, check=True, timeout=30)
        visible = Path(str(target) + '.txt').read_text(encoding='utf-8')
        required = {
            'menu': ['ITEM BONUSES', 'YOUR TOTAL', 'SPELL', 'LIGHT', '1–4/Tab Sort'],
            'journal': ['NEXT STEP', 'CONTACT', 'Enter Track'],
            'dialogue': ['Mara Vey', 'Elf', 'Reply'],
            'creation': ['VANGUARD', 'ARCANIST', 'RANGER', 'LIGHTBEARER', 'DOSSIER', 'Esc Back'],
            'character': ['STR', 'DEX', 'ITEM · ATK', 'Enter Remove'],
            'spells': ['COST', 'RANGE', 'MASTERY'],
            'lore': ['Enter Read Full Account'],
            'skill-tree': ['EFFECT', 'STATUS', 'Enter Invest'],
        }.get(screen, [])
        for marker in required:
            assert marker in visible, (screen, width, height, 'not visible:', marker)
        if screen not in ['help', 'splash', 'effects']:
            assert 'More text · PgUp/PgDn in reading panels' not in visible, (screen, width, height, 'unexpected shared-frame clipping')
print(f'Rendered {len(screens)*len(sizes)} terminal snapshots in {output}')
