// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Reflection;
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
    public partial class EzAudioVisualiser : CompositeDrawable, ISerialisableDrawable, IScopedSkinSettings
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
        /// Fraction of <see cref="BarWidth"/> used as the gap between adjacent bars.
        /// </summary>
        private const float spacing_ratio = 0.2f;

        /// <summary>
        /// Height of the optional peak-hold line drawn above each bar.
        /// </summary>
        private const float peak_line_height = 2f;

        /// <summary>
        /// Exponent applied to amplitudes (values &lt; 1 are lifted), improving sensitivity at low volumes.
        /// </summary>
        private const float amplitude_curve = 0.65f;

        /// <summary>
        /// Additional gain applied during kiai sections. Normal sections use full amplitude (no penalty).
        /// </summary>
        private const float kiai_boost = 1.25f;

        /// <summary>
        /// Neon colour ramp endpoints, matching the full gradient defined in <see cref="EzBarVisualiser"/>.
        /// </summary>
        private static readonly Colour4 neon_bottom = Colour4.FromHex("17E01C");
        private static readonly Colour4 neon_top = Colour4.FromHex("FF3326");

        [SettingSource(typeof(EzHUDStrings), nameof(EzHUDStrings.AUDIO_VISUALISER_STYLE_LABEL), nameof(EzHUDStrings.AUDIO_VISUALISER_STYLE_DESCRIPTION), 10)]
        public Bindable<EzAudioVisualiserStyle> StyleMode { get; } = new Bindable<EzAudioVisualiserStyle>(EzAudioVisualiserStyle.Solid);

        [SettingSource(typeof(EzHUDStrings), nameof(EzHUDStrings.AUDIO_VISUALISER_SOLID_HEADER), null, 11, SettingControlType = typeof(EzSettingsSectionDivider))]
        public Bindable<bool> SolidHeader { get; } = new Bindable<bool>(true);

        [SettingSource(typeof(EzHUDStrings), nameof(EzHUDStrings.AUDIO_VISUALISER_BAR_WIDTH_LABEL), nameof(EzHUDStrings.AUDIO_VISUALISER_BAR_WIDTH_DESCRIPTION), 12)]
        public BindableNumber<float> BarWidth { get; } = new BindableNumber<float>(8)
        {
            MinValue = 2,
            MaxValue = 64,
            Precision = 1,
        };

        [SettingSource(typeof(EzHUDStrings), nameof(EzHUDStrings.AUDIO_VISUALISER_BAR_COUNT_LABEL), nameof(EzHUDStrings.AUDIO_VISUALISER_BAR_COUNT_DESCRIPTION), 13)]
        public BindableNumber<int> BarCount { get; } = new BindableNumber<int>(64)
        {
            MinValue = 4,
            MaxValue = spectrum_size,
            Precision = 1,
        };

        [SettingSource(typeof(EzHUDStrings), nameof(EzHUDStrings.AUDIO_VISUALISER_CURVE_HEADER), null, 22, SettingControlType = typeof(EzSettingsSectionDivider))]
        public Bindable<bool> CurveHeader { get; } = new Bindable<bool>(true);

        [SettingSource(typeof(EzHUDStrings), nameof(EzHUDStrings.AUDIO_VISUALISER_TOTAL_WIDTH_LABEL), nameof(EzHUDStrings.AUDIO_VISUALISER_TOTAL_WIDTH_DESCRIPTION), 23)]
        public BindableNumber<float> TotalWidth { get; } = new BindableNumber<float>(512)
        {
            MinValue = 100,
            MaxValue = 2000,
            Precision = 1,
        };

        [SettingSource(typeof(EzHUDStrings), nameof(EzHUDStrings.AUDIO_VISUALISER_POINT_COUNT_LABEL), nameof(EzHUDStrings.AUDIO_VISUALISER_POINT_COUNT_DESCRIPTION), 24)]
        public BindableNumber<int> PointCount { get; } = new BindableNumber<int>(48)
        {
            MinValue = 8,
            MaxValue = spectrum_size,
            Precision = 1,
        };

        [SettingSource(typeof(EzHUDStrings), nameof(EzHUDStrings.AUDIO_VISUALISER_LINE_THICKNESS_LABEL), nameof(EzHUDStrings.AUDIO_VISUALISER_LINE_THICKNESS_DESCRIPTION), 25)]
        public BindableNumber<float> LineThickness { get; } = new BindableNumber<float>(3)
        {
            MinValue = 1,
            MaxValue = 20,
            Precision = 0.5f,
        };

        [SettingSource(typeof(EzHUDStrings), nameof(EzHUDStrings.AUDIO_VISUALISER_TOTAL_HEIGHT_LABEL), nameof(EzHUDStrings.AUDIO_VISUALISER_TOTAL_HEIGHT_DESCRIPTION), 0)]
        public BindableNumber<float> TotalHeight { get; } = new BindableNumber<float>(240)
        {
            MinValue = 20,
            MaxValue = 1000,
            Precision = 1,
        };

        [SettingSource(typeof(EzHUDStrings), nameof(EzHUDStrings.AUDIO_VISUALISER_EDGE_FADE_LABEL), nameof(EzHUDStrings.AUDIO_VISUALISER_EDGE_FADE_DESCRIPTION), 3)]
        public BindableNumber<float> EdgeFade { get; } = new BindableNumber<float>(0.05f)
        {
            MinValue = 0,
            MaxValue = 0.25f,
            Precision = 0.01f,
        };

        [SettingSource(typeof(EzHUDStrings), nameof(EzHUDStrings.AUDIO_VISUALISER_SENSITIVITY_LABEL), nameof(EzHUDStrings.AUDIO_VISUALISER_SENSITIVITY_DESCRIPTION), 1)]
        public BindableNumber<float> Sensitivity { get; } = new BindableNumber<float>(1.2f)
        {
            MinValue = 0.1f,
            MaxValue = 5f,
            Precision = 0.05f,
        };

        [SettingSource(typeof(EzHUDStrings), nameof(EzHUDStrings.AUDIO_VISUALISER_LAYOUT_LABEL), nameof(EzHUDStrings.AUDIO_VISUALISER_LAYOUT_DESCRIPTION), 5)]
        public Bindable<EzAudioVisualiserLayout> LayoutMode { get; } = new Bindable<EzAudioVisualiserLayout>(EzAudioVisualiserLayout.Left);

        [SettingSource(typeof(EzHUDStrings), nameof(EzHUDStrings.AUDIO_VISUALISER_ALPHA_LABEL), nameof(EzHUDStrings.AUDIO_VISUALISER_ALPHA_DESCRIPTION), 4)]
        public BindableNumber<float> BarAlpha { get; } = new BindableNumber<float>(1)
        {
            MinValue = 0,
            MaxValue = 1,
            Precision = 0.01f,
        };

        [SettingSource(typeof(EzHUDStrings), nameof(EzHUDStrings.AUDIO_VISUALISER_COLOUR_TOP_LABEL), nameof(EzHUDStrings.AUDIO_VISUALISER_COLOUR_TOP_DESCRIPTION), 8)]
        public BindableColour4 ColourTop { get; } = new BindableColour4(Color4.White);

        [SettingSource(typeof(EzHUDStrings), nameof(EzHUDStrings.AUDIO_VISUALISER_COLOUR_BOTTOM_LABEL), nameof(EzHUDStrings.AUDIO_VISUALISER_COLOUR_BOTTOM_DESCRIPTION), 9)]
        public BindableColour4 ColourBottom { get; } = new BindableColour4(Color4.White);

        [SettingSource(typeof(EzHUDStrings), nameof(EzHUDStrings.AUDIO_VISUALISER_COLOUR_MODE_LABEL), nameof(EzHUDStrings.AUDIO_VISUALISER_COLOUR_MODE_DESCRIPTION), 7)]
        public Bindable<EzAudioVisualiserColourMode> ColourMode { get; } = new Bindable<EzAudioVisualiserColourMode>(EzAudioVisualiserColourMode.Gradient);

        [SettingSource(typeof(EzHUDStrings), nameof(EzHUDStrings.AUDIO_VISUALISER_SCALE_LABEL), nameof(EzHUDStrings.AUDIO_VISUALISER_SCALE_DESCRIPTION), 6)]
        public Bindable<EzAudioVisualiserFrequencyScale> FrequencyScale { get; } = new Bindable<EzAudioVisualiserFrequencyScale>(EzAudioVisualiserFrequencyScale.Logarithmic);

        [SettingSource(typeof(EzHUDStrings), nameof(EzHUDStrings.AUDIO_VISUALISER_SMOOTHING_LABEL), nameof(EzHUDStrings.AUDIO_VISUALISER_SMOOTHING_DESCRIPTION), 2)]
        public BindableNumber<float> Smoothing { get; } = new BindableNumber<float>(0.5f)
        {
            MinValue = 0,
            MaxValue = 0.95f,
            Precision = 0.01f,
        };

        [SettingSource(typeof(EzHUDStrings), nameof(EzHUDStrings.AUDIO_VISUALISER_PEAKS_LABEL), nameof(EzHUDStrings.AUDIO_VISUALISER_PEAKS_DESCRIPTION), 19)]
        public Bindable<bool> ShowPeaks { get; } = new Bindable<bool>(true);

        [SettingSource(typeof(EzHUDStrings), nameof(EzHUDStrings.AUDIO_VISUALISER_PEAK_HOLD_LABEL), nameof(EzHUDStrings.AUDIO_VISUALISER_PEAK_HOLD_DESCRIPTION), 20)]
        public BindableNumber<float> PeakHoldTime { get; } = new BindableNumber<float>(500)
        {
            MinValue = 0,
            MaxValue = 3000,
            Precision = 10,
        };

        [SettingSource(typeof(EzHUDStrings), nameof(EzHUDStrings.AUDIO_VISUALISER_PEAK_FALL_LABEL), nameof(EzHUDStrings.AUDIO_VISUALISER_PEAK_FALL_DESCRIPTION), 21)]
        public BindableNumber<float> PeakFall { get; } = new BindableNumber<float>(1.6f)
        {
            MinValue = 0,
            MaxValue = 10,
            Precision = 0.1f,
        };

        [SettingSource(typeof(EzHUDStrings), nameof(EzHUDStrings.AUDIO_VISUALISER_LED_HEIGHT_LABEL), nameof(EzHUDStrings.AUDIO_VISUALISER_LED_HEIGHT_DESCRIPTION), 14)]
        public BindableNumber<float> LedHeight { get; } = new BindableNumber<float>(8)
        {
            MinValue = 0,
            MaxValue = 40,
            Precision = 1,
        };

        [SettingSource(typeof(EzHUDStrings), nameof(EzHUDStrings.AUDIO_VISUALISER_LED_GAP_LABEL), nameof(EzHUDStrings.AUDIO_VISUALISER_LED_GAP_DESCRIPTION), 15)]
        public BindableNumber<float> LedGap { get; } = new BindableNumber<float>(2f)
        {
            MinValue = 0,
            MaxValue = 6,
            Precision = 0.5f,
        };

        [SettingSource(typeof(EzHUDStrings), nameof(EzHUDStrings.AUDIO_VISUALISER_GHOST_LABEL), nameof(EzHUDStrings.AUDIO_VISUALISER_GHOST_DESCRIPTION), 16)]
        public BindableNumber<float> GhostAlpha { get; } = new BindableNumber<float>(0.1f)
        {
            MinValue = 0,
            MaxValue = 0.5f,
            Precision = 0.01f,
        };

        [SettingSource(typeof(EzHUDStrings), nameof(EzHUDStrings.AUDIO_VISUALISER_BEVEL_LABEL), nameof(EzHUDStrings.AUDIO_VISUALISER_BEVEL_DESCRIPTION), 17)]
        public BindableNumber<float> BevelHighlight { get; } = new BindableNumber<float>(0.35f)
        {
            MinValue = 0,
            MaxValue = 1f,
            Precision = 0.05f,
        };

        [SettingSource(typeof(EzHUDStrings), nameof(EzHUDStrings.AUDIO_VISUALISER_GLOW_LABEL), nameof(EzHUDStrings.AUDIO_VISUALISER_GLOW_DESCRIPTION), 18)]
        public BindableNumber<float> GlowStrength { get; } = new BindableNumber<float>(0.35f)
        {
            MinValue = 0,
            MaxValue = 1f,
            Precision = 0.05f,
        };

        public bool UsesFixedAnchor { get; set; }

        private readonly float[] currentAmplitudes = new float[spectrum_size];
        private readonly float[] displayAmplitudes = new float[max_display_points];
        private readonly float[] displayPeaks = new float[max_display_points];
        private readonly float[] peakAmplitudes = new float[spectrum_size];
        private readonly float[] peakVelocities = new float[spectrum_size];
        private readonly double[] peakHoldUntil = new double[spectrum_size];
        private readonly float[] bandLow = new float[spectrum_size];
        private readonly float[] bandHigh = new float[spectrum_size];

        private EzBarVisualiser? barVisualiser;
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
            PointCount.BindValueChanged(_ =>
            {
                updateLayout();
                rebuildBands();
            }, true);
            LineThickness.BindValueChanged(_ => updateLayout(), true);
            ColourTop.BindValueChanged(_ => applyColour(), true);
            ColourBottom.BindValueChanged(_ => applyColour(), true);
            ColourMode.BindValueChanged(_ => applyColour());
            BarAlpha.BindValueChanged(_ => applyColour(), true);

            FrequencyScale.BindValueChanged(_ => rebuildBands());
            ShowPeaks.BindValueChanged(_ => applyBarSettings());
            LedHeight.BindValueChanged(_ => applyBarSettings());
            LedGap.BindValueChanged(_ => applyBarSettings());
            GhostAlpha.BindValueChanged(_ => applyBarSettings());
            BevelHighlight.BindValueChanged(_ => applyBarSettings());
            GlowStrength.BindValueChanged(_ => applyBarSettings());

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
            float sensitivity = Sensitivity.Value;

            // Frame-rate independent follow coefficient: 1 = jump straight to target, 0 = never move.
            float follow = 1f - Math.Clamp(Smoothing.Value, 0f, 0.95f);
            float elapsedFrames = (float)Math.Clamp(Time.Elapsed / 16.0, 0.1, 8.0);
            float followPerFrame = 1f - MathF.Pow(1f - follow, elapsedFrames);
            float attackFollow = MathF.Min(1f, followPerFrame * 1.8f);

            double now = Time.Current;
            float dt = (float)Math.Clamp(Time.Elapsed / 1000.0, 0.0, 0.1);
            float holdMs = PeakHoldTime.Value;
            float fallAccel = PeakFall.Value * 2f;

            for (int i = 0; i < count; i++)
            {
                int sourceIndex = getSourceIndex(i, count);

                float raw = sampleBand(sourceIndex, amplitudes);

                // apply sensitivity gain, then a gentle curve to lift low amplitudes for better readability.
                float target = MathF.Pow(MathF.Min(raw * kiaiMultiplier * sensitivity, 1f), amplitude_curve);

                float coeff = target >= currentAmplitudes[i] ? attackFollow : followPerFrame;
                currentAmplitudes[i] += (target - currentAmplitudes[i]) * coeff;

                updatePeak(i, currentAmplitudes[i], now, dt, holdMs, fallAccel);
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

            if (ShowPeaks.Value)
            {
                Array.Clear(displayPeaks, 0, total);

                for (int i = 0; i < count; i++)
                    displayPeaks[edgePad + i] = peakAmplitudes[i];
            }

            barVisualiser?.UpdateData(displayAmplitudes, displayPeaks, total);
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

        private void rebuildBands() => buildBands(StyleMode.Value == EzAudioVisualiserStyle.Curve ? PointCount.Value : BarCount.Value);

        public void WatchSettingsScope(Action onChange)
        {
            StyleMode.BindValueChanged(_ => onChange());
            ColourMode.BindValueChanged(_ => onChange());
            LedHeight.BindValueChanged(_ => onChange());
            ShowPeaks.BindValueChanged(_ => onChange());
        }

        /// <summary>
        /// Hides settings which do not apply to the current style (or to the current sub-options)
        /// so the layout editor sidebar only shows relevant controls.
        /// </summary>
        public bool IsSettingActive(PropertyInfo property)
        {
            bool isCurve = StyleMode.Value == EzAudioVisualiserStyle.Curve;

            switch (property.Name)
            {
                case nameof(SolidHeader):
                case nameof(BarWidth):
                case nameof(BarCount):
                case nameof(LedHeight):
                case nameof(ShowPeaks):
                    return !isCurve;

                case nameof(LedGap):
                    return !isCurve && (LedHeight.Value > 0 || ColourMode.Value == EzAudioVisualiserColourMode.Neon);

                case nameof(GhostAlpha):
                case nameof(BevelHighlight):
                case nameof(GlowStrength):
                    return !isCurve && (LedHeight.Value > 0 || ColourMode.Value == EzAudioVisualiserColourMode.Neon);

                case nameof(PeakHoldTime):
                case nameof(PeakFall):
                    return !isCurve && ShowPeaks.Value;

                case nameof(CurveHeader):
                case nameof(TotalWidth):
                case nameof(PointCount):
                case nameof(LineThickness):
                    return isCurve;

                case nameof(ColourTop):
                case nameof(ColourBottom):
                    return ColourMode.Value == EzAudioVisualiserColourMode.Gradient;

                default:
                    return true;
            }
        }

        /// <summary>
        /// Precomputes the frequency bin range covered by each bar. Linear scales split the spectrum evenly,
        /// while logarithmic scales give low frequencies proportionally more bars.
        /// </summary>
        private void buildBands(int count)
        {
            count = Math.Clamp(count, 1, spectrum_size);

            float minBin = 0.5f;
            float maxBin = spectrum_size - 1;

            if (FrequencyScale.Value == EzAudioVisualiserFrequencyScale.Logarithmic)
            {
                double logMin = Math.Log(minBin);
                double logMax = Math.Log(maxBin);

                for (int i = 0; i < count; i++)
                {
                    bandLow[i] = (float)Math.Exp(logMin + (logMax - logMin) * i / count);
                    bandHigh[i] = (float)Math.Exp(logMin + (logMax - logMin) * (i + 1) / count);
                }
            }
            else
            {
                for (int i = 0; i < count; i++)
                {
                    bandLow[i] = minBin + (maxBin - minBin) * i / count;
                    bandHigh[i] = minBin + (maxBin - minBin) * (i + 1) / count;
                }
            }
        }

        /// <summary>
        /// Samples one band, taking the maximum of all covered bins plus interpolated edge values so that
        /// narrow high-frequency bands do not collapse onto a single bin.
        /// </summary>
        private float sampleBand(int band, ReadOnlySpan<float> amplitudes)
        {
            if (amplitudes.Length == 0)
                return 0;

            float lo = bandLow[band];
            float hi = bandHigh[band];

            float value = MathF.Max(sampleBin(amplitudes, lo), sampleBin(amplitudes, hi));

            int start = Math.Max(0, (int)MathF.Ceiling(lo));
            int end = (int)MathF.Floor(hi);

            for (int b = start; b <= end && b < amplitudes.Length; b++)
                value = MathF.Max(value, amplitudes[b]);

            return value;
        }

        private static float sampleBin(ReadOnlySpan<float> amplitudes, float position)
        {
            int index = Math.Clamp((int)position, 0, amplitudes.Length - 1);
            int next = Math.Min(index + 1, amplitudes.Length - 1);
            float frac = Math.Clamp(position - index, 0f, 1f);
            return amplitudes[index] + (amplitudes[next] - amplitudes[index]) * frac;
        }

        private void updatePeak(int index, float value, double now, float dt, float holdMs, float fallAccel)
        {
            if (value >= peakAmplitudes[index])
            {
                peakAmplitudes[index] = value;
                peakVelocities[index] = 0;
                peakHoldUntil[index] = now + holdMs;
                return;
            }

            if (fallAccel <= 0 || now < peakHoldUntil[index])
                return;

            peakVelocities[index] += fallAccel * dt;
            peakAmplitudes[index] = MathF.Max(value, peakAmplitudes[index] - peakVelocities[index] * dt);
        }

        private void rebuildContent()
        {
            ClearInternal();
            barVisualiser = null;
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
                barVisualiser = new EzBarVisualiser
                {
                    RelativeSizeAxes = Axes.Both,
                };
                AddInternal(barVisualiser);
            }

            Array.Clear(currentAmplitudes);
            Array.Clear(peakAmplitudes);
            Array.Clear(peakVelocities);
            Array.Clear(peakHoldUntil);

            updateLayout();
            applyColour();
            rebuildBands();
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

                applyBarSettings();
            }
        }

        private void applyBarSettings()
        {
            if (barVisualiser == null)
                return;

            barVisualiser.BarWidth = BarWidth.Value;
            barVisualiser.Spacing = BarWidth.Value * spacing_ratio;
            barVisualiser.LedHeight = LedHeight.Value;
            barVisualiser.SegmentGap = LedGap.Value;
            barVisualiser.GhostAlpha = GhostAlpha.Value;
            barVisualiser.BevelHighlight = BevelHighlight.Value;
            barVisualiser.GlowStrength = GlowStrength.Value;
            barVisualiser.ShowPeaks = ShowPeaks.Value;
            barVisualiser.PeakLineHeight = peak_line_height;
        }

        private void applyColour()
        {
            float alpha = BarAlpha.Value;
            bool neon = ColourMode.Value == EzAudioVisualiserColourMode.Neon;

            // Transparency is baked directly into the colours (rather than using Drawable.Alpha)
            // to work around a framework bug where Drawable.Alpha is not applied to gradient (multi-colour) colours.
            var top = neon ? neon_top.Opacity(neon_top.A * alpha) : ColourTop.Value.Opacity(ColourTop.Value.A * alpha);
            var bottom = neon ? neon_bottom.Opacity(neon_bottom.A * alpha) : ColourBottom.Value.Opacity(ColourBottom.Value.A * alpha);

            if (barVisualiser != null)
            {
                barVisualiser.NeonColour = neon;
                barVisualiser.FillAlpha = alpha;
                barVisualiser.TopColour = top;
                barVisualiser.BottomColour = bottom;
            }

            if (curveVisualiser != null)
            {
                curveVisualiser.TopColour = top;
                curveVisualiser.BottomColour = bottom;
            }
        }
    }

    /// <summary>
    /// Draws the flat-bottom bar chart in a single draw node, including optional LED segmentation and peak-hold lines.
    /// </summary>
    internal partial class EzBarVisualiser : Drawable
    {
        private const int max_points = 512;

        internal float BarWidth = 8f;
        internal float Spacing = 1.6f;
        internal float LedHeight;
        internal float SegmentGap = 1f;
        internal bool ShowPeaks = true;
        internal float PeakLineHeight = 2f;
        internal bool NeonColour;
        internal float FillAlpha = 1f;
        internal float GhostAlpha = 0.1f;
        internal float BevelHighlight = 0.35f;
        internal float GlowStrength = 0.35f;
        internal Color4 TopColour = Color4.White;
        internal Color4 BottomColour = Color4.White;

        private const float neon_default_led_height = 8f;
        private const float neon_gamma = 0.75f;

        private static readonly (float pos, Color4 colour)[] neon_stops =
        {
            (0f, new Color4(0.09f, 0.88f, 0.11f, 1f)), // green
            (0.18f, new Color4(0.62f, 0.95f, 0.12f, 1f)), // lime
            (0.36f, new Color4(0.95f, 0.89f, 0.16f, 1f)), // yellow
            (0.55f, new Color4(1f, 0.63f, 0.12f, 1f)), // orange
            (0.72f, new Color4(1f, 0.20f, 0.15f, 1f)), // red
            (1f, new Color4(1f, 0.20f, 0.15f, 1f)), // red
        };

        private static Color4 neonColour(float level)
        {
            // lift mid-levels so taller bars actually reach the warm colours
            level = MathF.Pow(Math.Clamp(level, 0f, 1f), neon_gamma);

            for (int i = 1; i < neon_stops.Length; i++)
            {
                if (level > neon_stops[i].pos)
                    continue;

                var (p0, c0) = neon_stops[i - 1];
                var (p1, c1) = neon_stops[i];
                float span = p1 - p0;

                return interpolate(c0, c1, span <= 0 ? 0 : (level - p0) / span);
            }

            return neon_stops[^1].colour;
        }

        private static Color4 interpolate(Color4 from, Color4 to, float t)
        {
            t = Math.Clamp(t, 0f, 1f);

            return new Color4(
                from.R + (to.R - from.R) * t,
                from.G + (to.G - from.G) * t,
                from.B + (to.B - from.B) * t,
                from.A + (to.A - from.A) * t);
        }

        private readonly float[] amplitudes = new float[max_points];
        private readonly float[] peaks = new float[max_points];
        private int pointCount;

        internal void UpdateData(ReadOnlySpan<float> source, ReadOnlySpan<float> peakSource, int count)
        {
            pointCount = Math.Min(count, max_points);

            if (pointCount <= 0)
            {
                Invalidate(Invalidation.DrawNode);
                return;
            }

            source.Slice(0, Math.Min(pointCount, source.Length)).CopyTo(amplitudes);
            peakSource.Slice(0, Math.Min(pointCount, peakSource.Length)).CopyTo(peaks);

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

        protected override DrawNode CreateDrawNode() => new BarDrawNode(this);

        private class BarDrawNode : DrawNode
        {
            private new EzBarVisualiser Source => (EzBarVisualiser)base.Source;

            private IShader shader = null!;
            private Texture texture = null!;
            private float width;
            private float height;
            private float barWidth;
            private float spacing;
            private float ledHeight;
            private float segmentGap;
            private bool showPeaks;
            private float peakLineHeight;
            private bool neon;
            private float fillAlpha;
            private float ghostAlpha;
            private float bevelHighlight;
            private float glowStrength;
            private Color4 topColour;
            private Color4 bottomColour;

            private readonly float[] amplitudes = new float[max_points];
            private readonly float[] peaks = new float[max_points];
            private int pointCount;

            private IVertexBatch<TexturedVertex2D>? vertexBatch;

            public BarDrawNode(EzBarVisualiser source)
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
                barWidth = Source.BarWidth;
                spacing = Source.Spacing;
                ledHeight = Source.LedHeight;
                segmentGap = Source.SegmentGap;
                showPeaks = Source.ShowPeaks;
                peakLineHeight = Source.PeakLineHeight;
                neon = Source.NeonColour;
                fillAlpha = Source.FillAlpha;
                ghostAlpha = Source.GhostAlpha;
                bevelHighlight = Source.BevelHighlight;
                glowStrength = Source.GlowStrength;
                topColour = Source.TopColour;
                bottomColour = Source.BottomColour;
                pointCount = Source.pointCount;

                Source.amplitudes.AsSpan().CopyTo(amplitudes);
                Source.peaks.AsSpan().CopyTo(peaks);
            }

            protected override void Draw(IRenderer renderer)
            {
                base.Draw(renderer);

                if (width <= 0 || height <= 0 || pointCount <= 0)
                    return;

                vertexBatch ??= renderer.CreateQuadBatch<TexturedVertex2D>(1000, 10);

                shader.Bind();

                float targetSegmentHeight = ledHeight > 0 ? ledHeight : (neon ? neon_default_led_height : 0f);

                if (targetSegmentHeight <= 0)
                {
                    var gradient = ColourInfo.GradientVertical(topColour, bottomColour);

                    for (int i = 0; i < pointCount; i++)
                    {
                        float barHeight = Math.Clamp(amplitudes[i], 0f, 1f) * height;

                        if (barHeight <= 0)
                            continue;

                        drawQuad(renderer, i * (barWidth + spacing), height - barHeight, barWidth, barHeight, gradient);
                    }
                }
                else
                {
                    int divisions = Math.Max(1, (int)MathF.Ceiling(height / targetSegmentHeight));
                    float segmentHeight = height / divisions;
                    float gap = Math.Min(segmentGap, segmentHeight * 0.5f);
                    float bevelHeight = segmentHeight * 0.3f;

                    for (int i = 0; i < pointCount; i++)
                    {
                        float barHeight = Math.Clamp(amplitudes[i], 0f, 1f) * height;
                        int lit = barHeight <= 0 ? 0 : Math.Min(divisions, (int)MathF.Ceiling(barHeight / segmentHeight));

                        float x = i * (barWidth + spacing);

                        // Unlit grid first so lit segments cleanly cover it (Win7-style LED panel).
                        if (ghostAlpha > 0)
                        {
                            var ghost = ColourInfo.SingleColour(new Color4(1f, 1f, 1f, ghostAlpha * fillAlpha));

                            for (int s = lit; s < divisions; s++)
                            {
                                float gy0 = height - (s + 1) * segmentHeight + gap * 0.5f;
                                float gy1 = height - s * segmentHeight - gap * 0.5f;

                                if (gy1 <= gy0)
                                    continue;

                                drawQuad(renderer, x, gy0, barWidth, gy1 - gy0, ghost);
                            }
                        }

                        if (glowStrength > 0 && lit > 0)
                        {
                            float litHeight = lit * segmentHeight;
                            float pad = 6f * glowStrength;
                            var glow = levelColour(Math.Min(1f, (lit - 0.5f) / divisions));
                            glow.A = 0.35f * glowStrength * fillAlpha;

                            drawQuad(renderer, x - pad, height - litHeight - pad, barWidth + pad * 2, litHeight + pad * 2,
                                ColourInfo.SingleColour(glow));
                        }

                        for (int s = 0; s < lit; s++)
                        {
                            float y0 = height - (s + 1) * segmentHeight + gap * 0.5f;
                            float y1 = height - s * segmentHeight - gap * 0.5f;

                            if (y1 <= y0)
                                continue;

                            float t = (s + 0.5f) / divisions;
                            var colour = levelColour(t);

                            drawQuad(renderer, x, y0, barWidth, y1 - y0, ColourInfo.SingleColour(colour));

                            if (bevelHighlight > 0 && bevelHeight > 0.5f)
                            {
                                var highlight = interpolate(colour, Color4.White, bevelHighlight * 0.55f);
                                highlight.A = colour.A;

                                drawQuad(renderer, x, y0, barWidth, Math.Min(bevelHeight, y1 - y0), ColourInfo.SingleColour(highlight));
                            }
                        }
                    }
                }

                if (showPeaks && peakLineHeight > 0)
                {
                    for (int i = 0; i < pointCount; i++)
                    {
                        float peak = Math.Clamp(peaks[i], 0f, 1f);

                        if (peak <= 0)
                            continue;

                        var peakColour = ColourInfo.SingleColour(neon ? levelColour(peak) : topColour);
                        float y = height - peak * height - peakLineHeight;

                        drawQuad(renderer, i * (barWidth + spacing), y, barWidth, peakLineHeight, peakColour);
                    }
                }

                shader.Unbind();
            }

            private Color4 levelColour(float t)
            {
                var colour = neon ? neonColour(t) : interpolate(bottomColour, topColour, t);

                if (neon)
                    colour.A *= fillAlpha;

                return colour;
            }

            private void drawQuad(IRenderer renderer, float x, float y, float w, float h, ColourInfo colour)
            {
                var quad = new Quad(
                    Vector2Extensions.Transform(new Vector2(x, y), DrawInfo.Matrix),
                    Vector2Extensions.Transform(new Vector2(x + w, y), DrawInfo.Matrix),
                    Vector2Extensions.Transform(new Vector2(x, y + h), DrawInfo.Matrix),
                    Vector2Extensions.Transform(new Vector2(x + w, y + h), DrawInfo.Matrix));

                renderer.DrawQuad(texture, quad, colour, null, vertexBatch!.AddAction);
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
