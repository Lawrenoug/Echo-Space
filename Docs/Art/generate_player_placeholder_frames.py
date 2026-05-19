from __future__ import annotations

import json
import math
import shutil
from dataclasses import dataclass
from pathlib import Path
from typing import Dict, List, Tuple

from PIL import Image, ImageDraw, ImageFilter


ROOT = Path(r"F:\Godot project\echo-space\Docs\Art")
SHEETS_ROOT = ROOT / "PlayerSprite"
FRAMES_ROOT = ROOT / "PlayerSpriteFrames"
ARCHIVE_ROOT = ROOT / "Archive"

CANVAS_WIDTH = 48
CANVAS_HEIGHT = 64
BASELINE_Y = 56

ANIMATIONS: List[Tuple[str, int]] = [
    ("idle", 6),
    ("run", 8),
    ("jumpstart", 3),
    ("fall", 3),
    ("attack", 8),
    ("hurt", 3),
    ("dead", 6),
    ("execute", 6),
    ("guard", 3),
    ("parry", 4),
]


@dataclass(frozen=True)
class Palette:
    outline: Tuple[int, int, int, int]
    skin: Tuple[int, int, int, int]
    cloth: Tuple[int, int, int, int]
    trim: Tuple[int, int, int, int]
    glow: Tuple[int, int, int, int]


PALETTES: Dict[str, Palette] = {
    "reality": Palette(
        outline=(18, 22, 34, 255),
        skin=(211, 186, 165, 255),
        cloth=(64, 86, 118, 255),
        trim=(146, 92, 75, 255),
        glow=(0, 0, 0, 0),
    ),
    "soul": Palette(
        outline=(14, 18, 40, 255),
        skin=(168, 223, 255, 255),
        cloth=(76, 109, 188, 255),
        trim=(159, 123, 228, 255),
        glow=(126, 220, 255, 92),
    ),
}


def archive_previous_outputs() -> None:
    ARCHIVE_ROOT.mkdir(parents=True, exist_ok=True)
    for source in (SHEETS_ROOT, FRAMES_ROOT):
        if not source.exists():
            continue

        archive_target = ARCHIVE_ROOT / f"{source.name}_legacy_20260519"
        suffix = 1
        while archive_target.exists():
            suffix += 1
            archive_target = ARCHIVE_ROOT / f"{source.name}_legacy_20260519_{suffix}"

        shutil.move(str(source), str(archive_target))


def ensure_clean_directories() -> None:
    SHEETS_ROOT.mkdir(parents=True, exist_ok=True)
    FRAMES_ROOT.mkdir(parents=True, exist_ok=True)


def clamp(value: float, minimum: int, maximum: int) -> int:
    return max(minimum, min(maximum, int(round(value))))


def ease(value: float) -> float:
    return 0.5 - 0.5 * math.cos(value * math.pi)


def segment(draw: ImageDraw.ImageDraw, points: List[Tuple[float, float]], outline, fill, width: int = 2) -> None:
    rounded = [(int(round(x)), int(round(y))) for x, y in points]
    draw.line(rounded, fill=outline, width=width + 2, joint="curve")
    draw.line(rounded, fill=fill, width=width, joint="curve")


def outlined_rect(draw: ImageDraw.ImageDraw, box: Tuple[int, int, int, int], outline, fill) -> None:
    draw.rectangle(box, fill=outline)
    inner = (box[0] + 1, box[1] + 1, box[2] - 1, box[3] - 1)
    if inner[0] <= inner[2] and inner[1] <= inner[3]:
        draw.rectangle(inner, fill=fill)


def add_soul_glow(frame: Image.Image, palette: Palette) -> Image.Image:
    if palette.glow[3] <= 0:
        return frame

    alpha = frame.getchannel("A")
    glow_mask = alpha.filter(ImageFilter.MaxFilter(5))
    glow = Image.new("RGBA", frame.size, palette.glow)
    glow.putalpha(glow_mask.point(lambda value: min(value, palette.glow[3])))
    combined = Image.alpha_composite(glow, frame)
    return combined


