using System;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace ATLab.Models;

/// <summary>Shared bitmaps for the pass and fail result icons.</summary>
public static class Icons
{
    public static readonly Bitmap Pass;
    public static readonly Bitmap Fail;

    static Icons()
    {
        Pass = Load("avares://ATLab/Assets/Pass.png");
        Fail = Load("avares://ATLab/Assets/Fail.png");
    }

    private static Bitmap Load(string uri)
    {
        return new Bitmap(AssetLoader.Open(new Uri(uri)));
    }
}
