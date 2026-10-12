using Kronos.RosterWizard.Presentation.ViewModels;

namespace Kronos.RosterWizard.Presentation.Pages;

public sealed class DataToolsPage : ContentPage
{
    private readonly DataToolsViewModel _viewModel;

    public DataToolsPage(DataToolsViewModel viewModel)
    {
        _viewModel = viewModel;
        BindingContext = viewModel;
        Title = "Data import / export";
        BackgroundColor = UiKit.Canvas;

        var jsonCard = new VerticalStackLayout
        {
            Spacing = 12,
            Children =
            {
                UiKit.Heading("Portable data", 18),
                UiKit.Caption("All central roster entities are stored together as versioned JSON. Export this file to transform/load it into your PostgreSQL schema later."),
                UiKit.Action("Export complete JSON snapshot", viewModel.ExportJsonCommand)
            }
        };

        var importButton = UiKit.Secondary("Import JSON snapshot");
        importButton.Clicked += async (_, _) =>
        {
            var confirmed = await DisplayAlertAsync(
                "Replace local roster data?",
                "Import replaces all roster data currently stored on this device. Export a backup first if you need to keep it.",
                "Choose file",
                "Cancel");
            if (confirmed)
            {
                await viewModel.ImportJsonCommand.ExecuteAsync(null);
            }
        };

        var csvCard = new VerticalStackLayout
        {
            Spacing = 12,
            Children =
            {
                UiKit.Heading("Roster export", 18),
                UiKit.Caption("Export the generated or manually edited assignments as CSV, one row per assigned staff member and shift slot."),
                UiKit.Action("Export roster assignments CSV", viewModel.ExportCsvCommand)
            }
        };

        var status = new Label { TextColor = UiKit.Accent, FontSize = 13 };
        status.SetBinding(Label.TextProperty, nameof(DataToolsViewModel.StatusMessage));
        Content = new ScrollView
        {
            BackgroundColor = UiKit.Canvas,
            Content = new VerticalStackLayout
            {
                Padding = 24,
                Spacing = 16,
                Children =
                {
                    UiKit.Heading("Data tools", 26),
                    UiKit.Caption("Local-first storage keeps all roster configuration, requests, assignments and rules in one portable dataset."),
                    UiKit.Card(jsonCard),
                    UiKit.Card(new VerticalStackLayout { Spacing = 12, Children = { UiKit.Heading("Restore", 18), importButton } }),
                    UiKit.Card(csvCard),
                    status,
                    UiKit.Caption($"Data file: {Path.Combine(FileSystem.AppDataDirectory, "roster-data.json")}"),
                    UiKit.Caption($"Exports folder: {Path.Combine(FileSystem.AppDataDirectory, "Exports")}")
                }
            }
        };
    }
}
