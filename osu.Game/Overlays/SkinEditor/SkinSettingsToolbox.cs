// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using System.Reflection;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Game.Configuration;
using osu.Game.EzOsuGame.Effects;
using osu.Game.EzOsuGame.Edit.Components;
using osu.Game.Localisation;
using osu.Game.Overlays.Settings;
using osu.Game.Screens.Edit;
using osu.Game.Screens.Edit.Components;
using osu.Game.Skinning;
using osuTK;

namespace osu.Game.Overlays.SkinEditor
{
    internal partial class SkinSettingsToolbox : EditorSidebarSection
    {
        [Resolved]
        private IEditorChangeHandler? changeHandler { get; set; }

        [Resolved(canBeNull: true)]
        private SkinEditor? skinEditor { get; set; }

        [Resolved(canBeNull: true)]
        private EzSkinEditorHudLayer? hudLayer { get; set; }

        [Resolved(canBeNull: true)]
        private ISkinSettingsToolboxExtension? extension { get; set; }

        protected override Container<Drawable> Content { get; }

        private readonly Drawable component;
        private SkinHudCommonSettings? commonSettings;

        public SkinSettingsToolbox(Drawable component)
            : base(SkinEditorStrings.Settings(component.GetType().Name))
        {
            this.component = component;

            base.Content.Add(Content = new FillFlowContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(10),
            });
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            rebuild();

            if (component is IScopedSkinSettings scoped)
                scoped.WatchSettingsScope(() => Scheduler.AddOnce(rebuild));

            foreach (var property in component.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (property.GetCustomAttribute<EzBuiltinEffectSourceAttribute>(true) == null)
                    continue;

                if (property.GetValue(component) is IScopedSkinSettings host)
                    host.WatchSettingsScope(() => Scheduler.AddOnce(rebuild));
            }
        }

        private void rebuild()
        {
            Content.Clear(disposeChildren: true);

            var controls = component.CreateSettingsControls().ToArray();
            Drawable[] positionControls = [];

            commonSettings = SkinHudCommonSettings.TryCreate(component);

            if (commonSettings != null)
            {
                positionControls = commonSettings.CreateControls();
                Content.AddRange(positionControls);
            }

            var extensionControls = extension?.CreateAdditionalControls(new SkinSettingsToolboxContext(
                component,
                changeHandler,
                createHostAction(skinEditor == null ? null : skinEditor.BringSelectionToFront, hudLayer == null ? null : hudLayer.BringComponentToFront),
                createHostAction(skinEditor == null ? null : skinEditor.SendSelectionToBack, hudLayer == null ? null : hudLayer.SendComponentToBack),
                createDeleteAction()))?.ToArray() ?? [];

            Content.AddRange(extensionControls);
            Content.AddRange(controls);

            // track any changes to update undo states.
            foreach (var c in controls.Concat(positionControls).Concat(extensionControls).OfType<ISettingsItem>())
            {
                // TODO: SettingChanged is called too often for cases like SettingsTextBox and SettingsSlider.
                // We will want to expose a SettingCommitted or similar to make this work better.
                c.SettingChanged += () => changeHandler?.SaveState();
            }
        }

        private Action? createHostAction(Action? officialAction, Action<ISerialisableDrawable>? ezAction)
        {
            if (component is not ISerialisableDrawable serialisable)
                return null;

            if (hudLayer is { } ezLayer && ezLayer.SelectedComponents.Contains(serialisable))
                return ezAction == null ? null : () => ezAction(serialisable);

            if (skinEditor != null)
                return officialAction;

            return hudLayer == null || ezAction == null ? null : () => ezAction(serialisable);
        }

        private Action? createDeleteAction()
        {
            if (component is not ISerialisableDrawable serialisable)
                return null;

            if (hudLayer is { } ezLayer && ezLayer.SelectedComponents.Contains(serialisable))
                return () => ezLayer.DeleteComponent(serialisable);

            if (skinEditor != null)
                return () => skinEditor.DeleteItems(new[] { serialisable });

            return hudLayer == null ? null : () => hudLayer.DeleteComponent(serialisable);
        }

        protected override void Update()
        {
            base.Update();
            commonSettings?.SyncFromComponent();
        }
    }
}
