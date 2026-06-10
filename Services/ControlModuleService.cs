using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ATLab.Enums;
using ATLab.Interfaces;
using ATLab.Models;
using HidSharp;

namespace ATLab.Services;

public sealed class ControlModuleService : IDisposable
{
    private readonly IErrorService _errorService;
    private readonly ProjectSettings _settings;

    private const int Vid = 0xCAFE;
    private const int Pid = 0x4001;

    private HidDevice? _device;
    private HidStream? _stream;
    private CancellationTokenSource? _cts;
    private bool _disposed;
    
    private bool _userResponseMode;

    public event Action? StartPressed;
    public event Action? StopPressed;
    public event Action? PassPressed;
    public event Action? FailPressed;

    public bool IsConnected => _stream != null && _stream.CanRead && !_disposed;

    public ControlModuleService(IErrorService errorService,
        ProjectSettings settings)
    {
        _errorService = errorService;
        _settings = settings;
    }

    public void Initialize()
    {
        if (!_settings.IsControlModuleEnabled)
        {
            Dispose();
            return;
        }
        
        if (_stream != null)
            return;

        _device = DeviceList.Local.GetHidDevices(Vid, Pid).FirstOrDefault();
        if (_device == null)
        {
            _errorService.AddError("No control module connected.");
            return;
        }

        if (!_device.TryOpen(out var stream))
        {
            _errorService.AddError("Failed to connect to control module.");
            return;
        }

        _stream = stream;
        _stream.ReadTimeout = Timeout.Infinite;
        
        SetStatus(TestStatus.IDLE);
        
        _disposed = false;

        _cts = new CancellationTokenSource();
        Task.Run(() => ListenForInputReportsAsync(_stream, _cts.Token), _cts.Token);
    }

    private async Task ListenForInputReportsAsync(HidStream stream, CancellationToken token)
    {
        var buffer = new byte[3];

        while (!token.IsCancellationRequested)
        {
            int count;

            try
            {
                count = await stream.ReadAsync(buffer, token)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception)
            {
                break;
            }

            if (count < 3)
                continue;

            if (buffer[0] != 0x01)
                continue;

            HandleButton(buffer[2]);
        }
    }

    private void HandleButton(byte buttonId)
    {
        switch (buttonId)
        {
            case 0x00:
                if (_userResponseMode)
                {
                    _userResponseMode = false;
                    PassPressed?.Invoke();
                }
                else
                    StartPressed?.Invoke();
                break;
            case 0x01:
                if (_userResponseMode)
                {
                    _userResponseMode = false;
                    FailPressed?.Invoke();
                }
                else
                    StopPressed?.Invoke();
                break;
        }
    }

    public void SetButtonColor(byte buttonId, ControlModuleColors color)
    {
        if (!_settings.IsControlModuleEnabled)
            return;

        if (_stream == null)
            return;
        
        var outReport = new byte[3];
        outReport[0] = 0x02;
        outReport[1] = buttonId;
        outReport[2] = (byte)color;

        try
        {
            _stream.Write(outReport);
        }
        catch
        {
            _errorService.AddError("Lost connection to control module.");
            Dispose();
        }
    }

    public void SetStatus(TestStatus? status)
    {
        if (_stream == null)
            return;
        
        const byte ledGroup = 0x02;
        var cmd = status switch
        {
            TestStatus.IDLE      => (byte)ControlModuleColors.LED_MODE_TEST_IDLE,
            TestStatus.RUNNING   => (byte)ControlModuleColors.LED_MODE_TEST_RUNNING,
            TestStatus.PASSED    => (byte)ControlModuleColors.LED_MODE_TEST_PASSED,
            TestStatus.FAILED    => (byte)ControlModuleColors.LED_MODE_TEST_FAILED,
            TestStatus.CANCELLED => (byte)ControlModuleColors.LED_MODE_TEST_CANCELLED,
            _ => (byte)ControlModuleColors.LED_MODE_OFF
        };
        
        var outReport = new byte[3];
        outReport[0] = 0x02;
        outReport[1] = ledGroup;
        outReport[2] = cmd;

        try
        {
            _stream.Write(outReport);
        }
        catch
        {
            _errorService.AddError("Lost connection to control module.");
        }
    }
    
    public void SetUserResponseMode(bool enabled)
    {
        _userResponseMode = enabled;
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        
        SetStatus(null);

        _disposed = true;

        _cts?.Cancel();
        _stream?.Dispose();

        _stream = null;
        _device = null;
        _cts = null;
    }
}