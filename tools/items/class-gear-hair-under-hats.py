# Which hair styles come through which hat: the measurement behind HairThroughHat in SyntyPlayerAppearance.cs.
#
# A hat (the hats array, Chr_HeadCoverings_Base_Hair) hides nothing, so the player's hair stays on under it, and many
# styles poke through the crown or the brim. For every hat an item uses, run this and put the styles it lists as
# "comes through" in the table; the game then shows a flat style under that hat instead.
#
#   python tools/items/class-gear-hair-under-hats.py jobs 1 4      (the hats to measure, by index)
#   unity command eval_file --file tools/items/class-gear-preview.cs      (then wait for Temp/class-gear-preview.job.done)
#   python tools/items/class-gear-hair-under-hats.py analyze 1 4
#
# How: the preview tool renders every hair style under the hat from 8 sides, level and from 40 degrees above, on a
# bare body with keyed colors: hair magenta, skin and eyes green, background blue; what is left is the hat. Hair seen
# above the hat's lower edge in its column is on or through the hat; hair below the brim or beside the hat is how
# hair is worn. A style comes through when that hair covers 1% of the hat or more in the worst view (a few pixels
# along the brim's edge are how the two meet, not a hole).
#
# The renders go through the game's own code, so a hat that is in the table already is measured with its flat hair on:
# it must list nothing (that is the check that the table is complete; 2026-11-21: hats 1 and 4 list nothing). To
# measure such a hat from scratch, take its row out of the table first.
#
# Needs Pillow and numpy. The pictures are kept in Temp/hair-under-hats for a look.
import os, sys

PROJECT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
OUT = os.path.join(PROJECT, "Temp", "hair-under-hats")
JOB = os.path.join(PROJECT, "Temp", "class-gear-preview.job")
HAIRS = range(0, 38)  # hairs[38..44] are empty slots
YAWS = [0, 45, 90, 135, 180, 225, 270, 315]
PITCHES = [0, 40]
W, H = 200, 220
COMES_THROUGH = 0.01

def jobs(hats):
    os.makedirs(OUT, exist_ok=True)
    renders = []
    for hat in hats:
        for hair in HAIRS:
            for pitch in PITCHES:
                lines = ["out=" + os.path.join(OUT, "hat%d_hair%02d_%d.png" % (hat, hair, pitch)), "w=%d" % W, "h=%d" % H, "ss=1",
                         "viewH=0.72", "cy=1.68", "pitch=%d" % pitch, "bg=#0000FF", "hair=%d" % hair, "brows=-1", "beard=-1",
                         "hairColor=#FF00FF", "skinColor=#00FF00", "eyeColor=#00FF00", "stubbleColor=#00FF00", "scarColor=#00FF00",
                         "warPaintColor=#00FF00", "pose=tpose", "key=1.0", "fill=0.8", "ambient=#909090"]
                # the hat in a light material, so no part of it is taken for hair or skin
                for i, yaw in enumerate(YAWS):
                    lines.append("v%d=yaw %d | yaw=%d | do=wear custom Hat Lightbringer %d" % (i, yaw, yaw, hat))
                renders.append("\n".join(lines))
    with open(JOB, "w") as f:
        f.write("\n---\n".join(renders) + "\n")
    print("%d renders in %s; now run tools/items/class-gear-preview.cs" % (len(renders), JOB))

def analyze(hats):
    from PIL import Image
    import numpy as np
    for hat in hats:
        worst = {}
        for hair in HAIRS:
            worst[hair] = 0.0
            for pitch in PITCHES:
                im = np.asarray(Image.open(os.path.join(OUT, "hat%d_hair%02d_%d.png" % (hat, hair, pitch))).convert("RGB")).astype(int)
                r, g, b = im[..., 0], im[..., 1], im[..., 2]
                background = (b > 200) & (r < 40) & (g < 40)
                hair_px = (r > 1.4 * g + 20) & (b > 1.4 * g + 20)
                skin = (g > 1.4 * r + 20) & (g > 1.4 * b + 20)
                hat_px = ~background & ~hair_px & ~skin
                rows = np.arange(H)[:, None]
                for i in range(len(YAWS)):
                    cell_hat = hat_px[:, i * W:(i + 1) * W]
                    cell_hair = hair_px[:, i * W:(i + 1) * W]
                    area = cell_hat.sum()
                    if area == 0:
                        continue
                    # the hat's lowest row in each column (-1 where the column has no hat)
                    bottom = np.where(cell_hat.any(axis=0), H - 1 - np.argmax(cell_hat[::-1, :], axis=0), -1)[None, :]
                    through = (cell_hair & (rows < bottom)).sum()
                    worst[hair] = max(worst[hair], through / area)
        print("hats[%d]" % hat)
        for hair in HAIRS:
            print("  hair %2d  %5.1f%% of the hat in the worst view%s" % (hair, worst[hair] * 100, "  comes through" if worst[hair] >= COMES_THROUGH else ""))
        print("  comes through: { " + ", ".join(str(h) for h in HAIRS if worst[h] >= COMES_THROUGH) + " }")

if __name__ == "__main__":
    if len(sys.argv) < 3 or sys.argv[1] not in ("jobs", "analyze"):
        sys.exit("usage: class-gear-hair-under-hats.py jobs|analyze <hat index> [<hat index> ...]")
    (jobs if sys.argv[1] == "jobs" else analyze)([int(a) for a in sys.argv[2:]])
