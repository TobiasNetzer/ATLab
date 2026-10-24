# 15. Fehlerbehebung

## Das Ereignisprotokoll

Die meisten Probleme erklärt eine Meldung im **Ereignisprotokoll**. Öffnen Sie es mit der Info-Schaltfläche am linken Ende der Statusleiste. Die Meldungen sind nach Aktualität sortiert (neueste zuerst) und werden zusätzlich an die Protokolldatei angehängt:

- Windows: `%LOCALAPPDATA%\ATLab\events.log`
- Linux: `~/.local/share/ATLab/events.log`

Die Datei wächst mit der Zeit; Sie können sie löschen, während ATLab geschlossen ist. Fehlermeldungen im Schrittkontext beginnen mit *„Error in module '\<Name\>', step \<n\>: …“*. Die Meldungen selbst sind englisch.

## Start und Hardware

| Symptom | Ursache / Lösung |
|---------|------------------|
| Die Test-Hardware Auswahl erscheint bei jedem Start | Der Adapter wurde an der zuletzt verwendeten Schnittstelle nicht gefunden. USB-Kabel prüfen, die richtige Schnittstelle wählen und **Verbinden** klicken. |
| `Failed to connect to COMx` | Die Schnittstelle wird von einem anderen Programm verwendet oder (Linux) die Berechtigung fehlt – Benutzer zur Gruppe `dialout` hinzufügen. |
| `Initialization failed: …` | An der Schnittstelle antwortet ein Gerät, das kein Test Interface Adapter ist, oder dessen Firmware reagiert nicht. Schnittstelle prüfen und Adapter neu starten. |
| ATLab verbindet sich automatisch, ich möchte aber den Simulator | Adapter vor dem Start von ATLab abziehen, dann **Simulator** wählen. |
| Statusleiste zeigt *Keine Test-Hardware verbunden* | ATLab läuft im Simulatormodus; Relais und Schnittstellen werden nicht wirklich geschaltet. Mit angeschlossenem Adapter neu starten. |

## Projekte öffnen

| Symptom | Ursache / Lösung |
|---------|------------------|
| Warnung *Der angeschlossenen Test-Hardware fehlen benötigte Kanäle* | Das Projekt wurde für einen Adapter mit mehr Kanälen erstellt. Nicht speichern; mit der richtigen Hardware oder im Simulator öffnen. |
| Skripte fehlen in der Skriptauswahl | Der Skriptordner des Projekts existiert auf diesem Rechner nicht. ATLab legt dann an dieser Stelle einen leeren Ordner an (oder verwendet den Standardordner) und protokolliert eine Meldung. Auf der Registerkarte Skripte den richtigen Ordner wählen (**Skriptordner ändern**) und das Projekt speichern. Ein Skriptordner im Projektordner vermeidet das Problem. |
| *Unsupported project file format version 0 (expected 2)* | Das Projekt wurde mit einer ATLab-Version ohne Module gespeichert. Solche Dateien können nicht geöffnet werden. |
| *Image not found* / *File not found* | Referenzierte Dateien wurden verschoben. Bilder und Anhänge im Projektordner ablegen, damit relative Pfade funktionieren. |

## Messgeräte

