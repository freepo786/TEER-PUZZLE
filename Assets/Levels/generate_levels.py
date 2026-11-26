#!/usr/bin/env python3
"""Teer Puzzle - deterministic generator for solver-verified levels.

Generates level_001.json ... level_NNN.json in Assets/Levels/ plus identical
copies in Assets/Resources/Levels/ (Unity loads them at runtime with
Resources.LoadAll). Default count is 2000 (--count to change it).

Method (reverse-removal construction, guarantees the greedy solver passes):
  Arrows are placed one by one. When an arrow is placed, its exit ray must
  be clear of every arrow placed BEFORE it (and of its own path). Arrows
  placed later may block its ray. The greedy solver then removes arrows in
  the exact reverse of the placement order: the last-placed arrow is free
  at the start, and after removing it the previous one becomes free, and
  so on. Every generated level is still checked with the independent
  greedy solver below and only PASS levels are saved.

  To reach the required "blocked at start" ratios, the exit ray length is
  adapted to how full the board already is: early arrows (empty board)
  get long rays (2-3 cells, likely to be blocked later), late arrows
  (crowded board) get short rays (0-1 cells, so placement stays possible).
  New arrows start next to already occupied cells (frontier starts) so the
  remaining free space stays contiguous instead of fragmenting.

  A running cell budget guarantees the paths fit: an arrow may use at most
  (free cells - 2 per arrow still to place - SPARE) cells, so the last
  arrows always have room.

Deterministic: fixed seed (default 20261126). Running the script again
with the same seed reproduces byte-identical files.

Standard library only. No network, no pip.

Level JSON format (exactly what the Unity scripts parse):
  {"id": 1, "pack": "easy", "width": 5, "height": 5, "lives": 3,
   "arrows": [{"color": 0,
               "nodalPoints": [{"column": 1, "row": 4},
                               {"column": 1, "row": 3},
                               {"column": 2, "row": 3}]}]}
  column 0..width-1 left->right, row 0..height-1 top->bottom.
  A path lists cells tail -> head; the head is the last cell. The exit
  direction is from the second-last cell to the last cell.
  Rules: every step is 1 orthogonal cell (no diagonals), a path never
  revisits a cell, two arrows never share a cell, color is 0-7.
"""

import argparse
import hashlib
import json
import random
import sys
from pathlib import Path

SEED_DEFAULT = 20261126
MAX_TRIES = 500          # max generation attempts per level
SPARE = 8                # free cells deliberately left over at the end
DIRS = [(0, -1), (0, 1), (-1, 0), (1, 0)]  # up, down, left, right

# Per-pack settings (grids, path lengths, weights, blocked ratio).
# The id ranges are computed from --count in build_tiers():
#   Easy 30%, Medium 30%, Hard 25%, Expert 15% of the total count
#   (count=2000 -> Easy 1-600, Medium 601-1200, Hard 1201-1700,
#    Expert 1701-2000). "ids" is filled in by build_tiers().
PACK_FRACTIONS = [("easy", 0.30), ("medium", 0.30),
                  ("hard", 0.25), ("expert", 0.15)]
TIER_CONFIG = {
    "easy": {
        "grids": [(4, 4, 4, 5), (5, 5, 5, 7)],
        "len_range": (2, 4),
        "weights": [3, 2, 1],
        "min_blocked": 0.00,
    },
    "medium": {
        "grids": [(6, 6, 8, 10), (7, 7, 10, 14)],
        "len_range": (2, 5),
        "weights": [4, 3, 2, 1],
        "min_blocked": 0.15,
    },
    "hard": {
        "grids": [(8, 8, 15, 22)],
        "len_range": (2, 6),
        "weights": [5, 3, 2, 1, 1],
        "min_blocked": 0.30,
    },
    "expert": {
        "grids": [(9, 9, 24, 26), (10, 10, 26, 32)],
        "len_range": (2, 6),
        "weights": [5, 3, 2, 1, 1],
        "min_blocked": 0.45,
    },
}
TIERS = {}
PACK_BY_ID = {}


