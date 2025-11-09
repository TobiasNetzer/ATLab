using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ATLab.Models;

namespace ATLab.Services;

public class SetCommands
{
    private readonly CTIACommunication _CTIA;
    public SetCommands(CTIACommunication cTIA) => _CTIA = cTIA;
    public async Task<bool> SetExtStimCh(byte channel)
    {
        CTIACommandFrame SetExtStimChFrame = new CTIACommandFrame();
        SetExtStimChFrame.Command = (ushort)SetCmd.SET_EXT_STIM_CH;
        SetExtStimChFrame.PayloadSize = 1;
        SetExtStimChFrame.Payload = new byte[] { channel };

        CTIACommandFrame responseFrame = await _CTIA.SendCommandAsync(SetExtStimChFrame);

        if ((RespCmd)responseFrame.Command == RespCmd.RESP_OK)
            return true;
        else
            return false;
    }
}

