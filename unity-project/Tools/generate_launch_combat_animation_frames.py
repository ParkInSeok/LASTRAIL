#!/usr/bin/env python3
"""Generate visual-only LAST RAIL launch combat animation frame sequences.

The script derives 512x512 frame sequences from existing high-quality launch
operator/enemy/boss art. It does not encode gameplay timing, damage, reward,
economy, story, unlock, offline, gacha, auth, or server data.
"""

from __future__ import annotations

import argparse
import json
from dataclasses import dataclass
from pathlib import Path
from typing import Iterable

from PIL import Image, ImageDraw, ImageEnhance, ImageFilter


UNIT_STATES = ("idle", "attack", "skill_or_special", "hit", "down_or_defeated")
BOSS_STATES = ("idle", "warning", "attack", "hit", "defeat")


@dataclass(frozen=True)
class Spec:
    asset_id: str
    kind: str
    folder: str
    source: str
    states: tuple[str, ...]


def specs() -> list[Spec]:
    operators = [
        "op_vex",
        "op_nyx",
        "op_sable",
        "op_marrow",
        "op_iris",
        "op_rail",
        "op_anchor",
        "op_lumen",
        "op_cinder",
        "op_warden",
    ]
    enemies = [
        "enemy_raider_scout",
        "enemy_railbike_marauder",
        "enemy_scrapshield_brute",
        "enemy_signal_jammer_drone",
        "enemy_spike_cart_gunner",
        "enemy_cinder_hound_mech",
    ]
    elites = ["elite_broadcast_executioner", "elite_ironclad_boarder"]
    result = [
        Spec(op, "operator_elite", "Operators", f"Assets/_LASTRAIL/UI/Operators/Portraits/{op}_portrait.png", UNIT_STATES)
        for op in operators
    ]
    result.extend(
        Spec(enemy, "enemy", "Enemies", f"Assets/_LASTRAIL/UI/CombatResources/Enemies/{enemy}.png", UNIT_STATES)
        for enemy in enemies
    )
    result.extend(
        Spec(elite, "elite_enemy", "EliteEnemies", f"Assets/_LASTRAIL/UI/CombatResources/Elites/{elite}.png", UNIT_STATES)
        for elite in elites
    )
    result.append(
        Spec(
            "boss_raider_king_engine",
            "boss",
            "Bosses",
            "Assets/_LASTRAIL/UI/CombatResources/Bosses/boss_raider_king_engine.png",
            BOSS_STATES,
        )
    )
    return result


def frame_plan(state: str, frame: int) -> tuple[float, int, int, float, float]:
    if state == "idle":
        return (1.0 + (0.012 if frame in (2, 3) else 0.0), 0, -3 if frame in (1, 4) else 2, 0.0, 1.0)
    if state == "attack":
        return ((1.02, -10, 0, -2.0, 1.0), (1.05, 16, -3, 1.0, 1.0), (1.08, 34, -7, 3.0, 1.0), (1.03, 10, 0, 0.0, 1.0))[frame - 1]
    if state == "skill_or_special":
        return ((1.0, 0, 0, 0.0, 1.0), (1.04, 0, -5, -1.0, 1.0), (1.08, 0, -8, 1.0, 1.0), (1.03, 0, -2, 0.0, 1.0))[frame - 1]
    if state == "hit":
        return ((1.0, -11, 0, -3.0, 1.0), (1.0, 9, 1, 2.0, 1.0), (1.0, -6, -1, -1.5, 1.0), (1.0, 0, 0, 0.0, 1.0))[frame - 1]
    if state == "down_or_defeated":
        return ((1.0, 0, 0, 0.0, 1.0), (0.98, -8, 12, -5.0, 0.9), (0.94, -18, 28, -11.0, 0.78), (0.9, -25, 40, -16.0, 0.62))[frame - 1]
    if state == "warning":
        return ((1.0, 0, 0, 0.0, 1.0), (1.03, 0, -4, 0.0, 1.0), (1.06, 0, -6, 0.0, 1.0), (1.02, 0, -2, 0.0, 1.0))[frame - 1]
    if state == "defeat":
        return ((1.0, 0, 0, 0.0, 1.0), (0.98, -10, 12, -2.0, 0.88), (0.94, -22, 32, -5.0, 0.7), (0.9, -34, 52, -7.0, 0.55))[frame - 1]
    return (1.0, 0, 0, 0.0, 1.0)


def fit_source(source: Image.Image, kind: str) -> Image.Image:
    source = source.convert("RGBA")
    max_size = 490 if kind == "boss" else 430
    scale = min(max_size / source.width, max_size / source.height)
    resized = source.resize((max(1, int(source.width * scale)), max(1, int(source.height * scale))), Image.Resampling.LANCZOS)
    if kind.startswith("operator"):
        resized = ImageEnhance.Contrast(resized).enhance(1.06)
        resized = ImageEnhance.Color(resized).enhance(1.05)
    return resized


