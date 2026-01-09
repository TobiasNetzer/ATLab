using System;
using ATLab.Enums;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ATLab.Models;

public class ShellCommand : ObservableObject
{
    public string Command { get; set; } = string.Empty;

    public ShellCommandOptions Option { get; set; }
    
    public ShellCommand()
    {

    }
    
    public ShellCommand(ShellCommand other)
    {
        Command = other.Command;
        Option = other.Option;
    }
}