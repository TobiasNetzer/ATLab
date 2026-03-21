using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ATLab.Interfaces;
using ATLab.Models;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ATLab.ViewModels;

public partial class ProjectDocumentationViewModel : ViewModelBase
{
    private readonly IFileDialogService _fileDialogService;
    private readonly IErrorService _errorService;

    public ProjectDocumentation ProjectDocumentation { get; }

    public ObservableCollection<ImagePreviewViewModel> Images { get; } = new();

    public ProjectDocumentationViewModel(
        IFileDialogService fileDialogService,
        ProjectDocumentation projectDocumentation,
        IErrorService errorService)
    {
        _fileDialogService = fileDialogService;
        ProjectDocumentation = projectDocumentation;
        _errorService = errorService;
        
        ProjectDocumentation.ImagePaths.CollectionChanged += (_, __) => LoadImages();
    }

    [ObservableProperty]
    private Bitmap? _selectedImage;

    [RelayCommand]
    private async Task AddImage()
    {
        var file = await _fileDialogService.OpenFileAsync(
            "Select Image",
            new[] { "png", "jpg", "jpeg", "bmp", "gif", "webp" });

        if (file != null)
        {
            var path = file.Path.LocalPath;

            Images.Add(new ImagePreviewViewModel(path));
            ProjectDocumentation.ImagePaths.Add(path);
        }
    }

    [RelayCommand]
    private void RemoveImage(string path)
    {
        var item = Images.First(i => i.Path == path);
        Images.Remove(item);
        ProjectDocumentation.ImagePaths.Remove(path);
    }

    [RelayCommand]
    private void OpenPreview(ImagePreviewViewModel item)
    {
        SelectedImage?.Dispose();
        SelectedImage = null;

        if (File.Exists(item.Path))
            SelectedImage = item.Thumbnail;
    }

    [RelayCommand]
    private void ClosePreview()
    {
        SelectedImage?.Dispose();
        SelectedImage = null;
    }

    private void LoadImages()
    {
        Images.Clear();

        foreach (var path in ProjectDocumentation.ImagePaths)
        {
            if (!File.Exists(path))
                _errorService.AddError($"Image not found: {path}");

            Images.Add(new ImagePreviewViewModel(path));
        }
    }
}
