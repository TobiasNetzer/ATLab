# 1. Einführung

ATLab ist eine Anwendung für die automatisierte Prüfung elektronischer Baugruppen, zum Beispiel für Funktions- und Endprüfungen. ATLab arbeitet mit handelsüblichen Mess- und Prüfgeräten wie Multimetern und Netzteilen und steuert sie fern. Der **Test Interface Adapter** verbindet sie mit dem Prüfling.

Ein Prüfingenieur erstellt eine **Prüfsequenz** aus Testschritten, die Werte messen und mit Grenzwerten vergleichen. Ein Bediener führt die Sequenz aus und erhält ein Bestanden/Fehlgeschlagen-Ergebnis, optional mit Testbericht.

![ATLab Hauptfenster](images/Testing_Tab_de.png)

## Was ATLab kann

### Prüfsequenzen erstellen

- **Gliedern**: die Sequenz in Module aufteilen (z. B. Spannungsversorgung, Programmierung, LED-Test), die sich einzeln aktivieren oder allein ausführen lassen.
- **Signale routen**: pro Schritt über die Relais-Matrix des Adapters und dessen Stimulations- und externe Stimulationsrelais.
- **Werte prüfen** gegen Grenzwerte mit Einheiten und SI-Präfixen, z. B. `500mV` oder `4.7kOhm`.
- **Verzweigen** je nach Ergebnis: einen Schritt wiederholen, zu einem anderen Schritt springen, den Test beenden oder dem Bediener eine Meldung anzeigen.
- **Rechnen** mit Laufzeit- und Systemvariablen (Seriennummer, Benutzer, Datum …) in Befehlen, Grenzwerten und Ausdrücken.

### Messen und kommunizieren

- **Messgeräte fernsteuern** über serielle Schnittstelle (VCOM/RS232), TCP/IP-Sockets oder NI-VISA. Das Befehlssystem ist vor allem auf SCPI ausgelegt.
- **Gerätskripte wiederverwenden**: Skripte aus mehreren Befehlen mit Variablen, projektübergreifend nutzbar. Jeder Befehl kann **seine Antwort prüfen**, mit eigener Maske und eigenen Grenzwerten, z. B. dass das Gerät keinen Fehler meldet.
- **Mit dem Prüfling kommunizieren** über die galvanisch isolierten I²C- und UART-Schnittstellen des Adapters.
- **Boundary Scan nutzen** (JTAG) mit einem SEGGER J-Link oder einer FTDI-Probe, um Pins von Bausteinen der Baugruppe zu lesen und zu treiben und darüber mit I²C- und SPI-Bausteinen zu kommunizieren.
- **Werte gewinnen** aus Text- oder Binärantworten mit der Antwortverarbeitung (Überspringen/Länge, Prüfregeln, Zahlenextraktion).
- **Den Bediener einbeziehen**: eine Sichtprüfung bestätigen (Bestanden / Fehlgeschlagen) oder einen Messwert eingeben lassen, optional mit Bild.
- **Externe Programme** ausführen und deren Exit-Code oder Ausgabe verwenden oder **einen Wert aus einer Datei lesen**.

### Entwickeln und Fehler suchen

- **Entwicklungsmodus** zum Bearbeiten, getrennt von der Bedieneransicht; er lässt sich mit einem Passwort schützen.
- **Schrittweise ausprobieren**: einen Einzelschritt ausführen, ab dem ausgewählten Schritt starten oder ein Modul ausführen; der **Debug-Modus** hält vor jedem Schritt an.
- **Skripte testen** in der Skriptausführung, die jede Antwort und das Ergebnis jeder Antwortprüfung zeigt.
- **Ohne Hardware arbeiten**: Der Simulator ersetzt den Test Interface Adapter, z. B. auf einem Entwicklungsrechner.

### Tests in der Fertigung ausführen

- **Seriennummerneingabe** mit Prüfregeln; Barcodescanner im Tastaturmodus werden unterstützt.
- **Dauertests**, ein großes **Messfenster**, das auch aus der Entfernung lesbar ist, und Schaltflächen zum Springen zu fehlgeschlagenen Schritten.
- Ein optionales USB-**Steuermodul** mit beleuchteten Start-/Stopp-Tastern.

### Dokumentieren und berichten

