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
und Bogen/Pfeile. Dieser Absatz beschreibt den Stand vor den folgenden Mechanik-Schritten.

## Mechanik-Schritte nach Abschluss der Netzwerkphase

Gold pro Figur, HP-/Mana-Schnellplätze und RetroPixel-Testausrüstung sind
implementiert; Details: [Item-Fortschritt und Builds](item-progression.md).
Der Nutzer bestätigt, dass alle Testrüstungen anlegbar sind und das Aussehen
ändern. Die Schadensreduktionsformel bleibt zur späteren Abstimmung offen.

Shared Stash ist ergänzt: 40 gemeinsame Weltplätze, atomare Host-Transfers,
Format-15-Speicherung und leeres Lager für alte Saves. Die Beschreibung
enthält keine Taschenplatznummern mehr; das permanente I-/Start-Hilfefenster
ist entfernt. Inventaricons verwenden gleiche zentrierte Flächen mit
normalisierten sichtbaren Bildausschnitten.

Prüfung: Domain-/Protokolltests, nativer Unity-Import/Preset-Aufbau und
Easy-Save-Dateitest bestehen. Zwei echte Spielprozesse bestätigen Client-Deposit,
Host-Entnahme, Schutz gegen veraltete Doppelentnahme und korrekten Datei-Roundtrip.
Windows-Build vom 9. Oktober, 12:28 Uhr: 0 Fehler, 1 Warnung.

Als nächster manueller Test: Shared Stash mit Maus und Controller bedienen,
gleichzeitig entnehmen, volles Inventar sowie Save/Load und Rejoin prüfen.
Danach Stat-Berechnung/Rüstungs-Balancing abstimmen, bevor neue Build-Boni
und ein erstes Zaubersystem ergänzt werden.

Anleitungen: [Items in Unity anlegen](item-authoring.md), [Shared Stash](shared-stash.md).

## Inventar und Lager: Canvas-Umstellung

Der Nutzer bestätigt den Shared Stash als technisch funktionierend und gibt
die Umstellung auf im Unity-Editor bearbeitbare Oberflächen frei.
Inventar, Lager und Trank-/Revive-HUD verwenden jetzt uGUI; InventoryInteraction
enthält keine OnGUI-Darstellung mehr. Andere Menüs behalten vorerst ihren
bisherigen Aufbau. Saveformat 15 und Netzwerkprotokoll 16 bleiben erhalten.

- Nativer Unity-Import und Prefab-Referenzprüfung bestanden: 57 Inventar-/Equipment-
  Schnellplätze und 80 Slots in der zweigeteilten Lageransicht.
- Echte Host-/Client-Prozesse bestätigen Canvas-Erstellung, Panelwechsel,
  Anlegen/Ablegen über UI-Aktionen, Client-Einlagerung und Host-Entnahme.
- Veraltete Entnahme dupliziert kein Item; Gold-/Trank-Isolation, gemeinsamer
  Trank-Cooldown und echter Easy-Save-Datei-Roundtrip bestehen weiterhin.
- Beide Canvas-Panels im Unity-Player separat gerendert und visuell geprüft:
  zentrierte Icons, frontale Figur, graue Equipment-Glyphen, Auswahl-/Qualitätsrahmen
  und segmentierte Menü-/Buttonrahmen. Versteckte Fenster liefern keinen
  Backbuffer-Screenshot; der Test rendert deshalb das Canvas in eine RenderTexture.
- Windows-Testbuild erfolgreich: 0 Fehler, 1 Warnung. Spielassembly
  `Builds/LocalCoop/SecretsReborn_Data/Managed/Assembly-CSharp.dll` vom
  9. Oktober 2026, **13:02:43 Uhr**. Testprozesse sind beendet.

