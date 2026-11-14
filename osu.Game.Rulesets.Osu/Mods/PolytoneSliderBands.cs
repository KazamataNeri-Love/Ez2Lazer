// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Framework.Graphics.Containers;
using osu.Game.Rulesets.Osu.Objects.Drawables;
using osuTK;

namespace osu.Game.Rulesets.Osu.Mods
{
    /// <summary>
    /// 承载所有存活滑条的身，按帧标记、按帧回收，避免每条滑条各自长期持有绘制层。
    /// </summary>
    public partial class PolytoneSliderBands : CompositeDrawable
    {
        private readonly Dictionary<DrawableSlider, (PolytoneSliderBand Band, int Frame)> bands = new();
        private readonly List<DrawableSlider> stale = new();

        private int frame;

        public void BeginFrame() => frame++;

        internal void UpdateBand(DrawableSlider slider, ReadOnlySpan<Vector2> centres, ReadOnlySpan<float> halfWidths)
        {
            if (!bands.TryGetValue(slider, out var entry))
            {
                entry = (new PolytoneSliderBand(), frame);
                bands.Add(slider, entry);
                AddInternal(entry.Band);
            }

            bands[slider] = (entry.Band, frame);
            entry.Band.UpdateBand(centres, halfWidths);
        }

        public void EndFrame()
        {
            stale.Clear();

            foreach (var pair in bands)
            {
                if (pair.Value.Frame != frame)
                    stale.Add(pair.Key);
            }

            foreach (var slider in stale)
            {
                RemoveInternal(bands[slider].Band, true);
                bands.Remove(slider);
            }
        }
    }
}
