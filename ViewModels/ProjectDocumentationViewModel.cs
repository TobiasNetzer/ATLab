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

    public ProjectDocumentation ProjectDocumentation { get; }

    public ObservableCollection<ImagePreviewViewModel> Images { get; } = new();

    public ProjectDocumentationViewModel(
        IFileDialogService fileDialogService,
        ProjectDocumentation projectDocumentation)
    {
        _fileDialogService = fileDialogService;
        ProjectDocumentation = projectDocumentation;
        
        ProjectDocumentation.ImagePaths.CollectionChanged += (_, __) => LoadImages();
    }

    [ObservableProperty]
    private string? _selectedImagePath;

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

        if (SelectedImagePath == path)
            ClosePreview();
    }

    [RelayCommand]
    private void OpenPreview(ImagePreviewViewModel item)
    {
        SelectedImagePath = item.Path;

        SelectedImage?.Dispose();
        SelectedImage = null;

        if (File.Exists(item.Path))
            SelectedImage = new Bitmap(item.Path);
    }

    [RelayCommand]
    private void ClosePreview()
    {
        SelectedImagePath = null;

        SelectedImage?.Dispose();
        SelectedImage = null;
    }

    private void LoadImages()
    {
        Images.Clear();
        foreach (var path in ProjectDocumentation.ImagePaths.Where(path => File.Exists(path)))
        {
            Images.Add(new ImagePreviewViewModel(path));
        }
    }
}
