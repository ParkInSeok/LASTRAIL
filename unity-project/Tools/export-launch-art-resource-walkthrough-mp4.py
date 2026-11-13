#!/usr/bin/env python3
"""Encode Unity-rendered LAST RAIL art resource walkthrough frames into a 30fps MP4."""

from __future__ import annotations

import argparse
import json
import math
from pathlib import Path
from typing import Any

import numpy as np
from PIL import Image


DEFAULT_WIDTH = 1080
DEFAULT_HEIGHT = 1920
DEFAULT_FPS = 30
DEFAULT_SECONDS = 60
LATEST_VIDEO_NAME = "act1-launch-latest-qa.mp4"


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--project-root", default=str(Path(__file__).resolve().parents[1]))
    parser.add_argument("--frames")
    parser.add_argument("--report")
    parser.add_argument("--output")
    parser.add_argument("--seconds", type=int, default=DEFAULT_SECONDS)
    parser.add_argument("--fps", type=int, default=DEFAULT_FPS)
    parser.add_argument("--pattern", default="frame_*_1080x1920.png")
    parser.add_argument("--capture-mode", default="resource_showcase_render")
    parser.add_argument("--no-high-quality-art-resource-coverage", dest="high_quality_art_resource_coverage", action="store_false")
    parser.set_defaults(high_quality_art_resource_coverage=True)
    args = parser.parse_args()

    project_root = Path(args.project_root).resolve()
    qa_root = project_root / "QA" / "Act1Launch"
    frames_root = Path(args.frames).resolve() if args.frames else qa_root / "Frames" / "ArtResourceWalkthrough"
    report_path = Path(args.report).resolve() if args.report else qa_root / "Reports" / "art-resource-walkthrough-render-report.json"
    output_path = Path(args.output).resolve() if args.output else qa_root / "Video" / LATEST_VIDEO_NAME
    seconds = max(30, min(120, args.seconds))
    fps = max(24, min(60, args.fps))

    frames = sorted(frames_root.glob(args.pattern))
    if not frames:
        raise SystemExit(f"No art walkthrough frames found in {frames_root}")

    width, height = validate_frame_dimensions(frames)
    prepare_single_latest_output(output_path)
    encoded_frame_count, encoder = encode_with_motion(frames, output_path, seconds, fps)
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
        encoded_frame_count,
        fps,
        seconds,
        args.capture_mode,
        args.high_quality_art_resource_coverage,
    )

    print(json.dumps(
        {
            "mp4Path": str(output_path),
            "mp4SizeBytes": size,
            "width": width,
            "height": height,
            "durationSeconds": seconds,
            "fps": fps,
            "sourceFrameCount": len(frames),
            "encodedFrameCount": encoded_frame_count,
            "encoder": encoder,
            "reportPath": str(report_path),
            "captureMode": args.capture_mode,
            "usesFallbackComposite": False,
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


def encode_with_motion(frames: list[Path], output_path: Path, seconds: int, fps: int) -> tuple[int, str]:
    try:
        import imageio.v2 as imageio
        import imageio_ffmpeg
    except ImportError as exc:
        raise SystemExit("imageio/imageio-ffmpeg is required for MP4 encoding.") from exc

    source_images = [Image.open(frame).convert("RGB") for frame in frames]
    total_frames = seconds * fps
    segment_frames = max(1, math.ceil(total_frames / len(source_images)))
    fade_frames = max(6, min(fps // 2, segment_frames // 5))
    _ = imageio_ffmpeg.get_ffmpeg_exe()

    written = 0
    with imageio.get_writer(
        str(output_path),
        format="FFMPEG",
        mode="I",
        fps=fps,
        codec="libx264",
        quality=8,
        macro_block_size=1,
        ffmpeg_params=["-pix_fmt", "yuv420p", "-movflags", "+faststart"],
    ) as writer:
        for index, source in enumerate(source_images):
            next_source = source_images[(index + 1) % len(source_images)]
            for local_frame in range(segment_frames):
                if written >= total_frames:
                    break

                progress = local_frame / max(1, segment_frames - 1)
                image = apply_gentle_camera_motion(source, progress, index)
                if local_frame >= segment_frames - fade_frames and index < len(source_images) - 1:
                    blend = (local_frame - (segment_frames - fade_frames)) / max(1, fade_frames - 1)
                    next_image = apply_gentle_camera_motion(next_source, 0.0, index + 1)
                    image = Image.blend(image, next_image, blend)

                writer.append_data(np.asarray(image))
                written += 1

    return written, "unity_rendered_frames_imageio_ffmpeg"


def apply_gentle_camera_motion(source: Image.Image, progress: float, index: int) -> Image.Image:
    zoom = 1.0 + 0.035 * progress
    width, height = source.size
    crop_w = int(width / zoom)
    crop_h = int(height / zoom)
    pan_x = math.sin((index + 1) * 1.7) * progress
    pan_y = math.cos((index + 1) * 1.3) * progress
    max_x = width - crop_w
    max_y = height - crop_h
    left = int((max_x * 0.5) + pan_x * max_x * 0.45)
    top = int((max_y * 0.5) + pan_y * max_y * 0.35)
    left = max(0, min(max_x, left))
    top = max(0, min(max_y, top))
    return source.crop((left, top, left + crop_w, top + crop_h)).resize((width, height), Image.Resampling.LANCZOS)


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
    encoded_frame_count: int,
    fps: int,
    duration_seconds: int,
    capture_mode: str,
    high_quality_art_resource_coverage: bool,
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
            "captureMode": capture_mode,
            "hasMp4Artifact": True,
            "hasRecorderPackage": has_recorder_package(project_root),
            "mp4Path": str(output_path),
            "mp4Encoder": encoder,
            "mp4SizeBytes": size,
            "mp4Width": width,
            "mp4Height": height,
            "mp4DurationSeconds": duration_seconds,
            "mp4Fps": fps,
            "mp4SourceFrameCount": source_frame_count,
            "mp4EncodedFrameCount": encoded_frame_count,
            "width": width,
            "height": height,
            "frameCount": source_frame_count,
            "estimatedDurationSeconds": duration_seconds,
            "encodedFps": fps,
            "usesFallbackComposite": False,
            "fallbackCompositeFrameCount": 0,
            "hasHighQualityArtResourceCoverage": high_quality_art_resource_coverage,
            "latestVideoPolicy": "single_project_local_mp4",
            "retainedMp4Count": count_mp4_files(output_path.parent),
            "checkedSurfaceCount": checked_surface_count,
        }
    )

    report_path.parent.mkdir(parents=True, exist_ok=True)
    report_path.write_text(json.dumps(data, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


def has_recorder_package(project_root: Path) -> bool:
    manifest_path = project_root / "Packages" / "manifest.json"
    return manifest_path.exists() and "com.unity.recorder" in manifest_path.read_text(encoding="utf-8")


def prepare_single_latest_output(output_path: Path) -> None:
    output_path.parent.mkdir(parents=True, exist_ok=True)
    for candidate in output_path.parent.glob("*.mp4"):
        try:
            candidate.unlink()
        except PermissionError as exc:
            raise SystemExit(
                "Cannot clean previous QA mp4 because it is open in another process. "
                f"Close video players or Explorer previews using this file, then retry: {candidate}"
            ) from exc


def count_mp4_files(video_root: Path) -> int:
    return len(list(video_root.glob("*.mp4"))) if video_root.exists() else 0


if __name__ == "__main__":
    raise SystemExit(main())
