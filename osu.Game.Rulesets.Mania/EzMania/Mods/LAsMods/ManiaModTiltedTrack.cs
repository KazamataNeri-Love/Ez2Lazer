// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using osu.Framework.Bindables;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Localisation;
using osu.Game.Configuration;
using osu.Game.EzOsuGame.Localization;
using osu.Game.Rulesets.Mania.Objects;
using osu.Game.Rulesets.Mania.UI;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.UI;

namespace osu.Game.Rulesets.Mania.EzMania.Mods.LAsMods
{
    /// <summary>
    /// Projects the mania playfield into a receding track (Malody-style track angle / distance compensation).
    /// Judgement, input and skin resolution are untouched; only the rendered playfield is warped.
    /// </summary>
    /// <remarks>
    /// 舞台斜轨（<c>ManiaPlayfieldLayoutConfig.TiltAngle</c>）存在时优先：本 Mod 只把参数交给皮肤级容器，不再自建容器。
    /// 容器类型不匹配时退回自建，因此即使皮肤级斜轨被改动或移除，本 Mod 仍然单独可用。
    /// </remarks>
    public partial class ManiaModTiltedTrack : Mod, IApplicableToDrawableRuleset<ManiaHitObject>
    {
        public override string Name => "Tilted Track";

        public override string Acronym => "TT";

        public override LocalisableString Description => TiltedTrackStrings.DESCRIPTION;

        public override IconUsage? Icon => FontAwesome.Solid.CompressArrowsAlt;

        public override ModType Type => ModType.NeriMod;

        public override bool Ranked => false;

        public override bool ValidForMultiplayer => true;

        public override bool ValidForFreestyleAsRequiredMod => false;

        [SettingSource(typeof(TiltedTrackStrings), nameof(TiltedTrackStrings.TRACK_ANGLE_LABEL), nameof(TiltedTrackStrings.TRACK_ANGLE_DESCRIPTION))]
        public BindableFloat TrackAngle { get; } = new BindableFloat(30f)
        {
            MinValue = 0,
            MaxValue = 90,
            Precision = 1,
        };

        [SettingSource(typeof(TiltedTrackStrings), nameof(TiltedTrackStrings.COMPENSATION_LABEL), nameof(TiltedTrackStrings.COMPENSATION_DESCRIPTION))]
        public BindableFloat CompensationDistance { get; } = new BindableFloat(0.2f)
        {
            MinValue = 0,
            MaxValue = 1,
            Precision = 0.01f,
        };

        public override IEnumerable<(LocalisableString setting, LocalisableString value)> SettingDescription
        {
            get
            {
                yield return (TiltedTrackStrings.TRACK_ANGLE_LABEL, $"{TrackAngle.Value:0}");
                yield return (TiltedTrackStrings.COMPENSATION_LABEL, $"{CompensationDistance.Value:0.00}");
            }
        }

        public void ApplyToDrawableRuleset(DrawableRuleset<ManiaHitObject> drawableRuleset)
        {
            if (TrackAngle.Value <= 0)
                return;

            // 皮肤级容器已经在场上时只交参数，避免再套一层 framebuffer。
            if (drawableRuleset.PlayfieldAdjustmentContainer is ManiaPlayfieldAdjustmentContainer skinTilt)
            {
                skinTilt.SetModOverride(TrackAngle.Value, CompensationDistance.Value);
                return;
            }

            var playfield = drawableRuleset.Playfield;

            if (playfield.Parent is not Container parent)
                return;

            parent.Remove(playfield, false);

            var tilt = new TiltBufferContainer { Child = playfield };
            tilt.ApplyTilt(TrackAngle.Value, CompensationDistance.Value);

            parent.Add(tilt);
        }
    }

    public static class TiltedTrackStrings
    {
        public static readonly LocalisableString DESCRIPTION = new EzLocalizationManager.EzLocalisableString(
            "面板向后收窄的斜轨（Malody 轨道倾斜）。选中后覆盖皮肤的斜轨设置。", "The track recedes towards the distance. Overrides the skin's tilt settings.");

        public static readonly LocalisableString TRACK_ANGLE_LABEL = new EzLocalizationManager.EzLocalisableString(
            "轨道倾斜角", "Track angle");

        public static readonly LocalisableString TRACK_ANGLE_DESCRIPTION = new EzLocalizationManager.EzLocalisableString(
            "倾斜程度，0 关闭效果。", "How far the track recedes. 0 disables the effect.");

        public static readonly LocalisableString COMPENSATION_LABEL = new EzLocalizationManager.EzLocalisableString(
            "距离补偿", "Distance compensation");

        public static readonly LocalisableString COMPENSATION_DESCRIPTION = new EzLocalizationManager.EzLocalisableString(
            "削弱远端的间距压缩，保留形状收窄。", "Reduces the spacing compression at the far end.");
    }
}
