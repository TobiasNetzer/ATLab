using System.Collections.ObjectModel;
using System.Threading.Tasks;
using ATLab.ViewModels;

namespace ATLab.Interfaces;

public interface IScpiScriptService
{
    ObservableCollection<ScpiScriptItemViewModel> Scripts { get; }
    Task LoadAllAsync();
    Task SaveAsync(ScpiScriptItemViewModel script);
    Task DeleteAsync(ScpiScriptItemViewModel script);
    ScpiScriptItemViewModel CreateNew();
}
