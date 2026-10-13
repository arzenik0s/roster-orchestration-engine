using System.Linq;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Kronos.RosterWizard.Domain;
using Kronos.RosterWizard.Presentation.ViewModels;

namespace Kronos.RosterWizard.Presentation.Pages;

public sealed class StaffPage : ContentPage
{
    private readonly StaffViewModel _viewModel;

    public StaffPage(StaffViewModel viewModel)
    {
        _viewModel = viewModel;
        BindingContext = viewModel;
        Title = "Staff";
        BackgroundColor = UiKit.Canvas;

        var staffList = new CollectionView
        {
            SelectionMode = SelectionMode.Single,
            ItemTemplate = new DataTemplate(() =>
            {
                var name = new Label { FontAttributes = FontAttributes.Bold, TextColor = UiKit.Ink };
                name.SetBinding(Label.TextProperty, nameof(StaffMember.FirstName));
                var surname = new Label { TextColor = UiKit.Ink };
                surname.SetBinding(Label.TextProperty, nameof(StaffMember.LastName));
                var email = new Label { FontSize = 12, TextColor = UiKit.Muted };
                email.SetBinding(Label.TextProperty, nameof(StaffMember.Email));
                return new Border
                {
                    Stroke = UiKit.Line,
                    StrokeThickness = 1,
                    Padding = 10,
                    Margin = new Thickness(0, 0, 0, 6),
                    Content = new VerticalStackLayout
                    {
                        Spacing = 1,
                        Children = { name, surname, email }
                    }
                };
            })
        };
        staffList.SetBinding(ItemsView.ItemsSourceProperty, nameof(StaffViewModel.Staff));
        staffList.SelectionChanged += (_, args) =>
        {
            viewModel.SelectedStaff = args.CurrentSelection.FirstOrDefault() as StaffMember;
        };

        var roles = new CollectionView
        {
            HeightRequest = 150,
            ItemTemplate = new DataTemplate(() =>
            {
                var checkBox = new CheckBox();
                checkBox.SetBinding(CheckBox.IsCheckedProperty, nameof(RoleSelectionViewModel.IsSelected));
                var label = new Label { VerticalTextAlignment = TextAlignment.Center };
                label.SetBinding(Label.TextProperty, nameof(RoleSelectionViewModel.Name));
                return new HorizontalStackLayout { Spacing = 8, Children = { checkBox, label } };
            })
        };
        roles.SetBinding(ItemsView.ItemsSourceProperty, nameof(StaffViewModel.RoleOptions));

        var form = new VerticalStackLayout
        {
            Spacing = 10,
            Children =
            {
                UiKit.Heading("Staff details", 18),
                EntryFor(nameof(StaffViewModel.FirstName), "First name"),
                EntryFor(nameof(StaffViewModel.LastName), "Last name"),
                EntryFor(nameof(StaffViewModel.Email), "Email", Keyboard.Email),
                new HorizontalStackLayout
                {
                    Spacing = 8,
                    Children =
                    {
                        UiKit.Caption("Shifts per roster"),
                        new Entry { Keyboard = Keyboard.Numeric, WidthRequest = 100 }
                    }
                },
                roles,
                CheckOption("Available for roster generation", nameof(StaffViewModel.IsAvailable)),
                CheckOption("Enforce shift target (leave-adjusted)", nameof(StaffViewModel.EnforceShiftsPerRoster)),
                CheckOption("At most one shift per day", nameof(StaffViewModel.EnforceOneShiftPerDay)),
                CheckOption("Round leave-adjusted target up (otherwise down)", nameof(StaffViewModel.MaxShifts)),
                new HorizontalStackLayout
                {
                    Spacing = 8,
                    Children =
                    {
                        UiKit.Action("Save staff member", viewModel.SaveStaffCommand),
                        UiKit.Secondary("New", viewModel.NewStaffCommand),
                        UiKit.Secondary("Delete selected", viewModel.DeleteSelectedCommand)
                    }
                },
                new Label { TextColor = UiKit.Accent, FontSize = 13 }
            }
        };
        var targetShiftsEntry = (Entry)((HorizontalStackLayout)form.Children[4]).Children[1];
        targetShiftsEntry.SetBinding(Entry.TextProperty, nameof(StaffViewModel.ShiftsPerRoster));
        targetShiftsEntry.TextChanged += (_, args) =>
        {
            if (int.TryParse(args.NewTextValue, out var value))
            {
                viewModel.ShiftsPerRoster = value;
            }
        };
        ((Label)form.Children[form.Children.Count - 1]).SetBinding(Label.TextProperty, nameof(StaffViewModel.StatusMessage));

        var contentGrid = new Grid
        {
            ColumnDefinitions = { new ColumnDefinition(new GridLength(1, GridUnitType.Star)), new ColumnDefinition(new GridLength(2, GridUnitType.Star)) },
            ColumnSpacing = 16
        };
        contentGrid.Add(UiKit.Card(staffList), 0, 0);
        contentGrid.Add(UiKit.Card(form), 1, 0);

        Content = new ScrollView
        {
            BackgroundColor = UiKit.Canvas,
            Content = new VerticalStackLayout
            {
                Padding = 24,
                Spacing = 16,
                Children =
                {
                    UiKit.Heading("Staff management", 26),
                    UiKit.Caption("Staff may hold multiple roles. Availability and shift-count preferences feed the roster solver."),
                    contentGrid
                }
            }
        };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadAsync();
    }

    private Entry EntryFor(string property, string placeholder, Keyboard? keyboard = null)
    {
        var entry = new Entry
        {
            Placeholder = placeholder,
            Keyboard = keyboard ?? Keyboard.Default,
            BackgroundColor = Colors.White
        };
        entry.SetBinding(Entry.TextProperty, property);
        return entry;
    }

    private View CheckOption(string labelText, string property)
    {
        var checkBox = new CheckBox();
        checkBox.SetBinding(CheckBox.IsCheckedProperty, property);
        return new HorizontalStackLayout
        {
            Spacing = 8,
            Children = { checkBox, new Label { Text = labelText, VerticalTextAlignment = TextAlignment.Center } }
        };
    }
}
