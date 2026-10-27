// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Bindables;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Localisation;
using osu.Game.Configuration;
using osu.Game.Rulesets.Mania.Objects;
using osu.Game.Rulesets.Mania.UI;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.UI;

namespace osu.Game.Rulesets.Mania.EzMania.Mods.LAsMods
{
    /// <summary>
    /// Emiria（Malody V）Key 模式 3D 斜轨补偿的移植。
    /// 在既有「斜轨角度」透视（<c>Ez2Setting.ManiaPseudo3DRotation</c>）之上，修正 note 的屏幕下落速度与视觉厚度，
    /// 使斜轨的手感接近平面轨道。
    /// </summary>
    /// <remarks>
    /// 只改写物件自身的 <c>Y</c> / Height / Scale，不修改透视容器，
    /// 因此与透视设置完全解耦：透视关闭时本 Mod 无任何效果。
    /// </remarks>
    public class ManiaModSlantCompensation : Mod, IApplicableToDrawableRuleset<ManiaHitObject>
    {
        public override string Name => "Slant Compensation";
        public override string Acronym => "SLC";
        public override IconUsage? Icon => null;
        public override ModType Type => ModType.NeriMod;
        public override LocalisableString Description => "Compensates note speed and thickness on the slanted (pseudo-3D) playfield.";
        public override double ScoreMultiplier => 1;
        public override bool Ranked => false;
        public override bool ValidForMultiplayer => true;
        public override bool ValidForFreestyleAsRequiredMod => false;

        /// <summary>
        /// 运动补偿（对应 Emiria 的 <c>com_motion</c>）：0 = 不补偿（纯透视），1 = 屏幕下落速度严格恒定。
        /// </summary>
        [SettingSource("运动补偿", "让物件在屏幕上的下落速度保持均匀（Emiria com_motion）。", 1)]
        public BindableFloat MotionCompensation { get; } = new BindableFloat(0.1f)
        {
            MinValue = 0,
            MaxValue = 1,
            Precision = 0.05f,
        };

        /// <summary>
        /// 厚度补偿（对应 Emiria 的 <c>com_thick</c>）：0 = 不补偿，1 = 顶部的视觉高度完全拉回。
        /// </summary>
        [SettingSource("厚度补偿", "让物件在斜轨顶部不被透视压扁（Emiria com_thick）。", 2)]
        public BindableFloat ThicknessCompensation { get; } = new BindableFloat(0.5f)
        {
            MinValue = 0,
            MaxValue = 1,
            Precision = 0.05f,
        };

        public void ApplyToDrawableRuleset(DrawableRuleset<ManiaHitObject> drawableRuleset)
        {
            if (drawableRuleset.Playfield is not ManiaPlayfield maniaPlayfield)
                return;

            // 每个列末尾挂一个补偿组件：顶层容器的更新晚于 HitObjectArea（滚动容器），
            // 因此能读到「本帧刚算好的线性 Y」再改写。详见 SlantNoteCompensator。
            foreach (var stage in maniaPlayfield.Stages)
            {
                foreach (var column in stage.Columns)
                    column.TopLevelContainer.Add(new SlantNoteCompensator(column, MotionCompensation, ThicknessCompensation));
            }
        }
    }
}
