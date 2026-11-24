// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Testing;
using osu.Game.Overlays;
using osu.Game.Overlays.SkinEditor;
using osu.Game.Skinning;
using osuTK;

namespace osu.Game.Tests.Visual.Editing
{
    public partial class TestSceneSkinSettingsToolboxExtension : OsuTestScene
    {
        [Cached]
        private readonly OverlayColourProvider colourProvider = new OverlayColourProvider(OverlayColourScheme.Blue);

        [Test]
        public void TestCoordinatePreservedWhenChangingAnchorAndOrigin()
        {
            Vector2 expectedScreenCentre = Vector2.Zero;
            var drawable = new Box
            {
                Size = new Vector2(100),
                Position = new Vector2(40, 60),
                Anchor = Anchor.TopLeft,
                Origin = Anchor.TopLeft,
            };

            AddStep("add drawable", () => Child = drawable);
            AddUntilStep("wait for layout", () => drawable.IsPresent);
            AddStep("record screen centre", () => expectedScreenCentre = drawable.ScreenSpaceDrawQuad.Centre);

            AddStep("apply fixed anchor", () => SkinSelectionHandler.ApplyFixedAnchor(drawable, Anchor.BottomRight));
            AddAssert("fixed anchor preserves screen centre", () => Vector2.Distance(drawable.ScreenSpaceDrawQuad.Centre, expectedScreenCentre), () => Is.LessThan(0.1f));

            AddStep("apply local origin", () => SkinSelectionHandler.ApplyLocalSpaceOrigin(drawable, Anchor.Centre));
            AddAssert("local origin preserves screen centre", () => Vector2.Distance(drawable.ScreenSpaceDrawQuad.Centre, expectedScreenCentre), () => Is.LessThan(0.1f));

            AddStep("apply closest anchor", () => SkinSelectionHandler.ApplyClosestAnchor(drawable));
            AddAssert("closest anchor preserves screen centre", () => Vector2.Distance(drawable.ScreenSpaceDrawQuad.Centre, expectedScreenCentre), () => Is.LessThan(0.1f));
        }

        [Test]
        public void TestToolboxWithoutExtension()
        {
            AddStep("add toolbox without extension", () => Child = new SkinSettingsToolbox(new TestDrawable()));
            AddUntilStep("toolbox loads", () => Child.IsLoaded);
            AddAssert("no extension controls", () => Child.ChildrenOfType<ExtensionControl>().Count(), () => Is.EqualTo(0));
        }

        [Test]
        public void TestToolboxWithExtension()
        {
            AddStep("add toolbox with extension", () => Child = new ExtensionHost());
            AddUntilStep("toolbox loads", () => Child.IsLoaded);
            AddAssert("extension control inserted", () => Child.ChildrenOfType<ExtensionControl>().Count(), () => Is.EqualTo(1));
        }

        private partial class ExtensionHost : Container
        {
            [Cached(typeof(ISkinSettingsToolboxExtension))]
            private readonly ISkinSettingsToolboxExtension extension = new TestExtension();

            public ExtensionHost()
            {
                Child = new SkinSettingsToolbox(new TestDrawable());
            }
        }

        private partial class TestExtension : ISkinSettingsToolboxExtension
        {
            public IEnumerable<Drawable> CreateAdditionalControls(SkinSettingsToolboxContext context)
            {
                yield return new ExtensionControl();
            }
        }

        private partial class ExtensionControl : Container
        {
        }

        private partial class TestDrawable : Box, ISerialisableDrawable
        {
            public bool UsesFixedAnchor { get; set; }
        }
    }
}
