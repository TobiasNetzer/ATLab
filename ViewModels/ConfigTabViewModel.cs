using ATLab.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ATLab.ViewModels;

public partial class ConfigTabViewModel : ViewModelBase
{
    
    public TestConfigurationViewModel TestConfiguration { get; set; }
    
    [ObservableProperty]
    private string _tolerance = "10";

    public ConfigTabViewModel(TestConfigurationViewModel testConfigurationViewModel)
    {
        TestConfiguration = testConfigurationViewModel;

        Title = "Config";
    }

    partial void OnToleranceChanged(string value)
    {
        if (double.TryParse(value, out var tolerance))
        {
            TestConfiguration.TestStepConfiguratorViewModel.Tolerance = tolerance / 100.0;
        }
    }
}