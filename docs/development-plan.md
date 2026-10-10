# Entwicklungsplan

Stand: 10. Oktober 2026. Die Netzwerk-Testphase ist laut Nutzer abgeschlossen.
Für einen neuen Rechner oder Chat zuerst die [aktuelle Übergabe](handoff-2026-10-10.md)
lesen; sie trennt verbindliche Entscheidungen, aktuellen Code und offene Tests.
Schwerpunkt bleibt die Weiterentwicklung der Mechaniken.
Die neue Richtung und Reihenfolge stehen in [Item-Fortschritt und Builds](item-progression.md):
Gold, Trank-Schnellplätze und Testausrüstung zuerst; anschließend Shared Stash,
gemeinsame Stat-Berechnung, Händler und ein erstes Zaubersystem. Vorerst keine XP.
Der erste Schritt ist implementiert und automatisch geprüft (einschließlich
echtem Host-/Client-Trankverbrauch und Easy Save). Der Nutzer bestätigt die
anlegbaren Testrüstungen und den funktionierenden Shared Stash.
Der Shared Stash ist der nächste umgesetzte Schritt: 40 gemeinsame Weltplätze,
Host-validierte atomare Transfers und Save-Migration. Siehe [Shared Stash](shared-stash.md).
Inventar und Lager sind anschließend auf editierbare Canvas-Prefabs umgestellt;
siehe [UI-Authoring](inventory-ui-authoring.md). Als nächstes die neue Oberfläche
mit Maus und Controller prüfen, danach Stat-Berechnung und Rüstungs-Balancing
abstimmen. Das erste Zaubersystem baut auf dieser gemeinsamen Stat-Grundlage auf.
Rüstungsformel bleibt unverändert und wird vor weiterem Stat-Balancing abgestimmt.
Die gemeinsame Stat-Berechnung und Canvas-Anzeige sind jetzt ergänzt:
[Charakterwerte](character-stats.md). Equipment-Maxima bleiben von dauerhaften
Basiswerten getrennt. Der Nutzer bestätigt den Stat-Spieltest als erfolgreich.
Nächster Schwerpunkt ist das [vereinbarte Zaubersystem](spell-system.md):
Secrets-Effekte sichten, persistente Zauberdefinitionen/Ränge und Host-Ausführung
aufbauen, danach Canvas-Ringmenü, Zielwahl und Feuerball/Heal durchgängig prüfen.
Die erste Definition-/Lernstatus-Grundlage ist angelegt, nativ importiert und
mit Speicherdatei sowie Host-/Client-Snapshots geprüft. Aktuell Saveformat 17,
Protokoll 21. Die Host-Ausführung von Feuerball und Heal ist jetzt angeschlossen
und mit zwei echten Spielprozessen geprüft: Mana, geteiltes Budget,
Umgebung ohne Kollision und Bewegungssperre während des Wirkens.
Siehe [Zauberablauf und Testtasten](spell-casting.md).
Das editierbare Canvas-Ringmenü mit Elementen, Zaubern, expliziter Zielwahl
und separatem Zauberbuch ist ergänzt. Controller-Eingaben und
Bewegungssperre während der Auswahl sind angebunden: [Ringmenü](spell-ring-menu.md).
Der rotierende Icon-Ring nach der SoM-Referenz ist umgesetzt. Secrets-Icons
führen von Elementen zu den Zaubern, ohne feste Dreier- oder Achtergrenze;
die Auswahlposition bleibt oben. Zielwahl erfolgt anschließend in der Welt.
Nächster konkreter Schritt: einen aktuellen Windows-Build erstellen und die
cooldownfreien Tränke sowie HUD-Abstand und Transparenz mit Host und Client
prüfen. Danach folgen Cast-/Treffereffekte und Feuerball-Projektile, deren
Schaden erst beim Host-gesteuerten Eintreffen entsteht.
Restore Point vor Magie: `restore/pre-spells-2026-10-09` (`7a3753b`).
Restore Point vor dem Verbrauchsgegenstände-Ring:
`restore/pre-consumable-ring-2026-10-09` (`f741399`). Der gemeinsame Ring
enthält nun Verbrauchsgegenstände mit Mengen und Zauber; oben/unten wechselt
den Menütyp. Trank-Schnellslots entfallen. Fehlendes Mana graut Zauber aus.
Am 10. Oktober angepasst: Tränke bleiben immer farbig. Zauber brauchen nur
Element → Zauber → Ziel; Zielbestätigung startet den Cast ohne weitere Ebene.
Tränke haben keinen Cooldown. Der halbtransparente Info-Streifen liegt unter
dem Herzen-/Mana-HUD; der aktuelle Mana-Vorrat wird dort nicht doppelt angezeigt.
Der Nutzer akzeptiert den aktuellen Stand vorerst. Unity hat den Laufzeitcode
neu kompiliert; Build und Host-/Client-Prüfung der letzten Änderungen stehen
noch aus. Details: [Prüfung vom 10. Oktober](verification-2026-10-10.md).
Zauber ignorieren Umgebungskollision; die Figur steht während Auswahl und Wirken.
Händler, essentielle Wiederbeschaffung und wiederholbare Boss-Dungeons folgen
auf dieser Grundlage. Vorläufige Kosten, Rundung und Unterbrechungsregeln
sind im Zauberablauf dokumentiert; Balance bleibt anpassbar.
Code ist freigegeben; Commits und Pushes erfolgen auf ausdrücklichen Auftrag.

