# Multiplayer: Zustimmung und Laden

Stand: 7. Oktober 2026.

## Gemeinsame Entscheidung

Beim Betreten eines freigeschalteten Portals fragt der Host alle verbundenen
Spieler einschließlich sich selbst. Jeder bestätigt mit Enter / Controller A
oder per Button. Esc / B bzw. Nein bricht für alle ab. Erst wenn sämtliche
verbundenen Spieler zustimmen, beginnt der gemeinsame Szenenwechsel.

Während der Abfrage sind Bewegung, Kampf und Interaktionen angehalten. Die
Bestätigung eines einzelnen Spielers genügt nicht. Nach 30 Sekunden ohne
vollständige Zustimmung oder bei einem Disconnect wird abgebrochen. Neue
Spieler können während Abfrage und Laden nicht beitreten.

Nach einer Ablehnung das Portal verlassen und erneut betreten, um neu anzufragen.

## Savegame laden

Der Host öffnet das Speicherbuch am vorgesehenen Ort und wählt einen der drei
Slots zum Laden. Anschließend erhalten alle Spieler eine Ladeabfrage. Erst nach
Zustimmung aller ersetzt der Host den gemeinsamen Weltzustand und lädt die
Zielszene. Auch beim Laden derselben Szene werden die Figuren neu zugeordnet.

Inventare, Ausrüstung, Vitals, Rätsel-/Truhenzustand, Szene, Host-Position und
Spielzeit kommen aus dem gespeicherten Zustand. Alle aktiven Gäste starten an
freien Plätzen neben dem geladenen Host-Punkt; ihre früheren Szenenkoordinaten
werden beim Laden nicht als Spawn verwendet. Die aktuellen Verbindungen und
aktiven Charakter-IDs bleiben erhalten. Historische Charaktere im Savegame
werden nicht automatisch zu aktiven Spielern. Ein verbundener Gast, der im
älteren Spielstand noch nicht vorkommt, erhält zunächst den Standardzustand
seiner ID und einen Spawn nahe dem Host. Lampen starten nach dem Laden aus.

Clients schreiben oder lesen keine Host-Savegames. Das Buch wird weiterhin vom
Host bedient; ein Client kann dem Laden zustimmen oder es ablehnen.
Gruppen-Retry nach Game Over verwendet denselben gemeinsamen Ladeablauf; siehe [Neustart](party-retry.md).

## Runen

Die Aktivierung wird auf dem Host für alle lebenden, handlungsfähigen Figuren
geprüft. Gäste benötigen wie der Host eine angelegte, eingeschaltete magische
Lampe und müssen den jeweiligen Kreis betreten. Die Reihenfolge ist gemeinsamer
Weltzustand. Mehrere Figuren im gleichen Kreis lösen beim selben Eintritt keine
doppelte Aktivierung aus. Zeichen werden pro Fenster anhand der eigenen Lampe
und Entfernung angezeigt.

Host und Client benötigen Netzwerkprotokoll Version 5 und denselben aktuellen
Build. Die editorseitige Karte und bestehende Prefabs werden nicht neu aufgebaut.

## Prüfstand

Runtime-Kompilierung und Protokolltests bestehen. Native Editor-Kompilierung und
Windows-Build bestehen mit 0 Fehlern und einer bekannten optionalen Pipeline-Warnung.
Der echte Zwei-Prozess-Test besteht auf Host und Client:
`Temp/CoopVoteLoadV2-Host.txt`, `Temp/CoopVoteLoadV2-Client.txt`.

Geprüft wurden alle vier echten Runenkreise durch eine Gastfigur mit magischer
Lampe, Warten auf die noch ausstehende Host-Zustimmung, Abbruch durch Nein,
anschließender gemeinsamer Hin-/Rückweg und Laden eines tatsächlich mit Easy
Save geschriebenen Temp-Testspielstands. Das Laden stellt eigenes Equipment,
Vitals und Weltmarker wieder her und entfernt Änderungen nach dem Speichern.
Danach wurden erneut Client-Revive, Gruppen-Game-Over und Disconnect geprüft.
Beide Prozesse sind beendet; bestehende Benutzer-Spielstände sind unverändert.

Regression: Der Host erkennt Ladebestätigungen auch dann, wenn JsonUtility den
fehlenden Eintrittspunkt als leeren Text überträgt. Veraltete Bestätigungen und
abweichende Zielszene werden weiterhin abgewiesen.

Manueller Spieltest der neuen Abfrage und des Buch-Ladens steht noch aus.
Vier Spieler, Timeout/Disconnect mitten in der Abfrage und ältere Spielstände
mit noch nicht gespeicherten verbundenen Gästen benötigen weitere gezielte Tests.

Spawn-Regression: Nach dem Laden sammelt sich die gesamte aktive Gruppe am
Host-Punkt. Der Zwei-Prozess-Test speichert den Gast absichtlich 20 Einheiten
entfernt und prüft anschließend einen Spawn neben dem Host (maximal 1,6 Einheiten)
bei erhaltener eigener Ausrüstung. Host und Client bestehen:
`Temp/CoopRegroup-Host.txt`, `Temp/CoopRegroup-Client.txt`.
Der aktualisierte native Windows-Build besteht; die Testprozesse sind beendet.
