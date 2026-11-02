// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using osu.Framework.Localisation;
using osu.Game.EzOsuGame.Localization;
using osu.Game.EzOsuGame.Screens;

namespace osu.Game.EzOsuGame.Edit.Scenes
{
    /// <summary>
    /// 结算场景：在结算屏打开编辑器时生效，编辑结算 HUD / 背景两个布局层，
    /// 侧栏提供结算卡片背景配置。
    /// </summary>
    public class ResultsSceneStrategy : IEzSkinEditorSceneStrategy
    {
        public const string GROUP_ID_CARD_BACKGROUND = "results_card_background";

        public EzSkinEditorSceneType SceneType => EzSkinEditorSceneType.Results;

        public EzSkinEditorSceneGroup Group => EzSkinEditorSceneGroup.Gameplay;

        public LocalisableString TabTitle => EzEditorStrings.TAB_RESULTS;

        public bool SupportsHudLayer => true;

        public IReadOnlyList<EzSkinEditorSidebarGroupDefinition> CreateSidebarGroups(EzSkinEditorSceneContext context) => new[]
        {
            new EzSkinEditorSidebarGroupDefinition
            {
                Id = GROUP_ID_CARD_BACKGROUND,
                Title = EzEditorStrings.GROUP_RESULTS_CARD_BACKGROUND,
                CreateContent = () => new EzEditorSectionResultsSidebar(),
            },
        };
    }
}
