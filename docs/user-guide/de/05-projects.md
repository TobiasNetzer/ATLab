# 5. Arbeiten mit Projekten

Ein **Projekt** wird in einer einzelnen **Projektdatei** mit der Endung `.atlab` gespeichert. Sie enthält alles, was zu einem Test gehört:

- die Testschritte mit ihrer vollständigen Konfiguration,
- die Geräte aus dem Geräte-Manager,
- Laufzeitvariablen und ihre Initialwerte,
- benutzerdefinierte Namen für Matrix-, Stimulations- und externe Stimulationskanäle,
- die Projekteinstellungen (Toleranz, Seriennummer, Exportoptionen …),
- die Dokumentation (Texte, Pfade zu Bildern und Anhängen),
- die Prüflingsdaten für den Testbericht,
- die Boundary-Scan-Einrichtung: Probe-Einstellungen, Scan-Kette, Pfade der BSDL-Dateien, Bezeichnungen und Busse,
- den Pfad des Skriptordners.

Die Skripte selbst sind **nicht** im Projekt gespeichert; sie liegen als eigene Dateien im Skriptordner (siehe [Skripte](07-scripting.md)).

## Projektbefehle

Die Projektbefehle sind die ersten vier Symbole der Werkzeugleiste auf der Registerkarte Testausführung.

| Schaltfläche | Kürzel | Aktion |
|--------------|--------|--------|
| **Neues Projekt** | `Strg+N` | Beginnt ein leeres, unbenanntes Projekt. |
| **Projekt öffnen** | `Strg+O` | Öffnet eine vorhandene `.atlab`-Projektdatei. |
| **Projekt speichern** | `Strg+S` | Speichert in die aktuelle Projektdatei. Bei einem unbenannten Projekt wie *Projekt speichern unter*. |
| **Projekt speichern unter** | `Strg+Umschalt+S` | Speichert unter neuem Namen. Vorschlag ist `Test.atlab`. |
| **Projekt schließen** | `Strg+W` | Schließt das Projekt und kehrt zum Willkommensbildschirm zurück. Bei ungespeicherten Änderungen wird vorher nachgefragt. Auch der nächste Start zeigt den Willkommensbildschirm. |

Während ein Test läuft, sind die Projektbefehle gesperrt.

## Ungespeicherte Änderungen

Jede Änderung markiert das Projekt als geändert und fügt der Titelleiste ein `*` hinzu. Wenn Sie bei ungespeicherten Änderungen ein neues Projekt anlegen, ein Projekt öffnen oder schließen oder ATLab beenden, fragt ein Dialog **„Das Projekt hat ungespeicherte Änderungen. Speichern?“**:

| Schaltfläche | Wirkung |
|--------------|---------|
| **Speichern** | Speichert das Projekt und fährt dann fort. Bei einem unbenannten Projekt wird zuerst nach einem Dateinamen gefragt; wird dieser Dialog abgebrochen, wird die ganze Aktion abgebrochen. |
| **Nicht speichern** | Verwirft die Änderungen und fährt fort. |
| **Abbrechen** | Kehrt zum Projekt zurück. |

Das Ausführen eines Tests markiert das Projekt nicht als geändert.

## Welches Projekt beim Start geöffnet wird

1. Wurde ATLab mit einem Dateipfad als erstem Argument gestartet (z. B. per Doppelklick auf eine `.atlab`-Datei unter Windows), wird diese Datei geöffnet.
2. Andernfalls wird das zuletzt geöffnete oder gespeicherte Projekt erneut geöffnet.
3. Gibt es keines davon oder lässt sich die Datei nicht öffnen, zeigt ATLab einen Willkommensbildschirm mit **Neues Projekt** und **Projekt öffnen**. Solange kein Projekt geöffnet ist, sind nur die Registerkarten *Testausführung* und *Info* sichtbar.

```bash
ATLab.exe "D:\Tests\PowerBoard.atlab"
```

Während eine Projektdatei gelesen wird, etwa von einer Netzwerkfreigabe, zeigt ein Dialog **Projekt wird geöffnet…** mit ihrem Dateinamen.

## Relative Pfade

Beim Speichern werden alle referenzierten Dateien **innerhalb des Projektordners (oder eines Unterordners)** mit relativem Pfad gespeichert. Das gilt für

