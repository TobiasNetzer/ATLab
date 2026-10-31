# 3. Erste Schritte

## Erster Start: Test-Hardware auswählen

Beim Start versucht ATLab zuerst, sich über die zuletzt verwendete serielle Schnittstelle mit dem Test Interface Adapter zu verbinden. Gelingt das, öffnet sich direkt das Hauptfenster.

Gibt es keine zuletzt verwendete Schnittstelle oder antwortet der Adapter nicht, erscheint das Fenster **Test-Hardware Auswahl**:

| Element | Funktion |
|---------|----------|
| Schnittstellenliste + Aktualisieren | Listet die seriellen (COM-)Schnittstellen des Rechners. Nach dem Einstecken des Adapters auf Aktualisieren klicken. |
| **Verbinden** | Öffnet die gewählte Schnittstelle und initialisiert den Adapter. Bei Erfolg wird die Schnittstelle für den nächsten Start gespeichert. |
| **Simulator** | Ohne Hardware fortfahren, mit einem virtuellen Adapter. |
| **Beenden** | Schließt ATLab. |

Eine Statuszeile zeigt das Ergebnis, z. B. `Connected to COM5` oder `Initialization failed: …`.

### Simulatormodus

Im Simulatormodus verhält sich ATLab so, als wäre ein Adapter mit 32 Matrix-Kanälen, 16 Stimulationskanälen, 4 externen Stimulationskanälen und allen Schnittstellen (I²C, UART, RS485) angeschlossen. Relaisschaltungen und Schnittstellenbefehle werden erfolgreich quittiert, ohne etwas zu tun; Schnittstellen-Lesevorgänge liefern das ASCII-Zeichen `0`. Messgeräte (Seriell/TCP/VISA), externe Programme, Dateien, Ausdrücke, Benutzerbestätigungen und Benutzereingaben funktionieren normal.

Solange der Simulator aktiv ist, zeigt die Statusleiste in Rot **Keine Test-Hardware verbunden**. Der Simulatormodus eignet sich ideal zum Bearbeiten von Sequenzen am Schreibtisch.

> Um mit anderer Hardware neu zu starten, schließen Sie ATLab und starten es erneut. Ist der zuletzt verwendete Adapter noch angeschlossen, verbindet sich ATLab automatisch; ziehen Sie ihn ab, um die Auswahl zu erhalten.

## Das Hauptfenster

![Hauptfenster](images/Testing_Tab_de.png)

Das Fenster besteht aus drei Bereichen:

1. **Registerkarten** oben:
   - **Testausführung** – Prüfsequenz erstellen und ausführen ([Kapitel 8](08-building-test-sequences.md), [Kapitel 14](14-running-tests.md))
   - **Konfiguration** – Geräte, Variablen, Kanalnamen, Projekteinstellungen ([Kapitel 6](06-configuration.md))
   - **Dokumentation** – Notizen, Bilder, Anhänge, Prüflingsdaten ([Kapitel 13](13-documentation-tab.md))
   - **Skripte** – der Skript-Editor ([Kapitel 7](07-scripting.md))
   - **Boundary Scan** – JTAG-Probe, Scan-Kette, Pins und Busse ([Kapitel 12](12-boundary-scan.md))
   - **Test-Hardware** – Adapterinformationen und Diagnose ([Kapitel 4](04-test-hardware.md))
   - **Info** – Version und Systeminformationen
2. **Inhalt der Registerkarte** in der Mitte.
3. **Statusleiste** unten.

Die Titelleiste zeigt den Projektnamen, gefolgt von `*` bei ungespeicherten Änderungen, z. B. `ATLab - MyBoard*`.

### Statusleiste

