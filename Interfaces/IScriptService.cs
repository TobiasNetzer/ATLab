using System.Collections.ObjectModel;
using System.Threading.Tasks;
using ATLab.ViewModels;

namespace ATLab.Interfaces;

public interface IScriptService
{
    ObservableCollection<ScriptItemViewModel> Scripts { get; }
    Task LoadAllAsync();
    Task SaveAsync(ScriptItemViewModel script);
    Task DeleteAsync(ScriptItemViewModel script);
    ScriptItemViewModel CreateNew();
}
