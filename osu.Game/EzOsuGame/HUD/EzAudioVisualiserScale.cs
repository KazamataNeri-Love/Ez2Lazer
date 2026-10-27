// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Game.Configuration;
using osu.Game.EzOsuGame.Configuration;
using osu.Game.EzOsuGame.Localization;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Skinning;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.EzOsuGame.HUD
{
    /// <summary>
    /// Frequency scale labels (83 Hz … 16 kHz) which can be aligned beneath <see cref="EzAudioVisualiser"/>.
    /// Bar geometry settings must match the visualiser so the tick positions line up.
    /// </summary>
    public partial class EzAudioVisualiserScale : CompositeDrawable, ISerialisableDrawable
    {
        /// <summary>
        /// Approximate frequency range covered by the amplitude bins exposed by the beat sync provider
        /// (bin 0.5 … 255 at a 44.1 kHz sample rate).
        /// </summary>
        private const float min_frequency = 43f;

        private const float max_frequency = 22000f;

        private const float tick_height = 5f;
        private const float bar_spacing_ratio = 0.2f;

        private static readonly float[] tick_frequencies = { 83f, 125f, 250f, 500f, 1000f, 2000f, 4000f, 8000f, 16000f, 20000f };

        public bool UsesFixedAnchor { get; set; }

        [SettingSource(typeof(EzHUDStrings), nameof(EzHUDStrings.AUDIO_SCALE_BAR_COUNT_LABEL), nameof(EzHUDStrings.AUDIO_SCALE_BAR_COUNT_DESCRIPTION), 0)]
        public BindableNumber<int> BarCount { get; } = new BindableNumber<int>(64)
        {
            MinValue = 4,
            MaxValue = 256,
            Precision = 1,
        };

        [SettingSource(typeof(EzHUDStrings), nameof(EzHUDStrings.AUDIO_SCALE_BAR_WIDTH_LABEL), nameof(EzHUDStrings.AUDIO_SCALE_BAR_WIDTH_DESCRIPTION), 1)]
        public BindableNumber<float> BarWidth { get; } = new BindableNumber<float>(8)
        {
            MinValue = 2,
            MaxValue = 64,
            Precision = 1,
        };

        [SettingSource(typeof(EzHUDStrings), nameof(EzHUDStrings.AUDIO_SCALE_BAR_SPACING_LABEL), nameof(EzHUDStrings.AUDIO_SCALE_BAR_SPACING_DESCRIPTION), 2)]
        public BindableNumber<float> BarSpacing { get; } = new BindableNumber<float>(1.6f)
        {
            MinValue = 0,
            MaxValue = 32,
            Precision = 0.1f,
        };

        [SettingSource(typeof(EzHUDStrings), nameof(EzHUDStrings.AUDIO_SCALE_EDGE_FADE_LABEL), nameof(EzHUDStrings.AUDIO_SCALE_EDGE_FADE_DESCRIPTION), 3)]
        public BindableNumber<float> EdgeFade { get; } = new BindableNumber<float>(0.05f)
        {
            MinValue = 0,
            MaxValue = 0.25f,
            Precision = 0.01f,
        };

        [SettingSource(typeof(EzHUDStrings), nameof(EzHUDStrings.AUDIO_SCALE_FREQUENCY_SCALE_LABEL), nameof(EzHUDStrings.AUDIO_SCALE_FREQUENCY_SCALE_DESCRIPTION), 4)]
        public Bindable<EzAudioVisualiserFrequencyScale> FrequencyScale { get; } = new Bindable<EzAudioVisualiserFrequencyScale>(EzAudioVisualiserFrequencyScale.Logarithmic);

        [SettingSource(typeof(EzHUDStrings), nameof(EzHUDStrings.AUDIO_SCALE_LABEL_SIZE_LABEL), nameof(EzHUDStrings.AUDIO_SCALE_LABEL_SIZE_DESCRIPTION), 5)]
        public BindableNumber<float> LabelSize { get; } = new BindableNumber<float>(12)
        {
            MinValue = 6,
            MaxValue = 32,
            Precision = 1,
        };

        [SettingSource(typeof(EzHUDStrings), nameof(EzHUDStrings.AUDIO_SCALE_COLOUR_LABEL), nameof(EzHUDStrings.AUDIO_SCALE_COLOUR_DESCRIPTION), 6)]
        public BindableColour4 TickColour { get; } = new BindableColour4(new Color4(0.78f, 0.8f, 0.84f, 0.9f));

        [SettingSource(typeof(EzHUDStrings), nameof(EzHUDStrings.AUDIO_SCALE_FOLLOW_LABEL), nameof(EzHUDStrings.AUDIO_SCALE_FOLLOW_DESCRIPTION), 7)]
        public Bindable<bool> FollowVisualiser { get; } = new Bindable<bool>(true);

        private Container items = null!;
        private EzAudioVisualiser? followedVisualiser;

        [BackgroundDependencyLoader]
        private void load()
        {
            InternalChild = items = new Container
            {
                RelativeSizeAxes = Axes.Both,
            };
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            BarCount.BindValueChanged(_ => rebuild());
            BarWidth.BindValueChanged(_ => rebuild());
            BarSpacing.BindValueChanged(_ => rebuild());
            EdgeFade.BindValueChanged(_ => rebuild());
            FrequencyScale.BindValueChanged(_ => rebuild());
            LabelSize.BindValueChanged(_ => rebuild());
            TickColour.BindValueChanged(_ => rebuild());
            FollowVisualiser.BindValueChanged(follow =>
            {
                if (follow.NewValue)
                    Scheduler.AddOnce(tryFollowVisualiser);
            });

            rebuild();
            Scheduler.AddOnce(tryFollowVisualiser);
        }

        private void tryFollowVisualiser()
        {
            if (!FollowVisualiser.Value || followedVisualiser != null)
                return;

            followedVisualiser = (Parent as Container)?.OfType<EzAudioVisualiser>().FirstOrDefault();

            if (followedVisualiser == null)
                return;

            BarCount.BindTo(followedVisualiser.BarCount);
            BarWidth.BindTo(followedVisualiser.BarWidth);
            EdgeFade.BindTo(followedVisualiser.EdgeFade);
            FrequencyScale.BindTo(followedVisualiser.FrequencyScale);

            followedVisualiser.BarWidth.BindValueChanged(w => BarSpacing.Value = w.NewValue * bar_spacing_ratio, true);
        }

        private void rebuild()
        {
            int count = Math.Max(1, BarCount.Value);
            float barWidth = BarWidth.Value;
            float spacing = BarSpacing.Value;
            int edgePad = EdgeFade.Value <= 0 ? 0 : (int)MathF.Round(EdgeFade.Value * count);
            int total = count + edgePad * 2;

            float width = total * barWidth + Math.Max(0, total - 1) * spacing;
            float labelHeight = LabelSize.Value * 1.35f;

            Size = new Vector2(width, tick_height + 2 + labelHeight);

            items.Clear();

            foreach (float frequency in tick_frequencies)
            {
                float position = getFrequencyPosition(frequency);

                if (position <= 0 || position >= 1)
                    continue;

                float x = (edgePad + position * count) * (barWidth + spacing);

                items.Add(new FillFlowContainer
                {
                    Anchor = Anchor.TopLeft,
                    Origin = Anchor.TopCentre,
                    Position = new Vector2(x, 0),
                    AutoSizeAxes = Axes.Both,
                    Direction = FillDirection.Vertical,
                    Spacing = new Vector2(0, 1),
                    Children = new Drawable[]
                    {
                        new Box
                        {
                            Anchor = Anchor.TopCentre,
                            Origin = Anchor.TopCentre,
                            Width = 1,
                            Height = tick_height,
                            Colour = TickColour.Value,
                        },
                        new OsuSpriteText
                        {
                            Anchor = Anchor.TopCentre,
                            Origin = Anchor.TopCentre,
                            Text = formatFrequency(frequency),
                            Font = OsuFont.GetFont(size: LabelSize.Value),
                            Colour = TickColour.Value,
                        },
                    },
                });
            }
        }

        private float getFrequencyPosition(float frequency)
        {
            if (FrequencyScale.Value == EzAudioVisualiserFrequencyScale.Logarithmic)
            {
                double logMin = Math.Log(min_frequency);
                double logMax = Math.Log(max_frequency);

                return (float)((Math.Log(frequency) - logMin) / (logMax - logMin));
            }

            return (frequency - min_frequency) / (max_frequency - min_frequency);
        }

        private static string formatFrequency(float frequency)
            => frequency >= 1000 ? $"{frequency / 1000:0.#}k" : $"{frequency:0}";
    }
}
