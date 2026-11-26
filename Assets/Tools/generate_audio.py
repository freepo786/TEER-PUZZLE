#!/usr/bin/env python3
"""
Teer Puzzle — sound effects generator (v1.1)
=============================================
11 SFX .wav files banata hai — 44.1 kHz, 16-bit, mono.
Sirf Python stdlib (wave / math / random / struct) — CI mein koi
extra install nahi chahiye.

Har sound original ki reverse-engineered recipe se bani hai:
decaying sine tones, pitch sweeps (chirps), two-note chimes aur
aik band-passed noise whoosh. Durations original jaisi (exact
sample counts), taake game timing na badle.

Output do jagah likhta hai (dono copies identical):
    Assets/Audio/            (Unity import)
    Assets/Resources/Audio/  (Resources.Load ke liye)

Usage:
    python3 Assets/Tools/generate_audio.py [--out-dir DIR]
    --out-dir dene par sirf us dir mein likhta hai (testing ke liye).
"""
import argparse
import math
import os
import random
import struct
import wave

# ---------------------------------------------------------------- paths
TOOLS_DIR = os.path.dirname(os.path.abspath(__file__))
PROJECT_ROOT = os.path.dirname(os.path.dirname(TOOLS_DIR))
AUDIO_DIRS = [
    os.path.join(PROJECT_ROOT, "Assets", "Audio"),
    os.path.join(PROJECT_ROOT, "Assets", "Resources", "Audio"),
]

SR = 44100  # sample rate — original jaisa

# ---------------------------------------------------------------- helpers

def write_wav(path, samples):
    """Float samples (-1..1) -> 16-bit mono WAV."""
    frames = b"".join(
        struct.pack("<h", max(-32767, min(32767, int(s * 32767))))
        for s in samples
    )
    with wave.open(path, "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(SR)
        w.writeframes(frames)


def save_both(samples, name, out_dirs):
    for d in out_dirs:
        os.makedirs(d, exist_ok=True)
        write_wav(os.path.join(d, name), samples)
    print(f"  wrote {name} ({len(samples)} samples, {len(samples)/SR:.2f}s)")


def tone(freq, n, amp=0.6, tau=None):
    """Decaying sine. tau = decay time-constant (seconds); None = no decay."""
    out = []
    for i in range(n):
        t = i / SR
        env = math.exp(-t / tau) if tau else 1.0
        out.append(amp * env * math.sin(2 * math.pi * freq * t))
    return out


def chirp(f0, f1, n, amp=0.6, tau=None, attack=None):
    """Linear pitch sweep (f0 -> f1) with exponential decay.
    attack (seconds) dene par shuru mein fade-in bhi hota hai."""
    dur = n / SR
    out = []
    phase = 0.0
    for i in range(n):
        t = i / SR
        f = f0 + (f1 - f0) * (t / dur)
        phase += 2 * math.pi * f / SR
        env = math.exp(-t / tau) if tau else 1.0
        if attack:
            env *= (1 - math.exp(-t / attack))
        out.append(amp * env * math.sin(phase))
    return out


def notes(seq, amp=0.6, tau=0.4):
    """Melody: [(freq_hz, seconds), ...] — har note fresh attack."""
    out = []
    for freq, dur in seq:
        out += tone(freq, int(dur * SR), amp=amp, tau=tau)
    return out


def whoosh(n, amp=0.55, seed=1234):
    """Band-passed noise — centre freq upar phir neeche, sin envelope.
    RBJ biquad bandpass (Q ~ 1.2), pure stdlib."""
    rnd = random.Random(seed)
    out = []
    x1 = x2 = y1 = y2 = 0.0
    dur = n / SR
    for i in range(n):
        t = i / SR
        frac = t / dur
        # centre freq: 500 Hz -> 2400 Hz -> 500 Hz (parabola)
        fc = 500 + 1900 * 4 * frac * (1 - frac)
        w0 = 2 * math.pi * fc / SR
        alpha = math.sin(w0) / (2 * 1.2)
        cw = math.cos(w0)
        b0, b1, b2 = alpha, 0.0, -alpha
        a0, a1, a2 = 1 + alpha, -2 * cw, 1 - alpha
        x0 = rnd.uniform(-1, 1)
        y0 = (b0 * x0 + b1 * x1 + b2 * x2 - a1 * y1 - a2 * y2) / a0
        x2, x1 = x1, x0
        y2, y1 = y1, y0
        env = math.sin(math.pi * frac) ** 1.5
        out.append(amp * env * y0)
    # normalise to target peak (filter gain varies)
    peak = max(abs(s) for s in out) or 1.0
    return [s * (amp / peak) * 0.9 for s in out]


# ---------------------------------------------------------------- sounds
# (name, exact sample count, generator) — durations original jaisi.

SOUNDS = [
    # 900 Hz tap blip, fast decay (0.07s)
    ("arrow_tap.wav", 3087, lambda: tone(900, 3087, amp=0.55, tau=0.02)),
    # 1140 -> 1000 Hz click (0.08s)
    ("button_click.wav", 3528, lambda: chirp(1140, 1000, 3528, amp=0.6, tau=0.02)),
    # coin: B5 -> E6, do notes (0.20s)
    ("coin.wav", 8820, lambda: notes([(988, 0.10), (1319, 0.10)], amp=0.60, tau=0.10)),
    # low thud: 160 -> 90 Hz, 4ms attack + decay (0.12s)
    ("error_bump.wav", 5292, lambda: chirp(160, 90, 5292, amp=1.0, tau=0.025, attack=0.004)),
    # hint chime: A5 -> E6 (0.35s)
    ("hint.wav", 15434, lambda: notes([(880, 0.175), (1319, 0.175)], amp=0.55, tau=0.20)),
    # rising sweep 440 -> 820 Hz (0.30s)
    ("level_start.wav", 13230, lambda: chirp(440, 820, 13230, amp=0.58, tau=0.30)),
    # falling sweep 375 -> 150 Hz (0.60s)
    ("lose.wav", 26460, lambda: chirp(375, 150, 26460, amp=0.58, tau=0.30)),
    # falling sweep 225 -> 125 Hz (0.40s)
    ("out_of_lives.wav", 17640, lambda: chirp(225, 125, 17640, amp=0.58, tau=0.25)),
    # noise whoosh (0.25s)
    ("slide_whoosh.wav", 11025, lambda: whoosh(11025, amp=0.55)),
    # 659 -> 1200 Hz rising pop (0.15s)
    ("star_pop.wav", 6615, lambda: chirp(650, 1200, 6615, amp=0.68, tau=0.06)),
    # win jingle: C5 - E5 - G5 - C6 arpeggio (1.20s)
    ("win_jingle.wav", 52920, lambda: notes(
        [(523, 0.30), (659, 0.30), (784, 0.30), (1047, 0.30)], amp=0.55, tau=0.35)),
]


def main():
    ap = argparse.ArgumentParser(description="Teer Puzzle SFX generator")
    ap.add_argument("--out-dir", default=None,
                    help="Sirf is dir mein likho (testing ke liye)")
    args = ap.parse_args()
    out_dirs = [args.out_dir] if args.out_dir else AUDIO_DIRS

    print("Teer Puzzle audio generator — output:", ", ".join(out_dirs))
    for name, n, gen in SOUNDS:
        samples = gen()
        assert len(samples) == n, f"{name}: {len(samples)} != {n}"
        save_both(samples, name, out_dirs)
    print("Done — 11 WAVs x %d dir(s), 44.1 kHz 16-bit mono." % len(out_dirs))


if __name__ == "__main__":
    main()
