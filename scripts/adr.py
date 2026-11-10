# /// script
# dependencies = [
#   "pydantic>=2.0",
#   "pyyaml>=6.0",
#   "tiktoken>=0.7.0",
#   "typer>=0.15",
# ]
# ///
"""Line ADR CLI — same as otai/scripts/adr.py with Line docs as default.

uv run otai-desk/scripts/adr.py show LD-084
uv run otai/scripts/adr.py show LD-084 --product line
"""

from __future__ import annotations

import sys
from pathlib import Path

if hasattr(sys.stdout, "reconfigure"):
    sys.stdout.reconfigure(encoding="utf-8")
    sys.stderr.reconfigure(encoding="utf-8")

import os

_scripts = Path(__file__).resolve().parents[2] / "otai" / "scripts"
sys.path.insert(0, str(_scripts))

os.environ.setdefault("OTAI_DOCS_PRODUCT", "line")

from adr.cli import app

if __name__ == "__main__":
    app(prog_name="otai-desk/scripts/adr.py")
