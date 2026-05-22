using System.Threading.Tasks;
using ATLab.Interfaces;
using ATLab.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ATLab.ViewModels;

public partial class FilePathEditorViewModel : ViewModelBase
{
    private readonly ISettingsService _settingsService;
    private readonly IFileDialogService _fileDialogService;
    
    [ObservableProperty]
    private bool _isExpanded;
    
    [ObservableProperty]
    private TestStep? _testStep;

    public FilePathEditorViewModel(ISettingsService settingsService,
        IFileDialogService fileDialogService)
    {
        _settingsService = settingsService;
        _fileDialogService = fileDialogService;
        
        IsExpanded = settingsService.Settings.IsFilePathEditorExpanded;
    }
    
    partial void OnIsExpandedChanged(bool value)
    {
        _settingsService.Settings.IsFilePathEditorExpanded = value;
    }

    public void LoadTestStep(TestStep testStep)
    {
        TestStep = testStep;
    }
    
    [RelayCommand]
    private async Task SelectFilePath()
    {
        var result = await _fileDialogService.OpenFileAsync("Select file");
        if (result == null) return;
        
        TestStep?.FilePath = result.Path.LocalPath;
    }
}