// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Game.EzOsuGame.Configuration;
using osuTK;

namespace osu.Game.EzOsuGame.Screens.Menu
{
    public partial class EzMenuLogoSunStripes : CompositeDrawable
    {
        private const int max_bands = 10;
        private const float top_end = 0.44f;
        private const float lower_height = 0.56f;
        private const int stripe_count = 6;
        private const float min_gap_width = 0.008f;
        private const float bridge_overlap = 0.0015f;

        private readonly Box[] bands = new Box[max_bands];
        private readonly Box birthBridge;
        private Bindable<EzLogoSunStripesStyle>? style;
        private Bindable<EzLogoBackdropStyle>? backdropStyle;
        private Bindable<Colour4>? solidColour;
        private Bindable<Colour4>? topColour;
        private Bindable<Colour4>? bottomColour;
        private EzLogoSunStripesStyle appliedStyle;
        private Colour4 appliedTop;
        private Colour4 appliedBottom;
        private float scrollPhase;

        public EzMenuLogoSunStripes()
        {
            RelativeSizeAxes = Axes.Both;
            var children = new Drawable[max_bands + 1];

            for (int i = 0; i < max_bands; i++)
                children[i] = bands[i] = createBand();

            birthBridge = createBand();
            birthBridge.Depth = 1;
            children[max_bands] = birthBridge;
            InternalChildren = children;
        }

        [BackgroundDependencyLoader(true)]
        private void load(Ez2ConfigManager? ezConfig)
        {
            if (ezConfig == null)
                return;

            style = ezConfig.GetBindable<EzLogoSunStripesStyle>(Ez2Setting.MenuLogoSunStripesStyle);
            backdropStyle = ezConfig.GetBindable<EzLogoBackdropStyle>(Ez2Setting.MenuLogoBackdropStyle);
            solidColour = ezConfig.GetBindable<Colour4>(Ez2Setting.MenuLogoBackdropColour);
            topColour = ezConfig.GetBindable<Colour4>(Ez2Setting.MenuLogoBackdropColourTop);
            bottomColour = ezConfig.GetBindable<Colour4>(Ez2Setting.MenuLogoBackdropColourBottom);
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            if (style == null || backdropStyle == null || solidColour == null || topColour == null || bottomColour == null)
                return;

            style.BindValueChanged(_ => refresh(), true);
            backdropStyle.BindValueChanged(_ => refresh());
            solidColour.BindValueChanged(_ => refresh());
            topColour.BindValueChanged(_ => refresh());
            bottomColour.BindValueChanged(_ => refresh());
        }

        protected override void Update()
        {
            base.Update();

            var currentStyle = style?.Value;
            if (currentStyle is not (EzLogoSunStripesStyle.Scrolling or EzLogoSunStripesStyle.ScrollingUp) || !isEffectivelyVisible)
                return;

            float delta = (float)(Time.Elapsed / 1000.0) * 0.25f;
            scrollPhase = AdvancePhase(scrollPhase, delta, currentStyle.Value);
            refresh();
        }

        /// <summary>
        /// Advances the stripe phase in the selected scrolling direction.
        /// </summary>
        public static float AdvancePhase(float phase, float delta, EzLogoSunStripesStyle style)
        {
            phase = (phase + (style == EzLogoSunStripesStyle.ScrollingUp ? -delta : delta)) % 1f;
            if (phase < 0)
                phase += 1f;

            return phase;
        }

        /// <summary>
        /// Returns the opacity of the bridge over the first scrolling gap.
        /// </summary>
        public static float ComputeBirthBridgeAlpha(float phase)
        {
            float t = Math.Clamp(phase, 0f, 1f);
            float smoothT = t * t * (3f - 2f * t);
            return 1f - smoothT;
        }

