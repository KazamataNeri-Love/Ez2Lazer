// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using osu.Game.Rulesets.Mania.EzMania.Mods.LAsMods;
using osu.Game.Rulesets.Mods;

namespace osu.Game.Rulesets.Mania.UI
{
    /// <summary>
    /// 是否需要把 playfield 包进投影容器。任一条件成立即建：皮肤编辑器内（为了拖滑块即时生效）、
    /// 皮肤角度非零、Tilted Track 选中且角度非零。游玩时角度为 0 且没有 TT 就不付这层 framebuffer。
    /// </summary>
    public static class Ez3DTiltGate
    {
        public static bool RequiresBuffer(bool editorActive, double angle, IReadOnlyList<Mod>? mods)
            => editorActive || angle > 0 || tiltModActive(mods);

        private static bool tiltModActive(IReadOnlyList<Mod>? mods)
            => mods != null && mods.OfType<ManiaModTiltedTrack>().Any(tilt => tilt.TrackAngle.Value > 0);
    }
}
