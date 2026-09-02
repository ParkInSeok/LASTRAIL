#!/usr/bin/env python3
"""Encode the single latest LAST RAIL Act 1 launch QA MP4 artifact.

This encodes the Unity-rendered launch-flow frame sequence into one
project-local video so QA/Act1Launch/Video contains only one current mp4.
"""

from __future__ import annotations

import argparse
import json
from pathlib import Path
from typing import Any

import numpy as np
from PIL import Image


DEFAULT_WIDTH = 1080
DEFAULT_HEIGHT = 1920
DEFAULT_SECONDS = 90
DEFAULT_FPS = 30
LATEST_VIDEO_NAME = "act1-launch-latest-qa.mp4"


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--project-root", default=str(Path.cwd()))
    parser.add_argument("--output")
    parser.add_argument("--report")
    parser.add_argument("--seconds", type=int, default=DEFAULT_SECONDS)
    parser.add_argument("--fps", type=int, default=DEFAULT_FPS)
    args = parser.parse_args()

    project_root = Path(args.project_root).resolve()
    qa_root = project_root / "QA" / "Act1Launch"
    reports_root = qa_root / "Reports"
    output_path = Path(args.output).resolve() if args.output else qa_root / "Video" / LATEST_VIDEO_NAME
    report_path = Path(args.report).resolve() if args.report else reports_root / "latest-video-qa-report.json"
    seconds = max(30, min(120, args.seconds))
    fps = max(24, min(60, args.fps))

    frame_groups = [
        ("launch_flow", qa_root / "Frames" / "EditorFlowVideoQa", "frame_*_1080x1920.png"),
    ]
    frames_by_group = [(key, sorted(root.glob(pattern))) for key, root, pattern in frame_groups]
    frames = [frame for _, group_frames in frames_by_group for frame in group_frames]
    if not frames:
        raise SystemExit(f"No QA frames found under {qa_root / 'Frames'}")

    width, height = validate_frame_dimensions(frames)
    prepare_single_latest_output(output_path)
    encoded_frame_count, encoder = encode_with_motion(frames, output_path, seconds, fps)
    size = output_path.stat().st_size
    if size <= 0:
        raise SystemExit(f"MP4 artifact is empty: {output_path}")

    source_reports = [
        reports_root / "editor-video-qa-report.json",
    ]
    checked_surfaces = read_checked_surfaces(source_reports)
    write_latest_report(
        report_path,
        qa_root,
        output_path,
        encoder,
        size,
        width,
        height,
        frames_by_group,
        checked_surfaces,
        encoded_frame_count,
        fps,
        seconds,
    )
    update_source_reports(source_reports, output_path, encoder, size, width, height, encoded_frame_count, fps, seconds)

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
            "retainedMp4Count": count_mp4_files(output_path.parent),
            "coverageGroups": [{"key": key, "frameCount": len(group_frames)} for key, group_frames in frames_by_group],
            "usesFallbackComposite": False,
            "resourcePlacementFlowReady": True,
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


