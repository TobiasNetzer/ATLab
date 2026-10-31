# 17. Referenz

## Tastenkürzel

### Global (Hauptfenster)

| Kürzel | Aktion |
|--------|--------|
| `Strg+N` | Neues Projekt |
| `Strg+O` | Projekt öffnen |
| `Strg+S` | Projekt speichern |
| `Strg+Umschalt+S` | Projekt speichern unter |
| `F5` | Testausführung starten |
| `Strg+F5` | Dauertest starten (Entwicklungsmodus) |
| `F3` | Einzelschritt ausführen (Entwicklungsmodus) |
| `F4` | Testausführung des ausgewählten Moduls starten (Entwicklungsmodus) |
| `Umschalt+F4` | Testausführung ab ausgewähltem Schritt starten (Entwicklungsmodus) |
| `F8` | Zum nächsten fehlgeschlagenen Testschritt springen |
| `Umschalt+F8` | Zum vorherigen fehlgeschlagenen Testschritt springen |
| `F9` | Debug-Modus ein- / ausschalten (Entwicklungsmodus) |
| `F10` | Einzelschritt weiterschalten (Debug-Modus) |
| `F12` | Matrix-Kanal über externe Prüfspitze ermitteln |
| `Enter` | Testausführung fortsetzen (sich wiederholenden Schritt beenden) |
| `Esc` | Testausführung stoppen |
| `F1` | Benutzerhandbuch öffnen (PDF in der aktuellen Oberflächensprache) |

### Schrittliste (Registerkarte Testausführung)

| Kürzel | Aktion |
|--------|--------|
| `Einfg` | Schritt hinzufügen (Entwicklungsmodus) |
| `Entf` | Ausgewählte Schritte entfernen (Entwicklungsmodus) |
| `Strg+C` | Ausgewählte Schritte kopieren |
| `Strg+X` | Ausgewählte Schritte ausschneiden (Entwicklungsmodus) |
| `Strg+V` | Schritte einfügen (Entwicklungsmodus) |
| `Strg+D` | Ausgewählte Schritte duplizieren (Entwicklungsmodus) |
| `F2` | Namen des ausgewählten Schritts bearbeiten (Entwicklungsmodus) |
| `Strg`/`Umschalt` + Klick | Mehrfachauswahl |

## Werkzeugleiste der Registerkarte Testausführung

Von links nach rechts:

| Gruppe | Schaltflächen |
|--------|---------------|
| Projekt | Neues Projekt · Projekt öffnen · Projekt speichern · Projekt speichern unter |
| Bearbeiten | Hinzufügen · Entfernen · Nach oben · Nach unten · Duplizieren · Ausschneiden · Kopieren · Einfügen |
| Ausführung | Einzelschritt ausführen · Ausgewähltes Modul starten · Ab ausgewähltem Schritt starten · Dauertest starten · Testausführung starten · Fortsetzen (während eines Laufs) · Stoppen |
| Fehlgeschlagene Schritte | Zum vorherigen fehlgeschlagenen Schritt · Zum nächsten fehlgeschlagenen Schritt |
| Debug (nur Entwicklungsmodus) | Debug-Modus aktivieren · Einzelschritt weiterschalten |
| Ansicht | Messfenster anzeigen · Entwicklungsmodus aktivieren |

Der Bereich **Module** (Entwicklungsmodus) hat eigene Schaltflächen: Hinzufügen · Entfernen · Nach oben · Nach unten · Duplizieren.

## Standardwerte

### Testschritt

| Eigenschaft | Standard |
|-------------|----------|
| Unter-/Soll-/Obergrenze | `0` |
| Einheit | *(leer)* |
| Verzögerung | `0` s |
| Auswertung | Keine |
| Matrix H / L | Off |
| Bestanden-/Fehlgeschlagen-Aktion | Fortfahren |
| Timeout Gerätebefehl | 1000 ms |
| Schnittstelle: Typ / I²C-Frequenz / Adresse | I2C / 100 kHz / 0 |
| Schnittstelle: Baudrate / Datenbits / Parität / Stoppbits | 9600 / 8 / None / 1 |
| Schnittstelle: Timeout | 1000 ms |
| Boundary Scan: Modus / Einschwingzeit / getriebene Pins prüfen | Pin-Aktionen / 0 ms / aus |
| Boundary-Scan-Bus: Adresse / Bytes lesen | 0 / 0 |

