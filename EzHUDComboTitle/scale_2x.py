from PIL import Image
import os, glob

src_dir = os.path.dirname(os.path.abspath(__file__))

for path in glob.glob(os.path.join(src_dir, "*.png")):
    img = Image.open(path).convert("RGBA")
    w, h = img.size
    new_img = img.resize((w * 2, h * 2), Image.NEAREST)
    new_img.save(path)
    print(f"{os.path.basename(path)}: {w}x{h} → {w*2}x{h*2}")

print("完成！")
