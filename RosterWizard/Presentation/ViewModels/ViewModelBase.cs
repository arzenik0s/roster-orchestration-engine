using CommunityToolkit.Mvvm.ComponentModel;

namespace Kronos.RosterWizard.Presentation.ViewModels;

public abstract partial class ViewModelBase : ObservableObject
{
    [ObservableProperty]
    private string statusMessage = "";
}
