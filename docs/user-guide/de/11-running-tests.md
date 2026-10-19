# 11. Tests ausführen

Tests werden auf der Registerkarte **Testausführung** gestartet. Bediener arbeiten normalerweise mit ausgeschaltetem Entwicklungsmodus und sehen nur Werkzeugleiste, Schrittliste und Ergebnisleiste.

![Einen Test ausführen](images/Testing_Tab_de.png)

## Steuerung der Ausführung

| Schaltfläche | Kürzel | Aktion |
|--------------|--------|--------|
| **Einzelschritt ausführen** | `F3` | Führt nur den ausgewählten Schritt aus (siehe unten). |
| **Testausführung des ausgewählten Moduls starten** | `F4` | Führt nur die Schritte des ausgewählten Moduls aus, auch wenn das Modul deaktiviert ist. |
| **Testausführung ab ausgewähltem Schritt starten** | `Umschalt+F4` | Führt das Modul des ausgewählten Schritts ab diesem Schritt aus. Der Lauf endet mit dem letzten Schritt des Moduls, auch wenn das Modul deaktiviert ist. |
| **Dauertest starten** | `Strg+F5` | Führt die ganze Sequenz in einer Endlosschleife aus, bis sie gestoppt wird. |
| **Testausführung starten** (▶) | `F5` | Führt die ganze Sequenz einmal aus. |
| **Testausführung fortsetzen** | `Enter` | Während eines Laufs sichtbar. Beendet einen sich wiederholenden Schritt (Aktion *Wiederholen*) und fährt mit dem nächsten Schritt fort. |
| **Testausführung stoppen** (■) | `Esc` | Bricht den laufenden Test ab. |

Während ein Test läuft, sind Bearbeitungs- und Projektbefehle gesperrt, und Modul- und Schrittliste folgen dem aktuellen Schritt. *Testausführung starten* und *Dauertest starten* führen nur aktivierte Module aus.

### Unterschiede zwischen den Startarten

| | Test starten | Dauertest | Ab Auswahl | Modul | Einzelschritt |
|---|:---:|:---:|:---:|:---:|:---:|
| Fragt Seriennummer ab (falls aktiv) | ✔ | ✔ (einmal) | – | – | – |
| Automatischer Ergebnisexport (falls aktiv) | ✔ | ✔ (jeder Lauf) | – | – | – |
| Setzt Relais vorher und nachher zurück | ✔ | ✔ | ✔ | ✔ | – |
| Befolgt Bestanden-/Fehlgeschlagen-Aktionen | ✔ | ✔ | ✔ | ✔ | – |
| Zeigt *Kommentar anzeigen*-Dialog vor dem Schritt | ✔ | ✔ | ✔ | ✔ | – |
| Aktualisiert die Laufzähler | ✔ | ✔ | – | – | – |

**Testausführung des ausgewählten Moduls starten** und **Testausführung ab ausgewähltem Schritt starten** sind für die Entwicklung einzelner Module gedacht. Ein *Springe zu* auf einen Schritt außerhalb des Moduls beendet den Lauf mit einem Fehler.

**Einzelschritt ausführen** ist für die Entwicklung gedacht: Es schaltet die Relais des Schritts, wartet, erfasst und bewertet den Wert und zeigt das Ergebnis – sonst nichts. Die Relais **bleiben danach im Zustand des Schritts**. Verwenden Sie es zum Abstimmen von Masken, Grenzwerten und Verzögerungen. Ignorierte Schritte werden nicht ausgeführt.

## Eingabe der Seriennummer

Ist **Seriennummer abfragen** in den Projekteinstellungen aktiv, erscheint beim Klick auf *Testausführung starten* oder *Dauertest starten* der Dialog **Seriennummer eingeben**. Ein Barcodescanner im Tastaturmodus kann verwendet werden.

- **Starten** beginnt den Test. Die Schaltfläche ist nur aktiv, wenn die Eingabe nicht leer ist und – bei aktivierter Validierung – die Validierungsregeln erfüllt.
- **Abbrechen** bricht den Start ab.

