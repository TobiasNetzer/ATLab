using System;
using System.Text.RegularExpressions;
using ATLab.Models;

namespace ATLab.Helpers;

public static class ResponseProcessor
{
    public static string ApplyMask(string? input, ResponseMask? mask)
    {
        if (mask == null || string.IsNullOrEmpty(input))
            return input ?? string.Empty;

        mask.OriginalResponse = input;

        // Process Input (Skip and Length)
        var processedInput = input;
        if (mask.Skip > 0)
        {
            processedInput = mask.Skip < processedInput.Length
                ? processedInput.Substring(mask.Skip)
                : string.Empty;
        }

        if (mask.Length > 0 && mask.Length < processedInput.Length)
        {
            processedInput = processedInput.Substring(0, mask.Length);
        }

        processedInput = processedInput.Trim();

        // HEX/BIN Conversion
        var isHex = mask.Mask.Contains("HEX", StringComparison.OrdinalIgnoreCase);
        var isBin = mask.Mask.Contains("BIN", StringComparison.OrdinalIgnoreCase);

        if (isHex || isBin)
        {
            try
            {
                // Remove prefix if present (e.g., 0x or 0b)
                var cleanValue = processedInput.Replace("0x", "").Replace("0b", "").Trim();
                
                var value = isHex ? Convert.ToInt64(cleanValue, 16) : Convert.ToInt64(cleanValue, 2);
                processedInput = value.ToString();
            }
            catch
            {
                // If conversion fails, keep the processedInput as is
            }
        }

        // Clean Numeric
        if (mask.IsOnlyNumeric)
        {
            // Keep only digits, decimal point, signs and scientific notation (E/e)
            processedInput = Regex.Replace(processedInput, @"[^0-9\.\+\-Ee]", "");
        }

        mask.ProcessedInput = processedInput;

        string finalResult;

        // Evaluate Mask
        // Check for quoted strings: All of them must be found in the original input
        var quotedMatches = Regex.Matches(mask.Mask, "\"(.*?)\"");
        var hasQuotes = quotedMatches.Count > 0;

        // Extract plain text by removing all quoted parts and keywords
        var plainText = Regex.Replace(mask.Mask, "\".*?\"", "");
        plainText = plainText.Replace("HEX", "", StringComparison.OrdinalIgnoreCase);
        plainText = plainText.Replace("BIN", "", StringComparison.OrdinalIgnoreCase);
        plainText = plainText.Trim();
        var hasPlainText = !string.IsNullOrEmpty(plainText);

        if (!hasQuotes && !hasPlainText)
        {
            // Extraction Mode: If no quotes and no plain text (other than modifiers), return the processed string
            finalResult = processedInput;
        }
        else
        {
            // Comparison Mode: If a mask is provided, look for matches
            var allQuotesMatched = true;

            if (hasQuotes)
            {
                foreach (Match match in quotedMatches)
                {
                    var search = match.Groups[1].Value;
                    if (!string.IsNullOrEmpty(search) && processedInput.Contains(search))
                        continue;
                    
                    allQuotesMatched = false;
                    break;
                }
            }

            var plainTextMatched = true;
            if (hasPlainText)
            {
                // Compare with processed input
                plainTextMatched = (processedInput == plainText);
            }

            // If we have both quotes and plain text, both must match (AND logic)
            var isMatch = allQuotesMatched && plainTextMatched;

            finalResult = isMatch ? "1" : "0";
        }

        mask.FinalResult = finalResult;

        return finalResult;
    }
}