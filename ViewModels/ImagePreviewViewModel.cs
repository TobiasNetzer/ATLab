using System;
using System.IO;
using Avalonia.Media.Imaging;

namespace ATLab.ViewModels;

public class ImagePreviewViewModel : IDisposable
{
    private Bitmap? _thumbnail;

    public string Path { get; }

    public ImagePreviewViewModel(string path)
    {
        Path = path;
    }
    
    public Bitmap Thumbnail
    {
        get
        {
            if (_thumbnail != null)
                return _thumbnail;

            if (!File.Exists(Path))
                return null!;

            using var stream = File.OpenRead(Path);
            
            _thumbnail = Bitmap.DecodeToWidth(stream, 256);

            return _thumbnail;
        }
    }

    public void Dispose()
    {
        _thumbnail?.Dispose();
        _thumbnail = null;
    }
}