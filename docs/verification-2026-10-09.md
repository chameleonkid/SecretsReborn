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

## Restore Point und begonnene Magie-Grundlage

Der Nutzer bestätigt den Stat-Spieltest und beauftragt einen Commit mit Restore
Point vor Magie. Commit **7a3753b**, annotierter Tag
**restore/pre-spells-2026-10-09**. Arbeitsverzeichnis direkt danach sauber;
kein Push beauftragt. [Restore-Anleitung](restore-point-2026-10-09.md).

Anschließend uncommittet begonnen: SpellDefinition, pro Figur gespeicherte
Lernränge, Saveformat 17/Protokoll 18 und erstes Secrets-Assetreview.
SpellBookChecks, Protokollchecks und separate Runtime-Kompilierung bestanden.
Noch keine angeschlossene Zauberausführung oder Ringoberfläche.

Der erste Buildversuch scheiterte am unterschiedlichen
Editor-/Player-Datenlayout (neues CharacterSaveData.spells war im geöffneten
Editor noch nicht geladen). Nach Nutzerbestätigung und erneutem Scriptimport
wurden Runtime und Editor vollständig neu kompiliert; das Datenlayout ist jetzt
konsistent. Der vorbereitete Import und der anschließende Build sind erfolgreich.

Bestanden: native Feuer-/Licht-Definitionen unter `Assets/World/Magic/Spells`,
Easy-Save-Dateitest für erlernten Feuerball-Rang 2 und Migration aus Format 16
zu leerem Zauberbuch. Zwei echte Spielprozesse bestätigen den auf dem Host
erlernten Rang 3 auf dem Client sowie dessen Save-Roundtrip. Vorhandene
Trankgrößen-Zuweisung, Equipment-Maxima, Inventar-/Lagertransfers, Gold-Isolation,
Cooldown und Schutz gegen veraltete Doppelentnahme bestehen weiterhin.

Windows-Spielassembly vom **9. Oktober 2026, 19:03:41 Uhr**: Build erfolgreich,
0 Fehler, 1 Warnung. Keine Exceptions in den beiden Testlogs; Testprozesse beendet.
Das beweist die Daten-/Persistenz-/Snapshot-Grundlage, noch keine spielbare
Zauberausführung. Nächster Schritt: Host-Cast-Ablauf, Zielregeln und Bewegungssperre,
danach Ringmenü. Neue Magie-Arbeit bleibt nach dem Restore Point uncommitted;
kein neuer Commit oder Push durchgeführt.

## Host-Zauberausführung: Feuerball und Heal

Implementiert: Host prüft Lernrang, Zielart, Gebiet, Reichweite, Mana und
Cooldown. Er fixiert die Ziele, zieht einmal Mana ab und verteilt das Budget
beim Abschluss ganzzahlig ohne Vervielfachung. Keine Umgebungskollision oder
Sichtlinienprüfung. Bewegung und physikalisches Schieben sind während der
Wirkzeit gesperrt; Schaden bleibt möglich. Caststatus wird repliziert.
Saveformat bleibt 17, Netzwerkprotokoll jetzt 19.

Vorläufiger Tastaturzugriff: F9 am Host lernt/steigert Testzauber; F10 wechselt
Ziele, F7 wirkt Feuerball, F8 Heilung, Shift wählt alle gültigen Ziele,
Escape bricht das Wirken ab. F6 bleibt ausschließlich das Koop-Menü.
Ringmenü, Controller-Zielwahl, separate Zauberbuch-Oberfläche und Effektanimationen
sind noch offen. Regeln und Testbedienung: [Zauberablauf](spell-casting.md).

Bestanden: SpellBookChecks, SpellCastChecks (Budget/Rundung), Protokollchecks
einschließlich ungültiger Zauberanfragen, separate Runtime-Kompilierung und
nativer Unity-Build. Spielassembly: **9. Oktober 2026, 19:24:56 Uhr**;
Buildbericht: **0 Fehler, 1 Warnung**.

Zwei echte Spielprozesse bestehen: Client-Feuerball trifft durch Testwand,
zweite Anfrage während Wirkzeit verbraucht kein zusätzliches Mana,
Bewegungsinput verschiebt die wirkende Figur nicht, Heilung verteilt zwei
halbe Herzen auf zwei Figuren (je ein halbes Herz), erneute Heilung während
Cooldown wird ohne weiteren Manaverbrauch abgelehnt. Vorhandene Inventar-,
Trank-, Stat-, Lager- und Save-Roundtrip-Prüfungen bestehen weiterhin.
Keine Exceptions oder Netzwerkfehler in beiden Logs; Testprozesse beendet.

Nächster Schritt: editierbares Canvas-Ringmenü mit Element → Zauber →
expliziter Zielvorschau → Bestätigung. Auswahlbewegungssperre und
Controller-Bedienung dort ergänzen. Änderungen bleiben uncommittet;
Restore Point `restore/pre-spells-2026-10-09` bleibt erhalten.

## Editierbares Ringmenü und Zauberbuch

