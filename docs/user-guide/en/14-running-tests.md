# 14. Running Tests

Tests are run from the **Test Execution** tab. Operators normally work with development mode switched off, so they see only the toolbar, the step list and the result bar.

![Running a test](images/Testing_Tab_en.png)

## Execution controls

| Toolbar button | Shortcut | Action |
|----------------|----------|--------|
| **Execute single step** | `F3` | Runs only the selected step (see below). |
| **Start test execution of selected module** | `F4` | Runs only the steps of the selected module, even if the module is disabled. |
| **Start test execution from selected step** | `Shift+F4` | Runs the selected step's module, starting at the selected step. The run ends with the last step of the module, even if the module is disabled. |
| **Start continuous test execution** | `Ctrl+F5` | Runs the whole sequence in an endless loop until stopped. |
| **Start test execution** (▶) | `F5` | Runs the whole sequence once. |
| **Continue test execution** | `Enter` | Visible during a run. Ends a step that is repeating (Pass/Fail-Action *Repeat*) and proceeds to the next step. |
| **Stop test execution** (■) | `Esc` | Cancels the running test. |

While a test runs, editing and project commands are disabled, and the module list and step list follow the current step. *Start test execution* and *Start continuous test execution* only execute enabled modules.

### How a test step is executed

Every step goes through the same stages:

1. **Comment dialog (optional)** — if *Show Comment* is enabled, the comment is shown and the operator must click **Continue**.
2. **Resolve values** — limits, nominal value and delay are calculated, including any `{Variable}` references.
3. **Switch relays** — matrix, stimulation and external stimulation relays are set to the step's configuration.
4. **Wait** — the step's delay elapses (a progress bar appears for delays over one second).
5. **Acquire** — the evaluation source produces a raw value; response processing may extract a number from it.
6. **Evaluate** — the value is compared against the lower and upper limit; the deviation from nominal is calculated.
7. **Decide** — the Pass-Action or Fail-Action determines what happens next (continue, repeat, jump, end, or show a comment).

Before the first step and after the last one, all relays are cleared.

### Differences between the start modes

| | Start test | Continuous | From selection | Module | Single step |
|---|:---:|:---:|:---:|:---:|:---:|
| Asks for serial number (if enabled) | ✔ | ✔ (once) | – | – | – |
| Automatic result export (if enabled) | ✔ | ✔ (every run) | – | – | – |
| Clears relays before and after | ✔ | ✔ | ✔ | ✔ | – |
| Follows Pass/Fail-Actions | ✔ | ✔ | ✔ | ✔ | – |
| Shows *Show Comment* dialog before the step | ✔ | ✔ | ✔ | ✔ | – |
| Updates the run counters | ✔ | ✔ | – | – | – |

**Start test execution of selected module** and **Start test execution from selected step** are meant for developing a single module. A *Jump To* to a step outside the module ends the run with an error.

