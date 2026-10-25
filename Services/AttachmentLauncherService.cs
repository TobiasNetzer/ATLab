using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using ATLab.Interfaces;

namespace ATLab.Services;

/// <summary>Opens attachments through the shell of the operating system.</summary>
public class AttachmentLauncherService : IAttachmentLauncherService
{
    public Task OpenAttachmentAsync(string path)
    {
        if (!File.Exists(path))
            return Task.CompletedTask;

        Process.Start(new ProcessStartInfo
        {
            FileName = path,
            UseShellExecute = true
        });

        return Task.CompletedTask;
    }
}