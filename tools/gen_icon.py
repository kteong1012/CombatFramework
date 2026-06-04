"""
临时搓图工具 — 纯 Python + Pillow，不依赖任何引擎。
用法: python tools/gen_icon.py slash|burst|charge|all  [--size 128]  [--out path]
"""
import math, sys, os
from PIL import Image, ImageDraw

OUT_DIR = os.path.join(os.path.dirname(__file__), "..", "GodotDemo", "Textures")

def draw_slash(draw, cx, cy, s):
    """金色剑刃斜斩"""
    hw, hh = s * 0.30, s * 0.14
    angle = math.radians(-35)
    cos, sin = math.cos(angle), math.sin(angle)
    cx2, cy2 = cx + 20, cy - 20

    # 刀身菱形
    pts = [
        (cx2 - hw * cos, cy2 - hw * sin),
        (cx2 - hh * sin, int(cx2 + hh * cos - (cy2 - s * 0.12))),
        (cx2 + hw * cos, cy2 + hw * sin),
        (cx2 + hh * sin, int(cx2 - hh * cos - (cy2 - s * 0.12))),
    ]
    draw.polygon(pts, fill=(220, 160, 30), outline=(255, 255, 255))

    # 护手
    gw = s * 0.28
    gx, gy = cx - gw, int(cy + s * 0.08)
    draw.line([(gx, gy), (gx + gw * 2, gy)], fill=(200, 200, 210), width=4)

    # 挥砍弧线 (画 3 条)
    for i, r_off in enumerate([0, 6, 12]):
        r = s * 0.30 + r_off
        alpha = 180 - i * 40
        arc_pts = []
        for a in range(-110, -40, 5):
            ar = math.radians(a)
            arc_pts.append((cx2 + r * math.cos(ar), cy2 + r * math.sin(ar)))
        draw.line(arc_pts, fill=(255, 210, 60, alpha), width=2)


def draw_burst(draw, cx, cy, s):
    """青色环形冲击波"""
    r1, r2 = s * 0.30, s * 0.38

    # 外环
    draw.ellipse([cx - r2, cy - r2, cx + r2, cy + r2],
                 outline=(30, 180, 200), width=3)

    # 内环填充
    draw.ellipse([cx - r1, cy - r1, cx + r1, cy + r1],
                 fill=(30, 200, 220, 70))

    # 八方向放射线
    for i in range(8):
        a = i * math.tau / 8
        inner = r1 * 0.45
        x1 = cx + math.cos(a) * inner
        y1 = cy + math.sin(a) * inner
        x2 = cx + math.cos(a) * r2
        y2 = cy + math.sin(a) * r2
        draw.line([(x1, y1), (x2, y2)], fill=(40, 200, 220, 150), width=2)

    # 中心点
    draw.ellipse([cx - 5, cy - 5, cx + 5, cy + 5], fill=(255, 255, 255))


def draw_charge(draw, cx, cy, s):
    """紫色能量汇聚"""
    orb_r = s * 0.14

    # 中心能量球
    draw.ellipse([cx - orb_r, cy - orb_r, cx + orb_r, cy + orb_r],
                 fill=(150, 70, 210))
    draw.ellipse([cx - orb_r * 0.5, cy - orb_r * 0.5, cx + orb_r * 0.5, cy + orb_r * 0.5],
                 fill=(255, 255, 255))

    # 四颗粒子 + 连接线
    pd = s * 0.28
    pr = s * 0.04
    for i in range(4):
        a = i * math.tau / 4 - math.pi / 4
        px, py = cx + math.cos(a) * pd, cy + math.sin(a) * pd

        # 连接线
        draw.line([(cx, cy), (px, py)], fill=(140, 60, 200, 60), width=1)

        # 菱形粒子
        ppts = [(px, py - pr), (px + pr, py), (px, py + pr), (px - pr, py)]
        draw.polygon(ppts, fill=(180, 100, 240, 80), outline=(150, 70, 210))


def generate(name, size=128):
    img = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)
    cx, cy = size / 2, size / 2
    s = size

    drawers = {
        "slash":  draw_slash,
        "burst":  draw_burst,
        "charge": draw_charge,
    }

    if name not in drawers:
        print(f"未知图标: {name}，可选: {list(drawers.keys())}")
        sys.exit(1)

    drawers[name](draw, cx, cy, s)

    os.makedirs(OUT_DIR, exist_ok=True)
    path = os.path.join(OUT_DIR, f"icon_{name}.png")
    img.save(path)
    print(f"已生成: {path}  ({size}x{size})")


if __name__ == "__main__":
    if len(sys.argv) < 2:
        print("用法: python tools/gen_icon.py <slash|burst|charge|all> [--size 128]")
        sys.exit(1)

    arg = sys.argv[1]
    size = 128
    if "--size" in sys.argv:
        size = int(sys.argv[sys.argv.index("--size") + 1])

    if arg == "all":
        for name in ["slash", "burst", "charge"]:
            generate(name, size)
    else:
        generate(arg, size)
