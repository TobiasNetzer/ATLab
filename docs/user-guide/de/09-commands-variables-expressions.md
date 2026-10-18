# 9. Befehle, Variablen & Ausdrücke

Befehlstexte in ATLab sind mehr als einfache Zeichenketten. In geschweiften Klammern können Sie Variablen einfügen, Binärdaten kodieren und Werte berechnen. Dieses Kapitel beschreibt die Syntax.

## Wo die Syntax gilt

| Ort | Variablen `{Name}` | Kodierungsblöcke `{enc:…}` | Ausdrücke `{expr:…}` |
|-----|:---:|:---:|:---:|
| Skriptbefehle | ✔ (Skript + Laufzeit) | ✔ | ✔ |
| Gerätebefehl | ✔ | ✔ | ✔ |
| Schnittstellenbefehl | ✔ | ✔ | ✔ |
| Shell-Befehl | ✔ | nur Textkodierungen | ✔ |
| Kommentar / Meldungstext | ✔ | nur Textkodierungen | ✔ |
| Unter-/Soll-/Obergrenze, Verzögerung | ✔ | – | ✔ |
| Auswertungsquelle Ausdruck | ✔ | – | das ganze Feld ist ein Ausdruck |

## Variablen

Schreiben Sie einen Variablennamen in geschweifte Klammern:

```text
SOUR:VOLT {V_Supply}
```

Drei Arten von Variablen sind verfügbar:

1. **Skriptvariablen** (nur in Skripten) – im Skript definiert, Wert pro Schritt festgelegt.
2. **Laufzeitvariablen** – auf der Registerkarte Konfiguration definiert, überall verfügbar.
3. **Systemvariablen** – werden von ATLab während der Testausführung bereitgestellt, siehe unten.

Skriptvariablen werden zuerst ersetzt. Namen dürfen Buchstaben, Ziffern und `_` enthalten. Ein Verweis auf eine unbekannte Variable wird durch `0.0` ersetzt – prüfen Sie die Schreibweise daher sorgfältig.

### Systemvariablen

Systemvariablen beginnen mit `sys.` und sind schreibgeschützt. Sie können überall verwendet werden, wo auch Laufzeitvariablen erlaubt sind, lassen sich aber nicht unter *Ergebnis einer Variable zuweisen* auswählen.

| Variable | Wert |
|----------|------|
| `{sys.SerialNumber}` | Beim Teststart eingegebene Seriennummer (`-`, falls keine) |
| `{sys.User}` | Bediener (angemeldeter Benutzername) |
| `{sys.ProjectName}` | Name des Projekts |
| `{sys.Year}` | Aktuelles Jahr, vierstellig, z. B. `2026` |
| `{sys.Month}` | Aktueller Monat, zweistellig, `01`–`12` |
| `{sys.Day}` | Aktueller Tag, zweistellig, `01`–`31` |
| `{sys.Hour}` | Aktuelle Stunde, zweistellig, `00`–`23` |
| `{sys.Minute}` | Aktuelle Minute, zweistellig, `00`–`59` |
| `{sys.Second}` | Aktuelle Sekunde, zweistellig, `00`–`59` |
| `{sys.ElapsedTime}` | Sekunden seit Teststart, z. B. `12.34` |
| `{sys.ModuleName}` | Name des Moduls des aktuellen Schritts |
| `{sys.StepNumber}` | Nummer des aktuellen Schritts innerhalb seines Moduls |
| `{sys.FailedSteps}` | Anzahl der bisher fehlgeschlagenen Schritte |

Die Werte werden zu Beginn und am Ende jedes Schritts aktualisiert. `{sys.FailedSteps}` enthält den aktuellen Schritt daher noch nicht, solange er läuft (z. B. in seinem Pass/Fail-Kommentar).

Datums- und Zeitteile lassen sich zu jedem gewünschten Format kombinieren:

```text
{sys.Day}.{sys.Month}.{sys.Year} {sys.Hour}:{sys.Minute}
```

Beispiel – Seriennummer per UART an den Prüfling senden: `SN={sys.SerialNumber}{hex:0A}`

## Kodierungsblöcke

Ein Kodierungsblock hat die Form `{kodierung:inhalt}`. Der Inhalt darf selbst Variablen enthalten.

| Block | Ergebnis | Beispiel | Gesendet / erzeugt |
|-------|----------|----------|--------------------|
| `{hex:…}` | Rohbytes aus Hexadezimal. Leerzeichen, `-` und `0x` werden ignoriert. | `{hex:0A 1F}` | Bytes `0x0A 0x1F` |
| `{bin:…}` | Rohbytes aus Binärziffern (auf ganze Bytes aufgefüllt). | `{bin:10100101}` | Byte `0xA5` |
| `{ascii:…}` | Text unverändert. | `{ascii:ABC}` | `ABC` |
| `{upper:…}` | Text in Großbuchstaben. | `{upper:{Mode}}` | `DC` |
| `{lower:…}` | Text in Kleinbuchstaben. | `{lower:VOLT}` | `volt` |
| `{len:…}` | Länge des Texts. | `{len:HELLO}` | `5` |
| `{bytes:…}` | UTF-8-Bytewerte als Liste. | `{bytes:AB}` | `[65, 66]` |
| `{expr:…}` | Ergebnis eines arithmetischen Ausdrucks. | `{expr:{Range}*2}` | `200` |