Die Seriennummer erscheint im Feld **Seriennummer** der Ergebnisleiste, in den Dateinamen exportierter Ergebnisse und im PDF-Bericht.

## Bedienerdialoge

Während eines Laufs kann ATLab Dialoge anzeigen:

| Dialog | Titel | Schaltflächen | Wirkung |
|--------|-------|---------------|---------|
| Kommentar anzeigen (vor dem Schritt) | *Benutzerinformation* | **Weiter** / **Abbrechen** | Abbrechen stoppt den Test. |
| Aktion *Kommentar anzeigen* | *Benutzerinformation* | **Weiter** / **Abbrechen** | Zeigt optional den Messwert. Abbrechen stoppt den Test. |
| Schritt mit Benutzereingabe | *Warten auf Benutzereingabe* | **Bestanden** / **Fehlgeschlagen** | Ergebnis `1` oder `0`. |

Dialoge können ein Bild enthalten. Wird ein Dialog ohne Wahl einer Schaltfläche geschlossen, zählt das als **Abbrechen** (bzw. **Fehlgeschlagen** bei einer Benutzereingabe).

## Die Ergebnisleiste

Unterhalb der Schrittliste:

| Element | Beschreibung |
|---------|--------------|
| Statusbanner | **Test läuft** mit Fortschrittsbalken, danach **Test bestanden** (grün), **Test fehlgeschlagen** (rot), **Test abgebrochen** oder **Testausführung fehlgeschlagen**. |
| **Testdauer** | Verstrichene Zeit des Laufs. |
| **Fehlgeschlagene Schritte** | Anzahl fehlgeschlagener Schritte in diesem Lauf. Rot hervorgehoben, wenn größer als null. |
| **Bestandene Tests** | Bestandene Läufe / alle Läufe seit dem Öffnen des Projekts, mit Prozentangabe. Gezählt werden nur *Testausführung starten* und *Dauertest starten*. Nützlich bei Dauertests. |
| **Bediener** | Benutzername des Betriebssystems; erscheint auch im Bericht als *Bediener*. |
| **Prüfling (DUT)** | Gerätename, Revision, Variante und Teilenummer aus der [Registerkarte Dokumentation](13-documentation-tab.md#prüfling-dut), oder `-`. Beim Darüberfahren werden die einzelnen Felder angezeigt. |
| **Seriennummer** | Seriennummer des aktuellen Laufs oder `-`. Beim Darüberfahren wird eine lange Seriennummer vollständig angezeigt. |

Bei schmalem Fenster werden *Prüfling (DUT)* und *Seriennummer* mit „…“ gekürzt.

### Gesamtergebnis

| Status | Wann |
|--------|------|
| **Test bestanden** | Der Lauf wurde beendet und kein Schritt ist fehlgeschlagen. |
| **Test fehlgeschlagen** | Der Lauf wurde beendet und mindestens ein Schritt ist fehlgeschlagen (einschließlich Timeouts und Überläufen). |
| **Test abgebrochen** | Der Lauf wurde vom Bediener gestoppt (Stopp-Schaltfläche, `Esc`, Steuermodul oder *Abbrechen* in einem Dialog). |
| **Testausführung fehlgeschlagen** | Ein Schritt konnte gar nicht ausgeführt werden (z. B. Gerät nicht erreichbar, Antwort nicht lesbar). Der Lauf wird abgebrochen. |

Ergebnisse werden nur bei **bestandenen** und **fehlgeschlagenen** Läufen exportiert.

## Zwischen fehlgeschlagenen Schritten springen

Nach einem Lauf springen zwei Schaltflächen rechts neben der Ausführungssteuerung zwischen den fehlgeschlagenen Schritten:

| Schaltfläche | Kürzel | Funktion |
|--------------|--------|----------|
| **Zum vorherigen fehlgeschlagenen Testschritt springen** (Umriss-Pfeil nach oben mit Strich) | `Umschalt+F8` | Wählt den nächstgelegenen fehlgeschlagenen Schritt vor dem ausgewählten Schritt aus. |
| **Zum nächsten fehlgeschlagenen Testschritt springen** (Umriss-Pfeil nach unten mit Strich) | `F8` | Wählt den nächstgelegenen fehlgeschlagenen Schritt nach dem ausgewählten Schritt aus. Ist kein Schritt ausgewählt, wird der erste fehlgeschlagene Schritt ausgewählt. |

Die Suche läuft über alle Module in ihrer Reihenfolge und wechselt in der Modulliste zum Modul des gefundenen Schritts. Eine Schaltfläche ist nur aktiv, wenn es in ihrer Richtung einen fehlgeschlagenen Schritt gibt; ist kein ausgeführter Schritt fehlgeschlagen, sind beide gesperrt. Während ein Test läuft, sind beide gesperrt. Sie stehen auch bei ausgeschaltetem Entwicklungsmodus zur Verfügung.

## Messfenster

Die Lupen-Schaltfläche (**Messfenster anzeigen**) blendet zwischen Schrittliste und Ergebnisleiste eine große Anzeige ein. Sie zeigt Untergrenze, Messwert und Obergrenze des ausgewählten Schritts auf einer Skala – aus der Entfernung lesbar und praktisch, um mit einem sich wiederholenden Schritt ein Potentiometer abzugleichen.

## Debug-Modus

Im Entwicklungsmodus erscheinen rechts neben den Schaltflächen für fehlgeschlagene Schritte zwei zusätzliche Schaltflächen:

| Schaltfläche | Kürzel | Funktion |
|--------------|--------|----------|
| **Debug-Modus aktivieren** (Käfer-Symbol) | `F9` | Wenn aktiv, hält jeder gestartete Lauf **vor jedem Schritt** an. |
| **Einzelschritt weiterschalten** (gebogener Pfeil mit Punkt) | `F10` | Führt den Schritt aus, an dem der Lauf wartet, und hält vor dem nächsten erneut an. |

Mit dem Debug-Modus gehen Sie eine Sequenz Schritt für Schritt durch – mit allen Aktionen, Dialogen und Relaisänderungen – und können dazwischen Ergebnisse und Relaiszustände prüfen. Mit `Esc` stoppen Sie den Lauf jederzeit. Der Debug-Modus lässt sich nur umschalten, während kein Test läuft.

## Steuermodul

Das Steuermodul ist ein USB-HID-Gerät mit zwei beleuchteten Tasten. Aktivieren Sie es in den Projekteinstellungen (**Steuermodul aktivieren**).

| Situation | Linke (grüne) Taste | Rechte (rote) Taste |
|-----------|---------------------|---------------------|
| Ruhezustand | Test starten (wie `F5`) | Test stoppen |
| Während eines sich wiederholenden Schritts | Fortsetzen (wie `Enter`) | Test stoppen |
| Seriennummer-Dialog | Starten (bei gültiger Eingabe) | Abbrechen |
| Benutzereingabe-Dialog | **Bestanden** | **Fehlgeschlagen** |
| Kommentar-Dialog | **Weiter** | **Abbrechen** |

Die Status-LEDs des Moduls zeigen den Testzustand: Ruhe, läuft, bestanden, fehlgeschlagen und abgebrochen. Wird das Modul beim Aktivieren der Einstellung nicht gefunden, wird die Meldung *„No control module connected.“* protokolliert.

## Einen Test sicher beenden

Wenn ein Test endet – regulär, durch Fehler oder durch Stoppen –, setzt ATLab alle Relais des Test Interface Adapters zurück und schließt die Verbindungen zu den Messgeräten. Bei **Einzelschritt ausführen** bleiben die Relais so, wie der Schritt sie gesetzt hat; führen Sie einen vollständigen Test aus oder einen Schritt ohne Relais, um sie zurückzusetzen.
