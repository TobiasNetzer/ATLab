# 15. Testergebnisse & Berichte

ATLab kann das Ergebnis jedes Laufs automatisch als Datendatei (CSV), als formatierten Testbericht (PDF) und/oder als Ergebnisdatei für Auswerteprogramme (JSON) exportieren.

## Automatischen Export aktivieren

Registerkarte **Konfiguration** → **Projekteinstellungen** → **Testausführungseinstellungen**:

1. **Seriennummer abfragen** aktivieren (erforderlich – die Seriennummer ist Teil des Dateinamens).
2. **Testergebnis speichern** aktivieren.
3. **Immer** oder **Nur wenn bestanden** wählen.
4. Beliebige von **Testdaten exportieren (CSV)**, **Ergebnisdatei für Auswertung exportieren (JSON)** und **Testbericht exportieren (PDF)** aktivieren.
5. Optional **In Unterordner Pass / Fail sortieren** aktivieren.
6. Den **Ausgabepfad** festlegen. Der Ordner wird bei Bedarf angelegt.

## Wann Dateien geschrieben werden

Dateien werden am Ende eines mit **Testausführung starten** oder **Dauertest starten** (bei jedem Durchlauf) begonnenen Laufs geschrieben, wenn der Lauf mit **Test bestanden** oder **Test fehlgeschlagen** endet. Bei *Nur wenn bestanden* werden fehlgeschlagene Läufe nicht exportiert.

Keine Dateien entstehen bei abgebrochenen Läufen, bei durch einen Ausführungsfehler beendeten Läufen, bei Läufen ab dem ausgewählten Schritt und bei Einzelschritt-Ausführungen.

Jeder Export erzeugt eine Meldung im Ereignisprotokoll, z. B. `PDF exported to 'D:\Results\PB-2026001_2026-10-17_14-03-22.pdf'.` Ist kein Ausgabepfad gesetzt, wird der Fehler *„No output path for test results provided.“* protokolliert.

## Dateinamen

```
<Seriennummer>_<JJJJ-MM-TT>_<HH-mm-ss>.csv
<Seriennummer>_<JJJJ-MM-TT>_<HH-mm-ss>.pdf
<Seriennummer>_<JJJJ-MM-TT>_<HH-mm-ss>.json
```

Datum und Uhrzeit sind der Beginn des Laufs. Beispiel: `Prototype-001_2026-10-18_18-50-00.pdf`. Vermeiden Sie in Seriennummern Zeichen, die in Dateinamen nicht erlaubt sind (`\ / : * ? " < > |`).

## Ordner

Standardmäßig werden alle Dateien direkt in den Ausgabepfad geschrieben. Mit **In Unterordner Pass / Fail sortieren** landen sie je nach Ergebnis des Laufs in einem Unterordner:

```
<Ausgabepfad>\Pass\<Seriennummer>_<JJJJ-MM-TT>_<HH-mm-ss>.*
<Ausgabepfad>\Fail\<Seriennummer>_<JJJJ-MM-TT>_<HH-mm-ss>.*
```

Die Unterordner werden bei Bedarf angelegt. Sie heißen unabhängig von der Oberflächensprache immer `Pass` und `Fail`. Ein Lauf gilt als fehlgeschlagen, wenn irgendein Schritt fehlgeschlagen ist, auch einer mit **Vom Export ausschließen**. Bei *Nur wenn bestanden* wird nur der Ordner `Pass` verwendet.

## Welche Schritte enthalten sind

Alle Dateien enthalten nur Schritte, die

- in diesem Lauf ausgeführt wurden,
- nicht als **Schritt ignorieren** markiert sind und
- nicht als **Vom Export ausschließen** markiert sind.

## CSV-Datendatei

Trotz der Endung `.csv` ist die Datei **tabulatorgetrennt** (UTF-8). Sie lässt sich direkt in Excel oder LibreOffice Calc öffnen; wählen Sie bei Nachfrage *Tabulator* als Trennzeichen.

| Spalte | Inhalt |
|--------|--------|
| Module | Name des Moduls |
| Step | Schrittnummer innerhalb des Moduls |
| Name | Name des Schritts |
| Lower Limit | Untergrenze (reine Zahl, `.` als Dezimaltrennzeichen) |
| Measured Value | Messwert oder `Timeout` / `NACK` / `Overflow` |
| Upper Limit | Obergrenze |
| Unit | Einheitentext (ohne Klammern) |
| Deviation (%) | Abweichung vom Sollwert |
| Result | `Pass` oder `Fail` |
| Details | Zusätzliche Meldungen des Schritts, z. B. fehlgeschlagene Antwortprüfungen oder Boundary-Scan-Pins, getrennt durch `; `, leer, wenn keine vorhanden sind. Dieselben Zeilen wie `steps[].details` in der JSON-Datei. |

Die Spaltenüberschriften und Werte sind unabhängig von der Oberflächensprache englisch, damit Auswertungsskripte stabil bleiben. Zahlen werden ohne SI-Präfixe geschrieben; ein als `1,3mA` angezeigter Wert erscheint als `0.0013`.

## JSON-Ergebnisdatei

Die JSON-Datei enthält dieselben Schritte wie die CSV-Datei und zusätzlich die Metadaten des Laufs, sodass jede Datei für sich vollständig ist. Sie ist für Auswerteprogramme und Skripte gedacht (Statistik, Ausbeute, Trends), nicht für die Tabellenkalkulation; [ATLab Analyzer](16-analyzer.md) liest diese Dateien. Feldnamen, Aufzählungswerte und Zahlen sind unabhängig von der Oberflächensprache.

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

