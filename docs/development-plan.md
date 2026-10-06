# Entwicklungsplan

Stand: 6. Oktober 2026. Schwerpunkt bleibt die Weiterentwicklung der Mechaniken.
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

## Nächster konkreter Schritt: gemeinsamer Gebietswechsel

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

## Danach: Multiplayer-Save/Load und Gruppen-Retry

Der Host speichert am Buch in drei Slots die Welt und alle Charaktere inklusive
Position, Szene und Spielzeit. Das gemeinsame Laden verwendet den Ablauf des
Gebietswechsels. Clients schreiben keine eigenen Host-Spielstände. Historische
Charakterdaten sind von tatsächlich verbundenen Gruppenmitgliedern zu trennen.
Nach Gruppen-Game-Over muss der Host den gemeinsamen Checkpoint laden können.

## Weitere Mechaniken – Vorschläge zur Reihenfolge

1. Vier Spieler, Rejoin, Verbindungsabbruch und verständliche Rückmeldungen zu
   vom Host abgelehnten Aktionen prüfen.
2. Runen durch jeden geeigneten Spieler aktivierbar machen; Truhenpräsentation
   auf allen Clients anzeigen und gleichzeitige Interaktionen prüfen.
3. Gold als Währung, Verbrauchsgegenstände und Pfeile als Munition ergänzen;
   optionale Schnellslots für Controller vorsehen.
4. Bogen und Projektile mit hostseitiger Trefferprüfung implementieren.
5. Eigene XP und Fortschritt ausbauen. Level-Effekte vorher festlegen;
   zusätzliche Herzen bleiben zunächst an Herzcontainer gebunden.
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