## Erreichter Stand

Die editierbaren Gebiete verfügen über Grid, mehrere Ground-/Terrain-Layer und
Prefabs. Inventar, Ausrüstung, Lampen, Runen, Solo-Gebietswechsel, Savegame,
Herzen/Mana, Nahkampf, Gegner, Loot und persistente Truhen sind vorhanden.
Tod, Gruppen-Game-Over und gehaltenes Revive sind umgesetzt.

Der erste lokale Netzwerk-Koop funktioniert mit Host und Client. Der Host
entscheidet über Bewegung, Kampf und Zustandsänderungen. Spieler und Gegner
werden auf Clients geglättet dargestellt. Der Zwei-Prozess-Test besteht;
der manuelle Bewegungstest wurde ebenfalls bestätigt.

Details: [Übergabe](handoff-2026-10-06.md), [Prüfstand](verification-2026-10-06.md)
und [Koop-Test](local-coop.md).

## Gemeinsamer Gebietswechsel – umgesetzt am 7. Oktober 2026

1. Waldheiligtum und Rätselhöhle gemeinsam über einen Host-gesteuerten Ablauf laden.
2. Währenddessen Eingaben sperren und laufende Aktionen abbrechen; Verbindung,
   Charakteridentitäten, Inventar und Weltzustand erhalten.
3. Auf Ladebestätigungen der Clients warten, Figuren und Kamera in der neuen
   Szene zuordnen und sichere Spawnpunkte verwenden.
4. Befehle aus dem vorherigen Gebiet verwerfen und erst danach Eingaben freigeben.
5. Hin- und Rückweg mit zwei Prozessen prüfen: Ausrüstung, HP/Mana, Runen und
   Truhenzustand dürfen nicht verloren gehen.

Die bestehende Sperre für Netzwerk-Gebietswechsel nicht einfach entfernen:
Szenenobjekte und die Zuordnungen der Netzwerkfiguren müssen erneuert werden.

Der beschriebene Ladeablauf ist implementiert und im echten Zwei-Prozess-Test
mit Hin-/Rückweg, Equipment, HP, Weltzustand und anschließendem Revive geprüft.
Der manuelle Spieltest steht noch aus; siehe [Details](multiplayer-area-transitions.md).

## Multiplayer-Save/Load und Gruppen-Retry – umgesetzt

Der Host speichert am Buch in drei Slots die Welt und alle Charaktere inklusive
Position, Szene und Spielzeit. Das gemeinsame Laden verwendet den Ablauf des
Gebietswechsels. Clients schreiben keine eigenen Host-Spielstände. Historische
Charakterdaten sind von tatsächlich verbundenen Gruppenmitgliedern zu trennen.
Nach Gruppen-Game-Over muss der Host den gemeinsamen Checkpoint laden können.

## Weitere Mechaniken – Vorschläge zur Reihenfolge

1. Wiederbeschaffung wichtiger Truhengegenstände für spätere Mitspieler festlegen:
   vorhandene gemeinsame Fund-Freischaltungen als Händlerangebot verwenden.
   Preise und Einstiegsausrüstung vorher klären; dies bleibt ein Vorschlag.
