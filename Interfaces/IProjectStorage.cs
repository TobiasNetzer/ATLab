using System.Collections.Generic;
using System.Threading.Tasks;
using ATLab.Models;

namespace ATLab.Interfaces;

public interface IProjectStorage
{
    Task SaveAsync(string path, AtlabFileDto dto);
    Task<AtlabFileDto?> LoadAsync(string path);
}
