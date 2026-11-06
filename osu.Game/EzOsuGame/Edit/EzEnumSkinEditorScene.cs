// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Game.EzOsuGame.Edit
{
    /// <summary>
    /// 可编辑场景的一级分组
    /// </summary>
    public enum EzEditorSceneGroup
    {
        SongSelect,
        Gameplay,
        BeatmapEditor
    }

    /// <summary>
    /// 可编辑场景的二级分组
    /// 一级分组见 <see cref="EzEditorSceneGroup"/>。
    /// </summary>
    public enum EzEditorSceneType
    {
        SongSelectHud,
        BeatmapPreview,
        GameplayHud,
        MusicPlayer,
        Appearance,
        Note,
        SkinIni,
        SkinSpecific,
        EditorColouring,

        /// <summary>结算屏（编辑结算 HUD 与卡片背景）。</summary>
        Results,
    }
}
