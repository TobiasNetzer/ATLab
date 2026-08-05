using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ATLab.Models;

namespace ATLab.Interfaces;

public interface IScriptRepository
{
    event EventHandler<string?>? RepositoryFolderChanged;
    void SetRepositoryFolder(string? folderPath);
    Task<IReadOnlyList<CustomScript>> LoadAllAsync(CancellationToken ct = default);
    Task<CustomScript?> LoadAsync(string id, CancellationToken ct = default);
    Task SaveAsync(CustomScript script, CancellationToken ct = default);
    Task DeleteAsync(string id, CancellationToken ct = default);
    Task ConfigureRepositoryFolderAsync();
}
