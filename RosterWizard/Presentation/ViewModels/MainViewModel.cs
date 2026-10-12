using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using Kronos.RosterWizard.Application.Services;
using Kronos.RosterWizard.Infrastructure;
using Microsoft.Extensions.Logging;

namespace Kronos.RosterWizard.Presentation.ViewModels;

public partial class MainViewModel(
    IRosterRepository repository,
    IRosterGenerationService generationService,
    ILogger<MainViewModel> logger) : ViewModelBase
{
    [ObservableProperty] private DateTime startDate = DateTime.Today;
    [ObservableProperty] private string rosterName = "";
    [ObservableProperty] private string summary = "Loading roster data…";
    [ObservableProperty] private bool isBusy;

    [RelayCommand]
    public async Task RefreshAsync()
    {
        var data = await repository.GetSnapshotAsync();
        RosterName = data.Settings.RosterName;
        var assignedCount = data.Slots.Sum(slot => slot.StaffIds.Count);
        var rosterPeriod = data.Slots.Count == 0
            ? "No roster has been generated yet."
            : $"{data.Slots.Count} shift slots • {assignedCount} assignments";
        Summary = $"{data.Staff.Count} staff · {data.Roles.Count} roles · {data.Shifts.Count} shift types\n{rosterPeriod}";
    }

    [RelayCommand]
    private async Task GenerateAsync()
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        StatusMessage = "Solving roster…";
        try
        {
            GenerationResult? result = null;
            var date = DateOnly.FromDateTime(StartDate);
            await Task.Run(() => repository.UpdateAsync(data => result = generationService.Generate(data, date)));
            StatusMessage = $"Roster generated for {result!.StartDate:dd MMM}–{result.EndDate:dd MMM yyyy} ({result.SolverStatus}, request score {result.RequestScore}).";
            await RefreshAsync();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Roster generation failed for start date {StartDate}.", StartDate);
            StatusMessage = $"Roster generation failed: {exception.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