| Feld | Inhalt |
|------|--------|
| `formatId`, `formatVersion` | Kennzeichnen die Datei als ATLab-Testergebnis und ihre Formatversion. |
| `run.startTime` | Beginn des Laufs (ISO 8601 mit UTC-Versatz). Die Dateinamen verwenden dieselbe Zeit. |
| `run.overallResult`, `run.failedSteps` | Ergebnis des gesamten Laufs, einschließlich fehlgeschlagener Schritte, die vom Export ausgeschlossen sind. |
| `run.operator`, `run.station` | Benutzername des Betriebssystems und Computername. |
| `run.runMode`, `run.loopIndex` | `Single` oder bei Dauertest `Continuous` mit der Nummer des Durchlaufs (ab 1). |
| `project.fileHash` | SHA-256 der Projektdatei auf dem Datenträger. Läufe mit gleichem Hash verwendeten dieselbe Sequenz und dieselben Grenzwerte. `null`, wenn das Projekt nie gespeichert wurde. |
| `project.hasUnsavedChanges` | `true`, wenn das Projekt ungespeicherte Änderungen hatte; der Lauf passt dann eventuell nicht zu `fileHash`. |
| `testHardware.isSimulator` | `true` für Läufe im Simulatormodus. Diese sollten nicht in Produktionsstatistiken einfließen. |
| `testEquipment` | Geräte mit **In Testbericht aufnehmen** und ihre Identifikation (`null`, wenn das Gerät nicht geantwortet hat). |
| `steps[].moduleId`, `steps[].stepId` | Feste IDs aus der Projektdatei. Sie ändern sich nicht, wenn Module oder Schritte umbenannt, umnummeriert oder verschoben werden, sodass derselbe Schritt über mehrere Läufe verglichen werden kann. |
| `steps[].unit` | Einheit wie im Schritt eingegeben. Eine Einheit in geschweiften Klammern (z. B. `{°C}`) wird ohne SI-Präfix angezeigt, siehe [Name, Grenzwerte und Einheit](08-building-test-sequences.md#name-grenzwerte-und-einheit). Anders als in der CSV-Datei und im PDF-Bericht bleiben die Klammern in der JSON-Datei erhalten, damit Auswertewerkzeuge die Werte genauso formatieren können. |
| `steps[].status` | `Measured`, `NoValue` (Schritt liefert keinen Wert), `Timeout`, `NACK`, `Overflow` oder `Unknown`. |
| `steps[].value` | Messwert ohne SI-Präfix (`1,3mA` → `0.0013`); `null`, außer bei `status` = `Measured`. |
| `steps[].details` | Zusätzliche Meldungen des Schritts, z. B. fehlgeschlagene Antwortprüfungen; `null`, wenn keine vorhanden sind. |

## PDF-Testbericht

Der Bericht wird in der aktuellen Oberflächensprache (Deutsch oder Englisch) erzeugt. Zwei Beispieldurchläufe liegen im Repository, jeweils mit Bericht und Datendatei: Prototype-001 ([PDF](../../Prototype-001_2026-10-18_18-50-00.pdf), [CSV](../../Prototype-001_2026-10-18_18-50-00.csv)) und Prototype-002 ([PDF](../../Prototype-002_2026-10-18_18-50-52.pdf), [CSV](../../Prototype-002_2026-10-18_18-50-52.csv)).

Inhalt:

1. **Kopf** – ATLab-Logo, Titel *Testbericht – \<Prüflingsname\> …*.
2. **Testinformationen** – Projektname, Bediener (Benutzername des Betriebssystems), Datum und Uhrzeit des Laufbeginns sowie Testdauer.
3. **Prüfling (DUT)** – Seriennummer, Gerätename, Revision, Variante, Teilenummer (aus der [Registerkarte Dokumentation](13-documentation-tab.md#prüfling-dut)).
4. **Zusätzliche Informationen** – nur wenn ausgefüllt.
5. **Verwendete Prüfmittel**
   - *Test-Hardware*: Name, Seriennummer und Firmware-Version des Test Interface Adapters (`Simulator` / `N/A` im Simulatormodus).
   - *Prüfmittel*: jedes Gerät mit aktivierter Option **In Testbericht aufnehmen**, mit der Antwort auf seine **Identifikationsabfrage** (z. B. den `*IDN?`-String) oder `-`, wenn es nicht geantwortet hat.
6. **Gesamtergebnis** – ein großes **BESTANDEN** (grün) oder **FEHLGESCHLAGEN** (rot). Sind fehlgeschlagene Schritte vom Export ausgeschlossen, nennt ein Hinweis darunter deren Anzahl.
7. **Ergebnistabelle** – Schritt, Name, Untergrenze, Messwert, Obergrenze und ein Bestanden/Fehlgeschlagen-Symbol, gruppiert nach Modulen mit einer Überschriftszeile je Modul.
8. **Fußzeile** – ATLab-Version und Seitenzahl.

Das Gesamtergebnis im Bericht ist das Ergebnis des **gesamten Laufs**, wie in der Registerkarte Testen, den Unterordnern `Pass` / `Fail` und der JSON-Datei. Ein fehlgeschlagener Schritt mit **Vom Export ausschließen** erscheint nicht in der Tabelle, der Bericht zeigt aber trotzdem **FEHLGESCHLAGEN** mit dem Hinweis *„1 fehlgeschlagener Schritt ist in diesem Bericht nicht aufgeführt.“*

> Beim Erzeugen des Berichts oder der JSON-Datei wird jedes Gerät mit *In Testbericht aufnehmen* abgefragt (einmal pro Lauf, auch wenn beide aktiviert sind). Ein ausgeschaltetes Gerät verzögert den Export um seinen Timeout und wird im Bericht mit `-` und in der JSON-Datei mit `null` aufgeführt.
