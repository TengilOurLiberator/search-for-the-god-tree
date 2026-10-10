"""A small local server that gives the sandbox's NPCs a voice and ears.

  GET  /health            -> {"ok": true, "tts": bool, "stt": bool}
  POST /tts   JSON {"text": "...", "voice": "am_michael", "speed": 1.0}  -> audio/wav (mono, 16-bit)
  POST /stt   body: a .wav file                                         -> {"text": "..."}

Speaking is Kokoro-82M (Apache-2.0 weights) through kokoro-onnx (MIT); hearing is Whisper
small.en (MIT) through faster-whisper (MIT), on the processor so the graphics card is left to
the game. Standard library HTTP only. It listens on this machine alone (127.0.0.1).

Run with the Python in D:/godtree-tools/voice-venv:
  python godtree_voice_server.py --models D:/godtree-tools/models/voice --port 8791

Licence note: kokoro-onnx turns text into sounds with phonemizer and espeak-ng, both GPL-3.
Here they are separate programs on the developer's PC, which is fine. They must not be bundled
into the game as shipped without checking that again (see THIRD_PARTY_LICENCES.md).
"""
import argparse
import io
import json
import sys
import threading
import wave
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer

import numpy as np

parser = argparse.ArgumentParser()
parser.add_argument("--models", required=True)
parser.add_argument("--port", type=int, default=8791)
parser.add_argument("--whisper", default="small.en")
args = parser.parse_args()

tts = None
stt = None
tts_lock = threading.Lock()
stt_lock = threading.Lock()

try:
    from kokoro_onnx import Kokoro
    tts = Kokoro(args.models + "/kokoro-v1.0.onnx", args.models + "/voices-v1.0.bin")
    print("voice: Kokoro ready", flush=True)
except Exception as error:  # the server still answers /health and says what is missing
    print("voice: Kokoro not available: %r" % (error,), flush=True)

try:
    from faster_whisper import WhisperModel
    stt = WhisperModel(args.whisper, device="cpu", compute_type="int8")
    print("ears: Whisper %s ready" % args.whisper, flush=True)
except Exception as error:
    print("ears: Whisper not available: %r" % (error,), flush=True)


def to_wav(samples, rate):
    pcm = (np.clip(samples, -1.0, 1.0) * 32767.0).astype("<i2")
    out = io.BytesIO()
    with wave.open(out, "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(int(rate))
        w.writeframes(pcm.tobytes())
    return out.getvalue()


def from_wav(data):
    with wave.open(io.BytesIO(data), "rb") as w:
        rate, channels, width = w.getframerate(), w.getnchannels(), w.getsampwidth()
        raw = w.readframes(w.getnframes())
    if width != 2:
        raise ValueError("only 16-bit wav is accepted")
    samples = np.frombuffer(raw, dtype="<i2").astype(np.float32) / 32768.0
    if channels > 1:
        samples = samples.reshape(-1, channels).mean(axis=1)
    if rate != 16000:  # Whisper wants 16 kHz
        positions = np.arange(0, len(samples), rate / 16000.0)
        samples = np.interp(positions, np.arange(len(samples)), samples).astype(np.float32)
    return samples


class Handler(BaseHTTPRequestHandler):
    def log_message(self, *_):
        pass

    def reply(self, code, body, kind="application/json"):
        if isinstance(body, (dict, list)):
            body = json.dumps(body).encode("utf-8")
        self.send_response(code)
        self.send_header("Content-Type", kind)
        self.send_header("Content-Length", str(len(body)))
        self.end_headers()
        self.wfile.write(body)

    def do_GET(self):
        if self.path == "/health":
            self.reply(200, {"ok": True, "tts": tts is not None, "stt": stt is not None})
        else:
            self.reply(404, {"error": "not found"})

    def do_POST(self):
        try:
            data = self.rfile.read(int(self.headers.get("Content-Length", "0")))
            if self.path == "/tts":
                if tts is None:
                    return self.reply(503, {"error": "voice not loaded"})
                request = json.loads(data.decode("utf-8"))
                text = str(request.get("text", "")).strip()[:600]
                if not text:
                    return self.reply(400, {"error": "no text"})
                with tts_lock:
                    samples, rate = tts.create(text, voice=str(request.get("voice", "af_heart")),
                                               speed=float(request.get("speed", 1.0)), lang="en-us")
                self.reply(200, to_wav(samples, rate), "audio/wav")
            elif self.path == "/stt":
                if stt is None:
                    return self.reply(503, {"error": "ears not loaded"})
                samples = from_wav(data)
                with stt_lock:
                    segments, _ = stt.transcribe(samples, language="en", beam_size=1, condition_on_previous_text=False)
                    text = " ".join(segment.text.strip() for segment in segments).strip()
                self.reply(200, {"text": text})
            else:
                self.reply(404, {"error": "not found"})
        except Exception as error:
            self.reply(500, {"error": repr(error)})


server = ThreadingHTTPServer(("127.0.0.1", args.port), Handler)
print("listening on 127.0.0.1:%d" % args.port, flush=True)
try:
    server.serve_forever()
except KeyboardInterrupt:
    sys.exit(0)
