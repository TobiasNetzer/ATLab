using ATLab.Models;

namespace ATLab.Interfaces;

public interface IResponseProcessor
{
    string ApplyMask(string? input, ResponseMask? mask);
}