def draw_generic_pose(world: str, action: str, index: int, count: int) -> Image.Image:
    palette = PALETTES[world]
    image = Image.new("RGBA", (CANVAS_WIDTH, CANVAS_HEIGHT), (0, 0, 0, 0))
    draw = ImageDraw.Draw(image)

    progress = 0.0 if count <= 1 else index / (count - 1)
    cycle = 0.0 if count <= 1 else index / count
    bob = 0.0
    lean = 0.0
    crouch = 0.0
    torso_height = 13
    head_offset_y = 0.0
    front_arm: List[Tuple[float, float]] = []
    back_arm: List[Tuple[float, float]] = []
    front_leg: List[Tuple[float, float]] = []
    back_leg: List[Tuple[float, float]] = []
    cloak_tail = 0.0
    torso_shift_y = 0.0

    if action == "idle":
        bob = math.sin(cycle * math.tau) * 1.2
        cloak_tail = math.sin(cycle * math.tau) * 1.4
        front_arm = [(28, 28 + bob), (31, 36 + bob), (31, 45 + bob)]
        back_arm = [(21, 29 + bob), (19, 37 + bob), (19, 45 + bob)]
        front_leg = [(25, 41 + bob), (25, 49 + bob), (26, BASELINE_Y + bob)]
        back_leg = [(22, 41 + bob), (22, 49 + bob), (21, BASELINE_Y + bob)]
    elif action == "run":
        bob = math.sin(cycle * math.tau) * 1.1
        lean = 2.5
        phase = math.sin(cycle * math.tau)
        anti_phase = math.sin(cycle * math.tau + math.pi)
        front_arm = [(29 + lean, 29 + bob), (32 + lean, 34 + bob), (34 + lean + phase * 2, 40 + bob)]
        back_arm = [(22 + lean, 29 + bob), (19 + lean, 34 + bob), (17 + lean + anti_phase * 2, 40 + bob)]
        front_leg = [(25 + lean, 41 + bob), (28 + lean + phase * 2, 48 + bob), (31 + lean + phase * 4, BASELINE_Y + bob)]
        back_leg = [(22 + lean, 41 + bob), (20 + lean + anti_phase * 2, 48 + bob), (18 + lean + anti_phase * 4, BASELINE_Y + bob)]
        cloak_tail = anti_phase * 2.2
    elif action == "jumpstart":
        crouch = (1.0 - progress) * 4.0
        lean = progress * 1.5
        head_offset_y = -progress * 2.0
        front_arm = [(28 + lean, 30 + crouch), (31 + lean, 36 + crouch), (34 + lean, 41 + crouch)]
        back_arm = [(21 + lean, 30 + crouch), (18 + lean, 36 + crouch), (18 + lean, 42 + crouch)]
        front_leg = [(25 + lean, 41 + crouch), (27 + lean, 48 + crouch), (28 + lean, BASELINE_Y)]
        back_leg = [(22 + lean, 41 + crouch), (21 + lean, 48 + crouch), (20 + lean, BASELINE_Y)]
        cloak_tail = progress * 2.0
    elif action == "fall":
        torso_shift_y = progress * 1.0
        front_arm = [(28, 29 + torso_shift_y), (31, 34 + torso_shift_y), (34, 39 + torso_shift_y)]
        back_arm = [(21, 29 + torso_shift_y), (18, 34 + torso_shift_y), (16, 39 + torso_shift_y)]
        front_leg = [(25, 41 + torso_shift_y), (27, 47 + torso_shift_y), (29, 53 + torso_shift_y)]
        back_leg = [(22, 41 + torso_shift_y), (20, 47 + torso_shift_y), (18, 53 + torso_shift_y)]
        cloak_tail = 2.5
    elif action == "attack":
        if progress < 0.35:
            wind = ease(progress / 0.35)
            lean = -2.0 * wind
            front_arm = [(28 + lean, 30), (23 + lean, 27), (19 + lean, 29)]
            back_arm = [(21 + lean, 30), (19 + lean, 35), (19 + lean, 41)]
            front_leg = [(25 + lean, 41), (24 + lean, 48), (23 + lean, BASELINE_Y)]
            back_leg = [(22 + lean, 41), (23 + lean, 48), (25 + lean, BASELINE_Y)]
        elif progress < 0.75:
            strike = ease((progress - 0.35) / 0.40)
            lean = 3.0 * strike
            front_arm = [(28 + lean, 29), (33 + lean, 27), (38 + lean, 30)]
            back_arm = [(21 + lean, 30), (20 + lean, 36), (20 + lean, 43)]
            front_leg = [(25 + lean, 41), (28 + lean, 47), (31 + lean, BASELINE_Y)]
            back_leg = [(22 + lean, 41), (21 + lean, 48), (20 + lean, BASELINE_Y)]
            cloak_tail = -2.0
        else:
            recover = ease((progress - 0.75) / 0.25)
            lean = 1.2 * (1.0 - recover)
            front_arm = [(28 + lean, 29), (31 + lean, 33), (31 + lean, 40)]
            back_arm = [(21 + lean, 30), (18 + lean, 36), (18 + lean, 42)]
            front_leg = [(25 + lean, 41), (26 + lean, 48), (27 + lean, BASELINE_Y)]
            back_leg = [(22 + lean, 41), (21 + lean, 48), (20 + lean, BASELINE_Y)]
    elif action == "hurt":
        recoil = ease(progress)
        lean = -3.2 * recoil
        torso_shift_y = recoil * 1.5
        front_arm = [(28 + lean, 29 + torso_shift_y), (24 + lean, 33 + torso_shift_y), (20 + lean, 39 + torso_shift_y)]
        back_arm = [(21 + lean, 30 + torso_shift_y), (18 + lean, 35 + torso_shift_y), (16 + lean, 40 + torso_shift_y)]
        front_leg = [(25 + lean, 41 + torso_shift_y), (24 + lean, 48 + torso_shift_y), (23 + lean, BASELINE_Y)]
        back_leg = [(22 + lean, 41 + torso_shift_y), (21 + lean, 48 + torso_shift_y), (20 + lean, BASELINE_Y)]
    elif action == "dead":
        collapse = ease(progress)
        center_y = 44 + collapse * 10
        head_x = 17 + collapse * 10
        outlined_rect(draw, (clamp(head_x, 6, 34), clamp(center_y - 11, 4, 58), clamp(head_x + 7, 10, 41), clamp(center_y - 4, 6, 61)), palette.outline, palette.skin)
        segment(draw, [(18 + collapse * 6, center_y - 2), (28 + collapse * 8, center_y + 1)], palette.outline, palette.cloth, 5)
        segment(draw, [(26 + collapse * 7, center_y + 1), (34 + collapse * 7, center_y + 2)], palette.outline, palette.trim, 4)
        segment(draw, [(21 + collapse * 5, center_y + 3), (30 + collapse * 8, center_y + 7)], palette.outline, palette.cloth, 4)
        segment(draw, [(25 + collapse * 7, center_y + 2), (37 + collapse * 8, center_y + 11)], palette.outline, palette.cloth, 4)
        return add_soul_glow(image, palette)
    elif action == "execute":
        drive = ease(progress)
        lean = 4.0 * drive
        crouch = max(0.0, 2.0 - drive * 2.0)
        front_arm = [(28 + lean, 28 + crouch), (34 + lean, 25 + crouch), (39 + lean, 27 + crouch)]
        back_arm = [(21 + lean, 29 + crouch), (18 + lean, 35 + crouch), (19 + lean, 42 + crouch)]
        front_leg = [(25 + lean, 41 + crouch), (29 + lean, 47 + crouch), (34 + lean, BASELINE_Y)]
        back_leg = [(22 + lean, 41 + crouch), (21 + lean, 48 + crouch), (19 + lean, BASELINE_Y)]
        cloak_tail = -1.5
    elif action == "guard":
        settle = ease(progress)
        lean = 0.8 * settle
        crouch = 1.4 * settle
        front_arm = [(28 + lean, 29 + crouch), (32 + lean, 26 + crouch), (34 + lean, 30 + crouch)]
        back_arm = [(21 + lean, 30 + crouch), (19 + lean, 35 + crouch), (19 + lean, 41 + crouch)]
        front_leg = [(25 + lean, 41 + crouch), (26 + lean, 48 + crouch), (27 + lean, BASELINE_Y)]
        back_leg = [(22 + lean, 41 + crouch), (21 + lean, 48 + crouch), (20 + lean, BASELINE_Y)]
    elif action == "parry":
        snap = ease(progress)
        lean = 2.4 * snap
        front_arm = [(28 + lean, 28), (35 + lean, 24), (40 + lean, 26)]
        back_arm = [(21 + lean, 30), (18 + lean, 35), (17 + lean, 40)]
        front_leg = [(25 + lean, 41), (27 + lean, 47), (30 + lean, BASELINE_Y)]
        back_leg = [(22 + lean, 41), (21 + lean, 48), (19 + lean, BASELINE_Y)]
        cloak_tail = -1.8
    else:
        front_arm = [(28, 28), (31, 36), (31, 45)]
        back_arm = [(21, 29), (19, 37), (19, 45)]
        front_leg = [(25, 41), (25, 49), (26, BASELINE_Y)]
        back_leg = [(22, 41), (22, 49), (21, BASELINE_Y)]

    center_x = 24 + lean
    torso_top = 24 + torso_shift_y + crouch
    torso_bottom = torso_top + torso_height
    head_left = clamp(center_x - 4, 4, 36)
    head_top = clamp(14 + bob + head_offset_y + torso_shift_y, 4, 32)

    draw.polygon(
        [
            (center_x - 6, torso_top + 2),
            (center_x - 4, torso_bottom - 2),
            (center_x, torso_bottom + 4 + cloak_tail),
            (center_x + 6, torso_bottom - 1),
            (center_x + 5, torso_top + 2),
        ],
        fill=palette.outline,
    )
    draw.polygon(
        [
            (center_x - 5, torso_top + 3),
            (center_x - 3, torso_bottom - 2),
            (center_x, torso_bottom + 2 + cloak_tail),
            (center_x + 5, torso_bottom - 2),
            (center_x + 4, torso_top + 3),
        ],
        fill=palette.trim,
    )

    outlined_rect(
        draw,
        (
            clamp(center_x - 4, 6, 36),
            clamp(torso_top, 8, 44),
            clamp(center_x + 4, 12, 42),
            clamp(torso_bottom, 12, 54),
        ),
        palette.outline,
        palette.cloth,
    )
    outlined_rect(
        draw,
        (head_left, head_top, head_left + 8, head_top + 8),
        palette.outline,
        palette.skin,
    )
    draw.point((head_left + 5, head_top + 4), fill=palette.outline)

    segment(draw, back_leg, palette.outline, palette.cloth, 3)
    segment(draw, front_leg, palette.outline, palette.cloth, 3)
    segment(draw, back_arm, palette.outline, palette.trim, 3)
    segment(draw, front_arm, palette.outline, palette.trim, 3)

    return add_soul_glow(image, palette)


