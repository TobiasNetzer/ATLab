# 10. Antwortverarbeitung

Messgeräte und Prüflinge antworten nicht immer mit einer reinen Zahl. Ein Multimeter antwortet vielleicht `+1.2001555E+01` (in Ordnung), ein Prüfling sendet aber womöglich `ID:42;OK\r\n`, einen binären Rahmen oder ein Statusregister. Die **Antwortverarbeitung** macht aus solchen Rohantworten die eine Zahl, die der Schritt bewertet.

Der Bereich **Antwortverarbeitung** steht für die Auswertungsquellen **Skript**, **Gerätebefehl**, **Schnittstellenbefehl**, **Shell-Befehl** (mit **Ausgabe auswerten**), **Datei** und **Benutzereingabe** zur Verfügung.

## Ohne benutzerdefinierte Verarbeitung

Ist **Benutzerdefinierte Antwortverarbeitung aktivieren** ausgeschaltet, wird die Rohantwort als ASCII-Text interpretiert und in eine Zahl umgewandelt. Führende und nachgestellte Leerzeichen (einschließlich Zeilenende) werden ignoriert. Alles andere, was keine Zahl ist, führt zu einem Ausführungsfehler.

## Mit benutzerdefinierter Verarbeitung

Schalten Sie **Benutzerdefinierte Antwortverarbeitung aktivieren** ein und füllen Sie aus:

| Feld | Beschreibung |
|------|--------------|
| **Überspringen** | Anzahl Bytes, die am Anfang der Antwort verworfen werden. |
| **Länge** | Anzahl Bytes, die nach dem Überspringen behalten werden (`0` = alle restlichen). Sind weniger Bytes übrig, meldet der Schritt einen Ausführungsfehler. |
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
| `{float}` | ASCII-Dezimalzahl mit optionalen Nachkommastellen/Exponent; `.` oder `,` als Dezimaltrennzeichen, keine Tausendertrennzeichen | `3.274` → 3,274, `3,274` → 3,274 |
| `{dec}` | ASCII-Ganzzahl | `42` → 42 |
| `{hex}` | ASCII-Hexadezimalziffern (bis zu 16) | `1F` → 31 |
| `{bin}` | ASCII-Binärziffern (bis zu 64) | `1010` → 10 |
| `{byte:N}` | die ersten *N* Rohbytes (1–8) als vorzeichenlose Ganzzahl, Big-Endian (höchstwertiges Byte zuerst) | `0x01 0x2C` mit `{byte:2}` → 300 |
| `{byte:N,le}` | die ersten *N* Rohbytes, Little-Endian (niederwertigstes Byte zuerst) | `0x2C 0x01` mit `{byte:2,le}` → 300 |
| `{byte:N,be}` | wie `{byte:N}` | |
| `{byte:N,signed}` | die ersten *N* Rohbytes als vorzeichenbehaftete Ganzzahl (Zweierkomplement); kombinierbar mit `le` | `0xFF 0xFE` mit `{byte:2,signed}` → -2, `0xFE 0xFF` mit `{byte:2,le,signed}` → -2 |
| `{f32}` | die ersten 4 Rohbytes als 32-Bit-Gleitkommazahl (IEEE 754), Big-Endian | `0x40 0x53 0x33 0x33` → 3,3 |
| `{f32:le}` | wie `{f32}`, Little-Endian | `0x33 0x33 0x53 0x40` → 3,3 |

Pro Maske wird nur eine Zahlenextraktion verwendet. Wo sie liest, hängt von ihrer Position in der Maske ab:

- **Extraktion zuerst** (keine Prüfregel davor, z. B. `{float}` oder `{dec} "OK"`): Die Extraktion arbeitet auf dem **gesamten verarbeiteten Teil** (nach Überspringen/Länge).
- **Extraktion nach einer Prüfregel** (z. B. `"U3=" {float}`): Die Extraktion beginnt **direkt hinter dem Text bzw. der Bytefolge**, die die letzte Prüfregel davor gefunden hat.
  - Textextraktionen (`{float}`, `{dec}`, `{hex}`, `{bin}`) lesen nur die Zahl an dieser Stelle; führende Leerzeichen werden übersprungen, alles nach der Zahl wird ignoriert. `{hex}` akzeptiert zusätzlich ein Präfix `0x`.
  - Binäre Extraktionen (`{byte:N}`, `{f32}`) lesen die Bytes direkt hinter der Fundstelle.

#### Zahl hinter einer Bezeichnung lesen

Beispiel – ein Prüfling antwortet `Result=3.27\r\n`:

| Maske | Überspringen | Länge | Was passiert | Ergebnis |
|-------|--------------|-------|--------------|----------|
| `"Result=" {float}` | 0 | 0 | `Result=` wird gefunden ✔, `{float}` liest `3.27` direkt dahinter ✔ | `3.27` |
| `{float}` | 7 | 0 | Überspringen entfernt `Result=`, `{float}` liest `3.27\r\n` ✔ | `3.27` |
| `{float}` | 0 | 0 | `{float}` versucht den ganzen Text `Result=3.27\r\n` zu lesen ✘ | Ausführungsfehler |
| `"3."` | 7 | 0 | Überspringen entfernt `Result=`, `3.` wird gefunden ✔, keine Extraktion | `1` |

Anders als Überspringen funktioniert eine Bezeichnung auch, wenn die Position der Zahl variiert, z. B. `CH1:3.27` vs. `CH12:3.27`, oder wenn eine Antwort mehrere Werte enthält. Ein Werkzeug, das

```text
U1=3.271
U2=3.305
U3=12.48
```

ausgibt, lässt sich mit einem Schritt pro Wert auswerten: `"U1=" {float}` → 3,271, `"U2=" {float}` → 3,305, `"U3=" {float}` → 12,48. Der Text wird ab dem Anfang des verarbeiteten Teils gesucht, die erste Fundstelle gilt – wählen Sie die Bezeichnung daher eindeutig, z. B. `"U1="` statt `"U1"`, das auch in `U10` gefunden würde. Wird die Bezeichnung nicht gefunden, ist das Ergebnis `0` (siehe unten).

Prüfregeln hinter der Extraktion (z. B. `"ID:" {dec} ";OK"`) werden weiterhin geprüft; fehlt eine davon, ist das Ergebnis `0`.

### Ergebnisregeln

| Inhalt der Maske | Daten erfüllen alle Regeln | Daten erfüllen sie nicht |
|------------------|----------------------------|--------------------------|
| Nur Prüfregeln | `1` | `0` |
| Nur Zahlenextraktion | die extrahierte Zahl | – |
| Prüfregeln + Extraktion | die extrahierte Zahl | `0` |
| *(leere Maske)* | die Zahl, gelesen wie mit `{float}` (z. B. `3` → 3, `3.27` → 3,27) | – |

Der Schritt meldet einen **Ausführungsfehler** (statt eines Ergebnisses), wenn

- die Antwort kürzer ist als **Überspringen** + **Länge** (bei Länge > 0), z. B. bei einem abgeschnittenen Frame,
- nach dem **Überspringen** keine Daten übrig bleiben oder die Antwort leer ist,
- die Zahlenextraktion fehlschlägt, z. B. `{dec}` auf `4.2` oder `{byte:2}` auf ein einzelnes Byte,
- die Maske ungültig ist, z. B. eine unbekannte Extraktion wie `{foo}`.

Die Fehlermeldung nennt die Ursache, z. B. *Response too short: expected 5 bytes after skipping 2, but only 3 available*.

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
