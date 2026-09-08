// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Game.EzOsuGame.Localization;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Overlays;
using osuTK;

namespace osu.Game.EzOsuGame.LocalProfile
{
    /// <summary>
    /// Mania play-count bars grouped by key mode (bar portion of former <see cref="EzLocalProfileManiaOverview"/>).
    /// 常驻显示白名单键数档位（含 0 次），行高/间距/标签宽与星级条状（<see cref="EzLocalProfileBucketBars"/>）一致以便两卡对齐。
    /// </summary>
    public partial class EzLocalProfileManiaKeyPlayBars : FillFlowContainer
    {
        public EzLocalProfileManiaKeyPlayBars(IReadOnlyList<EzLocalProfileManiaKeyStats> keyStats)
        {
            RelativeSizeAxes = Axes.X;
            AutoSizeAxes = Axes.Y;
            Direction = FillDirection.Vertical;
            Spacing = new Vector2(0, 8); // 与星级条状一致

            var byKey = keyStats.ToDictionary(k => k.KeyCount);

            // 常驻白名单全部档位（4k…18k，含 0 次）。
            var displayedKeys = EzLocalProfileConstants.MANIA_DISPLAYED_KEY_COUNTS.OrderBy(k => k).ToList();

            int maxPlays = displayedKeys.Max(k => byKey.TryGetValue(k, out var s) ? s.ScoreCount : 0);

            foreach (int keyCount in displayedKeys)
            {
                int plays = byKey.TryGetValue(keyCount, out var key) ? key.ScoreCount : 0;
                float ratio = maxPlays <= 0 ? 0 : (float)plays / maxPlays;
                Add(new KeyPlayBarRow($"{keyCount}K", plays, ratio));
            }
        }

        private partial class KeyPlayBarRow : Container
        {
            public KeyPlayBarRow(string label, int plays, float ratio)
            {
                RelativeSizeAxes = Axes.X;
                Height = 22;
                Padding = new MarginPadding { Horizontal = 4 };

                Children = new Drawable[]
                {
                    new OsuSpriteText
                    {
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                        Text = label,
                        Font = OsuFont.GetFont(size: 12, weight: FontWeight.Bold),
                        Width = 72,
                    },
                    new Container
                    {
                        RelativeSizeAxes = Axes.Both,
                        Padding = new MarginPadding { Left = 80, Right = 56 },
                        Child = new EzLocalProfileRoundedBar(ratio),
                    },
                    new CountText(plays),
                };
            }

            private partial class CountText : OsuSpriteText
            {
                public CountText(int plays)
                {
                    Anchor = Anchor.CentreRight;
                    Origin = Anchor.CentreRight;
                    Text = plays.ToString("N0");
                    Font = OsuFont.GetFont(size: 12, weight: FontWeight.Bold);
                }

                [BackgroundDependencyLoader]
                private void load(OverlayColourProvider colours) => Colour = colours.Content2;
            }
        }
    }
}
