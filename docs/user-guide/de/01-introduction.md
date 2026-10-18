# 1. Einführung

ATLab ist ein Framework für die automatisierte Prüfung elektronischer Baugruppen. Ein Prüfingenieur erstellt eine **Prüfsequenz**: eine Liste von Schritten, gegliedert in Module, von denen jeder Relais schaltet, mit einem Messgerät oder dem Prüfling kommuniziert, einen Zahlenwert ermittelt und ihn mit Grenzwerten vergleicht. Ein Bediener führt die Sequenz anschließend aus, gibt eine Seriennummer ein und erhält ein Bestanden/Fehlgeschlagen-Ergebnis sowie optional einen PDF-Testbericht.

![ATLab Hauptfenster](images/Testing_Tab_de.png)

## Was ATLab kann

- **Signale routen** über die Relais-Matrix des Test Interface Adapters und dessen Stimulations- und externe Stimulationsrelais pro Schritt schalten.
- **Messgeräte fernsteuern** (Multimeter, Netzteile, Oszilloskope …) über serielle Schnittstelle (VCOM/RS232), TCP/IP-Sockets oder NI-VISA. Das Befehlssystem ist vor allem auf SCPI ausgelegt.
- **Mit dem Prüfling kommunizieren** über die galvanisch isolierten I²C- und UART-Schnittstellen des Adapters.
- **Lokale Programme** oder Shell-Befehle ausführen und deren Exit-Code als Ergebnis verwenden.
- **Den Bediener fragen**, z. B. für eine Sichtprüfung (Bestanden / Fehlgeschlagen).
- **Werte berechnen** aus Laufzeitvariablen mit Ausdrücken.
- **Dateien einlesen** und daraus einen Wert gewinnen.
- **Gliedern**: die Sequenz in Module aufteilen (z. B. Spannungsversorgung, Programmierung, LED-Test), die sich einzeln aktivieren lassen.
- **Verzweigen**: einen Schritt wiederholen, zu einem anderen Schritt springen oder den Test je nach Ergebnis beenden.
- **Dokumentieren und berichten**: Testnotizen, Bilder und Anhänge im Projekt ablegen und Ergebnisse als CSV und PDF exportieren.

ATLab läuft unter Windows und Linux, bietet ein helles und ein dunkles Design und ist auf Deutsch und Englisch verfügbar.

## Grundbegriffe

| Begriff | Bedeutung |
|---------|-----------|
| **Projekt** | Eine `.atlab`-Datei mit Prüfsequenz, Geräten, Laufzeitvariablen, Kanalnamen, Projekteinstellungen, Dokumentation und Prüflingsdaten. Siehe [Arbeiten mit Projekten](04-projects.md). |
| **Modul** | Eine benannte Gruppe von Testschritten im Projekt, z. B. *Spannungsversorgung* oder *LED-Test*. Module werden der Reihe nach ausgeführt; deaktivierte Module werden übersprungen. Siehe [Module](07-building-test-sequences.md#module). |
| **Testschritt** | Eine Zeile in einem Modul mit Name, Grenzwerten, Einheit, Verzögerung, Auswertungsquelle, Relaiszuständen und Bestanden-/Fehlgeschlagen-Aktion. |
| **Auswertungsquelle** | Woher ein Schritt seinen Messwert bekommt: *Keine, Skript, Gerätebefehl, Schnittstellenbefehl, Shell-Befehl, Benutzereingabe, Ausdruck* oder *Datei*. Siehe [Auswertungsquellen](08-evaluation-sources.md). |
| **Gerät** | Ein externes Messgerät (oder ein über Seriell/TCP/VISA erreichbarer Prüfling), angelegt im Geräte-Manager. |
| **Skript** | Eine wiederverwendbare, benannte Liste von Gerätebefehlen, gespeichert als JSON-Datei in einem *Skriptordner*, damit sie projektübergreifend genutzt werden kann. |
| **Skriptvariable** | Platzhalter wie `{Range}` in einem Skript, der pro Testschritt einen konkreten Wert erhält. |
| **Laufzeitvariable** | Projektweite Variable, die in Befehlen, Grenzwerten und Ausdrücken gelesen und von einem Schritt über *Ergebnis einer Variable zuweisen* geschrieben werden kann. |
| **Relais-Matrix** | Die Messmatrix des Adapters. Jeder Schritt wählt einen Kanal für **Matrix H** und einen für **Matrix L** (oder *Off*). |
| **Stimulations- / Ext. Stimulationskanäle** | Einzeln schaltbare Relais des Adapters, pro Schritt eingestellt. |
| **Entwicklungsmodus** | Bearbeitungsmodus der Registerkarte Testausführung. Ist er aus, zeigt die Registerkarte nur Sequenz und Ergebnisse – die Bedieneransicht. |
| **Simulator** | Virtueller Test Interface Adapter, der verwendet wird, wenn keine Hardware angeschlossen ist. Relais- und Schnittstellenbefehle werden erfolgreich quittiert, ohne etwas zu tun. |

## Wie ein Testschritt ausgeführt wird

Jeder Schritt durchläuft dieselben Phasen:

1. **Kommentardialog (optional)** – ist *Kommentar anzeigen* aktiv, wird der Kommentar angezeigt und der Bediener muss **Weiter** klicken.
2. **Werte auflösen** – Grenzwerte, Sollwert und Verzögerung werden berechnet, einschließlich `{Variable}`-Verweisen.
3. **Relais schalten** – Matrix-, Stimulations- und externe Stimulationsrelais werden gemäß Schritt eingestellt.
4. **Warten** – die Verzögerung des Schritts läuft ab (bei mehr als einer Sekunde mit Fortschrittsbalken).
5. **Erfassen** – die Auswertungsquelle liefert einen Rohwert; die Antwortverarbeitung kann daraus eine Zahl extrahieren.
6. **Bewerten** – der Wert wird mit Unter- und Obergrenze verglichen; die Abweichung vom Sollwert wird berechnet.
7. **Entscheiden** – die Bestanden- oder Fehlgeschlagen-Aktion legt fest, wie es weitergeht (fortfahren, wiederholen, springen, beenden oder Kommentar anzeigen).

Vor dem ersten und nach dem letzten Schritt werden alle Relais zurückgesetzt.

## Für wen dieses Handbuch gedacht ist

- **Prüfingenieure**, die Sequenzen entwerfen: Kapitel 4–10.
- **Bediener**, die freigegebene Sequenzen ausführen: Kapitel 3 und 11 genügen.
- **Administratoren**, die ATLab auf Prüfplätzen installieren: Kapitel 2 und die [Referenz](16-reference.md).
