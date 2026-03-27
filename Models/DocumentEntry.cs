namespace ATLab.Models;

public class DocumentEntry
{
    public string Path { get; init; } = string.Empty;
    public string FileName => System.IO.Path.GetFileName(Path);
}
