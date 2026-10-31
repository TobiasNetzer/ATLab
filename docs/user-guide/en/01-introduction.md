# 1. Introduction

ATLab is an application for automated testing of electronic assemblies, for example functional and end-of-line tests. ATLab works with standard test instruments such as multimeters and power supplies and controls them remotely. The **Test Interface Adapter** connects them to the device under test.

A test engineer builds a **test sequence** from test steps that measure values and compare them with limits. An operator runs the sequence and gets a pass/fail result, optionally with a test report.

![ATLab main window](images/Testing_Tab_en.png)

## What ATLab can do

### Build test sequences

- **Organize** a sequence into modules (e.g. power supply, programming, LED test) that can be enabled individually or run on their own.
- **Route signals** per step through the adapter's relay matrix and switch its stimulation and external stimulation relays.
- **Check values** against limits with units and SI prefixes, e.g. `500mV` or `4.7kOhm`.
- **Branch** depending on the result: repeat a step, jump to another step, end the test or show a message to the operator.
- **Calculate** with runtime and system variables (serial number, user, date …) in commands, limits and expressions.

### Measure and communicate

- **Remote-control instruments** over serial port (VCOM/RS232), raw TCP/IP sockets or NI-VISA. The command system is designed primarily for SCPI.
- **Reuse instrument scripts**: multi-command scripts with variables, shared across projects. Each command can **check its response** with its own mask and limits, e.g. that the instrument reports no error.
- **Talk to the DUT** through the adapter's isolated I²C and UART interfaces.
- **Use boundary scan** (JTAG) with a SEGGER J-Link or FTDI probe to read and drive the pins of chips on the board, and to talk to I²C and SPI devices through them.
- **Extract values** from text or binary answers with response processing (skip/length, match rules, numeric extraction).
- **Involve the operator**: confirm a visual check (Passed / Failed) or enter a reading, optionally with an image.
- **Run external programs** and use their exit code or output, or **read a value from a file**.

### Develop and debug

- **Development mode** for editing, separate from the operator view; it can be protected by a password.
- **Try things out** step by step: execute a single step, start from the selected step or run one module; **debug mode** pauses before every step.
- **Test scripts** in the Script Runner, which shows every response and the result of each response check.
- **Work without hardware**: the simulator replaces the Test Interface Adapter, e.g. on an engineering PC.

### Run tests in production

- **Serial number entry** with validation rules; barcode scanners that act as a keyboard are supported.
- **Continuous runs**, a large **measurement panel** that is readable from a distance, and buttons to jump to failed steps.
- An optional USB **control module** with illuminated start/stop buttons.

### Document and report

- **Project documentation** with notes, images, attachments and information about the device under test.
- **CSV data files**, **PDF test reports** and **JSON result files** with run metadata, including the instruments used.
- An **event log** with every message of the session.

ATLab runs on Windows and Linux. Projects and scripts are plain JSON files, well suited for version control. The user interface is available in English and German with a light and a dark theme. Language and theme are chosen in the settings dialog. This guide opens with `F1`.

## Key concepts

| Term | Meaning |
|------|---------|
| **Project** | One `.atlab` file containing the test sequence, devices, runtime variables, channel names, project settings, documentation and DUT information. See [Working with Projects](05-projects.md). |
| **Module** | A named group of test steps within the project, e.g. *Power Supply* or *LED Test*. Modules run in order; disabled modules are skipped. See [Modules](08-building-test-sequences.md#modules). |
| **Test step** | One row in a module. It has a name, limits, a unit, a delay, an evaluation source, relay states and pass/fail actions. See [How a test step is executed](14-running-tests.md#how-a-test-step-is-executed). |
| **Evaluation source** | Where a step gets its measured value from: *None, Script, Device Command, Interface Command, External Program, User Confirmation, User Input, Expression, File, Boundary Scan* or *Boundary Scan Bus*. See [Evaluation Sources](09-evaluation-sources.md). |
| **Device** | An external instrument (or a DUT reachable over serial/TCP/VISA) defined in the Device Manager. |
| **Script** | A reusable, named list of instrument commands stored as a JSON file in a *script folder*, so it can be shared across projects. |
| **Script variable** | A placeholder such as `{Range}` inside a script, given a concrete value per test step. |
| **Runtime variable** | A project-wide variable that can be read in commands, limits and expressions, and written by a step via *Assign Result to Variable*. |
| **Relay matrix** | The adapter's measurement matrix. Each step selects one channel for **Matrix H** and one for **Matrix L** (or *Off*). |
| **Stimulation / Ext. stimulation channels** | Individually switchable relays on the adapter, set per step. |
| **Development mode** | The editing mode of the Test Execution tab. When it is off, the tab shows only the sequence and results. This is the operator view. |
| **Simulator** | A virtual Test Interface Adapter used when no hardware is connected. Relay and interface commands succeed without doing anything. |
| **Boundary scan chain** | The boundary-scan capable chips on the board, connected in series to a JTAG probe. Set up on the [Boundary Scan tab](12-boundary-scan.md). |

## Who this guide is for

- **Test engineers** who design sequences: read chapters 5–13 (chapter 12 only if you use boundary scan).
- **Operators** who run released sequences: chapters 3, 14 and 15 are enough.
- **Administrators** who install ATLab on test stations: chapters 2 and 4 and the [Reference](17-reference.md).
