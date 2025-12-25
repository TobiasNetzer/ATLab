using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using ATLab.Interfaces;

namespace ATLab.Services;

public class FileDialogService : IFileDialogService
{
    private IStorageProvider? GetStorageProvider()
    {
        var desktop = Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;
        return desktop?.MainWindow?.StorageProvider;
    }

    public async Task<IStorageFile?> OpenFileAsync(string title, IEnumerable<string>? extensions = null)
    {
        var storageProvider = GetStorageProvider();
        if (storageProvider == null) return null;

        var options = new FilePickerOpenOptions
        {
            Title = title,
            AllowMultiple = false
        };

        if (extensions != null)
        {
            options.FileTypeFilter = new List<FilePickerFileType>
            {
                new FilePickerFileType(title) { Patterns = extensions.Select(e => $"*.{e}").ToList() }
            };
        }

        var results = await storageProvider.OpenFilePickerAsync(options);
        return results.Count > 0 ? results[0] : null;
    }

    public async Task<IStorageFile?> SaveFileAsync(string title, string suggestedName, string defaultExtension, IEnumerable<string>? extensions = null)
    {
        var storageProvider = GetStorageProvider();
        if (storageProvider == null) return null;

        var options = new FilePickerSaveOptions
        {
            Title = title,
            SuggestedFileName = suggestedName,
            DefaultExtension = defaultExtension
        };

        if (extensions != null)
        {
            options.FileTypeChoices = new List<FilePickerFileType>
            {
                new FilePickerFileType(title) { Patterns = extensions.Select(e => $"*.{e}").ToList() }
            };
        }

        return await storageProvider.SaveFilePickerAsync(options);
    }
}
