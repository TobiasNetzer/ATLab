using ATLab.Models;

namespace ATLab.Interfaces;

/// <summary>
/// Personal settings of the user (window, theme, language, last project), stored outside the project.
/// </summary>
public interface ISettingsService
{
    AppSettings Settings { get; }
    void Save();
}
