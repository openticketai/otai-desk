# /// script
# dependencies = [
#   "pydantic>=2.0",
#   "pyyaml>=6.0",
#   "tiktoken>=0.7.0",
#   "typer>=0.15",
# ]
# ///
"""Line diagram CLI — same as otai/scripts/diagrams.py with Line docs as default.

uv run otai-desk/scripts/diagrams.py show otai-desk-architecture
uv run otai-desk/scripts/diagrams.py for-path otai-desk/src/Program.cs
uv run otai/scripts/diagrams.py show otai-desk-architecture --product line
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

from diagrams.cli import app

if __name__ == "__main__":
    app(prog_name="otai-desk/scripts/diagrams.py")
