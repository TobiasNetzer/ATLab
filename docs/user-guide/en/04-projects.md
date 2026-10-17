# 4. Working with Projects

A **project** is a single file with the extension `.atlab`. It stores everything that belongs to one test:

- the test steps and their full configuration,
- the devices from the Device Manager,
- runtime variables and their initial values,
- custom names for matrix, stimulation and external stimulation channels,
- the project settings (tolerance, serial number, export options …),
- the documentation (texts, image and attachment paths),
- the Device Under Test information used in reports,
- the path of the script repository folder.

Scripts themselves are **not** stored in the project; they live as separate files in the script repository (see [Scripting](06-scripting.md)).

## File commands

The file buttons are the first four icons in the Test Execution toolbar.

| Button | Shortcut | Action |
|--------|----------|--------|
| **New file** | `Ctrl+N` | Starts an empty, untitled project. |
| **Open file** | `Ctrl+O` | Opens an existing `.atlab` file. |
| **Save file** | `Ctrl+S` | Saves to the current file. For an untitled project this behaves like *Save as*. |
| **Save file as** | `Ctrl+Shift+S` | Saves under a new name. The default suggestion is `Test.atlab`. |

File commands are disabled while a test is running.

## Unsaved changes

Any edit marks the project as modified and adds `*` to the window title. If you create a new project, open another one or close ATLab while there are unsaved changes, a dialog asks **"You have unsaved changes. Continue without saving?"**. Choose **Continue** to discard the changes or **Cancel** to go back.

Running a test does not mark the project as modified.

## Which project opens at start-up

1. If ATLab was started with a file path as its first argument (for example by double-clicking an `.atlab` file on Windows), that file is opened.
2. Otherwise the last opened or saved project is reopened.
3. If neither exists, ATLab starts with an empty project.

```bash
ATLab.exe "D:\Tests\PowerBoard.atlab"
```

## Relative paths

When a project is saved, any referenced file **inside the project folder (or a sub-folder)** is stored with a relative path. This applies to:

- the script repository folder,
- documentation images and attachments,
- message-box images of test steps,
- file paths of **File** evaluation steps,
- the test result **Output Path**.

Paths outside the project folder are stored as absolute paths.

**Recommendation:** keep a project together with its scripts, images and attachments in one folder, for example:

```
PowerBoard/
├── PowerBoard.atlab
├── Scripts/            ← script repository
├── Images/
└── Attachments/
```

The whole folder can then be copied to another test station or committed to version control and still work.

## Hardware compatibility check

When you open a project, ATLab checks whether the connected adapter has enough channels for it. If the project uses matrix, stimulation or external stimulation channels that the adapter does not have, a warning lists them, for example:

```
Connected test hardware is missing required channels:

- Matrix channels (available: 16/24)

All unavailable channels will be permanently lost upon saving!
```

Steps that refer to a missing matrix channel fail with *"Matrix Channel is not available."* when executed. Do not save such a project unless you really want to drop those channel assignments — open it on the correct hardware (or in the simulator, which provides 32/16/4 channels) instead.

## File format

Project files are human-readable, indented JSON. This makes them well suited to version control: changes to individual steps show up as small, readable diffs. Editing the file by hand is possible but not recommended; always keep a backup.

Each test step has a permanent internal ID. *Jump To* targets refer to this ID, so jumps keep working when steps are moved or renumbered.
