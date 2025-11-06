using ATLab.Models;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.Input;
using ATLab.Views;

namespace ATLab.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private CTIAController? _cTIA;

    public MainWindowViewModel(CTIAController cTIA)
    {
        _cTIA = cTIA;
    }

}
