# 15. Testergebnisse & Berichte

ATLab kann das Ergebnis jedes Laufs automatisch als Datendatei (CSV) und/oder als formatierten Testbericht (PDF) exportieren.

## Automatischen Export aktivieren

Registerkarte **Konfiguration** → **Projekteinstellungen** → **Testausführungseinstellungen**:

1. **Seriennummer abfragen** aktivieren (erforderlich – die Seriennummer ist Teil des Dateinamens).
2. **Testergebnis speichern** aktivieren.
3. **Immer** oder **Nur wenn bestanden** wählen.
4. **Testdaten exportieren (CSV)** und/oder **Testbericht exportieren (PDF)** aktivieren.
5. Den **Ausgabepfad** festlegen. Der Ordner wird bei Bedarf angelegt.

## Wann Dateien geschrieben werden

Dateien werden am Ende eines mit **Testausführung starten** oder **Dauertest starten** (bei jedem Durchlauf) begonnenen Laufs geschrieben, wenn der Lauf mit **Test bestanden** oder **Test fehlgeschlagen** endet. Bei *Nur wenn bestanden* werden fehlgeschlagene Läufe nicht exportiert.

Keine Dateien entstehen bei abgebrochenen Läufen, bei durch einen Ausführungsfehler beendeten Läufen, bei Läufen ab dem ausgewählten Schritt und bei Einzelschritt-Ausführungen.

Jeder Export erzeugt eine Meldung im Ereignisprotokoll, z. B. `PDF exported to 'D:\Results\PB-2026001_2026-10-17_14-03-22.pdf'.` Ist kein Ausgabepfad gesetzt, wird der Fehler *„No output path for test results provided.“* protokolliert.

## Dateinamen

```
<Seriennummer>_<JJJJ-MM-TT>_<HH-mm-ss>.csv
<Seriennummer>_<JJJJ-MM-TT>_<HH-mm-ss>.pdf
```

Beispiel: `Prototype-001_2026-10-18_18-50-00.pdf`. Vermeiden Sie in Seriennummern Zeichen, die in Dateinamen nicht erlaubt sind (`\ / : * ? " < > |`).

## Welche Schritte enthalten sind

Beide Dateien enthalten nur Schritte, die

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
| Measured Value | Messwert oder `Timeout` / `Overflow` |
| Upper Limit | Obergrenze |
| Unit | Einheitentext (ohne Klammern) |
| Deviation (%) | Abweichung vom Sollwert |
| Result | `Pass` oder `Fail` |

Die Spaltenüberschriften und Werte sind unabhängig von der Oberflächensprache englisch, damit Auswertungsskripte stabil bleiben. Zahlen werden ohne SI-Präfixe geschrieben; ein als `1,3mA` angezeigter Wert erscheint als `0.0013`.

## PDF-Testbericht

Der Bericht wird in der aktuellen Oberflächensprache (Deutsch oder Englisch) erzeugt. Zwei Beispieldurchläufe liegen im Repository, jeweils mit Bericht und Datendatei: Prototype-001 ([PDF](../../Prototype-001_2026-10-18_18-50-00.pdf), [CSV](../../Prototype-001_2026-10-18_18-50-00.csv)) und Prototype-002 ([PDF](../../Prototype-002_2026-10-18_18-50-52.pdf), [CSV](../../Prototype-002_2026-10-18_18-50-52.csv)).

Inhalt:

1. **Kopf** – ATLab-Logo, Titel *Testbericht – \<Prüflingsname\> …*.
2. **Testinformationen** – Projektname, Bediener (Benutzername des Betriebssystems), Datum, Uhrzeit und Testdauer.
3. **Prüfling (DUT)** – Seriennummer, Gerätename, Revision, Variante, Teilenummer (aus der [Registerkarte Dokumentation](13-documentation-tab.md#prüfling-dut)).
4. **Zusätzliche Informationen** – nur wenn ausgefüllt.
5. **Verwendete Prüfmittel**
   - *Test-Hardware*: Name, Seriennummer und Firmware-Version des Test Interface Adapters (`Simulator` / `N/A` im Simulatormodus).
   - *Prüfmittel*: jedes Gerät mit aktivierter Option **In Testbericht aufnehmen**, mit der Antwort auf seine **Identifikationsabfrage** (z. B. den `*IDN?`-String) oder `-`, wenn es nicht geantwortet hat.
6. **Gesamtergebnis** – ein großes **BESTANDEN** (grün) oder **FEHLGESCHLAGEN** (rot).
7. **Ergebnistabelle** – Schritt, Name, Untergrenze, Messwert, Obergrenze und ein Bestanden/Fehlgeschlagen-Symbol, gruppiert nach Modulen mit einer Überschriftszeile je Modul.
8. **Fußzeile** – ATLab-Version und Seitenzahl.

Das Gesamtergebnis im Bericht wird aus den **im Bericht enthaltenen** Schritten berechnet. Fehlgeschlagene Schritte, die vom Export ausgeschlossen sind, erscheinen darin daher nicht.

> Beim Erzeugen des Berichts wird jedes Gerät mit *In Testbericht aufnehmen* abgefragt. Ein ausgeschaltetes Gerät verzögert den Export um seinen Timeout und wird mit `-` aufgeführt.
