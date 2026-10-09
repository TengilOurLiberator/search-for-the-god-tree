"""Run TripoSR on images that already have a transparent background.

Why this exists: TripoSR's own run.py removes backgrounds with the rembg library,
whose default model (BRIA RMBG) is licensed for non-commercial use only. This runner
never touches rembg: it refuses any image without real transparency. Cut the
background out first with an approved tool (stylekit/workflows/birefnet_cutout.api.json).

Usage:
  python tools/pipeline/triposr_run.py --triposr-root D:/godtree-tools/TripoSR \
      --out OUT_DIR [--mc-resolution 256] [--foreground-ratio 0.85] IMAGE.png [IMAGE.png ...]

Writes OUT_DIR/<index>/mesh.glb (vertex colours) and OUT_DIR/<index>/input.png.
"""
import argparse
import os
import sys

import numpy as np
import torch
from PIL import Image


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("images", nargs="+")
    ap.add_argument("--triposr-root", required=True)
    ap.add_argument("--out", required=True)
    ap.add_argument("--mc-resolution", type=int, default=256)
    ap.add_argument("--foreground-ratio", type=float, default=0.85)
    args = ap.parse_args()

    sys.path.insert(0, args.triposr_root)
    # tsr.utils imports rembg at module level; give it an inert stand-in so the real
    # library (and its model download) can never run from here.
    import types
    blocker = types.ModuleType("rembg")

    def _blocked(*_a, **_k):
        raise RuntimeError("rembg is blocked in this pipeline; supply images with transparency")

    blocker.remove = _blocked
    blocker.new_session = _blocked
    sys.modules["rembg"] = blocker

    from tsr.system import TSR
    from tsr.utils import resize_foreground

    device = "cuda:0" if torch.cuda.is_available() else "cpu"
    model = TSR.from_pretrained("stabilityai/TripoSR", config_name="config.yaml", weight_name="model.ckpt")
    model.renderer.set_chunk_size(8192)
    model.to(device)

    for index, path in enumerate(args.images):
        image = Image.open(path)
        if image.mode != "RGBA" or image.getextrema()[3][0] == 255:
            raise SystemExit(f"{path}: needs an alpha channel with real transparency")
        image = resize_foreground(image, args.foreground_ratio)
        rgba = np.array(image).astype(np.float32) / 255.0
        rgb = rgba[:, :, :3] * rgba[:, :, 3:4] + (1 - rgba[:, :, 3:4]) * 0.5
        prepared = Image.fromarray((rgb * 255.0).astype(np.uint8))

        out_dir = os.path.join(args.out, str(index))
        os.makedirs(out_dir, exist_ok=True)
        prepared.save(os.path.join(out_dir, "input.png"))
        with torch.no_grad():
            scene_codes = model([prepared], device=device)
        meshes = model.extract_mesh(scene_codes, True, resolution=args.mc_resolution)
        meshes[0].export(os.path.join(out_dir, "mesh.glb"))
        print(f"TRIPOSR_DONE {index} {os.path.basename(path)}", flush=True)


main()
