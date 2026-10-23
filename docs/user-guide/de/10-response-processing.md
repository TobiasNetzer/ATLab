# 10. Antwortverarbeitung

Messgeräte und Prüflinge antworten nicht immer mit einer reinen Zahl. Ein Multimeter antwortet vielleicht `+1.2001555E+01` (in Ordnung), ein Prüfling sendet aber womöglich `ID:42;OK\r\n`, einen binären Rahmen oder ein Statusregister. Die **Antwortverarbeitung** macht aus solchen Rohantworten die eine Zahl, die der Schritt bewertet.

Der Bereich **Antwortverarbeitung** steht für die Auswertungsquellen **Skript**, **Gerätebefehl**, **Schnittstellenbefehl**, **Datei** und **Benutzereingabe** zur Verfügung.

## Ohne benutzerdefinierte Verarbeitung

Ist **Benutzerdefinierte Antwortverarbeitung aktivieren** ausgeschaltet, wird die Rohantwort als ASCII-Text interpretiert und in eine Zahl umgewandelt. Führende und nachgestellte Leerzeichen (einschließlich Zeilenende) werden ignoriert. Alles andere, was keine Zahl ist, führt zu einem Ausführungsfehler.

## Mit benutzerdefinierter Verarbeitung

Schalten Sie **Benutzerdefinierte Antwortverarbeitung aktivieren** ein und füllen Sie aus:

| Feld | Beschreibung |
|------|--------------|
| **Überspringen** | Anzahl Bytes, die am Anfang der Antwort verworfen werden. |
| **Länge** | Anzahl Bytes, die nach dem Überspringen behalten werden (`0` = alle restlichen). |
| **Maske** | Regeln, die die verbleibenden Bytes erfüllen müssen, und/oder wie sie in eine Zahl umgewandelt werden. |
| **Anzeigemodus** | `ASCII` oder `HEX` – wie die empfangenen Daten im Bereich angezeigt werden. |

Verarbeitungsreihenfolge:

1. **Überspringen** und **Länge** schneiden den relevanten Teil der Antwort heraus.
2. Die **Prüfregeln** der Maske werden auf diesen Teil angewendet.
3. Sind alle Prüfregeln erfüllt, wandelt die **Zahlenextraktion** (falls vorhanden) diesen Teil in eine Zahl um.

### Empfangene Sequenz

Nach der Ausführung des Schritts zeigt der Bereich **Empfangene Sequenz**:

- **Original:** die vollständige Rohantwort,
- **Verarbeitet:** den nach Überspringen/Länge verbleibenden Teil,
- **Ergebnis:** den an die Grenzwertbewertung übergebenen Wert.

Führen Sie den Schritt beim Entwerfen einer Maske mit **Einzelschritt ausführen** (`F3`) aus, um diese Werte sofort zu sehen. Schalten Sie für binäre Protokolle den **Anzeigemodus** auf `HEX`.

## Maskensyntax

Eine Maske ist eine Folge von Elementen, getrennt durch Leerzeichen.

### Prüfregeln

| Element | Trifft zu, wenn | Beispiel |
|---------|-----------------|----------|
| `"Text"` | der ASCII-Text in den Daten vorkommt | `"OK"` |
| `0x…` | die Bytefolge in den Daten vorkommt | `0xAA55` |

Alle Prüfregeln müssen gefunden werden. Textregeln werden **in der angegebenen Reihenfolge** gesucht, z. B. verlangt `"ID:" ";OK"` ein `ID:`, auf das später `;OK` folgt; dasselbe gilt für Bytefolgen untereinander. (Text- und Byteregeln werden getrennt verfolgt; die Reihenfolge zwischen einer Text- und einer Byteregel wird daher nicht geprüft.)

### Zahlenextraktion

| Element | Wandelt die verarbeiteten Bytes um als … | Beispieldaten → Ergebnis |
|---------|-------------------------------------------|--------------------------|
| `{float}` | ASCII-Dezimalzahl mit optionalen Nachkommastellen/Exponent | `3.274` → 3,274 |
| `{dec}` | ASCII-Ganzzahl | `42` → 42 |
| `{hex}` | ASCII-Hexadezimalziffern | `1F` → 31 |
| `{bin}` | ASCII-Binärziffern | `1010` → 10 |
| `{ascii}` | ASCII-Zeichen, zu einer Ganzzahl zusammengesetzt (Big Endian) | `AB` → 16706 |
| `{byte:N}` | die ersten *N* Rohbytes als vorzeichenlose Ganzzahl, höchstwertiges Byte zuerst | `0x01 0x2C` mit `{byte:2}` → 300 |
| `{byte:N,lsb}` | die ersten *N* Rohbytes, niederwertigstes Byte zuerst | `0x2C 0x01` mit `{byte:2,lsb}` → 300 |
| `{byte:N,msb}` | wie `{byte:N}` | |

