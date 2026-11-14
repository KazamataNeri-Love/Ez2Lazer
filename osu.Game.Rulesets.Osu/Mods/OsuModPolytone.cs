// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using osu.Framework.Bindables;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Localisation;
using osu.Game.Configuration;
using osu.Game.Graphics;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Objects.Drawables;
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
    public class OsuModPolytone : Mod, IUpdatableByPlayfield, IApplicableToDrawableRuleset<OsuHitObject>, IApplicableToDrawableHitObject
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
        public BindableFloat SpawnDepth { get; } = new BindableFloat(350)
        {
            MinValue = 100,
            MaxValue = 1200,
            Precision = 10,
        };

        [SettingSource("Perspective blend", "0 = constant speed approach, 1 = projected approach that accelerates into the hit plane.", 2)]
        public BindableFloat PerspectiveBlend { get; } = new BindableFloat(0.85f)
        {
            MinValue = 0,
            MaxValue = 1,
            Precision = 0.05f,
        };

        [SettingSource("Tunnel opacity", "Opacity of the tunnel boundary drawn behind the notes.", 3)]
        public BindableFloat TunnelOpacity { get; } = new BindableFloat(0.6f)
        {
            MinValue = 0,
            MaxValue = 1,
            Precision = 0.05f,
        };

        private PolytoneTunnel? tunnel;
        private PolytoneSliderBands? bands;

        private readonly Vector2[] bandCentres = new Vector2[PolytoneSliderBand.SAMPLE_COUNT];
        private readonly float[] bandHalfWidths = new float[PolytoneSliderBand.SAMPLE_COUNT];

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
            playfield.AddBackgroundLayer(bands = new PolytoneSliderBands());

            applyTunnelGeometry(new PolytoneGeometry(ViewAngle.Value, SpawnDepth.Value), true);
        }

        public void ApplyToDrawableHitObject(DrawableHitObject drawable)
        {
            if (!(drawable is DrawableSlider slider))
                return;

            // skin 身由本 Mod 自绘代替；每次状态刷新后重新压掉，避免被 FadeIn 重新点亮。
            drawable.ApplyCustomUpdateState += (_, _) => hideSliderBody(slider);
        }

        private static void hideSliderBody(DrawableSlider slider)
        {
            // 折返滑条走整体缩放降级，保留 skin 身。
            if (slider.HitObject.RepeatCount > 0)
                return;

            slider.Body.ClearTransforms();
            slider.Body.Alpha = 0;
        }

        public void Update(Playfield playfield)
        {
            var geometry = new PolytoneGeometry(ViewAngle.Value, SpawnDepth.Value);
            float blend = PerspectiveBlend.Value;

            applyTunnelGeometry(geometry);

            double time = playfield.Time.Current;

            bands?.BeginFrame();

            foreach (var entry in playfield.HitObjectContainer.AliveEntries)
            {
                switch (entry.Value)
                {
                    case DrawableHitCircle circle:
                        applyCircleDepth(circle, geometry, blend, time);
                        break;

                    case DrawableSlider slider:
                        applySliderDepth(playfield, slider, geometry, blend, time);
                        break;
                }
            }

            bands?.EndFrame();
        }

        private static void applyCircleDepth(DrawableOsuHitObject drawable, in PolytoneGeometry geometry, float blend, double time)
        {
            var hitObject = drawable.HitObject;
            double preempt = hitObject.TimePreempt;

            if (preempt <= 0)
                return;

            double appearTime = hitObject.StartTime - preempt;
            float ratio = geometry.RatioAt((time - appearTime) / preempt, blend);

            drawable.Position = geometry.Project(hitObject.StackedPosition, ratio);
            drawable.Scale = new Vector2(ratio);
        }

        private void applySliderDepth(Playfield playfield, DrawableSlider slider, in PolytoneGeometry geometry, float blend, double time)
        {
            var hitObject = slider.HitObject;
            double preempt = hitObject.TimePreempt;

            if (preempt <= 0)
                return;

            // 折返滑条：蛇形揭示与锥形身互相打架，退回整体缩放。
            if (hitObject.RepeatCount > 0)
            {
                float ratio = geometry.RatioAt((time - (hitObject.StartTime - preempt)) / preempt, blend);
                slider.Position = geometry.Project(hitObject.StackedPosition, ratio);
                slider.Scale = new Vector2(ratio);
                return;
            }

            // 球所在处恒为命中面（比例 1），所以父级保持自然变换 —— 跟踪判定因此与视觉天然一致。
            slider.Position = hitObject.StackedPosition;
            slider.Scale = Vector2.One;
            slider.Body.Alpha = 0;

            foreach (var nested in slider.NestedHitObjects.OfType<DrawableOsuHitObject>())
                applyNestedDepth(playfield, slider, nested, geometry, blend, time);

            updateBand(slider, geometry, blend, time);
        }

        private static void applyNestedDepth(Playfield playfield, DrawableSlider slider, DrawableOsuHitObject nested, in PolytoneGeometry geometry, float blend, double time)
        {
            var hitObject = nested.HitObject;
            double preempt = hitObject.TimePreempt;

            if (preempt <= 0)
                return;

            float ratio = geometry.RatioAt((time - (hitObject.StartTime - preempt)) / preempt, blend);

            double sliderDuration = slider.HitObject.Duration;
            double progress = sliderDuration <= 0 ? 0 : Math.Clamp((hitObject.StartTime - slider.HitObject.StartTime) / sliderDuration, 0, 1);

            Vector2 targetScreen = playfield.ToScreenSpace(geometry.Project(slider.HitObject.StackedPositionAt(progress), ratio));
            Vector2 currentScreen = nested.ScreenSpaceDrawQuad.Centre;

            nested.Position += slider.ToLocalSpace(targetScreen) - slider.ToLocalSpace(currentScreen);
            nested.Scale = new Vector2(ratio);
        }

        private void updateBand(DrawableSlider slider, in PolytoneGeometry geometry, float blend, double time)
        {
            if (bands == null)
                return;

            var hitObject = slider.HitObject;
            double preempt = hitObject.TimePreempt;
            double duration = hitObject.Duration;

            if (preempt <= 0 || duration <= 0)
                return;

            // 按时间采样：每个路径点的深度由它自己的命中时刻决定，近端恒为 1、远端收到出生比例。
            for (int i = 0; i < PolytoneSliderBand.SAMPLE_COUNT; i++)
            {
                double progress = (double)i / (PolytoneSliderBand.SAMPLE_COUNT - 1);
                double timeAtSample = hitObject.StartTime + duration * progress;
                float ratio = geometry.RatioAt((time - (timeAtSample - preempt)) / preempt, blend);

                bandCentres[i] = geometry.Project(hitObject.StackedPositionAt(progress), ratio);
                bandHalfWidths[i] = (float)(hitObject.Radius * ratio);
            }

            bands.UpdateBand(slider, bandCentres, bandHalfWidths);
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
