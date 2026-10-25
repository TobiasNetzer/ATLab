# 12. Registerkarte Boundary Scan

**Boundary Scan** (IEEE 1149.1, auch JTAG genannt) ermöglicht den Zugriff auf die Pins von Bausteinen einer Baugruppe, ohne sie mit einer Prüfspitze zu berühren. Jeder Pin eines Boundary-Scan-fähigen Bausteins hat eine Zelle im *Boundary-Register* des Bausteins. Über eine JTAG-Probe kann ATLab die Pegel dieser Pins lesen und die Pins treiben, während die interne Logik des Bausteins von ihnen getrennt ist. Typische Anwendungen:

- Verbindungen zwischen Bausteinen (Interconnect-Test) und zu Steckverbindern, LEDs, Tastern oder Jumpern prüfen,
- Bestückungsoptionen oder DIP-Schalter auslesen,
- mit I²C- oder SPI-Bausteinen (EEPROMs, Sensoren, Flash-Speicher) kommunizieren, deren Busleitungen an einem Boundary-Scan-Baustein hängen,
- eine Baugruppe untersuchen, die nicht startet.

Auf der Registerkarte **Boundary Scan** wird die Scan-Kette eingerichtet und manuell mit den Pins gearbeitet. Testschritte nutzen diese Einrichtung über die Auswertungsquellen [Boundary Scan](09-evaluation-sources.md#boundary-scan) und [Boundary-Scan-Bus](09-evaluation-sources.md#boundary-scan-bus).

Boundary Scan verwendet nicht den Test Interface Adapter, sondern eine eigene JTAG-Probe am JTAG-Anschluss der Baugruppe.

## Voraussetzungen

### Unterstützte Probes

| Probe | Treiber / Bibliothek | Hinweise |
|-------|----------------------|----------|
| **SEGGER J-Link** | *J-Link Software and Documentation Pack* von SEGGER (`JLink_x64.dll` unter Windows, `libjlinkarm.so` unter Linux) | Alle J-Link-Modelle mit JTAG-Unterstützung. |
| **FTDI MPSSE** (FT2232H, FT232H, FT4232H) | FTDI-*D2XX*-Treiber (`ftd2xx.dll` unter Windows, `libftd2xx.so` unter Linux) | Belegung an Kanal A: ADBUS0 = TCK, ADBUS1 = TDI, ADBUS2 = TDO, ADBUS3 = TMS. |

ATLab sucht die Bibliothek der Probe in dieser Reihenfolge:

1. im **Programmordner von ATLab**,
2. im Suchpfad des Betriebssystems (`PATH`, `System32`, Bibliothekscache),
3. in den Standard-Installationsordnern des Herstellers, z. B. `C:\Program Files\SEGGER\JLink…` (neueste Version zuerst).

Wird die Bibliothek nicht automatisch gefunden, kopieren Sie sie in den Programmordner von ATLab. Beim nächsten Versuch wird erneut gesucht, ATLab muss also nicht neu gestartet werden.

### BSDL-Dateien

Zum Lesen und Treiben von Pins benötigt ATLab die **BSDL-Datei** (Boundary Scan Description Language, `.bsd` oder `.bsdl`) jedes Bausteins. Sie beschreibt das Befehlsregister, die Zellen des Boundary-Registers und die Pinbelegung des Gehäuses. BSDL-Dateien stellt der Hersteller des Bausteins bereit, meist auf der Produktseite oder im Supportbereich seiner Website. Achten Sie darauf, dass die Datei genau zum Baustein und Gehäuse passt.

## Überblick

Die Registerkarte ist in drei Bereiche gegliedert:

- **JTAG-Probe** (links oben) – Auswahl und Verbindung der Probe,
- **Scan-Kette** (links unten) – die Bausteine der Kette und ihre Einstellungen,
- der rechte Bereich zeigt entweder die **Pins** des ausgewählten Bausteins oder die **Busse**. Umgeschaltet wird mit den beiden Schaltflächen in seiner Kopfzeile.

Solange ein Testlauf die Kette steuert, ist die Registerkarte gesperrt und ein blauer Hinweis wird angezeigt (siehe [Boundary Scan während der Testausführung](#boundary-scan-während-der-testausführung)).

## Probe verbinden

| Feld | Beschreibung |
|------|--------------|
| **Probe-Typ** | `SEGGER J-Link` oder `FTDI MPSSE (FT2232H / FT232H / FT4232H)`. |
| **Seriennummer** | Seriennummer der zu verwendenden Probe. Leer lassen, um die erste verfügbare Probe zu verwenden. Die Suchen-Schaltfläche listet die angeschlossenen Probes auf; ein Eintrag aus der Auswahlliste neben dem Feld übernimmt die Seriennummer. FTDI-Kanäle erscheinen einzeln, z. B. `…A` und `…B`; JTAG verwendet normalerweise Kanal A. |
| **TCK-Frequenz (kHz)** | JTAG-Taktfrequenz, Standard 1000 kHz. Verringern, wenn die Kettenerkennung bei langen Kabeln oder langsamen Bausteinen fehlschlägt. |
| **ADBUS-Wert (hex)** / **ADBUS-Richtung (hex)** | Nur FTDI. Startpegel und Richtung von ADBUS0–7. TCK, TDI, TDO und TMS sind fest vorgegeben; mit den übrigen Pins lassen sich Treiber oder Pegelwandler des Adapters freigeben. Standard `08` / `0B`. |
| **Verbinden** / **Trennen** | Öffnet oder schließt die Probe. Nach dem Verbinden wird die Kette automatisch gescannt und mit der eingerichteten Kette verglichen (siehe [Scan-Kette](#scan-kette)). |

Die Einstellungen der Probe werden im Projekt gespeichert. **Trennen** versetzt alle Bausteine in den Normalbetrieb (Test-Logic-Reset), bevor die Probe geschlossen wird; dasselbe geschieht beim Beenden von ATLab.

## Scan-Kette

Die Liste zeigt die Bausteine in der Reihenfolge der Kette, von **TDI** (oben) nach **TDO** (unten). Jeder Eintrag zeigt:

- die Position in der Kette und den Namen (aus **Gerätename** oder, wenn kein Name vergeben ist, den Bausteintyp),
- den aus dem Baustein gelesenen IDCODE, grün wenn er zur BSDL-Datei passt, rot wenn nicht,
- den aktuell geladenen Befehl (`BYPASS`, `SAMPLE`, `EXTEST`), solange eine Verbindung besteht,
- *Keine BSDL*, solange noch keine BSDL-Datei zugeordnet ist.

| Schaltfläche | Funktion |
|--------------|----------|
| **Bausteine in der Scan-Kette erkennen** | Scannt die Kette erneut: Anzahl der Bausteine, Gesamtlänge der Befehlsregister und IDCODEs, und vergleicht sie mit der eingerichteten Kette. |
| **Erkannte Kette übernehmen** | Ersetzt die eingerichtete Kette nach einer Rückfrage durch die auf der Baugruppe gefundenen Bausteine. Wiedererkannte Bausteine behalten ihre Einstellungen; alle anderen werden mit BSDL-Datei, Gehäuse und Bezeichnungen entfernt. |
| **Baustein hinzufügen** / **Ausgewählten Baustein entfernen** | Bearbeitet die Kette von Hand, z. B. um ein Projekt ohne Hardware vorzubereiten. Nur ohne Verbindung möglich. |

Das Scannen **verändert eine eingerichtete Kette nie**. Ist eine andere Baugruppe angeschlossen, fehlt ein Baustein oder ist die Kette unterbrochen, bleibt Ihre Einrichtung mit BSDL-Dateien und Bezeichnungen erhalten. Nur eine leere Konfiguration übernimmt die erkannte Kette automatisch, was das Einrichten eines neuen Projekts erleichtert. Um eine bestehende Konfiguration aus der Baugruppe neu aufzubauen, verwenden Sie **Erkannte Kette übernehmen**.

Ein Baustein gilt als wiedererkannt, wenn sein IDCODE zu seiner BSDL-Datei passt. Die BSDL-Datei maskiert in der Regel die Versionsbits, sodass auch andere Silizium-Revisionen desselben Bausteins passen. Bausteine ohne BSDL-Datei müssen den gespeicherten IDCODE haben.

Bei bestehender Verbindung zeigt die Kettenliste die von der Baugruppe gelesenen IDCODEs. Die Statuszeile über der Liste zeigt `<n> device(s), instruction register <bits> bits`, wenn die Baugruppe zur Konfiguration passt, sonst jede Abweichung, z. B. *Found 2 device(s), the project expects 3.*, *Device 2 (U3): IDCODE 0x… does not match the BSDL file of …* oder *Device 1 (U1): instruction length unknown*. Befehle können erst geladen werden, wenn der Status grün ist.

### Baustein-Einstellungen

| Feld | Beschreibung |
|------|--------------|
| **Gerätename** | Bestückungsbezeichnung oder Name, z. B. `U3`. Wird in der Kettenliste, in Testschritten und in Ergebnismeldungen verwendet. |
| **BSDL-Datei** | Ordner-Schaltfläche: BSDL-Datei importieren. ×-Schaltfläche: entfernen. Die Datei wird sofort eingelesen; Warnungen (z. B. fehlende Befehle) erscheinen im Ereignisprotokoll. BSDL-Dateien im Projektordner werden mit relativem Pfad gespeichert. |
| **Befehlslänge** | Wird aus der BSDL-Datei übernommen. Für Bausteine ohne BSDL-Datei muss sie von Hand eingetragen werden. Ist nur bei einem Baustein der Kette die Länge unbekannt, berechnet ATLab sie aus der Gesamtlänge. |
| **Gehäuse** | Gehäusevariante aus der BSDL-Datei (z. B. `TQFP144`, `BGA256`). Bestimmt die Pinnamen und die Gehäusedarstellung. |

Bausteine ohne BSDL-Datei können in der Kette bleiben; sie bleiben immer in BYPASS.

## Ansicht Pins

Die Ansicht **Pins** zeigt den ausgewählten Baustein: **Bausteintyp** (der Entity-Name aus der BSDL-Datei, z. B. `STM32F407_LQFP100`), **IDCODE**, **Befehlslänge** und **Boundary-Register** (Anzahl der Zellen).

### Befehle

| Schaltfläche | Wirkung |
|--------------|---------|
| **BYPASS** | Der Baustein wird überbrückt; seine Pins arbeiten normal. |
| **SAMPLE** | Die Pinpegel werden gelesen, ohne den Baustein zu beeinflussen. Geeignet, um Signale im normalen Betrieb der Baugruppe zu beobachten. |
| **EXTEST** | Die Pins werden vom Boundary-Register übernommen: Ausgänge werden aus ihm getrieben, Eingänge gelesen. Der Baustein verlässt den Normalbetrieb. |

Bevor EXTEST aktiv wird, lädt ATLab zuerst die gewünschten Ausgangswerte (PRELOAD), sodass die Ausgänge nie mit zufälligen Pegeln starten. Nach dem Laden einer BSDL-Datei stehen alle Zellen auf den **sicheren Werten** der BSDL-Datei; in der Regel sind damit alle Ausgänge abgeschaltet.

> **Vorsicht:** In EXTEST treiben die Ausgänge gegen alles, was sonst noch an dem Netz hängt. Stellen Sie vor dem Treiben eines Pins sicher, dass kein anderer Treiber auf der Baugruppe dagegen arbeitet, sonst können Bausteine beschädigt werden.

| Element | Funktion |
|---------|----------|
| Schaltfläche Automatisch aktualisieren | Liest die Pins fortlaufend (etwa zehnmal pro Sekunde), solange ein Baustein in SAMPLE oder EXTEST ist. Erneut klicken zum Beenden; die Schaltfläche ist hervorgehoben, solange sie aktiv ist. |
| Aktualisieren-Schaltfläche | Liest die Pins einmal. |
| Schild-Schaltfläche | **Alle Ausgänge auf die sicheren Werte der BSDL-Datei zurücksetzen.** |

### Gehäuseansicht

Die Zeichnung zeigt das Gehäuse mit dem aktuellen Zustand jedes Pins. Die Anordnung ergibt sich aus den Pinnamen: nummerierte Pins werden rund um ein Quad-Gehäuse (QFP, QFN) oder auf zwei Seiten (SOIC, TSOP, DIP) angeordnet, Namen aus Buchstabe und Zahl wie `A1` bilden ein BGA-Raster.

| Darstellung | Bedeutung |
|-------------|-----------|
| Orange | Pin liest High |
| Blau | Pin liest Low |
| Grau | Versorgungs- oder Linkage-Pin (ohne Boundary-Zelle) |
| Violett | JTAG-Pin (TCK, TMS, TDI, TDO, TRST) |
| Dicker weißer/schwarzer Rahmen | Ausgang wird über Boundary Scan getrieben (EXTEST) |
| Blauer Ring | Ausgewählter Pin |

Fährt man mit der Maus über einen Pin, erscheinen seine Details in der Zeile unter der Zeichnung, z. B. `Pin 8 · IO(3) · LED_RED · INOUT · In 0 · Out Z · Zellen I11 O10 C9`. Ein Klick wählt den Pin in der Tabelle aus; ein **Doppelklick** auf einen getriebenen Ausgang schaltet ihn zwischen 0 und 1 um.

### Pin-Tabelle

| Spalte | Inhalt |
|--------|--------|
| **Pin** | Pinname des Gehäuses. |
| **Port** | Portname aus der BSDL-Datei, z. B. `PA3` oder `IO(3)`. |
| **Bezeichnung** | Eigener Name für das Signal, z. B. der Netzname aus dem Schaltplan. Zum Bearbeiten die Zelle doppelklicken oder `F2` drücken. |
| **Typ** | `IN`, `OUT`, `INOUT`, `LINKAGE` oder `TAP`. |
| **In** | Vom Pin gelesener Pegel (`0`/`1`), leer solange der Baustein in BYPASS ist. |
| **Out** | Schaltflächen `0`, `1` und `Z` zum Treiben des Pins; nur in EXTEST aktiv. `Z` wird nur angeboten, wenn sich der Ausgang abschalten lässt. |

- Ein Klick auf eine Spaltenüberschrift sortiert; Pins und Ports werden natürlich sortiert (`2` vor `10`, `IO(2)` vor `IO(10)`).
- Das Filterfeld über der Tabelle durchsucht Pin, Port und Bezeichnung.
- **Linkage-Pins anzeigen** blendet Versorgungs- und andere Pins ohne Boundary-Zellen aus.
- Fährt man mit der Maus über die Zelle **In** oder **Out**, werden die Boundary-Register-Zellen des Pins angezeigt (`I` = Eingang, `O` = Ausgang, `C` = Steuerung).

Viele Bausteine verwenden eine **Steuerzelle** für mehrere Ausgänge. Wird einer davon auf `Z` geschaltet, werden alle Ausgänge dieser Gruppe abgeschaltet; der Tooltip nennt die betroffenen Pins.

**Bezeichnungen** werden im Projekt pro Port gespeichert und bleiben deshalb bei einem Gehäusewechsel erhalten. Sie erscheinen in der Gehäuseansicht, in Testschritten und in Ergebnismeldungen.

## Ansicht Busse

In der Ansicht **Busse** werden Busse festgelegt, die über Boundary-Scan-Pins nachgebildet werden. Testschritte wählen einen Bus über die Auswertungsquelle [Boundary-Scan-Bus](09-evaluation-sources.md#boundary-scan-bus) aus. Die Registerkarte dient nur der Einrichtung der Busse; die Übertragungen führen Testschritte aus.

Busse werden mit **+** und **−** hinzugefügt und entfernt. Für den ausgewählten Bus:

| Feld | Beschreibung |
|------|--------------|
| **Name** | In Testschritten angezeigter Name, z. B. `EEPROM`. |
| **Typ** | `I²C`, `SPI` oder `Pin Group`. |
| **SPI-Modus** | Nur SPI: Modus 0–3 (CPOL/CPHA). |
| **CS high-aktiv** | Nur SPI: Chip-Select ist high-aktiv statt low-aktiv. |
| **LSB zuerst** | Nur SPI: Bits werden mit dem LSB zuerst übertragen. |
| **Anzahl Pins** | Nur Pin-Gruppe: Anzahl der Pins (1–32). |

Die Signaltabelle listet die Signale des Bustyps. Zum Zuordnen eines Bausteins und Pins die Zelle **Baustein** oder **Pin** doppelklicken; Bezeichnungen werden neben den Ports angezeigt. Die Signale dürfen auf mehrere Bausteine der Kette verteilt sein.

| Typ | Signale | Anforderungen |
|-----|---------|---------------|
| **I²C** | SCL, SDA | Beide benötigen einen auf `Z` schaltbaren Ausgang und einen Eingang. Auf der Baugruppe werden Pull-up-Widerstände benötigt. |
| **SPI** | SCK, MOSI, MISO, CS | SCK ist erforderlich, MOSI, MISO und CS sind optional, mindestens MOSI oder MISO wird jedoch benötigt. Ausgänge für SCK, MOSI und CS, ein Eingang für MISO. |
| **Pin Group** | Bit 0 … Bit n | Ausgänge zum Schreiben; Eingänge (und auf `Z` schaltbare Ausgänge) zum Lesen. |

Unter der Tabelle listet ATLab auf, was noch fehlt oder nicht möglich ist, z. B. *SDA has no pin assigned.* oder *SCL: U1 PB6 cannot be switched to Z.* Ist alles in Ordnung, erscheint *Der Bus kann in Testschritten verwendet werden.*

Eine **Pin-Gruppe** behandelt mehrere Pins als eine Zahl: Beim Schreiben wird der Wert auf Bit 0 … n getrieben, beim Lesen werden die Pins freigegeben und eingelesen. Geeignet für Bestückungsoptionen, DIP-Schalter oder LED-Ports. Sie hat keine Steuerleitungen und kann daher keine Speicher oder anderen parallelen Busse mit Strobe-Signalen ansprechen.

### Geschwindigkeit und Einschränkungen

Ein Bus wird nachgebildet, indem seine Pins über das Boundary-Register gesetzt werden; jeder Wechsel einer Busleitung kostet daher einen vollständigen Scan der Kette. Der Bustakt ergibt sich somit aus dem JTAG-Takt:

- **Ein Scan** dauert *DR-Länge + 5* TCK-Takte. Die DR-Länge ist die Summe der Boundary-Register-Längen aller Bausteine in EXTEST oder SAMPLE, plus 1 Bit je Baustein in BYPASS.
- **Ein Bustakt** braucht bei I²C 3 Scans (SDA setzen, SCL high, SCL low; Start, Stopp und ACK kommen hinzu), bei SPI mit CPHA 0 3 Scans und mit CPHA 1 2 Scans. Eine Pin-Gruppe braucht einen Scan je Schreib- und einen je Lesezugriff.

Beispiel: TCK 1000 kHz und ein Baustein mit 100 Zellen im Boundary-Register ergeben (100 + 5) µs je Scan, etwa 315 µs je I²C-Bit, also einen SCL-Takt von rund 3 kHz.

Alle Scans einer Übertragung werden in einem einzigen Transfer an die Probe geschickt; die USB-Latenz spielt daher kaum eine Rolle, und ein I²C-Registerzugriff dauert nur wenige Millisekunden. So wird ein Bus schneller:

- **TCK-Frequenz (kHz)** erhöhen; der Bustakt steigt im gleichen Verhältnis.
- Bausteine, die der Bus nicht nutzt, in BYPASS lassen. Ein Baustein, der aus einem früheren Schritt oder von dieser Registerkarte noch in EXTEST oder SAMPLE ist, verlängert jeden Scan um sein ganzes Boundary-Register. EXTEST bleibt bis zum Ende des Testlaufs aktiv.
- Wenn möglich, Pins eines Bausteins mit kurzem Boundary-Register verwenden.

I²C und SPI haben keine Mindesttaktrate; EEPROMs, Sensoren, Port-Expander und Flash-Speicher funktionieren damit.

- I²C-**Clock-Stretching** wird nicht unterstützt. Hält ein Baustein SCL low, schlägt der Schritt mit *SCL is held low* fehl.
- UART, 1-Wire und andere Protokolle mit festem Timing lassen sich nicht nachbilden.

## Boundary Scan während der Testausführung

Testschritte verwenden die auf dieser Registerkarte eingerichtete Kette:

- Die Probe wird beim **ersten** Boundary-Scan-Schritt eines Laufs verbunden, nicht beim Teststart. Anschließend wird die Kette mit dem Projekt verglichen (Anzahl der Bausteine, IDCODEs, Befehlslängen). Passt sie nicht, z. B. weil die falsche Baugruppe eingelegt ist, schlägt der Schritt mit derselben Meldung wie die Statuszeile dieser Registerkarte fehl, z. B. *Boundary scan chain: Found 2 device(s), the project expects 3.*
- Eine auf dieser Registerkarte geöffnete Verbindung wird vom Test übernommen. Die Registerkarte bleibt gesperrt, bis der Test die Kette freigibt.
- Pins, die ein Schritt nicht auflistet, behalten ihren Zustand aus den vorherigen Schritten. Ein Baustein, dessen Ausgänge getrieben wurden, bleibt in EXTEST, bis die Kette freigegeben wird.
- Die Kette wird freigegeben – alle Bausteine kehren in den Normalbetrieb zurück und die Probe wird geschlossen –, wenn der Lauf endet, wenn er gestoppt wird (`Esc`), bei einem Schritt im Modus **Boundary Scan ausschalten** und wenn ein anderes Projekt geöffnet wird.
- **Einzelschritt ausführen** (`F3`) behält die Kette nach dem Schritt unter Kontrolle des Tests, sodass mehrere Einzelschritte aufeinander aufbauen können. **Stopp** (`Esc`) gibt sie frei. Ein vollständiger Lauf beginnt immer mit den sicheren Werten.

Verwenden Sie **Boundary Scan ausschalten** vor Schritten, die die Bausteine im Normalbetrieb benötigen oder selbst auf die Probe zugreifen, z. B. das Programmieren des Bausteins mit einem externen Programm über denselben J-Link.
