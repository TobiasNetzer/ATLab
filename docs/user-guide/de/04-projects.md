# 4. Arbeiten mit Projekten

Ein **Projekt** wird in einer einzelnen **Projektdatei** mit der Endung `.atlab` gespeichert. Sie enthält alles, was zu einem Test gehört:

- die Testschritte mit ihrer vollständigen Konfiguration,
- die Geräte aus dem Geräte-Manager,
- Laufzeitvariablen und ihre Initialwerte,
- benutzerdefinierte Namen für Matrix-, Stimulations- und externe Stimulationskanäle,
- die Projekteinstellungen (Toleranz, Seriennummer, Exportoptionen …),
- die Dokumentation (Texte, Pfade zu Bildern und Anhängen),
- die Prüflingsdaten für den Testbericht,
- den Pfad des Skriptordners.

Die Skripte selbst sind **nicht** im Projekt gespeichert; sie liegen als eigene Dateien im Skriptordner (siehe [Skripte](06-scripting.md)).

## Projektbefehle

Die Projektbefehle sind die ersten vier Symbole der Werkzeugleiste auf der Registerkarte Testausführung.

| Schaltfläche | Kürzel | Aktion |
|--------------|--------|--------|
| **Neues Projekt** | `Strg+N` | Beginnt ein leeres, unbenanntes Projekt. |
| **Projekt öffnen** | `Strg+O` | Öffnet eine vorhandene `.atlab`-Projektdatei. |
| **Projekt speichern** | `Strg+S` | Speichert in die aktuelle Projektdatei. Bei einem unbenannten Projekt wie *Projekt speichern unter*. |
| **Projekt speichern unter** | `Strg+Umschalt+S` | Speichert unter neuem Namen. Vorschlag ist `Test.atlab`. |

Während ein Test läuft, sind die Projektbefehle gesperrt.

## Ungespeicherte Änderungen

Jede Änderung markiert das Projekt als geändert und fügt der Titelleiste ein `*` hinzu. Wenn Sie bei ungespeicherten Änderungen ein neues Projekt anlegen, ein anderes öffnen oder ATLab schließen, fragt ein Dialog **„Sie haben ungespeicherte Änderungen. Trotzdem fortfahren?“**. Wählen Sie **Weiter**, um die Änderungen zu verwerfen, oder **Abbrechen**, um zurückzukehren.

Das Ausführen eines Tests markiert das Projekt nicht als geändert.

## Welches Projekt beim Start geöffnet wird

1. Wurde ATLab mit einem Dateipfad als erstem Argument gestartet (z. B. per Doppelklick auf eine `.atlab`-Datei unter Windows), wird diese Datei geöffnet.
2. Andernfalls wird das zuletzt geöffnete oder gespeicherte Projekt erneut geöffnet.
3. Gibt es keines davon, startet ATLab mit einem leeren Projekt.

```bash
ATLab.exe "D:\Tests\PowerBoard.atlab"
```

## Relative Pfade

Beim Speichern werden alle referenzierten Dateien **innerhalb des Projektordners (oder eines Unterordners)** mit relativem Pfad gespeichert. Das gilt für

- den Skriptordner,
- Bilder und Anhänge der Dokumentation,
- Bilder in Meldungsfenstern von Testschritten,
- Dateipfade von Schritten mit Auswertung **Datei**,
- den **Ausgabepfad** für Testergebnisse.

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

## Prüfung der Hardware-Kompatibilität

Beim Öffnen eines Projekts prüft ATLab, ob der angeschlossene Adapter genügend Kanäle hat. Verwendet das Projekt Matrix-, Stimulations- oder externe Stimulationskanäle, die der Adapter nicht besitzt, listet eine Warnung diese auf, z. B.:

```
Der angeschlossenen Test-Hardware fehlen benötigte Kanäle:

- Matrix-Kanäle (verfügbar: 16/24)

Alle nicht verfügbaren Kanäle gehen beim Speichern unwiderruflich verloren!
```

Schritte, die einen fehlenden Matrix-Kanal verwenden, schlagen bei der Ausführung mit *„Matrix Channel is not available.“* fehl. Speichern Sie ein solches Projekt nur, wenn Sie diese Kanalzuordnungen wirklich verwerfen wollen – öffnen Sie es stattdessen mit der passenden Hardware (oder im Simulator mit 32/16/4 Kanälen).

## Dateiformat

Projektdateien sind lesbares, eingerücktes JSON. Damit eignen sie sich gut für die Versionsverwaltung: Änderungen an einzelnen Schritten erscheinen als kleine, lesbare Diffs. Bearbeiten von Hand ist möglich, aber nicht empfohlen; legen Sie immer eine Sicherung an.

Die Testschritte werden je Modul gespeichert (`Modules` → `TestSteps`). Jedes Modul und jeder Testschritt hat eine dauerhafte interne ID. *Springe zu*-Ziele verweisen auf die Schritt-ID, sodass Sprünge auch nach Verschieben oder Umnummerieren von Schritten funktionieren.

Die Datei enthält eine `FormatVersion`. Projektdateien aus Versionen vor Einführung der Module (ohne `FormatVersion`) können nicht geöffnet werden.
