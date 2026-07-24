using System.Collections.Generic;
using System.Threading.Tasks;
using ATLab.Models;

namespace ATLab.Interfaces;

public interface IInterfaceCommandExecuter
{
    Task<OperationResult<double>> ExecuteAsync(
        TestInterfaceConfig config,
        List<CustomVariable> runtimeVariables,
        ResponseMask? mask);
}