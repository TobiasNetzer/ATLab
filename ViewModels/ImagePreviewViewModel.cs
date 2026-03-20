using System.IO;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ATLab.ViewModels;

public class ImagePreviewViewModel : ObservableObject
{
    public string Path { get; }
    public Bitmap? Thumbnail { get; }

    public ImagePreviewViewModel(string path)
    {
        Path = path;

        if (File.Exists(path))
            Thumbnail = new Bitmap(path);
    }
}