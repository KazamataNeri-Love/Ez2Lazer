// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Diagnostics;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Extensions.LocalisationExtensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osu.Game.EzOsuGame.Configuration;
using osu.Game.EzOsuGame.UI;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osu.Game.Overlays;
using osuTK;
using osuTK.Graphics;
using WebCommonStrings = osu.Game.Resources.Localisation.Web.CommonStrings;

namespace osu.Game.Screens.Select
{
    public partial class PanelGroupStarDifficulty : Panel
    {
        public const float HEIGHT = PanelGroup.HEIGHT;

        [Resolved]
        private OsuColour colours { get; set; } = null!;

        [Resolved]
        private OverlayColourProvider colourProvider { get; set; } = null!;

        private Box contentBackground = null!;
        private OsuSpriteText starRatingText = null!;
        private CircularContainer countPill = null!;
        private OsuSpriteText countText = null!;

        [Resolved]
        private Ez2ConfigManager ezConfig { get; set; } = null!;

        private Bindable<EzMaterialUiMode> materialUiMode = null!;

        [BackgroundDependencyLoader]
        private void load()
        {
            Height = PanelGroup.HEIGHT;

            materialUiMode = ezConfig.GetBindable<EzMaterialUiMode>(Ez2Setting.MaterialUiMode);

            AccentColour = colourProvider.Highlight1;

            Container colourOverlays;

            Content.Children = new Drawable[]
            {
                colourOverlays = new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    Children = new Drawable[]
                    {
                        contentBackground = new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                        },
                    }
                },
                new FillFlowContainer
                {
                    Anchor = Anchor.CentreLeft,
                    Origin = Anchor.CentreLeft,
                    AutoSizeAxes = Axes.Both,
                    Spacing = new Vector2(10f, 0f),
                    Margin = new MarginPadding { Left = 10f },
                    Children = new Drawable[]
                    {
                        starRatingText = new OsuSpriteText
                        {
                            Anchor = Anchor.CentreLeft,
                            Origin = Anchor.CentreLeft,
                            UseFullGlyphHeight = false,
                            Font = OsuFont.Style.Heading2,
                        }
                    }
                },
                countPill = new CircularContainer
                {
                    Anchor = Anchor.CentreRight,
                    Origin = Anchor.CentreRight,
                    Size = new Vector2(50f, 14f),
                    Margin = new MarginPadding { Right = 30f },
                    Masking = true,
                    Children = new Drawable[]
                    {
                        new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                            Colour = Color4.Black.Opacity(0.7f),
                        },
                        countText = new OsuSpriteText
                        {
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            Font = OsuFont.Style.Caption1.With(weight: FontWeight.Bold),
                            UseFullGlyphHeight = false,
                        }
                    },
                }
            };

            EzMaterialOverlayAlpha.BindHiddenWhenAcrylic(colourOverlays, materialUiMode);
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();
        }

        private Color4 ratingColour;

        protected override void PrepareForUse()
        {
            base.PrepareForUse();

            Debug.Assert(Item != null);

            var group = (StarDifficultyGroupDefinition)Item.Model;
            int starNumber = (int)group.Difficulty.Stars;

            ratingColour = starNumber >= 9 ? OsuColour.Gray(0.2f) : colours.ForStarDifficulty(starNumber);

            AccentColour = ratingColour;
            contentBackground.Colour = ratingColour.Darken(1f);
            starRatingText.Colour = colourProvider.Content1;
            starRatingText.Text = group.Title;

            countText.Text = Item.NestedItemCount.ToLocalisableString(@"N0");
        }

        protected override void Update()
        {
            base.Update();

            // Move the count pill in the opposite direction to keep it pinned to the screen regardless of the X position of TopLevelContent.
            countPill.X = -TopLevelContent.X;
        }

        public override MenuItem[] ContextMenuItems
        {
            get
            {
                if (Item == null)
                    return Array.Empty<MenuItem>();

                return new MenuItem[]
                {
                    new OsuMenuItem(Expanded.Value ? WebCommonStrings.ButtonsCollapse.ToSentence() : WebCommonStrings.ButtonsExpand.ToSentence(), MenuItemType.Highlighted, () => TriggerClick())
                };
            }
        }
    }
}
