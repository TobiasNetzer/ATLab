using System.Collections.ObjectModel;
using System.Threading.Tasks;
using ATLab.ViewModels;

namespace ATLab.Interfaces;

public interface IScriptService
{
    ObservableCollection<ScriptViewModel> Scripts { get; }
    Task LoadAllAsync();
    Task SaveAsync(ScriptViewModel script);
    Task DeleteAsync(ScriptViewModel script);
    ScriptViewModel CreateNew();
}