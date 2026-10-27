// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using osu.Framework.Graphics.UserInterface;
using osu.Game.Graphics.UserInterface;
using osu.Game.Localisation;
using osu.Game.Overlays.Mods;

namespace osu.Game.EzOsuGame.Overlays
{
    /// <summary>
    /// 置顶栏中的 Mod 面板：保留类别列的不兼容提示，并额外提供取消置顶与顺序调整。
    /// </summary>
    public partial class PinnedModPanel : IncompatibilityDisplayingModPanel
    {
        public PinnedModPanel(ModState modState)
            : base(modState)
        {
        }

        public override MenuItem[] ContextMenuItems
        {
            get
            {
                var items = base.ContextMenuItems.ToList();

                if (PinnedModsController == null)
                    return items.ToArray();

                items.Add(new OsuMenuItem(ModSelectOverlayStrings.PinnedModMoveUp, MenuItemType.Standard, () => PinnedModsController.Move(Mod, -1)));
                items.Add(new OsuMenuItem(ModSelectOverlayStrings.PinnedModMoveDown, MenuItemType.Standard, () => PinnedModsController.Move(Mod, 1)));

                return items.ToArray();
            }
        }
    }
}
