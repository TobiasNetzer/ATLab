using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using ATLab.Interfaces;

namespace ATLab.Services;

public class DocumentLauncherService : IDocumentLauncherService
{
    public Task OpenDocumentAsync(string path)
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