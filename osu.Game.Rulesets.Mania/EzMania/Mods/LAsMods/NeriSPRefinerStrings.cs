// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Localisation;
using osu.Game.EzOsuGame.Localization;

namespace osu.Game.Rulesets.Mania.EzMania.Mods.LAsMods
{
    public static class NeriSPRefinerStrings
    {
        public static readonly LocalisableString DESCRIPTION = new EzLocalizationManager.EzLocalisableString(
            "14K SpaceMix 键型顺手器：消解三方冲突，缓冲区域切换",
            "14K SpaceMix refiner: resolve triple-zone conflicts, buffer zone transitions.");

        public static readonly LocalisableString TRANSITION_WINDOW_LABEL = new EzLocalizationManager.EzLocalisableString(
            "切换窗口(拍)", "Transition Window (beats)");

        public static readonly LocalisableString TRANSITION_WINDOW_DESCRIPTION = new EzLocalizationManager.EzLocalisableString(
            "DP↔Effect 急促切换的判定窗口大小（拍数），小于此窗口视为急促",
            "Window size for detecting rapid DP↔Effect transitions. Smaller gap = rapid.");

        public static readonly LocalisableString MAX_DELETE_LABEL = new EzLocalizationManager.EzLocalisableString(
            "最大删除", "Max Delete");

        public static readonly LocalisableString MAX_DELETE_DESCRIPTION = new EzLocalizationManager.EzLocalisableString(
            "每个和弦最多删除的 note 数量",
            "Maximum notes to delete per chord.");

        public static readonly LocalisableString SETTING_TRANSITION_LABEL = new EzLocalizationManager.EzLocalisableString(
            "切换窗口", "Transition Window");

        public static readonly LocalisableString SETTING_MAX_DELETE_LABEL = new EzLocalizationManager.EzLocalisableString(
            "最大删除", "Max Delete");
    }
}
