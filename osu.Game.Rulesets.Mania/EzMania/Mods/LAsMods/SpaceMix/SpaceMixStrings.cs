// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Localisation;
using osu.Game.EzOsuGame.Localization;

namespace osu.Game.Rulesets.Mania.EzMania.Mods.LAsMods
{
    public static class SpaceMixStrings
    {
        // ── 主模组 ──
        public static readonly LocalisableString SPACEMIX_MAIN_DESCRIPTION = new EzLocalizationManager.EzLocalisableString(
            "14K SpaceMix 谱面重构：将任意Mania谱面转换为14K并重新分配列，三面板区域感知",
            "14K SpaceMix rebuild: convert any Mania beatmap to 14K, reassign columns with 3-panel awareness.");

        public static readonly LocalisableString SPACEMIX_DENSITY_LABEL = new EzLocalizationManager.EzLocalisableString("密度", "Density");
        public static readonly LocalisableString SPACEMIX_DENSITY_DESCRIPTION = new EzLocalizationManager.EzLocalisableString("密度强度（1-10）", "Density strength (1-10).");
        public static readonly LocalisableString SPACEMIX_MAX_CHORD_LABEL = new EzLocalizationManager.EzLocalisableString("和弦上限", "Max Chord");
        public static readonly LocalisableString SPACEMIX_MAX_CHORD_DESCRIPTION = new EzLocalizationManager.EzLocalisableString("每一排最多保留的note数量", "Maximum notes per row.");
        public static readonly LocalisableString SPACEMIX_ALIGN_LABEL = new EzLocalizationManager.EzLocalisableString("对齐", "Align");
        public static readonly LocalisableString SPACEMIX_ALIGN_DESCRIPTION = new EzLocalizationManager.EzLocalisableString("对齐到节拍网格，0=关闭", "Snap to beat grid. 0=off.");
        public static readonly LocalisableString SPACEMIX_DELAY_LABEL = new EzLocalizationManager.EzLocalisableString("Delay", "Delay");
        public static readonly LocalisableString SPACEMIX_DELAY_DESCRIPTION = new EzLocalizationManager.EzLocalisableString("随机偏移note时间", "Randomly offset note times.");
        public static readonly LocalisableString SPACEMIX_REGENERATE_LABEL = new EzLocalizationManager.EzLocalisableString("重生成", "Regenerate");
        public static readonly LocalisableString SPACEMIX_REGENERATE_DESCRIPTION = new EzLocalizationManager.EzLocalisableString("重生成谱面note密度", "Regenerate note density.");
        public static readonly LocalisableString SPACEMIX_REGENERATE_DIFFICULTY_LABEL = new EzLocalizationManager.EzLocalisableString("重生成难度", "Regen Difficulty");
        public static readonly LocalisableString SPACEMIX_REGENERATE_DIFFICULTY_DESCRIPTION = new EzLocalizationManager.EzLocalisableString("重生成难度等级（2-10）", "Regeneration difficulty (2-10).");

        // ── 面板切换（主模组用）──
        public static readonly LocalisableString SPACEMIX_PANEL_OSCILLATION_LABEL = new EzLocalizationManager.EzLocalisableString("面板切换周期", "Panel Switch Cycle");
        public static readonly LocalisableString SPACEMIX_PANEL_OSCILLATION_DESCRIPTION = new EzLocalizationManager.EzLocalisableString("面板切换的拍数周期，越长切换越慢", "Beat cycle for panel switching. Longer = slower switches.");