def build_tiers(count):
    """Fill TIERS / PACK_BY_ID with pack id ranges for `count` levels."""
    global TIERS, PACK_BY_ID
    TIERS = {}
    PACK_BY_ID = {}
    start = 1
    for idx, (pack, frac) in enumerate(PACK_FRACTIONS):
        if idx == len(PACK_FRACTIONS) - 1:
            n = count - start + 1  # last pack takes the remainder
        else:
            n = int(count * frac)
        cfg = dict(TIER_CONFIG[pack])
        cfg["ids"] = list(range(start, start + n))
        TIERS[pack] = cfg
        for i in cfg["ids"]:
            PACK_BY_ID[i] = pack
        start += n
    return TIERS


# ---------------------------------------------------------------- helpers

def ray_cells(head, direction, w, h):
    """Cells from head (exclusive) to the board edge in `direction`."""
    dx, dy = direction
    cells = []
    c, r = head[0] + dx, head[1] + dy
    while 0 <= c < w and 0 <= r < h:
        cells.append((c, r))
        c += dx
        r += dy
    return cells


def exit_dir(path):
    (c1, r1), (c2, r2) = path[-2], path[-1]
    return (c2 - c1, r2 - r1)


def target_ray_len(occupied_frac):
    """Preferred exit-ray length for a new arrow, by board fullness."""
    if occupied_frac < 0.30:
        return 3
    if occupied_frac < 0.50:
        return 2
    if occupied_frac < 0.70:
        return 1
    return 0


def edge_dist(cell, w, h):
    c, r = cell
    return min(c, w - 1 - c, r, h - 1 - r)


# ------------------------------------------------------------- generation

def gen_arrow(rng, w, h, occ, length, walk_tries=60):
    """Place one arrow by random walk.

    The arrow's exit ray must be clear of all already placed arrows
    (`occ`) and of its own path. Returns the path (list of (col, row),
    tail -> head) or None if no walk worked.
    """
    free = [(c, r) for c in range(w) for r in range(h) if (c, r) not in occ]
    if len(free) < length:
        return None
    free_set = set(free)
    if length >= 2:
        viable = [cell for cell in free
                  if any((cell[0] + dx, cell[1] + dy) in free_set
                         for dx, dy in DIRS)]
        if not viable:
            return None
    else:
        viable = free
    # Frontier starts: free cells next to occupied ones. Growing from the
    # occupied cluster keeps the remaining free space contiguous, so late
    # arrows still find adjacent free cells.
    frontier = [cell for cell in viable
                if any((cell[0] + dx, cell[1] + dy) in occ
                       for dx, dy in DIRS)]
    starts = frontier if frontier else viable

    frac = 1.0 - len(free) / (w * h)
    tgt = target_ray_len(frac)

    for _ in range(walk_tries):
        start = rng.choice(starts)
        path = [start]
        cur = start
        ok = True
        for step in range(length - 1):
            cands = []
            for dx, dy in DIRS:
                nc, nr = cur[0] + dx, cur[1] + dy
                if (0 <= nc < w and 0 <= nr < h
                        and (nc, nr) not in occ and (nc, nr) not in path):
                    cands.append((nc, nr))
            if not cands:
                ok = False
                break
            if step == length - 2:
                # Final step: steer the exit ray toward the target length.
                def ray_len(cell):
                    dx, dy = cell[0] - cur[0], cell[1] - cur[1]
                    return len(ray_cells(cell, (dx, dy), w, h))
                cands.sort(key=lambda cell: abs(ray_len(cell) - tgt)
                           + rng.random() * 1.5)
                cur = cands[0]
            elif frac >= 0.70:
                # Crowded board: drift toward the edge so the exit ray
                # stays short and placeable.
                cands.sort(key=lambda cell: edge_dist(cell, w, h)
                           + rng.random() * 1.0)
                cur = cands[0]
            else:
                cur = rng.choice(cands)
            path.append(cur)
        if not ok:
            continue
        direction = exit_dir(path)
        ray = ray_cells(path[-1], direction, w, h)
        if any(cell in path for cell in ray):
            continue  # ray must not run through its own path
        if any(cell in occ for cell in ray):
            continue  # ray must be clear of arrows placed before it
        return path
    return None


