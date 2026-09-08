// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Rendering.Vertices;
using osu.Framework.Graphics.Shaders;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Textures;
using osu.Game.Beatmaps;
using osu.Game.Configuration;
using osu.Game.EzOsuGame.Configuration;
using osu.Game.EzOsuGame.Localization;
using osu.Game.Overlays.Settings;
using osu.Game.Skinning;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.EzOsuGame.HUD
{
    /// <summary>
    /// A skinnable flat-bottom bar chart which reacts to the audio frequency spectrum in real time.
    /// Bars grow upward from a shared baseline.
    /// </summary>
    public partial class EzAudioVisualiser : CompositeDrawable, ISerialisableDrawable
    {
        /// <summary>
        /// The number of frequency bins exposed by the beat sync provider's amplitude data.
        /// </summary>
        private const int spectrum_size = 256;

        /// <summary>
        /// Maximum number of display points, including the zero-faded virtual segments appended at both edges.
        /// </summary>
        private const int max_display_points = 512;

        /// <summary>
        /// How much each bar falls each millisecond (as a fraction of a full-height bar).
        /// </summary>
        private const float decay_per_millisecond = 0.0024f;

        /// <summary>
        /// Fraction of <see cref="BarWidth"/> used as the gap between adjacent bars.
        /// </summary>
        private const float spacing_ratio = 0.2f;

        /// <summary>
        /// Exponent applied to amplitudes (values &lt; 1 are lifted), improving sensitivity at low volumes.
        /// </summary>
        private const float amplitude_curve = 0.65f;

        /// <summary>
        /// Additional gain applied during kiai sections. Normal sections use full amplitude (no penalty).
        /// </summary>
        private const float kiai_boost = 1.25f;

        [SettingSource(typeof(EzHUDStrings), nameof(EzHUDStrings.AUDIO_VISUALISER_STYLE_LABEL), nameof(EzHUDStrings.AUDIO_VISUALISER_STYLE_DESCRIPTION), 0)]
        public Bindable<EzAudioVisualiserStyle> StyleMode { get; } = new Bindable<EzAudioVisualiserStyle>(EzAudioVisualiserStyle.Solid);

        [SettingSource(typeof(EzHUDStrings), nameof(EzHUDStrings.AUDIO_VISUALISER_SOLID_HEADER), null, 4, SettingControlType = typeof(EzSettingsSectionDivider))]
        public Bindable<bool> SolidHeader { get; } = new Bindable<bool>(true);

        [SettingSource(typeof(EzHUDStrings), nameof(EzHUDStrings.AUDIO_VISUALISER_BAR_WIDTH_LABEL), nameof(EzHUDStrings.AUDIO_VISUALISER_BAR_WIDTH_DESCRIPTION), 5)]
        public BindableNumber<float> BarWidth { get; } = new BindableNumber<float>(8)
        {
            MinValue = 2,
            MaxValue = 64,
            Precision = 1,
        };

        [SettingSource(typeof(EzHUDStrings), nameof(EzHUDStrings.AUDIO_VISUALISER_BAR_COUNT_LABEL), nameof(EzHUDStrings.AUDIO_VISUALISER_BAR_COUNT_DESCRIPTION), 6)]
        public BindableNumber<int> BarCount { get; } = new BindableNumber<int>(64)
        {
            MinValue = 4,
            MaxValue = spectrum_size,
            Precision = 1,
        };

        [SettingSource(typeof(EzHUDStrings), nameof(EzHUDStrings.AUDIO_VISUALISER_CURVE_HEADER), null, 7, SettingControlType = typeof(EzSettingsSectionDivider))]
        public Bindable<bool> CurveHeader { get; } = new Bindable<bool>(true);

        [SettingSource(typeof(EzHUDStrings), nameof(EzHUDStrings.AUDIO_VISUALISER_TOTAL_WIDTH_LABEL), nameof(EzHUDStrings.AUDIO_VISUALISER_TOTAL_WIDTH_DESCRIPTION), 8)]
        public BindableNumber<float> TotalWidth { get; } = new BindableNumber<float>(512)
        {
            MinValue = 100,
            MaxValue = 2000,
            Precision = 1,
        };

        [SettingSource(typeof(EzHUDStrings), nameof(EzHUDStrings.AUDIO_VISUALISER_POINT_COUNT_LABEL), nameof(EzHUDStrings.AUDIO_VISUALISER_POINT_COUNT_DESCRIPTION), 9)]
        public BindableNumber<int> PointCount { get; } = new BindableNumber<int>(48)
        {
            MinValue = 8,
            MaxValue = spectrum_size,
            Precision = 1,
        };

        [SettingSource(typeof(EzHUDStrings), nameof(EzHUDStrings.AUDIO_VISUALISER_LINE_THICKNESS_LABEL), nameof(EzHUDStrings.AUDIO_VISUALISER_LINE_THICKNESS_DESCRIPTION), 10)]
        public BindableNumber<float> LineThickness { get; } = new BindableNumber<float>(3)
        {
            MinValue = 1,
            MaxValue = 20,
            Precision = 0.5f,
        };

        [SettingSource(typeof(EzHUDStrings), nameof(EzHUDStrings.AUDIO_VISUALISER_TOTAL_HEIGHT_LABEL), nameof(EzHUDStrings.AUDIO_VISUALISER_TOTAL_HEIGHT_DESCRIPTION), 1)]
        public BindableNumber<float> TotalHeight { get; } = new BindableNumber<float>(240)
        {
            MinValue = 20,
            MaxValue = 1000,
            Precision = 1,
        };

        [SettingSource(typeof(EzHUDStrings), nameof(EzHUDStrings.AUDIO_VISUALISER_EDGE_FADE_LABEL), nameof(EzHUDStrings.AUDIO_VISUALISER_EDGE_FADE_DESCRIPTION), 2)]
        public BindableNumber<float> EdgeFade { get; } = new BindableNumber<float>(0.05f)
        {
            MinValue = 0,
            MaxValue = 0.25f,
            Precision = 0.01f,
        };

        [SettingSource(typeof(EzHUDStrings), nameof(EzHUDStrings.AUDIO_VISUALISER_SENSITIVITY_LABEL), nameof(EzHUDStrings.AUDIO_VISUALISER_SENSITIVITY_DESCRIPTION), 3)]
        public BindableNumber<float> Sensitivity { get; } = new BindableNumber<float>(1.2f)
        {
            MinValue = 0.1f,
            MaxValue = 5f,
            Precision = 0.05f,
        };

        [SettingSource(typeof(EzHUDStrings), nameof(EzHUDStrings.AUDIO_VISUALISER_LAYOUT_LABEL), nameof(EzHUDStrings.AUDIO_VISUALISER_LAYOUT_DESCRIPTION))]
        public Bindable<EzAudioVisualiserLayout> LayoutMode { get; } = new Bindable<EzAudioVisualiserLayout>(EzAudioVisualiserLayout.Left);

        [SettingSource(typeof(EzHUDStrings), nameof(EzHUDStrings.AUDIO_VISUALISER_ALPHA_LABEL), nameof(EzHUDStrings.AUDIO_VISUALISER_ALPHA_DESCRIPTION))]
        public BindableNumber<float> BarAlpha { get; } = new BindableNumber<float>(1)
        {
            MinValue = 0,
            MaxValue = 1,
            Precision = 0.01f,
        };

        [SettingSource(typeof(EzHUDStrings), nameof(EzHUDStrings.AUDIO_VISUALISER_COLOUR_TOP_LABEL), nameof(EzHUDStrings.AUDIO_VISUALISER_COLOUR_TOP_DESCRIPTION))]
        public BindableColour4 ColourTop { get; } = new BindableColour4(Color4.White);

        [SettingSource(typeof(EzHUDStrings), nameof(EzHUDStrings.AUDIO_VISUALISER_COLOUR_BOTTOM_LABEL), nameof(EzHUDStrings.AUDIO_VISUALISER_COLOUR_BOTTOM_DESCRIPTION))]
        public BindableColour4 ColourBottom { get; } = new BindableColour4(Color4.White);

        public bool UsesFixedAnchor { get; set; }

        private readonly float[] currentAmplitudes = new float[spectrum_size];
        private readonly float[] displayAmplitudes = new float[max_display_points];
        private readonly List<Box> bars = new List<Box>();

        private Container<Box> barsContainer = null!;
        private EzCurveVisualiser? curveVisualiser;

        [Resolved(canBeNull: true)]
        private IBeatSyncProvider? beatSyncProvider { get; set; }

        public EzAudioVisualiser()
        {
            RelativeSizeAxes = Axes.None;
            AutoSizeAxes = Axes.None;

            // The component's own size is driven entirely by the width/height settings,
            // so the skin editor's drag-resize (which writes Drawable.Width/Height) is overridden here.
            Anchor = Anchor.Centre;
            Origin = Anchor.Centre;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            StyleMode.BindValueChanged(_ => rebuildContent(), true);
            BarCount.BindValueChanged(_ => rebuildContent(), true);
            EdgeFade.BindValueChanged(_ => rebuildContent(), true);
            BarWidth.BindValueChanged(_ => updateLayout(), true);
            TotalHeight.BindValueChanged(_ => updateLayout(), true);
            TotalWidth.BindValueChanged(_ => updateLayout(), true);
            PointCount.BindValueChanged(_ => updateLayout(), true);
            LineThickness.BindValueChanged(_ => updateLayout(), true);
            ColourTop.BindValueChanged(_ => applyColour(), true);
            ColourBottom.BindValueChanged(_ => applyColour(), true);
            BarAlpha.BindValueChanged(_ => applyColour(), true);

            rebuildContent();
        }

        protected override void Update()
        {
            base.Update();

            if (beatSyncProvider == null)
                return;

            int count = StyleMode.Value == EzAudioVisualiserStyle.Curve ? PointCount.Value : BarCount.Value;

            var amplitudes = beatSyncProvider.CurrentAmplitudes.FrequencyAmplitudes.Span;

            float kiaiMultiplier = beatSyncProvider.CheckIsKiaiTime() ? kiai_boost : 1f;

            int binsPerBar = Math.Max(1, amplitudes.Length / count);

            float sensitivity = Sensitivity.Value;

            for (int i = 0; i < count; i++)
            {
                int sourceIndex = getSourceIndex(i, count);
                int start = sourceIndex * binsPerBar;
                int end = sourceIndex == count - 1 ? amplitudes.Length : Math.Min(start + binsPerBar, amplitudes.Length);

                float peak = 0;
                for (int b = start; b < end; b++)
                    peak = MathF.Max(peak, amplitudes[b]);

                // apply sensitivity gain, then a gentle curve to lift low amplitudes for better readability.
                float target = MathF.Pow(MathF.Min(peak * kiaiMultiplier * sensitivity, 1f), amplitude_curve);

                // attack: jump straight up to the newest sample; the decay below handles the fall-off.
                if (target > currentAmplitudes[i])
                    currentAmplitudes[i] = target;
            }

            float decayFactor = (float)Time.Elapsed * decay_per_millisecond;
            float totalHeight = TotalHeight.Value;

            for (int i = 0; i < count; i++)
            {
                currentAmplitudes[i] -= decayFactor * (currentAmplitudes[i] + 0.03f);
                if (currentAmplitudes[i] < 0)
                    currentAmplitudes[i] = 0;
            }

            // build the displayed amplitudes: the real segment is kept untouched in the middle,
            // and zero-faded virtual segments are appended on both edges (outermost point is exactly 0).
            int edgePad = getEdgePad(count);
            int total = count + edgePad * 2;

            for (int i = 0; i < edgePad; i++)
                displayAmplitudes[i] = i == 0 ? 0 : currentAmplitudes[0] * smoothstep(i / (float)edgePad);

            for (int i = 0; i < count; i++)
                displayAmplitudes[edgePad + i] = currentAmplitudes[i];

            for (int i = 0; i < edgePad; i++)
            {
                int idx = edgePad + count + i;
                displayAmplitudes[idx] = i == edgePad - 1 ? 0 : currentAmplitudes[count - 1] * smoothstep((edgePad - 1 - i) / (float)edgePad);
            }

            for (int i = 0; i < total; i++)
            {
                if (barsContainer != null && i < bars.Count)
                    bars[i].Height = displayAmplitudes[i] * totalHeight;
            }

            curveVisualiser?.UpdateData(displayAmplitudes, total);
        }

        /// <summary>
        /// Maps a bar index to a frequency source index depending on the selected <see cref="LayoutMode"/>.
        /// </summary>
        private int getSourceIndex(int barIndex, int count)
        {
            switch (LayoutMode.Value)
            {
                case EzAudioVisualiserLayout.Side:
                case EzAudioVisualiserLayout.Center:
                {
                    if (count <= 1)
                        return 0;

                    // fold the bar position into a triangle wave; edges = one extreme, centre = the other.
                    float t = barIndex / (float)(count - 1);
                    float folded = 1 - MathF.Abs(2 * t - 1);

                    if (LayoutMode.Value == EzAudioVisualiserLayout.Side)
                        // Side: centre = high frequency, edges = low frequency.
                        return (int)MathF.Round(folded * (count - 1));

                    // Center: centre = low frequency (high energy, e.g. bass), edges = high frequency.
                    return (int)MathF.Round((1 - folded) * (count - 1));
                }

                default:
                    return barIndex;
            }
        }

        /// <summary>
        /// Number of zero-faded virtual points appended on each edge, based on <see cref="EdgeFade"/> (a fraction of the real point count).
        /// </summary>
        private int getEdgePad(int count) => EdgeFade.Value <= 0 ? 0 : (int)MathF.Round(EdgeFade.Value * count);

        private static float smoothstep(float t)
        {
            t = Math.Clamp(t, 0, 1);
            return t * t * (3 - 2 * t);
        }

        private void rebuildContent()
        {
            bars.Clear();
            ClearInternal();
            barsContainer = null;
            curveVisualiser = null;

            if (StyleMode.Value == EzAudioVisualiserStyle.Curve)
            {
                curveVisualiser = new EzCurveVisualiser
                {
                    RelativeSizeAxes = Axes.Both,
                };
                AddInternal(curveVisualiser);
            }
            else
            {
                barsContainer = new Container<Box>
                {
                    RelativeSizeAxes = Axes.Both,
                };
                AddInternal(barsContainer);

                int count = BarCount.Value;
                int total = count + getEdgePad(count) * 2;
                for (int i = 0; i < total; i++)
                {
                    var bar = new Box
                    {
                        Anchor = Anchor.BottomLeft,
                        Origin = Anchor.BottomLeft,
                    };

                    bars.Add(bar);
                    barsContainer.Add(bar);
                }
            }

            Array.Clear(currentAmplitudes);
            updateLayout();
            applyColour();
        }

        private void updateLayout()
        {
            if (StyleMode.Value == EzAudioVisualiserStyle.Curve)
            {
                int count = PointCount.Value;
                int edgePad = getEdgePad(count);

                // keep the real segment's point spacing unchanged; extend the total width by the virtual segments.
                float spacingX = count > 1 ? TotalWidth.Value / (count - 1) : TotalWidth.Value;
                float totalWidth = (count - 1 + edgePad * 2) * spacingX;

                Size = new Vector2(totalWidth, TotalHeight.Value);

                if (curveVisualiser != null)
                    curveVisualiser.Thickness = LineThickness.Value;
            }
            else
            {
                float barWidth = BarWidth.Value;
                float spacing = barWidth * spacing_ratio;
                int count = BarCount.Value;
                int total = count + getEdgePad(count) * 2;

                Size = new Vector2(total * barWidth + (total - 1) * spacing, TotalHeight.Value);

                if (barsContainer != null)
                {
                    for (int i = 0; i < bars.Count; i++)
                    {
                        bars[i].Position = new Vector2(i * (barWidth + spacing), 0);
                        bars[i].Width = barWidth;
                    }
                }
            }
        }

        private void applyColour()
        {
            float alpha = BarAlpha.Value;

            // Transparency is baked directly into the gradient colours (rather than using Drawable.Alpha)
            // to work around a framework bug where Drawable.Alpha is not applied to gradient (multi-colour) colours.
            var top = ColourTop.Value.Opacity(ColourTop.Value.A * alpha);
            var bottom = ColourBottom.Value.Opacity(ColourBottom.Value.A * alpha);

            var gradient = ColourInfo.GradientVertical(top, bottom);

            if (barsContainer != null)
            {
                foreach (var bar in bars)
                    bar.Colour = gradient;
            }

            if (curveVisualiser != null)
            {
                curveVisualiser.TopColour = top;
                curveVisualiser.BottomColour = bottom;
            }
        }
    }

    /// <summary>
    /// Renders a smooth curve (line + filled bottom area with a vertical gradient) from frequency amplitude data.
    /// </summary>
    internal partial class EzCurveVisualiser : Drawable
    {
        private const int catmull_steps = 6;

        internal float Thickness = 3f;
        internal Color4 TopColour = Color4.White;
        internal Color4 BottomColour = Color4.White;
        internal int PointCount = 48;

        private readonly float[] amplitudes = new float[512];

        internal void UpdateData(float[] source, int pointCount)
        {
            PointCount = pointCount;
            Array.Copy(source, amplitudes, Math.Min(source.Length, amplitudes.Length));
            Invalidate(Invalidation.DrawNode);
        }

        private IShader shader = null!;
        private Texture texture = null!;

        [BackgroundDependencyLoader]
        private void load(IRenderer renderer, ShaderManager shaders)
        {
            texture = renderer.WhitePixel;
            shader = shaders.Load(VertexShaderDescriptor.TEXTURE_2, FragmentShaderDescriptor.TEXTURE);
        }

        protected override DrawNode CreateDrawNode() => new CurveDrawNode(this);

        private class CurveDrawNode : DrawNode
        {
            private new EzCurveVisualiser Source => (EzCurveVisualiser)base.Source;

            private IShader shader = null!;
            private Texture texture = null!;
            private float width;
            private float height;
            private float thickness;
            private int pointCount;
            private ColourInfo fillColour;

            private readonly float[] amplitudes = new float[512];
            private readonly Vector2[] rawPoints = new Vector2[512];
            private readonly Vector2[] curvePoints = new Vector2[(512 - 1) * catmull_steps + 1];

            private IVertexBatch<TexturedVertex2D>? vertexBatch;

            public CurveDrawNode(EzCurveVisualiser source)
                : base(source)
            {
            }

            public override void ApplyState()
            {
                base.ApplyState();

                shader = Source.shader;
                texture = Source.texture;
                width = Source.DrawSize.X;
                height = Source.DrawSize.Y;
                thickness = Source.Thickness;
                pointCount = Source.PointCount;
                fillColour = ColourInfo.GradientVertical(Source.TopColour, Source.BottomColour);

                Source.amplitudes.AsSpan().CopyTo(amplitudes);

                int count = Math.Min(pointCount, amplitudes.Length);
                if (count < 2)
                    return;

                for (int i = 0; i < count; i++)
                {
                    float x = i / (float)(count - 1) * width;
                    float y = height - Math.Clamp(amplitudes[i], 0, 1) * height;
                    rawPoints[i] = new Vector2(x, y);
                }

                fillCatmullOpen(rawPoints, curvePoints, count, catmull_steps);
            }

            protected override void Draw(IRenderer renderer)
            {
                base.Draw(renderer);

                if (width <= 0 || height <= 0 || pointCount < 2)
                    return;

                vertexBatch ??= renderer.CreateQuadBatch<TexturedVertex2D>(1000, 10);

                shader.Bind();

                Vector2 inflation = DrawInfo.MatrixInverse.ExtractScale().Xy;

                int curvePointCount = (pointCount - 1) * catmull_steps + 1;

                // fill the region between the curve and the baseline, with a vertical gradient.
                for (int i = 0; i < curvePointCount - 1; i++)
                {
                    Vector2 topLeft = curvePoints[i];
                    Vector2 topRight = curvePoints[i + 1];
                    Vector2 bottomLeft = new Vector2(topLeft.X, height);
                    Vector2 bottomRight = new Vector2(topRight.X, height);

                    var quad = new Quad(
                        Vector2Extensions.Transform(topLeft, DrawInfo.Matrix),
                        Vector2Extensions.Transform(topRight, DrawInfo.Matrix),
                        Vector2Extensions.Transform(bottomLeft, DrawInfo.Matrix),
                        Vector2Extensions.Transform(bottomRight, DrawInfo.Matrix)
                    );

                    renderer.DrawQuad(
                        texture,
                        quad,
                        fillColour,
                        null,
                        vertexBatch!.AddAction,
                        Vector2.Divide(inflation, new Vector2(Math.Max(topRight.X - topLeft.X, 0.001f), height)));
                }

                // stroke the curve.
                for (int i = 0; i < curvePointCount - 1; i++)
                    drawSegment(renderer, fillColour, inflation, curvePoints[i], curvePoints[i + 1], thickness);

                shader.Unbind();
            }

            private void drawSegment(IRenderer renderer, ColourInfo colourInfo, Vector2 inflation, Vector2 start, Vector2 end, float thickness)
            {
                Vector2 delta = end - start;
                float length = delta.Length;

                if (length < 0.001f)
                    return;

                float halfThickness = thickness / 2;
                var normal = new Vector2(-delta.Y / length, delta.X / length) * halfThickness;

                var rectangle = new Quad(
                    Vector2Extensions.Transform(start - normal, DrawInfo.Matrix),
                    Vector2Extensions.Transform(end - normal, DrawInfo.Matrix),
                    Vector2Extensions.Transform(start + normal, DrawInfo.Matrix),
                    Vector2Extensions.Transform(end + normal, DrawInfo.Matrix)
                );

                renderer.DrawQuad(
                    texture,
                    rectangle,
                    colourInfo,
                    null,
                    vertexBatch!.AddAction,
                    Vector2.Divide(inflation, new Vector2(thickness, length)));
            }

            /// <summary>
            /// Catmull-Rom spline interpolation for an open (non-closed) polyline.
            /// Open ends use an extrapolated (mirrored) virtual point instead of clamping the
            /// tangent, which avoids the first/last segment visually shrinking at the edges.
            /// </summary>
            private static void fillCatmullOpen(Vector2[] source, Vector2[] dest, int pointCount, int steps)
            {
                int d = 0;

                for (int i = 0; i < pointCount - 1; i++)
                {
                    // Mirror-extrapolate a virtual control point past each open end so the endpoint
                    // tangent points towards the next interior sample (natural extension, no shrink).
                    Vector2 p0 = i == 0
                        ? source[0] + (source[0] - source[1])
                        : source[i - 1];

                    Vector2 p1 = source[i];
                    Vector2 p2 = source[i + 1];

                    Vector2 p3 = i == pointCount - 2
                        ? source[pointCount - 1] + (source[pointCount - 1] - source[pointCount - 2])
                        : source[i + 2];

                    int lastT = i == pointCount - 2 ? steps : steps - 1;

                    for (int t = 0; t <= lastT; t++)
                    {
                        float s = t / (float)steps;
                        float s2 = s * s;
                        float s3 = s2 * s;

                        dest[d++] = 0.5f * (
                            2f * p1
                            + (-p0 + p2) * s
                            + (2f * p0 - 5f * p1 + 4f * p2 - p3) * s2
                            + (-p0 + 3f * p1 - 3f * p2 + p3) * s3);
                    }
                }
            }

            protected override void Dispose(bool isDisposing)
            {
                base.Dispose(isDisposing);
                vertexBatch?.Dispose();
            }
        }
    }
}
