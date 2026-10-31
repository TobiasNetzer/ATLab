# 15. Test Results & Reports

ATLab can export the result of every run automatically as a data file (CSV), a formatted test report (PDF) and/or a data file with metadata for evaluation tools (JSON).

## Enabling automatic export

On the **Configuration** tab → **Project Settings** → **Test Execution Settings**:

1. Enable **Request Serial Number** (required — the serial number is part of the file name).
2. Enable **Save Test Result**.
3. Choose **Always** or **Passed Only**.
4. Enable any of **Export Test Data (CSV)**, **Export Test Report (PDF)** and **Export Test Data with Metadata (JSON)**.
5. Optionally enable **Sort into Pass / Fail Subfolders**.
6. Set the **Output Path**. The folder is created if necessary.

## When files are written

Files are written at the end of a run started with **Start test execution** or **Start continuous test execution** (every loop) when the run ends as **Test Passed** or **Test Failed**. With *Passed Only*, failed runs are not exported.

No files are written for canceled runs, runs aborted by an execution error, runs started from the selected step, or single-step executions.

Each export produces a message in the event log, e.g. `PDF exported to 'D:\Results\PB-2026001_2026-10-17_14-03-22.pdf'.` If no output path is set, the error *"No output path for test results provided."* is logged.

## File names

```
<SerialNumber>_<yyyy-MM-dd>_<HH-mm-ss>.csv
<SerialNumber>_<yyyy-MM-dd>_<HH-mm-ss>.pdf
<SerialNumber>_<yyyy-MM-dd>_<HH-mm-ss>.json
```

Date and time are the start of the run. Example: `Prototype-001_2026-10-18_18-50-00.pdf`. Avoid characters that are not allowed in file names (`\ / : * ? " < > |`) in serial numbers.

## Folders

By default all files are written directly to the output path. With **Sort into Pass / Fail Subfolders**, they are written to a subfolder depending on the result of the run:

```
<Output Path>\Pass\<SerialNumber>_<yyyy-MM-dd>_<HH-mm-ss>.*
<Output Path>\Fail\<SerialNumber>_<yyyy-MM-dd>_<HH-mm-ss>.*
```

The subfolders are created when needed. Their names are always `Pass` and `Fail`, regardless of the UI language. A run counts as failed if any step failed, including steps marked **Exclude from Export**. With *Passed Only*, only the `Pass` folder is used.

## Which steps are included

All files contain only steps that

- were executed in this run,
- are not marked **Ignore Step**, and
- are not marked **Exclude from Export**.

## CSV data file

Despite the `.csv` extension, the file is **tab-separated** (UTF-8). It opens directly in Excel or LibreOffice Calc; choose *Tab* as separator if asked.

| Column | Content |
|--------|---------|
| Module | Module name |
| Step | Step number within the module |
| Name | Step name |
| Lower Limit | Lower limit (plain number, `.` as decimal separator) |
| Measured Value | Measured value, or `Timeout` / `NACK` / `Overflow` |
| Upper Limit | Upper limit |
| Unit | Unit text (braces removed) |
| Deviation (%) | Deviation from nominal |
| Result | `Pass` or `Fail` |
| Details | Additional messages of the step, e.g. failed response checks or boundary scan pins, separated by `; `, empty if there are none. The same lines as `steps[].details` in the JSON file. |

Numbers are written without SI prefixes, so a value shown as `1,3mA` appears as `0.0013`.

## JSON result file

The JSON file contains the same steps as the CSV file plus the metadata of the run, so every file is complete on its own. It is meant for evaluation tools and scripts (statistics, yield, trends) rather than for reading in a spreadsheet. Property names, enum values and numbers are independent of the UI language.

```json
{
  "formatId": "0xA71AB-RESULT",
  "formatVersion": 1,
  "run": {
    "serialNumber": "Prototype-001",
    "startTime": "2026-10-18T18:50:00+02:00",
    "durationSeconds": 83.4,
    "overallResult": "Pass",
    "failedSteps": 0,
    "operator": "jdoe",
    "station": "LAB-PC-01",
    "runMode": "Single",
    "loopIndex": null
  },
  "project": {
    "name": "Powerboard EOL",
    "fileHash": "sha256:9f2c…",
    "hasUnsavedChanges": false,
    "atlabVersion": "1.0.0"
  },
  "deviceUnderTest": { "deviceName": "Powerboard", "revision": "B", "variant": "24V", "partNumber": "PB-100" },
  "testHardware": { "name": "cTIA", "serialNumber": "…", "firmwareVersion": "…", "isSimulator": false },
  "testEquipment": [ { "name": "DMM", "identification": "KEITHLEY,DMM6500,…" } ],
  "notes": null,
  "steps": [
    {
      "moduleId": "3f1e…", "module": "Power Supply",
      "stepId": "a92b…", "number": 1, "name": "VCC 3V3",
      "lowerLimit": 3.2, "upperLimit": 3.4, "unit": "V",
      "status": "Measured", "value": 3.301, "deviationPercent": 0.03,
      "result": "Pass", "details": null
    }
  ]
}
```

