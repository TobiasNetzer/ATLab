using System.Collections.Generic;
using System.Threading.Tasks;
using ATLab.Models;

namespace ATLab.Interfaces;

public interface IFileService
{
    string Serialize(AtlabFileDto dto);
    AtlabFileDto? Deserialize(string json);
    Task SaveAsync(string path, AtlabFileDto dto);
    Task<AtlabFileDto?> LoadAsync(string path);
}