Pro Maske wird nur eine Zahlenextraktion verwendet. Zahlen in der Antwort müssen einen Punkt als Dezimaltrennzeichen verwenden. **Die Extraktion arbeitet immer auf dem gesamten verarbeiteten Teil** (nach Überspringen/Länge), nicht auf dem Text hinter einer Prüfregel – isolieren Sie die Zahl mit Überspringen und Länge.

#### Prüfregeln prüfen, Überspringen/Länge schneiden

Prüfregeln und Zahlenextraktion erledigen zwei unabhängige Aufgaben: Die Prüfregeln entscheiden nur, *ob* die Daten akzeptabel sind; sie verschieben nicht den Startpunkt der Extraktion. Beispiel – ein Prüfling antwortet `Result=3.27\r\n`:

| Maske | Überspringen | Länge | Was passiert | Ergebnis |
|-------|--------------|-------|--------------|----------|
| `"Result=" {float}` | 0 | 0 | `Result=` wird gefunden ✔, dann versucht `{float}` den ganzen Text `Result=3.27\r\n` zu lesen ✘ | `0` |
| `{float}` | 7 | 0 | Überspringen entfernt `Result=`, `{float}` liest `3.27\r\n` ✔ | `3.27` |
| `"3."` | 7 | 0 | Überspringen entfernt `Result=`, `3.` wird gefunden ✔, keine Extraktion | `1` |

Hat eine Zahl also ein Präfix, entfernen Sie das Präfix mit **Überspringen** (und ein Suffix mit **Länge**), statt es zu prüfen. Das funktioniert, solange die Zahl an einer festen Position beginnt – ihre eigene Länge darf variieren (`Result=3.27` und `Result=12.5` funktionieren beide mit Überspringen `7`, Länge `0`). Variiert die Länge des Präfixes selbst (z. B. `CH1:3.27` vs. `CH12:3.27`), lassen Sie den Prüfling bzw. das Gerät ein Format mit fester Breite senden, oder lassen Sie ein kleines Skript (Shell-Befehl-Schritt) den reinen Wert in eine Datei schreiben und werten diese mit einem Datei-Schritt aus.

### Ergebnisregeln

| Inhalt der Maske | Daten erfüllen alle Regeln | Daten erfüllen sie nicht |
|------------------|----------------------------|--------------------------|
| Nur Prüfregeln | `1` | `0` |
| Nur Zahlenextraktion | die extrahierte Zahl | – |
| Prüfregeln + Extraktion | die extrahierte Zahl | `0` |
| *(leere Maske)* | `0` | `0` |
| Extraktion schlägt fehl (z. B. `{dec}` auf `4.2`) | `0` | |

Bleiben nach Überspringen/Länge überhaupt keine Daten übrig, ist das Ergebnis leer, und der Schritt meldet einen Ausführungsfehler.

## Beispiele

**1. Auf Quittung prüfen**

Der Prüfling antwortet `ACK\r\n` oder `NAK\r\n`.

| Feld | Wert |
|------|------|
| Maske | `"ACK"` |
| Grenzwerte | `1 … 1` |

**2. Wert an fester Position**

Der Prüfling antwortet `T=023.5C\r\n`.

| Feld | Wert |
|------|------|
| Überspringen | `2` |
| Länge | `5` |
| Maske | `{float}` |
| Ergebnis | `23.5` |

**3. 16-Bit-Register über I²C**

Schnittstellenbefehl, I²C, **Bytes lesen** `2`, der Sensor sendet `0x0C 0xE4`.

| Feld | Wert |
|------|------|
| Maske | `{byte:2}` |
| Ergebnis | `3300` |

Skalieren Sie den Wert mit einem folgenden Ausdruck-Schritt, z. B. `{Raw} * 0.001` (mit *Ergebnis einer Variable zuweisen* = `Raw` im ersten Schritt).

**4. Binärer Rahmen mit Kopf**

Antwort `0xAA 0x55 0x03 0xE8`: zwei Kopfbytes, dann ein Big-Endian-Wert.

| Feld | Wert |
|------|------|
| Überspringen | `0` |
| Maske | `0xAA55` |

prüft den Kopf → Ergebnis `1`. Um stattdessen den Wert zu lesen, verwenden Sie Überspringen `2`, Maske `{byte:2}` → `1000`.

**5. Statusbit**

Ein Registerlesevorgang liefert ein Byte; Bit 3 muss gesetzt sein. Lesen Sie es mit `{byte:1}`, weisen Sie es der Variable `Status` zu und verwenden Sie dann einen Ausdruck-Schritt `({Status} >> 3) & 1` mit Grenzwerten `1 … 1`.
