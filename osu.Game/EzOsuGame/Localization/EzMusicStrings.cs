// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Localisation;

namespace osu.Game.EzOsuGame.Localization
{
    public static class EzMusicStrings
    {
        public static readonly EzLocalizationManager.EzLocalisableString LOOP_SINGLE = new EzLocalizationManager.EzLocalisableString(
            "单曲循环",
            "Loop single");

        public static readonly EzLocalizationManager.EzLocalisableString LOOP_SEQUENTIAL = new EzLocalizationManager.EzLocalisableString(
            "顺序循环",
            "Loop sequential");

        public static readonly EzLocalizationManager.EzLocalisableString LOOP_SHUFFLE = new EzLocalizationManager.EzLocalisableString(
            "随机循环",
            "Shuffle");

        public static readonly EzLocalizationManager.EzLocalisableString PLAYLIST = new EzLocalizationManager.EzLocalisableString(
            "播放列表",
            "Playlist");

        public static readonly EzLocalizationManager.EzLocalisableString ADD_TO_FAVOURITES = new EzLocalizationManager.EzLocalisableString(
            "添加到收藏",
            "Add to Favourites");

        public static readonly EzLocalizationManager.EzLocalisableString IN_FAVOURITES = new EzLocalizationManager.EzLocalisableString(
            "已在收藏中",
            "In Favourites");

        public static readonly LocalisableString PLAYER_INFO_TEXT_SIZE = new EzLocalizationManager.EzLocalisableString(
            "信息字号",
            "Info text size");

        public static readonly LocalisableString PLAYER_INFO_WIDTH = new EzLocalizationManager.EzLocalisableString(
            "信息宽度",
            "Info width");

        public static readonly LocalisableString PLAYER_INFO_SHOW_DIFFICULTY = new EzLocalizationManager.EzLocalisableString(
            "显示难度",
            "Show difficulty");

        public static readonly LocalisableString PLAYER_INFO_SHOW_BPM = new EzLocalizationManager.EzLocalisableString(
            "显示 BPM",
            "Show BPM");

        public static readonly LocalisableString PLAYER_INFO_SHOW_LENGTH = new EzLocalizationManager.EzLocalisableString(
            "显示时长",
            "Show length");

        public static readonly LocalisableString PLAYER_COVER_SIZE = new EzLocalizationManager.EzLocalisableString(
            "封面直径",
            "Cover diameter");

        public static readonly LocalisableString PLAYER_COVER_RING_THICKNESS = new EzLocalizationManager.EzLocalisableString(
            "进度环粗细",
            "Progress ring thickness");

        public static readonly LocalisableString PLAYER_COVER_ALLOW_SEEK = new EzLocalizationManager.EzLocalisableString(
            "允许拖动进度",
            "Allow seeking");

        public static readonly LocalisableString PLAYER_CONTROLS_WIDTH = new EzLocalizationManager.EzLocalisableString(
            "控制条宽度",
            "Controls width");

        public static readonly LocalisableString PLAYER_CONTROLS_HEIGHT = new EzLocalizationManager.EzLocalisableString(
            "控制条高度",
            "Controls height");

        public static readonly LocalisableString PLAYER_CONTROLS_AUTO_HIDE = new EzLocalizationManager.EzLocalisableString(
            "悬停时显示",
            "Show on hover");

        public static readonly LocalisableString PLAYER_PLAYLIST_WIDTH = new EzLocalizationManager.EzLocalisableString(
            "列表宽度",
            "Playlist width");

        public static readonly LocalisableString PLAYER_PLAYLIST_HEIGHT = new EzLocalizationManager.EzLocalisableString(
            "列表高度",
            "Playlist height");

        public static readonly LocalisableString PLAYER_PLAYLIST_VISIBLE = new EzLocalizationManager.EzLocalisableString(
            "默认展开",
            "Start expanded");

        public static readonly LocalisableString PLAYER_PLAYLIST_FAVOURITES_ONLY_LABEL = new EzLocalizationManager.EzLocalisableString(
            "仅显示收藏夹",
            "Favourites only");

        public static readonly LocalisableString PLAYER_PLAYLIST_FAVOURITES_ONLY_DESCRIPTION = new EzLocalizationManager.EzLocalisableString(
            "列表只显示指定收藏夹中的曲目",
            "Only list beatmap sets contained in the selected collection.");

        public static readonly LocalisableString PLAYER_PLAYLIST_COLLECTION_LABEL = new EzLocalizationManager.EzLocalisableString(
            "收藏夹名称",
            "Collection name");

        public static readonly LocalisableString PLAYER_PLAYLIST_COLLECTION_DESCRIPTION = new EzLocalizationManager.EzLocalisableString(
            "要筛选的收藏夹名称，默认 Favourites",
            "Name of the collection to filter by; defaults to Favourites.");

        public static readonly LocalisableString PLAYER_COLLECTION_SELECTOR_LABEL = new EzLocalizationManager.EzLocalisableString(
            "显示收藏夹选择器",
            "Show collection selector");

        public static readonly LocalisableString PLAYER_COLLECTION_SELECTOR_DESCRIPTION = new EzLocalizationManager.EzLocalisableString(
            "在播放列表顶部显示收藏夹下拉，可直接切换筛选",
            "Show a collection dropdown above the playlist for quick filtering.");

        public static readonly LocalisableString PLAYER_COLLECTION_SELECTED_LABEL = new EzLocalizationManager.EzLocalisableString(
            "当前收藏夹筛选",
            "Current collection filter");

        public static readonly LocalisableString PLAYER_UNTITLED = new EzLocalizationManager.EzLocalisableString(
            "未知曲目",
            "Unknown track");
    }
}
