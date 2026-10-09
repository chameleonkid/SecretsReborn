# Beitritt und Reconnect zu laufenden Sessions

## Verbindliche Regel ab 9. Oktober 2026

Beim Disconnect verschwindet die Figur aus der aktiven Gruppe und ist sofort
für andere Spieler frei. Es gibt keine fünfminütige Reservierung und keine
automatische Wiederübernahme anhand des Verbindungsschlüssels.

Jeder Beitritt zu einer laufenden Session zeigt die freien gespeicherten Figuren.
Der Spieler bestätigt seine Wahl über „Mit Figur beitreten“. Auch wenn genau eine
Figur frei ist, wird sie niemals automatisch zugewiesen. In der Start-Lobby gelten
weiterhin Charakterauswahl und Bereit-Bestätigung. Bei weniger als vier gespeicherten
Figuren kann der beitretende Spieler einen neuen Charakter erstellen.

Die Bestätigung ist erst bei vollständig aufgebauter Netzwerkverbindung möglich.
Der Host prüft jede Auswahl erneut. Aktive Figuren und Figuren, deren Beitritt
bereits bestätigt wurde und noch lädt, sind belegt. Konkurrierende Anfragen können
niemals dieselbe Figur übernehmen. Ein Disconnect während dieser Ladephase gibt
auch diesen Platz sofort frei. Bei echten Netzwerkabbrüchen beginnt die Freigabe,
sobald der Transport den Disconnect erkennt; die Erkennungszeit ist kein zusätzlicher
Reservierungszeitraum.

Die Charakterdaten bleiben vollständig in der Host-Welt: Inventar, Ausrüstung,
Aussehen, HP/Mana, Tod und Position. Auch abwesende Figuren werden beim nächsten
Speichern am Buch erfasst. Disconnect allein speichert nicht automatisch auf Platte.
Ein gestorbener Charakter bleibt nach erneuter Auswahl gestorben und benötigt Revive.
Da freie Figuren gemeinschaftlich auswählbar sind, kann ein anderer Spieler eine
frühere Figur übernehmen. Persönliche Besitzrechte sind derzeit nicht vorgesehen.

## Ablauf und Grenzen

Die Welt läuft beim Beitritt weiter; nur der neue Client lädt das aktuelle Gebiet.
Seine Figur erscheint an einem geprüften Platz nahe dem Host. Ein Ladehandshake
prüft Welt-ID, Charakter-ID, Szene, Gebiets-Epoch und einmalige Ladekennung.
Der Host bindet die Figur erst nach passender Bestätigung ein. Ein zwischenzeitlicher
Gebietswechsel aktualisiert den Ladeauftrag. Host-Ladebestätigungen laufen nach
45 Sekunden aus; der Client wartet maximal 60 Sekunden auf die Aufnahme.

Beitritte warten während Save/Load, Speicherbuch, Abstimmung, Szenenwechsel,
Truhenpräsentation und Game Over. Neue Figuren umgehen keine Gruppenniederlage.
Maximal vier Teilnehmer einschließlich Host; auch wartende Verbindungen zählen.
Nach erfolgreicher Aufnahme erscheint bei allen aktiven Teilnehmern für fünf
Sekunden „Spielername ist beigetreten.“ Der Spielername stammt aus dem Connect-Menü;
der Charaktername ist davon unabhängig.

## Lokale Tests und Verbindungsschlüssel

Mehrere EXE-Instanzen können über `127.0.0.1` zum Editor-Host verbinden.
Prozessübergreifende lokale Profile (1–4) halten ihre Verbindungsschlüssel getrennt,
weil Windows-PlayerPrefs geteilt werden. Das Profil bleibt bis zum Prozessende belegt.
Die Reihenfolge beim erneuten Start bestimmt keine Charakterzuordnung mehr:
jeder Spieler wählt seine Figur selbst. GamerTags sind keine Identitätsprüfung.

Der entfernte Button „Neue Verbindung ohne Wiederübernahme“ bleibt entfernt.
`OpenLobby(..., resume:false)` existiert weiterhin für Entwickler-/Testzwecke
und erzeugt einen anderen Verbindungsschlüssel. Auch damit erfolgt immer die
explizite Charakterauswahl. Ein abgebrochener Versuch überschreibt keine bisherige
Verbindungskennung. Es gibt keine lokalen Charakterdateien.

Netzwerkprotokoll **14** erfordert denselben aktuellen Build auf allen Teilnehmern.
Das Savegame-Schema bleibt **13**; bestehende Spielstände bleiben kompatibel.
Build: `Builds/LocalCoop/SecretsReborn.exe`. Für den Code-Zeitstempel ist
`SecretsReborn_Data/Managed/Assembly-CSharp.dll` maßgeblich; der EXE-Launcher kann
älter datiert sein. Bereits laufende EXEs nach einem Build neu starten.

## Regressionstests

Der opt-in Zwei-Prozess-Test (`--rejoin-host` / `--rejoin-client`, jeweils
`--rejoin-report <Datei>`) prüft abwesende Figuren im Save, sofortige Freigabe,
explizite Auswahl, Warteschlange während einer Host-Operation, aktuelles Gebiet,
Inventar, HP/Mana und Tod, Abbruch und Schutz der belegten Hostfigur.

Der Drei-Prozess-Test (`--late-join-role host|first|third`, jeweils
`--late-join-report <Datei>` im selben temporären Ordner) prüft gemeinsamen lokalen
PlayerPrefs-Bereich, späten Beitritt mit Creator, Optik-Save-Roundtrip, ungültige
Optik, doppelte Erstellung und Meldung an alle. Zusätzlich verlassen beide Gäste
die Session und kehren in umgekehrter Reihenfolge zurück. Beide bestätigen ihre
Figur ausdrücklich; auch die letzte einzelne freie Figur bleibt bis zur Bestätigung
unbesetzt. Alle Saves und PlayerPrefs-Testschlüssel sind isoliert.

Die frühere Reservierungsklasse und deren Tests wurden entfernt: sie beschrieben
die nun verworfene Regel. Die Nachweise vom 8. Oktober betreffen diese alte Regel.
Aktueller Build- und Testnachweis steht in [Prüfstand 9. Oktober](verification-2026-10-09.md).