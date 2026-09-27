using System.Threading.Tasks;
using ATLab.Models;

namespace ATLab.Interfaces;

public interface IProjectFileService
{
    Task SaveAsync(string path, AtlabFileDto dto);
    Task<AtlabFileDto?> LoadAsync(string path);
}