- den Skriptordner,
- Bilder und Anhänge der Dokumentation,
- Bilder in Meldungsfenstern von Testschritten,
- Dateipfade von Schritten mit Auswertung **Datei**,
- BSDL-Dateien der Boundary-Scan-Kette,
- die KiCad-Platine und die importierte Nadeladapter-Datei (siehe [Platine und Netze](06-configuration.md#platine-und-netze)),
- die **Ausgabeordner** der Testergebnis-Exporte.

Pfade außerhalb des Projektordners werden absolut gespeichert.

Relative Pfade verwenden immer `/` als Trennzeichen, sodass ein unter Windows gespeichertes Projekt auch unter Linux geöffnet werden kann (und umgekehrt). Absolute Pfade sind nicht zwischen Betriebssystemen übertragbar – ein Pfad wie `D:\Results` kann unter Linux nicht aufgelöst werden.

**Empfehlung:** Legen Sie ein Projekt mit seinen Skripten, Bildern und Anhängen in einem gemeinsamen Ordner ab, zum Beispiel:

```
PowerBoard/
├── PowerBoard.atlab
├── Scripts/            ← Skriptordner
├── Images/
└── Attachments/
```

Der ganze Ordner kann dann auf einen anderen Prüfplatz kopiert oder in eine Versionsverwaltung eingecheckt werden und funktioniert weiterhin.

## Ziel-Test-Hardware

Jedes Projekt wird für eine bestimmte Art von Test-Hardware entwickelt, seine *Ziel-Test-Hardware*. Die Test-Hardware meldet beim Verbinden eine **Geräte-ID** und einen Gerätenamen; der Test Interface Adapter meldet `0xA101` und *compact Test Interface Adapter*. Ein neues Projekt übernimmt die Geräte-ID der angeschlossenen Test-Hardware, sie wird in der Projektdatei gespeichert.

Ein Projekt lässt sich nur öffnen, wenn die angeschlossene Test-Hardware dieselbe Geräte-ID meldet. Andernfalls verweigert ATLab das Öffnen mit der Meldung *„Das Projekt '…' wurde für die Test-Hardware … entwickelt, angeschlossen ist aber ….“*, damit ein Projekt nicht auf Hardware läuft, für die es nicht gemacht wurde.

Der **Simulator** meldet die Geräte-ID der nachgebildeten Hardware und wird genauso geprüft: Im Simulatormodus lassen sich Projekte für den Test Interface Adapter am Schreibtisch öffnen und bearbeiten.

## Prüfung der Hardware-Kompatibilität

Beim Öffnen eines Projekts prüft ATLab außerdem, ob die angeschlossene Test-Hardware genügend Kanäle hat. Verwendet das Projekt Matrix-, Stimulations- oder externe Stimulationskanäle, die die Test-Hardware nicht besitzt, listet eine Warnung diese auf, z. B.:

```
Der angeschlossenen Test-Hardware fehlen benötigte Kanäle:

- Matrix-Kanäle (verfügbar: 16/24)

Alle nicht verfügbaren Kanäle gehen beim Speichern unwiderruflich verloren!
```

Schritte, die einen fehlenden Matrix-Kanal verwenden, schlagen bei der Ausführung mit *„Matrix Channel is not available.“* fehl. Speichern Sie ein solches Projekt nur, wenn Sie diese Kanalzuordnungen wirklich verwerfen wollen – öffnen Sie es stattdessen mit der passenden Hardware (oder im Simulator mit 32/16/4 Kanälen).

## Dateiformat

Projektdateien sind lesbares, eingerücktes JSON. Damit eignen sie sich gut für die Versionsverwaltung: Änderungen an einzelnen Schritten erscheinen als kleine, lesbare Diffs. Bearbeiten von Hand ist möglich, aber nicht empfohlen; legen Sie immer eine Sicherung an.

Die Testschritte werden je Modul gespeichert (`Modules` → `TestSteps`). Jedes Modul und jeder Testschritt hat eine dauerhafte interne ID. *Springe zu*-Ziele verweisen auf die Schritt-ID, sodass Sprünge auch nach Verschieben oder Umnummerieren von Schritten funktionieren.

Die Datei enthält eine `FormatId`, die sie als ATLab-Projektdatei kennzeichnet, eine `FormatVersion` und die `TargetTestHardwareId` (die Geräte-ID hexadezimal, z. B. `0xA101`, siehe [Ziel-Test-Hardware](#ziel-test-hardware)). Dateien ohne gültige `FormatId` oder `TargetTestHardwareId` werden abgelehnt. Formatversion 4 hat die Platine und die Netze der Pins der Test-Hardware hinzugefügt (`Board`); diese ATLab-Version liest auch Dateien der Version 3 und speichert sie als Version 4. Ältere ATLab-Versionen öffnen Dateien der Version 4 nicht, sodass sie die Platine beim Speichern nicht verlieren können.
