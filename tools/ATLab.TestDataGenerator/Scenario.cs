using System;
using System.Collections.Generic;

namespace ATLab.TestDataGenerator;

/// <summary>What is known about a run when its steps are measured.</summary>
internal sealed record RunContext(
    DateTimeOffset Time,
    double Progress,
    string Station,
    string Variant,
    bool IsSimulator);

internal enum Distribution
{
    Normal,

    /// <summary>Positive values with a long upper tail, e.g. leakage current or ripple. Mean is the median.</summary>
    LogNormal
}

/// <summary>A step of a project and how its results scatter.</summary>
internal sealed record StepDefinition(
    string ModuleId,
    string Module,
    string StepId,
    int Number,
    string Name,
    string Unit = "",
    double? Lower = null,
    double? Upper = null)
{
    /// <summary>Null for a step without value (e.g. a communication check).</summary>
    public double? Mean { get; init; }

    /// <summary>Standard deviation; for <see cref="Distribution.LogNormal"/> the sigma of the logarithm.</summary>
    public double StdDev { get; init; }

    public Distribution Distribution { get; init; } = Distribution.Normal;

    /// <summary>Systematic offset of the mean, e.g. drift over time or a station-specific fixture.</summary>
    public Func<RunContext, double>? Shift { get; init; }

    /// <summary>Limits used from this share of the period on (0..1), e.g. tightened limits.</summary>
    public (double From, double? Lower, double? Upper)? LaterLimits { get; init; }

    /// <summary>Name used from this share of the period on; the step ID stays the same.</summary>
    public (double From, string Name)? LaterName { get; init; }

    /// <summary>Probability of a result without value: Timeout, NACK, Overflow or a failed check of a step without value.</summary>
    public double ErrorRate { get; init; }

    public string ErrorStatus { get; init; } = "Timeout";

    /// <summary>Detail line of the error in ATLab's wording; Timeout and Overflow have none.</summary>
    public string? ErrorDetail { get; init; }

    /// <summary>
    /// Probability that a script response check fails although the value is measured (and maybe within the limits),
    /// with the detail line ATLab writes for it.
    /// </summary>
    public (double Rate, string Detail)? CheckFailure { get; init; }

    /// <summary>
    /// Boundary scan pin check: the value is the number of mismatching pins and every mismatch gets a detail line.
    /// Pins with a higher weight fail more often, e.g. a pin with a recurring solder defect.
    /// </summary>
    public (double FailureRate, IReadOnlyList<(string Pin, double Weight)> Pins)? PinCheck { get; init; }

    /// <summary>The step only exists in this variant.</summary>
    public string? OnlyVariant { get; init; }
}

internal sealed record ProjectDefinition(
    string Name,
    string DeviceName,
    string PartNumber,
    double Weight,
    IReadOnlyList<string> Variants,
    IReadOnlyList<StepDefinition> Steps,
    string SerialPrefix,
    double BaseDurationSeconds);