**Execute single step** is meant for development: it switches the step's relays, waits, acquires and evaluates the value, and shows the result — nothing else. The relays **stay in the step's state** afterwards. Use it to tune masks, limits and delays. Ignored steps are not executed. A boundary scan chain used by the step also stays under test control, so the next single step continues from its pin states; press **Stop** (`Esc`) to release it (see [Boundary scan during test execution](12-boundary-scan.md#boundary-scan-during-test-execution)).

## Serial number entry

If **Request Serial Number** is enabled in the project settings, the **Enter Serial Number** dialog appears when you press *Start test execution* or *Start continuous test execution*. A barcode scanner that acts as a keyboard can be used.

- **Start** begins the test. It is only enabled when the input is not empty and — if validation is enabled — satisfies the validation rules.
- **Cancel** aborts the start.

The serial number appears in the result bar's **Serial Number** field, in the file names of exported results and in the PDF report.

## Operator dialogs

During a run, ATLab may show dialogs:

| Dialog | Title | Buttons | Effect |
|--------|-------|---------|--------|
| Show Comment (before step) | *User Information* | **Continue** / **Cancel** | Cancel stops the test. |
| Pass/Fail-Action *Show Comment* | *User Information* | **Continue** / **Cancel** | Optionally shows the measured value. Cancel stops the test. |
| User Confirmation step | *Waiting for User Confirmation* | **Passed** / **Failed** | Result `1` or `0`. |
| User Input step | *Waiting for User Input* | **Continue** / **Cancel** | The entered number is the result. Cancel stops the test. |

Dialogs may contain an image. Closing a dialog without choosing a button counts as **Cancel** (or **Failed** for a user confirmation).

## The result bar

Below the step list:

| Element | Description |
|---------|-------------|
| Status banner | **Running Test** with a progress bar, then **Test Passed** (green), **Test Failed** (red), **Test Canceled** or **Test Execution Failed**. |
| **Test Duration** | Elapsed time of the run. |
| **Failed Steps** | Number of failed steps in this run. Highlighted in red when greater than zero. |
| **Passed Tests** | Passed runs / total runs since the project was opened, with percentage. Only *Start test execution* and *Start continuous test execution* are counted. Useful for continuous runs. |
| **Operator** | Operating-system user name; also printed as *Operator* in the report. |
| **Device Under Test** | Device name, revision, variant and part number from the [Documentation tab](13-documentation-tab.md#device-under-test), or `-`. Hover for the individual fields. |
| **Serial Number** | Serial number of the current run, or `-`. Hover to see a long serial number in full. |

If the window is narrow, *Device Under Test* and *Serial Number* are shortened with "…".

### Final verdict

| Status | When |
|--------|------|
| **Test Passed** | The run completed and no step failed. |
| **Test Failed** | The run completed and at least one step failed (including timeouts and overflows). |
| **Test Canceled** | The run was stopped by the operator (Stop button, `Esc`, control module, or *Cancel* in a dialog). |
| **Test Execution Failed** | A step could not be executed at all (e.g. device unreachable, unparsable answer). The run is aborted. |

Results are only exported for **Passed** and **Failed** runs.

## Navigating failed steps

After a run, two buttons to the right of the execution controls jump between the failed steps:

| Button | Shortcut | Function |
|--------|----------|----------|
| **Go to previous failed test step** (outlined arrow up with bar) | `Shift+F8` | Selects the closest failed step before the selected step. |
| **Go to next failed test step** (outlined arrow down with bar) | `F8` | Selects the closest failed step after the selected step. If no step is selected, it selects the first failed step. |

The search covers all modules in their order and switches the module list to the module of the found step. A button is only enabled if there is a failed step in its direction, so both are disabled when no executed step has failed. Both are disabled while a test is running. They are also available with development mode switched off.

## Measurement panel

The magnifier button (**Show measurement panel**) toggles a large display between the step list and the result bar. It shows the selected step's lower limit, measured value and upper limit on a scale — readable from a distance and handy when adjusting a potentiometer with a repeating step.

## Debug mode

In development mode, two extra buttons appear to the right of the failed step buttons:

| Button | Shortcut | Function |
|--------|----------|----------|
| **Enable debug mode** (bug icon) | `F9` | When active, every started run pauses **before each step**. |
| **Advance single step** (curved arrow with dot) | `F10` | Executes the step the run is paused at and pauses again before the next one. |

Debug mode lets you walk through a sequence step by step with all Pass/Fail-Actions, dialogs and relay changes, and inspect results and relay states in between. Stop the run at any time with `Esc`. Debug mode can only be switched while no test is running.

## Control module

The control module is a USB HID device with two illuminated buttons. Enable it in the project settings (**Enable Control Module**).

| Situation | Left (green) button | Right (red) button |
|-----------|--------------------|--------------------|
| Idle | Start test (like `F5`) | Stop test |
| During a repeating step | Continue (like `Enter`) | Stop test |
| Serial number dialog | Start (if input is valid) | Cancel |
| User Confirmation dialog | **Passed** | **Failed** |
| User Input dialog | **Continue** | **Cancel** |
| Comment dialog | **Continue** | **Cancel** |

The module's status LEDs show the test state: idle, running, passed, failed and canceled. If the module is not found when the setting is enabled, the message *"No control module connected."* is logged.

## Stopping a test safely

When a test ends — normally, by failure, or when stopped — ATLab returns all boundary scan chips to normal operation, clears all relays of the Test Interface Adapter and closes the connection to the instruments. With **Execute single step** the relays are left as set by the step; run a full test or switch to a step without relays to clear them.
