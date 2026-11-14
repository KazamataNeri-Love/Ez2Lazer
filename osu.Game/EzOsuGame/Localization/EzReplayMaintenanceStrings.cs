// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Localisation;

namespace osu.Game.EzOsuGame.Localization
{
    public class EzReplayMaintenanceStrings
    {
        public static readonly LocalisableString SECTION_HEADER =
            new EzLocalizationManager.EzLocalisableString("回放管理", "Replay management");

        public static readonly LocalisableString MANAGE_BUTTON =
            new EzLocalizationManager.EzLocalisableString("按玩家名管理回放…", "Manage replays by player…");

        public static readonly LocalisableString MANAGE_BUTTON_TOOLTIP = new EzLocalizationManager.EzLocalisableString(
            "扫描本地成绩与回放，按玩家名整批删除（连回放文件一起删）。"
            + "\n用于清理那些统计里能看到、但选歌界面永远查不到的回放。",
            "Scan local scores and replays, then delete them per player name (replay files included)."
            + "\nUse it to clean up replays that show up in statistics but can never be found in song select.");

        public static readonly LocalisableString DIALOG_HEADER =
            new EzLocalizationManager.EzLocalisableString("选择要删除回放的玩家", "Select players whose replays to delete");

        public static readonly LocalisableString DIALOG_BODY = new EzLocalizationManager.EzLocalisableString(
            "括号内是该玩家名下的本地成绩/回放数量。删除会永久移除成绩记录与回放文件，不可恢复。",
            "The number in brackets is that player's local score/replay count. Deletion permanently removes the score records and replay files.");

        public static readonly LocalisableString DELETE =
            new EzLocalizationManager.EzLocalisableString("删除所选玩家的成绩与回放", "Delete checked players' scores & replays");

        public static readonly LocalisableString CANCEL =
            new EzLocalizationManager.EzLocalisableString("取消", "Cancel");

        public static readonly LocalisableString CONFIRM_BODY = new EzLocalizationManager.EzLocalisableString(
            "将永久删除以下玩家名下的全部本地成绩与回放文件，不可恢复：\n{0}",
            "This permanently deletes every local score and replay file of the following players:\n{0}");

        public static readonly LocalisableString DELETING =
            new EzLocalizationManager.EzLocalisableString("正在删除成绩与回放…", "Deleting scores & replays…");

        public static readonly LocalisableString DELETE_DONE =
            new EzLocalizationManager.EzLocalisableString("已删除 {0} 条成绩与回放。", "Deleted {0} scores and replays.");

        public static readonly LocalisableString DELETE_FAILED =
            new EzLocalizationManager.EzLocalisableString("删除成绩与回放失败。", "Failed to delete scores and replays.");

        public static readonly LocalisableString NO_SCORES =
            new EzLocalizationManager.EzLocalisableString("未找到可删除的本地成绩。", "No local scores found to delete.");

        public static readonly LocalisableString SCAN_FAILED =
            new EzLocalizationManager.EzLocalisableString("扫描本地成绩失败。", "Failed to scan local scores.");
    }
}
