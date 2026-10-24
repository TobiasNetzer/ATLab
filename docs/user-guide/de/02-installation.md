# 2. Installation

ATLab wird über die Releases-Seite des Projekts auf GitHub verteilt: <https://github.com/TobiasNetzer/ATLab/releases>.

Jedes Release enthält:

| Datei | Plattform | Hinweise |
|-------|-----------|----------|
| `ATLab-setup-<version>.exe` | Windows (64 Bit) | Installer mit Startmenü-Eintrag und `.atlab`-Dateizuordnung |
| `ATLab-win-<version>.zip` | Windows | Portable Version, ohne Installation |
| `ATLab-linux-<version>.tar.gz` | Linux | Portable Version |

Alle Pakete enthalten dieses Benutzerhandbuch als PDF (Deutsch und Englisch) im Unterordner `docs` des Programmordners, z. B. `C:\Program Files\ATLab\docs\ATLab-User-Guide-de.pdf`. In ATLab öffnen Sie es mit der Buch-Schaltfläche in der Statusleiste oder mit `F1`.

## Windows

### Mit dem Installer

1. `ATLab-setup-<version>.exe` herunterladen und ausführen. Administratorrechte sind erforderlich.
2. Lizenz (GNU GPL v3) akzeptieren und Installationsordner wählen. Standard ist `C:\Program Files\ATLab`.
3. Auf der letzten Seite können Sie ATLab sofort starten und eine Desktop-Verknüpfung anlegen.

Der Installer

- legt **ATLab** im Startmenü an,
- registriert die Endung `.atlab`, sodass ein Doppelklick auf eine Projektdatei sie in ATLab öffnet,
- trägt ein Deinstallationsprogramm unter *Einstellungen → Apps* ein.

Bei der Deinstallation werden Programmordner, Verknüpfungen und Dateizuordnung entfernt. Projekte, Skripte und persönliche Einstellungen bleiben erhalten (siehe [Dateiablage](16-reference.md#dateiablage)).

### Portables ZIP

`ATLab-win-<version>.zip` in einen beliebigen Ordner entpacken und `ATLab.exe` starten. Es wird keine Dateizuordnung registriert.

## Linux

```bash
mkdir -p ~/ATLab
tar -xzf ATLab-linux-<version>.tar.gz -C ~/ATLab
cd ~/ATLab
chmod +x ATLab
./ATLab
```

Für den Zugriff auf die serielle Schnittstelle des Test Interface Adapters muss Ihr Benutzer in der Regel Mitglied der Gruppe `dialout` (oder des Äquivalents Ihrer Distribution) sein:

```bash
sudo usermod -aG dialout $USER   # danach ab- und wieder anmelden
```

## Optional: NI-VISA

VISA-Geräte benötigen die **NI-VISA**-Laufzeitumgebung von National Instruments. VISA wird derzeit **nur unter Windows** unterstützt. Serielle und TCP/IP-Geräte funktionieren ohne.

## Optional: Steuermodul

Das USB-Steuermodul (ein HID-Gerät mit Start-/Stopp-Tasten) benötigt keinen Treiber. Es wird pro Projekt aktiviert, siehe [Tests ausführen](11-running-tests.md#steuermodul).

## Build aus dem Quellcode

Voraussetzung: **.NET 10 SDK** oder neuer.

```bash
git clone https://github.com/TobiasNetzer/ATLab.git
cd ATLab
dotnet restore
dotnet build -c Release
dotnet run --project ATLab.csproj
```

Alternativ `ATLab.sln` in JetBrains Rider, Visual Studio 2022 oder VS Code mit C# Dev Kit öffnen.

## Systemvoraussetzungen

- Windows 10/11 (64 Bit) oder eine aktuelle Linux-Desktop-Distribution.
- Ein freier USB-Anschluss für den Test Interface Adapter (er erscheint als serielle/COM-Schnittstelle).
