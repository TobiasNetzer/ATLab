using System.Linq;
using System.Threading.Tasks;
using ATLab.FixtureDesigner.Fixture;
using ATLab.Pcb.Fixture;
using ATLab.FixtureDesigner.Localization;
using Avalonia.Controls;
using Avalonia.Platform.Storage;

namespace ATLab.FixtureDesigner.Services;

/// <summary>File pickers, kept behind an interface so the view model does not depend on a window.</summary>
public interface IDialogs
{
    /// <summary>A fixture or a KiCad board.</summary>
    Task<string?> OpenFileAsync();

    Task<string?> OpenBoardAsync();
    Task<string?> SaveFixtureAsync(string suggestedName);
    Task<string?> SavePdfAsync(string suggestedName);
    Task<string?> OpenFolderAsync(string title);

    /// <summary>A DXF or SVG file with the outline of the probe plates.</summary>
    Task<string?> OpenOutlineAsync(bool svg);
}

public sealed class WindowDialogs(TopLevel topLevel) : IDialogs
{
    private static FilePickerFileType FixtureFiles =>
        new(Localizer.Get("Dialog_FixtureFiles")) { Patterns = ["*" + FixtureFile.Extension] };

    private static FilePickerFileType BoardFiles => new(Localizer.Get("Dialog_BoardFiles")) { Patterns = ["*.kicad_pcb"] };

    private static FilePickerFileType SupportedFiles =>
        new(Localizer.Get("Dialog_SupportedFiles")) { Patterns = ["*" + FixtureFile.Extension, "*.kicad_pcb"] };

    private static FilePickerFileType PdfFiles => new(Localizer.Get("Dialog_PdfFiles")) { Patterns = ["*.pdf"] };

    public Task<string?> OpenFileAsync() =>
        OpenAsync(Localizer.Get("Dialog_Open"), SupportedFiles, FixtureFiles, BoardFiles);

    public Task<string?> OpenBoardAsync() => OpenAsync(Localizer.Get("Dialog_OpenBoard"), BoardFiles);

    public Task<string?> SaveFixtureAsync(string suggestedName) =>
        SaveAsync(Localizer.Get("Dialog_SaveFixture"), suggestedName, FixtureFile.Extension.TrimStart('.'), FixtureFiles);

    public Task<string?> SavePdfAsync(string suggestedName) =>
        SaveAsync(Localizer.Get("Dialog_SaveBuildSheet"), suggestedName, "pdf", PdfFiles);

    public Task<string?> OpenOutlineAsync(bool svg) =>
        OpenAsync(Localizer.Get("Dialog_OpenOutline"), svg
            ? new FilePickerFileType(Localizer.Get("Dialog_SvgFiles")) { Patterns = ["*.svg"] }
            : new FilePickerFileType(Localizer.Get("Dialog_DxfFiles")) { Patterns = ["*.dxf"] });

    public async Task<string?> OpenFolderAsync(string title)
    {
        var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions { Title = title, AllowMultiple = false });
        return folders.Count > 0 ? folders[0].TryGetLocalPath() : null;
    }

    private async Task<string?> OpenAsync(string title, params FilePickerFileType[] types)
    {
        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = title,
            AllowMultiple = false,
            FileTypeFilter = [.. types, FilePickerFileTypes.All]
        });

        return files.Select(f => f.TryGetLocalPath()).FirstOrDefault(p => p is not null);
    }

    private async Task<string?> SaveAsync(string title, string suggestedName, string extension, FilePickerFileType type)
    {
        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = title,
            SuggestedFileName = suggestedName,
            DefaultExtension = extension,
            FileTypeChoices = [type],
            ShowOverwritePrompt = true
        });

        return file?.TryGetLocalPath();
    }
}