using Kronos.RosterWizard.Domain;
using Kronos.RosterWizard.Presentation.ViewModels;

namespace Kronos.RosterWizard.Presentation.Pages;

public sealed class RulesPage : ContentPage
{
    private readonly RulesViewModel _viewModel;

    public RulesPage(RulesViewModel viewModel)
    {
        _viewModel = viewModel;
        BindingContext = viewModel;
        Title = "Rules & patterns";
        BackgroundColor = UiKit.Canvas;

        var days = new Stepper { Minimum = 1, Maximum = 62, Increment = 1 };
        days.SetBinding(Stepper.ValueProperty, nameof(RulesViewModel.DayCount));
        var dayOptions = SelectableList(nameof(RulesViewModel.DayOptions), nameof(DaySelectionViewModel.Number), nameof(DaySelectionViewModel.IsSelected), 190);
        var groupPickerForDays = PickerFor<DayGroup>(nameof(RulesViewModel.DayGroups), nameof(DayGroup.Name), nameof(RulesViewModel.SelectedDayGroup), "Day group");
        var groupPickerForShift = PickerFor<DayGroup>(nameof(RulesViewModel.DayGroups), nameof(DayGroup.Name), nameof(RulesViewModel.SelectedDayGroup), "Day group");
        var groupPickerForSequence = PickerFor<DayGroup>(nameof(RulesViewModel.DayGroups), nameof(DayGroup.Name), nameof(RulesViewModel.SelectedDayGroup), "Day group");
        var shiftPicker = PickerFor<ShiftDefinition>(nameof(RulesViewModel.Shifts), nameof(ShiftDefinition.ShiftType), nameof(RulesViewModel.SelectedShift), "Shift type");
        var rulePicker = PickerFor<SkillMixRule>(nameof(RulesViewModel.SkillMixRules), nameof(SkillMixRule.Name), nameof(RulesViewModel.SelectedSkillMixRule), "Skill-mix rule");
        var rolePicker = PickerFor<Role>(nameof(RulesViewModel.Roles), nameof(Role.RoleName), nameof(RulesViewModel.SelectedRole), "Role");
        var sequencePicker = PickerFor<ShiftSequence>(nameof(RulesViewModel.Sequences), nameof(ShiftSequence.Name), nameof(RulesViewModel.SelectedSequence), "Shift sequence");
        var positionShiftPicker = PickerFor<ShiftDefinition>(nameof(RulesViewModel.Shifts), nameof(ShiftDefinition.ShiftType), nameof(RulesViewModel.SelectedPositionShift), "Shift (leave blank for off)");
        var sequenceStaff = SelectableList(nameof(RulesViewModel.SequenceStaffOptions), nameof(SequenceStaffOptionViewModel.Name), nameof(SequenceStaffOptionViewModel.IsSelected), 140);
        var requirementCount = new Stepper { Minimum = 0, Maximum = 25, Increment = 1 };
        requirementCount.SetBinding(Stepper.ValueProperty, nameof(RulesViewModel.RequirementCount));

        var rolesCard = new VerticalStackLayout
        {
            Spacing = 10,
            Children =
            {
                UiKit.Heading("Roles", 18),
                EntryFor(nameof(RulesViewModel.RoleName), "Role name"),
                UiKit.Action("Add role", viewModel.AddRoleCommand),
                DeletableList(nameof(RulesViewModel.Roles), nameof(Role.RoleName), nameof(RulesViewModel.DeleteRoleCommand), 140)
            }
        };

        var dayPatternCard = new VerticalStackLayout
        {
            Spacing = 10,
            Children =
            {
                UiKit.Heading("Roster-day pattern", 18),
                UiKit.Caption("The number of day records determines the generated roster length. Each day group maps shifts to selected day numbers."),
                new HorizontalStackLayout { Spacing = 8, Children = { UiKit.Caption("Days"), days, UiKit.Secondary("Save length", viewModel.SaveDaysCommand) } },
                EntryFor(nameof(RulesViewModel.DayGroupName), "New day group name"),
                UiKit.Action("Add day group (all days)", viewModel.AddDayGroupCommand),
                groupPickerForDays,
                dayOptions,
                UiKit.Secondary("Save selected days", viewModel.SaveDayGroupDaysCommand),
                DeletableList(nameof(RulesViewModel.DayGroups), nameof(DayGroup.Name), nameof(RulesViewModel.DeleteDayGroupCommand), 120)
            }
        };

        var shiftsCard = new VerticalStackLayout
        {
            Spacing = 10,
            Children =
            {
                UiKit.Heading("Shift types", 18),
                EntryFor(nameof(RulesViewModel.ShiftType), "Shift name (e.g. Day, Evening, Night)"),
                groupPickerForShift,
                UiKit.Action("Add shift", viewModel.AddShiftCommand),
                DeletableList(nameof(RulesViewModel.Shifts), nameof(ShiftDefinition.ShiftType), nameof(RulesViewModel.DeleteShiftCommand), 130)
            }
        };

        var mixCard = new VerticalStackLayout
        {
            Spacing = 10,
            Children =
            {
                UiKit.Heading("Skill-mix rules", 18),
                UiKit.Caption("Each rule specifies exact counts for roles. A shift may have multiple alternatives; the solver selects one per shift slot."),
                EntryFor(nameof(RulesViewModel.SkillMixRuleName), "Rule name"),
                shiftPicker,
                UiKit.Action("Add rule for shift", viewModel.AddSkillMixRuleCommand),
                DeletableList(nameof(RulesViewModel.SkillMixRules), nameof(SkillMixRule.Name), nameof(RulesViewModel.DeleteSkillMixRuleCommand), 130),
                rulePicker,
                new HorizontalStackLayout
                {
                    Spacing = 8,
                    Children = { rolePicker, UiKit.Caption("Required count"), requirementCount, UiKit.Secondary("Save role count", viewModel.AddRequirementCommand) }
                },
                DeletableList(nameof(RulesViewModel.Requirements), nameof(SkillMixRequirementItem.DisplayText), nameof(RulesViewModel.DeleteRequirementCommand), 110)
            }
        };

        var sequencesCard = new VerticalStackLayout
        {
            Spacing = 10,
            Children =
            {
                UiKit.Heading("Shift sequences", 18),
                UiKit.Caption("A sequence is an ordered set of shifts/off positions for selected staff. Repeating the entire sequence is forbidden."),
                EntryFor(nameof(RulesViewModel.SequenceName), "Sequence name"),
                groupPickerForSequence,
                UiKit.Caption("Apply to staff"),
                sequenceStaff,
                UiKit.Action("Add sequence", viewModel.AddSequenceCommand),
                DeletableList(nameof(RulesViewModel.Sequences), nameof(ShiftSequence.Name), nameof(RulesViewModel.DeleteSequenceCommand), 120),
                sequencePicker,
                positionShiftPicker,
                UiKit.Secondary("Append position (blank shift = off)", viewModel.AddSequencePositionCommand),
                DeletableList(nameof(RulesViewModel.SequencePositions), nameof(ShiftSequencePositionItem.DisplayText), nameof(RulesViewModel.DeleteSequencePositionCommand), 130)
            }
        };

        var status = new Label { TextColor = UiKit.Accent, FontSize = 13 };
        status.SetBinding(Label.TextProperty, nameof(RulesViewModel.StatusMessage));
        Content = new ScrollView
        {
            BackgroundColor = UiKit.Canvas,
            Content = new VerticalStackLayout
            {
                Padding = 24,
                Spacing = 14,
                Children =
                {
                    UiKit.Heading("Roster rules & patterns", 26),
                    UiKit.Caption("Set up roles, day groups, shifts, exact skill mix, and repeating shift-sequence constraints."),
                    status,
                    UiKit.Card(rolesCard),
                    UiKit.Card(dayPatternCard),
                    UiKit.Card(shiftsCard),
                    UiKit.Card(mixCard),
                    UiKit.Card(sequencesCard)
                }
            }
        };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadAsync();
    }