| Element | Funktion |
|---------|----------|
| Info-Schaltfläche (links) | Öffnet das **Ereignisprotokoll**: alle Info-, Warn- und Fehlermeldungen dieser Sitzung, neueste zuerst. |
| **Keine Test-Hardware verbunden** | Wird im Simulatormodus angezeigt. |
| Prüfspitzen-Schaltfläche (rechts) | **Matrix-Kanal über externe Prüfspitze ermitteln** (`F12`). Berühren Sie ein Netz mit der externen Prüfspitze, und ATLab zeigt `Matrix Channel: n`. Siehe [Test-Hardware](04-test-hardware.md#matrix-kanal-mit-der-prüfspitze-ermitteln). |
| Zahnrad | Öffnet den Dialog **Einstellungen** (siehe [Einstellungen](#einstellungen)). Während ein Test läuft, nicht verfügbar. |
| Buch (ganz rechts) | **Benutzerhandbuch öffnen** (`F1`). Öffnet dieses Handbuch als PDF in der aktuellen Oberflächensprache. Fehlt das PDF (z. B. in einer selbst gebauten Entwicklungsversion), wird stattdessen das englische PDF oder das Online-Handbuch auf GitHub geöffnet. |

Meldungen erscheinen zusätzlich kurz als Benachrichtigung unten links; welche, legt die Projekteinstellung **Protokolliergrad** fest (siehe [Konfiguration](06-configuration.md#allgemeine-einstellungen)).

![Helles und dunkles Design](../../Themes.png)

### Einstellungen

Die Zahnrad-Schaltfläche in der Statusleiste öffnet die persönlichen Einstellungen dieses Rechners (sie liegen in `settings.json`, nicht im Projekt). Änderungen werden mit **Speichern** übernommen; **Abbrechen** verwirft sie.

| Einstellung | Funktion |
|-------------|----------|
| **Sprache** | Deutsch oder Englisch. |
| **Design** | Dunkel oder hell. |
| **Nur Simulation** | Immer im Simulatormodus starten, ohne Verbindungsversuch zur Testhardware – praktisch für einen Entwicklungsrechner, an dem nie Hardware hängt. Wird beim nächsten Start wirksam. |
| **Passwort zum Aktivieren des Entwicklungsmodus verlangen** | Schützt den Entwicklungsmodus mit einem Passwort, damit Bediener die Sequenz nicht bearbeiten können. Passwort in **Neues Passwort** und **Passwort bestätigen** eingeben. Zum Ändern ein neues eingeben; beide Felder leer lassen, um das aktuelle Passwort zu behalten. |

Solange ein Passwort gesetzt ist, verlangt das Ausschalten oder Ändern des Passworts sowie das Ändern von **Nur Simulation** das aktuelle Passwort: Es erscheint ein Feld **Aktuelles Passwort**, das beim **Speichern** geprüft wird. Ausschalten des Passworts löscht es. Das Passwort wird nur als gesalzener Hash gespeichert.

### Bedieneransicht und Entwicklungsmodus

Die Registerkarte Testausführung hat zwei Gesichter:

- **Bedieneransicht** (Standard): Werkzeugleiste, Modulliste, Schrittliste, Ergebnisleiste. Die Schrittkonfiguration ist ausgeblendet, und nur die Registerkarten Testausführung und Info werden angezeigt.
- **Entwicklungsmodus**: umschaltbar über die Baustellen-Schaltfläche am rechten Ende der Werkzeugleiste (**Entwicklungsmodus aktivieren**). Rechts erscheinen die Konfigurationsbereiche, die Modulliste erhält Schaltflächen zum Bearbeiten der Module, außerdem stehen die Debug-Schaltflächen zur Verfügung.

Die Einstellung wird gespeichert, sodass ein Prüfplatz in der Bedieneransicht verbleiben kann. Ist ein Passwort für den Entwicklungsmodus gesetzt (siehe [Einstellungen](#einstellungen)), fragt die Schaltfläche danach, und ATLab startet immer in der Bedieneransicht.

## Ihr erster Test in fünf Minuten

Diese Anleitung funktioniert im Simulatormodus und benötigt keine Messgeräte.

1. **Projekt anlegen.** Auf der Registerkarte Testausführung **Neues Projekt** klicken (oder `Strg+N`). Ein neues Projekt enthält ein Modul, *Modul 1*, das in der Modulliste ausgewählt ist.
2. **Entwicklungsmodus aktivieren** mit der Baustellen-Schaltfläche.
3. **Schritt hinzufügen.** **Neuen Testschritt hinzufügen** (`+` oder Taste `Einfg`) klicken. Eine neue Zeile erscheint und ist ausgewählt.
4. **Konfigurieren** im Bereich **Testschritt-Konfiguration** rechts:
   - **Name**: `Bedienerprüfung`
   - **Auswertung**: `Benutzerbestätigung`
   - **Kommentar**: `Leuchtet die Power-LED grün?`
   - **Sollwert**: `1` – Unter- und Obergrenze werden automatisch aus der Standardtoleranz berechnet; setzen Sie **Untergrenze** und **Obergrenze** jeweils auf `1`.
5. **Zweiten Schritt hinzufügen**, der etwas berechnet:
   - **Name**: `Versorgungsverhältnis`
   - **Auswertung**: `Ausdruck`
   - Im Bereich **Ausdruck** `12 / 4` eingeben
   - **Sollwert** `3`, **Untergrenze** `2.9`, **Obergrenze** `3.1`
6. **Speichern** mit `Strg+S`; Speicherort und Namen wählen, z. B. `ErsterTest.atlab`.
7. **Ausführen** mit `F5` (oder der Schaltfläche **Testausführung starten** ▶).
   - Der erste Schritt fragt *„Leuchtet die Power-LED grün?“*. Klicken Sie **Bestanden** (Ergebnis `1`) oder **Fehlgeschlagen** (Ergebnis `0`).
   - Der zweite Schritt berechnet `12 / 4 = 3` und besteht.
8. Die Ergebnisleiste wird grün (**Test bestanden**) oder rot (**Test fehlgeschlagen**). Die Spalten **Messwert**, **Abweichung** und **Ergebnis** sind ausgefüllt.

Weiter geht es mit der [Konfiguration](06-configuration.md), um echte Messgeräte einzubinden.
