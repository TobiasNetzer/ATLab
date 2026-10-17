# 8. Evaluation Sources

The **Evaluate** field of a step decides where its measured value comes from. Every source ultimately produces one number, which is then compared with the step's limits.

| Source | Produces | Editor panel | Response Processing |
|--------|----------|--------------|:---:|
| [None](#none) | no value, always passes | – | – |
| [Script](#script) | answer of the script's *Evaluate* command | Script Configuration | ✔ |
| [Device Command](#device-command) | answer of a single command | Device Command | ✔ |
| [Interface Command](#interface-command) | data read via the adapter's I²C/UART | Interface Command | ✔ |
| [Shell Command](#shell-command) | process exit code | Shell Command | – |
| [User Response](#user-response) | `1` (Passed) or `0` (Failed) | – (uses Comment) | – |
| [Expression](#expression) | result of a calculation | Expression | – |
| [File](#file) | number read from a file | File Path | ✔ |

## Special results

| Shown as | Meaning | Verdict |
|----------|---------|---------|
| *(empty)* with ✔ | Source **None** | Pass |
| `Timeout` | The instrument/interface did not answer within the timeout. The Fail-Action applies and the test continues. | Fail |
| `Overflow` | Value ≥ 9.9 × 10³⁷ (the SCPI overload value, e.g. a DMM out of range) | Fail |
| *(empty)* with ✘ and an error in the event log | The step could not be executed (device not found, connection failed, parse error …). **The test stops** with status *Test Execution Failed*. | Fail |

A response that cannot be converted to a number — for example `OK` or `+1.23E+00 VDC` — is an execution error. Use [Response Processing](10-response-processing.md) to turn such answers into a number.

---

## None

The step only switches relays, waits for its delay and optionally shows its comment. It always passes and shows no measured value. Typical uses: configuration steps, delays, operator instructions, connecting a stimulus before the next measurement.

## Script

Runs a script from the [script repository](06-scripting.md) on a device.

**Script Configuration panel**

| Field | Description |
|-------|-------------|
| **Target Device** | Device from the Device Manager, shown as `Name (resource)`. The × button clears it. |
| **Script** | Script from the repository. Hover over an entry to see its description. The × button clears it. |
| **Variable / Value** | The script's variables with this step's values. When a script is first selected, its default values are copied in; edit them per step. |

Execution: all commands of the script are sent in order; the answer of the command marked **Evaluate** becomes the measured value. Script variables are substituted first, then runtime variables, encodings and expressions are processed.

If the script later gains new variables, they are added to the step with their default values when the step is opened; variables removed from the script are dropped from the step.

## Device Command

Sends one command to a device — handy for quick, one-off queries without creating a script.

| Field | Description |
|-------|-------------|
| **Target Device** | Device from the Device Manager. |
| **Command** | Command text, e.g. `MEAS:VOLT:DC?`. Supports variables, encodings and expressions. |
| **Expect Response** | On: send and read the answer, which becomes the measured value. Off: just send; measured value is `0`. |
| **Timeout (ms)** | Maximum wait for the answer. |

## Interface Command

Communicates with the DUT through the Test Interface Adapter's isolated interfaces.

| Field | I²C | UART |
|-------|:---:|:---:|
| **Interface Type** | `I2C` | `UART` |
| **Address 0-127** | 7-bit slave address | – |
| **Speed Mode** | 100 kHz, 400 kHz, 1 MHz | – |
| **Baud Rate / Data Bits / Parity / Stop Bits** | – | serial parameters (data bits 7 or 8) |
| **Termination** | – | appended to the command: NONE, LF, CR, CRLF |
| **Command** | bytes to write | bytes to send |
| **Read Bytes** | number of bytes to read (0–255) | number of bytes expected back (0–255) |
| **Timeout (ms)** | transaction timeout | transaction timeout |

The command is usually written with encoding blocks, e.g. `{hex:0A 1F}` to send the two bytes 0x0A 0x1F (see [chapter 9](09-commands-variables-expressions.md#encoding-blocks)).

**I²C behaviour**

- With a command and **Read Bytes** = 0: the bytes are written; the result is `1` if the slave acknowledged, `0` on NACK.
- With **Read Bytes** > 0: after the optional write, the given number of bytes is read. The received bytes go through response processing; a NACK on read gives `0`.
- Without response processing, the received bytes are interpreted as ASCII text. For binary registers, enable response processing with e.g. `{byte:2}`.

**UART behaviour**

- The command (plus termination) is sent and **Read Bytes** bytes are awaited. If fewer arrive, an error is logged.
- With **Read Bytes** = 0, the measured value is `0`.

The interface must be available on the connected adapter (see the Test Hardware tab); otherwise the step fails with *"Interface not available"*. RS485 is listed but not yet supported for test steps.

## Shell Command

Runs a program or shell command on the test PC. The **exit code** is the measured value — `0` normally means success, so set the limits to `0 … 0`.

| Field | Description |
|-------|-------------|
| **Command** | Command line. May contain `{Variable}` references. |
| **Direct Launch** | On: start the program directly, without a shell window. The first word (or a `"quoted path"`) is the program, the rest are arguments. Off: run the command in a terminal window. |
| **Window Behavior** | Without direct launch: **Auto Close** closes the window when the command finishes; **Keep Open** leaves it open — the step then waits until the operator closes the window. |

The working directory is the project folder, so you can call scripts stored next to the project with relative paths. While the step waits, the status panel shows *Waiting for external application…*.

Platform details:

- **Windows:** commands run in `cmd.exe` (`/c` for Auto Close, `/k` for Keep Open). With **Direct Launch**, ATLab waits for and returns the exit code only if the program is an `.exe`; other files (e.g. `.bat`, `.py`, documents) are opened with their associated application and the result is `0` immediately.
- **Linux:** commands run in `bash` inside the first available terminal emulator (ptyxis, gnome-terminal, konsole, xfce4-terminal, xterm). Some terminal emulators return immediately, before the command has finished; use **Direct Launch** if you depend on the exit code.

Examples:

```text
python flash_firmware.py --port COM7 --sn {SerialNo}
"C:\Program Files\SEGGER\JLink\JLink.exe" -CommanderScript program.jlink
```

## User Response

Shows the step's **Comment** in a dialog titled *Waiting for User Response* with the buttons **Passed** and **Failed**. Clicking **Passed** yields `1`, **Failed** yields `0`. Set all limits to `1` so that only *Passed* passes.

An image added via *Show Comment* (Advanced Settings) is shown in the dialog. With the [control module](11-running-tests.md#control-module) enabled, the green button answers *Passed* and the red one *Failed*.

## Expression

Calculates a value. The **Expression** field may contain numbers, runtime variables and operators, for example:

```text
{V_In} / {I_In}
({V_Out} - {V_Ref}) * 1000
0x3F & {Status}
```

The supported operators are listed in [chapter 9](09-commands-variables-expressions.md#expressions). Combined with *Assign Result to Variable*, expression steps can compute derived values, counters or flags. An expression that cannot be evaluated is an execution error.

## File

Reads a file and extracts a number from its content.

| Field | Description |
|-------|-------------|
| **File Path** | Path to the file. Use the browse button to pick it. Paths inside the project folder are stored relatively. |

Without response processing, the entire file content must be a number (e.g. a file containing `3.274`). With response processing you can cut a fixed-position value out of a larger file with **Skip**/**Length**, or simply check that the file contains a given text (see [Response Processing](10-response-processing.md)). A typical pattern is a Shell Command step that runs an external tool which writes its result into a file, followed by a File step that evaluates it.
