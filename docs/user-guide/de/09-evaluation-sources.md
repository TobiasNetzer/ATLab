# 9. Auswertungsquellen

Das Feld **Auswertung** eines Schritts legt fest, woher sein Messwert kommt. Jede Quelle liefert letztlich eine Zahl, die anschließend mit den Grenzwerten des Schritts verglichen wird.

| Quelle | Liefert | Editorbereich | Antwortverarbeitung |
|--------|---------|---------------|:---:|
| [Keine](#keine) | keinen Wert, besteht immer | – | – |
| [Skript](#skript) | Antwort des mit *Auswertung* markierten Skriptbefehls | Skript-Konfiguration | ✔ |
| [Gerätebefehl](#gerätebefehl) | Antwort eines einzelnen Befehls | Gerätebefehl | ✔ |
| [Schnittstellenbefehl](#schnittstellenbefehl) | über I²C/UART des Adapters gelesene Daten | Schnittstellenbefehl | ✔ |
| [Externes Programm](#externes-programm) | Exit-Code oder Ausgabe des Programms | Externes Programm | ✔ (nur Ausgabe) |
| [Benutzerbestätigung](#benutzerbestätigung) | `1` (Bestanden) oder `0` (Fehlgeschlagen) | – (verwendet den Kommentar) | – |
| [Benutzereingabe](#benutzereingabe) | vom Bediener eingegebene Zahl | – (verwendet den Kommentar) | ✔ |
| [Ausdruck](#ausdruck) | Ergebnis einer Berechnung | Ausdruck | – |
| [Datei](#datei) | aus einer Datei gelesene Zahl | Dateipfad | ✔ |
| [Boundary Scan](#boundary-scan) | Anzahl der Pins ohne den erwarteten Pegel | Boundary Scan | – |
| [Boundary-Scan-Bus](#boundary-scan-bus) | von einem über Boundary Scan nachgebildeten I²C-/SPI-Baustein oder einer Pin-Gruppe gelesene Daten | Boundary-Scan-Bus | ✔ |

## Besondere Ergebnisse

| Anzeige | Bedeutung | Bewertung |
|---------|-----------|-----------|
| *(leer)* mit ✔ | Quelle **Keine** | Bestanden |
| `Timeout` | Messgerät/Schnittstelle hat nicht innerhalb des Timeouts geantwortet. Die Fehlgeschlagen-Aktion greift, der Test läuft weiter. | Fehlgeschlagen |
| `Overflow` | Wert ≥ 9,9 × 10³⁷ (der SCPI-Überlaufwert, z. B. Multimeter außerhalb des Messbereichs) | Fehlgeschlagen |
| *(leer)* mit ✘ und Fehler im Ereignisprotokoll | Der Schritt konnte nicht ausgeführt werden (Gerät nicht gefunden, Verbindung fehlgeschlagen, Parse-Fehler …). **Der Test stoppt** mit dem Status *Testausführung fehlgeschlagen*. | Fehlgeschlagen |

Eine Antwort, die sich nicht in eine Zahl umwandeln lässt – z. B. `OK` oder `+1.23E+00 VDC` –, ist ein Ausführungsfehler. Verwenden Sie die [Antwortverarbeitung](11-response-processing.md), um solche Antworten in eine Zahl umzuwandeln. Sowohl `.` als auch `,` werden als Dezimaltrennzeichen akzeptiert (`4,5` ergibt 4,5); Tausendertrennzeichen werden nicht unterstützt.

---

## Keine

Der Schritt schaltet nur Relais, wartet seine Verzögerung ab und zeigt optional seinen Kommentar. Er besteht immer und zeigt keinen Messwert. Typische Anwendungen: Einrichtungsschritte, Wartezeiten, Bedieneranweisungen, Zuschalten eines Stimulus vor der nächsten Messung.

## Skript

Führt ein Skript aus dem [Skriptordner](07-scripting.md) auf einem Gerät aus.

**Bereich Skript-Konfiguration**

| Feld | Beschreibung |
|------|--------------|
| **Zielgerät** | Gerät aus dem Geräte-Manager, angezeigt als `Name (Ressource)`. Die ×-Schaltfläche leert die Auswahl. |
| **Skript** | Skript aus dem Skriptordner. Beim Überfahren eines Eintrags erscheint seine Beschreibung. Die ×-Schaltfläche leert die Auswahl. |
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

Der Befehl wird meist mit Kodierungsblöcken geschrieben, z. B. `{hex:0A 1F}` zum Senden der beiden Bytes 0x0A 0x1F (siehe [Kapitel 10](10-commands-variables-expressions.md#kodierungsblöcke)).

**Verhalten bei I²C**

- Mit Befehl und **Bytes lesen** = 0: Die Bytes werden geschrieben; das Ergebnis ist `1`, wenn der Slave quittiert hat, `0` bei NACK.
- Mit **Bytes lesen** > 0: Nach dem optionalen Schreiben wird die angegebene Anzahl Bytes gelesen. Die empfangenen Bytes durchlaufen die Antwortverarbeitung; ein NACK beim Lesen ergibt `0`.
- Ohne Antwortverarbeitung werden die empfangenen Bytes als ASCII-Text interpretiert. Für binäre Register aktivieren Sie die Antwortverarbeitung, z. B. mit `{byte:2}`.
- Ein NACK wird im Ergebnis-Tooltip angezeigt (*I²C write failed: NACK received.*) und nach Abschluss des Schritts als Warnung ins Ereignisprotokoll geschrieben, auch wenn der Schritt laut Grenzwerten besteht. Ein Schritt, der wiederholt wird, bis der Prüfling antwortet, bleibt während der Wiederholungen still und warnt nur, wenn auch der letzte Versuch nicht quittiert wurde.

**Verhalten bei UART**

- Der Befehl (plus Abschlusszeichen) wird gesendet und **Bytes lesen** Bytes werden erwartet. Kommen weniger an, zeigt der Schritt das im Ergebnis-Tooltip und protokolliert es nach Abschluss des Schritts als Warnung.
- Bei **Bytes lesen** = 0 ist der Messwert `0`.

Die Schnittstelle muss am angeschlossenen Adapter vorhanden sein (siehe Registerkarte Test-Hardware); sonst schlägt der Schritt mit *„Interface not available“* fehl. RS485 ist in der Liste aufgeführt, wird für Testschritte aber noch nicht unterstützt.

## Externes Programm

Führt ein Programm auf dem Prüfrechner aus. Standardmäßig ist der **Exit-Code** der Messwert – `0` bedeutet normalerweise Erfolg, setzen Sie die Grenzwerte also auf `0 … 0`. Mit **Ausgabe auswerten** ist stattdessen die Ausgabe des Programms der Messwert.

| Feld | Beschreibung |
|------|--------------|
| **Befehl** | Befehlszeile. Darf `{Variable}`-Verweise enthalten. Das erste Wort (oder ein `"Pfad in Anführungszeichen"`) ist das Programm, der Rest sind Argumente. |
| **Ausgabe auswerten** | Aus: Der Exit-Code ist der Messwert. Ein: Das Programm läuft ohne Fenster, seine Standardausgabe wird wie eine Geräteantwort ausgewertet (bei Bedarf mit [Antwortverarbeitung](11-response-processing.md)) und muss eine Zahl ergeben. Ein Exit-Code ungleich `0` lässt den Schritt fehlschlagen; die Fehlermeldung enthält die Fehlerausgabe des Programms. |

Der Befehl wird genau so ausgeführt, wie er eingegeben wurde – ohne Shell dazwischen. Für Shell-Funktionen wie Pipes, Umleitungen, eingebaute Befehle (`dir`, `echo`, `type`) oder Batch-Dateien rufen Sie die Shell selbst auf:

- **Windows:** `cmd /c <Befehl>` führt den Befehl aus und schließt das Fenster; `cmd /k <Befehl>` lässt das Fenster offen – der Schritt wartet dann, bis der Bediener es schließt, und der Exit-Code ist der von `cmd`, nicht der Ihres Befehls.
- **Linux:** `bash -c "<Befehl>"`. Verwenden Sie doppelte Anführungszeichen für Argumente mit Leerzeichen; einfache Anführungszeichen werden nicht unterstützt. Unter Linux wird kein Terminalfenster geöffnet.

> **Terminalfenster unter Linux:** Sie können jeden Terminal-Emulator selbst starten, z. B. `gnome-terminal -- bash -c "./flash.sh; exec bash"`. Der Schritt erhält dann den Exit-Code des **Terminal-Emulators**, nicht den Ihres Befehls: `gnome-terminal` und `ptyxis` liefern sofort `0`, noch bevor der Befehl fertig ist; `xterm` wartet, liefert aber meist unabhängig vom Ergebnis `0`. Brauchen Sie ein sichtbares Fenster *und* ein verlässliches Ergebnis, lassen Sie den Befehl seinen Exit-Code in eine Datei schreiben und werten diese mit einem nachfolgenden [Datei](#datei)-Schritt aus:
>
> ```text
> gnome-terminal --wait -- bash -c "./flash.sh; echo $? > exit.txt"
> ```
>
> `--wait` lässt `gnome-terminal` warten, bis der Befehl beendet ist. Beachten Sie, dass `{...}` als ATLab-Variablenverweis interpretiert wird; vermeiden Sie daher Bash-Syntax wie `${VAR}` im Befehl.

Arbeitsverzeichnis ist der Projektordner; Skripte neben dem Projekt lassen sich also mit relativen Pfaden aufrufen. Während der Schritt wartet, zeigt der Statusbereich *Warten auf externe Anwendung...*. Wird der Testlauf gestoppt, beendet ATLab das Programm und alle von ihm gestarteten Prozesse.

Beispiele:

```text
python flash_firmware.py --port COM7 --sn {SerialNo}
"C:\Program Files\SEGGER\JLink\JLink.exe" -CommanderScript program.jlink
cmd /c program_board.bat {SerialNo}
cmd /c type result.txt | findstr Voltage
```

Das letzte Beispiel ist nur mit **Ausgabe auswerten** und einer Antwortmaske sinnvoll, die die Zahl aus der gefundenen Zeile herausschneidet.

## Benutzerbestätigung

Zeigt den **Kommentar** des Schritts in einem Dialog mit dem Titel *Warten auf Benutzerbestätigung* und den Schaltflächen **Bestanden** und **Fehlgeschlagen**. **Bestanden** ergibt `1`, **Fehlgeschlagen** ergibt `0`. Setzen Sie alle Grenzwerte auf `1`, damit nur *Bestanden* besteht.

Ein über die Bild-Schaltfläche neben *Kommentar anzeigen* (Erweiterte Einstellungen) hinzugefügtes Bild wird im Dialog angezeigt. Mit aktiviertem [Steuermodul](14-running-tests.md#steuermodul) beantwortet die grüne Taste *Bestanden* und die rote *Fehlgeschlagen*.

## Benutzereingabe

Fordert den Bediener auf, einen Wert einzugeben – z. B. die Ablesung eines Zeigerinstruments, einen Wert von einem Handmessgerät oder eine auf dem Prüfling aufgedruckte Zahl. Zeigt den **Kommentar** des Schritts in einem Dialog mit dem Titel *Warten auf Benutzereingabe* mit einem Textfeld und den Schaltflächen **Weiter** und **Abbrechen**. Der Cursor steht bereits im Textfeld, der Bediener kann also sofort tippen.

- Der eingegebene Wert ist der Messwert und wird wie jede andere Messung mit den Grenzwerten verglichen.
- Sowohl `.` als auch `,` werden als Dezimaltrennzeichen akzeptiert (`1.5` und `1,5` ergeben beide 1,5). Tausendertrennzeichen und SI-Präfixe (`k`, `m` …) werden nicht unterstützt – geben Sie eine einfache Zahl ein, z. B. `4700` oder `4.7e3`.
- Ist die [Antwortverarbeitung](11-response-processing.md) aktiviert, wird der eingegebene Text vor der Umwandlung wie eine Geräteantwort verarbeitet, z. B. um die Zahl aus einem gescannten Etikett wie `R=4.7` herauszulösen.
- Text, der sich nicht in eine Zahl umwandeln lässt, ist ein Ausführungsfehler und stoppt den Test.
- **Abbrechen** stoppt den Test.

Ein über die Bild-Schaltfläche neben *Kommentar anzeigen* (Erweiterte Einstellungen) hinzugefügtes Bild wird im Dialog angezeigt. Mit aktiviertem [Steuermodul](14-running-tests.md#steuermodul) wirkt die grüne Taste als **Weiter** und die rote als **Abbrechen**.

## Ausdruck

Berechnet einen Wert. Das Feld **Ausdruck** darf Zahlen, Laufzeitvariablen und Operatoren enthalten, zum Beispiel:

```text
{V_In} / {I_In}
({V_Out} - {V_Ref}) * 1000
0x3F & {Status}
```

Die unterstützten Operatoren sind in [Kapitel 10](10-commands-variables-expressions.md#ausdrücke) aufgeführt. Kombiniert mit *Ergebnis einer Variable zuweisen* lassen sich abgeleitete Werte, Zähler oder Flags berechnen. Ein nicht auswertbarer Ausdruck ist ein Ausführungsfehler.

## Datei

Liest eine Datei und extrahiert daraus eine Zahl.

| Feld | Beschreibung |
|------|--------------|
| **Dateipfad** | Pfad zur Datei. Mit der Durchsuchen-Schaltfläche auswählen. Pfade im Projektordner werden relativ gespeichert. |

Ohne Antwortverarbeitung muss der gesamte Dateiinhalt eine Zahl sein (z. B. eine Datei mit `3.274`). Sowohl `.` als auch `,` werden als Dezimaltrennzeichen akzeptiert (`3,274` ergibt ebenfalls 3,274); Tausendertrennzeichen werden nicht unterstützt. Mit Antwortverarbeitung können Sie einen Wert an fester Position mit **Überspringen**/**Länge** aus einer größeren Datei herausschneiden oder einfach prüfen, ob die Datei einen bestimmten Text enthält (siehe [Antwortverarbeitung](11-response-processing.md)). Ein typisches Muster ist ein Schritt mit der Auswertungsquelle *Externes Programm*, der ein externes Werkzeug startet, das sein Ergebnis in eine Datei schreibt, gefolgt von einem Datei-Schritt, der sie auswertet.

## Boundary Scan

Treibt und prüft Pins von Boundary-Scan-Bausteinen. Kette, BSDL-Dateien und Bezeichnungen werden auf der [Registerkarte Boundary Scan](12-boundary-scan.md) eingerichtet.

**Bereich Boundary Scan**

| Feld | Beschreibung |
|------|--------------|
| **Pin-Aktionen** / **Boundary Scan ausschalten** | Modus des Schritts, siehe unten. |
| Aktionstabelle | Eine Zeile pro Pin: **Baustein**, **Pin** (Port und Bezeichnung) und **Aktion**. Zum Ändern die Aktion doppelklicken. |
| Zeile zum Hinzufügen | Baustein, Pin und Aktion wählen, dann **+** klicken. Ein Pin wird pro Schritt höchstens einmal getrieben und einmal geprüft; erneutes Hinzufügen ersetzt die vorhandene Aktion. JTAG- und Versorgungspins sind nicht auswählbar. |
| Schaltfläche **−** | Entfernt die ausgewählte Aktion. |
| Import-Schaltfläche | **Aus dem Boundary-Scan-Tab übernehmen**: Jeder Ausgang, den Sie auf der Registerkarte gegenüber seinem sicheren Wert geändert haben, wird zur Drive-Aktion, jeder Pin mit Bezeichnung und gelesenem Pegel zur Expect-Aktion. |
| **Getriebene Pins prüfen** | Liest zusätzlich jeden auf `0` oder `1` getriebenen Pin zurück und zählt einen abweichenden Pegel als Abweichung, siehe unten. |
| **Einschwingzeit (ms)** | Wartezeit zwischen dem Treiben der Ausgänge und dem Lesen der Eingänge. |

| Aktion | Wirkung |
|--------|---------|
| **Drive 0** / **Drive 1** | Treibt den Pin auf Low bzw. High. |
| **Drive Z** | Schaltet den Ausgang ab (nur bei Pins, deren Ausgang abschaltbar ist). |
| **Expect 0** / **Expect 1** | Liest den Pin und prüft seinen Pegel. |

**Ablauf**

1. Alle Drive-Aktionen werden angewendet. Bausteine mit getriebenen Pins werden in EXTEST geschaltet, Bausteine, die nur gelesen werden, in SAMPLE.
2. Nach der Einschwingzeit werden alle Expect-Aktionen gelesen – mit **Getriebene Pins prüfen** auch die getriebenen Pins.
3. Der Messwert ist die **Anzahl der Abweichungen**. Setzen Sie die Grenzwerte auf `0 … 0`: Der Schritt besteht, wenn jeder Pin den erwarteten Pegel hat.

Ein Schritt nur mit Drive-Aktionen liefert keinen Wert und besteht immer, wie die Quelle *Keine* – außer **Getriebene Pins prüfen** ist aktiviert. Pins, die ein Schritt nicht auflistet, behalten ihren Zustand aus den vorherigen Schritten.

Bei einem fehlgeschlagenen Schritt zeigt der Tooltip über dem Messwert oder dem Ergebnissymbol, welche Pins abweichen (bis zu 50 Zeilen), z. B. `U3 IO(5) (FAULT_N): expected 0, read 1`. Ist der Schritt abgeschlossen und fehlgeschlagen, werden die Abweichungen einmal ins Ereignisprotokoll geschrieben, z. B. *Module 'LED', step 3: U3 IO(5) (FAULT_N): expected 0, read 1*. Ein Schritt, der wiederholt wird, bis eine Bedingung erfüllt ist – etwa bis der Bediener einen Taster drückt –, protokolliert während der Wiederholungen nichts und gar nichts, wenn er am Ende besteht.

**Getriebene Pins prüfen** liest jeden Pin mit *Drive 0* oder *Drive 1* über seine eigene Eingangszelle zurück. Ein abweichender Pegel wird z. B. als `U1 PB3 (LED_RED): driven 1, read 0` gemeldet. Damit werden Kurzschlüsse nach GND, VCC oder zu einem anderen Treiber sowie defekte Ausgangstreiber gefunden. Eine Unterbrechung weiter im Netz wird nicht erkannt, weil der treibende Pin weiterhin seinen eigenen Pegel liest – dafür den empfangenden Pin mit einer Expect-Aktion prüfen. Pins ohne Eingangszelle und *Drive Z* werden nicht geprüft; eine Expect-Aktion für denselben Pin hat Vorrang vor dem Zurücklesen.

Die Probe wird beim ersten Boundary-Scan-Schritt des Laufs verbunden und die Kette geprüft. Eine Kette, die nicht zum Projekt passt, ein Baustein ohne BSDL-Datei oder ein Pin, der nicht getrieben oder gelesen werden kann, ist ein Ausführungsfehler und beendet den Test.

**Boundary Scan ausschalten** versetzt alle Bausteine in den Normalbetrieb und gibt die Probe frei. Verwenden Sie es vor Schritten, die die Bausteine im Normalbetrieb benötigen oder selbst auf die Probe zugreifen, z. B. das Programmieren über denselben J-Link mit einem Schritt [Externes Programm](#externes-programm). Ein späterer Boundary-Scan-Schritt verbindet automatisch neu. Siehe auch [Boundary Scan während der Testausführung](12-boundary-scan.md#boundary-scan-während-der-testausführung).

Beispiel – prüfen, dass eine LED angesteuert wird und ihre Rückmeldeleitung folgt:

| Schritt | Pin-Aktionen | Grenzwerte |
|---------|--------------|------------|
| *LED an* | `U1 PB3 (LED_RED)`: Drive 1 · `U2 PA0 (LED_SENSE)`: Expect 1 | `0 … 0` |
| *LED aus* | `U1 PB3 (LED_RED)`: Drive 0 · `U2 PA0 (LED_SENSE)`: Expect 0 | `0 … 0` |
| *Freigeben zum Programmieren* | Boundary Scan ausschalten | – |

## Boundary-Scan-Bus

Führt eine Übertragung auf einem über Boundary-Scan-Pins nachgebildeten Bus aus: mit einem I²C- oder SPI-Baustein oder einer Pin-Gruppe. Die Busse werden auf der [Registerkarte Boundary Scan](12-boundary-scan.md#ansicht-busse) eingerichtet; der Schritt funktioniert wie ein [Schnittstellenbefehl](#schnittstellenbefehl).

**Bereich Boundary-Scan-Bus**

| Feld | Beschreibung |
|------|--------------|
| **Bus** | Bus aus der Registerkarte Boundary Scan, angezeigt als `Name (Typ)`. |
| **Adresse 0-127** | Nur I²C: 7-Bit-Zieladresse. Als Dezimalzahl oder mit `0x`-Präfix eingeben, z. B. `80` oder `0x50`. |
| **Befehl** | Zu schreibende Bytes, meist als Kodierungsblock, z. B. `{hex:10}`. Unterstützt Variablen und Ausdrücke (siehe [Kapitel 10](10-commands-variables-expressions.md)). |
| **Bytes lesen** | Anzahl der danach zu lesenden Bytes. |

| Bustyp | Übertragung |
|--------|-------------|
| **I²C** | Start, Adresse + Schreiben, Befehlsbytes; dann, falls Bytes gelesen werden sollen, Repeated Start, Adresse + Lesen und die Bytes; Stop. Ein Befehl gefolgt von einem Lesezugriff ist ein typischer Registerzugriff. |
| **SPI** | Chip-Select wird aktiv, die Befehlsbytes werden gesendet, dann werden **Bytes lesen** Bytes eingetaktet, während `0x00` gesendet wird; Chip-Select wird freigegeben. |
| **Pin Group** | Ein Befehl treibt seinen Wert auf Bit 0 … n (erstes Byte = Bits 0–7). Beim Lesen werden die Pins freigegeben und ihre Pegel gelesen. |

**Ergebnis**

- Mit **Bytes lesen** = 0: `1`, wenn die Übertragung abgeschlossen wurde (I²C: quittiert), `0` bei einem I²C-NACK.
- Mit **Bytes lesen** > 0: Die empfangenen Bytes durchlaufen die [Antwortverarbeitung](11-response-processing.md). Ohne Antwortverarbeitung werden sie als ASCII-Text interpretiert; für Binärdaten daher aktivieren, z. B. `{byte:2}` für ein 16-Bit-Register. Ein NACK ergibt `0`.
- Bei einem NACK erklärt der Ergebnis-Tooltip die Ursache, z. B. *No ACK from 0x51 for address 0x51 (write).* Die Erklärung wird nach Abschluss des Schritts als Warnung ins Ereignisprotokoll geschrieben, auch wenn der Schritt laut Grenzwerten besteht. Ein Schritt kann wiederholt werden, bis ein Baustein antwortet, ohne bei jeder Wiederholung eine Warnung anzuzeigen; er warnt nur, wenn auch der letzte Versuch nicht quittiert wurde.

Beispiele:

| Zweck | Bus | Adresse | Befehl | Bytes lesen | Maske |
|-------|-----|---------|--------|-------------|-------|
| 16-Bit-Register 0x10 eines I²C-Bausteins lesen | I²C | `0x48` | `{hex:10}` | `2` | `{byte:2}` |
| Zwei Bytes ab Adresse 0x00 in ein EEPROM schreiben | I²C | `0x50` | `{hex:00 12 34}` | `0` | – |
| JEDEC-ID eines SPI-Flash lesen | SPI | – | `{hex:9F}` | `3` | `{byte:3}` |
| 4-Bit-Baugruppenkennung lesen | Pin Group | – | – | `1` | `{byte:1}` |

Alle Signalwechsel einer Übertragung werden in einem Transfer an die Probe geschickt. I²C-Clock-Stretching wird nicht unterstützt; siehe [Geschwindigkeit und Einschränkungen](12-boundary-scan.md#geschwindigkeit-und-einschränkungen).