def gen_level_paths(rng, w, h, n_arrows, lo, hi, weights, arrow_tries=25):
    """Place `n_arrows` arrows; returns list of paths or None."""
    occ = set()
    arrows = []
    for j in range(n_arrows):
        placed = False
        for _ in range(arrow_tries):
            free_n = w * h - len(occ)
            remaining_after = n_arrows - 1 - j
            # Cell budget: leave 2 cells for every arrow still to place
            # plus SPARE cells, so the board never fills up mid-level.
            cap = free_n - 2 * remaining_after - SPARE
            if cap < 2:
                return None
            hi_c = min(hi, cap)
            ws = weights[:hi_c - lo + 1]
            length = rng.choices(range(lo, hi_c + 1), weights=ws)[0]
            path = gen_arrow(rng, w, h, occ, length)
            if path is None:
                continue
            for cell in path:
                occ.add(cell)
            arrows.append(path)
            placed = True
            break
        if not placed:
            return None
    return arrows


# ----------------------------------------------------------------- solver

def solve_level(w, h, arrows):
    """Greedy solver from the spec.

    An arrow is free if every cell from its head to the board edge in its
    exit direction is empty of other arrows' cells. Remove free arrows
    (their cells become empty) and repeat. Returns
    (solvable, blocked_at_start_ratio).
    """
    occ = {}
    for i, path in enumerate(arrows):
        for cell in path:
            occ[cell] = i

    def ray(path):
        return ray_cells(path[-1], exit_dir(path), w, h)

    blocked0 = 0
    for i, path in enumerate(arrows):
        if any(cell in occ and occ[cell] != i for cell in ray(path)):
            blocked0 += 1
    blocked_ratio = blocked0 / len(arrows) if arrows else 0.0

    remaining = set(range(len(arrows)))
    while remaining:
        progress = False
        for i in sorted(remaining):
            if all(cell not in occ or occ[cell] == i
                   for cell in ray(arrows[i])):
                for cell in arrows[i]:
                    del occ[cell]
                remaining.remove(i)
                progress = True
        if not progress:
            return False, blocked_ratio
    return True, blocked_ratio


# ------------------------------------------------------------ level build

def build_level(level_id, pack, w, h, paths, rng):
    return {
        "id": level_id,
        "pack": pack,
        "width": w,
        "height": h,
        "lives": 3,
        "arrows": [
            {
                "color": rng.randint(0, 7),
                "nodalPoints": [{"column": c, "row": r} for (c, r) in path],
            }
            for path in paths
        ],
    }


def generate_one(rng, level_id):
    """Generate one level. Returns (level_dict, blocked_ratio, tries, best_effort)."""
    pack = PACK_BY_ID[level_id]
    cfg = TIERS[pack]
    lo, hi = cfg["len_range"]
    best = None  # (blocked_ratio, level_dict) - best candidate seen
    for attempt in range(1, MAX_TRIES + 1):
        w, h, cmin, cmax = rng.choice(cfg["grids"])
        n = rng.randint(cmin, cmax)
        paths = gen_level_paths(rng, w, h, n, lo, hi, cfg["weights"])
        if paths is None:
            continue
        solvable, ratio = solve_level(w, h, paths)
        if not solvable:
            continue  # cannot happen by construction; guard anyway
        level = build_level(level_id, pack, w, h, paths, rng)
        if ratio >= cfg["min_blocked"]:
            return level, ratio, attempt, False
        if best is None or ratio > best[0]:
            best = (ratio, level)
    if best is not None:
        return best[1], best[0], MAX_TRIES, True
    # Emergency fallback (never expected to trigger): smallest grid,
    # fewest arrows, ignore the blocked ratio.
    w, h, cmin, _cmax = cfg["grids"][0]
    for _ in range(200):
        paths = gen_level_paths(rng, w, h, cmin, lo, hi, cfg["weights"])
        if paths is None:
            continue
        solvable, ratio = solve_level(w, h, paths)
        if solvable:
            return build_level(level_id, pack, w, h, paths, rng), ratio, MAX_TRIES, True
    raise RuntimeError(f"could not generate level {level_id}")


