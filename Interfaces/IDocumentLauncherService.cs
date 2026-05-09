using System.Threading.Tasks;

namespace ATLab.Interfaces;

public interface IDocumentLauncherService
{
    Task OpenDocumentAsync(string path);
}