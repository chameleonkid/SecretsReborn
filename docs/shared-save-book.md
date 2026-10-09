# Gemeinsames Speicherbuch und Menüführung

Stand: 8. Oktober 2026.

Singleplayer und Multiplayer haben jeweils drei Speicherplätze. Multiplayer-Slot 1
behält `SecretsReborn/multiplayer.es3`; Slot 2 und 3 verwenden
`multiplayer-slot-2.es3` bzw. `multiplayer-slot-3.es3`. Bestehende Dateien bleiben
lesbar. Das Buch speichert weiterhin ausschließlich am vorgesehenen Ort.

Jeder lebende aktive Spieler kann das Buch in Interaktionsreichweite öffnen.
Der Host entscheidet atomar, wer es bedient; ein zweiter gleichzeitiger Zugriff
übernimmt die Bedienung nicht. Das Buch öffnet sich für alle Fenster. Auswahl,
Speichern/Laden-Modus, Überschreibbestätigung, Slot-Zusammenfassungen und Ergebnisse
werden vom Host verteilt. Beobachter sehen, wer bedient, und haben keine Eingaberechte.
Clients lesen oder schreiben dafür keine eigenen Savegame-Dateien.

Während das Buch offen ist, sind Bewegung, Gegnerbewegung, Kampf und normale
Inventar-/Interaktionsaktionen gesperrt. Schließen oder Disconnect der bedienenden
Person gibt die Gruppe frei. Stirbt die bedienende Figur oder verlässt sie die
Reichweite, wird ebenfalls geschlossen. Buchbefehle prüfen Absender, Gebiet,
Buchkennung und Revision; alte Bestätigungen oder fremde Bedienung werden verworfen.

Der bedienende Spieler kann speichern oder Laden anfragen. Der Speicherpunkt
bleibt der Host-Punkt, auch wenn ein Gast das Buch bedient. Laden benötigt weiter
die Zustimmung aller aktiven Spieler. Das Buch schließt vor dieser Abstimmung;
Ablehnung kehrt zur laufenden Welt zurück. Eine andere Welt wird über Hauptmenü
und Lobby geöffnet, damit deren Figuren neu zugeordnet werden können.

## Hauptmenü

- Singleplayer: Start New, Load Game (drei Slots), Back.
- Multiplayer: Host Game oder Join Game, Back.
- Host Game: New Game, Load Game (drei Multiplayer-Slots), Back; Spielername.
- Join Game: Spielername, Host-IP, Connect, Back.
- Optionen: zunächst leer, Back.
- Exit.

Ein neues Spiel ersetzt noch keine Datei. Erst Speichern im Buch mit bestätigtem
Überschreiben ersetzt einen belegten Slot. Die vier Charakterplätze gehören zum
jeweils gespeicherten Weltzustand; die drei Slots sind davon unabhängig.

## Manueller Test

1. MainMenu öffnen; alle Untermenüs und Back prüfen.
2. Host und Client starten; als Gast ein Speicherbuch öffnen.
3. Host muss die gleiche Auswahl sehen und darf sie nicht ändern können.
4. Alle drei Slots speichern; bestehende Slots brauchen eine zweite Bestätigung.
5. Als Gast Laden anfragen. Beide müssen zustimmen und gemeinsam laden.
6. Einmal ablehnen; das Abenteuer muss weiterlaufen.
7. Gast während der Buchbedienung trennen; Host muss wieder spielen können.

Der opt-in Zwei-Prozess-Test `SharedBookIntegrationDriver` nutzt separate temporäre
Dateien und prüft Client-Bedienung, Beobachterrechte, drei Slots, Host-Punkt,
gemeinsames Laden und Freigabe beim Disconnect. Berichte:
`Temp/SharedBookFinal-Host.txt`, `Temp/SharedBookFinal-Client.txt`.
Netzwerkprotokoll 8 verlangt den aktuellen Build auf Host und Client.

Savegame-Version 10 ergänzt den Speicherzeitpunkt in UTC und die beim Speichern
aktiven Charaktere mit ID und Name. Beide Slot-Ansichten zeigen Ort, Spieldauer,
lokalen Speicherzeitpunkt und diese Namen. Historische abwesende Figuren bleiben
gespeichert, zählen aber nicht als Teilnehmer des Speicherzeitpunkts. Alte Saves
bleiben lesbar; unbekannte Zeitpunkte/Teilnehmer werden ausdrücklich gekennzeichnet.
Der Host überträgt auch diese Details an Buch-Beobachter; Clients benötigen keine Datei.

Prüfergebnis: Beide Prozesse bestehen. Die gespeicherten Dateien liegen ausschließlich
in einem temporären Testordner. Client-Auswahl, Beobachterrechte, alle drei unabhängigen
Slots, Host-Punkt, gemeinsames Laden und Disconnect-Freigabe wurden geprüft.
Auch der bestehende Koop-Test besteht im finalen Build auf Host und Client
(`Temp/BookRegression-Host.txt`, `Temp/BookRegression-Client.txt`), einschließlich
Truhen, Ausrüstung, Runen, Gebietswechsel, Save/Load, Revive und Gruppen-Retry.
Alle Testprozesse sind beendet. Native Kompilierung und Windows-Build bestehen
mit 0 Fehlern und einer bekannten optionalen Pipeline-Warnung. Abenteuer-Szenen
wurden nicht verändert; die visuelle Menüprüfung steht noch aus.

Zusätzlicher Prüfstand: `Temp/BookMetadata-Host.txt` und
`Temp/BookMetadata-Client.txt` bestehen mit Schema 10. Die gespeicherten Teilnehmer
und Zeitpunkte werden nach Easy-Save-Roundtrip sowie in der Client-Slot-Zusammenfassung
geprüft. Der Client legt keine eigene Slot-Datei an. Domain- und Protokolltests bestehen.
