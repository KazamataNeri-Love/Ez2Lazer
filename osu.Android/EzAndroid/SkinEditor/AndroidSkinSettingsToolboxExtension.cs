// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Framework.Graphics;
using osu.Game.Overlays.SkinEditor;
using osu.Game.Skinning;

namespace osu.Android.EzAndroid.SkinEditor
{
    internal sealed class AndroidSkinSettingsToolboxExtension : ISkinSettingsToolboxExtension
    {
        public IEnumerable<Drawable> CreateAdditionalControls(SkinSettingsToolboxContext context)
        {
            if (context.Component is not ISerialisableDrawable serialisable)
                yield break;

            var component = context.Component;

            yield return new AndroidSkinAnchorSelector(component, serialisable, false, anchor => change(context, () =>
            {
                if (anchor is Anchor value)
                    SkinSelectionHandler.ApplyFixedAnchor(component, value);
                else
                    SkinSelectionHandler.ApplyClosestAnchor(component);
            }));

            yield return new AndroidSkinAnchorSelector(component, serialisable, true, origin =>
            {
                if (origin is Anchor value)
                    change(context, () => SkinSelectionHandler.ApplyLocalSpaceOrigin(component, value));
            });

            yield return new AndroidSkinActionBar(context, () => change(context, () =>
            {
                SkinSelectionHandler.ResetPosition(component);
                SkinSelectionHandler.ResetRotation(component);
                SkinSelectionHandler.ResetScale(component);
            }));
        }

        private static void change(SkinSettingsToolboxContext context, Action apply)
        {
            var handler = context.ChangeHandler;
            handler?.BeginChange();

            try
            {
                apply();
            }
            finally
            {
                handler?.EndChange();
            }
        }
    }
}
