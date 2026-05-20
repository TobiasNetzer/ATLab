using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using ATLab.Models;

namespace ATLab.Helpers;

public static class ResponseProcessor
{
    public static string? Process(byte[] input, ResponseMask? mask)
    {
        if (mask == null)
            return Encoding.ASCII.GetString(input);

        mask.OriginalResponse = Encoding.ASCII.GetString(input);

        var processedInput = input;

        // Skip
        if (mask.Skip > 0)
        {
            processedInput = mask.Skip < processedInput.Length
                ? processedInput.Skip(mask.Skip).ToArray()
                : Array.Empty<byte>();
        }

        // Length
        if (mask.Length > 0 && mask.Length < processedInput.Length)
        {
            processedInput = processedInput.Take(mask.Length).ToArray();
        }
        
        mask.ProcessedInput = Encoding.ASCII.GetString(processedInput);
        
        
        

        return Encoding.ASCII.GetString(processedInput);
    }
}