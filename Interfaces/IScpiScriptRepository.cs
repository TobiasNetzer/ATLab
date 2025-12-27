using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ATLab.Models;

namespace ATLab.Interfaces;

public interface IScpiScriptRepository
{
    Task<IReadOnlyList<ScpiScript>> LoadAllAsync(CancellationToken ct = default);
    Task<ScpiScript?> LoadAsync(string id, CancellationToken ct = default);
    Task SaveAsync(ScpiScript script, CancellationToken ct = default);
    Task DeleteAsync(string id, CancellationToken ct = default);
    Task ConfigureRepositoryFolderAsync();
}
