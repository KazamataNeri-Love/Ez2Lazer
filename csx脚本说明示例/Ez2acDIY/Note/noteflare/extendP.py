from pathlib import Path
from PIL import Image
import shutil

# 放大倍率
SCALE = 1.5

# 当前脚本所在目录
folder = Path(__file__).parent

# 备份目录
backup_dir = folder / "backup"
backup_dir.mkdir(exist_ok=True)

# 查找所有 png
png_files = list(folder.glob("*.png"))

if not png_files:
    print("当前目录没有找到 PNG 文件。")
    exit()

for file in png_files:
    try:
        # 备份原图
        shutil.copy2(file, backup_dir / file.name)

        # 打开图片
        with Image.open(file) as img:
            width, height = img.size

            # 新尺寸（四舍五入）
            new_size = (
                round(width * SCALE),
                round(height * SCALE)
            )

            # 缩放
            resized = img.resize(new_size, Image.Resampling.LANCZOS)

            # 覆盖保存
            resized.save(file)

        print(f"✓ {file.name}: {width}x{height} -> {new_size[0]}x{new_size[1]}")

    except Exception as e:
        print(f"✗ {file.name} 处理失败：{e}")

print("\n全部完成。原图已备份到 backup 文件夹。")