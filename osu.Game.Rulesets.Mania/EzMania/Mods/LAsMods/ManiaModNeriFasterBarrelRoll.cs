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
    /// Mania Faster Barrel Roll — 转速由判定结果驱动。
    /// 非 Miss 判定累积加速，Miss 则以 2 倍量减速。
    /// 转速变化通过平滑插值实现，避免突变。
    /// </summary>
    public class ManiaModNeriFasterBarrelRoll : Mod, IApplicableToDrawableRuleset<ManiaHitObject>,
        IApplicableToPlayer, IApplicableToDrawableHitObject, IUpdatableByPlayfield
    {
        public override string Name => "Mania Faster Barrel Roll";
        public override string Acronym => "MFBR";
        public override LocalisableString Description => "The playfield spins faster as you play!";
        public override ModType Type => ModType.NeriMod;
        public override bool Ranked => false;
        public override bool ValidForMultiplayer => true;
        public override bool ValidForFreestyleAsRequiredMod => false;
        public override double ScoreMultiplier => 1;
        public override Type[] IncompatibleMods => Array.Empty<Type>();

        /// <summary>
        /// 当前实际旋转角度。
        /// </summary>
        private float currentRotation;
        private double lastUpdateTime;

        /// <summary>
        /// 当前实际转速（每帧向 targetSpinSpeed 平滑靠拢）。
        /// </summary>
        private double currentSpinSpeed;

        /// <summary>
        /// 目标转速（判定结果改变此值）。
        /// </summary>
        private double targetSpinSpeed;

        /// <summary>
        /// 累积速度偏移量（从 BaseSpeed 开始累加）。
        /// </summary>
        private double speedOffset;

        // ─── 设置参数 ───────────────────────────────────────────

        [SettingSource("Direction", "The direction of rotation")]
        public Bindable<RotationDirection> Direction { get; } = new Bindable<RotationDirection>();

        [SettingSource("Base speed", "Minimum spin speed (RPM)")]
        public BindableNumber<double> BaseSpeed { get; } = new BindableDouble(0.5)
        {
            MinValue = 0.02,
            MaxValue = 12,
            Precision = 0.01,
        };

        [SettingSource("Max speed", "Maximum spin speed (RPM)")]
        public BindableNumber<double> MaxSpeed { get; } = new BindableDouble(4.0)
        {
            MinValue = 0.5,
            MaxValue = 12,
            Precision = 0.01,
        };

        [SettingSource("Acceleration step", "Speed increment per non-Miss hit (RPM)")]
        public BindableNumber<double> AccelerationStep { get; } = new BindableDouble(0.05)
        {
            MinValue = 0.01,
            MaxValue = 1.0,
            Precision = 0.01,
        };

        // ─── 运行时状态 ─────────────────────────────────────────

        private readonly IBindable<bool> isBreakTime = new Bindable<bool>();

        private Drawable? rotationTarget;
        private bool setupDone;

        // ─── 接口实现 ───────────────────────────────────────────

        public void ApplyToDrawableRuleset(DrawableRuleset<ManiaHitObject> drawableRuleset)
        {
            // 初始化转速
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
            // 判定监听 — 每当有判定结果时更新目标转速
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

            // ── 平滑插值 ──
            currentSpinSpeed = Interpolation.DampContinuously(currentSpinSpeed, targetSpinSpeed, 100, playfield.Clock.ElapsedFrameTime);

            // ── 增量累计旋转角度 ──
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
            // 沿父链向上找到 PlayfieldAdjustmentContainer
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

            // ManiaPAC 构造: InternalChild = scalingContainer (Centre/Centre)
            foreach (var child in pac.ChildrenOfType<DrawSizePreservingFillContainer>())
            {
                if (child.Anchor == Anchor.Centre && child.Origin == Anchor.Centre)
                {
                    rotationTarget = child;
                    break;
                }
            }

            // 撤销基类的缩放（Mania 布局已自洽）
            pac.Scale = Vector2.One;

            setupDone = true;
        }
    }
}
