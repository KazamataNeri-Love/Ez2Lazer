// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Localisation;
using osu.Game.Configuration;
using osu.Game.Rulesets.Mania.Objects;
using osu.Game.Rulesets.Mania.Objects.Drawables;
using osu.Game.Rulesets.Mania.UI;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Objects.Drawables;
using osu.Game.Rulesets.UI;

namespace osu.Game.Rulesets.Mania.EzMania.Mods.LAsMods
{
    /// <summary>
    /// Mania New Dance — 实时空间扭曲。
    ///
    /// 数学模型：X_visual = X_logic + A × sin(2π × f × Y_normalized + φ)
    ///
    /// 每帧遍历各个 Column 的 AliveEntries，读取当前 drawable.Y（由 ScrollingHitObjectContainer
    /// 设置），实时计算 X = f(Y) 并赋值。Note 锚点以为自己还在直线下落（Y 轴不受影响），
    /// 但 X 被 Y 扭曲，形成蛇形扭动效果。
    ///
    /// 与 3D 透视（顶层 BufferedContainer）完全共存——各自操作不同层级。
    /// 只影响 Note Drawable 层，Column 背景/判定线保持笔直。
    /// </summary>
    public class ManiaModNeriNewDance : Mod, IUpdatableByPlayfield, IApplicableToDrawableRuleset<ManiaHitObject>
    {
        public override string Name => "New Dance";
        public override string Acronym => "ND2";
        public override IconUsage? Icon => null;
        public override ModType Type => ModType.NeriMod;
        public override LocalisableString Description => "Space is distorted into a sine wave! Notes slither like snakes.";
        public override double ScoreMultiplier => 1;
        public override bool Ranked => false;
        public override bool ValidForMultiplayer => true;
        public override bool ValidForFreestyleAsRequiredMod => false;

        /// <summary>
        /// 判定线的 Y 位置（Stage.HIT_TARGET_POSITION），用于归一化 Y 坐标。
        /// </summary>
        private const float hit_target_y = 110;

        [SettingSource("Amplitude", "Max horizontal offset (pixels).", 1)]
        public BindableFloat Amplitude { get; } = new BindableFloat(25)
        {
            MinValue = 0f,
            MaxValue = 80f,
            Precision = 1f,
        };

        [SettingSource("Frequency", "Number of sine waves across visible height.", 2)]
        public BindableFloat Frequency { get; } = new BindableFloat(2)
        {
            MinValue = 0.5f,
            MaxValue = 8f,
            Precision = 0.1f,
        };

        [SettingSource("Phase Shift", "Phase offset per column (0=in phase, 1=max cascade).", 3)]
        public BindableFloat PhaseShift { get; } = new BindableFloat(0.5f)
        {
            MinValue = 0f,
            MaxValue = 2f,
            Precision = 0.1f,
        };

        private ManiaPlayfield? maniaPlayfield;

        public void ApplyToDrawableRuleset(DrawableRuleset<ManiaHitObject> drawableRuleset)
        {
            maniaPlayfield = (ManiaPlayfield)drawableRuleset.Playfield;
        }

        public void Update(Playfield playfield)
        {
            float amplitude = Amplitude.Value;
            float frequency = Frequency.Value;
            float phaseShift = PhaseShift.Value;

            if (amplitude <= 0 || maniaPlayfield == null)
                return;

            foreach (var stage in maniaPlayfield.Stages)
            {
                foreach (var column in stage.Columns)
                {
                    float colHeight = column.DrawHeight;
                    if (colHeight <= 0) continue;

                    float colPhase = column.Index * phaseShift;

                    foreach (var entry in column.HitObjectContainer.AliveEntries)
                    {
                        var drawable = entry.Value;

                        // 跳过 HoldNote 容器（子部件单独处理）
                        if (drawable is DrawableHoldNote)
                            continue;

                        // Y_normalized: 0 = 判定线, 1 = 屏幕顶部（Note 刚出现的位置）
                        // drawable.Y 由 ScrollingHitObjectContainer.updatePosition 每帧设置
                        float yNorm = Math.Clamp((hit_target_y - drawable.Y) / colHeight, 0f, 1f);

                        double phase = 2 * Math.PI * frequency * yNorm + colPhase;
                        float offset = (float)(amplitude * Math.Sin(phase));

                        drawable.X = offset;
                    }
                }
            }
        }
    }
}
