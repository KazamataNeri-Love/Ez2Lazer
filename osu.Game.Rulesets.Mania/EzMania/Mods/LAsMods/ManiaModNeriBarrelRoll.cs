// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Localisation;
using osu.Framework.Testing;
using osu.Framework.Utils;
using osu.Game.Configuration;
using osu.Game.EzOsuGame.Localization;
using osu.Game.Rulesets.Mania.Objects;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Objects.Drawables;
using osu.Game.Rulesets.Scoring;
using osu.Game.Rulesets.UI;
using osu.Game.Screens.Play;
using osuTK;

namespace osu.Game.Rulesets.Mania.EzMania.Mods.LAsMods
{
    /// <summary>
    /// Mania Barrel Roll — 整个 playfield 绕中心旋转。
    /// 转速由判定结果驱动：非 Miss 累积加速，Miss 以两倍量减速；加速步长设为 0 即为匀速旋转。
    /// </summary>
    /// <remarks>
    /// ManiaPAC 默认 Anchor=Origin=TopLeft，但内部 scalingContainer 是 Centre/Centre，
    /// 因此旋转目标是 scalingContainer 而不是 PAC 本身。
    /// </remarks>
    public class ManiaModNeriBarrelRoll : Mod, IApplicableToDrawableRuleset<ManiaHitObject>,
        IApplicableToPlayer, IApplicableToDrawableHitObject, IUpdatableByPlayfield
    {
        public override string Name => "Barrel Roll";
        public override string Acronym => "BR";
        public override LocalisableString Description => NeriBarrelRollStrings.DESCRIPTION;
        public override ModType Type => ModType.NeriMod;
        public override bool Ranked => false;
        public override bool ValidForMultiplayer => true;
        public override bool ValidForFreestyleAsRequiredMod => false;
        public override Type[] IncompatibleMods => Array.Empty<Type>();

        /// <summary>当前实际旋转角度。</summary>
        private float currentRotation;
        private double lastUpdateTime;

        /// <summary>当前实际转速（每帧向 targetSpinSpeed 平滑靠拢）。</summary>
        private double currentSpinSpeed;

        /// <summary>目标转速（判定结果改变此值）。</summary>
        private double targetSpinSpeed;

        /// <summary>累积速度偏移量（从 BaseSpeed 开始累加）。</summary>
        private double speedOffset;

        [SettingSource(typeof(NeriBarrelRollStrings), nameof(NeriBarrelRollStrings.DIRECTION_LABEL), nameof(NeriBarrelRollStrings.DIRECTION_DESCRIPTION))]
        public Bindable<RotationDirection> Direction { get; } = new Bindable<RotationDirection>();

        [SettingSource(typeof(NeriBarrelRollStrings), nameof(NeriBarrelRollStrings.BASE_SPEED_LABEL), nameof(NeriBarrelRollStrings.BASE_SPEED_DESCRIPTION))]
        public BindableNumber<double> BaseSpeed { get; } = new BindableDouble(0.5)
        {
            MinValue = 0.02,
            MaxValue = 12,
            Precision = 0.01,
        };

        [SettingSource(typeof(NeriBarrelRollStrings), nameof(NeriBarrelRollStrings.MAX_SPEED_LABEL), nameof(NeriBarrelRollStrings.MAX_SPEED_DESCRIPTION))]
        public BindableNumber<double> MaxSpeed { get; } = new BindableDouble(4.0)
        {
            MinValue = 0.5,
            MaxValue = 12,
            Precision = 0.01,
        };

        [SettingSource(typeof(NeriBarrelRollStrings), nameof(NeriBarrelRollStrings.ACCELERATION_LABEL), nameof(NeriBarrelRollStrings.ACCELERATION_DESCRIPTION))]
        public BindableNumber<double> AccelerationStep { get; } = new BindableDouble(0)
        {
            MinValue = 0,
            MaxValue = 1.0,
            Precision = 0.01,
        };

        private readonly IBindable<bool> isBreakTime = new Bindable<bool>();

        private Drawable? rotationTarget;
        private bool setupDone;

        public void ApplyToDrawableRuleset(DrawableRuleset<ManiaHitObject> drawableRuleset)
        {
            speedOffset = 0;
            currentSpinSpeed = BaseSpeed.Value;
            targetSpinSpeed = BaseSpeed.Value;
        }

        public void ApplyToPlayer(Player player)
        {
            isBreakTime.BindTo(player.IsBreakTime);
        }

        public void ApplyToDrawableHitObject(DrawableHitObject d)
        {
            d.OnNewResult += (_, result) =>
            {
                if (!result.Type.AffectsAccuracy())
                    return;

                if (result.IsHit)
                    speedOffset += AccelerationStep.Value;
                else
                    speedOffset -= AccelerationStep.Value * 2;

                targetSpinSpeed = Math.Clamp(BaseSpeed.Value + speedOffset, BaseSpeed.Value, MaxSpeed.Value);
            };
        }

        public void Update(Playfield playfield)
        {
            if (!setupDone)
                performSetup(playfield);

            double currentTime = playfield.Time.Current;

            if (isBreakTime.Value)
            {
                lastUpdateTime = -1;
                return;
            }

            if (lastUpdateTime < 0)
                lastUpdateTime = currentTime;

            currentSpinSpeed = Interpolation.DampContinuously(currentSpinSpeed, targetSpinSpeed, 100, playfield.Clock.ElapsedFrameTime);

            double deltaTime = currentTime - lastUpdateTime;
            lastUpdateTime = currentTime;

            float deltaAngle = (Direction.Value == RotationDirection.Counterclockwise ? -1 : 1)
                               * 360 * (float)(deltaTime / 60000 * currentSpinSpeed);

            currentRotation += deltaAngle;

            if (rotationTarget != null)
                rotationTarget.Rotation = currentRotation;
        }

        private void performSetup(Playfield playfield)
        {
            PlayfieldAdjustmentContainer? pac = null;
            var current = playfield.Parent;

            while (current != null)
            {
                if (current is PlayfieldAdjustmentContainer p)
                {
                    pac = p;
                    break;
                }

                current = current.Parent;
            }

            if (pac == null) return;

            foreach (var child in pac.ChildrenOfType<DrawSizePreservingFillContainer>())
            {
                if (child.Anchor == Anchor.Centre && child.Origin == Anchor.Centre)
                {
                    rotationTarget = child;
                    break;
                }
            }

            // Mania 的 playfield 布局已自洽，不需要基类对 PAC 的额外缩放。
            pac.Scale = Vector2.One;

            setupDone = true;
        }
    }

