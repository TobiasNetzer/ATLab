using System.Collections.ObjectModel;
using System.Threading.Tasks;
using ATLab.ViewModels;

namespace ATLab.Interfaces;

/// <summary>Keeps the scripts of the script folder in sync with the Scripting tab.</summary>
public interface IScriptService
{
    ObservableCollection<ScriptViewModel> Scripts { get; }
    Task LoadAllAsync();
    Task SaveAsync(ScriptViewModel script);
    Task DeleteAsync(ScriptViewModel script);
    ScriptViewModel CreateNew();
}