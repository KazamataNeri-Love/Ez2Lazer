// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using osu.Framework.Localisation;
using osu.Game.EzOsuGame.Edit.Components;
using osu.Game.EzOsuGame.Localization;

namespace osu.Game.EzOsuGame.Edit.Scenes
{
    /// <summary>
    /// 听歌界面场景：以真实的 EzMusicPlayerScreen 为宿主，编辑 MusicPlayer 布局容器。
    /// </summary>
    public class MusicPlayerSceneStrategy : IEzSkinEditorSceneStrategy
    {
        public EzSkinEditorSceneType SceneType => EzSkinEditorSceneType.MusicPlayer;

        public EzSkinEditorSceneGroup Group => EzSkinEditorSceneGroup.SongSelect;

        public LocalisableString TabTitle => EzEditorStrings.SCENE_MUSIC_PLAYER;

        public bool SupportsHudLayer => true;

        public bool HoverExpandsSidebar => false;

        public IReadOnlyList<EzSkinEditorSidebarGroupDefinition> CreateSidebarGroups(EzSkinEditorSceneContext context) => new[]
        {
            new EzSkinEditorSidebarGroupDefinition
            {
                Id = GameplaySceneStrategy.GROUP_ID_HUD,
                Title = EzEditorStrings.SIDEBAR_TAB_HUD,
                CreateContent = () => new EzSkinEditorHudWorkspaceSection(),
            },
            new EzSkinEditorSidebarGroupDefinition
            {
                Id = GameplaySceneStrategy.GROUP_ID_LIBRARY_GLOBAL,
                Title = EzEditorStrings.LIBRARY_GLOBAL_TITLE,
                Visible = context.ActiveLayer != EzSkinEditorLayer.HudOff,
                CreateContent = () => new EzSkinEditorComponentLibrarySection(rulesetScoped: false) { Title = EzEditorStrings.LIBRARY_GLOBAL_TITLE },
            },
        };
    }
}
