// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Primitives;
using osu.Game.EzOsuGame.Configuration;
using osu.Game.EzOsuGame.Layout;
using osu.Game.Rulesets.UI;
using osu.Game.Skinning;
using osuTK;

namespace osu.Game.EzOsuGame.Background
{
    internal partial class EzGameplayBackgroundLayer : Container
    {
        private readonly DrawableRuleset drawableRuleset;
        private readonly Container globalBackgroundContainer;
        private readonly Container playfieldAlignedContainer;
        private IBindable<bool>? scaleSkinElements;

        internal SkinnableContainer SkinGlobalBackground { get; }
        internal SkinnableContainer SkinBackground { get; }
        internal SkinnableContainer SkinRulesetBackground { get; }
        internal Container EzGlobalBackgroundHost { get; } = new Container { RelativeSizeAxes = Axes.Both };
        internal Container EzBackgroundHost { get; } = new Container { RelativeSizeAxes = Axes.Both };
        internal Container EzRulesetBackgroundHost { get; } = new Container { RelativeSizeAxes = Axes.Both };

        public EzGameplayBackgroundLayer(DrawableRuleset drawableRuleset)
        {
            this.drawableRuleset = drawableRuleset;

            Children = new Drawable[]
            {
                globalBackgroundContainer = new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    Anchor = Anchor.TopLeft,
                    Origin = Anchor.TopLeft,
                    AlwaysPresent = true,
                    Children = new Drawable[]
                    {
                        SkinGlobalBackground = new SkinnableContainer(new GlobalSkinnableContainerLookup(GlobalSkinnableContainers.GlobalBackground))
                        {
                            RelativeSizeAxes = Axes.Both,
                            AlwaysPresent = true,
                        },
                        EzGlobalBackgroundHost,
                    },
                },
                playfieldAlignedContainer = new Container
                {
                    Anchor = Anchor.TopLeft,
                    Origin = Anchor.TopLeft,
                    AlwaysPresent = true,
                    Children = new Drawable[]
                    {
                        SkinBackground = new SkinnableContainer(new GlobalSkinnableContainerLookup(GlobalSkinnableContainers.Background))
                        {
                            RelativeSizeAxes = Axes.Both,
                            AlwaysPresent = true,
                        },
                        EzBackgroundHost,
                        SkinRulesetBackground = new SkinnableContainer(new GlobalSkinnableContainerLookup(GlobalSkinnableContainers.Background, drawableRuleset.Ruleset.RulesetInfo))
                        {
                            RelativeSizeAxes = Axes.Both,
                            AlwaysPresent = true,
                        },
                        EzRulesetBackgroundHost,
                    },
                },
            };

            RelativeSizeAxes = Axes.Both;
            playfieldAlignedContainer.RelativeSizeAxes = Axes.None;
            playfieldAlignedContainer.AutoSizeAxes = Axes.None;
        }

        [BackgroundDependencyLoader]
        private void load(Ez2ConfigManager ezConfig) => scaleSkinElements = ezConfig.GetBindable<bool>(Ez2Setting.ScaleSkinElements);

        internal static (Vector2 Position, Vector2 Size) ComputePlayfieldBounds(Vector2 localTopLeft, Vector2 localTopRight, Vector2 localBottomLeft) =>
            (localTopLeft, new Vector2((localTopRight - localTopLeft).Length, (localBottomLeft - localTopLeft).Length));

        protected override void Update()
        {
            base.Update();

            if (scaleSkinElements != null)
            {
                var compensation = EzHudSkinnableContainer.ComputeScaleCompensation(EzHudSkinnableContainer.FindAncestorUiScale(this), scaleSkinElements.Value);
                if (globalBackgroundContainer.Size != compensation.Size)
                    globalBackgroundContainer.Size = compensation.Size;
                if (globalBackgroundContainer.Scale != compensation.Scale)
                    globalBackgroundContainer.Scale = compensation.Scale;
            }

            if (!drawableRuleset.IsLoaded || !drawableRuleset.Playfield.IsLoaded)
                return;

            Quad quad = drawableRuleset.Playfield.SkinnableComponentScreenSpaceDrawQuad;
            var bounds = ComputePlayfieldBounds(ToLocalSpace(quad.TopLeft), ToLocalSpace(quad.TopRight), ToLocalSpace(quad.BottomLeft));
            playfieldAlignedContainer.Position = bounds.Position;
            playfieldAlignedContainer.Width = bounds.Size.X;
            playfieldAlignedContainer.Height = bounds.Size.Y;
            playfieldAlignedContainer.Rotation = drawableRuleset.PlayfieldAdjustmentContainer.Rotation;
        }
    }
}