    public static class NeriBarrelRollStrings
    {
        public static readonly LocalisableString DESCRIPTION = new EzLocalizationManager.EzLocalisableString(
            "整个游戏区域像车轮一样旋转！命中会加速旋转。", "The whole playfield is on a wheel! Hits spin it faster.");

        public static readonly LocalisableString DIRECTION_LABEL = new EzLocalizationManager.EzLocalisableString(
            "旋转方向", "Direction");

        public static readonly LocalisableString DIRECTION_DESCRIPTION = new EzLocalizationManager.EzLocalisableString(
            "顺时针或逆时针旋转。", "The direction of rotation.");

        public static readonly LocalisableString BASE_SPEED_LABEL = new EzLocalizationManager.EzLocalisableString(
            "基础转速", "Base speed");

        public static readonly LocalisableString BASE_SPEED_DESCRIPTION = new EzLocalizationManager.EzLocalisableString(
            "最低转速（RPM）。", "Minimum spin speed (RPM).");

        public static readonly LocalisableString MAX_SPEED_LABEL = new EzLocalizationManager.EzLocalisableString(
            "最高转速", "Max speed");

        public static readonly LocalisableString MAX_SPEED_DESCRIPTION = new EzLocalizationManager.EzLocalisableString(
            "加速后的转速上限（RPM）。", "Maximum spin speed (RPM).");

        public static readonly LocalisableString ACCELERATION_LABEL = new EzLocalizationManager.EzLocalisableString(
            "加速步长", "Acceleration step");

        public static readonly LocalisableString ACCELERATION_DESCRIPTION = new EzLocalizationManager.EzLocalisableString(
            "每次非 Miss 判定增加的转速（RPM），设为 0 则匀速旋转。",
            "Speed increment per non-Miss hit (RPM), 0 keeps a constant speed.");
    }
}
