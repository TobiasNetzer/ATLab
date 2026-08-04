using System;
using System.Collections.ObjectModel;
using ATLab.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ATLab.Models;

public partial class ProjectDocumentation : ObservableObject
{
    public event Action? DocumentationChanged;

    [ObservableProperty]
    private string _testDocumentation = string.Empty;

    partial void OnTestDocumentationChanged(string value) => DocumentationChanged?.Invoke();

    [ObservableProperty]
    private string _requiredGearDocumentation = string.Empty;

    partial void OnRequiredGearDocumentationChanged(string value) => DocumentationChanged?.Invoke();

    [ObservableProperty]
    private string _knownIssuesDocumentation = string.Empty;

    partial void OnKnownIssuesDocumentationChanged(string value) => DocumentationChanged?.Invoke();

    public ObservableCollection<string> ImagePaths { get; set; } = new();
    
    public ObservableCollection<CustomAttachment> Attachments { get; set; } = new();

    public ProjectDocumentation()
    {
        ImagePaths.CollectionChanged += (_, __) => DocumentationChanged?.Invoke();
        Attachments.CollectionChanged += (_, __) => DocumentationChanged?.Invoke();
    }

    public void CopyFrom(ProjectDocumentation other)
    {
        TestDocumentation = other.TestDocumentation;
        RequiredGearDocumentation = other.RequiredGearDocumentation;
        KnownIssuesDocumentation = other.KnownIssuesDocumentation;

        ImagePaths.Clear();
        foreach (var path in other.ImagePaths)
            ImagePaths.Add(path);
        
        Attachments.Clear();
        foreach (var path in other.Attachments)
            Attachments.Add(path);
    }

    public ProjectDocumentation Clone()
    {
        var clone = new ProjectDocumentation();
        clone.CopyFrom(this);
        return clone;
    }

    public void ResetToDefault()
    {
        TestDocumentation = string.Empty;
        RequiredGearDocumentation = string.Empty;
        KnownIssuesDocumentation = string.Empty;
        ImagePaths.Clear();
        Attachments.Clear();
    }

    public void PrepareForSave(PathService pathService)
    {
        for (var i = 0; i < ImagePaths.Count; i++)
        {
            ImagePaths[i] = pathService.ToRelative(ImagePaths[i]);
        }

        foreach (var attachment in Attachments)
        {
            attachment.Path = pathService.ToRelative(attachment.Path);
        }
    }

    public void RestoreAfterLoad(PathService pathService)
    {
        for (var i = 0; i < ImagePaths.Count; i++)
        {
            ImagePaths[i] = pathService.ToAbsolute(ImagePaths[i]);
        }

        foreach (var attachment in Attachments)
        {
            attachment.Path = pathService.ToAbsolute(attachment.Path);
        }
    }
}