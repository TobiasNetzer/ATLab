# ATLab Benutzerhandbuch

ATLab ist eine plattformübergreifende Anwendung zum Erstellen und Ausführen automatisierter Prüfabläufe. Sie steuert den **Test Interface Adapter** (Relais-Matrix, Stimulationskanäle, I²C-/UART-Schnittstellen), bedient externe Messgeräte über **serielle Schnittstelle**, **TCP/IP** und **VISA** fern und greift per **Boundary Scan** (JTAG) auf Bausteine der Baugruppe zu.

Dieses Handbuch beschreibt die Installation von ATLab, das Einrichten eines Projekts, das Schreiben von Geräteskripten sowie das Erstellen und Ausführen einer Prüfsequenz in der Fertigung.

*English version: [../en/README.md](../en/README.md)*

## Kapitel

| Nr. | Kapitel | Inhalt |
|-----|---------|--------|
| 1 | [Einführung](01-introduction.md) | Was ATLab leistet, Grundbegriffe |
| 2 | [Installation](02-installation.md) | Installer, Linux-Paket, Build aus dem Quellcode, NI-VISA |
| 3 | [Erste Schritte](03-getting-started.md) | Erster Start, Hardware-Auswahl, Hauptfenster, ein erster Test |
| 4 | [Registerkarte Test-Hardware](04-test-hardware.md) | Hardware-Informationen, Relais-Matrix-Test, Prüfspitze |
| 5 | [Arbeiten mit Projekten](05-projects.md) | Projektdateien, Speichern, Öffnen, relative Pfade |
| 6 | [Registerkarte Konfiguration](06-configuration.md) | Kanalnamen, Geräte-Manager, Laufzeitvariablen, Projekteinstellungen |
| 7 | [Registerkarte Skripte](07-scripting.md) | Skriptordner und Geräteskripte mit mehreren Befehlen |
| 8 | [Prüfsequenzen erstellen](08-building-test-sequences.md) | Schritte bearbeiten, Grenzwerte, Einheiten, Aktionen, Relais |
| 9 | [Auswertungsquellen](09-evaluation-sources.md) | Skript, Gerätebefehl, Schnittstellenbefehl, Externes Programm, Benutzerbestätigung, Benutzereingabe, Ausdruck, Datei, Boundary Scan, Boundary-Scan-Bus |
| 10 | [Befehle, Variablen & Ausdrücke](10-commands-variables-expressions.md) | Platzhalter, Kodierungen, Arithmetik, Einheiten |
| 11 | [Antwortverarbeitung](11-response-processing.md) | Überspringen/Länge, Masken, Zahlenextraktion |
| 12 | [Registerkarte Boundary Scan](12-boundary-scan.md) | JTAG-Probes, Scan-Kette, BSDL-Dateien, Pins lesen und treiben, I²C-/SPI-Busse |
| 13 | [Registerkarte Dokumentation](13-documentation-tab.md) | Projektnotizen, Bilder, Anhänge, Prüflingsdaten |
| 14 | [Tests ausführen](14-running-tests.md) | Ausführung, fehlgeschlagene Schritte, Debugging, Seriennummern, Steuermodul |
| 15 | [Testergebnisse & Berichte](15-results-and-reports.md) | CSV-Export und PDF-Testbericht |
| 16 | [Fehlerbehebung](16-troubleshooting.md) | Häufige Probleme und das Ereignisprotokoll |
| 17 | [Referenz](17-reference.md) | Tastenkürzel, Standardwerte, Dateiablage, Glossar |

## Konventionen

- **Fett** gedruckter Text bezeichnet Beschriftungen der deutschen Benutzeroberfläche.
- `Festbreitenschrift` kennzeichnet Eingaben, Dateinamen oder Werte.
- Meldungen im Ereignisprotokoll sowie einige Auswahlwerte (z. B. Framing-Modi, Protokollstufen) erscheinen auch in der deutschen Oberfläche auf Englisch und werden hier unverändert wiedergegeben.

## PDF-Version

Jedes ATLab-Paket (Installer, Windows-ZIP, Linux-Archiv) enthält dieses Handbuch als PDF auf Deutsch und Englisch im Ordner `docs` neben dem Programm. In ATLab öffnen Sie es über die Buch-Schaltfläche unten rechts im Fenster oder mit `F1` in der aktuellen Oberflächensprache. Selbst erzeugen:

```bash
pip install -r scripts/requirements-docs.txt
python -m playwright install chromium
python scripts/build_user_guide_pdf.py --lang de   # oder --lang en
```

## Siehe auch

- Projekt-Repository: <https://github.com/TobiasNetzer/ATLab>
- Beispielprojekte: <https://github.com/TobiasNetzer/ATLab-Examples>
- Test Interface Adapter: <https://github.com/TobiasNetzer/TestInterfaceAdapter>
