# 1. Einführung

ATLab ist eine Anwendung für die automatisierte Prüfung elektronischer Baugruppen, zum Beispiel für Funktions- und Endprüfungen. ATLab arbeitet mit handelsüblichen Mess- und Prüfgeräten wie Multimetern und Netzteilen und steuert sie fern. Der **Test Interface Adapter** verbindet sie mit dem Prüfling.

Ein Prüfingenieur erstellt eine **Prüfsequenz** aus Testschritten, die Werte messen und mit Grenzwerten vergleichen. Ein Bediener führt die Sequenz aus und erhält ein Bestanden/Fehlgeschlagen-Ergebnis, optional mit Testbericht.

![ATLab Hauptfenster](images/Testing_Tab_de.png)

## Was ATLab kann

### Prüfsequenzen erstellen

- **Module**: Eine Sequenz lässt sich in Module gliedern, z. B. Spannungsversorgung, Programmierung und LED-Test. Module können einzeln aktiviert oder für sich allein ausgeführt werden.
- **Signalführung**: Jeder Schritt schaltet die Relais-Matrix des Adapters sowie dessen Stimulations- und externe Stimulationsrelais.
- **Grenzwerte**: Messwerte werden mit Grenzwerten verglichen, die auch Einheiten und SI-Präfixe enthalten dürfen, z. B. `500mV` oder `4.7kOhm`.
- **Ablaufsteuerung**: Je nach Ergebnis wird ein Schritt wiederholt, zu einem anderen Schritt gesprungen, der Test beendet oder dem Bediener eine Meldung angezeigt.
- **Variablen und Ausdrücke**: Laufzeit- und Systemvariablen (Seriennummer, Benutzer, Datum …) lassen sich in Befehlen, Grenzwerten und Berechnungen verwenden.

### Messen und kommunizieren

- **Messgeräte**: ATLab steuert Messgeräte über die serielle Schnittstelle (VCOM/RS232), über TCP/IP-Sockets oder über NI-VISA. Das Befehlssystem ist vor allem auf SCPI ausgelegt.
- **Skripte**: Häufig benötigte Befehlsfolgen werden als Skripte mit Variablen gespeichert und in mehreren Projekten verwendet. Jeder Befehl kann seine Antwort mit eigener Maske und eigenen Grenzwerten prüfen, etwa ob das Gerät einen Fehler meldet.
- **Prüfling**: Über die galvanisch isolierten I²C- und UART-Schnittstellen des Adapters kommuniziert ATLab direkt mit dem Prüfling.
- **Boundary Scan**: Mit einem SEGGER J-Link oder einer FTDI-Probe liest und treibt ATLab über JTAG die Pins von Bausteinen auf der Baugruppe und spricht darüber auch I²C- und SPI-Bausteine an.
- **Antwortverarbeitung**: Aus Text- oder Binärantworten lassen sich Werte gezielt herauslösen (Überspringen/Länge, Prüfregeln, Zahlenextraktion).
- **Bediener**: Der Bediener kann eine Sichtprüfung bestätigen (Bestanden / Fehlgeschlagen) oder einen Messwert eingeben, auf Wunsch mit einem Bild als Hilfe.
- **Programme und Dateien**: Externe Programme werden ausgeführt und ihr Exit-Code oder ihre Ausgabe ausgewertet. Werte können außerdem aus Dateien gelesen werden.

### Entwickeln und Fehler suchen

- **Entwicklungsmodus**: Bearbeitet wird im Entwicklungsmodus, getrennt von der Bedieneransicht. Der Entwicklungsmodus lässt sich mit einem Passwort schützen.
- **Gezielt testen**: Ein einzelner Schritt, ein Modul oder die Sequenz ab dem ausgewählten Schritt lässt sich gezielt ausführen. Der **Debug-Modus** hält vor jedem Schritt an.
- **Skriptausführung**: Skripte lassen sich direkt am Gerät ausprobieren. Dabei werden jede Antwort und das Ergebnis jeder Antwortprüfung angezeigt.
- **Simulator**: Ohne angeschlossene Hardware ersetzt der Simulator den Test Interface Adapter, z. B. auf einem Entwicklungsrechner.

### Tests in der Fertigung ausführen

- **Seriennummer**: Auf Wunsch fragt ATLab vor dem Test die Seriennummer ab und prüft sie anhand von Validierungsregeln. Barcodescanner im Tastaturmodus werden unterstützt.
- **Prüfplatz**: Dauertests, ein großes Messfenster, das auch aus der Entfernung lesbar ist, und Schaltflächen zum Springen zu fehlgeschlagenen Schritten erleichtern die Arbeit am Prüfplatz.
- **Steuermodul**: Optional lässt sich der Test über ein USB-Steuermodul mit beleuchteten Tastern starten und stoppen.

### Dokumentieren und berichten

- **Projektdokumentation**: Notizen, Bilder, Anhänge und Angaben zum Prüfling werden im Projekt gespeichert.
- **Berichte**: Die Ergebnisse werden als CSV-Datei, als PDF-Testbericht und als JSON-Datei mit Metadaten gespeichert, einschließlich der verwendeten Messgeräte.
- **Ereignisprotokoll**: Alle Meldungen der Sitzung werden im Ereignisprotokoll gesammelt.

ATLab läuft unter Windows und Linux. Projekte und Skripte sind einfache JSON-Dateien und eignen sich daher gut für die Versionsverwaltung. Die Oberfläche gibt es auf Deutsch und Englisch, jeweils mit hellem und dunklem Design. Sprache und Design werden im Einstellungsdialog gewählt. Dieses Handbuch öffnet sich mit `F1`.

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
