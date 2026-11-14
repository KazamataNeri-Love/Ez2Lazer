// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Game.Rulesets.Osu.UI;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Rulesets.Osu.Mods
{
    /// <summary>
    /// Polytone 的区域边界：近端矩形（击打层）、远端矩形（出生层）与四条连接棱。
    /// 只画硬边，不做雾化与线宽补偿。
    /// </summary>
    public partial class PolytoneTunnel : CompositeDrawable
    {
        private const float line_width = 2f;
        private const int lines_per_edge = 4;

        private static readonly Color4 near_colour = new Color4(226, 238, 255, 235);
        private static readonly Color4 far_colour = new Color4(158, 196, 240, 165);
        private static readonly Color4 side_colour = new Color4(120, 162, 214, 95);

        private static readonly Vector2[] rect_corners =
        {
            Vector2.Zero,
            new Vector2(OsuPlayfield.BASE_SIZE.X, 0),
            OsuPlayfield.BASE_SIZE,
            new Vector2(0, OsuPlayfield.BASE_SIZE.Y),
        };

        private readonly Box[] lines = new Box[lines_per_edge * 3];

        public PolytoneTunnel()
        {
            Size = OsuPlayfield.BASE_SIZE;

            for (int i = 0; i < lines.Length; i++)
                AddInternal(lines[i] = new Box { Origin = Anchor.TopLeft });
        }

        internal void UpdateGeometry(in PolytoneGeometry geometry, float opacity)
        {
            Alpha = Math.Clamp(opacity, 0, 1);

            if (Alpha <= 0)
                return;

            for (int i = 0; i < lines_per_edge; i++)
            {
                int next = (i + 1) % lines_per_edge;

                setLine(lines[i], rect_corners[i], rect_corners[next], near_colour);
                setLine(
                    lines[lines_per_edge + i],
                    geometry.Project(rect_corners[i], geometry.SpawnRatio),
                    geometry.Project(rect_corners[next], geometry.SpawnRatio),
                    far_colour);
                setLine(lines[lines_per_edge * 2 + i], rect_corners[i], geometry.Project(rect_corners[i], geometry.SpawnRatio), side_colour);
            }
        }

        private static void setLine(Box line, Vector2 from, Vector2 to, Color4 colour)
        {
            Vector2 diff = to - from;

            line.Position = from;
            line.Size = new Vector2(MathF.Max(diff.Length, 0.001f), line_width);
            line.Rotation = MathF.Atan2(diff.Y, diff.X) * (180 / MathF.PI);
            line.Colour = colour;
        }
    }
}