# ------------------------------------------------------------- validation

def validate_level(level):
    """Independent format + rules + solver check. Returns list of errors."""
    errors = []
    pack = PACK_BY_ID[level["id"]]
    cfg = TIERS[pack]
    w, h = level["width"], level["height"]
    if level["pack"] != pack:
        errors.append(f"pack is {level['pack']!r}, expected {pack!r}")
    if level["lives"] != 3:
        errors.append(f"lives is {level['lives']}, expected 3")
    grids = [(gw, gh) for gw, gh, _a, _b in cfg["grids"]]
    if (w, h) not in grids:
        errors.append(f"grid {w}x{h} not allowed for {pack} {grids}")
    n = len(level["arrows"])
    _cmin = min(a for _gw, _gh, a, _b in cfg["grids"])
    _cmax = max(b for _gw, _gh, _a, b in cfg["grids"])
    if not (_cmin <= n <= _cmax):
        errors.append(f"{n} arrows, expected {_cmin}-{_cmax}")
    lo, hi = cfg["len_range"]
    seen = set()
    paths = []
    for ai, arrow in enumerate(level["arrows"]):
        if not (0 <= arrow["color"] <= 7):
            errors.append(f"arrow {ai}: color {arrow['color']} out of 0-7")
        pts = [(p["column"], p["row"]) for p in arrow["nodalPoints"]]
        if not (lo <= len(pts) <= hi):
            errors.append(f"arrow {ai}: path length {len(pts)}, "
                          f"expected {lo}-{hi}")
        if len(set(pts)) != len(pts):
            errors.append(f"arrow {ai}: path revisits a cell")
        for k in range(1, len(pts)):
            dc = abs(pts[k][0] - pts[k - 1][0])
            dr = abs(pts[k][1] - pts[k - 1][1])
            if dc + dr != 1:
                errors.append(f"arrow {ai}: step {k} is not 1 orthogonal cell")
        for (c, r) in pts:
            if not (0 <= c < w and 0 <= r < h):
                errors.append(f"arrow {ai}: cell {(c, r)} outside board")
            if (c, r) in seen:
                errors.append(f"arrow {ai}: cell {(c, r)} shared with another arrow")
            seen.add((c, r))
        paths.append(pts)
    solvable, ratio = solve_level(w, h, paths)
    if not solvable:
        errors.append("greedy solver: UNSOLVABLE")
    if ratio < cfg["min_blocked"]:
        errors.append(f"blocked-at-start {ratio:.2f} below minimum "
                      f"{cfg['min_blocked']:.2f}")
    return errors, solvable, ratio


# ------------------------------------------------------------------- main

