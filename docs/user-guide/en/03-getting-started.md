# 3. Getting Started

## First launch: selecting the test hardware

When ATLab starts, it first tries to reconnect to the Test Interface Adapter on the serial port that was used last time. If that succeeds, the main window opens directly.

If there is no previous port, or the adapter does not answer, the **Test Hardware Selector** window appears:

| Control | Function |
|---------|----------|
| Port list + refresh button | Lists the serial (COM) ports present on the computer. Click refresh after plugging in the adapter. |
| **Connect** | Opens the selected port and initialises the adapter. On success the port is remembered for the next start. |
| **Simulator** | Continues without hardware, using a virtual adapter. |
| **Exit** | Closes ATLab. |

A status line shows the result, for example `Connected to COM5` or `Initialization failed: …`.

### Simulator mode

In simulator mode ATLab behaves as if an adapter with 32 matrix channels, 16 stimulation channels, 4 external stimulation channels and all interfaces (I²C, UART, RS485) were connected. Relay switching and interface commands succeed without doing anything; interface reads return the ASCII character `0`. Instrument devices (serial/TCP/VISA), shell commands, files, expressions and user responses work normally.

While the simulator is active, the status bar shows **No test hardware connected** in red. Simulator mode is ideal for editing sequences at a desk.

> To restart with different hardware, close ATLab and start it again. If the last adapter is still connected, ATLab reconnects automatically; unplug it to get the selector.

## The main window

![Main window](images/Testing_Tab_en.png)

The window has three areas:

1. **Tab bar** at the top:
   - **Test Execution** – build and run the test sequence ([chapter 7](07-building-test-sequences.md), [chapter 11](11-running-tests.md))
   - **Configuration** – devices, variables, channel names, project settings ([chapter 5](05-configuration.md))
   - **Documentation** – notes, images, attachments, DUT data ([chapter 13](13-documentation-tab.md))
   - **Scripting** – the instrument script editor ([chapter 6](06-scripting.md))
   - **Test Hardware** – adapter information and diagnostics ([chapter 14](14-test-hardware.md))
   - **About** – version and system information
2. **Tab content** in the middle.
3. **Status bar** at the bottom.

The window title shows the project name, followed by `*` when there are unsaved changes, e.g. `ATLab - MyBoard*`.

### Status bar

| Element | Function |
|---------|----------|
| Info button (left) | Opens the **event log**: every info, warning and error message of this session, newest first. |
| **No test hardware connected** | Shown in simulator mode. |
| Probe button (right) | **Determine matrix channel via external probe** (`F6`). Touch a net with the external probe and ATLab shows `Matrix Channel: n`. See [Test Hardware](14-test-hardware.md#finding-a-matrix-channel-with-the-probe). |
| Sun/moon button | **Toggle theme** between light and dark. |
| Languages button | **Toggle language** between English and German. |
| Book button (far right) | **Open user guide** (`F1`). Opens this guide as a PDF in the current UI language. If the PDF is missing (e.g. in a self-built development version), the English PDF or the online guide on GitHub is opened instead. |

Theme and language are remembered between sessions. Messages also pop up briefly as notifications in the bottom-left corner; the project setting **Log Level** controls which ones (see [Configuration](05-configuration.md#general-settings)).

![Light and dark theme](../../Themes.png)

### Operator view vs. development mode

The Test Execution tab has two faces:

- **Operator view** (default): toolbar, step list, result bar. The step configuration panel is hidden.
- **Development mode**: toggled with the construction-cone button at the right end of the toolbar (**Enable development mode**). The configuration panels appear on the right and the debug controls become available.

The setting is remembered, so a test station can be left in operator view.

## Your first test in five minutes

This walkthrough works in simulator mode and needs no instruments.

1. **Create a project.** On the Test Execution tab click **New project** (or press `Ctrl+N`).
2. **Enable development mode** with the construction-cone button.
3. **Add a step.** Click **Add a new test step** (`+`, or press `Insert`). A new row appears and is selected.
4. **Configure it** in the **Test Step Configuration** panel on the right:
   - **Name**: `Operator check`
   - **Evaluate**: `User Response`
   - **Comment**: `Is the power LED green?`
   - **Nominal**: `1` — the lower and upper limits are filled in automatically from the default tolerance; set both **Lower Limit** and **Upper Limit** to `1`.
5. **Add a second step** that calculates something:
   - **Name**: `Supply ratio`
   - **Evaluate**: `Expression`
   - In the **Expression** panel enter `12 / 4`
   - **Nominal** `3`, **Lower Limit** `2.9`, **Upper Limit** `3.1`
6. **Save** with `Ctrl+S`; choose a location and a name such as `FirstTest.atlab`.
7. **Run** with `F5` (or the **Start test execution** ▶ button).
   - The first step asks *"Is the power LED green?"*. Click **Passed** (result `1`) or **Failed** (result `0`).
   - The second step evaluates `12 / 4 = 3` and passes.
8. The result bar turns green (**Test Passed**) or red (**Test Failed**). The **Measured Value**, **Deviation** and **Result** columns are filled in.

From here, continue with [Configuration](05-configuration.md) to add real instruments.
