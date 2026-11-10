# /// script
# dependencies = [
#   "pydantic>=2.0",
#   "pyyaml>=6.0",
#   "tiktoken>=0.7.0",
# ]
# ///
"""Line D2 index + PNG renderer (shared implementation in otai/docs/diagrams/).

uv run otai-desk/docs/diagrams/render_diagrams.py --index-only
uv run otai-desk/docs/diagrams/render_diagrams.py --diagram otai-desk-architecture
uv run otai-desk/docs/diagrams/render_diagrams.py --check
"""

from __future__ import annotations

import runpy
import sys
from pathlib import Path

HERE = Path(__file__).resolve().parent
REPO_ROOT = HERE.parents[2]
SHARED = REPO_ROOT / "otai" / "docs" / "diagrams" / "render_diagrams.py"

if __name__ == "__main__":
    if not SHARED.is_file():
        print(f"missing shared renderer: {SHARED}", file=sys.stderr)
        raise SystemExit(1)
    if "--diagrams-root" not in sys.argv:
        sys.argv[1:1] = ["--diagrams-root", HERE.relative_to(REPO_ROOT).as_posix()]
    runpy.run_path(str(SHARED), run_name="__main__")