2. Gold pro Charakter und hostseitig geprüfte Händlerkäufe zusammen ergänzen.
3. Heil-/Mana-Potions und Verbrauchsgegenstände über den gemeinsamen Ring nutzen.
4. Bogen und Projektile mit hostseitiger Trefferprüfung implementieren.
5. Fortschritt über Items, Erkundung und Zauberränge ausbauen; derzeit kein XP-System.
   Zusätzliche Herzen und Mana bleiben an Herzcontainer und Mana-Kristalle gebunden.
6. Zauber mit Mana, Cooldown und replizierten Effekten ergänzen.
7. Weitere Gegner/Bosse und Itemfortschritt ausbauen: normale Gegner geben
   vorwiegend Verbrauchsmaterial, Bosse und Truhen auch Ausrüstung.
8. Tag/Nacht über eine gespeicherte, synchronisierte Host-Weltzeit ergänzen.
9. Pause-/Sitzungsmenüs und Balancing bearbeiten. Vorschlag: Solo pausiert die
   Welt, im Multiplayer sperrt ein lokales Menü zunächst nur eigene Eingaben.

## Arbeitsregeln

Manuelle Karten- und Assetänderungen erhalten. Szenen und Prefabs mit Unity-APIs
bearbeiten; bei geöffnetem Projekt keinen parallelen CLI-Editor starten.
Generierte IDE-Dateien nicht manuell korrigieren. Library behalten.
Easy Save 3 ist eine lokal installierte Kaufabhängigkeit; Library, Temp, Builds
und das ausgeschlossene Plugin werden nicht ins Git aufgenommen.

Prüfstand 7. Oktober: Der Nutzer hat den gemeinsamen Szenenwechsel bestätigt.
Gäste-Runen, Zustimmung/Ablehnung und gemeinsames Laden bestehen den erweiterten
Zwei-Prozess-Test. Details: [Abfrage und Laden](multiplayer-votes-and-load.md).
Gruppen-Retry nach Game Over ist inzwischen umgesetzt; siehe [Prüfung und Ablauf](party-retry.md).

Truhenstand 7. Oktober: Persönliche, bestätigungspflichtige Belohnungsanzeige
auch auf Clients ist umgesetzt; nur der Empfänger ist geschützt und gesperrt,
die Welt läuft weiter. Jede Truhe gibt ein Item. Gemeinsame Fund-Freischaltungen
liegen in Savegame-Version 8 als Grundlage für den späteren Händler.
Details und bestandener Zwei-Prozess-Test: [Truhenbelohnungen](chest-rewards.md).

## Nächster konkreter Schritt

Update 9. Oktober: Der Nutzer bestätigt den letzten manuellen Test als erfolgreich.
Der vereinbarte Stand wird jetzt strukturiert committed und gepusht; die Prüfung
und weiterhin offenen Grenzfälle stehen in [Prüfstand 9. Oktober](verification-2026-10-09.md).
Vier Teilnehmer, echte Abbrüche und Host-Ende bleiben die nächsten konkreten
Prüfungen. Weitere Mechaniken erst danach festlegen.

Tagesabschluss 8. Oktober: **Manueller Test und Push am 9. Oktober**.
Heute keine weiteren Spieltests, kein Commit und kein Push. Aktuelle Übergabe
mit Prüfablauf und Git-Aufteilung: [8. Oktober](handoff-2026-10-08.md).
Zuerst den letzten Fix für mehrere EXE-Clients am selben Rechner bestätigen:
zu zweit starten, dritten Spieler normal verbinden, Figur erstellen und
reconnecten. Danach Vier-Spieler-Grenzen und gemeinsame Aktionen prüfen.
Der letzte Drei-Prozess-Test mit gemeinsam genutzten Test-PlayerPrefs besteht;
die manuelle Bestätigung steht noch aus. Build: 22:17, Netzwerk-/Save-Schema 13.

Stand 8. Oktober: MainMenu, Host-Lobby, Spielernamen und vier benannte
Charakterplätze pro Multiplayer-Welt sind umgesetzt. Keine lokalen Multiplayer-
Charakterprofile; abwesende Figuren bleiben gespeichert. Singleplayer behält drei
lokale Slots, Multiplayer verwendet drei eigene Speicherplätze. Schema 13 übernimmt
ältere Spielstände. Details: [MainMenu und Lobby](main-menu-and-lobby.md).

