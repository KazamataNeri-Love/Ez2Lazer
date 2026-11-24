// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;
using NUnit.Framework;
using osu.Framework.Audio;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Game.EzOsuGame.Background;
using osu.Game.EzOsuGame.Edit;
using osu.Game.EzOsuGame.Layout;
using osu.Game.EzOsuGame.SkinEditor;
using osu.Game.Rulesets.Judgements;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Osu;
using osu.Game.Rulesets.UI;
using osu.Game.Scoring;
using osu.Game.Skinning;
using osuTK;

namespace osu.Game.Tests.NonVisual
{
    [TestFixture]
    public partial class EzGameplayBackgroundLayerTest
    {
        [Test]
        public void TestComputePlayfieldBounds()
        {
            var bounds = EzGameplayBackgroundLayer.ComputePlayfieldBounds(new Vector2(10, 20), new Vector2(110, 20), new Vector2(10, 70));

            Assert.That(bounds.Position, Is.EqualTo(new Vector2(10, 20)));
            Assert.That(bounds.Size, Is.EqualTo(new Vector2(100, 50)));
        }

        [Test]
        public void TestComputePlayfieldBoundsWithRotatedNonUniformQuad()
        {
            var bounds = EzGameplayBackgroundLayer.ComputePlayfieldBounds(new Vector2(30, 40), new Vector2(90, 120), new Vector2(-10, 70));

            Assert.That(bounds.Position, Is.EqualTo(new Vector2(30, 40)));
            Assert.That(bounds.Size.X, Is.EqualTo(100).Within(0.001));
            Assert.That(bounds.Size.Y, Is.EqualTo(50).Within(0.001));
        }

        [Test]
        public void TestBackgroundCanSnapInBothScopes()
        {
            Assert.That(SkinHudSnapSettings.CanSnap(new GlobalSkinnableContainerLookup(GlobalSkinnableContainers.GlobalBackground)), Is.True);
            Assert.That(SkinHudSnapSettings.CanSnap(new GlobalSkinnableContainerLookup(GlobalSkinnableContainers.Background)), Is.True);
            Assert.That(SkinHudSnapSettings.CanSnap(new GlobalSkinnableContainerLookup(GlobalSkinnableContainers.Background, new OsuRuleset().RulesetInfo)), Is.True);
        }

        [Test]
        public void TestGlobalBackgroundLayerOptions()
        {
            var layers = EzSkinEditorLayerOptions.For(EzEditorSceneType.GameplayHud);

            Assert.That(layers, Does.Contain(EzSkinEditorLayerOptions.EZ_GLOBAL_BACKGROUND));
            Assert.That(layers, Does.Contain(EzSkinEditorLayerOptions.SKIN_GLOBAL_BACKGROUND));
            Assert.That(EzSkinEditorLayerOptions.ResolveLookup(EzEditorSceneType.GameplayHud, EzHUDLayer.EzGlobalBackground, null)?.Lookup, Is.EqualTo(GlobalSkinnableContainers.GlobalBackground));
            Assert.That(EzSkinEditorLayerOptions.ResolveLookup(EzEditorSceneType.GameplayHud, EzHUDLayer.SkinGlobalBackground, null)?.Lookup, Is.EqualTo(GlobalSkinnableContainers.GlobalBackground));
        }

        [TestCase(2, false, 2, 0.5f)]
        [TestCase(2, true, 1, 1)]
        [TestCase(0, false, 1, 1)]
        [TestCase(-1, false, 1, 1)]
        public void TestGlobalBackgroundScaleCompensation(float uiScale, bool scaleSkinElements, float expectedSize, float expectedScale)
        {
            var compensation = EzHudSkinnableContainer.ComputeScaleCompensation(uiScale, scaleSkinElements);

            Assert.That(compensation.Size, Is.EqualTo(new Vector2(expectedSize)));
            Assert.That(compensation.Scale, Is.EqualTo(new Vector2(expectedScale)));
        }

        [Test]
        public void TestLayerOrderAndSkinContainers()
        {
            var layer = new EzGameplayBackgroundLayer(new TestDrawableRuleset());
            var globalBackground = (Container)layer.Children[0];
            var playfieldAligned = (Container)layer.Children[1];

            Assert.That(globalBackground.Children[0], Is.SameAs(layer.SkinGlobalBackground));
            Assert.That(globalBackground.Children[1], Is.SameAs(layer.EzGlobalBackgroundHost));
            Assert.That(playfieldAligned.Children[0], Is.SameAs(layer.SkinBackground));
            Assert.That(playfieldAligned.Children[1], Is.SameAs(layer.EzBackgroundHost));
            Assert.That(playfieldAligned.Children[2], Is.SameAs(layer.SkinRulesetBackground));
            Assert.That(playfieldAligned.Children[3], Is.SameAs(layer.EzRulesetBackgroundHost));
            Assert.That(globalBackground.AlwaysPresent, Is.True);
            Assert.That(globalBackground.Anchor, Is.EqualTo(Anchor.TopLeft));
            Assert.That(globalBackground.Origin, Is.EqualTo(Anchor.TopLeft));
            Assert.That(playfieldAligned.RelativeSizeAxes, Is.EqualTo(Axes.None));
            Assert.That(playfieldAligned.AutoSizeAxes, Is.EqualTo(Axes.None));
            Assert.That(playfieldAligned.Anchor, Is.EqualTo(Anchor.TopLeft));
            Assert.That(playfieldAligned.Origin, Is.EqualTo(Anchor.TopLeft));

            foreach (var skinContainer in new[] { layer.SkinGlobalBackground, layer.SkinBackground, layer.SkinRulesetBackground })
            {
                Assert.That(skinContainer.RelativeSizeAxes, Is.EqualTo(Axes.Both));
                Assert.That(skinContainer.ExternalTarget, Is.Null);
                Assert.That(skinContainer.AlwaysPresent, Is.True);
            }
        }

        private partial class TestDrawableRuleset : DrawableRuleset
        {
            public override IEnumerable<HitObject> Objects => Array.Empty<HitObject>();
            public override event Action<JudgementResult> NewResult { add { } remove { } }
            public override event Action<JudgementResult> RevertResult { add { } remove { } }
            public override IAdjustableAudioComponent Audio => null;
            public override Playfield Playfield => null;
            public override PlayfieldAdjustmentContainer PlayfieldAdjustmentContainer => null;
            public override Container Overlays => null;
            public override Container FrameStableComponents => null;
            public override IFrameStableClock FrameStableClock => null;
            internal override bool FrameStablePlayback { get; set; }
            public override IReadOnlyList<Mod> Mods => Array.Empty<Mod>();
            public override double GameplayStartTime => 0;
            public override GameplayCursorContainer Cursor => null;

            public TestDrawableRuleset()
                : base(new OsuRuleset())
            {
            }

            public override void SetReplayScore(Score replayScore) { }
            public override void SetRecordTarget(Score score) { }
            public override void RequestResume(Action continueResume) { }
            public override void CancelResume() { }
        }
    }
}