Die automatische Prüfung benutzt die UI-Aktionsschnittstelle; sie simuliert
keinen physischen Maus-Drag oder Controller. Als nächster manueller Test:
I/Start öffnen, Slot-Auswahl und Bereichswechsel, Rechtsklick/A zum Anlegen und
Ablegen, Drag-and-drop, Trankzuweisung und Lagertransfer mit beiden Spielern.
Zusätzlich verschiedene Fenstergrößen prüfen. Layout-Anleitung:
[Canvas-Inventar gestalten](inventory-ui-authoring.md).

Kein Commit/Push für diese UI-Phase beauftragt oder durchgeführt.

## Trankvarianten und linke Schnellslots

Die zwei Trankplätze stehen jetzt links unter der Charaktervorschau als
**Schnellzugriff**. Rucksackstapel bleiben die Quelle; Small/Medium/Large sind
jeweils eigene Item-IDs für HP und Mana. Die sechs nativen Pickup-Prefabs stehen
als eigene Gruppe **World/PotionTests** im Waldheiligtum; jeweils drei Tränke.
Bestehende Trank-IDs, Katalogeinträge und Gelände werden beibehalten.
Werte und Authoring: [Trankgrößen](item-authoring.md).

Nativer Unity-Aufbau bestanden. Echter Host-/Client-Test bestanden:
alle sechs Varianten per UI-Drop-Aktion auf passenden Schnellslot binden,
Rucksackmengen erhalten, Mana am HP-Platz ablehnen und ursprüngliche Bindungen
wiederherstellen. Vorhandene Verbrauchs-/Cooldown-, Lager- und Easy-Save-Tests
bestehen weiterhin. Das neue Layout wurde im Player gerendert und visuell geprüft.
Windows-Spielassembly vom **9. Oktober 2026, 13:37:57 Uhr**, Build erfolgreich
mit 0 Fehlern und 1 Warnung. Testprozesse beendet. Keine neue Save-/Protokollversion.

Manuell als nächstes: die sechs Flaschen links unterhalb des Startpunkts
aufsammeln, Namen/Wirkungen vergleichen, Größen auf Schnellslots ziehen und
bei beschädigten Herzen bzw. verbrauchtem Mana benutzen. Bereits eingesammelte
Test-Pickups bleiben gemäß Weltspielstand eingesammelt. Für einen erneuten
Pickup-Test gegebenenfalls eine neue Testwelt starten. Kein Commit/Push durchgeführt.

## Gemeinsame Charakterwerte und Equipment-Boni

Armor, Bonus-Herzen und Bonus-Mana aller angelegten Items werden zentral
summiert. Canvas zeigt Gesamtwerte, Basis-Maxima, Waffenschaden/Cooldown und
die rechnerische Rüstungsreduktion. Bestehende Rüstungsformel einschließlich
Halbherz-Minimum beibehalten. Basiswerte und Equipment-Maxima sind getrennt;
Anlegen heilt/füllt nicht und Ablegen begrenzt aktuelle Werte.
Die drei Testoutfits erhalten unterschiedliche vorläufige HP-/Mana-Boni.

Saveformat **16**, Netzwerkprotokoll **17**. Alte Vitals-Daten übernehmen
bisherige Maxima als Basis. Auf allen PCs denselben vollständigen Build einsetzen.
Details, Testwerte und nächste Spieltests: [Charakterwerte](character-stats.md).

Bestanden: CharacterStatsChecks (Grenzen, Migration, Upgrades, Wechsel ohne
Heilung, Tod), Protokollchecks, Runtime-Kompilierung, nativer Unity-Aufbau und
echter Easy-Save-Roundtrip mit anschließender Entfernung geladener Boni.
Zwei echte Spielprozesse bestätigen Host/Client-Maxima beim Anlegen/Ablegen,
unveränderte Basis/aktuelle HP, weiterhin Trankgrößen-Zuweisung, Lager und Datei-Roundtrip.
Windows-Spielassembly vom **9. Oktober 2026, 14:14:13 Uhr**, 0 Buildfehler,
1 Buildwarnung. Manuelle HP-/Mana-/Armor-Vergleiche, Save/Load und Gebietswechsel
mit angelegten Boni stehen noch aus. Kein Commit/Push durchgeführt.
