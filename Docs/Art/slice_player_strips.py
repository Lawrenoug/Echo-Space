from __future__ import annotations

import json
from dataclasses import dataclass
from pathlib import Path

from PIL import Image


FRAME_COUNTS = {
    "player_reality_idle_sheet_v1.png": 6,
    "player_reality_run_sheet_v1.png": 8,
    "player_reality_jumpstart_sheet_v1.png": 3,
    "player_reality_fall_sheet_v1.png": 3,
    "player_reality_attack_sheet_v1.png": 8,
    "player_reality_hurt_sheet_v1.png": 3,
    "player_reality_dead_sheet_v1.png": 6,
    "player_reality_execute_sheet_v1.png": 6,
    "player_reality_guard_sheet_v1.png": 3,
    "player_reality_parry_sheet_v1.png": 4,
    "player_soul_idle_sheet_v1.png": 6,
    "player_soul_run_sheet_v1.png": 8,
    "player_soul_jumpstart_sheet_v1.png": 3,
    "player_soul_fall_sheet_v1.png": 3,
    "player_soul_attack_sheet_v1.png": 8,
    "player_soul_hurt_sheet_v1.png": 3,
    "player_soul_dead_sheet_v1.png": 6,
    "player_soul_execute_sheet_v1.png": 6,
    "player_soul_guard_sheet_v1.png": 3,
    "player_soul_parry_sheet_v1.png": 4,
}

SCALE_OVERRIDES = {
    "player_reality_run_sheet_v1.png": 459 / 320,
    "player_soul_run_sheet_v1.png": 464 / 328,
}

BACKGROUND_TOLERANCE = 16
HARD_KEY_THRESHOLD = 28
SOFT_KEY_THRESHOLD = 150
ALPHA_THRESHOLD = 16


@dataclass
class CropBox:
    left: int
    top: int
    right: int
    bottom: int

    @property
    def width(self) -> int:
        return self.right - self.left

    @property
    def height(self) -> int:
        return self.bottom - self.top


def load_alpha_sums(image: Image.Image) -> list[int]:
    rgba = image.convert("RGBA")
    width, height = rgba.size
    sums: list[int] = []
    for x in range(width):
        total = 0
        for y in range(height):
            alpha = rgba.getpixel((x, y))[3]
            if alpha > ALPHA_THRESHOLD:
                total += alpha
        sums.append(total)
    return sums


def make_background_transparent(image: Image.Image) -> Image.Image:
    rgba = image.convert("RGBA")
    width, height = rgba.size
    background = rgba.getpixel((0, 0))

    if background[3] <= 16:
        return rgba

    pixels = rgba.load()
    hard_sq = HARD_KEY_THRESHOLD * HARD_KEY_THRESHOLD
    soft_sq = SOFT_KEY_THRESHOLD * SOFT_KEY_THRESHOLD
    background_is_green = (
        background[1] > background[0] + BACKGROUND_TOLERANCE
        and background[1] > background[2] + BACKGROUND_TOLERANCE
    )

    for x in range(width):
        for y in range(height):
            r, g, b, a = pixels[x, y]
            if a <= 16:
                continue

            dr = r - background[0]
            dg = g - background[1]
            db = b - background[2]
            distance_sq = dr * dr + dg * dg + db * db

            if distance_sq <= hard_sq:
                pixels[x, y] = (r, g, b, 0)
                continue

            original_alpha = a
            if distance_sq < soft_sq:
                distance = distance_sq ** 0.5
                alpha_scale = (distance - HARD_KEY_THRESHOLD) / (SOFT_KEY_THRESHOLD - HARD_KEY_THRESHOLD)
                a = int(a * max(0.0, min(1.0, alpha_scale)))

            if 0 < a < original_alpha:
                alpha_norm = a / 255.0
                inverse_alpha = 1.0 - alpha_norm
                r = int(max(0, min(255, round((r - background[0] * inverse_alpha) / alpha_norm))))
                g = int(max(0, min(255, round((g - background[1] * inverse_alpha) / alpha_norm))))
                b = int(max(0, min(255, round((b - background[2] * inverse_alpha) / alpha_norm))))

            if background_is_green and a > 0:
                spill = g - max(r, b)
                if spill > 0:
                    g = max(r, b, g - int(spill * 0.9))

            pixels[x, y] = (r, g, b, a)

    return rgba


