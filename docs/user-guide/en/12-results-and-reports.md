# 12. Test Results & Reports

ATLab can export the result of every run automatically as a data file (CSV) and/or a formatted test report (PDF).

## Enabling automatic export

On the **Configuration** tab → **Project Settings** → **Test Execution Settings**:

1. Enable **Request Serial Number** (required — the serial number is part of the file name).
2. Enable **Save Test Result**.
3. Choose **Always** or **Passed Only**.
4. Enable **Export Test Data (CSV)** and/or **Export Test Report (PDF)**.
5. Set the **Output Path**. The folder is created if necessary.

## When files are written

Files are written at the end of a run started with **Start test execution** or **Start continuous test execution** (every loop) when the run ends as **Test Passed** or **Test Failed**. With *Passed Only*, failed runs are not exported.

No files are written for canceled runs, runs aborted by an execution error, runs started from the selected step, or single-step executions.

Each export produces a message in the event log, e.g. `PDF exported to 'D:\Results\PB-2026001_2026-10-17_14-03-22.pdf'.` If no output path is set, the error *"No output path for test results provided."* is logged.

## File names

```
<SerialNumber>_<yyyy-MM-dd>_<HH-mm-ss>.csv
<SerialNumber>_<yyyy-MM-dd>_<HH-mm-ss>.pdf
```

Example: `Prototype-001_2026-10-18_18-50-00.pdf`. Avoid characters that are not allowed in file names (`\ / : * ? " < > |`) in serial numbers.

## Which steps are included

Both files contain only steps that

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
| Measured Value | Measured value, or `Timeout` / `Overflow` |
| Upper Limit | Upper limit |
| Unit | Unit text (braces removed) |
| Deviation (%) | Deviation from nominal |
| Result | `Pass` or `Fail` |

Numbers are written without SI prefixes, so a value shown as `1,3mA` appears as `0.0013`.

## PDF test report

The report is generated in the current UI language (English or German). Two example runs are included in the repository, each with report and data file: Prototype-001 ([PDF](../../Prototype-001_2026-10-18_18-50-00.pdf), [CSV](../../Prototype-001_2026-10-18_18-50-00.csv)) and Prototype-002 ([PDF](../../Prototype-002_2026-10-18_18-50-52.pdf), [CSV](../../Prototype-002_2026-10-18_18-50-52.csv)).

Contents:

1. **Header** – ATLab logo, title *Test Report – \<DUT name\> …*.
2. **Test Information** – project name, operator (OS user name), date, time and test duration.
3. **Device Under Test** – serial number, device name, revision, variant, part number (from the [Documentation tab](13-documentation-tab.md#device-under-test)).
4. **Additional Notes** – only if filled in.
5. **Used Test Equipment**
   - *Test Hardware*: name, serial number and firmware version of the Test Interface Adapter (`Simulator` / `N/A` in simulator mode).
   - *Test Equipment*: every device with **Include in Test Report** enabled, with the answer to its **Identification Query** (e.g. the `*IDN?` string), or `-` if it did not answer.
6. **Overall verdict** – a large **PASS** (green) or **FAIL** (red).
7. **Results table** – step, name, lower limit, measured value, upper limit and a pass/fail icon, grouped by module with a heading row per module.
8. **Footer** – ATLab version and page number.

The overall verdict in the report is calculated from the steps **contained in the report**. Failed steps that are excluded from export therefore do not appear in it.

> Generating the report queries every device marked *Include in Test Report*. A device that is switched off delays the export by its timeout and is listed with `-`.
