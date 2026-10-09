# Gemeinsame Multiplayer-Gebietswechsel

Stand: 7. Oktober 2026. Die Netzwerkadapter-Erweiterung liegt in
`Assets/Scripts/NetworkCoopArea.cs`. Die Karte und Portal-Prefabs bleiben unverändert.

## Ablauf

Ein lebender Host oder Gast kann auf dem Host ein freigeschaltetes Portal betreten.
Die bereits vorhandenen Bedingungen zu Entfernung, Rätsel und Menüs gelten weiter.
Damit wird die gesamte verbundene Gruppe ins Zielgebiet versetzt. Vor dem Wechsel fragt der Host alle Spieler. Dies ist jetzt
eine verbindliche Abstimmung: Nur nach ausdrücklicher Zustimmung aller verbundenen Spieler beginnt das Laden.

1. Der Host hält Bewegung, Angriffe und Revive an und sendet Zielszene,
   Eintrittspunkt und eine neue Gebietskennung.
2. Host und Clients laden über ihre bestehende Verbindung. Die Session und der
   Weltzustand bleiben erhalten; die Szene wird nicht erneut erzeugt.
3. Der Host bindet die neue Basisfigur an seine bisherige ID und erzeugt die
   Gastfiguren mit ihren bestehenden IDs. Gäste werden auf freien Positionen
   nahe dem Eintrittspunkt platziert; falls dort kein Kandidat frei ist, wird
   der gemeinsame Eintrittspunkt verwendet.
4. Clients konfigurieren ihre neue Basisfigur als lokale Darstellung ohne
   Zustandsautorität. Jeder bestätigt dem Host das abgeschlossene Laden.
5. Erst nach allen Bestätigungen sendet der Host den neuen vollständigen Zustand
   und gibt die Gruppe frei. Clients folgen wieder ihrer eigenen Figur.

Verspätete Befehle und Snapshots aus einem früheren Gebiet werden verworfen,
auch beim späteren Rückweg in dieselbe Szene. Neue Verbindungen werden während
laufender Übergänge abgewiesen. Ein nachträglich beitretender Gast übernimmt
beim ersten Snapshot die aktuelle Gebietskennung.

Ein Client ohne Ladebestätigung wird nach 30 Sekunden getrennt, damit der Rest
weiterspielen kann. Ein Client wartet höchstens 60 Sekunden auf die Hostfreigabe.
Fehlende Eintrittspunkte oder Ladefehler beenden die lokale Verbindung und führen
zum bestehenden Solo-Neustart; es erfolgt dabei kein automatisches Speichern.

## Abgrenzung

Netzwerkprotokoll ist jetzt Version 5. Host und Client benötigen denselben neu
gebauten Stand. Multiplayer-Laden ist ebenfalls angebunden; siehe [Zustimmung und Laden](multiplayer-votes-and-load.md). Gruppen-Retry ist ebenfalls umgesetzt;
dieser verwendet denselben gemeinsamen Ladeablauf; siehe [Gruppen-Retry](party-retry.md).

## Prüfung

Die Runtime-Kompilierung und Protokolltests bestehen, einschließlich Ablehnung einer
alten Attack-Anfrage nach einem Szenen-Rundweg. Der native Editor-Import ist
abgeschlossen. Der Windows-Build besteht mit 0 Fehlern und einer bereits bekannten
Warnung zur optionalen Unity-Pipeline-Konfiguration. Der erweiterte Zwei-Prozess-Test
besteht auf Host und Client (`Temp/CoopArea-Host.txt`, `Temp/CoopArea-Client.txt`).
Beide Testprozesse sind beendet. Der manuelle Spieltest steht noch aus.

Der Development-Test fährt nach dem Ausrüstungscheck in die Höhle und zurück.
Er prüft Verbindungs-/Charakterzuordnung, eigenes Equipment, HP, persistente
Test-Rätsel-/Truhenmarker und anschließend weiterhin Revive und Game Over.
Diese Marker sind ausschließlich Testzustand und werden nicht gespeichert.

Manuell: neue Client-Version bauen, Editor als Host und Build als Client starten,
Runen lösen und das nördliche Portal betreten. Beide Fenster müssen in der Höhle
ankommen; danach gemeinsam zurückgehen und Ausrüstung, Quelle und Truhe prüfen.
