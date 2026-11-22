// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Framework.Graphics.Containers;
using osu.Framework.Testing;
using osu.Game.EzOsuGame.Localization;
using osu.Game.EzOsuGame.Mods;
using osu.Game.EzOsuGame.Overlays;
using osu.Game.Graphics.UserInterface;
using osu.Game.Overlays.Mods;
using osu.Game.Tests.Visual.SongSelect;
using osuTK;
using osuTK.Input;

namespace osu.Game.Tests.Visual.EzOsuGame
{
    public partial class TestSceneEzModSelectJson : SongSelectTestScene
    {
        [Test]
        public void TestEditModeShowsModLayoutControls()
        {
            LoadSongSelect();

            AddStep("open mod overlay", () => InputManager.Key(Key.F1));
            AddUntilStep("overlay visible", () => modOverlay.State.Value == Visibility.Visible);
            AddUntilStep("columns loaded", () => modOverlay.ChildrenOfType<ModColumn>().Any(c => c.IsLoaded && c.ItemsLoaded));

            enterEditMode();

            AddUntilStep("shear control visible", () => shearControl.Alpha > 0);
            AddUntilStep("hidden pool visible", () => hiddenPool.Alpha > 0.9f);
            AddUntilStep("new group visible", () => newGroupButton.Alpha > 0);
        }

        [Test]
        public void TestExitEditModeHidesModLayoutControls()
        {
            LoadSongSelect();

            AddStep("open mod overlay", () => InputManager.Key(Key.F1));
            AddUntilStep("overlay visible", () => modOverlay.State.Value == Visibility.Visible);
            AddUntilStep("columns loaded", () => modOverlay.ChildrenOfType<ModColumn>().Any(c => c.IsLoaded && c.ItemsLoaded));

            enterEditMode();
            AddUntilStep("editing", () => shearControl.Alpha > 0);

            AddStep("done editing", clickEditToggle);
            AddUntilStep("shear hidden", () => shearControl.Alpha == 0);
            AddUntilStep("new group hidden", () => newGroupButton.Alpha == 0);
        }

        [Test]
        public void TestPopInFollowedByImmediateColumnRebuildDoesNotThrow()
        {
            LoadSongSelect();

            AddStep("open mod overlay and rebuild columns immediately", () =>
            {
                modOverlay.Show();
                var alternateMode = modOverlay.LayoutMode == EzModSelectLayoutMode.Horizontal
                    ? EzModSelectLayoutMode.Vertical
                    : EzModSelectLayoutMode.Horizontal;
                modOverlay.SetLayoutMode(alternateMode);
            });

            AddWaitStep("wait for delayed pop-in sample callbacks", 5);
            AddAssert("overlay remains visible without crashing", () => modOverlay.State.Value == Visibility.Visible);
        }

        [Test]
        public void TestZeroShearKeepsHorizontalColumnsAndPanelsVertical()
        {
            LoadSongSelect();

            AddStep("open mod overlay in horizontal mode", () =>
            {
                modOverlay.Show();
                modOverlay.SetLayoutMode(EzModSelectLayoutMode.Horizontal);
            });
            AddUntilStep("overlay visible", () => modOverlay.State.Value == Visibility.Visible);
            AddUntilStep("columns loaded", () => modOverlay.ChildrenOfType<ModColumn>().Any(c => c.IsLoaded && c.ItemsLoaded));

            enterEditMode();
            AddStep("set column shear to 0", () => shearControl.Current.Value = 0);

            AddAssert("column flow shear is zero", () => modOverlay.ChildrenOfType<ModSelectOverlay.ColumnFlowContainer>().Single().Shear == Vector2.Zero);
            AddAssert("all columns have zero shear", () => modOverlay.ChildrenOfType<ModSelectColumn>().All(c => c.Shear == Vector2.Zero));
            AddAssert("all panels have zero shear", () => modOverlay.ChildrenOfType<ModSelectPanel>().All(p => p.Shear == Vector2.Zero));
        }

        private EzUserModSelectOverlay modOverlay => this.ChildrenOfType<EzUserModSelectOverlay>().Single();

        private EzModLayoutShearControl shearControl => modOverlay.ChildrenOfType<EzModLayoutShearControl>().Single();

        private EzModHiddenPool hiddenPool => modOverlay.ChildrenOfType<EzModHiddenPool>().Single();

        private ShearedButton newGroupButton => modOverlay.ChildrenOfType<ShearedButton>()
                                                          .First(b => b.Text.ToString() == EzModLayoutStrings.NEW_GROUP.ToString());

        private ShearedButton editToggleButton => modOverlay.ChildrenOfType<FillFlowContainer>()
                                                            .Single(flow => flow.ChildrenOfType<EzModLayoutShearControl>().Any())
                                                            .ChildrenOfType<ShearedButton>().First();

        private void enterEditMode() => AddStep("enter edit mode", clickEditToggle);

        private void clickEditToggle()
        {
            InputManager.MoveMouseTo(editToggleButton);
            InputManager.Click(MouseButton.Left);
        }
    }
}
