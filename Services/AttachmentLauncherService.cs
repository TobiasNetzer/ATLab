using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using ATLab.Interfaces;

namespace ATLab.Services;

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