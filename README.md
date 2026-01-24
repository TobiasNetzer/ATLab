# ATLab - Automated Testing Toolkit

![image](https://github.com/TobiasNetzer/ATLab/releases/download/assets-v1/Example.gif)

ATLab is a cross‑platform automated testing toolkit for interfacing with hardware via the [Test Interface Adapter](https://github.com/TobiasNetzer/TestInterfaceAdapter) and controlling external test instruments through remote control interfaces.
Built with **Avalonia UI** and **.NET 9**, it offers a modern environment for developing and running automated test sequences.

## Features

-   **Test Instrument Interfacing**: Support for both **Serial Port (RS232/UART)** and **VISA (Virtual Instrument Software Architecture)** interfaces.
-   **Device Management**: Configure and manage multiple devices (Test Instruments or DUTs) with custom settings.
-   **Scripting Engine**: Create test sequences with multi-step scripts.
-   **Customizable Test Steps**: Each test step can perform multiple actions:
    -   **Instrument Commands**: Send commands to devices with configurable timeouts and delays.
    -   **Response Validation**: Use response masking to verify hardware output.
    -   **Shell Commands**: Execute local system commands or scripts as part of the test flow.
    -   **Relay Control**: Manage the Relay Matrix and Relay Groups, using the Test Interface Adapter to route signals through different relays.

## Build and Prerequisites

### Prerequisites

-   **.NET 9.0 SDK** (or newer)
-   *Optional:* NI-VISA drivers (if using VISA-based instruments) **Windows only**

### How to Build

1.  **Clone the repository**:
    ```bash
    git clone https://github.com/TobiasNetzer/ATLab.git
    cd ATLab
    ```

2.  **Restore dependencies**:
    ```bash
    dotnet restore
    ```

3.  **Build the project**:
    ```bash
    dotnet build -c Release
    ```

4.  **Run the application**:
    ```bash
    dotnet run --project ATLab.csproj
    ```

Alternatively, you can open the `ATLab.sln` file in **JetBrains Rider**, **Visual Studio 2022**, or **VS Code** (with C# Dev Kit) and use the built-in build/run commands.

## Usage

### 1. Configure Devices and Settings
-   **Hardware Setup**: Go to the **Config** tab to add and configure your hardware devices. You can set up **Serial Port (RS232/UART)** or **VISA** devices, defining their connection parameters.
-   **Project Settings**: Configure project-wide settings such as default tolerances, serial number requirements, and test result export options.

### 2. Manage Scripts
The **Scripts** tab allows you to create and manage reusable script snippets that can be used within your test steps.
-   **Create New Scripts**: Click on **New Script** to create a new script sequence.
-   **Define Commands**: Add instrument commands with specific delays and timeouts. The system is primarily designed for SCPI commands.
-   **Use Variables**: Define variables within your script (e.g., `{Range}`) to make them customizable when used in different test steps.
-   **Central Repository**: Scripts are saved in a configured repository folder, making them available across different projects.

### 3. Build Test Sequence
In the **Testing** tab, you can build your automated test sequence. Note that editing features require **Development Mode** to be enabled (toggle button in the toolbar).
-   **Add/Manage Steps**: Use the toolbar to add, remove, copy, paste, duplicate, or reorder test steps.
-   **Configure Step Parameters**: Customize step settings such as pass/fail limits and evaluation sources.
-   **Assign Scripts or Commands**: For each test step, you can either select a pre-configured script from your repository or enter a direct instrument command.
-   **Custom Variables**: If a script with variables is selected, you can provide specific values for that step.
-   **Response Validation**: Configure expected responses and use response masking to verify hardware output.
-   **Relay Control**: Set the state of the Relay Matrix or Relay Groups for signal routing during that step.
-   **Shell Commands**: Optionally execute local system commands or scripts.

### 4. Run Tests
Once your sequence is ready, use the execution controls:
-   **Start Test**: Run the entire sequence.
-   **Start from Selection**: Begin execution from the currently selected step.
-   **Single Step**: Execute only the selected step for debugging.
-   **Repeat Test**: Run the sequence in a continuous loop.
-   **Stop**: Cancel the running test at any time.