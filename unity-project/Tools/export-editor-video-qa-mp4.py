#!/usr/bin/env python3
"""Encode LAST RAIL editor-flow QA PNG frames into a local MP4 artifact."""

from __future__ import annotations

import argparse
import json
import math
from pathlib import Path
from typing import Any

from PIL import Image


DEFAULT_WIDTH = 1080
DEFAULT_HEIGHT = 1920
DEFAULT_SECONDS = 65


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--project-root", default=str(Path.cwd()))
    parser.add_argument("--frames")
    parser.add_argument("--report")
    parser.add_argument("--output")
    parser.add_argument("--seconds", type=int, default=DEFAULT_SECONDS)
    args = parser.parse_args()

    project_root = Path(args.project_root).resolve()
    qa_root = project_root / "QA" / "Act1Launch"
    frames_root = Path(args.frames).resolve() if args.frames else qa_root / "Frames" / "EditorFlowVideoQa"
    report_path = Path(args.report).resolve() if args.report else qa_root / "Reports" / "editor-video-qa-report.json"
    output_path = Path(args.output).resolve() if args.output else qa_root / "Video" / "editor-flow-video-qa.mp4"

    frames = sorted(frames_root.glob("frame_*_1080x1920.png"))
    if not frames:
        raise SystemExit(f"No editor-flow QA frames found in {frames_root}")

    width, height = validate_frame_dimensions(frames)
    duration_seconds = max(60, min(120, args.seconds))
    fps = 1
    repeats_per_frame = max(1, math.ceil(duration_seconds / len(frames)))
    encoded_frame_count = len(frames) * repeats_per_frame
    encoded_duration_seconds = encoded_frame_count // fps

    output_path.parent.mkdir(parents=True, exist_ok=True)
    encoder = encode_with_imageio_ffmpeg(frames, output_path, repeats_per_frame, fps)

    size = output_path.stat().st_size
    if size <= 0:
        raise SystemExit(f"MP4 artifact is empty: {output_path}")

    update_report(
        report_path,
        project_root,
        qa_root,
        output_path,
        encoder,
        size,
        width,
        height,
        len(frames),
        encoded_duration_seconds,
    )

    print(json.dumps(
        {
            "mp4Path": str(output_path),
            "mp4SizeBytes": size,
            "width": width,
            "height": height,
            "durationSeconds": encoded_duration_seconds,
            "sourceFrameCount": len(frames),
            "encodedFrameCount": encoded_frame_count,
            "encoder": encoder,
            "reportPath": str(report_path),
        },
        ensure_ascii=False,
        indent=2,
    ))
    return 0


def validate_frame_dimensions(frames: list[Path]) -> tuple[int, int]:
    expected = None
    for frame in frames:
        with Image.open(frame) as image:
            size = image.size
        if expected is None:
            expected = size
        elif size != expected:
            raise SystemExit(f"Frame dimension mismatch: {frame} is {size}, expected {expected}")

    if expected != (DEFAULT_WIDTH, DEFAULT_HEIGHT):
        raise SystemExit(f"Unexpected frame dimensions: {expected}, expected {(DEFAULT_WIDTH, DEFAULT_HEIGHT)}")

    return expected


def encode_with_imageio_ffmpeg(frames: list[Path], output_path: Path, repeats_per_frame: int, fps: int) -> str:
    try:
        import imageio.v2 as imageio
        import imageio_ffmpeg
    except ImportError as exc:
        raise SystemExit(
            "imageio/imageio-ffmpeg is required for MP4 fallback encoding. "
            "Install them locally or enable Unity Recorder."
        ) from exc

    _ = imageio_ffmpeg.get_ffmpeg_exe()
    with imageio.get_writer(
        str(output_path),
        format="FFMPEG",
        mode="I",
        fps=fps,
        codec="libx264",
        macro_block_size=1,
        ffmpeg_params=["-pix_fmt", "yuv420p", "-movflags", "+faststart"],
    ) as writer:
        for frame in frames:
            image = imageio.imread(str(frame))
            for _ in range(repeats_per_frame):
                writer.append_data(image)

    return "imageio_ffmpeg"


def update_report(
    report_path: Path,
    project_root: Path,
    qa_root: Path,
    output_path: Path,
    encoder: str,
    size: int,
    width: int,
    height: int,
    source_frame_count: int,
    duration_seconds: int,
) -> None:
    if report_path.exists():
        data: dict[str, Any] = json.loads(report_path.read_text(encoding="utf-8-sig"))
    else:
        data = {}

    surfaces = data.get("checkedSurfaces", [])
    checked_surface_count = len(surfaces) if isinstance(surfaces, list) else 0
    data.update(
        {
            "status": "PASS" if int(data.get("failCount", 0)) == 0 else "FAIL",
            "outputRoot": str(qa_root),
            "videoFormat": "mp4",
            "hasMp4Artifact": True,
            "hasRecorderPackage": has_recorder_package(project_root),
            "mp4Path": str(output_path),
            "mp4Encoder": encoder,
            "mp4SizeBytes": size,
            "mp4Width": width,
            "mp4Height": height,
            "mp4DurationSeconds": duration_seconds,
            "mp4SourceFrameCount": source_frame_count,
            "width": width,
            "height": height,
            "frameCount": source_frame_count,
            "estimatedDurationSeconds": duration_seconds,
            "checkedSurfaceCount": checked_surface_count,
        }
    )

    if int(data.get("failCount", 0)) == 0 and not data.get("blockers"):
        data["warningCount"] = 0

    report_path.parent.mkdir(parents=True, exist_ok=True)
    report_path.write_text(json.dumps(data, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


def has_recorder_package(project_root: Path) -> bool:
    manifest_path = project_root / "Packages" / "manifest.json"
    if not manifest_path.exists():
        return False

    return "com.unity.recorder" in manifest_path.read_text(encoding="utf-8")


if __name__ == "__main__":
    raise SystemExit(main())
