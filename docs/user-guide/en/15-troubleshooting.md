# 15. Troubleshooting

## The event log

Most problems are explained by a message in the **event log**. Open it with the info button at the left end of the status bar. Messages are listed newest first and are also appended to the log file:

- Windows: `%LOCALAPPDATA%\ATLab\events.log`
- Linux: `~/.local/share/ATLab/events.log`

The file grows over time; you can delete it while ATLab is closed. Error messages in the step context start with *"Error in module '\<name\>', step \<n\>: …"*.

## Start-up and hardware

| Symptom | Cause / solution |
|---------|------------------|
| The Test Hardware Selector appears every time | The adapter was not found on the last-used port. Check the USB cable, select the right port and click **Connect**. |
| `Failed to connect to COMx` | Port is in use by another program, or (Linux) missing permission — add your user to the `dialout` group. |
| `Initialization failed: …` | A device answered on the port but it is not a Test Interface Adapter, or its firmware does not respond. Check the port and power-cycle the adapter. |
| ATLab connects automatically, but I want the simulator | Unplug the adapter before starting ATLab, then choose **Simulator**. |
| Status bar shows *No test hardware connected* | ATLab runs in simulator mode; relays and interfaces are not really switched. Restart with the adapter connected. |

## Opening projects

| Symptom | Cause / solution |
|---------|------------------|
| Warning *Connected test hardware is missing required channels* | The project was made for an adapter with more channels. Don't save; open it with the right hardware or the simulator. |
| Scripts are missing in the script selector | The project's script folder does not exist on this computer. ATLab then creates an empty folder at that location (or falls back to the default folder) and logs a message. Point it to the right folder on the Scripting tab (**Change script folder**) and save the project. Keeping the script folder inside the project folder avoids this. |
| *Unsupported project file format version 0 (expected 2)* | The project was saved by an ATLab version without modules. These files cannot be opened. |
| *Image not found* / *File not found* | Referenced files were moved. Keep images and attachments in the project folder so relative paths work. |

## Instruments

| Symptom | Cause / solution |
|---------|------------------|
| Step shows `Timeout` | No (complete) answer within the timeout. Check the command (does it really return something?), increase **Timeout (ms)**, check the termination/framing settings. For serial devices with SCPI, use **LF Terminated** framing. |
| *Target Device ID … not found* | The step refers to a device that was deleted. Select a device again in the step. |
| *Device is required.* / *Script and Device are required.* | No target device (or script) selected in the step. |
| *TCP connection error* / *Connection timed out.* | Wrong IP address/port, instrument not on the network, firewall. Port 5025 is the usual SCPI socket port. |
| *VISA error: …* | NI-VISA not installed, wrong resource string, or instrument not visible in NI MAX. VISA is Windows-only. |
| *Failed to convert result '…'* | The answer is not a plain number. Enable [Response Processing](10-response-processing.md) to extract the value. |
| *Response too short: …* | The answer has fewer bytes than **Skip** + **Length** of the response processing. Check the answer under *Received Sequence*: a truncated answer points to the framing or timeout; otherwise correct Skip/Length. |
| *Numeric extraction … failed …* | The processed part is not in the expected format (e.g. a prefix left over, or too few bytes for `{byte:N}`). Adjust Skip/Length so that only the number remains, or put a match rule for the label in front of the extraction, e.g. `"Result=" {float}`. See [Response Processing](10-response-processing.md#reading-a-number-after-a-label). |
| Answers are truncated | Serial **Chunk** framing returned early; the rest of the answer is discarded before the next command. Switch to **LF Terminated**, **CRLF Terminated** or **Timeout Based**. |
| Answers belong to the previous command | The device answers slower than the step's **Timeout (ms)**, and the late answer arrived after the next command was sent. Late answers that arrive *before* the next command are discarded automatically. Increase **Timeout (ms)** for the slow command. |
| Instrument ignores commands over VISA socket | Append a terminator explicitly, e.g. `{hex:0A}`. |

## Test steps

| Symptom | Cause / solution |
|---------|------------------|
| *Matrix Channel is not available.* | The step uses a matrix channel that the connected adapter does not have. |
| *Interface not available* | The adapter has no I²C/UART interface, or RS485 was selected (not supported for steps). |
| *I²C write failed: NACK received.* | Wrong address (7-bit, 0–127), DUT not powered, missing pull-ups, or wrong speed mode. |
| *Failed to evaluate expression* | Syntax error (e.g. missing operand or parenthesis), an unsupported character such as `<`, or a variable that holds text instead of a number (e.g. `Timeout`). |
| Limits changed after entering the nominal value | Intended: limits are recalculated from the **Default Tolerance**. Enter the nominal first, then the limits. |
| Delay is much longer than expected | The delay is in **seconds**. Use `500ms` for half a second. |
| *Lower limit is greater than upper limit* | Fix the limits, typically when they are calculated from variables. |
| *Step … tries to jump to a step that does not exist or belongs to a disabled module* | The *Jump To* target was deleted, or its module is disabled (or not part of the run when only one module is executed, e.g. when starting from the selected step). Choose a new target or enable the module. |
| A runtime variable has an unexpected start value | Values assigned in a run persist into the next run. Reset the variable in an early step if needed. |
| Shell command fails with *The system cannot find the file specified* | The command is not run through a shell. Built-in commands (`dir`, `echo`, …), pipes and batch files need `cmd /c` (Windows) or `bash -c "…"` (Linux) in front. |

## Results and reports

| Symptom | Cause / solution |
|---------|------------------|
| No files are exported | Check that **Request Serial Number**, **Save Test Result** and at least one export format are enabled, an **Output Path** is set, the run was started with **Start test execution**, and it ended as passed or failed (not canceled). With *Passed Only*, failed runs are not exported. |
| CSV opens in a single column | The file is tab-separated. Choose *Tab* as delimiter when importing. |
| Report generation is slow | A device marked *Include in Test Report* does not answer its identification query. Switch it on or untick the option. |
| Device shows `-` under Test Equipment | No identification query configured, or no answer. |

## Control module

| Symptom | Cause / solution |
|---------|------------------|
| *No control module connected.* | Module not plugged in when the option was enabled. Plug it in and toggle **Enable Control Module** off and on. |
| *Lost connection to control module.* | The module was unplugged. Reconnect and toggle the option. |

## Resetting personal settings

Window position, theme, language, last port and last project are stored in `settings.json` (see [File locations](16-reference.md#file-locations)). Close ATLab and delete the file to return to defaults. Projects and scripts are not affected.
