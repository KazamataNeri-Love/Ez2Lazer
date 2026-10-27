// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Globalization;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Game.Beatmaps;
using osu.Game.Configuration;
using osu.Game.EzOsuGame.Localization;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Overlays;
using osu.Game.Skinning;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.EzOsuGame.Screens.MusicPlayer
{
    /// <summary>
    /// Now playing text block (title / artist / difficulty / BPM / length) usable as a layout editor component.
    /// </summary>
    public partial class EzMusicPlayerInfo : CompositeDrawable, ISerialisableDrawable
    {
        public bool UsesFixedAnchor { get; set; }

        [SettingSource(typeof(EzMusicStrings), nameof(EzMusicStrings.PLAYER_INFO_TEXT_SIZE), null, 0)]
        public BindableNumber<float> TextSize { get; } = new BindableNumber<float>(28)
        {
            MinValue = 8,
            MaxValue = 96,
            Precision = 1,
        };

        [SettingSource(typeof(EzMusicStrings), nameof(EzMusicStrings.PLAYER_INFO_WIDTH), null, 1)]
        public BindableNumber<float> PanelWidth { get; } = new BindableNumber<float>(420)
        {
            MinValue = 120,
            MaxValue = 1200,
            Precision = 1,
        };

        [SettingSource(typeof(EzMusicStrings), nameof(EzMusicStrings.PLAYER_INFO_SHOW_DIFFICULTY), null, 2)]
        public Bindable<bool> ShowDifficulty { get; } = new Bindable<bool>(true);

        [SettingSource(typeof(EzMusicStrings), nameof(EzMusicStrings.PLAYER_INFO_SHOW_BPM), null, 3)]
        public Bindable<bool> ShowBpm { get; } = new Bindable<bool>(true);

        [SettingSource(typeof(EzMusicStrings), nameof(EzMusicStrings.PLAYER_INFO_SHOW_LENGTH), null, 4)]
        public Bindable<bool> ShowLength { get; } = new Bindable<bool>(true);

        private OsuSpriteText titleText = null!;
        private OsuSpriteText artistText = null!;
        private OsuSpriteText metaText = null!;

        public EzMusicPlayerInfo()
        {
            AutoSizeAxes = Axes.Y;
            Width = 420;
        }

        [Resolved]
        private Bindable<WorkingBeatmap> beatmap { get; set; } = null!;

        [Resolved]
        private MusicController musicController { get; set; } = null!;

        [BackgroundDependencyLoader]
        private void load(OsuColour colours)
        {
            InternalChild = new FillFlowContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(2),
                Children = new Drawable[]
                {
                    titleText = new TruncatingSpriteText
                    {
                        RelativeSizeAxes = Axes.X,
                        Font = OsuFont.GetFont(size: 40, weight: FontWeight.Bold),
                    },
                    artistText = new TruncatingSpriteText
                    {
                        RelativeSizeAxes = Axes.X,
                        Font = OsuFont.GetFont(size: 24),
                        Colour = colours.GrayD,
                    },
                    metaText = new TruncatingSpriteText
                    {
                        RelativeSizeAxes = Axes.X,
                        Font = OsuFont.GetFont(size: 18),
                        Colour = new Color4(1f, 0.75f, 0.85f, 1f),
                    },
                },
            };
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            beatmap.BindValueChanged(_ => updateText(), true);
            musicController.TrackChanged += onTrackChanged;

            TextSize.BindValueChanged(_ => applyTextSize(), true);
            PanelWidth.BindValueChanged(w => Width = w.NewValue);
            ShowDifficulty.BindValueChanged(_ => updateText());
            ShowBpm.BindValueChanged(_ => updateText());
            ShowLength.BindValueChanged(_ => updateText());
        }

        private void onTrackChanged(WorkingBeatmap working, TrackChangeDirection direction = TrackChangeDirection.None) => updateText();

        private void applyTextSize()
        {
            float size = TextSize.Value;

            titleText.Font = OsuFont.GetFont(size: size * 1.4f, weight: FontWeight.Bold);
            artistText.Font = OsuFont.GetFont(size: size * 0.85f);
            metaText.Font = OsuFont.GetFont(size: size * 0.65f);
        }

        private void updateText()
        {
            var working = beatmap.Value;

            if (working == null || beatmap.IsDefault)
            {
                titleText.Text = EzMusicStrings.PLAYER_UNTITLED;
                artistText.Text = string.Empty;
                metaText.Text = string.Empty;
                return;
            }

            var metadata = working.Metadata;
            var info = working.BeatmapInfo;

            titleText.Text = string.IsNullOrEmpty(metadata.TitleUnicode) ? metadata.Title : metadata.TitleUnicode;
            artistText.Text = string.IsNullOrEmpty(metadata.ArtistUnicode) ? metadata.Artist : metadata.ArtistUnicode;

            var parts = new System.Collections.Generic.List<string>(3);

            if (ShowDifficulty.Value)
                parts.Add($"{info.Ruleset.Name} · {info.DifficultyName}");

            if (ShowBpm.Value && info.BPM > 0)
                parts.Add($"{info.BPM.ToString("0.##", CultureInfo.InvariantCulture)} BPM");

            if (ShowLength.Value)
            {
                double length = working.Track.Length;

                if (length > 0)
                    parts.Add(TimeSpan.FromMilliseconds(length).ToString(@"m\:ss"));
            }

            metaText.Text = string.Join("  ·  ", parts);
        }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);

            if (musicController != null)
                musicController.TrackChanged -= onTrackChanged;
        }
    }
}