Der Nutzer hat den Lobby-Ablauf bestätigt. Gemeinsame Buchbedienung durch jeden
Spieler und die neue Menüführung sind umgesetzt; der Zwei-Prozess-Buchtest besteht.
Details: [gemeinsames Speicherbuch](shared-save-book.md).

Die gemeinsame Buchoberfläche wurde manuell bestätigt. Noch ausstehend:
Vier-Spieler-Test, letzter Rejoin-Fix und Verbindungsabbrüche gezielt prüfen, insbesondere
während Abstimmung, Szenenwechsel, Save/Load und Truhenanzeige. Danach als
Vorschlag Gold/Währung, Trank-Schnellslots und Bogen/Pfeile ausbauen.

Festgehalten und umgesetzt: geschützte Gründerfigur ohne Teilnahmezwang,
Host-Charakterlöschung in der Lobby und Spielstand-Papierkorb im Hauptmenü.
Wiederbeschaffung unverzichtbarer Items ist eine spätere Anforderung;
gebietsgerechte Einstiegsausrüstung bleibt ein Vorschlag. Details:
[Charakter- und Spielstand-Lebenszyklus](character-and-save-lifecycle.md).

Menüs erhalten Waldgrafik und segmentierte Rahmen; die Lobby zeigt vier
Charakterkarten. Neue Figuren werden mit Namen, Körper, Haut, Frisur,
Haar- und Augenfarbe erstellt.
Farben bleiben im Host-Spielstand und werden an Clients übertragen; Namen sind
über den Figuren sichtbar. Details:
[Menüs und Charaktergestaltung](menu-art-and-character-creator.md).

Der Nutzer bestätigt die funktionalen Spieltests für Charaktergestaltung, Lobby,
Save/Load, gemeinsames Buch, Gebietswechsel, Truhe/Tod und Verbindungsabbruch.
Ein Vier-Spieler-Test ist damit nicht separat nachgewiesen. Reconnect und Beitritt
zur laufenden Session sind umgesetzt und im Zwei-Prozess-Test bestanden;
Details und ausstehende manuelle Tests:
[Reconnect](reconnect.md). Doku und Planung sind zum Tagesabschluss aktualisiert.
Nach den morgigen manuellen Tests den Prüfstand ergänzen und den bestätigten
Stand in strukturierten Commits pushen.

Laufender Beitritt erweitert: neue Spieler können im normalen Join-Ablauf eine
Figur in einem freien Host-Weltplatz erstellen. Der zusätzliche Test-Button
entfällt; der Code bleibt erhalten. Erfolgreiche Beitritte werden allen aktiven
Teilnehmern mit dem Spielernamen angezeigt. Details: [Reconnect](reconnect.md).

## Charakterauswahl nach Disconnect – 9. Oktober 2026

Figuren werden beim Disconnect sofort frei. Jeder Rejoin erfordert die explizite
Wahl einer freien Figur; auch eine einzige freie Figur wird nicht automatisch
übernommen. Die Daten aller Figuren bleiben in der Host-Welt. Verbindungsschlüssel
bestimmen keinen Charakterbesitz. Details und Tests: [Reconnect](reconnect.md).
Zwei Disconnects, Rückkehr in anderer Reihenfolge und die einzelne freie Figur
sind automatisiert geprüft. Vier aktive Teilnehmer mit Disconnect und anschließendem
Beitritt eines neuen Spielers zur freien Figur sind manuell bestätigt. Die Ablehnung
eines fünften Spielers ist aus dem vorherigen Build bestätigt.
Das geordnete Beenden der Session durch den Host ist manuell bestätigt:
alle Clients werden sofort getrennt. Der anschließende [LAN-Test](lan-test.md)
ist ebenfalls manuell bestätigt. Offen bleiben gezielte Netzwerkunterbrechungen
und Host-Absturz als eigene Grenzfälle. Die Netzwerk-Testphase ist abgeschlossen;
diese Restpunkte blockieren den nächsten Mechanikausbau nicht. Der genaue Prüfstand
steht in [Prüfung und Netzwerk-Abschluss](verification-2026-10-09.md).
