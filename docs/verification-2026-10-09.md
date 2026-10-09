# Prüfstand und Netzwerk-Abschluss – 9. Oktober 2026

Die Netzwerk-Testphase ist auf ausdrückliche Entscheidung des Nutzers abgeschlossen.
Die unten aufgeführten Restpunkte bleiben dokumentiert und blockieren den weiteren
Mechanikausbau nicht. Sie gelten nicht als getestete oder bereits gelöste Fälle.

## Verbindlicher Multiplayer-Stand

- Maximal vier Spieler einschließlich Host. Der Host verwaltet Welt und alle Figuren.
- Figuren gehören zur Host-Welt; lokale Verbindungsschlüssel bestimmen keinen Besitz.
- Nach erkanntem Disconnect werden Verbindungsplatz und Figur sofort freigegeben.
  Inventar, Ausrüstung, Aussehen, HP/Mana, Tod und Position bleiben in der Host-Welt.
- Jeder Beitritt verlangt Auswahl und Bestätigung einer freien Figur, auch wenn nur
  eine frei ist. Keine automatische Wiederübernahme und keine Reservierungsfrist.
- Belegte Figuren und Figuren während eines bestätigten Ladehandshakes sind gesperrt.
- Auswahl und Erstellung sind erst bei vollständig aufgebauter Verbindung möglich.
- Netzwerkprotokoll **14**: alle Teilnehmer benötigen denselben aktuellen Build.
  Savegame-Schema bleibt **13**; bestehende Spielstände bleiben kompatibel.

Details: [Reconnect](reconnect.md), [LAN-Testablauf](lan-test.md).

## Technische Nachweise

Nativer Unity-Import und Windows-Player-Build vom 9. Oktober, 08:44:26:
**Succeeded, 0 Fehler, 1 Warnung**. Die Player-Code-DLL ist vom 08:44:20.
Protokollchecks für Version 14 bestehen. Der Build liegt unter
`Builds/LocalCoop/SecretsReborn.exe`; zum Kopieren den gesamten Ordner verwenden.

Der Zwei-Prozess-Test besteht auf Host und Client:
`Temp/RejoinSelection-73b71799eced4c40988e193d334c04b7`.
Geprüft sind sofortige Freigabe, ausdrückliche Auswahl, Warteschlange während
Host-Operationen, Save/Load abwesender Figuren, aktuelles Gebiet, Inventar,
HP/Mana und Tod, abgebrochene Auswahl, erneuter Beitritt und Schutz der Hostfigur.

Der Drei-Prozess-Test besteht auf allen Teilnehmern:
`Temp/LateJoinSelection-d1e787c3581441d4bd3f61221e5ec7f8`.
Geprüft sind später Beitritt mit Creator, gültige und ungültige Optik, Schutz vor
doppelter Erstellung, Optik-Save-Roundtrip und Meldung an alle. Beide Gäste
verlassen das Spiel und kehren in umgekehrter Reihenfolge zurück. Beide Figuren
werden sofort frei und ausdrücklich bestätigt; auch die letzte einzelne freie
Figur wird nicht automatisch zugewiesen. Lokale Client-Profile sind getrennt.

Der anfängliche Test mit sofortiger Auswahl während des Verbindungsaufbaus blieb
hängen. Die Bereitschaftsprüfung und synchronisierte Testschritte beheben diesen
Fall; die abschließenden Berichte oben bestehen vollständig. Nachweise vom
8. Oktober zur alten Reservierungsregel gelten nicht für den neuen Ablauf.

Die temporären Berichte und Test-Saves bleiben außerhalb von Git. Testtreiber
und Protokollchecks werden mit dem Code versioniert. Die Testprozesse sind beendet.

## Manuell bestätigte Ergebnisse

- Laufender Beitritt, Figurenwahl und Rejoin funktionieren.
- Vier Spieler aktiv → einer disconnectet → ein neuer Spieler tritt bei und
  bestätigt die freie Figur. Verbindungsplatz und Figur werden sauber freigegeben.
- Ein fünfter Spieler wird abgelehnt. Dieser Test stammt aus dem vorherigen Build;
  die unveränderte Begrenzung benötigt laut vereinbartem Prüfstand keine Wiederholung.
- Geordnetes Host-Ende trennt alle anderen Spieler sofort.
- LAN-Spielbetrieb wurde nach der Anleitung mit „Funktioniert perfekt“ bestätigt.

Die Bestätigung eines allgemeinen LAN-Spieltests ersetzt keinen gesonderten
Nachweis für Kabelverlust, WLAN-Abschaltung oder Prozessabsturz.

## Nicht blockierende Restpunkte

- Abrupte Netzwerkunterbrechung eines Clients, Transport-Timeout und anschließender Rejoin.
- Host-Netzwerkverlust oder Host-Prozessabsturz; verständliche Rückkehr ins Menü.
- Disconnect während Buchbedienung, Abstimmung, Laden oder Truhenanzeige mit vier Spielern.
- Neustart aller lokalen Test-Clients und explizite Wahl unabhängig von der Startreihenfolge.

## Git-Abschluss und nächster Einstieg

Der Nutzer hat Dokumentation, Commit und Push freigegeben. Aufteilung:

1. Reconnect-Fix, Protokoll 14 und aktualisierte Regressionstests; alte
   Reservierungsklasse und deren Tests entfernen.
2. Dokumentation des abgeschlossenen Prüfstands, LAN-Anleitung und nächste Schritte.

Library, Temp, Builds, lokale Spielstände und das gekaufte Easy-Save-Plugin bleiben
ignoriert. Manuell bearbeitete Szenen und Assets bleiben erhalten.

Als Vorschlag folgt Wiederbeschaffung wichtiger Truhengegenstände für spätere
Mitspieler: vorhandene gemeinsame Fund-Freischaltungen für ein Händlerangebot
verwenden. Preise und Einstiegsausrüstung vorher festlegen. Gold pro Charakter
und hostseitig geprüfte Käufe ergänzen; danach Heil-/Mana-Potions, Schnellslots
und Bogen/Pfeile. Hier wird ausschließlich dokumentiert, nichts davon implementiert.