| Field | Content |
|-------|---------|
| `formatId`, `formatVersion` | Identify the file as ATLab test result and its format version. |
| `run.startTime` | Start of the run (ISO 8601 with UTC offset). The file names use the same time. |
| `run.overallResult`, `run.failedSteps` | Verdict of the whole run, including failed steps that are excluded from export. |
| `run.operator`, `run.station` | OS user name and computer name. |
| `run.runMode`, `run.loopIndex` | `Single`, or `Continuous` with the loop number (starting at 1) for continuous execution. |
| `project.fileHash` | SHA-256 of the project file on disk. Runs with the same hash used the same sequence and limits. `null` if the project was never saved. |
| `project.hasUnsavedChanges` | `true` if the project had unsaved changes, so the run may not match `fileHash`. |
| `testHardware.isSimulator` | `true` for runs in simulator mode. Exclude these from production statistics. |
| `testEquipment` | Devices with **Include in Test Report** and their identification string (`null` if the device did not answer). |
| `steps[].moduleId`, `steps[].stepId` | Stable IDs from the project file. They do not change when modules or steps are renamed, renumbered or moved, so the same step can be compared across runs. |
| `steps[].unit` | Unit as entered in the step. A unit in braces (e.g. `{°C}`) is shown without SI prefix, see [Name, limits and unit](08-building-test-sequences.md#name-limits-and-unit). Unlike the CSV file and the PDF report, the JSON file keeps the braces so evaluation tools can format values the same way. |
| `steps[].status` | `Measured`, `NoValue` (step produces no value), `Timeout`, `NACK`, `Overflow` or `Unknown`. |
| `steps[].value` | Measured value without SI prefix (`1,3mA` → `0.0013`); `null` unless `status` is `Measured`. |
| `steps[].details` | Additional messages of the step, e.g. failed response checks; `null` if there are none. |

## PDF test report

The report is generated in the current UI language (English or German). Two example runs are included in the repository, each with report and data file: Prototype-001 ([PDF](../../Prototype-001_2026-10-18_18-50-00.pdf), [CSV](../../Prototype-001_2026-10-18_18-50-00.csv)) and Prototype-002 ([PDF](../../Prototype-002_2026-10-18_18-50-52.pdf), [CSV](../../Prototype-002_2026-10-18_18-50-52.csv)).

Contents:

1. **Header** – ATLab logo, title *Test Report – \<DUT name\> …*.
2. **Test Information** – project name, operator (OS user name), date and time of the start of the run, and test duration.
3. **Device Under Test** – serial number, device name, revision, variant, part number (from the [Documentation tab](13-documentation-tab.md#device-under-test)).
4. **Additional Notes** – only if filled in.
5. **Used Test Equipment**
   - *Test Hardware*: name, serial number and firmware version of the Test Interface Adapter (`Simulator` / `N/A` in simulator mode).
   - *Test Equipment*: every device with **Include in Test Report** enabled, with the answer to its **Identification Query** (e.g. the `*IDN?` string), or `-` if it did not answer.
6. **Overall verdict** – a large **PASS** (green) or **FAIL** (red). If failed steps are excluded from export, a note below says how many are not shown.
7. **Results table** – step, name, lower limit, measured value, upper limit and a pass/fail icon, grouped by module with a heading row per module.
8. **Footer** – ATLab version and page number.

The overall verdict in the report is the result of the **whole run**, the same as in the Testing tab, the `Pass` / `Fail` subfolders and the JSON file. A failed step marked **Exclude from Export** is not listed in the table, but the report still shows **FAIL** with the note *"1 failed step is excluded from this report."*

> Generating the report or the JSON file queries every device marked *Include in Test Report* (once per run, even if both are enabled). A device that is switched off delays the export by its timeout and is listed with `-` in the report and `null` in the JSON file.
