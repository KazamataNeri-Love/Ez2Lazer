from PIL import Image
import numpy as np
import os

src_dir = os.path.dirname(os.path.abspath(__file__))
W, H = 100, 1000

# 坐标原点在底部中心
cx = W // 2  # 50

# 1) 基础渐变：底部 Alpha=204(0.8) → 顶部 Alpha=0
# 反比例函数: alpha = k/(y_rev + d) - 0.1, 其中 y_rev=0 为底部
# k=112.3875, d=124.875 满足底部 alpha=0.8, 顶部 alpha=0
# 1) 基础渐变：底部 Alpha=204(0.8) → 顶部 Alpha=0
# 平方根曲线: alpha = 0.8 * (1 - sqrt(y_rev/H)), 大部分区域比反比例更亮
base = np.zeros((H, W, 4), dtype=np.uint8)
base[:, :, 0] = 0xFF  # R
base[:, :, 1] = 0xFF  # G
base[:, :, 2] = 0xFF  # B

for y in range(H):
    y_rev = H - 1 - y  # 0=底部, 999=顶部
    alpha_norm = 0.8 * (1 - (y_rev / (H - 1)) ** 0.5)
    alpha = int(255 * alpha_norm)
    base[y, :, 3] = np.clip(alpha, 0, 255)

# 2) 三角形叠加区域
# 底边: [-25,0] ~ [25,0]  (像素坐标: x=25~75, y=999)
# 高度: 450px, 顶部在 y=550 (从底部往上450)
tri_height = 800
tri_top_y = H - tri_height  # 550

extra = np.zeros((H, W), dtype=np.float32)

for py in range(tri_top_y, H):
    y_local = H - 1 - py  # 从底部向上的距离, 0~449
    # 三角形在当前高度 y_local 的半宽度
    t = y_local / tri_height  # 0=底部, 1=顶部
    half_w = 50 * (1 - t)  # 底部45px, 顶部0px
    
    for px in range(W):
        dx = abs(px - cx)
        if dx <= half_w:
            # 在三角形内 → 计算额外Alpha
            # 底部中心(0,0)处extra=20, 向边缘和顶部衰减到0
            # 水平衰减: 1 - dx/half_w
            # 垂直衰减: 1 - t
            h_factor = 1 - dx / half_w if half_w > 0 else 0
            v_factor = 1 - t
            extra_val = 20 * h_factor * v_factor
            extra[py, px] = extra_val

# 叠加到基础Alpha上
base_alpha = base[:, :, 3].astype(np.float32)
new_alpha = np.clip(base_alpha + extra, 0, 255).astype(np.uint8)
base[:, :, 3] = new_alpha

img = Image.fromarray(base, 'RGBA')
out = os.path.join(src_dir, "spotlight.png")
img.save(out)
print(f"已生成: {out} (100x1000)")
