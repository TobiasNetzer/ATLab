# 8. Evaluation Sources

The **Evaluate** field of a step decides where its measured value comes from. Every source ultimately produces one number, which is then compared with the step's limits.

| Source | Produces | Editor panel | Response Processing |
|--------|----------|--------------|:---:|
| [None](#none) | no value, always passes | – | – |
| [Script](#script) | answer of the script's *Evaluate* command | Script Configuration | ✔ |
| [Device Command](#device-command) | answer of a single command | Device Command | ✔ |
| [Interface Command](#interface-command) | data read via the adapter's I²C/UART | Interface Command | ✔ |
| [External Program](#external-program) | process exit code or program output | External Program | ✔ (output only) |
| [User Confirmation](#user-confirmation) | `1` (Passed) or `0` (Failed) | – (uses Comment) | – |
| [User Input](#user-input) | number entered by the operator | – (uses Comment) | ✔ |
| [Expression](#expression) | result of a calculation | Expression | – |
| [File](#file) | number read from a file | File Path | ✔ |
| [Boundary Scan](#boundary-scan) | number of pins that do not have the expected level | Boundary Scan | – |
| [Boundary Scan Bus](#boundary-scan-bus) | data read from an I²C/SPI device or pin group emulated via boundary scan | Boundary Scan Bus | ✔ |

## Special results

| Shown as | Meaning | Verdict |
|----------|---------|---------|
| *(empty)* with ✔ | Source **None** | Pass |
| `Timeout` | The instrument/interface did not answer within the timeout. The Fail-Action applies and the test continues. | Fail |
| `Overflow` | Value ≥ 9.9 × 10³⁷ (the SCPI overload value, e.g. a DMM out of range) | Fail |
| *(empty)* with ✘ and an error in the event log | The step could not be executed (device not found, connection failed, parse error …). **The test stops** with status *Test Execution Failed*. | Fail |

A response that cannot be converted to a number — for example `OK` or `+1.23E+00 VDC` — is an execution error. Use [Response Processing](10-response-processing.md) to turn such answers into a number. Both `.` and `,` are accepted as decimal separator (`4,5` is 4.5); thousands separators are not supported.

---

## None

The step only switches relays, waits for its delay and optionally shows its comment. It always passes and shows no measured value. Typical uses: configuration steps, delays, operator instructions, connecting a stimulus before the next measurement.

## Script

Runs a script from the [script folder](06-scripting.md) on a device.

**Script Configuration panel**

| Field | Description |
|-------|-------------|
| **Target Device** | Device from the Device Manager, shown as `Name (resource)`. The × button clears it. |
| **Script** | Script from the script folder. Hover over an entry to see its description. The × button clears it. |
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

## External Program

Runs a program on the test PC. By default the **exit code** is the measured value — `0` normally means success, so set the limits to `0 … 0`. With **Evaluate Output** the program's output is the measured value instead.

| Field | Description |
|-------|-------------|
| **Command** | Command line. May contain `{Variable}` references. The first word (or a `"quoted path"`) is the program, the rest are its arguments. |
| **Evaluate Output** | Off: the exit code is the measured value. On: the program runs without a window, its standard output is evaluated like a device response (with [Response Processing](10-response-processing.md) if configured) and must yield a number. A non-zero exit code fails the step; the error message contains the program's error output. |

The command is executed exactly as entered — there is no shell in between. To use shell features such as pipes, redirection, built-in commands (`dir`, `echo`, `type`) or batch files, call the shell yourself:

- **Windows:** `cmd /c <command>` runs the command and closes the window; `cmd /k <command>` keeps the window open — the step then waits until the operator closes it, and the exit code is that of `cmd`, not of your command.
- **Linux:** `bash -c "<command>"`. Use double quotes for arguments that contain spaces; single quotes are not supported. No terminal window is opened on Linux.

> **Terminal windows on Linux:** you can start any terminal emulator yourself, e.g. `gnome-terminal -- bash -c "./flash.sh; exec bash"`. The step then receives the exit code of the **terminal emulator**, not of your command: `gnome-terminal` and `ptyxis` return `0` immediately, before the command has even finished; `xterm` waits but usually returns `0` regardless of the result. If you need a visible window *and* a reliable result, let the command write its exit code to a file and evaluate it with a following [File](#file) step:
>
> ```text
> gnome-terminal --wait -- bash -c "./flash.sh; echo $? > exit.txt"
> ```
>
> `--wait` makes `gnome-terminal` block until the command has finished. Note that `{...}` is interpreted as an ATLab variable reference, so avoid bash syntax like `${VAR}` in the command.

The working directory is the project folder, so you can call scripts stored next to the project with relative paths. While the step waits, the status panel shows *Waiting for external application…*. Stopping the test run terminates the program and everything it started.

Examples:

```text
python flash_firmware.py --port COM7 --sn {SerialNo}
"C:\Program Files\SEGGER\JLink\JLink.exe" -CommanderScript program.jlink
cmd /c program_board.bat {SerialNo}
cmd /c type result.txt | findstr Voltage
```

The last example only makes sense with **Evaluate Output** and a response mask that cuts the number out of the matching line.

## User Confirmation

Shows the step's **Comment** in a dialog titled *Waiting for User Confirmation* with the buttons **Passed** and **Failed**. Clicking **Passed** yields `1`, **Failed** yields `0`. Set all limits to `1` so that only *Passed* passes.

An image added with the image button next to *Show Comment* (Advanced Settings) is shown in the dialog. With the [control module](11-running-tests.md#control-module) enabled, the green button answers *Passed* and the red one *Failed*.

## User Input

Asks the operator to enter a value — for example a reading from an analog gauge, a value from a handheld instrument, or a number printed on the DUT. Shows the step's **Comment** in a dialog titled *Waiting for User Input* with a text box and the buttons **Continue** and **Cancel**. The cursor is placed in the text box, so the operator can start typing right away.

- The entered value is the measured value and is compared with the limits like any other measurement.
- Both `.` and `,` are accepted as decimal separator (`1.5` and `1,5` both give 1.5). Thousands separators and SI prefixes (`k`, `m` …) are not supported — enter a plain number such as `4700` or `4.7e3`.
- With [Response Processing](10-response-processing.md) enabled, the entered text is processed like a device answer before it is converted, e.g. to extract the number from a scanned label such as `R=4.7`.
- Text that cannot be converted to a number is an execution error and stops the test.
- **Cancel** stops the test.

An image added with the image button next to *Show Comment* (Advanced Settings) is shown in the dialog. With the [control module](11-running-tests.md#control-module) enabled, the green button acts as **Continue** and the red one as **Cancel**.

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

Without response processing, the entire file content must be a number (e.g. a file containing `3.274`). Both `.` and `,` are accepted as decimal separator (`3,274` is also 3.274); thousands separators are not supported. With response processing you can cut a fixed-position value out of a larger file with **Skip**/**Length**, or simply check that the file contains a given text (see [Response Processing](10-response-processing.md)). A typical pattern is an External Program step that runs an external tool which writes its result into a file, followed by a File step that evaluates it.

## Boundary Scan

Drives and checks pins of boundary-scan chips. The chain, the BSDL files and the user labels are set up on the [Boundary Scan tab](14-boundary-scan.md).

**Boundary Scan panel**

| Field | Description |
|-------|-------------|
| **Pin actions** / **Turn boundary scan off** | Mode of the step, see below. |
| Action table | One row per pin: **Device**, **Pin** (port and user label) and **Action**. Double-click an action to change it. |
| Add row | Choose device, pin and action, then click **+**. A pin is driven and checked at most once per step; adding it again replaces the existing action. JTAG and power pins cannot be selected. |
| **−** button | Removes the selected action. |
| Import button | **Take over from the Boundary Scan tab**: every output you changed from its safe value on the tab becomes a drive action, every pin with a user label and a read level becomes an expect action. |
| **Settle time (ms)** | Wait time between driving the outputs and reading the inputs. |

| Action | Effect |
|--------|--------|
| **Drive 0** / **Drive 1** | Drives the pin low or high. |
| **Drive Z** | Disables the output (only for pins whose output can be switched off). |
| **Expect 0** / **Expect 1** | Reads the pin and checks its level. |

**Execution**

1. All drive actions are applied. Chips with driven pins are switched to EXTEST, chips that are only read to SAMPLE.
2. After the settle time, all expect actions are read.
3. The measured value is the **number of mismatches**. Set the limits to `0 … 0`: the step passes when every pin has the expected level.

A step with drive actions only produces no value and always passes, like the source *None*. Pins that a step does not list keep their state from the previous steps. For a failed step, hovering the measured value or the result icon shows which pins did not match, e.g. `U3 IO(5) (FAULT_N): expected 0, read 1`; the same lines are written to the event log.

The probe is connected and the chain is checked at the first boundary scan step of the run. A chain that does not match the project, a device without BSDL file or a pin that cannot be driven or read is an execution error and stops the test.

**Turn boundary scan off** returns all chips to normal operation and releases the probe. Use it before steps that need the chips running normally or need the probe themselves, e.g. programming via the same J-Link with an [External Program](#external-program) step. A later boundary scan step reconnects automatically. See also [Boundary scan during test execution](14-boundary-scan.md#boundary-scan-during-test-execution).

Example — check that an LED is driven and its sense line follows:

| Step | Pin actions | Limits |
|------|-------------|--------|
| *LED on* | `U1 PB3 (LED_RED)`: Drive 1 · `U2 PA0 (LED_SENSE)`: Expect 1 | `0 … 0` |
| *LED off* | `U1 PB3 (LED_RED)`: Drive 0 · `U2 PA0 (LED_SENSE)`: Expect 0 | `0 … 0` |
| *Release for programming* | Turn boundary scan off | – |

## Boundary Scan Bus

Runs one transaction on a bus emulated through boundary scan pins: an I²C or SPI device, or a pin group. The buses are set up on the [Boundary Scan tab](14-boundary-scan.md#buses-view); the step works like an [Interface Command](#interface-command).

**Boundary Scan Bus panel**

| Field | Description |
|-------|-------------|
| **Bus** | Bus from the Boundary Scan tab, shown as `Name (Type)`. |
| **Address 0-127** | I²C only: 7-bit target address. Enter it as a decimal number or with `0x` prefix, e.g. `80` or `0x50`. |
| **Command** | Bytes to write, usually as encoding block, e.g. `{hex:10}`. Supports variables and expressions (see [chapter 9](09-commands-variables-expressions.md)). |
| **Read Bytes** | Number of bytes to read afterwards. |

| Bus type | Transaction |
|----------|-------------|
| **I²C** | Start, address + write, command bytes; then, if bytes are to be read, repeated start, address + read and the bytes; stop. A command followed by a read is a typical register read. |
| **SPI** | Chip select becomes active, the command bytes are sent, then **Read Bytes** bytes are clocked in while `0x00` is sent; chip select is released. |
| **Pin Group** | A command drives its value onto bit 0 … n (first byte = bits 0–7). A read releases the pins and reads their levels. |

**Result**

- With **Read Bytes** = 0: `1` when the transaction completed (I²C: acknowledged), `0` on an I²C NACK.
- With **Read Bytes** > 0: the received bytes go through [response processing](10-response-processing.md). Without response processing they are interpreted as ASCII text, so enable it for binary data, e.g. `{byte:2}` for a 16-bit register. A NACK gives `0`.
- On a NACK, the result tooltip explains it, e.g. *No ACK from 0x51 for address 0x51 (write).*

Examples:

| Purpose | Bus | Address | Command | Read Bytes | Mask |
|---------|-----|---------|---------|-----------|------|
| Read 16-bit register 0x10 of an I²C device | I²C | `0x48` | `{hex:10}` | `2` | `{byte:2}` |
| Write two bytes to an EEPROM at address 0x00 | I²C | `0x50` | `{hex:00 12 34}` | `0` | – |
| Read the JEDEC ID of an SPI flash | SPI | – | `{hex:9F}` | `3` | `{byte:3}` |
| Read a 4-bit board ID strap | Pin Group | – | – | `1` | `{byte:1}` |

All signal changes of a transaction are sent to the probe in one transfer. I²C clock stretching is not supported; see [Speed and limitations](14-boundary-scan.md#speed-and-limitations).
