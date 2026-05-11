using System.Threading.Tasks;

namespace ATLab.Interfaces;

public interface IAttachmentLauncherService
{
    Task OpenAttachmentAsync(string path);
}