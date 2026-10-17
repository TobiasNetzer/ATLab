# 8. Auswertungsquellen

Das Feld **Auswertung** eines Schritts legt fest, woher sein Messwert kommt. Jede Quelle liefert letztlich eine Zahl, die anschließend mit den Grenzwerten des Schritts verglichen wird.

| Quelle | Liefert | Editorbereich | Antwortverarbeitung |
|--------|---------|---------------|:---:|
| [Keine](#keine) | keinen Wert, besteht immer | – | – |
| [Skript](#skript) | Antwort des mit *Auswertung* markierten Skriptbefehls | Skript-Konfiguration | ✔ |
| [Gerätebefehl](#gerätebefehl) | Antwort eines einzelnen Befehls | Gerätebefehl | ✔ |
| [Schnittstellenbefehl](#schnittstellenbefehl) | über I²C/UART des Adapters gelesene Daten | Schnittstellenbefehl | ✔ |
| [Shell-Befehl](#shell-befehl) | Exit-Code des Prozesses | Shell-Befehl | – |
| [Benutzereingabe](#benutzereingabe) | `1` (Bestanden) oder `0` (Fehlgeschlagen) | – (verwendet den Kommentar) | – |
| [Ausdruck](#ausdruck) | Ergebnis einer Berechnung | Ausdruck | – |
| [Datei](#datei) | aus einer Datei gelesene Zahl | Dateipfad | ✔ |

## Besondere Ergebnisse

| Anzeige | Bedeutung | Bewertung |
|---------|-----------|-----------|
| *(leer)* mit ✔ | Quelle **Keine** | Bestanden |
| `Timeout` | Messgerät/Schnittstelle hat nicht innerhalb des Timeouts geantwortet. Die Fehlgeschlagen-Aktion greift, der Test läuft weiter. | Fehlgeschlagen |
| `Overflow` | Wert ≥ 9,9 × 10³⁷ (der SCPI-Überlaufwert, z. B. Multimeter außerhalb des Messbereichs) | Fehlgeschlagen |
| *(leer)* mit ✘ und Fehler im Ereignisprotokoll | Der Schritt konnte nicht ausgeführt werden (Gerät nicht gefunden, Verbindung fehlgeschlagen, Parse-Fehler …). **Der Test stoppt** mit dem Status *Testausführung fehlgeschlagen*. | Fehlgeschlagen |

Eine Antwort, die sich nicht in eine Zahl umwandeln lässt – z. B. `OK` oder `+1.23E+00 VDC` –, ist ein Ausführungsfehler. Verwenden Sie die [Antwortverarbeitung](10-response-processing.md), um solche Antworten in eine Zahl umzuwandeln.

---

## Keine

Der Schritt schaltet nur Relais, wartet seine Verzögerung ab und zeigt optional seinen Kommentar. Er besteht immer und zeigt keinen Messwert. Typische Anwendungen: Einrichtungsschritte, Wartezeiten, Bedieneranweisungen, Zuschalten eines Stimulus vor der nächsten Messung.

## Skript

Führt ein Skript aus dem [Skript-Repository](06-scripting.md) auf einem Gerät aus.

**Bereich Skript-Konfiguration**

| Feld | Beschreibung |
|------|--------------|
| **Zielgerät** | Gerät aus dem Geräte-Manager, angezeigt als `Name (Ressource)`. Die ×-Schaltfläche leert die Auswahl. |
| **Skript** | Skript aus dem Repository. Beim Überfahren eines Eintrags erscheint seine Beschreibung. Die ×-Schaltfläche leert die Auswahl. |
| **Variable / Wert** | Die Variablen des Skripts mit den Werten dieses Schritts. Bei der ersten Auswahl eines Skripts werden die Standardwerte übernommen; bearbeiten Sie sie pro Schritt. |

Ausführung: Alle Befehle des Skripts werden der Reihe nach gesendet; die Antwort des mit **Auswertung** markierten Befehls wird zum Messwert. Zuerst werden Skriptvariablen ersetzt, danach Laufzeitvariablen, Kodierungen und Ausdrücke verarbeitet.

Erhält das Skript später neue Variablen, werden sie beim Öffnen des Schritts mit ihren Standardwerten ergänzt; aus dem Skript entfernte Variablen werden im Schritt gelöscht.

## Gerätebefehl

Sendet einen einzelnen Befehl an ein Gerät – praktisch für schnelle, einmalige Abfragen ohne eigenes Skript.

| Feld | Beschreibung |
|------|--------------|
| **Zielgerät** | Gerät aus dem Geräte-Manager. |
| **Befehl** | Befehlstext, z. B. `MEAS:VOLT:DC?`. Unterstützt Variablen, Kodierungen und Ausdrücke. |
| **Antwort erwarten** | Ein: senden und Antwort lesen; die Antwort wird zum Messwert. Aus: nur senden; Messwert ist `0`. |
| **Timeout (ms)** | Maximale Wartezeit auf die Antwort. |

## Schnittstellenbefehl

Kommuniziert über die galvanisch isolierten Schnittstellen des Test Interface Adapters mit dem Prüfling.

| Feld | I²C | UART |
|------|:---:|:---:|
| **Schnittstelle** | `I2C` | `UART` |
| **Adresse 0-127** | 7-Bit-Slave-Adresse | – |
| **Frequenz** | 100 kHz, 400 kHz, 1 MHz | – |
| **Baudrate / Datenbits / Parität / Stoppbits** | – | serielle Parameter (Datenbits 7 oder 8) |
| **Abschlusszeichen** | – | wird an den Befehl angehängt: NONE, LF, CR, CRLF |
| **Befehl** | zu schreibende Bytes | zu sendende Bytes |
| **Bytes lesen** | Anzahl zu lesender Bytes (0–255) | Anzahl erwarteter Antwortbytes (0–255) |
| **Timeout (ms)** | Transaktions-Timeout | Transaktions-Timeout |

Der Befehl wird meist mit Kodierungsblöcken geschrieben, z. B. `{hex:0A 1F}` zum Senden der beiden Bytes 0x0A 0x1F (siehe [Kapitel 9](09-commands-variables-expressions.md#kodierungsblöcke)).

**Verhalten bei I²C**

- Mit Befehl und **Bytes lesen** = 0: Die Bytes werden geschrieben; das Ergebnis ist `1`, wenn der Slave quittiert hat, `0` bei NACK.
- Mit **Bytes lesen** > 0: Nach dem optionalen Schreiben wird die angegebene Anzahl Bytes gelesen. Die empfangenen Bytes durchlaufen die Antwortverarbeitung; ein NACK beim Lesen ergibt `0`.
- Ohne Antwortverarbeitung werden die empfangenen Bytes als ASCII-Text interpretiert. Für binäre Register aktivieren Sie die Antwortverarbeitung, z. B. mit `{byte:2}`.

**Verhalten bei UART**

- Der Befehl (plus Abschlusszeichen) wird gesendet und **Bytes lesen** Bytes werden erwartet. Kommen weniger an, wird ein Fehler protokolliert.
- Bei **Bytes lesen** = 0 ist der Messwert `0`.

Die Schnittstelle muss am angeschlossenen Adapter vorhanden sein (siehe Registerkarte Test-Hardware); sonst schlägt der Schritt mit *„Interface not available“* fehl. RS485 ist in der Liste aufgeführt, wird für Testschritte aber noch nicht unterstützt.

## Shell-Befehl

Führt ein Programm oder einen Shell-Befehl auf dem Prüfrechner aus. Der **Exit-Code** ist der Messwert – `0` bedeutet normalerweise Erfolg, setzen Sie die Grenzwerte also auf `0 … 0`.

| Feld | Beschreibung |
|------|--------------|
| **Befehl** | Befehlszeile. Darf `{Variable}`-Verweise enthalten. |
| **Direktstart** | Ein: Programm direkt starten, ohne Shell-Fenster. Das erste Wort (oder ein `"Pfad in Anführungszeichen"`) ist das Programm, der Rest sind Argumente. Aus: den Befehl in einem Terminalfenster ausführen. |
| **Fensterverhalten** | Ohne Direktstart: **Auto Close** schließt das Fenster, wenn der Befehl fertig ist; **Keep Open** lässt es offen – der Schritt wartet dann, bis der Bediener das Fenster schließt. |

Arbeitsverzeichnis ist der Projektordner; Skripte neben dem Projekt lassen sich also mit relativen Pfaden aufrufen. Während der Schritt wartet, zeigt der Statusbereich *Warten auf externe Anwendung...*.

Plattformdetails:

- **Windows:** Befehle laufen in `cmd.exe` (`/c` für Auto Close, `/k` für Keep Open). Mit **Direktstart** wartet ATLab nur dann auf den Exit-Code, wenn das Programm eine `.exe` ist; andere Dateien (z. B. `.bat`, `.py`, Dokumente) werden mit der zugeordneten Anwendung geöffnet, und das Ergebnis ist sofort `0`.
- **Linux:** Befehle laufen in `bash` im ersten verfügbaren Terminal-Emulator (ptyxis, gnome-terminal, konsole, xfce4-terminal, xterm). Manche Terminal-Emulatoren kehren sofort zurück, bevor der Befehl fertig ist; verwenden Sie **Direktstart**, wenn Sie den Exit-Code benötigen.

Beispiele:

```text
python flash_firmware.py --port COM7 --sn {SerialNo}
"C:\Program Files\SEGGER\JLink\JLink.exe" -CommanderScript program.jlink
```

## Benutzereingabe

Zeigt den **Kommentar** des Schritts in einem Dialog mit dem Titel *Warten auf Benutzereingabe* und den Schaltflächen **Bestanden** und **Fehlgeschlagen**. **Bestanden** ergibt `1`, **Fehlgeschlagen** ergibt `0`. Setzen Sie alle Grenzwerte auf `1`, damit nur *Bestanden* besteht.

Ein über *Kommentar anzeigen* (Erweiterte Einstellungen) hinzugefügtes Bild wird im Dialog angezeigt. Mit aktiviertem [Steuermodul](11-running-tests.md#steuermodul) beantwortet die grüne Taste *Bestanden* und die rote *Fehlgeschlagen*.

## Ausdruck

Berechnet einen Wert. Das Feld **Ausdruck** darf Zahlen, Laufzeitvariablen und Operatoren enthalten, zum Beispiel:

```text
{V_In} / {I_In}
({V_Out} - {V_Ref}) * 1000
0x3F & {Status}
```

Die unterstützten Operatoren sind in [Kapitel 9](09-commands-variables-expressions.md#ausdrücke) aufgeführt. Kombiniert mit *Ergebnis einer Variable zuweisen* lassen sich abgeleitete Werte, Zähler oder Flags berechnen. Ein nicht auswertbarer Ausdruck ist ein Ausführungsfehler.

## Datei

Liest eine Datei und extrahiert daraus eine Zahl.

| Feld | Beschreibung |
|------|--------------|
| **Dateipfad** | Pfad zur Datei. Mit der Durchsuchen-Schaltfläche auswählen. Pfade im Projektordner werden relativ gespeichert. |

Ohne Antwortverarbeitung muss der gesamte Dateiinhalt eine Zahl sein (z. B. eine Datei mit `3.274`). Mit Antwortverarbeitung können Sie einen Wert an fester Position mit **Überspringen**/**Länge** aus einer größeren Datei herausschneiden oder einfach prüfen, ob die Datei einen bestimmten Text enthält (siehe [Antwortverarbeitung](10-response-processing.md)). Ein typisches Muster ist ein Shell-Befehl-Schritt, der ein externes Werkzeug startet, das sein Ergebnis in eine Datei schreibt, gefolgt von einem Datei-Schritt, der sie auswertet.