### Skriptbefehl

| Eigenschaft | Standard |
|-------------|----------|
| Verzögerung | 0 ms |
| Timeout | 1000 ms |
| Antwort erwarten / Auswertung | aus |

### Gerät

| Schnittstelle | Standardwerte |
|---------------|---------------|
| Serial Port | 115200 Baud, 8 Datenbits, keine Parität, 1 Stoppbit, kein Handshake, kein Abschlusszeichen, Chunk-Framing, Framing-Timeout 100 ms |
| TCP/IP | Port 5025, Timeout 2000 ms, Abschlusszeichen LF |
| VISA | Timeout 2000 ms, Abschlusszeichen LF |

### Boundary Scan

| Einstellung | Standard |
|-------------|----------|
| Probe-Typ | SEGGER J-Link |
| Seriennummer | *(leer)* – erste verfügbare Probe |
| TCK-Frequenz | 1000 kHz |
| FTDI ADBUS-Wert / -Richtung | `08` / `0B` |
| Boundary-Register nach dem Laden einer BSDL-Datei | sichere Werte aus der BSDL-Datei |
| SPI-Bus | Modus 0, CS low-aktiv, MSB zuerst |
| Pin-Gruppe | 8 Pins |

### Projekteinstellungen

| Einstellung | Standard |
|-------------|----------|
| Standardtoleranz | 10 % |
| Angezeigte Nachkommastellen | 3 |
| Seriennummer abfragen | aus |
| Testergebnis speichern | aus (Immer) |
| CSV- / PDF- / JSON-Export | aus |
| Seriennummer-Validierung | aus |
| Steuermodul | aus |
| Protokolliergrad | Info |

## Dateiablage

