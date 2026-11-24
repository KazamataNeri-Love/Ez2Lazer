// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Framework.Graphics;
using osu.Game.Screens.Edit;

namespace osu.Game.Overlays.SkinEditor
{
    /// <summary>
    /// Supplies optional controls for a skin component's settings toolbox.
    /// </summary>
    public interface ISkinSettingsToolboxExtension
    {
        IEnumerable<Drawable> CreateAdditionalControls(SkinSettingsToolboxContext context);
    }

    /// <summary>
    /// Actions and state available to optional skin settings toolbox controls.
    /// </summary>
    public class SkinSettingsToolboxContext
    {
        public Drawable Component { get; }
        public IEditorChangeHandler? ChangeHandler { get; }
        public Action? BringToFront { get; }
        public Action? SendToBack { get; }
        public Action? DeleteComponent { get; }

        public SkinSettingsToolboxContext(Drawable component, IEditorChangeHandler? changeHandler, Action? bringToFront, Action? sendToBack, Action? deleteComponent)
        {
            Component = component;
            ChangeHandler = changeHandler;
            BringToFront = bringToFront;
            SendToBack = sendToBack;
            DeleteComponent = deleteComponent;
        }
    }
}