def make_sheet(world: str, action: str, count: int) -> Tuple[Image.Image, List[Image.Image]]:
    frames = [draw_generic_pose(world, action, index, count) for index in range(count)]
    sheet = Image.new("RGBA", (CANVAS_WIDTH * count, CANVAS_HEIGHT), (0, 0, 0, 0))
    for index, frame in enumerate(frames):
        sheet.paste(frame, (index * CANVAS_WIDTH, 0), frame)

    return sheet, frames


def save_animation(world: str, action: str, count: int, root_manifest: List[dict]) -> None:
    prefix = f"player_{world}_{action}"
    sheet_name = f"{prefix}_sheet_v1.png"
    sheet, frames = make_sheet(world, action, count)
    sheet.save(SHEETS_ROOT / sheet_name)

    frame_dir = FRAMES_ROOT / f"{prefix}_frames_v1"
    frame_dir.mkdir(parents=True, exist_ok=True)
    frame_names: List[str] = []

    for index, frame in enumerate(frames):
        frame_name = f"{prefix}_{index:02d}.png"
        frame.save(frame_dir / frame_name)
        frame_names.append(frame_name)

    entry = {
        "source": sheet_name,
        "frame_count": count,
        "canvas_width": CANVAS_WIDTH,
        "canvas_height": CANVAS_HEIGHT,
        "baseline": BASELINE_Y,
        "scale": 1.0,
        "frames": frame_names,
    }

    with open(frame_dir / "manifest.json", "w", encoding="utf-8") as handle:
        json.dump(entry, handle, ensure_ascii=False, indent=2)

    root_manifest.append(entry)


def main() -> None:
    archive_previous_outputs()
    ensure_clean_directories()

    manifest_entries: List[dict] = []
    for world in ("reality", "soul"):
        for action, count in ANIMATIONS:
            save_animation(world, action, count, manifest_entries)

    with open(FRAMES_ROOT / "manifest.json", "w", encoding="utf-8") as handle:
        json.dump(manifest_entries, handle, ensure_ascii=False, indent=2)


if __name__ == "__main__":
    main()
