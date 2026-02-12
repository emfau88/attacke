#!/usr/bin/env python3
from pathlib import Path
import os

ROOT = Path('/home/ubuntu/workspace/attacke')
TICKETS_DIR = ROOT / 'tickets'
MISSING_LOG = ROOT / 'reports' / 'rewrite_missing_keys.log'

START = int(os.environ.get('START', '241'))
END = int(os.environ.get('END', '260'))
DRY_RUN = os.environ.get('DRY_RUN', '0') == '1'

# Minimal mapping example used by rewrite utility.
# Missing keys are expected and will be logged instead of crashing.
rewrite = {
    241: "# T241\n\n## GOAL\nRewrite utility sample ticket 241.\n",
    242: "# T242\n\n## GOAL\nRewrite utility sample ticket 242.\n",
    243: "# T243\n\n## GOAL\nRewrite utility sample ticket 243.\n",
    246: "# T246\n\n## GOAL\nRewrite utility sample ticket 246.\n",
}

MISSING_LOG.parent.mkdir(parents=True, exist_ok=True)
MISSING_LOG.write_text('')

missing = []
rewritten = 0
total = 0

for n in range(START, END + 1):
    total += 1
    val = rewrite.get(n)
    if val is None:
        missing.append(n)
        with MISSING_LOG.open('a') as f:
            f.write(f"{n}\n")
        continue

    out = TICKETS_DIR / f"T{n:03d}.md"
    if not DRY_RUN:
        out.write_text(val)
    rewritten += 1

first_10_missing = ','.join(str(x) for x in missing[:10]) if missing else '-'
print(f"total_iterated={total}")
print(f"rewritten_count={rewritten}")
print(f"missing_count={len(missing)}")
print(f"first_10_missing={first_10_missing}")
print(f"dry_run={DRY_RUN}")
print(f"missing_log={MISSING_LOG}")
