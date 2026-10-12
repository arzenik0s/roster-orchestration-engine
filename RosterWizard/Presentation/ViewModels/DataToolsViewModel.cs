using CommunityToolkit.Mvvm.Input;
using Kronos.RosterWizard.Application.Services;

namespace Kronos.RosterWizard.Presentation.ViewModels;

public partial class DataToolsViewModel(RosterDataTransferService transferService) : ViewModelBase
{
    [RelayCommand]
    private async Task ExportJsonAsync()
    {
        var path = await SaveExportAsync("roster-data", ".json", await transferService.ExportJsonAsync());
        StatusMessage = $"Portable roster data exported to {path}";
    }

    [RelayCommand]
    private async Task ExportCsvAsync()
    {
        var path = await SaveExportAsync("roster-assignments", ".csv", await transferService.ExportCsvAsync());
        StatusMessage = $"Roster CSV exported to {path}";
    }

    [RelayCommand]
    private async Task ImportJsonAsync()
    {
        var file = await FilePicker.Default.PickAsync(new PickOptions
        {
            PickerTitle = "Import roster data",
            FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
            {
                [DevicePlatform.WinUI] = [".json"],
                [DevicePlatform.Android] = ["application/json"],
                [DevicePlatform.iOS] = ["public.json"],
                [DevicePlatform.MacCatalyst] = ["public.json"]
            })
        });
        if (file is null)
        {
            StatusMessage = "Import cancelled.";
            return;
        }

        await using var stream = await file.OpenReadAsync();
        await transferService.ImportJsonAsync(stream);
        StatusMessage = $"Imported roster data from {file.FileName}.";
    }

    private static async Task<string> SaveExportAsync(string baseName, string extension, string contents)
    {
        var directory = Path.Combine(FileSystem.AppDataDirectory, "Exports");
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, $"{baseName}-{DateTime.Now:yyyyMMdd-HHmmss}{extension}");
        await File.WriteAllTextAsync(path, contents);
        return path;
    }
}
