# 7. Scripting Tab

A **script** is a named, reusable sequence of instrument commands, for example *"Configure & Query DC Voltage"*. Instead of typing the same SCPI commands into many steps, you write them once as a script and select the script in each step. Variables let one script serve many steps with different settings.

![Scripting tab](images/Scripting_Tab_en.png)

## The script folder

Scripts are stored as individual JSON files (`<id>.json`) in a **script folder**, independent of any project. Several projects can share one script folder.

- The script folder path is saved in the project. If it lies inside the project folder, it is stored relatively.
- A new project uses the default script folder:
  - Windows: `%LOCALAPPDATA%\ATLab\Scripts`
  - Linux: `~/.local/share/ATLab/Scripts`
- If the configured folder does not exist, ATLab tries to create it; if that fails it falls back to the default folder and logs a message.

## Toolbar

| Button | Function |
|--------|----------|
| **New script** | Creates a new, empty script and saves it to the script folder immediately. |
| **Save script** | Saves the selected script. |
| **Delete script** | Permanently deletes the selected script file after confirmation. |
| **Change script folder** | Selects another folder as script folder and reloads the list. |
| **Reload script folder** | Re-reads all scripts from disk, e.g. after pulling changes from version control. |

Scripts with unsaved changes are marked with `*` in the list, and the tab title shows **Scripting\*** while any script is unsaved. Script changes are **not** saved with the project — use **Save script**.

## Editing a script

Select a script in the **Scripts** list. On the right you can edit:

- **Name** – shown in the step's script selector.
- **Description** – shown as a tooltip in the script selector.
- **Variables** – placeholders with default values.
- **Commands** – the command list.

### Variables

| Column | Description |
|--------|-------------|
| **Variable Name** | Letters, digits and `_` only. |
| **Default Value** | Value a step starts with when the script is first selected. |

Use a variable in a command by writing its name in curly braces, e.g. `SENS:VOLT:DC:RANGE {Range}`. Each test step that uses the script gets its own copy of the variables and can override the values (see [Script evaluation](09-evaluation-sources.md#script)).

### Commands

| Column | Description |
|--------|-------------|
| **Command** | Text sent to the instrument. May contain script variables, runtime variables, encoding blocks and expressions ([chapter 10](10-commands-variables-expressions.md)). Empty commands are skipped. |
| **Expect Response** | Send the command and wait for an answer (a query). If off, the command is only written. |
| **Evaluate** | Use the answer of this command as the step's measured value. Only **one** command per script can be marked; marking another clears the previous one. Requires **Expect Response** to be on as well. |
| **Check** | Check the answer of this command with its own mask and limits, see [Response checks](#response-checks). Only for commands with **Expect Response** that are not marked **Evaluate**. |
| **Delay (ms)** | Wait time **before** the command is sent. |
| **Timeout (ms)** | How long to wait for the answer (default 1000 ms). Exceeding it ends the step with result `Timeout`. |

Commands are executed from top to bottom. Add or remove commands with **+**/**−** or the context menu; a new command is inserted below the selected one.

### Response checks

The **Evaluate** command delivers the step's measured value, which the step compares with its limits. Often other answers matter too — a configuration command must answer `OK`, or the error queue must be empty before the measurement counts. For this, each other command with **Expect Response** can check its own answer.

Select the command in the list. The **Response Check** panel below the list shows its settings:

| Field | Description |
|-------|-------------|
| **Check the response of this command** | Turns the check on (same as the **Check** column). |
| **Mask**, **Skip**, **Length** | Processing of the answer, exactly as in [Response Processing](11-response-processing.md). An empty mask reads the answer as a number. |
| **Lower Limit**, **Upper Limit** | The processed result must lie within these limits (inclusive). |

Checks are part of the script, so every step that uses the script runs them. During execution:

- All commands are sent, even after a failed check.
- If a check fails — result outside the limits, or the answer cannot be processed (e.g. too short, not a number) — the step **fails**, even if its measured value is within the step's limits. The measured value is still shown, and the step's Fail-Action applies.
- Hovering the result shows which check failed and what was received, e.g. `Command 2 'SYST:ERR?': received '-113,"Undefined header"', result 0 not within 1 … 1.` The same text is written to the event log as a warning when the step has finished.
- A timeout on a checked command ends the step with `Timeout`, like any other command.

Typical masks: `"OK"` with limits `1 … 1` (answer must contain `OK`), `{dec}` with limits `0 … 0` (a bare status value such as `+0` must be 0), or an empty mask with limits `4.9 … 5.1` (a read-back value must be in range).

The **Evaluate** command itself has no check in the script; its answer is processed with the step's **Response Processing** panel and compared with the step's limits.

## Example: DC voltage measurement

**Variables**

| Variable Name | Default Value |
|---------------|---------------|
| `Range` | `100` |
| `NPLC` | `5` |

**Commands**

| Command | Expect Response | Evaluate | Delay (ms) | Timeout (ms) |
|---------|:---:|:---:|---:|---:|
| `SENS:VOLT:DC:RANGE {Range}` | | | 0 | 1000 |
| `SENS:VOLT:NPLC {NPLC}` | | | 0 | 1000 |
| `READ?` | ✔ | ✔ | 500 | 1000 |

A step using this script sends the two configuration commands, waits 500 ms, queries `READ?` and uses the instrument's answer (e.g. `+1.2001555E+01`) as its measured value.

To make sure the instrument accepted the configuration, add a final command `SYST:ERR?` with **Expect Response** and **Check** on, Mask `"No error"`, limits `1 … 1`. The instrument answers `+0,"No error"` when everything was accepted; any other answer fails the step. (A numeric extraction such as `{dec}` does not work here: without a label in front, it must read the whole answer as a number, and `+0,"No error"` is not one.)

## Good practice

- Keep scripts small and generic; push step-specific values into variables.
- Put the script folder next to your projects and under version control.
- If a script returns more than a bare number, use [Response Processing](11-response-processing.md) in the step to extract the value.
- If no command is marked **Evaluate**, the step's measured value is `0`.
