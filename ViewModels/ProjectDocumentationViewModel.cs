using System.Collections.ObjectModel;
using System.Collections.Specialized;
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
    private readonly IAttachmentLauncherService _attachmentLauncherService;
    private readonly IErrorService _errorService;

    public ProjectDocumentation ProjectDocumentation { get; }

    public ObservableCollection<ImagePreviewViewModel> Images { get; } = new();
    
    public ProjectDocumentationViewModel(
        IFileDialogService fileDialogService,
        ProjectModel projectModel,
        IAttachmentLauncherService attachmentLauncherService,
        IErrorService errorService)
    {
        _fileDialogService = fileDialogService;
        ProjectDocumentation = projectModel.Documentation;
        _attachmentLauncherService = attachmentLauncherService;
        _errorService = errorService;
        
        ProjectDocumentation.ImagePaths.CollectionChanged += OnImagePathsChanged;
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
            ProjectDocumentation.ImagePaths.Add(path);
        }
    }

    [RelayCommand]
    private void RemoveImage(string path)
    {
        ProjectDocumentation.ImagePaths.Remove(path);
    }

    [RelayCommand]
    private void OpenPreview(ImagePreviewViewModel item)
    {
        SelectedImage?.Dispose();
        SelectedImage = null;

        if (File.Exists(item.Path))
            SelectedImage = new Bitmap(item.Path);
    }

    [RelayCommand]
    private void ClosePreview()
    {
        SelectedImage?.Dispose();
        SelectedImage = null;
    }

    private void OnImagePathsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                foreach (string path in e.NewItems!)
                    AddImageVm(path);
                break;

            case NotifyCollectionChangedAction.Remove:
                foreach (string path in e.OldItems!)
                    RemoveImageVm(path);
                break;

            case NotifyCollectionChangedAction.Replace:
                foreach (string oldPath in e.OldItems!)
                    RemoveImageVm(oldPath);
                foreach (string newPath in e.NewItems!)
                    AddImageVm(newPath);
                break;

            case NotifyCollectionChangedAction.Reset:
                Images.Clear();
                foreach (var path in ProjectDocumentation.ImagePaths)
                    AddImageVm(path);
                break;
        }
    }

    private void AddImageVm(string path)
    {
        if (!File.Exists(path))
            _errorService.AddError($"Image not found: {path}");

        Images.Add(new ImagePreviewViewModel(path));
    }

    private void RemoveImageVm(string path)
    {
        var vm = Images.FirstOrDefault(i => i.Path == path);
        if (vm == null) return;
        vm.Dispose();
        Images.Remove(vm);
    }
    
    [RelayCommand]
    private async Task AddAttachment()
    {
        var file = await _fileDialogService.OpenFileAsync(
            "Select File",
            new[] { "*" }
        );

        if (file != null)
        {
            var path = file.Path.LocalPath;
            ProjectDocumentation.Attachments.Add(new CustomAttachment { Path = path });
        }
    }
    
    [RelayCommand]
    private void RemoveAttachment(CustomAttachment entry)
    {
        ProjectDocumentation.Attachments.Remove(entry);
    }
    
    [RelayCommand]
    private async Task OpenAttachment(CustomAttachment entry)
    {
        if (!File.Exists(entry.Path))
            _errorService.AddError($"File not found: {entry.Path}");
        
        await _attachmentLauncherService.OpenAttachmentAsync(entry.Path);
    }
}