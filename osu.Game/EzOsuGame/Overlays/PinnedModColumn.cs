// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Localisation;
using osu.Game.Overlays.Mods;
using osu.Game.Rulesets.Mods;

namespace osu.Game.EzOsuGame.Overlays
{
    /// <summary>
    /// 置顶栏：按 <see cref="EzPinnedModsController.Keys"/> 的顺序展示已置顶的 Mod，面板与类别列共享同一 <see cref="ModState"/>。
    /// </summary>
    public partial class PinnedModColumn : ModSelectColumn
    {
        private IReadOnlyList<ModState> availableMods = Array.Empty<ModState>();
        private CancellationTokenSource? cancellationTokenSource;
        private OsuSpriteText? emptyHint;

        [Resolved]
        private OsuColour colours { get; set; } = null!;

        public PinnedModColumn()
        {
            HeaderText = ModSelectOverlayStrings.PinnedMods;
        }

        public IReadOnlyList<ModState> AvailableMods
        {
            get => availableMods;
            set
            {
                availableMods = value;

                if (IsLoaded)
                    asyncLoadPanels();
            }
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            AccentColour = colours.Purple1;
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            asyncLoadPanels();
        }

        protected virtual ModPanel CreateModPanel(ModState modState) => new PinnedModPanel(modState);

        private void asyncLoadPanels()
        {
            cancellationTokenSource?.Cancel();

            if (availableMods.Count == 0)
            {
                ItemsFlow.ChildrenEnumerable = new Drawable[] { emptyHint ??= createEmptyHint() };
                return;
            }

            var panels = availableMods.Select(mod => CreateModPanel(mod)).ToArray();

            LoadComponentsAsync(panels, loaded => ItemsFlow.ChildrenEnumerable = loaded, (cancellationTokenSource = new CancellationTokenSource()).Token);
        }

        private static OsuSpriteText createEmptyHint() => new OsuSpriteText
        {
            Text = ModSelectOverlayStrings.PinnedModsHint,
            Font = OsuFont.Default.With(size: 13),
            Colour = Colour4.White.Opacity(0.45f),
            Margin = new MarginPadding { Horizontal = 8, Vertical = 6 },
        };
    }
}