| Datei | Windows | Linux |
|-------|---------|-------|
| Persönliche Einstellungen | `%LOCALAPPDATA%\ATLab\settings.json` | `~/.local/share/ATLab/settings.json` |
| Ereignisprotokoll | `%LOCALAPPDATA%\ATLab\events.log` | `~/.local/share/ATLab/events.log` |
| Standard-Skriptordner | `%LOCALAPPDATA%\ATLab\Scripts\` | `~/.local/share/ATLab/Scripts/` |
| Programm (Installer) | `C:\Program Files\ATLab\` | dort, wo Sie es entpackt haben |
| Benutzerhandbuch (PDF) | `<Programmordner>\docs\ATLab-User-Guide-de.pdf` / `-en.pdf` | `<Programmordner>/docs/ATLab-User-Guide-de.pdf` / `-en.pdf` |
| Projekte | `*.atlab`, beliebig | `*.atlab`, beliebig |
| Skripte | `<Skriptordner>\<id>.json` | `<Skriptordner>/<id>.json` |

### settings.json

Persönliche Einstellungen pro Benutzer – nicht Teil eines Projekts:

| Schlüssel | Bedeutung |
|-----------|-----------|
| `WindowWidth`, `WindowHeight`, `WindowX`, `WindowY`, `WindowState` | Fenstergröße und -position |
| `LastComPort` | Serielle Schnittstelle des Test Interface Adapters für die automatische Wiederverbindung |
| `LastOpenedFile` | Beim Start erneut geöffnetes Projekt |
| `IsDarkMode` | Design |
| `Language` | `en` oder `de` |
| `IsForceSimulation` | `true` = immer im Simulatormodus starten, ohne Verbindungsversuch zur Hardware |
| `IsDevelopmentMode` | Entwicklungsmodus ein/aus |
| `IsDevelopmentModePasswordEnabled` | Entwicklungsmodus verlangt ein Passwort |
| `DevelopmentModePasswordHash`, `DevelopmentModePasswordSalt` | Gesalzener PBKDF2-Hash des Passworts für den Entwicklungsmodus |
| `IsShowMeasurementPanel` | Messfenster sichtbar |
| `Is…Expanded` | Welche Editorbereiche aufgeklappt sind |

Sprache, Design, `IsForceSimulation` und das Passwort für den Entwicklungsmodus werden im [Einstellungsdialog](03-getting-started.md#einstellungen) festgelegt. Ist das Passwort vergessen, ATLab schließen, `IsDevelopmentModePasswordEnabled` in `settings.json` auf `false` setzen und neu starten.

## Unterstützte Schnittstellen im Überblick

| Schnittstelle | Verwendet für | Voraussetzungen |
|---------------|---------------|-----------------|
| Serielle Schnittstelle (VCOM/RS232) | Messgeräte, Prüfling, Test Interface Adapter | – |
| TCP/IP-Raw-Socket | LAN-Messgeräte (SCPI-RAW) | Netzwerkzugang |
| VISA (USB, GPIB, ASRL, TCPIP) | Messgeräte | NI-VISA, nur Windows |
| Adapter-I²C | Prüfling | Adapter mit I²C |
| Adapter-UART | Prüfling | Adapter mit UART |
| USB-HID | Steuermodul | – |
| JTAG (SEGGER J-Link) | Boundary Scan | J-Link Software and Documentation Pack |
| JTAG (FTDI FT2232H/FT232H/FT4232H) | Boundary Scan | FTDI-D2XX-Treiber |

## Glossar

| Begriff | Definition |
|---------|------------|
| **Abweichung** | `(Messwert − Sollwert) / Sollwert × 100 %`. |
| **BSDL** | Boundary Scan Description Language. Datei des Bausteinherstellers, die Boundary-Register, Befehle und Pinbelegung des Gehäuses eines Bausteins beschreibt. |
| **BYPASS / SAMPLE / EXTEST** | Boundary-Scan-Befehle: Baustein überbrückt / Pins lesen, ohne den Baustein zu beeinflussen / Pins aus dem Boundary-Register lesen und treiben. |
| **Boundary-Register** | Kette von Zellen, eine oder mehrere pro Pin, über die Boundary Scan die Pins liest und treibt. |
| **IDCODE** | 32-Bit-Kennung eines Bausteins (Hersteller, Teilenummer, Version), wird bei der Kettenerkennung gelesen. |
| **JTAG** | Testzugang nach IEEE 1149.1 mit den Signalen TCK, TMS, TDI, TDO (und optional TRST). |
| **Adapter / TIA / cTIA** | Test Interface Adapter (compact Test Interface Adapter), die von ATLab gesteuerte USB-Relais- und Schnittstellen-Hardware. |
| **Auswertungsquelle** | Mechanismus, der den Messwert eines Schritts liefert. |
| **DUT / Prüfling** | Device Under Test, das zu prüfende Gerät. |
| **Ext. Stimulationskanal** | Relais, das einen externen Stimulus (z. B. ein Netzteil) auf den Prüfling schaltet. |
| **Framing** | Regel zum Erkennen des Endes einer seriellen Antwort. |
| **Laufzeitvariable** | Projektweite Variable, überall lesbar und von Schritten beschreibbar. |
| **Maske** | Muster, mit dem die Antwortverarbeitung Daten prüft oder extrahiert. |
| **Matrix H / L** | High- und Low-Seite des Messbusses, jeweils mit einem Matrix-Kanal verbindbar. |
| **Overflow** | Ergebnis ≥ 9,9 × 10³⁷, der SCPI-„Überlauf“-Wert. |
| **SCPI** | Standard Commands for Programmable Instruments, z. B. `MEAS:VOLT:DC?`. |
| **Simulator** | Virtueller Adapter, der verwendet wird, wenn keine Hardware angeschlossen ist. |
| **Skriptordner** | Ordner mit den JSON-Dateien der Skripte. |
| **Stimulationskanal** | Relais am Adapter, mit dem ein Signal oder eine Last an den Prüfling angelegt wird. |
