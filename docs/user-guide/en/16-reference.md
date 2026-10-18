# 16. Reference

## Keyboard shortcuts

### Global (main window)

| Shortcut | Action |
|----------|--------|
| `Ctrl+N` | New project |
| `Ctrl+O` | Open project |
| `Ctrl+S` | Save project |
| `Ctrl+Shift+S` | Save project as |
| `F5` | Start test execution |
| `F3` | Execute single step |
| `F10` | Advance single step (debug mode) |
| `Enter` | Continue test execution (end a repeating step) |
| `Esc` | Stop test execution |
| `F6` | Determine matrix channel via external probe |
| `F1` | Open user guide (PDF in the current UI language) |

### Step list (Test Execution tab)

| Shortcut | Action |
|----------|--------|
| `Insert` | Add step |
| `Delete` | Remove selected steps |
| `Ctrl+C` | Copy selected steps |
| `Ctrl+X` | Cut selected steps |
| `Ctrl+V` | Paste steps |
| `Ctrl`/`Shift` + click | Multi-select |

## Toolbar of the Test Execution tab

From left to right:

| Group | Buttons |
|-------|---------|
| Project | New project · Open project · Save project · Save project as |
| Editing | Add · Remove · Move up · Move down · Duplicate · Cut · Copy · Paste |
| Execution | Execute single step · Start from selected step · Start selected module · Start continuous execution · Start test execution · Continue (during run) · Stop |
| Debug (development mode only) | Enable debug mode · Advance single step |
| View | Show measurement panel · Enable development mode |

The **Modules** panel (development mode) has its own buttons: Add · Remove · Move up · Move down · Duplicate.

## Default values

### Test step

| Property | Default |
|----------|---------|
| Lower / Nominal / Upper limit | `0` |
| Unit | *(empty)* |
| Delay | `0` s |
| Evaluate | None |
| Matrix H / L | Off |
| Pass-Action / Fail-Action | Continue |
| Device command timeout | 1000 ms |
| Interface: type / I²C speed / address | I2C / 100 kHz / 0 |
| Interface: baud / data bits / parity / stop bits | 9600 / 8 / None / 1 |
| Interface: timeout | 1000 ms |

### Script command

| Property | Default |
|----------|---------|
| Delay | 0 ms |
| Timeout | 1000 ms |
| Expect Response / Evaluate | off |

### Device

| Interface | Defaults |
|-----------|----------|
| Serial Port | 115200 baud, 8 data bits, no parity, 1 stop bit, no handshake, no termination, Chunk framing, framing timeout 100 ms |
| TCP/IP | Port 5025, timeout 2000 ms, termination LF |
| VISA | Timeout 2000 ms, termination LF |

### Project settings

| Setting | Default |
|---------|---------|
| Default Tolerance | 10 % |
| Displayed Decimal Places | 3 |
| Request Serial Number | off |
| Save Test Result | off (Always) |
| Export CSV / PDF | off |
| Serial number validation | off |
| Control module | off |
| Log Level | Info |

## File locations

| File | Windows | Linux |
|------|---------|-------|
| Personal settings | `%LOCALAPPDATA%\ATLab\settings.json` | `~/.local/share/ATLab/settings.json` |
| Event log | `%LOCALAPPDATA%\ATLab\events.log` | `~/.local/share/ATLab/events.log` |
| Default script folder | `%LOCALAPPDATA%\ATLab\Scripts\` | `~/.local/share/ATLab/Scripts/` |
| Program (installer) | `C:\Program Files\ATLab\` | wherever you extracted it |
| User guide PDFs | `<program folder>\docs\ATLab-User-Guide-en.pdf` / `-de.pdf` | `<program folder>/docs/ATLab-User-Guide-en.pdf` / `-de.pdf` |
| Projects | `*.atlab`, anywhere | `*.atlab`, anywhere |
| Scripts | `<script folder>\<id>.json` | `<script folder>/<id>.json` |

### settings.json

Personal, per-user settings — not part of any project:

| Key | Meaning |
|-----|---------|
| `WindowWidth`, `WindowHeight`, `WindowX`, `WindowY`, `WindowState` | Window size and position |
| `LastComPort` | Serial port of the Test Interface Adapter used for automatic reconnection |
| `LastOpenedFile` | Project reopened at start-up |
| `IsDarkMode` | Theme |
| `Language` | `en` or `de` |
| `IsForceSimulation` | `true` = always start in simulator mode without trying to connect to hardware |
| `IsDevelopmentMode` | Development mode on/off |
| `IsShowMeasurementPanel` | Measurement panel visible |
| `Is…Expanded` | Which editor panels are expanded |

`IsForceSimulation` has no switch in the user interface. To use it, close ATLab, set it to `true` in `settings.json` and restart — handy for an engineering PC that never has hardware attached.

## Supported interfaces at a glance

| Interface | Used for | Requirements |
|-----------|----------|--------------|
| Serial port (VCOM/RS232) | Instruments, DUT, Test Interface Adapter | – |
| TCP/IP raw socket | LAN instruments (SCPI-RAW) | network access |
| VISA (USB, GPIB, ASRL, TCPIP) | Instruments | NI-VISA, Windows only |
| Adapter I²C | DUT | adapter with I²C |
| Adapter UART | DUT | adapter with UART |
| USB HID | Control module | – |

## Glossary

| Term | Definition |
|------|------------|
| **Adapter / TIA / cTIA** | Test Interface Adapter (compact Test Interface Adapter), the USB relay and interface hardware ATLab controls. |
| **Deviation** | `(measured − nominal) / nominal × 100 %`. |
| **DUT** | Device Under Test. |
| **Evaluation source** | Mechanism that produces a step's measured value. |
| **Ext. stimulation channel** | Relay that connects an external stimulus (e.g. a power supply) to the DUT. |
| **Framing** | Rule for detecting the end of a serial response. |
| **Matrix H / L** | High and low side of the measurement bus, each connectable to one matrix channel. |
| **Mask** | Pattern used by response processing to validate or extract data. |
| **Overflow** | Result ≥ 9.9 × 10³⁷, the SCPI "overload" value. |
| **Runtime variable** | Project-wide variable, readable everywhere and writable by steps. |
| **SCPI** | Standard Commands for Programmable Instruments, e.g. `MEAS:VOLT:DC?`. |
| **Script folder** | Folder containing script JSON files. |
| **Simulator** | Virtual adapter used when no hardware is connected. |
| **Stimulation channel** | Relay on the adapter used to apply a signal or load to the DUT. |
