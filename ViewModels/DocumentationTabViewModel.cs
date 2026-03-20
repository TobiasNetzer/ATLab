using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ATLab.Interfaces;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ATLab.ViewModels;

public partial class DocumentationTabViewModel : ViewModelBase
{
    private readonly IFileDialogService _fileDialogService;

    public DocumentationTabViewModel(IFileDialogService fileDialogService)
    {
        _fileDialogService = fileDialogService;
        Title = "Documentation";
    }

    // List of image file paths (strings)
    public ObservableCollection<ImagePreviewViewModel> Images { get; } = new();


    // The selected image path (string)
    [ObservableProperty]
    private string? selectedImagePath;

    // The actual Bitmap used for preview
    [ObservableProperty]
    private Bitmap? selectedImage;

    // Add image
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

        }
    }

    // Remove image
    [RelayCommand]
    private void RemoveImage(string path)
    {
        Images.Remove(Images.First(i => i.Path == path));


        // If the removed image is currently shown, close preview
        if (SelectedImagePath == path)
            ClosePreview();
    }

    // Open preview
    [RelayCommand]
    private void OpenPreview(ImagePreviewViewModel item)
    {
        if (item is null)
            return;

        SelectedImagePath = item.Path;

        SelectedImage?.Dispose();
        SelectedImage = null;

        if (File.Exists(item.Path))
            SelectedImage = new Bitmap(item.Path);
    }



    // Close preview
    [RelayCommand]
    private void ClosePreview()
    {
        SelectedImagePath = null;

        SelectedImage?.Dispose();
        SelectedImage = null;
    }
}