# 2. Installation

ATLab is distributed through the project's GitHub releases page: <https://github.com/TobiasNetzer/ATLab/releases>.

Each release provides:

| Asset | Platform | Notes |
|-------|----------|-------|
| `ATLab-setup-<version>.exe` | Windows (64-bit) | Installer with Start-menu entry and `.atlab` file association |
| `ATLab-win-<version>.zip` | Windows | Portable build, no installation |
| `ATLab-linux-<version>.tar.gz` | Linux | Portable build |

All packages include this user guide as PDF (English and German) in the `docs` subfolder of the program folder, e.g. `C:\Program Files\ATLab\docs\ATLab-User-Guide-en.pdf`. Open it from within ATLab with the book button in the status bar or `F1`.

## Windows

### Using the installer

1. Download and run `ATLab-setup-<version>.exe`. Administrator rights are required.
2. Accept the license (GNU GPL v3) and choose an installation folder. The default is `C:\Program Files\ATLab`.
3. On the last page you can launch ATLab immediately and create a desktop shortcut.

The installer:

- adds **ATLab** to the Start menu,
- registers the `.atlab` extension, so double-clicking a project file opens it in ATLab,
- adds an uninstaller under *Settings → Apps*.

Uninstalling removes the program folder, shortcuts and the file association. Your projects, scripts and personal settings are not removed (see [File locations](17-reference.md#file-locations)).

### Portable ZIP

Extract `ATLab-win-<version>.zip` to any folder and start `ATLab.exe`. No file association is registered.

## Linux

```bash
mkdir -p ~/ATLab
tar -xzf ATLab-linux-<version>.tar.gz -C ~/ATLab
cd ~/ATLab
chmod +x ATLab
./ATLab
```

To access the Test Interface Adapter's serial port, your user usually needs to be in the `dialout` group (or your distribution's equivalent):

```bash
sudo usermod -aG dialout $USER   # log out and back in afterwards
```

## Optional: NI-VISA

VISA devices require the **NI-VISA** runtime from National Instruments. VISA support is currently **Windows only**. Serial and TCP/IP devices work without it.

## Optional: JTAG probe drivers

Boundary scan needs the driver of the JTAG probe (see [Boundary Scan Tab](12-boundary-scan.md#supported-probes)):

- **SEGGER J-Link:** *J-Link Software and Documentation Pack* from <https://www.segger.com>.
- **FTDI FT2232H / FT232H / FT4232H:** *D2XX* driver from <https://ftdichip.com>. On Linux, unload the `ftdi_sio` kernel module or add a udev rule so the D2XX library can access the device.

If ATLab does not find the library in the default install folder, copy it (`JLink_x64.dll`, `ftd2xx.dll`, `libjlinkarm.so`, `libftd2xx.so`) into the ATLab program folder.

## Optional: Control Module

The USB control module (a HID device with Start/Stop buttons) needs no driver. It is enabled per project, see [Running Tests](14-running-tests.md#control-module).

## Building from source

Prerequisites: **.NET 10 SDK** or newer.

```bash
git clone https://github.com/TobiasNetzer/ATLab.git
cd ATLab
dotnet restore
dotnet build -c Release
dotnet run --project src/ATLab/ATLab.csproj
```

You can also open `ATLab.sln` in JetBrains Rider, Visual Studio 2022 or VS Code with the C# Dev Kit.
