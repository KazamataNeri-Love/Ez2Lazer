from PIL import Image
import os, glob

# 给当前目录下所有 PNG 顶部加 10px 透明像素（内容不下移）
src_dir = os.path.dirname(os.path.abspath(__file__))

for path in glob.glob(os.path.join(src_dir, "*.png")):
    img = Image.open(path).convert("RGBA")
    w, h = img.size
    new_img = Image.new("RGBA", (w, h + 10), (0, 0, 0, 0))
    new_img.paste(img, (0, 10))  # 内容放在底部 10px 处
    new_img.save(path)
    print(f"{os.path.basename(path)}: {w}x{h} → {w}x{h+10} (+10px top)")

print("完成！")