def choose_boundaries(alpha_sums: list[int], frame_count: int) -> list[tuple[int, int]]:
    width = len(alpha_sums)
    boundaries = [0]
    for index in range(1, frame_count):
        expected = round(width * index / frame_count)
        search_radius = max(12, width // (frame_count * 6))
        start = max(boundaries[-1] + 1, expected - search_radius)
        end = min(width - 2, expected + search_radius)
        best = min(range(start, end + 1), key=lambda x: (alpha_sums[x], abs(x - expected)))
        boundaries.append(best)
    boundaries.append(width)

    segments: list[tuple[int, int]] = []
    for left, right in zip(boundaries[:-1], boundaries[1:]):
        segments.append((left, right))
    return segments


def crop_to_alpha(image: Image.Image, bounds: tuple[int, int]) -> CropBox:
    rgba = image.convert("RGBA")
    _, height = rgba.size
    left_bound, right_bound = bounds
    segment_width = right_bound - left_bound
    pixels = rgba.load()
    visited = bytearray(segment_width * height)
    largest_area = 0
    largest_box: CropBox | None = None

    def index_for(x: int, y: int) -> int:
        return y * segment_width + (x - left_bound)

    for x in range(left_bound, right_bound):
        for y in range(height):
            component_index = index_for(x, y)
            if visited[component_index]:
                continue

            if pixels[x, y][3] <= ALPHA_THRESHOLD:
                visited[component_index] = 1
                continue

            stack = [(x, y)]
            visited[component_index] = 1
            area = 0
            min_x = x
            min_y = y
            max_x = x
            max_y = y

            while stack:
                current_x, current_y = stack.pop()
                area += 1
                min_x = min(min_x, current_x)
                min_y = min(min_y, current_y)
                max_x = max(max_x, current_x)
                max_y = max(max_y, current_y)

                for offset_y in (-1, 0, 1):
                    for offset_x in (-1, 0, 1):
                        if offset_x == 0 and offset_y == 0:
                            continue

                        next_x = current_x + offset_x
                        next_y = current_y + offset_y
                        if next_x < left_bound or next_x >= right_bound or next_y < 0 or next_y >= height:
                            continue

                        next_index = index_for(next_x, next_y)
                        if visited[next_index]:
                            continue

                        visited[next_index] = 1
                        if pixels[next_x, next_y][3] > ALPHA_THRESHOLD:
                            stack.append((next_x, next_y))

            if area > largest_area:
                largest_area = area
                largest_box = CropBox(min_x, min_y, max_x + 1, max_y + 1)

    if largest_box is None:
        return CropBox(left_bound, 0, right_bound, height)

    return largest_box


def analyze_sheet(source: Path, frame_count: int) -> tuple[Image.Image, list[CropBox]]:
    image = make_background_transparent(Image.open(source))
    alpha_sums = load_alpha_sums(image)
    segments = choose_boundaries(alpha_sums, frame_count)
    boxes = [crop_to_alpha(image, segment) for segment in segments]
    return image, boxes


def scale_box(box: CropBox, scale: float) -> CropBox:
    return CropBox(
        0,
        0,
        max(1, round(box.width * scale)),
        max(1, round(box.height * scale)))


def resize_frame(frame: Image.Image, scale: float) -> Image.Image:
    if abs(scale - 1.0) < 0.001:
        return frame

    width = max(1, round(frame.width * scale))
    height = max(1, round(frame.height * scale))
    return frame.resize((width, height), Image.Resampling.NEAREST)


def keep_primary_components(frame: Image.Image) -> Image.Image:
    rgba = frame.convert("RGBA")
    width, height = rgba.size
    pixels = rgba.load()
    visited = bytearray(width * height)
    components: list[tuple[int, CropBox, list[tuple[int, int]]]] = []

    def index_for(x: int, y: int) -> int:
        return y * width + x

    for x in range(width):
        for y in range(height):
            component_index = index_for(x, y)
            if visited[component_index]:
                continue

            if pixels[x, y][3] <= ALPHA_THRESHOLD:
                visited[component_index] = 1
                continue

            stack = [(x, y)]
            visited[component_index] = 1
            area = 0
            min_x = x
            min_y = y
            max_x = x
            max_y = y
            component_pixels: list[tuple[int, int]] = []

            while stack:
                current_x, current_y = stack.pop()
                area += 1
                component_pixels.append((current_x, current_y))
                min_x = min(min_x, current_x)
                min_y = min(min_y, current_y)
                max_x = max(max_x, current_x)
                max_y = max(max_y, current_y)

                for offset_y in (-1, 0, 1):
                    for offset_x in (-1, 0, 1):
                        if offset_x == 0 and offset_y == 0:
                            continue

                        next_x = current_x + offset_x
                        next_y = current_y + offset_y
                        if next_x < 0 or next_x >= width or next_y < 0 or next_y >= height:
                            continue

                        next_index = index_for(next_x, next_y)
                        if visited[next_index]:
                            continue

                        visited[next_index] = 1
                        if pixels[next_x, next_y][3] > ALPHA_THRESHOLD:
                            stack.append((next_x, next_y))

            components.append((area, CropBox(min_x, min_y, max_x + 1, max_y + 1), component_pixels))

    if not components:
        return rgba

    largest_area, _, _ = max(components, key=lambda item: item[0])
    keep_pixels: set[tuple[int, int]] = set()
    minimum_keep_area = max(24, int(largest_area * 0.12))
    for area, _, component_pixels in components:
        if area >= minimum_keep_area:
            keep_pixels.update(component_pixels)

    for x in range(width):
        for y in range(height):
            if pixels[x, y][3] <= ALPHA_THRESHOLD:
                continue

            if (x, y) not in keep_pixels:
                pixels[x, y] = (0, 0, 0, 0)

    return rgba


def render_aligned_frames(
    source: Path,
    destination_root: Path,
    frame_count: int,
    canvas_width: int,
    canvas_height: int) -> dict:
    image, boxes = analyze_sheet(source, frame_count)
    scale = SCALE_OVERRIDES.get(source.name, 1.0)

    output_dir = destination_root / source.stem.replace("_sheet_v1", "_frames_v1")
    output_dir.mkdir(parents=True, exist_ok=True)

    frame_files: list[str] = []
    for index, box in enumerate(boxes):
        frame = image.crop((box.left, box.top, box.right, box.bottom))
        frame = resize_frame(frame, scale)
        frame = keep_primary_components(frame)
        canvas = Image.new("RGBA", (canvas_width, canvas_height), (0, 0, 0, 0))

        offset_x = (canvas_width - frame.width) // 2
        offset_y = canvas_height - frame.height
        canvas.paste(frame, (offset_x, offset_y), frame)

        filename = f"{source.stem.replace('_sheet_v1', '')}_{index:02d}.png"
        canvas.save(output_dir / filename)
        frame_files.append(filename)

    manifest = {
        "source": source.name,
        "frame_count": frame_count,
        "canvas_width": canvas_width,
        "canvas_height": canvas_height,
        "baseline": canvas_height,
        "scale": scale,
        "frames": frame_files,
    }

    with open(output_dir / "manifest.json", "w", encoding="utf-8") as handle:
        json.dump(manifest, handle, ensure_ascii=False, indent=2)

    return manifest


def main() -> None:
    source_root = Path(r"F:\Godot project\echo-space\Docs\Art\PlayerSprite")
    destination_root = Path(r"F:\Godot project\echo-space\Docs\Art\PlayerSpriteFrames")
    destination_root.mkdir(parents=True, exist_ok=True)

    analysis = {}
    global_canvas_width = 0
    global_canvas_height = 0
    for filename, frame_count in FRAME_COUNTS.items():
        image, boxes = analyze_sheet(source_root / filename, frame_count)
        analysis[filename] = (image, boxes)
        scale = SCALE_OVERRIDES.get(filename, 1.0)
        scaled_boxes = [scale_box(box, scale) for box in boxes]
        global_canvas_width = max(global_canvas_width, max(box.width for box in scaled_boxes))
        global_canvas_height = max(global_canvas_height, max(box.height for box in scaled_boxes))

    manifests = []
    for filename, frame_count in FRAME_COUNTS.items():
        manifests.append(
            render_aligned_frames(
                source_root / filename,
                destination_root,
                frame_count,
                global_canvas_width,
                global_canvas_height))

    with open(destination_root / "manifest.json", "w", encoding="utf-8") as handle:
        json.dump(manifests, handle, ensure_ascii=False, indent=2)


if __name__ == "__main__":
    main()