/// <summary>
/// The production line the data simulates: two projects whose steps cover the cases the analyzer has to handle,
/// e.g. SI prefixes and units in braces, one-sided limits, drift, limit changes, renamed steps, variant-only steps,
/// two fixtures with different offsets and steps without value that fail with Timeout or NACK.
/// </summary>
internal static class Scenario
{
    public static IReadOnlyList<ProjectDefinition> Projects { get; } =
    [
        new("Controller Board", "Controller", "CB-200", 0.85, ["Standard", "Standard", "Pro"],
        [
            // Power supply
            new("m-pwr", "Power supply", "cb-vin", 1, "Input current", "A", 0.08, 0.15)
            {
                // Script that reads the DMM and checks its error queue: fails within the limits when the check fails
                Mean = 0.112, StdDev = 0.006,
                CheckFailure = (0.003, "Command 2 'SYST:ERR?': received '-113,\"Undefined header\"', result -113 not within 0 … 0.")
            },
            new("m-pwr", "Power supply", "cb-3v3", 2, "3V3 rail", "V", 3.2, 3.4)
            {
                Mean = 3.302, StdDev = 0.018,
                LaterLimits = (0.6, 3.25, 3.35)
            },
            new("m-pwr", "Power supply", "cb-5v", 3, "5V rail", "V", 4.85, 5.15)
            {
                Mean = 5.04, StdDev = 0.03,
                Shift = c => 0.07 * c.Progress // regulator ages, more failures towards the end
            },
            new("m-pwr", "Power supply", "cb-ripple", 4, "Ripple 3V3", "V", Upper: 0.05)
            {
                Mean = 0.018, StdDev = 0.35, Distribution = Distribution.LogNormal
            },
            new("m-pwr", "Power supply", "cb-standby", 5, "Standby current", "A", 0.003, 0.006) { Mean = 0.0042, StdDev = 0.0003 },

            // Communication
            // Timeout has no detail lines in ATLab
            new("m-com", "Communication", "cb-uart", 1, "UART echo") { ErrorRate = 0.008, ErrorStatus = "Timeout" },
            new("m-com", "Communication", "cb-i2c", 2, "I2C sensor ID")
            {
                ErrorRate = 0.005, ErrorStatus = "NACK", ErrorDetail = "I²C write failed: NACK received."
            },
            new("m-com", "Communication", "cb-fw", 3, "Firmware version check")
            {
                ErrorRate = 0.003, ErrorStatus = "NoValue",
                ErrorDetail = "Command 1 'VER?': received 'V2.2.9', result 2.29 not within 2.31 … 2.31."
            },

            // Analog
            new("m-ana", "Analog", "cb-vref", 1, "ADC reference", "V", 2.495, 2.505)
            {
                Mean = 2.5003, StdDev = 0.0011, ErrorRate = 0.002, ErrorStatus = "Overflow"
            },
            new("m-ana", "Analog", "cb-temp", 2, "Board temperature", "{°C}", 15, 45)
            {
                Mean = 28, StdDev = 2.5,
                Shift = c => c.Station == "TEST-PC2" ? 1.5 : 0
            },
            new("m-ana", "Analog", "cb-osc", 3, "Oscillator frequency", "Hz", 7.99e6, 8.01e6) { Mean = 8.0003e6, StdDev = 2100 },
            new("m-ana", "Analog", "cb-leak", 4, "Leakage current", "A", Upper: 5e-6)
            {
                Mean = 1.2e-6, StdDev = 0.45, Distribution = Distribution.LogNormal
            },
            new("m-ana", "Analog", "cb-contact", 5, "Contact resistance", "Ohm", Upper: 0.15)
            {
                // Two fixtures: the histogram shows two peaks
                Mean = 0.08, StdDev = 0.008,
                Shift = c => c.Station == "TEST-PC2" ? 0.03 : 0
            },
            new("m-ana", "Analog", "cb-zin", 6, "Input impedance", "Ohm", 95e3, 105e3) { Mean = 100e3, StdDev = 1500 },

            // Outputs
            new("m-out", "Outputs", "cb-pwm", 1, "PWM duty cycle", "{%}", 48, 52) { Mean = 50, StdDev = 0.6 },
            new("m-out", "Outputs", "cb-fan", 2, "Fan speed", "{rpm}", 2200, 2600) { Mean = 2400, StdDev = 60 },
            new("m-out", "Outputs", "cb-relay", 3, "Relay switch time", "s", Upper: 0.018) { Mean = 0.0123, StdDev = 0.0011 },
            new("m-out", "Outputs", "cb-led", 4, "LED check", "", 1, 1)
            {
                // Operator confirmation: 1 = confirmed, 0 = rejected; no detail lines
                Mean = 1, ErrorRate = 0.004, ErrorStatus = "Rejected",
                LaterName = (0.4, "Status LED check")
            },
            new("m-out", "Outputs", "cb-can", 5, "CAN termination", "Ohm", 114, 126)
            {
                Mean = 120, StdDev = 1.2, OnlyVariant = "Pro"
            },

            // Boundary scan
            new("m-bsc", "Boundary scan", "cb-bs-inter", 1, "Interconnect test", "", 0, 0)
            {
                PinCheck = (0.012,
                [
                    ("U3 PA5 (LED_RED)", 6), // recurring solder defect
                    ("U3 PA6 (LED_GREEN)", 1),
                    ("U3 PB0 (SPI_CS)", 1),
                    ("U3 PB3 (SPI_SCK)", 1),
                    ("U5 IO12 (CAN_TX)", 1),
                    ("U5 IO13 (CAN_RX)", 1)
                ])
            },
            new("m-bsc", "Boundary scan", "cb-bs-reset", 2, "Reset line", "", 0, 0)
            {
                PinCheck = (0.003, [("U1 nRST", 1), ("U3 PC13 (nRESET)", 1)])
            },
            new("m-bsc", "Boundary scan", "cb-bs-i2c", 3, "Temperature sensor ID (I²C via JTAG)", "", 117, 117)
            {
                // Register read through the boundary scan bus: 0x75 when the sensor answers
                Mean = 117, ErrorRate = 0.006, ErrorStatus = "NACK",
                ErrorDetail = "No ACK from 0x48 for address 0x48 (write)."
            }
        ], "CB", 45),

        new("Power Module", "PSU", "PM-24", 0.15, ["24V"],
        [
            new("p-out", "Output", "pm-vout", 1, "Output voltage", "V", 23.5, 24.5) { Mean = 24.0, StdDev = 0.08 },
            new("p-out", "Output", "pm-reg", 2, "Load regulation", "{%}", Upper: 1) { Mean = 0.4, StdDev = 0.1 },
            new("p-out", "Output", "pm-eff", 3, "Efficiency", "{%}", Lower: 89) { Mean = 91.5, StdDev = 0.6 },
            new("p-out", "Output", "pm-ripple", 4, "Output ripple", "V", Upper: 0.12)
            {
                Mean = 0.045, StdDev = 0.3, Distribution = Distribution.LogNormal
            },
            new("p-prot", "Protection", "pm-ocp", 1, "Overcurrent trip", "A", 4.8, 5.6) { Mean = 5.2, StdDev = 0.1 },
            new("p-prot", "Protection", "pm-otp", 2, "Overtemperature shutdown", "{°C}", 100, 110) { Mean = 105, StdDev = 1.5 },
            new("p-prot", "Protection", "pm-short", 3, "Short-circuit test")
            {
                ErrorRate = 0.01, ErrorStatus = "NoValue",
                ErrorDetail = "Command 3 'MEAS:VOLT?': received '+1.20E-02', result 0.012 not within 23.5 … 24.5."
            }
        ], "PM", 30)
    ];

    /// <summary>Early shift and late shift.</summary>
    public static readonly (int FromHour, int ToHour, string[] Operators)[] Shifts =
    [
        (6, 14, ["anna.k", "ben.m"]),
        (14, 22, ["chris.w", "dana.s"])
    ];

    public static readonly string[] Stations = ["TEST-PC1", "TEST-PC2"];
}