Ergänzt: `Assets/Resources/Magic/UI/SpellRingCanvas.prefab`, acht editierbare
Ringpositionen, wiederverwendete Inventarrahmen, klare Auswahlmarkierung,
Element → Zauber → Einzelziel/alle → Bestätigung. Zauberbuchansicht zeigt
alle erlernten Definitionen mit Rängen und Werten. Maus, Tastatur und
Controller-Eingaben sind angebunden. Details: [Ringmenü](spell-ring-menu.md).

Auswahl sperrt Bewegung und physikalisches Schieben auf dem Host, ohne
Schadensimmunität. Ein eigener Netzwerk-Auswahlstatus sperrt parallele
Aktionen. Bestätigung prüft lokal Ziel/Mana/Cooldown; der Host validiert den
Cast erneut. Cast-Snapshots enthalten jetzt Cooldown-Anzeigen.
Netzwerkprotokoll **20**, Saveformat weiterhin **17**.

Ein erster Integrationstest deckte die leere Elementliste auf. Die
Verzweigung ist korrigiert; der Test prüft vorhandene Elemente jetzt explizit.
Einträge werden auf unterschiedliche, gleichmäßig verteilte Ringpositionen
gesetzt, ohne die im Prefab angelegten Positionen umzuschreiben.

Abschließender Build: **9. Oktober 2026, Spielassembly 19:54:28 Uhr**,
**0 Fehler, 1 Warnung**. SpellRingLayoutChecks und erweiterte Protokollchecks
bestehen; Runtime kompiliert. Zwei echte Spielprozesse bestehen:
Ring öffnen, Auswahlbewegungssperre, Zauberbuch, Zurück/Abbrechen,
Einzelzielvorschau, Gruppenheilung über Bestätigung sowie vorherige
Feuerball-, Mana-, Cooldown-, Inventar-, Lager-, Stat- und Save-Prüfungen.
Element-, Zauberbuch-, Ziel- und Bestätigungsansicht wurden als Canvas-Bilder
gerendert und geprüft. Keine Exceptions oder verworfenen Netzwerkzustände
in beiden Logs; Testprozesse beendet.

Noch manuell prüfen: View/M öffnen, Stick/D-Pad wählen, A/Cross bestätigen,
B/Circle zurück, Y/Triangle Zauberbuch; außerdem Auswahl unter Gegnerdruck.
F9 am Host dient weiterhin als temporärer Lernzugriff für eine Testwelt.
Nächster Ausbau: Cast-/Treffereffekte und zielverfolgende Projektile mit
Host-Schaden bei Ankunft. Feuerball trifft aktuell weiterhin direkt nach
der Wirkzeit. Kein Commit oder Push in dieser Phase.

## SoM-Icon-Ring mit Secrets-Grafiken

Der neue `SpellIconRingCanvas` ersetzt die Textfelder durch freistehende Icons
um die eigene Figur. Feste Auswahlmarkierung oben, gemeinsame Rotation,
Element → Zauber → Ziel in der Welt → Bestätigung. Separates Zauberbuch bleibt.
Originalicons aus Secrets sind nativ mit Point/None importiert; Sprite-Assets
beschneiden den transparenten Rand. Herkunft: [Icon-Quellen](spell-icon-sources.json).

Finaler Windows-Build: **9. Oktober 2026, Spielassembly 22:00:04 Uhr**,
**0 Fehler, 1 Warnung**. Unity-Prefab-/Icon-Prüfung besteht.
`SpellRingLayoutChecks` prüft Ringe bis 64 Einträge und Buch-Seitenwechsel.
Zwei echte Spielprozesse bestehen in `Temp/SpellIconRing-20261009-220138`:

- Zwölf Laufzeiticons ohne Achtergrenze; danach normale erlernte Elementauswahl.
- Rotation vorwärts/rückwärts und ausgewähltes Icon an der festen oberen Position.
- Auswahlbewegungssperre auf dem Host, Zauberbuch, Abbrechen und Weltzielwahl
  mit ausgeblendetem Icon-Ring.
- Client-Cast, Mana, Cooldown, Feuerball durch Umgebung, geteilte Heilung und
  bisherige Inventar-/Lager-/Stat-/Speicherregression.

Element-, Buch-, Ziel- und Bestätigungsansicht offscreen gerendert und visuell
geprüft. Keine Script-Exceptions oder NetworkConfig-Mismatches in den Testlogs.
Testprozesse sind beendet. Die D3D12-Meldung zur fehlenden Info-Queue und
Grafik-Ressourcen beim Testprozessende sind kein Nachweis eines Spiellogikfehlers.

Noch manuell: physischer Controller, Rotationstempo, Lesbarkeit über dem
normalen Spielfeld und Auswahl unter Gegnerdruck. Bedienung steht im
[Ringmenü-Dokument](spell-ring-menu.md). Danach Cast-/Treffereffekte und
Feuerball-Schaden erst beim Eintreffen des Host-gesteuerten Projektils.
