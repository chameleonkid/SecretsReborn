# Gebietswechsel und Höhle

`Waldheiligtum-Editable` führt nach Abschluss der vier Runen über die nördliche Treppe in `Raetselhoehle-Editable`. Der Rückweg liegt am südlichen Höhlenrand. Die Höhle ist zunächst ein kleiner begehbarer Bereich mit Speicherbuch; ein zusätzliches Höhlenrätsel ist noch offen.

## Bearbeitung

Die Höhle hat unter `World/Grid` die gleichen sieben editierbaren Tilemaps wie das Waldheiligtum. Die native Szene wird einmalig durch `SecretsReborn → World → Set up cave and area transitions` erstellt. Bestehende Höhlen und Wald-Tiles werden nicht neu aufgebaut.

Unter `World/Transitions` liegen `AreaPortal` und `AreaEntrance`. Ein Portal verweist auf den vollständigen Szenenpfad und die eindeutige ID des Eintrittspunktes in der Zielszene. Die zugehörigen Portal-Prefabs liegen unter `Assets/World/Cave/Prefabs`. Spawnpunkte liegen außerhalb der Portal-Trigger; eine kurze Sperrzeit schützt vor sofortigem Rückwechsel.

## Zustand und Koop

`GameSession.RequestAreaChange` ist der zentrale Einstieg. Die Host-Session bleibt über den Szenenwechsel erhalten; neue Spielerinstanzen binden sich an dasselbe Charakterinventar. Gesammelte Gegenstände und Rätselstand kommen aus dem Weltzustand. Savegames speichern weiterhin Szene, Position und Spielzeit; ein Höhlen-Save kann vom Wald-Buch aus geladen werden.

Seit dem 7. Oktober 2026 unterstützt die Netzwerksitzung gemeinsame Host-gesteuerte Gebietswechsel. Alle Clients bestätigen das Laden, bevor Bewegung und Aktionen wieder freigegeben werden. Charakteridentitäten, Ausrüstung und Weltzustand bleiben erhalten. Details und Prüfstand: [Multiplayer-Gebietswechsel](multiplayer-area-transitions.md). Multiplayer-Savegame-Laden und eine verbindliche Zustimmung aller Spieler sind ebenfalls umgesetzt; siehe [Abfrage und Laden](multiplayer-votes-and-load.md). Gruppen-Retry folgt als nächster Schritt.

## Manueller Test

1. Waldheiligtum starten, einen Gegenstand aufnehmen und die vier Runen lösen.
2. Die nördliche Treppe hinaufgehen: Höhle öffnet sich, Ausrüstung und Inventar bleiben erhalten.
3. Am Höhlenbuch speichern. Den südlichen Ausgang betreten und im Wald Inventar und Quelle prüfen.
4. Am Wald-Buch den Höhlen-Slot laden: Höhle und gespeicherte Position müssen wiederhergestellt sein.
5. Play beenden und neu starten: erst das Laden des Slots stellt die gespeicherte Welt wieder her.
