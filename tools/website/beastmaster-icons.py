"""
Stand-in pictures for the Beastmaster tree on the website's skill tree map (docs/combat-skills-design.md,
"Beastmaster"): the tree's emblem and its actives' and specials' icons, made from the game's own pet pictures
(the website's imgs/pets, rendered from the models) until painted ones are drawn with the other skill icons
(tools/website/skill-icon-sheets.py). Nothing is generated: each is a pet picture fitted into the 160 px square
the map's icons use.

    python tools/website/beastmaster-icons.py [RavenNest repo]
"""
import os
import sys

from PIL import Image

# icon file (imgs/skills/<name>.webp) -> the pet picture it borrows (imgs/pets/<name>.webp)
ICONS = {
    "tree-beastmaster": "wolf-pup",      # the pet everyone meets first
    "sic-em": "tiger",                   # goes for the target
    "mend-pet": "dog",                   # licks every wound better
    "call-of-the-wild": "wolf-pup-1",    # the wolf
    "trick-training": "parrot",          # learns one more trick
    "shared-mending": "sheep-1",         # the healer in its wool
}


def main():
    here = os.path.dirname(os.path.abspath(__file__))
    repo = sys.argv[1] if len(sys.argv) > 1 else os.path.join(here, "..", "..", "..", "RavenNest")
    imgs = os.path.join(repo, "src", "RavenNest.Blazor", "wwwroot", "imgs")
    for icon, pet in ICONS.items():
        src = Image.open(os.path.join(imgs, "pets", pet + ".webp")).convert("RGBA")
        box = src.getchannel("A").point(lambda a: 255 if a > 20 else 0).getbbox()
        if box:
            src = src.crop(box)
        side = 160
        fit = int(side * 0.86)
        scale = min(fit / src.size[0], fit / src.size[1])
        src = src.resize((max(1, int(src.size[0] * scale)), max(1, int(src.size[1] * scale))), Image.LANCZOS)
        out = Image.new("RGBA", (side, side), (0, 0, 0, 0))
        out.alpha_composite(src, ((side - src.size[0]) // 2, (side - src.size[1]) // 2))
        path = os.path.normpath(os.path.join(imgs, "skills", icon + ".webp"))
        out.save(path, quality=90)
        print(path)


if __name__ == "__main__":
    main()
