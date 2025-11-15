using System.Threading.Tasks;
using ATLab.Models;

namespace ATLab.Services;

public class ClrCommands
{
    private readonly CTIACommunication _CTIA;
    public ClrCommands(CTIACommunication cTIA) => _CTIA = cTIA;
    public async Task<OperationResult<bool>> ClearExtStimCh(byte channel)
    {
        CommandFrame frame = new CommandFrame
        {
            Command = (ushort)ClrCmd.CLR_EXT_STIM_CH,
            PayloadSize = 1,
            Payload = [channel]
        };

        CommandFrame responseFrame = await _CTIA.SendCommandAsync(frame);

        if ((RespCmd)responseFrame.Command == RespCmd.RESP_OK)
            return OperationResult<bool>.Success(true);
        else
            return OperationResult<bool>.Failure($"Unexpected response: {responseFrame.Command}", (RespCmd)responseFrame.Command);
    }
}

