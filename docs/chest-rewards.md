# Truhenbelohnungen im Solo- und Koop-Spiel

Stand: 7. Oktober 2026.

- Jede Truhe enthält genau einen Gegenstand (eine Loot-Zeile, Anzahl 1).
  Die bestehende Heiligtum-Truhe gibt die Rüstung. Gegner-/Boss-Loot bleibt getrennt.
- Der Host vergibt den Gegenstand einmalig an den öffnenden Charakter. Ist dessen
  Inventar voll, bleibt die Truhe geschlossen; es beginnt keine Belohnungsanzeige.
- Item-Sprite und Text erscheinen über der Truhe im Fenster des Empfängers,
  einschließlich eines Client-Spielers. Die Truhe erscheint für alle geöffnet.
- Die Welt läuft weiter. Nur der Empfänger ist unbeweglich, kann nicht angreifen,
  erhält keinen Schaden und wird von Gegnern nicht als Ziel gewählt.
- A / X am Controller, Enter oder der Bestätigungsbutton beendet die Anzeige.
  Das Öffnen bestätigt sie nicht im selben Moment; auch X löst dabei keinen
  zusätzlichen Schwertangriff aus. Erst die Hostbestätigung gibt Clients frei.
- Mehrere Spieler können an verschiedenen Truhen gleichzeitig Belohnungen lesen.
  Vor gemeinsamem Gebietswechsel/Laden müssen offene Belohnungen bestätigt sein.

## Persistenz und spätere Händler

Geöffnete Truhen und vergebene Items bleiben gespeichert. Zusätzlich hält die
Host-Welt eine eindeutige Liste gefundener Truhen-Item-IDs. Diese Freischaltung
bleibt unabhängig vom aktuellen Besitzer erhalten und wird mit Save/Load sowie
Snapshots übertragen. Der Händler selbst ist noch nicht implementiert.

Savegame-Version ist jetzt 8; frühere Spielstände bleiben ladbar und beginnen
mit einer leeren Freischaltungsliste. Die vorübergehende Anzeige/Schutzphase wird
nicht gespeichert. Ein Disconnect entfernt den Schutz und die Anzeige der
abgemeldeten Figur; bereits vergebene Items bleiben in ihrem Host-Welt-Inventar.

Vorschlag für später: Händler bieten die gemeinsam freigeschaltete Ausrüstung
jedem Charakter zum Kauf an. Notwendige Abenteuergegenstände sollen stattdessen
unmittelbar den gemeinsamen Weltfortschritt freischalten.

Host und Client benötigen den aktualisierten Build mit Netzwerkprotokoll 5.

## Manueller Test

1. Mit Host und Client eine noch ungeöffnete Truhe verwenden. Falls sie im
   aktuellen Spielstand bereits geöffnet ist, einen passenden älteren Slot laden.
2. Der Client öffnet: Sprite/Text müssen in seinem Fenster erscheinen. Er darf
   weder laufen noch angreifen; der Host kann normal weiterspielen.
3. Mit A oder X bestätigen. Danach muss der Client wieder beweglich/angreifbar sein.
4. Speichern und Laden: Truhe bleibt geöffnet, Item und Fund-Freischaltung bleiben erhalten.

## Prüfstand

Runtime-/Editor-Kompilierung, Weltzustands-/Migrationstests und Protokolltests
bestehen. Der native Windows-Build besteht mit 0 Fehlern und einer bekannten
optionalen Pipeline-Warnung. Unity hat die vorhandene Chest-Loot-Tabelle auf
ihre erste einzelne Belohnung normalisiert; die Szenen wurden nicht verändert.

Der echte Zwei-Prozess-Test besteht auf Host und Client:
`Temp/CoopChest-Host.txt`, `Temp/CoopChest-Client.txt`. Er öffnet die Truhe über
eine Client-Anfrage, prüft die erzeugte Item-/Textdarstellung auf dem Client,
verhinderten Schaden und Bewegung des Empfängers bei weiterlaufender Weltzeit,
Handlungsfähigkeit des Hosts, Bestätigungs-RPC, einmalige Belohnung und die
replizierte Fund-Freischaltung. Anschließend bestehen weiterhin Gäste-Runen,
Gruppenabfrage, Szenenwechsel, echtes Easy-Save-Laden, Revive und Game Over.
Beide Testprozesse sind beendet. Der manuelle Spieltest steht noch aus.

Die Schema-Tests prüfen gespeicherte Fund-Freischaltungen, Migration älterer
Spielstände und Ablehnung doppelter Item-IDs. Gleichzeitige Anzeigen an mehreren
Truhen und vier Spieler sind noch nicht gesondert interaktiv geprüft.

Physikschutz: Während der Anzeige friert der Host auch Position und Rotation
(Rigidbody2D-Constraints) des Empfängers ein. Damit können Mitspieler und Gegner
ihn durch Kollisionen nicht verschieben. Bestätigung bzw. Deaktivierung stellt
die vorherigen Constraints wieder her. Der Zwei-Prozess-Test mit zwei schweren,
überlappenden Kollisionskörpern auf unterschiedlichen Achsen besteht auf beiden
Seiten (`Temp/CoopChestFreeze-Host.txt`, `Temp/CoopChestFreeze-Client.txt`).
Auch die Wiederherstellung nach Bestätigung wurde geprüft. Der native Build
besteht mit 0 Fehlern; beide Testprozesse sind beendet.
