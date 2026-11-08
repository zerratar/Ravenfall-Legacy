# Crops the battle pet renders (tools/website/pet-renders.cs, run with unity command eval_file in edit mode) to the
# model, squares them with a little margin and writes 256 px webp files for the website's imgs/pets.
#   python tools/website/pet-renders.py [renders folder] [RavenNest repo]
import glob, os, sys, tempfile
from PIL import Image
src = sys.argv[1] if len(sys.argv) > 1 else os.path.join(tempfile.gettempdir(), "ravenfall-pet-renders")
repo = sys.argv[2] if len(sys.argv) > 2 else os.path.join(os.path.dirname(__file__), "..", "..", "..", "RavenNest")
out = os.path.join(repo, "src", "RavenNest.Blazor", "wwwroot", "imgs", "pets")
os.makedirs(out, exist_ok=True)
for f in sorted(glob.glob(os.path.join(src, "*.png"))):
    im = Image.open(f).convert("RGBA")
    box = im.getchannel("A").point(lambda a: 255 if a > 20 else 0).getbbox()
    if not box: continue
    im = im.crop(box); w, h = im.size; side = int(max(w, h) * 1.12)
    square = Image.new("RGBA", (side, side), (0, 0, 0, 0))
    square.alpha_composite(im, ((side - w) // 2, (side - h) // 2))
    square.resize((256, 256), Image.LANCZOS).save(os.path.join(out, os.path.basename(f)[:-4] + ".webp"), quality=90)
    print(os.path.basename(f))
