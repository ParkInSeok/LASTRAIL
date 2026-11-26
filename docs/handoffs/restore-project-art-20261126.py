"""Restore LASTRAIL FrameStudio or QA art from a finished GitHub release index."""
import argparse
import hashlib
import io
import json
from pathlib import Path, PurePosixPath, PureWindowsPath
import subprocess
import zipfile

REPO = "ParkInSeok/LASTRAIL"
CATALOGS = {
    "framestudio": ("framestudio-source-20261126", "frame-recovery-index.json", "packetSHA256"),
    "qa": ("project-art-and-evidence-20261126", "art-recovery-index.json", "packets"),
}


def stream_hash(stream):
    h = hashlib.sha256()
    for chunk in iter(lambda: stream.read(1024 * 1024), b""):
        h.update(chunk)
    return h.hexdigest()


def download(tag, name):
    return subprocess.check_output([
        "gh", "release", "download", tag, "--repo", REPO, "--pattern", name, "--output", "-"
    ])


def target(root, name):
    rel = PurePosixPath(name)
    win = PureWindowsPath(name)
    if win.anchor or "\\" in name or ":" in name or ".." in rel.parts or not rel.parts:
        raise ValueError("Unexpected recovery path")
    candidate = root.joinpath(*rel.parts).resolve()
    candidate.relative_to(root)
    return candidate


def restore(index, packet_hashes, tag, root, paths):
    groups = {}
    for path in paths:
        dest = target(root, path)
        digest = index["paths"][path]
        packet = index["objects"][digest]
        groups.setdefault(packet, []).append((dest, digest))
    for packet, members in groups.items():
        raw = download(tag, packet)
        if hashlib.sha256(raw).hexdigest() != packet_hashes[packet]:
            raise ValueError("Packet checksum mismatch: " + packet)
        with zipfile.ZipFile(io.BytesIO(raw)) as archive:
            for dest, digest in members:
                content = archive.read(digest)
                if hashlib.sha256(content).hexdigest() != digest:
                    raise ValueError("Source checksum mismatch: " + str(dest))
                dest.parent.mkdir(parents=True, exist_ok=True)
                if dest.exists():
                    with dest.open("rb") as src:
                        if stream_hash(src) != digest:
                            raise FileExistsError(dest)
                    continue
                with dest.open("xb") as out:
                    out.write(content)
                with dest.open("rb") as src:
                    if stream_hash(src) != digest:
                        raise OSError("Restored file checksum mismatch")
    return sum(map(len, groups.values()))


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("catalog", choices=CATALOGS)
    parser.add_argument("destination", type=Path)
    parser.add_argument("--path", action="append", help="Restore this original path only; repeat as needed")
    args = parser.parse_args()
    tag, index_name, key = CATALOGS[args.catalog]
    index = json.loads(download(tag, index_name))
    root = args.destination.resolve()
    print("Verified restored paths:", restore(index, index[key], tag, root, args.path or index["paths"]))


if __name__ == "__main__":
    main()