def draw_effect(draw: ImageDraw.ImageDraw, state: str, frame: int, kind: str) -> None:
    if state == "attack":
        alpha = 80 + frame * 28
        draw.line((122, 345, 390, 180), fill=(255, 190, 82, alpha), width=6)
        draw.line((150, 365, 416, 205), fill=(76, 226, 236, alpha // 2), width=3)
    elif state == "skill_or_special":
        alpha = 42 + frame * 24
        color = (86, 220, 238, alpha)
        draw.ellipse((58 - frame * 4, 58 - frame * 4, 454 + frame * 4, 454 + frame * 4), outline=color, width=5)
        draw.ellipse((130, 390 - frame * 5, 382, 432 + frame * 2), outline=(255, 210, 96, alpha), width=4)
    elif state == "hit":
        draw.rectangle((0, 0, 512, 512), fill=(255, 42, 42, 32 + frame * 9))
        draw.line((80, 145, 430, 392), fill=(255, 72, 56, 130), width=4)
    elif state in ("down_or_defeated", "defeat"):
        draw.rectangle((0, 0, 512, 512), fill=(10, 12, 14, 28 * frame))
    elif state == "warning":
        draw.rectangle((0, 0, 512, 512), fill=(172, 25, 20, 18 + frame * 14))
        draw.arc((70, 70, 442, 442), 205, 335, fill=(255, 72, 32, 90 + frame * 22), width=8)
    if kind == "boss":
        draw.line((60, 448, 452, 448), fill=(255, 72, 38, 85), width=4)


def make_frame(source: Image.Image, spec: Spec, state: str, frame: int) -> Image.Image:
    base = Image.new("RGBA", (512, 512), (0, 0, 0, 0))
    shadow = Image.new("RGBA", (512, 512), (0, 0, 0, 0))
    shadow_draw = ImageDraw.Draw(shadow)
    shadow_draw.ellipse((116, 410, 396, 468), fill=(0, 0, 0, 95))
    shadow = shadow.filter(ImageFilter.GaussianBlur(9))
    base.alpha_composite(shadow)

    scale, dx, dy, rot, opacity = frame_plan(state, frame)
    working = source.resize((max(1, int(source.width * scale)), max(1, int(source.height * scale))), Image.Resampling.LANCZOS)
    if opacity < 1.0:
        alpha = working.getchannel("A").point(lambda value: int(value * opacity))
        working.putalpha(alpha)
    if abs(rot) > 0.001:
        working = working.rotate(rot, resample=Image.Resampling.BICUBIC, expand=True)

    x = (512 - working.width) // 2 + dx
    y = (512 - working.height) // 2 + dy
    base.alpha_composite(working, (x, y))

    fx = Image.new("RGBA", (512, 512), (0, 0, 0, 0))
    draw_effect(ImageDraw.Draw(fx), state, frame, spec.kind)
    base.alpha_composite(fx)
    return base


def write_frames(project_root: Path) -> dict[str, object]:
    output_root = project_root / "Assets/_LASTRAIL/UI/CombatAnimations"
    source_count = 0
    frame_count = 0
    resources = []
    for spec in specs():
        source_path = project_root / spec.source
        if not source_path.exists():
            raise FileNotFoundError(f"Missing source art for {spec.asset_id}: {source_path}")
        source = fit_source(Image.open(source_path), spec.kind)
        source_count += 1
        for state in spec.states:
            state_dir = output_root / spec.folder / spec.asset_id / state
            state_dir.mkdir(parents=True, exist_ok=True)
            for frame in range(1, 5):
                out = state_dir / f"{spec.asset_id}_{state}_{frame:03d}.png"
                make_frame(source.copy(), spec, state, frame).save(out)
                frame_count += 1
        resources.append(
            {
                "id": spec.asset_id,
                "kind": spec.kind,
                "states": list(spec.states),
                "frameCount": len(spec.states) * 4,
                "sourceArtPath": spec.source,
                "qualityClass": "HIGH_QUALITY_2D_ILLUSTRATION",
                "presentationOnly": True,
            }
        )

    report = {
        "name": "LAST RAIL Act 1 Combat Animation Frame Source Report",
        "generatedBy": "local deterministic frame derivation from project launch art",
        "resourceCount": len(resources),
        "sourceArtCount": source_count,
        "frameCount": frame_count,
        "frameResolution": "512x512",
        "presentationOnly": True,
        "hasCombatMath": False,
        "hasServerTransport": False,
        "hasRewardEconomyStoryOfflineGachaTruth": False,
        "resources": resources,
    }
    report_path = output_root / "combat-animation-frame-source-report.json"
    report_path.write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8")
    return report


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--project-root", required=True)
    args = parser.parse_args()
    report = write_frames(Path(args.project_root))
    print(json.dumps(report, ensure_ascii=False, indent=2))


if __name__ == "__main__":
    main()
