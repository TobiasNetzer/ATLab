using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;

namespace ATLab.Interfaces;

/// <summary>Shows the platform file and folder pickers.</summary>
public interface IFileDialogService
{
    Task<IStorageFile?> OpenFileAsync(string title, IEnumerable<string>? extensions = null);
    Task<IStorageFile?> SaveFileAsync(string title, string suggestedName, string defaultExtension, IEnumerable<string>? extensions = null);
    Task<IStorageFolder?> OpenFolderAsync(string title);
}
