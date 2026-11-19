// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Bindables;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Localisation;
using osu.Game.Configuration;
using osu.Game.Graphics;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Rulesets.Osu.Objects.Drawables;
using osu.Game.Rulesets.Osu.UI;
using osu.Game.Rulesets.UI;
using osuTK;

namespace osu.Game.Rulesets.Osu.Mods
{
    /// <summary>
    /// 棱锥透视：物件的世界坐标 X/Y 不变，只沿 +Z 推进，
    /// 屏幕上等价于从远端矩形等比放大到近端矩形（击打层），并径向收敛到中央消失点。
    /// </summary>
    public class OsuModPolytone : Mod, IUpdatableByPlayfield, IApplicableToDrawableRuleset<OsuHitObject>
    {
        public override string Name => "Polytone";

        public override string Acronym => "PT";

        public override IconUsage? Icon => OsuIcon.ModDepth;

        public override ModType Type => ModType.NeriMod;

        public override LocalisableString Description => "Notes arrive from the far end of a tunnel.";

        public override Type[] IncompatibleMods => new[]
        {
            // 本 Mod 每帧重写 Position/Scale，与任何自行移动或缩放物件的 Mod 冲突。
            typeof(OsuModDepth),
            typeof(OsuModAutoplay),
            typeof(OsuModMagnetised),
            typeof(OsuModRepel),
            typeof(OsuModFreezeFrame),
            typeof(OsuModWiggle),
            typeof(OsuModTransform),
        };

        [SettingSource("View angle", "Half-angle of the tunnel in degrees. Larger values shrink notes faster with depth.", 0)]
        public BindableFloat ViewAngle { get; } = new BindableFloat(30)
        {
            MinValue = 20,
            MaxValue = 40,
            Precision = 1,
        };

        [SettingSource("Spawn depth", "Distance at which notes appear. Deeper spawns shrink notes more.", 1)]
        public BindableFloat SpawnDepth { get; } = new BindableFloat(1665)
        {
            MinValue = 100,
            MaxValue = 8000,
            Precision = 5,
        };

        [SettingSource("Tunnel opacity", "Opacity of the tunnel boundary drawn behind the notes.", 2)]
        public BindableFloat TunnelOpacity { get; } = new BindableFloat(0.6f)
        {
            MinValue = 0,
            MaxValue = 1,
            Precision = 0.05f,
        };

        private PolytoneTunnel? tunnel;

        private float appliedViewAngle = float.NaN;
        private float appliedSpawnDepth = float.NaN;
        private float appliedTunnelOpacity = float.NaN;

        public void ApplyToDrawableRuleset(DrawableRuleset<OsuHitObject> drawableRuleset)
        {
            if (!(drawableRuleset.Playfield is OsuPlayfield playfield))
                return;

            // follow point 固定在 playfield 坐标上，无法跟随透视，直接隐藏。
            playfield.FollowPoints.Hide();

            playfield.AddBackgroundLayer(tunnel = new PolytoneTunnel());

            applyTunnelGeometry(new PolytoneGeometry(ViewAngle.Value, SpawnDepth.Value), true);
        }

        public void Update(Playfield playfield)
        {
            var geometry = new PolytoneGeometry(ViewAngle.Value, SpawnDepth.Value);

            applyTunnelGeometry(geometry);

            double time = playfield.Time.Current;

            foreach (var entry in playfield.HitObjectContainer.AliveEntries)
            {
                switch (entry.Value)
                {
                    case DrawableHitCircle circle:
                        applyCircleDepth(circle, geometry, time);
                        break;

                    case DrawableSlider slider:
                        applySliderDepth(slider, geometry, time);
                        break;
                }
            }
        }

        private static void applyCircleDepth(DrawableOsuHitObject drawable, in PolytoneGeometry geometry, double time)
        {
            var hitObject = drawable.HitObject;
            double preempt = hitObject.TimePreempt;

            if (preempt <= 0)
                return;

            double appearTime = hitObject.StartTime - preempt;
            float ratio = geometry.RatioAt((time - appearTime) / preempt);

            drawable.Position = geometry.Project(hitObject.StackedPosition, ratio);
            drawable.Scale = new Vector2(ratio);
        }

        private static void applySliderDepth(DrawableSlider slider, in PolytoneGeometry geometry, double time)
        {
            var hitObject = slider.HitObject;
            double preempt = hitObject.TimePreempt;

            if (preempt <= 0)
                return;

            float ratio = geometry.RatioAt((time - (hitObject.StartTime - preempt)) / preempt);

            slider.Position = geometry.Project(hitObject.StackedPosition, ratio);
            slider.Scale = new Vector2(ratio);
        }

        private void applyTunnelGeometry(in PolytoneGeometry geometry, bool force = false)
        {
            if (tunnel == null)
                return;

            float opacity = TunnelOpacity.Value;

            if (!force && appliedViewAngle == ViewAngle.Value && appliedSpawnDepth == SpawnDepth.Value && appliedTunnelOpacity == opacity)
                return;

            appliedViewAngle = ViewAngle.Value;
            appliedSpawnDepth = SpawnDepth.Value;
            appliedTunnelOpacity = opacity;

            tunnel.UpdateGeometry(geometry, opacity);
        }
    }
}
