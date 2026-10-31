using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using ATLab.Analyzer.Localization;
using Avalonia.Platform.Storage;

namespace ATLab.Analyzer.Services;

/// <summary>File pickers, kept behind an interface so the view model does not depend on a window.</summary>
public interface IFileDialogs
{
    Task<IReadOnlyList<string>> OpenResultFilesAsync();
    Task<string?> OpenFolderAsync();
    Task<string?> SaveTextFileAsync(string title, string suggestedName);
}

public sealed class StorageFileDialogs(TopLevel topLevel) : IFileDialogs
{
    private static FilePickerFileType JsonFiles => new(Localizer.Get("Dialog_JsonFiles")) { Patterns = ["*.json"] };
    private static FilePickerFileType TextFiles => new(Localizer.Get("Dialog_CsvFiles")) { Patterns = ["*.csv"] };

    public async Task<IReadOnlyList<string>> OpenResultFilesAsync()
    {
        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = Localizer.Get("Dialog_OpenFiles"),
            AllowMultiple = true,
            FileTypeFilter = [JsonFiles, FilePickerFileTypes.All]
        });

        return files.Select(f => f.TryGetLocalPath()).OfType<string>().ToList();
    }

    public async Task<string?> OpenFolderAsync()
    {
        var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = Localizer.Get("Dialog_OpenFolder"),
            AllowMultiple = false
        });

        return folders.Count > 0 ? folders[0].TryGetLocalPath() : null;
    }

    public async Task<string?> SaveTextFileAsync(string title, string suggestedName)
    {
        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = title,
            SuggestedFileName = suggestedName,
            DefaultExtension = "csv",
            FileTypeChoices = [TextFiles]
        });

        return file?.TryGetLocalPath();
    }
}