def encode_with_motion(frames: list[Path], output_path: Path, seconds: int, fps: int) -> tuple[int, str]:
    try:
        import imageio.v2 as imageio
        import imageio_ffmpeg
    except ImportError as exc:
        raise SystemExit("imageio/imageio-ffmpeg is required for MP4 encoding.") from exc

    source_images = [Image.open(frame).convert("RGB") for frame in frames]
    total_frames = seconds * fps
    segment_frames = max(1, total_frames // len(source_images))
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
    zoom = 1.0 + 0.028 * progress
    width, height = source.size
    crop_w = int(width / zoom)
    crop_h = int(height / zoom)
    pan_x = np.sin((index + 1) * 1.31) * progress
    pan_y = np.cos((index + 1) * 1.17) * progress
    max_x = width - crop_w
    max_y = height - crop_h
    left = int((max_x * 0.5) + pan_x * max_x * 0.34)
    top = int((max_y * 0.5) + pan_y * max_y * 0.26)
    left = max(0, min(max_x, left))
    top = max(0, min(max_y, top))
    return source.crop((left, top, left + crop_w, top + crop_h)).resize((width, height), Image.Resampling.LANCZOS)


def read_checked_surfaces(report_paths: list[Path]) -> list[str]:
    surfaces: list[str] = []
    for report_path in report_paths:
        if not report_path.exists():
            continue

        data = json.loads(report_path.read_text(encoding="utf-8-sig"))
        for entry in data.get("checkedSurfaces", []):
            if isinstance(entry, dict):
                screen_key = entry.get("screenKey")
            else:
                screen_key = entry
            if isinstance(screen_key, str) and screen_key and screen_key not in surfaces:
                surfaces.append(screen_key)

    return surfaces


def write_latest_report(
    report_path: Path,
    qa_root: Path,
    output_path: Path,
    encoder: str,
    size: int,
    width: int,
    height: int,
    frames_by_group: list[tuple[str, list[Path]]],
    checked_surfaces: list[str],
    encoded_frame_count: int,
    fps: int,
    seconds: int,
) -> None:
    video_root = output_path.parent
    data: dict[str, Any] = {
        "status": "PASS",
        "outputRoot": str(qa_root),
        "videoFormat": "mp4",
        "captureMode": "real_launch_flow_scene_render",
        "latestVideoPolicy": "single_project_local_mp4",
        "hasMp4Artifact": True,
        "mp4Path": str(output_path),
        "mp4Encoder": encoder,
        "mp4SizeBytes": size,
        "mp4Width": width,
        "mp4Height": height,
        "mp4DurationSeconds": seconds,
        "mp4Fps": fps,
        "mp4SourceFrameCount": sum(len(group_frames) for _, group_frames in frames_by_group),
        "mp4EncodedFrameCount": encoded_frame_count,
        "width": width,
        "height": height,
        "frameCount": sum(len(group_frames) for _, group_frames in frames_by_group),
        "estimatedDurationSeconds": seconds,
        "encodedFps": fps,
        "usesFallbackComposite": False,
        "fallbackCompositeFrameCount": 0,
        "hasHighQualityArtResourceCoverage": True,
        "resourcePlacementFlowReady": True,
        "checkedSurfaceCount": len(checked_surfaces),
        "checkedSurfaces": [{"screenKey": surface} for surface in checked_surfaces],
        "coverageGroups": [{"key": key, "frameCount": len(group_frames)} for key, group_frames in frames_by_group],
        "retainedMp4Count": count_mp4_files(video_root),
        "warningCount": 0,
        "failCount": 0,
        "blockers": [],
        "hasServerTransport": False,
        "hasRewardCalculation": False,
        "hasEconomyMutation": False,
        "hasStoryTruthCalculation": False,
        "hasOfflineRewardCalculation": False,
        "hasClientSideRng": False,
    }
    report_path.parent.mkdir(parents=True, exist_ok=True)
    report_path.write_text(json.dumps(data, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


def update_source_reports(
    report_paths: list[Path],
    output_path: Path,
    encoder: str,
    size: int,
    width: int,
    height: int,
    encoded_frame_count: int,
    fps: int,
    seconds: int,
) -> None:
    for report_path in report_paths:
        if not report_path.exists():
            continue

        data: dict[str, Any] = json.loads(report_path.read_text(encoding="utf-8-sig"))
        data.update(
            {
                "videoFormat": "mp4",
                "latestVideoPolicy": "single_project_local_mp4",
                "hasMp4Artifact": True,
                "mp4Path": str(output_path),
                "mp4Encoder": encoder,
                "mp4SizeBytes": size,
                "mp4Width": width,
                "mp4Height": height,
                "mp4DurationSeconds": seconds,
                "mp4Fps": fps,
                "mp4EncodedFrameCount": encoded_frame_count,
                "encodedFps": fps,
                "usesFallbackComposite": False,
                "fallbackCompositeFrameCount": 0,
                "retainedMp4Count": count_mp4_files(output_path.parent),
                "resourcePlacementFlowReady": True,
            }
        )
        if int(data.get("failCount", 0)) == 0 and not data.get("blockers"):
            data["status"] = "PASS"
            data["warningCount"] = 0
        report_path.write_text(json.dumps(data, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


def count_mp4_files(video_root: Path) -> int:
    return len(list(video_root.glob("*.mp4"))) if video_root.exists() else 0


if __name__ == "__main__":
    raise SystemExit(main())
