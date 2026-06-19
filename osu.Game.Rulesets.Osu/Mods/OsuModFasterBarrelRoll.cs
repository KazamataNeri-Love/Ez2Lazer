// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Diagnostics;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Localisation;
using osu.Framework.Utils;
using osu.Game.Configuration;
using osu.Game.Graphics;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Objects.Drawables;
using osu.Game.Rulesets.Scoring;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Rulesets.Osu.Objects.Drawables;
using osu.Game.Rulesets.Osu.UI;
using osu.Game.Rulesets.UI;
using osu.Game.Screens.Play;
using osuTK;

namespace osu.Game.Rulesets.Osu.Mods
{
    /// <summary>
    /// Faster Barrel Roll — 转速由判定结果驱动。
    /// 非 Miss 判定累积加速，Miss 则以 2 倍量减速。
    /// 转速变化通过平滑插值实现，避免突变。
    /// </summary>
    public class OsuModFasterBarrelRoll : Mod, IApplicableToDrawableRuleset<OsuHitObject>,
        IApplicableToPlayer, IApplicableToDrawableHitObject, IUpdatableByPlayfield
    {
        public override string Name => "Faster Barrel Roll";
        public override string Acronym => "FBR";
        public override IconUsage? Icon => OsuIcon.ModBarrelRoll;
        public override ModType Type => ModType.NeriMod;
        public override LocalisableString Description => "The playfield spins faster as your combo grows!";
        public override double ScoreMultiplier => 1;
        public override Type[] IncompatibleMods => new[] { typeof(OsuModBubbles) };

        /// <summary>
        /// 当前实际旋转角度（供子物体反向旋转用）。
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
        private PlayfieldAdjustmentContainer playfieldAdjustmentContainer = null!;

        // ─── 接口实现 ───────────────────────────────────────────

        public void ApplyToDrawableRuleset(DrawableRuleset<OsuHitObject> drawableRuleset)
        {
            // 与 ModBarrelRoll 相同的缩放逻辑：保证旋转时所有元素在可见区域内
            var playfieldSize = drawableRuleset.Playfield.DrawSize;
            float minSide = MathF.Min(playfieldSize.X, playfieldSize.Y);
            float maxSide = MathF.Max(playfieldSize.X, playfieldSize.Y);

            playfieldAdjustmentContainer = drawableRuleset.PlayfieldAdjustmentContainer;
            playfieldAdjustmentContainer.Scale = new Vector2(minSide / maxSide);

            // 初始化
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
                // 只处理影响准确率的判定（忽略 slider ticks / ignore 等）
                if (!result.Type.AffectsAccuracy())
                    return;

                if (result.IsHit)
                {
                    // 非 Miss：累加加速步长
                    speedOffset += AccelerationStep.Value;
                }
                else
                {
                    // Miss：减去 2 倍加速步长（减速效果更强）
                    speedOffset -= AccelerationStep.Value * 2;
                }

                // 约束目标转速在 [BaseSpeed, MaxSpeed] 之间
                targetSpinSpeed = Math.Clamp(BaseSpeed.Value + speedOffset, BaseSpeed.Value, MaxSpeed.Value);
            };

            // 反向旋转 HitCircle 使其始终朝上
            d.OnUpdate += _ =>
            {
                switch (d)
                {
                    case DrawableHitCircle circle:
                        circle.CirclePiece.Rotation = -currentRotation;
                        break;
                }
            };
        }

        public void Update(Playfield playfield)
        {
            double currentTime = playfield.Time.Current;

            if (isBreakTime.Value)
            {
                lastUpdateTime = -1;
                return;
            }

            if (lastUpdateTime < 0)
                lastUpdateTime = currentTime;

            // ── 平滑插值：currentSpinSpeed 每帧向 targetSpinSpeed 靠拢 ──
            // 与 Adaptive Speed (ModAdaptiveSpeed) 相同的平滑策略，
            // 避免因判定导致的转速突变。
            currentSpinSpeed = Interpolation.DampContinuously(currentSpinSpeed, targetSpinSpeed, 100, playfield.Clock.ElapsedFrameTime);

            // ── 增量累计旋转角度 ──
            double deltaTime = currentTime - lastUpdateTime;
            lastUpdateTime = currentTime;

            float deltaAngle = (Direction.Value == RotationDirection.Counterclockwise ? -1 : 1)
                               * 360 * (float)(deltaTime / 60000 * currentSpinSpeed);

            currentRotation += deltaAngle;

            playfieldAdjustmentContainer.Rotation = currentRotation;

            // 反向旋转鼠标指针，使其始终朝上
            OsuPlayfield osuPlayfield = (OsuPlayfield)playfield;
            Debug.Assert(osuPlayfield.Cursor != null);
            osuPlayfield.Cursor.ActiveCursor.Rotation = -currentRotation;
        }
    }
}
