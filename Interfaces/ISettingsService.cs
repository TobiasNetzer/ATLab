using ATLab.Models;

namespace ATLab.Interfaces;

public interface ISettingsService
{
    AppSettings Settings { get; }
    void Save();
}
