using System;
using ATLab.CTIA;
using ATLab.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace ATLab.Services;

public class HardwareProvider : IHardwareProvider
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ISimulationService _simulationService;

    public HardwareProvider(IServiceProvider serviceProvider, ISimulationService simulationService)
    {
        _serviceProvider = serviceProvider;
        _simulationService = simulationService;
    }

    public ITestHardware GetHardware()
    {
        if (_simulationService.IsSimulationMode)
        {
            return _serviceProvider.GetRequiredService<TestHardwareSimulator>();
        }
        return _serviceProvider.GetRequiredService<CtiaHardware>();
    }
}