- **Projektdokumentation** mit Notizen, Bildern, Anhängen und Angaben zum Prüfling.
- **CSV-Datendateien** und **PDF-Testberichte**, einschließlich der verwendeten Messgeräte.
- Ein **Ereignisprotokoll** mit allen Meldungen der Sitzung.

ATLab läuft unter Windows und Linux. Projekte und Skripte sind einfache JSON-Dateien und eignen sich gut für die Versionsverwaltung. Die Oberfläche ist auf Deutsch und Englisch verfügbar, mit hellem und dunklem Design. Sprache und Design werden im Einstellungsdialog gewählt. Dieses Handbuch öffnet sich mit `F1`.

## Grundbegriffe

| Begriff | Bedeutung |
|---------|-----------|
| **Projekt** | Eine `.atlab`-Datei mit Prüfsequenz, Geräten, Laufzeitvariablen, Kanalnamen, Projekteinstellungen, Dokumentation und Prüflingsdaten. Siehe [Arbeiten mit Projekten](05-projects.md). |
| **Modul** | Eine benannte Gruppe von Testschritten im Projekt, z. B. *Spannungsversorgung* oder *LED-Test*. Module werden der Reihe nach ausgeführt; deaktivierte Module werden übersprungen. Siehe [Module](08-building-test-sequences.md#module). |
| **Testschritt** | Eine Zeile in einem Modul mit Name, Grenzwerten, Einheit, Verzögerung, Auswertungsquelle, Relaiszuständen und Bestanden-/Fehlgeschlagen-Aktion. Siehe [Wie ein Testschritt ausgeführt wird](14-running-tests.md#wie-ein-testschritt-ausgeführt-wird). |
| **Auswertungsquelle** | Woher ein Schritt seinen Messwert bekommt: *Keine, Skript, Gerätebefehl, Schnittstellenbefehl, Externes Programm, Benutzerbestätigung, Benutzereingabe, Ausdruck, Datei, Boundary Scan* oder *Boundary-Scan-Bus*. Siehe [Auswertungsquellen](09-evaluation-sources.md). |
| **Gerät** | Ein externes Messgerät (oder ein über Seriell/TCP/VISA erreichbarer Prüfling), angelegt im Geräte-Manager. |
| **Skript** | Eine wiederverwendbare, benannte Liste von Gerätebefehlen, gespeichert als JSON-Datei in einem *Skriptordner*, damit sie projektübergreifend genutzt werden kann. |
| **Skriptvariable** | Platzhalter wie `{Range}` in einem Skript, der pro Testschritt einen konkreten Wert erhält. |
| **Laufzeitvariable** | Projektweite Variable, die in Befehlen, Grenzwerten und Ausdrücken gelesen und von einem Schritt über *Ergebnis einer Variable zuweisen* geschrieben werden kann. |
| **Relais-Matrix** | Die Messmatrix des Adapters. Jeder Schritt wählt einen Kanal für **Matrix H** und einen für **Matrix L** (oder *Off*). |
| **Stimulations- / Ext. Stimulationskanäle** | Einzeln schaltbare Relais des Adapters, pro Schritt eingestellt. |
| **Entwicklungsmodus** | Bearbeitungsmodus der Registerkarte Testausführung. Ist er aus, zeigt die Registerkarte nur Sequenz und Ergebnisse. Das ist die Bedieneransicht. |
| **Simulator** | Virtueller Test Interface Adapter, der verwendet wird, wenn keine Hardware angeschlossen ist. Relais- und Schnittstellenbefehle werden erfolgreich quittiert, ohne etwas zu tun. |
| **Boundary-Scan-Kette** | Die Boundary-Scan-fähigen Bausteine der Baugruppe, in Reihe an eine JTAG-Probe angeschlossen. Wird auf der [Registerkarte Boundary Scan](12-boundary-scan.md) eingerichtet. |

## Für wen dieses Handbuch gedacht ist

- **Prüfingenieure**, die Sequenzen entwerfen: Kapitel 5–13 (Kapitel 12 nur bei Einsatz von Boundary Scan).
- **Bediener**, die freigegebene Sequenzen ausführen: Kapitel 3, 14 und 15 genügen.
- **Administratoren**, die ATLab auf Prüfplätzen installieren: Kapitel 2 und 4 sowie die [Referenz](17-reference.md).