Text außerhalb von Klammern wird als ASCII gesendet. Blöcke lassen sich beliebig kombinieren:

```text
WRITE{hex:00 FF}{Value}
```

Typische Anwendungen:

- Zeilenvorschub für ein Gerät anhängen, das ihn benötigt: `MEAS?{hex:0A}`
- Registeradresse und Wert über I²C senden: `{hex:10}{hex:{RegValue}}`

> Rohbyte-Blöcke (`hex`, `bin`) sind nur für Befehle an Geräte und Schnittstellen sinnvoll. In Shell-Befehlen, Kommentaren und Grenzwerten wird der Inhalt als Text eingefügt.

## Ausdrücke

Ein Ausdrucksblock `{expr:…}` – oder das ganze Feld eines **Ausdruck**-Schritts – wird arithmetisch ausgewertet.

Operatoren, von der stärksten zur schwächsten Bindung – in derselben Reihenfolge wie in C# (und C, Java, JavaScript):

| Operator | Bedeutung |
|----------|-----------|
| `( )` | Klammerung |
| `-x` `+x` | Vorzeichen (unäres Minus / Plus) |
| `*` `/` `%` | Multiplikation, Division, Rest |
| `+` `-` | Addition, Subtraktion |
| `<<` `>>` | Bitverschiebung links/rechts (Ganzzahlen) |
| `&` | Bitweises UND (Ganzzahlen) |
| `^` | Bitweises XOR (Ganzzahlen) |
| `\|` | Bitweises ODER (Ganzzahlen) |

Operatoren derselben Stufe werden von links nach rechts ausgewertet. Beispielsweise ergibt `1 + 2 << 1` den Wert `(1 + 2) << 1 = 6` und `1 | 2 * 3` den Wert `1 | (2 * 3) = 7`. Klammern machen gemischte Bit-/Rechenausdrücke trotzdem leichter lesbar.

Zahlen können dezimal (`3.3`), wissenschaftlich (`1.5e-3`) oder hexadezimal (`0x3F`) geschrieben werden. Sowohl `.` als auch das Dezimaltrennzeichen der eingestellten Sprache (bei Deutsch `,`) werden akzeptiert.

Negative Zahlen funktionieren wie erwartet: `-5`, `2 * -3`, `-(1 + 2)` und `1 - -0.3` sind alle gültig. Das gilt auch für Variablen mit negativen Werten – enthält `{Offset}` den Wert `-0.3`, wird `1 + {Offset}` zu `1 + -0.3 = 0.7`.

Beispiele:

```text
{expr:{V_Out} / {V_In} * 100}       → Wirkungsgrad in %
{expr:({Raw} >> 4) & 0x0F}          → oberes Nibble eines Registers
{expr:{Count} + 1}                  → Inkrement (Ergebnis Count zuweisen)
{expr:{V_Meas} - {Offset}}          → Offset-Korrektur, auch mit negativem Offset
```

Lässt sich ein Ausdruck in einem Befehl nicht auswerten, bleibt der Block unverändert. In einem **Ausdruck**-Schritt ist ein ungültiger Ausdruck ein Ausführungsfehler.

## Einheiten und SI-Präfixe

Überall dort, wo ATLab eine von Ihnen eingegebene Zahl einliest (Grenzwerte, Sollwert, Verzögerung), werden folgende Suffixe akzeptiert:

| Suffix | Faktor |
|--------|--------|
| `p` | 10⁻¹² |
| `n` | 10⁻⁹ |
| `u` | 10⁻⁶ |
| `m` | 10⁻³ |
| `k` | 10³ |
| `M` | 10⁶ |

Die Einheit des Schritts darf der Zahl folgen (`4.7kOhm` bei Einheit `Ohm`). Bei der Verzögerung ist die Einheit immer Sekunden (`250ms`).

Messwerte werden mit dem passendsten Präfix angezeigt. Setzen Sie die Einheit in geschweifte Klammern (z. B. `{rpm}`), um Zahlen ohne Präfix anzuzeigen.

## Zusammenspiel

Ein Skriptbefehl

```text
CONF:VOLT:DC {Range},{expr:{Range}/1000}
```

mit der Skriptvariable `Range = 10` wird gesendet als:

```text
CONF:VOLT:DC 10,0.01
```

Ein Kommentar

```text
Gemessene Versorgung: {V_Supply} V. Bitte bestätigen, dass das Display denselben Wert zeigt.
```

wird dem Bediener mit dem aktuellen Wert von `V_Supply` angezeigt.
