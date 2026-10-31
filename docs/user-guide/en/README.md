# ATLab User Guide

ATLab is a cross-platform application for building and running automated test sequences. It drives the **Test Interface Adapter** (relay matrix, stimulation channels, I²C/UART interfaces), remote-controls external test instruments over **serial port**, **TCP/IP** and **VISA**, and accesses chips on the board through **boundary scan** (JTAG).

This guide explains how to install ATLab, set up a project, write instrument scripts, build a test sequence and run it in production.

*Deutsche Version: [../de/README.md](../de/README.md)*

## Chapters

| # | Chapter | What it covers |
|---|---------|----------------|
| 1 | [Introduction](01-introduction.md) | What ATLab does, key concepts and terminology |
| 2 | [Installation](02-installation.md) | Installers, Linux package, building from source, NI-VISA |
| 3 | [Getting Started](03-getting-started.md) | First launch, hardware selection, the main window, a first test |
| 4 | [Test Hardware Tab](04-test-hardware.md) | Hardware info, relay matrix self-test, probing |
| 5 | [Working with Projects](05-projects.md) | Project files, saving, opening, relative paths |
| 6 | [Configuration Tab](06-configuration.md) | Channel names, Device Manager, runtime variables, project settings |
| 7 | [Scripting Tab](07-scripting.md) | The script folder and multi-command instrument scripts |
| 8 | [Building Test Sequences](08-building-test-sequences.md) | Editing steps, limits, units, pass/fail actions, relay routing |
| 9 | [Evaluation Sources](09-evaluation-sources.md) | Script, Device Command, Interface Command, External Program, User Confirmation, User Input, Expression, File, Boundary Scan, Boundary Scan Bus |
| 10 | [Commands, Variables & Expressions](10-commands-variables-expressions.md) | Placeholder syntax, encodings, arithmetic, units |
| 11 | [Response Processing](11-response-processing.md) | Skip/Length, masks and numeric extraction |
| 12 | [Boundary Scan Tab](12-boundary-scan.md) | JTAG probes, scan chain, BSDL files, reading and driving pins, I²C/SPI buses |
| 13 | [Documentation Tab](13-documentation-tab.md) | Project notes, images, attachments, DUT information |
| 14 | [Running Tests](14-running-tests.md) | Execution controls, failed step navigation, debugging, serial numbers, the control module |
| 15 | [Test Results & Reports](15-results-and-reports.md) | CSV data files, JSON result files and PDF test reports |
| 16 | [ATLab Analyzer](16-analyzer.md) | Evaluating result files: yield, failing steps, histograms, trends, capability, exports |
| 17 | [Troubleshooting](17-troubleshooting.md) | Common problems and the event log |
| 18 | [Reference](18-reference.md) | Keyboard shortcuts, defaults, file locations, glossary |

## Conventions

- **Bold** text refers to labels as they appear in the user interface (English UI).
- `Monospace` text is something you type, a file name, or a value.

## PDF version

Every ATLab package (installer, Windows ZIP, Linux archive) includes this guide as a PDF in English and German, in the `docs` folder next to the program. In ATLab, click the book button at the bottom right of the window or press `F1` to open it in the current UI language. To build the PDF yourself:

```bash
pip install -r scripts/requirements-docs.txt
python -m playwright install chromium
python scripts/build_user_guide_pdf.py --lang en   # or --lang de
```

## See also

- Project repository: <https://github.com/TobiasNetzer/ATLab>
- Example projects: <https://github.com/TobiasNetzer/ATLab-Examples>
- Test Interface Adapter hardware: <https://github.com/TobiasNetzer/TestInterfaceAdapter>
