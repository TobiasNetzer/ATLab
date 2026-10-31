using System;
using System.IO;
using Avalonia.Media.Imaging;

namespace ATLab.ViewModels;

/// <summary>A documentation image with a thumbnail that is loaded on first use.</summary>
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