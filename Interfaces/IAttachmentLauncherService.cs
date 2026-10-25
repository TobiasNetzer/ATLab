using System.Threading.Tasks;

namespace ATLab.Interfaces;

/// <summary>
/// Opens a documentation attachment with the application the operating system associates with it.
/// </summary>
public interface IAttachmentLauncherService
{
    Task OpenAttachmentAsync(string path);
}