| Symptom | Ursache / Lösung |
|---------|------------------|
| Schritt zeigt `Timeout` | Keine (vollständige) Antwort innerhalb des Timeouts. Befehl prüfen (liefert er überhaupt etwas zurück?), **Timeout (ms)** erhöhen, Abschlusszeichen-/Framing-Einstellungen prüfen. Für serielle SCPI-Geräte **LF Terminated** verwenden. |
| *Target Device ID … not found* | Der Schritt verweist auf ein gelöschtes Gerät. Im Schritt erneut ein Gerät wählen. |
| *Device is required.* / *Script and Device are required.* | Im Schritt ist kein Zielgerät (oder Skript) gewählt. |
| *TCP connection error* / *Connection timed out.* | Falsche IP-Adresse/Port, Gerät nicht im Netzwerk, Firewall. Port 5025 ist der übliche SCPI-Socket-Port. |
| *VISA error: …* | NI-VISA nicht installiert, falscher Ressourcenstring oder Gerät in NI MAX nicht sichtbar. VISA gibt es nur unter Windows. |
| *Failed to convert result '…'* | Die Antwort ist keine reine Zahl. [Antwortverarbeitung](10-response-processing.md) aktivieren, um den Wert zu extrahieren. |
| *Response too short: …* | Die Antwort hat weniger Bytes als **Überspringen** + **Länge** der Antwortverarbeitung. Antwort unter *Empfangene Sequenz* prüfen: Eine abgeschnittene Antwort deutet auf Framing oder Timeout hin; andernfalls Überspringen/Länge korrigieren. |
| *Numeric extraction … failed …* | Der verarbeitete Teil hat nicht das erwartete Format (z. B. ein übrig gebliebenes Präfix oder zu wenige Bytes für `{byte:N}`). Überspringen/Länge so anpassen, dass nur die Zahl übrig bleibt. Siehe [Antwortverarbeitung](10-response-processing.md#prüfregeln-prüfen-überspringenlänge-schneiden). |
| Antworten sind abgeschnitten | Serielles **Chunk**-Framing hat zu früh beendet; der Rest der Antwort wird vor dem nächsten Befehl verworfen. Auf **LF Terminated**, **CRLF Terminated** oder **Timeout Based** umstellen. |
| Antworten gehören zum vorherigen Befehl | Das Gerät antwortet langsamer als das **Timeout (ms)** des Schritts, und die verspätete Antwort kam erst nach dem Senden des nächsten Befehls an. Verspätete Antworten, die *vor* dem nächsten Befehl eintreffen, werden automatisch verworfen. **Timeout (ms)** für den langsamen Befehl erhöhen. |
| Gerät ignoriert Befehle über VISA-Socket | Abschlusszeichen explizit anhängen, z. B. `{hex:0A}`. |

## Testschritte

| Symptom | Ursache / Lösung |
|---------|------------------|
| *Matrix Channel is not available.* | Der Schritt verwendet einen Matrix-Kanal, den der angeschlossene Adapter nicht hat. |
| *Interface not available* | Der Adapter hat keine I²C-/UART-Schnittstelle, oder RS485 wurde gewählt (für Schritte nicht unterstützt). |
| *I²C write failed: NACK received.* | Falsche Adresse (7 Bit, 0–127), Prüfling nicht versorgt, fehlende Pull-ups oder falsche Frequenz. |
| *Failed to evaluate expression* | Syntaxfehler (z. B. fehlender Operand oder fehlende Klammer), ein nicht unterstütztes Zeichen wie `<` oder eine Variable mit Text statt Zahl (z. B. `Timeout`). |
| Grenzwerte haben sich nach Eingabe des Sollwerts geändert | Beabsichtigt: Die Grenzwerte werden aus der **Standardtoleranz** neu berechnet. Zuerst den Sollwert, dann die Grenzwerte eingeben. |
| Verzögerung ist viel länger als erwartet | Die Verzögerung ist in **Sekunden** angegeben. `500ms` für eine halbe Sekunde verwenden. |
| *Lower limit is greater than upper limit* | Grenzwerte korrigieren, typischerweise wenn sie aus Variablen berechnet werden. |
| *Step … tries to jump to a step that does not exist or belongs to a disabled module* | Das *Springe zu*-Ziel wurde gelöscht oder sein Modul ist deaktiviert (bzw. nicht Teil des Laufs, wenn nur ein Modul ausgeführt wird, z. B. beim Start ab dem ausgewählten Schritt). Ein neues Ziel wählen oder das Modul aktivieren. |
| Eine Laufzeitvariable hat einen unerwarteten Startwert | In einem Lauf zugewiesene Werte bleiben für den nächsten Lauf erhalten. Variable bei Bedarf in einem frühen Schritt zurücksetzen. |
| Shell-Befehl schlägt mit *Das System kann die angegebene Datei nicht finden* fehl | Der Befehl wird nicht über eine Shell ausgeführt. Eingebaute Befehle (`dir`, `echo`, …), Pipes und Batch-Dateien benötigen `cmd /c` (Windows) bzw. `bash -c "…"` (Linux) davor. |

## Ergebnisse und Berichte

| Symptom | Ursache / Lösung |
|---------|------------------|
| Es werden keine Dateien exportiert | Prüfen, ob **Seriennummer abfragen**, **Testergebnis speichern** und mindestens ein Exportformat aktiv sind, ein **Ausgabepfad** gesetzt ist, der Lauf mit **Testausführung starten** begonnen wurde und mit bestanden oder fehlgeschlagen endete (nicht abgebrochen). Bei *Nur wenn bestanden* werden fehlgeschlagene Läufe nicht exportiert. |
| CSV öffnet sich in einer einzigen Spalte | Die Datei ist tabulatorgetrennt. Beim Import *Tabulator* als Trennzeichen wählen. |
| Berichterzeugung ist langsam | Ein Gerät mit *In Testbericht aufnehmen* antwortet nicht auf seine Identifikationsabfrage. Gerät einschalten oder Option abwählen. |
| Gerät zeigt `-` unter Prüfmittel | Keine Identifikationsabfrage konfiguriert oder keine Antwort. |

## Steuermodul

| Symptom | Ursache / Lösung |
|---------|------------------|
| *No control module connected.* | Modul war beim Aktivieren der Option nicht eingesteckt. Einstecken und **Steuermodul aktivieren** aus- und wieder einschalten. |
| *Lost connection to control module.* | Das Modul wurde abgezogen. Wieder anschließen und die Option umschalten. |

## Persönliche Einstellungen zurücksetzen

Fensterposition, Design, Sprache, letzte Schnittstelle und letztes Projekt werden in `settings.json` gespeichert (siehe [Dateiablage](16-reference.md#dateiablage)). ATLab schließen und die Datei löschen, um zu den Standardwerten zurückzukehren. Projekte und Skripte sind davon nicht betroffen.
