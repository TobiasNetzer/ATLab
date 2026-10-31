using System.Threading.Tasks;
using ATLab.Models;

namespace ATLab.Interfaces;

/// <summary>Reads and writes the project file (JSON) and checks its format version.</summary>
public interface IProjectFileService
{
    Task SaveAsync(string path, AtlabFileDto dto);
    Task<AtlabFileDto?> LoadAsync(string path);
}