namespace ATLab.Models;

public class CustomAttachment
{
    public string Path { get; set; } = string.Empty;
    public string FileName => System.IO.Path.GetFileName(Path);
}