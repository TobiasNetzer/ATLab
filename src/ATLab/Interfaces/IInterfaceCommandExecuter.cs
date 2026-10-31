using System.Collections.Generic;
using System.Threading.Tasks;
using ATLab.Models;

namespace ATLab.Interfaces;

/// <summary>Runs Interface Command steps over the I²C and UART interfaces of the test hardware.</summary>
public interface IInterfaceCommandExecuter
{
    Task<OperationResult<double>> ExecuteAsync(
        TestInterfaceConfig config,
        List<CustomVariable> runtimeVariables,
        ResponseMask? mask,
        ICollection<string> details);
}