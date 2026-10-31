using System.Threading.Tasks;
using ATLab.CTIA;
using ATLab.Interfaces;
using ATLab.Models;

namespace ATLab.Services;

/// <summary>
/// Connects to the adapter on the port used last time, or switches to the simulator when simulation is forced in
/// the settings.
/// </summary>
public sealed class HardwareInitializer : IHardwareInitializer
{
    private readonly ISettingsService _settingsService;
    private readonly ICommunicationFactory _communicationFactory;
    private readonly IHardwareAccessor _hardwareAccessor;
    private readonly ApplicationState _applicationState;
    private readonly TestHardwareSimulator _simulator;
    private readonly ILoggingService _loggingService;

    public HardwareInitializer(
        ISettingsService settingsService,
        ICommunicationFactory communicationFactory,
        IHardwareAccessor hardwareAccessor,
        ApplicationState applicationState,
        TestHardwareSimulator simulator,
        ILoggingService loggingService)
    {
        _settingsService = settingsService;
        _communicationFactory = communicationFactory;
        _hardwareAccessor = hardwareAccessor;
        _applicationState = applicationState;
        _simulator = simulator;
        _loggingService = loggingService;
    }

    public async Task<bool> InitializeAsync()
    {
        if (_settingsService.Settings.IsForceSimulation)
        {
            _hardwareAccessor.Hardware = _simulator;
            _applicationState.IsSimulationMode = true;

            return true;
        }

        var lastPort = _settingsService.Settings.LastComPort;

        if (string.IsNullOrWhiteSpace(lastPort))
        {
            return false;
        }

        var communication = _communicationFactory.CreateSerial(
            lastPort,
            new DeviceConfiguration());

        var connectResult = await communication.ConnectAsync();

        if (!connectResult.IsSuccess)
        {
            await CleanupAsync(communication);
            return false;
        }

        var ctiaCommunication = new CtiaCommunication(communication, _loggingService);

        var hardware = new CtiaHardware(ctiaCommunication);

        var initResult = await hardware.InitializeAsync();

        if (!initResult.IsSuccess)
        {
            await CleanupAsync(communication);
            return false;
        }

        _applicationState.IsSimulationMode = false;
        _hardwareAccessor.Hardware = hardware;

        return true;
    }

    private static async Task CleanupAsync(ICommunication communication)
    {
        await communication.DisconnectAsync();
        await communication.DisposeAsync();
    }
}