    private Entry EntryFor(string property, string placeholder)
    {
        var entry = new Entry { Placeholder = placeholder };
        entry.SetBinding(Entry.TextProperty, property);
        return entry;
    }

    private Picker PickerFor<T>(string source, string display, string selection, string title)
    {
        var picker = new Picker { Title = title };
        picker.SetBinding(Picker.ItemsSourceProperty, source);
        picker.ItemDisplayBinding = new Binding(display);
        picker.SetBinding(Picker.SelectedItemProperty, selection);
        return picker;
    }

    private CollectionView SelectableList(string source, string labelPath, string selectedPath, double height)
    {
        var list = new CollectionView
        {
            HeightRequest = height,
            ItemTemplate = new DataTemplate(() =>
            {
                var checkBox = new CheckBox();
                checkBox.SetBinding(CheckBox.IsCheckedProperty, selectedPath);
                var label = new Label { VerticalTextAlignment = TextAlignment.Center };
                label.SetBinding(Label.TextProperty, labelPath);
                return new HorizontalStackLayout { Spacing = 8, Children = { checkBox, label } };
            })
        };
        list.SetBinding(ItemsView.ItemsSourceProperty, source);
        return list;
    }

    private CollectionView DeletableList(string source, string labelPath, string commandName, double height)
    {
        var list = new CollectionView
        {
            HeightRequest = height,
            ItemTemplate = new DataTemplate(() =>
            {
                var label = new Label { VerticalTextAlignment = TextAlignment.Center, FontSize = 13 };
                label.SetBinding(Label.TextProperty, labelPath);
                var remove = UiKit.Secondary("Remove");
                remove.Command = ResolveCommand(commandName);
                remove.SetBinding(Button.CommandParameterProperty, new Binding("."));
                var row = new Grid
                {
                    ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) },
                    ColumnSpacing = 8,
                    Padding = new Thickness(2, 4)
                };
                row.Add(label, 0, 0);
                row.Add(remove, 1, 0);
                return row;
            })
        };
        list.SetBinding(ItemsView.ItemsSourceProperty, source);
        return list;
    }

    private System.Windows.Input.ICommand ResolveCommand(string commandName) => commandName switch
    {
        nameof(RulesViewModel.DeleteRoleCommand) => _viewModel.DeleteRoleCommand,
        nameof(RulesViewModel.DeleteDayGroupCommand) => _viewModel.DeleteDayGroupCommand,
        nameof(RulesViewModel.DeleteShiftCommand) => _viewModel.DeleteShiftCommand,
        nameof(RulesViewModel.DeleteSkillMixRuleCommand) => _viewModel.DeleteSkillMixRuleCommand,
        nameof(RulesViewModel.DeleteRequirementCommand) => _viewModel.DeleteRequirementCommand,
        nameof(RulesViewModel.DeleteSequenceCommand) => _viewModel.DeleteSequenceCommand,
        nameof(RulesViewModel.DeleteSequencePositionCommand) => _viewModel.DeleteSequencePositionCommand,
        _ => throw new InvalidOperationException($"Unknown rules command '{commandName}'.")
    };
}
