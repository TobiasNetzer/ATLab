# 17. Reference

## Keyboard shortcuts

### Global (main window)

| Shortcut | Action |
|----------|--------|
| `Ctrl+N` | New project |
| `Ctrl+O` | Open project |
| `Ctrl+S` | Save project |
| `Ctrl+Shift+S` | Save project as |
| `F5` | Start test execution |
| `Ctrl+F5` | Start continuous test execution (development mode) |
| `F3` | Execute single step (development mode) |
| `F4` | Start test execution of selected module (development mode) |
| `Shift+F4` | Start test execution from selected step (development mode) |
| `F8` | Go to next failed test step |
| `Shift+F8` | Go to previous failed test step |
| `F9` | Enable / disable debug mode (development mode) |
| `F10` | Advance single step (debug mode) |
| `F12` | Determine matrix channel via external probe |
| `Enter` | Continue test execution (end a repeating step) |
| `Esc` | Stop test execution |
| `F1` | Open user guide (PDF in the current UI language) |

### Step list (Test Execution tab)

| Shortcut | Action |
|----------|--------|
| `Insert` | Add step (development mode) |
| `Delete` | Remove selected steps (development mode) |
| `Ctrl+C` | Copy selected steps |
| `Ctrl+X` | Cut selected steps (development mode) |
| `Ctrl+V` | Paste steps (development mode) |
| `Ctrl+D` | Duplicate selected steps (development mode) |
| `F2` | Edit the name of the selected step (development mode) |
| `Ctrl`/`Shift` + click | Multi-select |

## Toolbar of the Test Execution tab

From left to right:

| Group | Buttons |
|-------|---------|
| Project | New project · Open project · Save project · Save project as |
| Editing | Add · Remove · Move up · Move down · Duplicate · Cut · Copy · Paste |
| Execution | Execute single step · Start selected module · Start from selected step · Start continuous execution · Start test execution · Continue (during run) · Stop |
| Failed steps | Go to previous failed step · Go to next failed step |
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
| Boundary scan: mode / settle time / verify driven pins | Pin actions / 0 ms / off |
| Boundary scan bus: address / read bytes | 0 / 0 |

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

### Boundary scan

| Setting | Default |
|---------|---------|
| Probe type | SEGGER J-Link |
| Serial number | *(empty)* — first available probe |
| TCK frequency | 1000 kHz |
| FTDI ADBUS value / direction | `08` / `0B` |
| Boundary register after loading a BSDL file | safe values from the BSDL file |
| SPI bus | mode 0, CS active low, MSB first |
| Pin group | 8 pins |

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
| `IsDevelopmentModePasswordEnabled` | Development mode requires a password |
| `DevelopmentModePasswordHash`, `DevelopmentModePasswordSalt` | Salted PBKDF2 hash of the development mode password |
| `IsShowMeasurementPanel` | Measurement panel visible |
| `Is…Expanded` | Which editor panels are expanded |

Language, theme, `IsForceSimulation` and the development mode password are set in the [settings dialog](03-getting-started.md#settings). If the password is forgotten, close ATLab, set `IsDevelopmentModePasswordEnabled` to `false` in `settings.json` and restart.

## Supported interfaces at a glance

| Interface | Used for | Requirements |
|-----------|----------|--------------|
| Serial port (VCOM/RS232) | Instruments, DUT, Test Interface Adapter | – |
| TCP/IP raw socket | LAN instruments (SCPI-RAW) | network access |
| VISA (USB, GPIB, ASRL, TCPIP) | Instruments | NI-VISA, Windows only |
| Adapter I²C | DUT | adapter with I²C |
| Adapter UART | DUT | adapter with UART |
| USB HID | Control module | – |
| JTAG (SEGGER J-Link) | Boundary scan | J-Link Software and Documentation Pack |
| JTAG (FTDI FT2232H/FT232H/FT4232H) | Boundary scan | FTDI D2XX driver |

## Glossary

| Term | Definition |
|------|------------|
| **BSDL** | Boundary Scan Description Language. File from the chip manufacturer that describes the boundary register, the instructions and the package pinout of a chip. |
| **BYPASS / SAMPLE / EXTEST** | Boundary scan instructions: chip bypassed / pins read without affecting the chip / pins read and driven from the boundary register. |
| **Boundary register** | Chain of cells, one or more per pin, through which boundary scan reads and drives the pins. |
| **IDCODE** | 32-bit identification of a chip (manufacturer, part number, version), read during chain detection. |
| **JTAG** | IEEE 1149.1 test access port with the signals TCK, TMS, TDI, TDO (and optionally TRST). |
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
