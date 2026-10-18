"""Exercise actual shop frames at compact, wide and full-screen sizes."""
import os
import subprocess
import sys
from pathlib import Path

root = Path(__file__).resolve().parent
exe = root / 'LumenRogue-CPP.new.exe'
output = root / 'qa-shops'
output.mkdir(exist_ok=True)
sizes = [(60, 20), (80, 25), (120, 30), (160, 40), (220, 85), (160, 20), (120, 27)]
for width, height in sizes:
    for mode in ['shop', 'shop-sell', 'shop-kettle', 'shop-sigil']:
        env = dict(os.environ, LUMEN_QA_WIDTH=str(width), LUMEN_QA_HEIGHT=str(height))
        ansi = subprocess.check_output([str(exe), '--snapshot-' + mode, '--seed', '94117'], env=env, timeout=30)
        target = output / f'{mode}-{width}x{height}.png'
        subprocess.run([sys.executable, str(root / 'ansi_snapshot.py'), '-', str(target), str(width), str(height)], input=ansi, check=True, timeout=30)
        visible = Path(str(target) + '.txt').read_text(encoding='utf-8')
        for marker in ['BUY' if height >= 25 else 'Buy', 'SELL' if height >= 25 else 'Sell', 'QTY', 'Enter', 'Esc']:
            assert marker in visible, (mode, width, height, marker)
        if mode in ['shop', 'shop-sell']:
            for marker in ['WORN', 'ITEM', 'CHANGE', 'ATK', 'DEF', 'SPELL', 'LIGHT']:
                assert marker in visible, (mode, width, height, marker)
        assert 'More text · PgUp/PgDn' not in visible, (mode, width, height, 'clipping')
        print(mode, width, height, 'OK')
print(f'{len(sizes) * 4} shop snapshots passed.')
