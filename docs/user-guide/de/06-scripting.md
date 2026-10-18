# 6. Registerkarte Skripte

Ein **Skript** ist eine benannte, wiederverwendbare Folge von Gerätebefehlen, z. B. *„Configure & Query DC Voltage“*. Statt dieselben SCPI-Befehle in viele Schritte zu tippen, schreiben Sie sie einmal als Skript und wählen das Skript in jedem Schritt aus. Mit Variablen kann ein Skript viele Schritte mit unterschiedlichen Einstellungen bedienen.

![Registerkarte Skripte](images/Scripting_Tab_de.png)

## Der Skriptordner

Skripte werden als einzelne JSON-Dateien (`<id>.json`) in einem **Skriptordner** gespeichert, unabhängig von einem Projekt. Mehrere Projekte können einen Skriptordner gemeinsam nutzen.

- Der Pfad des Skriptordners wird im Projekt gespeichert. Liegt er im Projektordner, wird er relativ gespeichert.
- Ein neues Projekt verwendet den Standard-Skriptordner:
  - Windows: `%LOCALAPPDATA%\ATLab\Scripts`
  - Linux: `~/.local/share/ATLab/Scripts`
- Existiert der konfigurierte Ordner nicht, versucht ATLab, ihn anzulegen; gelingt das nicht, wird der Standardordner verwendet und eine Meldung protokolliert.

## Werkzeugleiste

| Schaltfläche | Funktion |
|--------------|----------|
| **Neues Skript** | Legt ein neues, leeres Skript an und speichert es sofort im Skriptordner. |
| **Skript speichern** | Speichert das ausgewählte Skript. |
| **Skript löschen** | Löscht die Datei des ausgewählten Skripts nach Rückfrage dauerhaft. |
| **Skriptordner ändern** | Wählt einen anderen Ordner als Skriptordner und lädt die Liste neu. |
| **Skriptordner neu laden** | Liest alle Skripte erneut vom Datenträger, z. B. nach dem Abrufen von Änderungen aus der Versionsverwaltung. |

Skripte mit ungespeicherten Änderungen sind in der Liste mit `*` markiert, und die Registerkarte heißt **Skripte\***, solange ein Skript ungespeichert ist. Skriptänderungen werden **nicht** mit dem Projekt gespeichert – verwenden Sie **Skript speichern**.

## Ein Skript bearbeiten

Wählen Sie ein Skript in der Liste **Skripte**. Rechts können Sie bearbeiten:

- **Name** – erscheint in der Skriptauswahl des Schritts.
- **Beschreibung** – erscheint als Tooltip in der Skriptauswahl.
- **Variablen** – Platzhalter mit Standardwerten.
- **Befehle** – die Befehlsliste.

### Variablen

| Spalte | Beschreibung |
|--------|--------------|
| **Variablenname** | Nur Buchstaben, Ziffern und `_`. |
| **Standardwert** | Wert, mit dem ein Schritt beginnt, wenn das Skript erstmals ausgewählt wird. |

Verwenden Sie eine Variable in einem Befehl, indem Sie ihren Namen in geschweifte Klammern setzen, z. B. `SENS:VOLT:DC:RANGE {Range}`. Jeder Testschritt, der das Skript verwendet, erhält eine eigene Kopie der Variablen und kann die Werte überschreiben (siehe [Auswertung Skript](08-evaluation-sources.md#skript)).

### Befehle

| Spalte | Beschreibung |
|--------|--------------|
| **Befehl** | An das Gerät gesendeter Text. Kann Skriptvariablen, Laufzeitvariablen, Kodierungsblöcke und Ausdrücke enthalten ([Kapitel 9](09-commands-variables-expressions.md)). Leere Befehle werden übersprungen. |
| **Antwort erwarten** | Befehl senden und auf eine Antwort warten (Abfrage). Ist die Option aus, wird nur geschrieben. |
| **Auswertung** | Die Antwort dieses Befehls als Messwert des Schritts verwenden. Nur **ein** Befehl pro Skript kann markiert sein; das Markieren eines anderen hebt die vorherige Markierung auf. Erfordert, dass auch **Antwort erwarten** aktiv ist. |
| **Verzögerung (ms)** | Wartezeit **vor** dem Senden des Befehls. |
| **Timeout (ms)** | Wie lange auf die Antwort gewartet wird (Standard 1000 ms). Bei Überschreitung endet der Schritt mit dem Ergebnis `Timeout`. |

Befehle werden von oben nach unten ausgeführt. Mit **+**/**−** oder über das Kontextmenü hinzufügen oder entfernen; ein neuer Befehl wird unter dem ausgewählten eingefügt.

## Beispiel: Gleichspannungsmessung

**Variablen**

| Variablenname | Standardwert |
|---------------|--------------|
| `Range` | `100` |
| `NPLC` | `5` |

**Befehle**

| Befehl | Antwort erwarten | Auswertung | Verzögerung (ms) | Timeout (ms) |
|--------|:---:|:---:|---:|---:|
| `SENS:VOLT:DC:RANGE {Range}` | | | 0 | 1000 |
| `SENS:VOLT:NPLC {NPLC}` | | | 0 | 1000 |
| `READ?` | ✔ | ✔ | 500 | 1000 |

Ein Schritt mit diesem Skript sendet die beiden Konfigurationsbefehle, wartet 500 ms, fragt `READ?` ab und verwendet die Antwort des Geräts (z. B. `+1.2001555E+01`) als Messwert.

## Bewährte Vorgehensweisen

- Halten Sie Skripte klein und allgemein; verlagern Sie schrittspezifische Werte in Variablen.
- Legen Sie den Skriptordner neben Ihre Projekte und unter Versionsverwaltung.
- Liefert ein Skript mehr als eine reine Zahl, verwenden Sie im Schritt die [Antwortverarbeitung](10-response-processing.md), um den Wert zu extrahieren.
- Ist kein Befehl mit **Auswertung** markiert, ist der Messwert des Schritts `0`.
