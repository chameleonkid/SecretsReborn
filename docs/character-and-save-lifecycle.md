# Charaktere, Löschungen und Schutz vor Sackgassen

Festlegung: 8. Oktober 2026.

## Verbindliche Regeln

- Weltfortschritt und persönlicher Charakterfortschritt bleiben getrennt.
  Geöffnete Wege, gelöste Rätsel und besiegte Bosse gehören der Host-Welt.
- Jede Multiplayer-Welt hat bis zu vier feste Charakterplätze und eine geschützte
  Gründerfigur. Erste erstellte Figur wird Gründer; bei älteren Welten wird der
  gespeicherte Host-Charakter bevorzugt, sonst die erste vorhandene Figur.
- Die Gründerfigur ist nicht löschbar. Sie muss nicht aktiv mitspielen und ist
  nicht an eine bestimmte Person oder den aktuellen Host gebunden. Keine Welt-
  Freischaltung darf allein von ihrer Teilnahme abhängen.
- Nur der Host darf Figuren löschen, ausschließlich in der Lobby und nach einer
  ausdrücklichen Bestätigung mit Name und Verlust von Inventar/Ausrüstung/Fortschritt.
  Eine von irgendeinem Spieler ausgewählte Figur muss zunächst freigegeben werden.
- Löschung gibt einen Charakterplatz frei; gemeinsamer Weltfortschritt bleibt bestehen.
  Nach einer Löschung müssen die Spieler ihre Bereitschaft neu bestätigen.
- Charakterlöschung betrifft zunächst die Session und wird beim nächsten Speichern
  am Buch dauerhaft. Ein Abbruch ohne Speichern ändert die vorhandene Datei nicht.
- Ältere Spielstände stellen ihren damaligen Zustand wieder her, einschließlich
  damals vorhandener, später gelöschter Figuren. Keine globale Löschliste außerhalb
  der Weltdatei. Wenn aktive neue Figuren nicht mehr in die vier Plätze eines alten
  Saves passen, wird Laden vor der Abstimmung abgewiesen; über Hauptmenü und Lobby
  lässt sich dessen damalige Charakterzuordnung wählen.
- Spielstandlöschung ausschließlich im Hauptmenü, nicht im Buch und nicht durch
  Clients einer aktiven Partie. Bestätigung zeigt Slot und gespeicherte Details.
  Löschen verschiebt Datei und Easy-Save-Backup in einen lokalen Papierkorb.
- Papierkorb ist im Laden-Untermenü erreichbar. Wiederherstellung schreibt nur in
  den freien ursprünglichen Slot und überschreibt niemals einen belegten Slot.
  Kein automatisches Leeren und zunächst keine endgültige Löschfunktion.

## Spätere Anforderungen, noch nicht implementiert

Für den weiteren Weg unverzichtbare Gegenstände müssen wiederbeschaffbar sein.
Vorgesehener Ansatz: Sobald eine Gegenstandsart in der Welt entdeckt wurde, kann
sie erneut erworben werden, beispielsweise eine magische Lampe beim Händler.
Das gilt auch für Mitspieler, die die ursprüngliche Truhe nicht geöffnet haben.
Die bereits gespeicherten gemeinsamen Fund-Freischaltungen sind die Grundlage;
Händler, Preise und konkrete Wiederbeschaffung fehlen noch.

Neue Charaktere brauchen in fortgeschrittenen Welten einen brauchbaren Einstieg.
Vorschlag: Gebietsgerechte Startausrüstung oder ein günstiger Einstiegssatz beim
Händler. Umfang, Werte und Kosten sind noch offen; keine automatische Angleichung
von XP/Herzen/Equipment implementiert.

Vor weiteren Gebieten prüfen: Kein erforderlicher Weg darf allein durch Löschung,
Abwesenheit einer Figur oder Verlust eines einzelnen Gegenstands dauerhaft blockiert
werden. Die geschützte Gründerfigur allein garantiert das nicht.

## Umsetzung und Prüfung

Savegame-Schema 11 speichert die feste Gründer-ID. Ältere Versionen bleiben lesbar.
Netzwerkprotokoll 9 zeigt die Gründerkennzeichnung auch auf Clients.
Löschrechte und Reservierungen werden auf dem Host erneut geprüft, nicht nur im UI.

Domain-Test prüft Gründer-Schutz, Entfernen persönlicher Daten, freien Ersatzplatz,
Migration und Persistenz der Gründer-ID. Der Lobby-Zwei-Prozess-Test prüft Host-
Löschung und Ablehnung bei Gründer, ausgewähltem Gast und Client. Der Menü-Test
prüft Papierkorb, Wiederherstellung einschließlich Gründerzustand sowie Schutz
belegter Slots mit temporären Dateien. Vorhandene Spielstände werden nicht verändert.

Prüfergebnis: Domain- und Protokolltests bestehen. `Temp/DeletionMenu.txt` besteht
mit Papierkorb-/Restore-Roundtrip, Schutz belegter Slots und erneutem Host/Join.
`Temp/DeletionLobby-Host.txt` und `Temp/DeletionLobby-Client.txt` bestehen mit
Gründer-Schutz, Host-Löschung, Reservierungsprüfung und abgewiesener Client-Löschung.
Der native Unity-Import und Windows-Build bestehen mit 0 Fehlern und einer bekannten
optionalen Pipeline-Warnung. Die visuelle Prüfung der neuen Löschdialoge steht noch aus.
`Temp/FounderBook-Host.txt` und `Temp/FounderBook-Client.txt` bestehen ebenfalls:
gemeinsames Speichern/Laden und Slot-Metadaten funktionieren mit Schema 11 weiterhin.
Alle Testprozesse sind beendet; Abenteuer-Szenen wurden nicht verändert.
