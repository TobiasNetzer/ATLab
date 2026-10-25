# 4. Test Hardware Tab

The **Test Hardware** tab shows what is connected and lets you check the relay matrix.

![Test Hardware tab](images/Hardware_Tab_en.png)

## Test Hardware Interface

A picture of the Test Interface Adapter with the pin assignment of its connectors:

- **Interface Connector** – analog measurement bus, sense lines and external stimulus inputs (DMM H/L/SH/SL, S1–S4),
- **Programming Connector** – 1:1 pass-through for a programmer,
- **External Probe** – routable to the measurement bus,
- **USB-C** – connection to the PC (USB 2.0).

The pin table (`a1…a32`, `b1…b32`, `c1…c32`) lists the signals on the DUT-side connector: external stimulation outputs, external programming lines, RS485, I²C and UART lines, stimulation inputs/outputs and the matrix measurement channels (`MEAS 1…32`). Use it when wiring a fixture.

## Test Hardware Info

| Section | Information |
|---------|-------------|
| **Connected Test Hardware** | Device name, serial number, firmware version, build date and build time of the adapter. |
| **Available Relay Channels** | Number of matrix, stimulation and external stimulation channels. |
| **Available Interfaces** | Whether isolated I²C, UART and RS485 are available (`True`/`False`). |

In simulator mode, the device is shown as `Simulator` with `N/A` for serial number and firmware, and 32 / 16 / 4 channels with all interfaces available.

The channel counts determine how many rows appear in the [Channel Labels](06-configuration.md#channel-labels) panel and the relay selectors, and are checked when a project is opened (see [Hardware compatibility check](05-projects.md#hardware-compatibility-check)).

## Test Hardware Diagnostics

The **relay matrix test** checks every matrix relay of the adapter.

> **Warning:** Ensure that everything is disconnected from the relay matrix before running the test. Leaving devices connected may cause permanent damage to the test hardware or the connected equipment.

1. Disconnect the fixture and any instruments from the adapter.
2. Tick **I have disconnected everything**.
3. Click **Run Relay Matrix Test**.

Result:

- **Relay Matrix Test Passed** – all relays work.
- **Defective Relays Detected** – the defective relays are listed separately for **Matrix H** and **Matrix L**.
- **Relay Matrix Test Failed: …** – the test could not be carried out (communication problem).

The acknowledgement checkbox is cleared after each run, so it must be ticked again for the next run. In simulator mode the test always passes.

## Finding a matrix channel with the probe

To find out which matrix channel a test point is connected to (for example while building a fixture or writing a sequence):

1. Touch the test point with the **external probe**.
2. Click the probe button in the status bar (**Determine matrix channel via external probe**) or press `F12`. This is not possible while a test is running.
3. The status bar shows `Matrix Channel: <n>`, or `-` if the point is not connected to any matrix channel. If no probe is detected, *External probe not detected* is shown and an error is logged.

This works from any tab.
