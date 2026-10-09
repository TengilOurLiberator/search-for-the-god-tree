"""Turn raw generated audio (.flac) into shipping audio (.ogg Vorbis, 48 kHz).

Usage:
  python tools/pipeline/finish_audio.py IN_DIR OUT_DIR --kind sfx|music [--stereo name1,name2]

sfx:   mixed to mono (positional sounds are mono, canon/audio-bible.md) unless the file
       name starts with one of --stereo; leading and trailing silence trimmed; short
       fades; peak set to -3 dBFS.
music: stereo kept; peak set to -1 dBFS; no trimming.

Prints one line of measurements per file and flags anything silent or clipped. The
numbers say a file is well-formed; they cannot say whether it sounds good.

Needs: numpy, scipy, soundfile (libsndfile with Vorbis).
"""
import argparse
import json
import math
import pathlib

import numpy as np
import soundfile as sf
from scipy.signal import resample_poly

TARGET_RATE = 48000


def db(x):
    return -120.0 if x <= 1e-6 else 20.0 * math.log10(x)


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("in_dir")
    ap.add_argument("out_dir")
    ap.add_argument("--kind", choices=["sfx", "music"], required=True)
    ap.add_argument("--stereo", default="")
    args = ap.parse_args()

    out_dir = pathlib.Path(args.out_dir)
    out_dir.mkdir(parents=True, exist_ok=True)
    stereo_prefixes = [s for s in args.stereo.split(",") if s]
    report = []

    for src in sorted(pathlib.Path(args.in_dir).glob("*.flac")):
        data, rate = sf.read(str(src), dtype="float32", always_2d=True)
        raw_peak = float(np.max(np.abs(data))) if data.size else 0.0
        clipped_share = float(np.mean(np.abs(data) >= 0.999)) if data.size else 0.0
        keep_stereo = args.kind == "music" or any(src.stem.startswith(p) for p in stereo_prefixes)
        if not keep_stereo:
            data = data.mean(axis=1, keepdims=True)

        if rate != TARGET_RATE:
            g = math.gcd(TARGET_RATE, rate)
            data = resample_poly(data, TARGET_RATE // g, rate // g, axis=0).astype(np.float32)

        if args.kind == "sfx" and not keep_stereo:
            level = np.max(np.abs(data), axis=1)
            loud = np.nonzero(level > 10 ** (-50 / 20))[0]
            if loud.size:
                start = max(0, loud[0] - int(0.005 * TARGET_RATE))
                end = min(len(data), loud[-1] + int(0.08 * TARGET_RATE))
                data = data[start:end]

        fade_in = min(len(data), int(0.004 * TARGET_RATE))
        fade_out = min(len(data), int((0.5 if keep_stereo else 0.03) * TARGET_RATE))
        if fade_in:
            data[:fade_in] *= np.linspace(0, 1, fade_in, dtype=np.float32)[:, None]
        if fade_out:
            data[-fade_out:] *= np.linspace(1, 0, fade_out, dtype=np.float32)[:, None]

        peak = float(np.max(np.abs(data))) if data.size else 0.0
        target = 10 ** ((-1.0 if args.kind == "music" else -3.0) / 20)
        if peak > 1e-5:
            data = data * (target / peak)
        rms = float(np.sqrt(np.mean(np.square(data)))) if data.size else 0.0

        dst = out_dir / (src.stem + ".ogg")
        # Written in one-second blocks: libsndfile's Vorbis encoder overflows the stack
        # on Windows when handed a long file in a single call.
        with sf.SoundFile(str(dst), "w", samplerate=TARGET_RATE, channels=data.shape[1], format="OGG", subtype="VORBIS") as out:
            for offset in range(0, len(data), TARGET_RATE):
                out.write(data[offset:offset + TARGET_RATE])
        flags = []
        if db(rms) < -55:
            flags.append("SILENT")
        if clipped_share > 0.0005:  # more than 0.05 % of samples at full scale
            flags.append("SOURCE_CLIPPED")
        entry = {
            "file": dst.name, "seconds": round(len(data) / TARGET_RATE, 2), "channels": data.shape[1],
            "source_peak_db": round(db(raw_peak), 1), "rms_db": round(db(rms), 1),
            "kb": round(dst.stat().st_size / 1024), "flags": flags,
        }
        report.append(entry)
        print(f"{entry['file']:44s} {entry['seconds']:6.2f}s ch{entry['channels']} "
              f"src_peak {entry['source_peak_db']:6.1f} dB  rms {entry['rms_db']:6.1f} dB  {entry['kb']:5d} KB  {' '.join(flags)}")

    (out_dir / "audio-report.json").write_text(json.dumps(report, indent=1) + "\n", encoding="utf-8")
    print(f"AUDIO_RESULT files={len(report)} flagged={sum(1 for r in report if r['flags'])}")


main()
