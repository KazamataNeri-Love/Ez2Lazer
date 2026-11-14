// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Shaders;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Rulesets.Osu.Mods
{
    /// <summary>
    /// Polytone 的滑条身：沿路径按时间采样，逐点收窄的硬边带。
    /// 近端（球所在处）截面比例为 1，远端收窄到出生比例。
    /// </summary>
    /// <remarks>
    /// 相邻采样点共用斜接后的顶点，因此不会在弯道处裂开；整条带一次提交。
    /// </remarks>
    public partial class PolytoneSliderBand : Drawable
    {
        public const int SAMPLE_COUNT = 33;

        private static readonly Color4 band_colour = new Color4(236, 241, 250, 255);

        private readonly Vector2[] centres = new Vector2[SAMPLE_COUNT];
        private readonly float[] halfWidths = new float[SAMPLE_COUNT];

        private IShader shader = null!;

        [BackgroundDependencyLoader]
        private void load(ShaderManager shaders)
        {
            shader = shaders.Load(VertexShaderDescriptor.TEXTURE_2, FragmentShaderDescriptor.TEXTURE);
        }

        internal void UpdateBand(ReadOnlySpan<Vector2> newCentres, ReadOnlySpan<float> newHalfWidths)
        {
            newCentres.CopyTo(centres);
            newHalfWidths.CopyTo(halfWidths);

            Invalidate(Invalidation.DrawNode);
        }

        protected override DrawNode CreateDrawNode() => new BandDrawNode(this);

        private class BandDrawNode : DrawNode
        {
            private const int segment_count = SAMPLE_COUNT - 1;

            protected new PolytoneSliderBand Source => (PolytoneSliderBand)base.Source;

            private readonly Vector2[] leftEdge = new Vector2[SAMPLE_COUNT];
            private readonly Vector2[] rightEdge = new Vector2[SAMPLE_COUNT];

            private IShader shader = null!;
            private float maxHalfWidth = 1;

            public BandDrawNode(PolytoneSliderBand source)
                : base(source)
            {
            }

            public override void ApplyState()
            {
                base.ApplyState();

                shader = Source.shader;

                Vector2[] centres = Source.centres;
                float[] halfWidths = Source.halfWidths;

                maxHalfWidth = 0.001f;

                for (int i = 0; i < SAMPLE_COUNT; i++)
                {
                    maxHalfWidth = MathF.Max(maxHalfWidth, halfWidths[i]);

                    Vector2 prevDirection = i > 0 ? centres[i] - centres[i - 1] : centres[1] - centres[0];
                    Vector2 nextDirection = i < SAMPLE_COUNT - 1 ? centres[i + 1] - centres[i] : prevDirection;

                    Vector2 tangent = prevDirection.LengthSquared > 0.000001f ? prevDirection.Normalized() : Vector2.UnitX;

                    // 前后两段方向的平均给出斜接法线，使相邻段共享顶点、边缘不收口。
                    if (nextDirection.LengthSquared > 0.000001f)
                    {
                        Vector2 combined = tangent + nextDirection.Normalized();

                        // 折返处两段方向相反，平均值退化；退回入射方向，避免生成翻面的四边形。
                        if (combined.LengthSquared > 0.0625f)
                            tangent = combined;
                    }

                    Vector2 normal = new Vector2(-tangent.Y, tangent.X).Normalized() * halfWidths[i];

                    leftEdge[i] = centres[i] + normal;
                    rightEdge[i] = centres[i] - normal;
                }
            }

            protected override void Draw(IRenderer renderer)
            {
                base.Draw(renderer);

                renderer.PushLocalMatrix(DrawInfo.Matrix);
                shader.Bind();

                for (int i = 0; i < segment_count; i++)
                {
                    float halfWidth = (leftEdge[i] - rightEdge[i]).Length * 0.5f;
                    float alpha = 0.20f + 0.32f * (halfWidth / maxHalfWidth);

                    var quad = new Quad(leftEdge[i], rightEdge[i], leftEdge[i + 1], rightEdge[i + 1]);
                    var colour = new Color4(band_colour.R, band_colour.G, band_colour.B, alpha);

                    renderer.DrawQuad(renderer.WhitePixel, quad, ColourInfo.SingleColour(colour));
                }

                shader.Unbind();
                renderer.PopLocalMatrix();
            }
        }
    }
}
