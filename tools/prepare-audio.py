#!/usr/bin/env python3
"""Create short mono PCM cues from downloaded originals. Requires system libsndfile.
Originals are untouched. Re-run from any directory; writes a provenance manifest.
"""
import array
import ctypes as C
import ctypes.util
import hashlib
import json
import sys
import wave
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / 'Assets/_Project/Audio/ThirdParty'
OUTPUT = ROOT / 'Assets/_Project/Audio/Prepared'

class Info(C.Structure):
    _fields_ = [('frames', C.c_int64), ('rate', C.c_int), ('channels', C.c_int),
                ('format', C.c_int), ('sections', C.c_int), ('seekable', C.c_int)]

lib = C.CDLL(ctypes.util.find_library('sndfile'))
lib.sf_open.argtypes = [C.c_char_p, C.c_int, C.POINTER(Info)]
lib.sf_open.restype = C.c_void_p
lib.sf_readf_float.argtypes = [C.c_void_p, C.POINTER(C.c_float), C.c_int64]
lib.sf_readf_float.restype = C.c_int64
lib.sf_close.argtypes = [C.c_void_p]

def read(name):
    path = SOURCE / name
    info = Info()
    handle = lib.sf_open(str(path).encode(), 0x10, C.byref(info))
    if not handle:
        raise RuntimeError(f'Cannot decode {path}')
    try:
        data = (C.c_float * (info.frames * info.channels))()
        frames = lib.sf_readf_float(handle, data, info.frames)
        return info.rate, [sum(data[i * info.channels + c] for c in range(info.channels))
                           / info.channels for i in range(frames)]
    finally:
        lib.sf_close(handle)

# Start offsets identified from the original transient envelopes, in seconds.
SLICES = {
    'running-footsteps.wav': [('run-step-1', 9.53, .24), ('run-step-2', 11.99, .24),
                              ('run-step-3', 22.52, .24), ('run-step-4', 23.54, .24)],
    'sand-footsteps.wav': [('sand-step-1', 2.80, .40), ('sand-step-2', 3.45, .40),
                           ('sand-step-3', 5.56, .40), ('sand-step-4', 6.76, .40)],
    'soccer-ball-kick.wav': [('kick-1', .53, .45), ('kick-2', 1.92, .45), ('kick-3', 3.62, .45)],
    'crowd-cheer.wav': [('cheer', .0, 2.3)],
    'volleyball-hit.wav': [('volley-hit', .0, .404)],
    'referee-whistle.wav': [('whistle', .0, .536)],
}

if __name__ == '__main__':
    OUTPUT.mkdir(parents=True, exist_ok=True)
    manifest = []
    for source, slices in SLICES.items():
        rate, data = read(source)
        for name, start, duration in slices:
            segment = data[round(start * rate):round((start + duration) * rate)]
            peak = max(map(abs, segment))
            if peak < 1e-5:
                raise RuntimeError(f'Silent cue: {name}')
            # Peak -6 dBFS, 3 ms attack/30 ms release (cheer: longer crossfade).
            gain = .5 / peak
            attack = int(rate * (.06 if name == 'cheer' else .003))
            release = int(rate * (.35 if name == 'cheer' else .03))
            pcm = array.array('h', (round(max(-1, min(1, value * gain)) * 32767 *
                      min(1, i / max(attack, 1), (len(segment) - 1 - i) / max(release, 1)))
                      for i, value in enumerate(segment)))
            if sys.byteorder != 'little':
                pcm.byteswap()
            path = OUTPUT / (name + '.wav')
            with wave.open(str(path), 'wb') as target:
                target.setparams((1, 2, rate, len(segment), 'NONE', 'not compressed'))
                target.writeframes(pcm.tobytes())
            manifest.append({'file': path.name, 'source': source, 'start_seconds': start,
                             'duration_seconds': len(segment) / rate, 'gain': gain,
                             'edits': 'Trim, stereo to mono, normalize peak to -6 dBFS, fade edges',
                             'source_sha256': hashlib.sha256((SOURCE / source).read_bytes()).hexdigest(),
                             'sha256': hashlib.sha256(path.read_bytes()).hexdigest()})
    (OUTPUT / 'provenance.json').write_text(json.dumps(manifest, indent=2) + '\n')
    print(f'Prepared {len(manifest)} cues in {OUTPUT}')