        // ── 子模组通用 ──
        public static readonly LocalisableString SPACEMIX_PATTERN_DESCRIPTION = new EzLocalizationManager.EzLocalisableString(
            "14K SpaceMix 键型变换：三面板区域自动切换",
            "14K SpaceMix pattern: three-panel auto-switching.");
        public static readonly LocalisableString SPACEMIX_PATTERN_LEVEL_LABEL = new EzLocalizationManager.EzLocalisableString("等级", "Level");
        public static readonly LocalisableString SPACEMIX_WAVEFORM_LABEL = new EzLocalizationManager.EzLocalisableString("波形", "Waveform");
        public static readonly LocalisableString SPACEMIX_WAVEFORM_DESCRIPTION = new EzLocalizationManager.EzLocalisableString("振荡器波形", "Oscillator waveform.");
        public static readonly LocalisableString SPACEMIX_OSCILLATION_BEATS_LABEL = new EzLocalizationManager.EzLocalisableString("振荡周期", "Oscillation Beats");
        public static readonly LocalisableString SPACEMIX_OSCILLATION_BEATS_DESCRIPTION = new EzLocalizationManager.EzLocalisableString("振荡周期（拍）", "Oscillation period in beats.");
        public static readonly LocalisableString SPACEMIX_WINDOW_INTERVAL_LABEL = new EzLocalizationManager.EzLocalisableString("窗口间隔", "Window Interval");
        public static readonly LocalisableString SPACEMIX_WINDOW_INTERVAL_DESCRIPTION = new EzLocalizationManager.EzLocalisableString("每隔几个窗口处理一次", "Process every Nth window.");
        public static readonly LocalisableString SPACEMIX_WINDOW_OFFSET_LABEL = new EzLocalizationManager.EzLocalisableString("窗口偏移", "Window Offset");
        public static readonly LocalisableString SPACEMIX_WINDOW_OFFSET_DESCRIPTION = new EzLocalizationManager.EzLocalisableString("窗口起始偏移", "Window start offset.");
        public static readonly LocalisableString SPACEMIX_SKIP_FINE_LABEL = new EzLocalizationManager.EzLocalisableString("跳过密集阈值", "Skip Fine Threshold");
        public static readonly LocalisableString SPACEMIX_SKIP_FINE_DESCRIPTION = new EzLocalizationManager.EzLocalisableString("密集窗口跳过的判定阈值", "Skip threshold for dense windows.");
        public static readonly LocalisableString SPACEMIX_SKIP_QUARTER_LABEL = new EzLocalizationManager.EzLocalisableString("跳过四分除数", "Skip Quarter Divisor");
        public static readonly LocalisableString SPACEMIX_SKIP_QUARTER_DESCRIPTION = new EzLocalizationManager.EzLocalisableString("跳过判定的四分线除数", "Skip quarter line divisor.");

        // ── 面板策略参数 ──
        public static readonly LocalisableString SPACEMIX_KPS_MAX_LABEL = new EzLocalizationManager.EzLocalisableString("KPS上限", "KPS Max");
        public static readonly LocalisableString SPACEMIX_KPS_MAX_DESCRIPTION = new EzLocalizationManager.EzLocalisableString("KPS参考上限", "Reference max KPS.");
        public static readonly LocalisableString SPACEMIX_EFFECT_WEIGHT_LABEL = new EzLocalizationManager.EzLocalisableString("Effect权重", "Effect Weight");
        public static readonly LocalisableString SPACEMIX_EFFECT_WEIGHT_DESCRIPTION = new EzLocalizationManager.EzLocalisableString("Effect面板基础权重（0~1）", "Base weight for Effect panel (0~1).");
        public static readonly LocalisableString SPACEMIX_MIN_BEATS_LABEL = new EzLocalizationManager.EzLocalisableString("最短持续拍", "Min Beats");
        public static readonly LocalisableString SPACEMIX_MIN_BEATS_DESCRIPTION = new EzLocalizationManager.EzLocalisableString("面板最短持续拍数", "Minimum beats per panel.");

        // ── 子模组描述 ──
        public static readonly LocalisableString SPACEMIX_DESC_JACK = new EzLocalizationManager.EzLocalisableString("14K SpaceMix 叠/子弹", "14K SpaceMix Jack pattern.");
        public static readonly LocalisableString SPACEMIX_DESC_DUMP = new EzLocalizationManager.EzLocalisableString("14K SpaceMix 楼梯", "14K SpaceMix Dump pattern.");
        public static readonly LocalisableString SPACEMIX_DESC_CHORD = new EzLocalizationManager.EzLocalisableString("14K SpaceMix 拍", "14K SpaceMix Chord pattern.");
        public static readonly LocalisableString SPACEMIX_DESC_BRACKET = new EzLocalizationManager.EzLocalisableString("14K SpaceMix 切叉", "14K SpaceMix Bracket pattern.");
        public static readonly LocalisableString SPACEMIX_DESC_DELAY = new EzLocalizationManager.EzLocalisableString("14K SpaceMix 偏移", "14K SpaceMix Delay pattern.");

        // ── 等级描述 ──
        public static readonly LocalisableString SPACEMIX_LEVEL_JACK_DESCRIPTION = new EzLocalizationManager.EzLocalisableString("Jack 变换强度", "Jack intensity.");
        public static readonly LocalisableString SPACEMIX_LEVEL_DUMP_DESCRIPTION = new EzLocalizationManager.EzLocalisableString("Dump 变换强度", "Dump intensity.");
        public static readonly LocalisableString SPACEMIX_LEVEL_CHORD_DESCRIPTION = new EzLocalizationManager.EzLocalisableString("Chord 变换强度", "Chord intensity.");
        public static readonly LocalisableString SPACEMIX_LEVEL_BRACKET_DESCRIPTION = new EzLocalizationManager.EzLocalisableString("Bracket 变换强度", "Bracket intensity.");
        public static readonly LocalisableString SPACEMIX_LEVEL_DELAY_DESCRIPTION = new EzLocalizationManager.EzLocalisableString("Delay 变换强度", "Delay intensity.");
    }
}
