using System;
using System.Collections.Generic;
using System.Globalization;

namespace ATLab.Models;

// Variables provided by ATLab during test execution, referenced as {sys.Name}.
// Not part of the project, so they can't be selected for "Assign Result to Variable".
public class SystemVariables
{
    public const string Prefix = "sys.";

    public CustomVariable SerialNumber { get; } = Create("SerialNumber");
    public CustomVariable User { get; } = Create("User");
    public CustomVariable ProjectName { get; } = Create("ProjectName");
    public CustomVariable Year { get; } = Create("Year");
    public CustomVariable Month { get; } = Create("Month");
    public CustomVariable Day { get; } = Create("Day");
    public CustomVariable Hour { get; } = Create("Hour");
    public CustomVariable Minute { get; } = Create("Minute");
    public CustomVariable Second { get; } = Create("Second");
    public CustomVariable ElapsedTime { get; } = Create("ElapsedTime");
    public CustomVariable ModuleName { get; } = Create("ModuleName");
    public CustomVariable StepNumber { get; } = Create("StepNumber");
    public CustomVariable FailedSteps { get; } = Create("FailedSteps");

    public IEnumerable<CustomVariable> All =>
    [
        SerialNumber, User, ProjectName, Year, Month, Day, Hour, Minute, Second, ElapsedTime, ModuleName, StepNumber, FailedSteps
    ];

    public void UpdateTimestamp(DateTimeOffset now, TimeSpan elapsed)
    {
        Year.Value = now.ToString("yyyy", CultureInfo.InvariantCulture);
        Month.Value = now.ToString("MM", CultureInfo.InvariantCulture);
        Day.Value = now.ToString("dd", CultureInfo.InvariantCulture);
        Hour.Value = now.ToString("HH", CultureInfo.InvariantCulture);
        Minute.Value = now.ToString("mm", CultureInfo.InvariantCulture);
        Second.Value = now.ToString("ss", CultureInfo.InvariantCulture);
        ElapsedTime.Value = elapsed.TotalSeconds.ToString("0.00", CultureInfo.InvariantCulture);
    }

    private static CustomVariable Create(string name) =>
        new() { IsSystem = true, Name = Prefix + name, Value = string.Empty };
}
