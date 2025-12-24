using ATLab.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ATLab.ViewModels;

public partial class ConfigTabViewModel : ViewModelBase
{
    
    public TestConfigurationViewModel TestConfiguration { get; set; }

    public ConfigTabViewModel(TestConfigurationViewModel testConfigurationViewModel)
    {
        TestConfiguration = testConfigurationViewModel;
        
        Title = "Config";
    }
}