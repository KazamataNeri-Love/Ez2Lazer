// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using System.Numerics;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Testing;
using osu.Game.Graphics.UserInterface;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Overlays;
using osu.Game.Overlays.Settings;

namespace osu.Game.Tests.Visual.UserInterface
{
    public partial class TestSceneMobileNumericControlHook : OsuManualInputManagerTestScene
    {
        [Cached]
        private readonly OverlayColourProvider colourProvider = new OverlayColourProvider(OverlayColourScheme.Aquamarine);

        [TestCase(false)]
        [TestCase(true)]
        public void TestDefaultSliders(bool registerFactory)
        {
            var factory = registerFactory ? new TestFactory(false) : null;
            var form = new FormSliderBar<float> { Current = new BindableFloat { MinValue = 0, MaxValue = 1 } };
            var settings = new SettingsSlider<float> { Current = new BindableFloat { MinValue = 0, MaxValue = 1 } };

            addSliders(form, settings, factory);

            AddAssert("form slider is visible", () => form.ChildrenOfType<FormSliderBar<float>.InnerSlider>().Single().IsPresent);
            AddAssert("settings slider is visible", () => settings.ChildrenOfType<RoundedSliderBar<float>>().Single().IsPresent);
            if (factory != null)
                AddAssert("factory declined both replacements", () => factory.FormContext != null && factory.SettingsContext != null);
        }

        [Test]
        public void TestInjectedSliders()
        {
            var factory = new TestFactory(true);
            var formValue = new BindableFloat(0.25f) { MinValue = 0, MaxValue = 1, Default = 0.25f };
            var settingsValue = new BindableFloat(0.25f) { MinValue = 0, MaxValue = 1, Default = 0.25f };
            var form = new FormSliderBar<float>
            {
                Current = formValue,
                DisplayAsPercentage = true,
                TransferValueOnCommit = true,
                PlaySamplesOnAdjust = false,
                TooltipFormat = _ => "custom",
            };
            var settings = new SettingsSlider<float> { Current = settingsValue };
            int formChanges = 0;
            int settingChanges = 0;
            form.ValueChanged += () => formChanges++;
            settings.SettingChanged += () => settingChanges++;

            addSliders(form, settings, factory);

            AddAssert("form keeps text box and replaces slider in right slot", () =>
                form.ChildrenOfType<FormTextBox.InnerTextBox>().Single().IsLoaded
                && !form.ChildrenOfType<FormSliderBar<float>.InnerSlider>().Any()
                && form.ChildrenOfType<Box>().Contains(factory.FormReplacement)
                && factory.FormReplacement.Anchor == Anchor.CentreRight
                && factory.FormReplacement.Origin == Anchor.CentreRight
                && factory.FormReplacement.RelativeSizeAxes == Axes.X
                && factory.FormReplacement.Width == 0.5f);
            AddAssert("settings factory was called", () => factory.SettingsContext != null);
            AddAssert("settings original slider is hidden", () => !settings.ChildrenOfType<RoundedSliderBar<float>>().Single().IsPresent);
            AddAssert("settings replacement is attached", () => settings.ChildrenOfType<Box>().Contains(factory.SettingsReplacement));
            AddAssert("settings replacement is present", () => factory.SettingsReplacement.IsPresent);
            AddAssert("form context preserves values and options", () =>
                factory.FormContext!.CurrentInstantaneous.Value == 0.25f
                && factory.FormContext.DisplayAsPercentage
                && factory.FormContext.TransferValueOnCommit
                && !factory.FormContext.PlaySamplesOnAdjust
                && factory.FormContext.FormatValue(0.25f).ToString() == "custom");

            AddStep("change slider options after load", () =>
            {
                form.KeyboardStep = 0.125f;
                settings.KeyboardStep = 0.25f;
                settings.DisplayAsPercentage = true;
                settings.TransferValueOnCommit = true;
            });
            AddAssert("context getters track later options", () =>
                factory.FormContext!.GetKeyboardStep() == 0.125f
                && factory.SettingsContext!.GetKeyboardStep() == 0.25f
                && factory.SettingsContext.GetDisplayAsPercentage()
                && factory.SettingsContext.GetTransferValueOnCommit());

            AddStep("edit instantaneous form value", () => factory.FormContext!.CurrentInstantaneous.Value = 0.5f);
            AddAssert("form waits for commit", () => formValue.Value == 0.25f && formChanges == 0);
            AddStep("commit form and edit settings value", () =>
            {
                factory.FormContext!.Commit();
                factory.SettingsContext!.Current.Value = 0.5f;
            });
            AddAssert("bound values and events are updated", () =>
                formValue.Value == 0.5f && formChanges == 1 && settingsValue.Value == 0.5f && settingChanges == 1);

            AddStep("reset both values", () =>
            {
                factory.FormContext!.ResetToDefault();
                settings.ApplyDefault();
            });
            AddAssert("default values and events are restored", () =>
                formValue.IsDefault && formChanges == 2 && settingsValue.IsDefault && settingChanges == 2);

            AddStep("update both source bindables", () =>
            {
                formValue.Value = 0.75f;
                settingsValue.Value = 0.75f;
            });
            AddAssert("replacements observe source updates", () =>
                factory.FormContext!.CurrentInstantaneous.Value == 0.75f && factory.SettingsContext!.Current.Value == 0.75f);

            AddStep("disable both source bindables", () =>
            {
                formValue.Disabled = true;
                settingsValue.Disabled = true;
                factory.FormContext!.ResetToDefault();
            });
            AddAssert("disabled form keeps its value", () => formValue.Value == 0.75f);
            AddAssert("disabled settings slider stays hidden", () => !settings.ChildrenOfType<RoundedSliderBar<float>>().Single().IsPresent);
        }

        private void addSliders(FormSliderBar<float> form, SettingsSlider<float> settings, TestFactory? factory)
        {
            AddStep("create sliders", () => Child = new SliderScope(factory)
            {
                Width = 400,
                AutoSizeAxes = Axes.Y,
                Direction = FillDirection.Vertical,
                Children = new Drawable[] { form, settings },
            });
            AddUntilStep("wait for sliders to load", () => form.IsLoaded && settings.IsLoaded);
        }

        private partial class SliderScope(TestFactory? factory) : FillFlowContainer
        {
            protected override IReadOnlyDependencyContainer CreateChildDependencies(IReadOnlyDependencyContainer parent)
            {
                var dependencies = new DependencyContainer(base.CreateChildDependencies(parent));
                if (factory != null)
                    dependencies.CacheAs<IMobileNumericControlFactory>(factory);

                return dependencies;
            }
        }

        private class TestFactory(bool replace) : IMobileNumericControlFactory
        {
            public FormNumericControlContext<float>? FormContext { get; private set; }
            public SettingsNumericControlContext<float>? SettingsContext { get; private set; }

            public readonly Box FormReplacement = new Box { Height = 20 };
            public readonly Box SettingsReplacement = new Box { RelativeSizeAxes = Axes.X, Height = 20 };

            public Drawable? CreateFormNumericControl<T>(FormNumericControlContext<T> context) where T : struct, INumber<T>, IMinMaxValue<T>
            {
                FormContext = (object)context as FormNumericControlContext<float>;
                return replace ? FormReplacement : null;
            }

            public Drawable? CreateSettingsNumericControl<T>(SettingsNumericControlContext<T> context) where T : struct, INumber<T>, IMinMaxValue<T>
            {
                SettingsContext = (object)context as SettingsNumericControlContext<float>;
                return replace ? SettingsReplacement : null;
            }
        }
    }
}