        /// <summary>
        /// Calculates the visible unit-height intervals for a stripe animation phase.
        /// </summary>
        /// <param name="phase">A repeating phase in the range from zero to one.</param>
        /// <param name="output">Storage for at least ten interval pairs.</param>
        /// <returns>The number of intervals written.</returns>
        public static int ComputeBands(float phase, Span<(float Start, float End)> output)
        {
            if (output.Length < max_bands)
                throw new ArgumentException("Output must hold ten stripe bands.", nameof(output));

            phase -= MathF.Floor(phase);
            int bandCount = 0;
            float cursor = 0;

            for (int i = 0; i < stripe_count; i++)
            {
                float u = (i + phase) / stripe_count;
                float gapStart = top_end + lower_height * MathF.Pow(u, 1.45f);
                float gapWidth = min_gap_width + lower_height * 0.115f * MathF.Pow(u, 1.35f);
                float gapEnd = Math.Min(1, gapStart + gapWidth);

                if (gapEnd <= gapStart)
                    continue;

                if (gapStart > cursor)
                    output[bandCount++] = (cursor, gapStart);

                cursor = Math.Max(cursor, gapEnd);
            }

            if (cursor < 1)
                output[bandCount++] = (cursor, 1);

            return bandCount;
        }

        private void refresh()
        {
            if (style == null || backdropStyle == null || solidColour == null || topColour == null || bottomColour == null)
                return;

            if (style.Value == EzLogoSunStripesStyle.Off)
            {
                for (int i = 0; i < bands.Length; i++)
                    bands[i].Alpha = 0;
                birthBridge.Alpha = 0;
                appliedStyle = style.Value;
                return;
            }

            Colour4 top = backdropStyle.Value == EzLogoBackdropStyle.Solid ? solidColour.Value : topColour.Value;
            Colour4 bottom = backdropStyle.Value == EzLogoBackdropStyle.Solid ? solidColour.Value.Darken(0.2f) : bottomColour.Value;

            if (style.Value == EzLogoSunStripesStyle.Static
                && appliedStyle == style.Value
                && appliedTop == top
                && appliedBottom == bottom)
                return;

            bool scrolling = style.Value is EzLogoSunStripesStyle.Scrolling or EzLogoSunStripesStyle.ScrollingUp;
            Span<(float Start, float End)> geometry = stackalloc (float Start, float End)[max_bands];
            int count = ComputeBands(scrolling ? scrollPhase : 0, geometry);
            ColourInfo gradient = ColourInfo.GradientVertical(top, bottom);

            for (int i = 0; i < bands.Length; i++)
            {
                if (i >= count || geometry[i].End <= geometry[i].Start)
                {
                    bands[i].Alpha = 0;
                    continue;
                }

                setBand(bands[i], geometry[i].Start, geometry[i].End, gradient);
                bands[i].Alpha = 1;
            }

            if (scrolling)
            {
                // Tuck the bridge under both solid bands so fractional edges cannot leave a seam.
                float bridgeStart = Math.Max(0, geometry[0].End - bridge_overlap);
                float bridgeEnd = Math.Min(1, geometry[1].Start + bridge_overlap);
                setBand(birthBridge, bridgeStart, bridgeEnd, gradient);
                birthBridge.Alpha = ComputeBirthBridgeAlpha(scrollPhase);
            }
            else
                birthBridge.Alpha = 0;

            appliedStyle = style.Value;
            appliedTop = top;
            appliedBottom = bottom;
        }

        private static Box createBand() => new Box
        {
            RelativeSizeAxes = Axes.Both,
            RelativePositionAxes = Axes.Y,
            Width = 1,
            Alpha = 0,
        };

        private static void setBand(Box band, float start, float end, ColourInfo gradient)
        {
            band.Y = start;
            band.Height = end - start;
            band.Colour = ColourInfo.GradientVertical(gradient.Interpolate(new Vector2(0, start)), gradient.Interpolate(new Vector2(0, end)));
        }

        private bool isEffectivelyVisible
        {
            get
            {
                for (Drawable? drawable = this; drawable != null; drawable = drawable.Parent)
                {
                    if (!drawable.IsPresent || drawable.Alpha <= 0)
                        return false;
                }

                return true;
            }
        }
    }
}