def main(argv=None):
    global MAX_TRIES
    ap = argparse.ArgumentParser(
        description="Generate solver-verified Teer Puzzle levels.")
    ap.add_argument("--seed", type=int, default=SEED_DEFAULT,
                    help=f"RNG seed (default {SEED_DEFAULT})")
    ap.add_argument("--count", type=int, default=2000,
                    help="How many levels to generate (default 2000)")
    ap.add_argument("--regenerate", action="store_true",
                    help="Rewrite all level files (default behaviour).")
    ap.add_argument("--max-tries", type=int, default=MAX_TRIES,
                    help=f"Attempts per level (default {MAX_TRIES}).")
    args = ap.parse_args(argv)

    MAX_TRIES = args.max_tries
    count = args.count
    build_tiers(count)

    root = Path(__file__).resolve().parents[2]
    out_dir = root / "Assets" / "Levels"
    res_dir = root / "Assets" / "Resources" / "Levels"
    out_dir.mkdir(parents=True, exist_ok=True)
    res_dir.mkdir(parents=True, exist_ok=True)

    rng = random.Random(args.seed)
    print(f"Teer Puzzle level generator  |  seed={args.seed}  "
          f"max_tries={MAX_TRIES}  count={count}")
    print("Packs: " + ", ".join(
        f"{p} {min(c['ids'])}-{max(c['ids'])} ({len(c['ids'])})"
        for p, c in TIERS.items()))
    print(f"Output: {out_dir}")
    print(f"Mirror: {res_dir}\n")

    report = []  # (id, pack, w, h, arrows, blocked, tries, best_effort, errors)
    for level_id in range(1, count + 1):
        level, ratio, tries, best_effort = generate_one(rng, level_id)
        text = json.dumps(level, indent=2) + "\n"
        name = f"level_{level_id:03d}.json"
        (out_dir / name).write_text(text, encoding="utf-8")
        (res_dir / name).write_text(text, encoding="utf-8")
        errors, solvable, ratio2 = validate_level(level)
        assert abs(ratio2 - ratio) < 1e-9, "solver mismatch on re-check"
        report.append((level_id, level["pack"], level["width"],
                       level["height"], len(level["arrows"]), ratio,
                       tries, best_effort, errors))
        flag = "  <-- BEST-EFFORT (ratio below minimum)" if best_effort else ""
        print(f"  level {level_id:03d} [{level['pack']:6s}] "
              f"{level['width']}x{level['height']} "
              f"arrows={len(level['arrows']):2d} "
              f"blocked={ratio:.2f} tries={tries:3d} "
              f"solver={'PASS' if solvable else 'FAIL'}{flag}")
        if errors:
            for e in errors:
                print(f"      ERROR: {e}")

    print("\n================ VALIDATION REPORT ================")
    total_fail = 0
    for pack, cfg in TIERS.items():
        rows = [r for r in report if r[1] == pack]
        n_fail = sum(1 for r in rows if r[8])
        n_solver_fail = sum(1 for r in rows if "UNSOLVABLE" in str(r[8]))
        n_best = sum(1 for r in rows if r[7])
        ratios = [r[5] for r in rows]
        total_fail += n_fail
        print(f"{pack:6s}: {len(rows):2d} levels | solver PASS "
              f"{len(rows) - n_solver_fail}/{len(rows)} | "
              f"blocked-at-start min {min(ratios):.2f} "
              f"mean {sum(ratios) / len(ratios):.2f} "
              f"max {max(ratios):.2f} | "
              f"best-effort: {n_best} | errors: {n_fail}")
    # Mirror check: both directories byte-identical.
    mismatch = []
    for level_id in range(1, count + 1):
        name = f"level_{level_id:03d}.json"
        a = (out_dir / name).read_bytes()
        b = (res_dir / name).read_bytes()
        if hashlib.sha256(a).hexdigest() != hashlib.sha256(b).hexdigest():
            mismatch.append(name)
    print(f"mirror check (Assets/Levels vs Assets/Resources/Levels): "
          f"{'IDENTICAL (' + str(count) + '/' + str(count) + ')' if not mismatch else 'MISMATCH: ' + str(mismatch)}")
    print(f"TOTAL: {count} levels, solver PASS "
          f"{sum(1 for r in report if not r[8])}/{count}, "
          f"errors: {total_fail}, best-effort: "
          f"{sum(1 for r in report if r[7])}")
    if total_fail:
        print("RESULT: FAILED - see errors above")
        return 1
    print(f"RESULT: OK - all {count} levels solver-verified")
    return 0


if __name__ == "__main__":
    sys.exit(main())
