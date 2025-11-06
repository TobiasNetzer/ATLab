using ATLab.Models;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.Input;
using ATLab.Views;
using System.Threading.Tasks;

namespace ATLab.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private CTIAController? _cTIA;

    public IRelayCommand OpenAboutWindowCommand { get; }

    public MainWindowViewModel(CTIAController cTIA)
    {
        _cTIA = cTIA;
        OpenAboutWindowCommand = new AsyncRelayCommand(OpenAboutWindow);
    }

    public async Task OpenAboutWindow()
    {
        var aboutVM = new AboutWindowViewModel(_cTIA!);
        var aboutWindow = new AboutWindow
        {
            DataContext = aboutVM
        };

        var desktop = Avalonia.Application.Current?.ApplicationLifetime 
            as IClassicDesktopStyleApplicationLifetime;

        if (desktop?.MainWindow != null)
            await aboutWindow.ShowDialog(desktop.MainWindow);
        else
            aboutWindow.Show();
    }

}
