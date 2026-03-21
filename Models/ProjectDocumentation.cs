using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

    public ProjectDocumentation()
    {
        ImagePaths.CollectionChanged += (_, __) => DocumentationChanged?.Invoke();
    }

    public void CopyFrom(ProjectDocumentation other)
    {
        TestDocumentation = other.TestDocumentation;
        RequiredGearDocumentation = other.RequiredGearDocumentation;
        KnownIssuesDocumentation = other.KnownIssuesDocumentation;

        ImagePaths.Clear();

        foreach (var path in other.ImagePaths)
            ImagePaths.Add(path);
    }
    
    public void ResetToDefault()
    {
        TestDocumentation = string.Empty;
        RequiredGearDocumentation = string.Empty;
        KnownIssuesDocumentation = string.Empty;
        ImagePaths.Clear();
    }
}
