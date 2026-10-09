# Beitritt und Reconnect zu laufenden Sessions

Update 9. Oktober: Der Nutzer bestätigt den zuletzt vereinbarten manuellen Test
mit mehreren lokalen Clients als erfolgreich. Weitere Grenzfälle bleiben offen;
siehe [Prüfstand und nächste Schritte](verification-2026-10-09.md).

Eine aktive Multiplayerwelt akzeptiert Clients aus dem Join-Menü. Der Host
führt die Welt weiter; nur der beitretende Client lädt das aktuelle Gebiet.
Die maximale Teilnehmerzahl bleibt vier einschließlich Host. Verbindungen in
der Charakterauswahl oder beim Laden zählen ebenfalls gegen dieses Limit.

Beim Disconnect wird die Figur aus der Szene und der aktiven Gruppe entfernt.
Ihr Host-Weltprofil, Inventar, Ausrüstung, Aussehen, HP/Mana und letzte Position
bleiben erhalten. Das nächste Speichern am Buch erfasst auch abwesende Figuren.
Es gibt weiterhin kein automatisches Speichern allein durch Disconnect.

Der Client behält einen zufälligen Verbindungsschlüssel pro Host-IP. Der Host
reserviert die zuletzt gespielte Figur fünf Minuten ab Disconnect für diesen
Schlüssel. Ein normaler Connect übernimmt diese Figur automatisch, falls sie
noch reserviert ist. Sonst zeigt der Client freie gespeicherte Figuren mit
Portraits; belegte oder für andere Verbindungen reservierte Figuren sind gesperrt.
GamerTags sind Anzeigenamen und übernehmen keine Identitätsprüfung.

## Mehrere Spieler auf einem Rechner testen

Editor als Host starten; zwei zusätzliche EXE-Instanzen über den normalen
Connect mit `127.0.0.1` verbinden. Unterschiedliche Spielernamen erleichtern
die Zuordnung, ersetzen jedoch keine getrennten Verbindungsschlüssel.
Windows-PlayerPrefs werden von allen Instanzen desselben Benutzerkontos geteilt.
Deshalb belegt jeder Client beim ersten Connect ein freies lokales Testprofil
(1–4) über eine prozessübergreifende Sperre. Profil 1 verwendet den bisherigen
Schlüssel; weitere Profile verwenden getrennte Schlüssel. Die Sperre bleibt
beim Verlassen einer Session erhalten und wird beim Beenden freigegeben;
auch nach einem Prozessabsturz kann der Platz wieder benutzt werden.

Dadurch funktioniert Reconnect innerhalb jeder Instanz, ohne eine andere aktive
Figur zu übernehmen. Nach dem Schließen aller Instanzen die Clients für dieselbe
Zuordnung in derselben Reihenfolge verbinden. Die Profile sind ausschließlich
lokale Verbindungseinstellungen; sämtliche Figuren bleiben in der Host-Welt.
Ein erstmaliger Beitritt während des laufenden Spiels öffnet die Auswahl mit
„Neue Figur erstellen“, sofern noch weniger als vier Weltfiguren existieren.
Ablehnungen zeigen ihren Grund nach der Rückkehr ins Hauptmenü an.

Der ursprüngliche Drei-Prozess-Test verwendete getrennte Testschlüssel und
deckte gemeinsam genutzte PlayerPrefs nicht ab. Der erweiterte Test verwendet
denselben isolierten PlayerPrefs-Bereich für beide Clients, normale Connects,
prüft unterschiedliche lokale Profile und zusätzlich den Reconnect des dritten
Spielers zu seiner neu erstellten Figur.
Prüfstand 8. Oktober, Build 22:17: alle drei Prozesse bestehen. Der erste Client
verwendet Profil 1, der zweite Profil 2; der späte Beitritt mit Erstellung und
anschließendem Reconnect zur selben Figur funktioniert. Player-Build: 0 Fehler.

Der zusätzliche Button „Neue Verbindung ohne Wiederübernahme“ wurde aus dem
regulären Join-Menü entfernt. `OpenLobby(..., resume:false)` bleibt als
Entwickler-/Testfunktion erhalten und erzeugt einen anderen Schlüssel, etwa
für mehrere Test-Clients auf einem Rechner. Es gibt keine neuen
lokalen Charakterdateien; gespeichert wird nur der Verbindungsschlüssel.
Der neue Schlüssel ist zunächst vorläufig: erst wenn der Host die tatsächlich
gespielte Figur im Weltzustand bestätigt, ersetzt er den bisherigen Rejoin-Schlüssel.
Ein Abbruch der Auswahl oder ein fehlgeschlagener Connect erhält damit den
normalen Rejoin zur zuvor gespielten Figur. Ist keine Figur verfügbar, erläutert
die Auswahl belegte Plätze und die bis zu fünf Minuten dauernde Reservierung.
Wurde der Schlüssel bereits mit der vorherigen Version überschrieben, nach Ablauf
der alten Reservierung die gewünschte freie Figur erneut auswählen. Erst deren
erfolgreiche Übernahme speichert die neue Zuordnung für den normalen Rejoin.
Reservierungen sind an die Host-Sitzung und Welt gebunden. Nach einem Host-Neustart
ist die gespeicherte Charakterauswahl verfügbar, aber keine alte Reservierung.

Die Figur erscheint an einem geprüften Platz nahe dem aktuellen Host, nicht an
historischen Koordinaten. HP/Mana werden nicht zurückgesetzt. Ein gestorbener
Charakter bleibt gestorben und benötigt Revive. Beitritte warten während
Save/Load, Speicherbuch, Abstimmung, Szenenwechsel, Truhenanzeige und Game Over;
ein neuer lebender Charakter darf die Niederlage der Gruppe nicht umgehen.

Ein eigener Ladehandshake enthält Welt-ID, Charakter-ID, Gebiet, Gebiets-Epoch
und einmalige Ladekennung. Der Host bindet die Figur erst nach passender Bestätigung
ein. Wenn die Gruppe zwischenzeitlich das Gebiet wechselt, lädt der wartende
Client den neuen Stand. Die Gruppe wartet dafür nicht auf den neuen Client.
Ladebestätigungen laufen nach 45 Sekunden aus; der Client wartet höchstens
60 Sekunden auf Bestätigung. Ein Abbruch gibt den wartenden Verbindungsplatz frei.

Der normale Connect unterstützt sowohl Lobby als auch laufendes Spiel. Ohne
reservierte Figur zeigt er gespeicherte Figuren und bei weniger als vier Figuren
„Neue Figur erstellen“. Der Creator bietet dieselbe Körper-, Haut-, Haar- und
Augenauswahl wie die Lobby. Rüstung wird weiterhin ausschließlich angelegt.
Der Host prüft Welt, Optik, Namen und verfügbare Plätze und legt genau eine Figur
für den wartenden Client an; doppelte Anfragen erzeugen keine weiteren Figuren.
Eine neue Figur bleibt nach ihrer Erstellung auch bei Ladeabbruch in der Host-Welt
erhalten und kann beim nächsten Versuch ausgewählt werden. Sie wird beim nächsten
Speichern erfasst. Ein späteres Verfahren zur Wiederbeschaffung wichtiger Items
bleibt offen; neue Figuren erhalten keine fremde Ausrüstung oder Weltfortschritt.

Nach erfolgreicher Aufnahme zeigt der Host allen aktiven Teilnehmern
„Spielername ist beigetreten.“ für fünf Sekunden an. Verwendet wird der beim
Connect eingegebene Spielername, nicht der separat vergebene Charaktername.
Die Meldung erscheint auch beim Rejoin und beim ersten gemeinsamen Spielstart,
aber nicht bei jedem Gebietswechsel. Fehlgeschlagene Versuche lösen sie nicht aus.

Netzwerkprotokoll 13 erfordert denselben Build auf allen Teilnehmern; das
Spielstandschema bleibt 13.

Tests: `tests/ReconnectChecks.cs` prüft Reservierung, Ablauf, Weltisolation und
Schlüsselvalidierung. Der opt-in Zwei-Prozess-Test (`--rejoin-host` / `--rejoin-client`,
jeweils `--rejoin-report <Datei>`) prüft abwesende Figuren im Save, Wiederübernahme,
Beitritt nach Host-Gebietswechsel, Warteschlange, HP/Mana, Tod und freie Auswahl
mit einem neuen Schlüssel. Er benutzt ausschließlich temporäre Save-Dateien.

Der Drei-Prozess-Test verwendet `--late-join-role host|first|third` und
`--late-join-report <Datei>` in einem gemeinsamen temporären Ordner. Er startet
mit zwei Figuren, lässt den dritten Spieler über den normalen Connect eine Figur
erstellen und prüft die Meldung auf allen drei Teilnehmern, Ablehnung ungültiger
Optik, Schutz vor doppelter Erstellung und den Save-Roundtrip der neuen Optik.
Stand 8. Oktober, Build 22:09: alle drei Testprozesse bestehen, Protokollchecks bestehen,
nativer Import und aktualisierter Player-Build bestehen (0 Fehler).
Auch der bestehende Zwei-Prozess-Reconnect-Test besteht erneut. Die Netzwerkanfrage
unterscheidet Erstellen und Auswählen ausdrücklich per Flag, weil JsonUtility
leere Unterobjekte statt `null` übertragen kann.

Manuell noch prüfen: echte Netzwerkunterbrechung, Client-Neustart,
vier Teilnehmer und Abbruch während des Beitritts.

Prüfstand 8. Oktober: Reservierungs- und Protokollchecks bestehen. Native
Editor-Kompilierung und Player-Build bestehen (0 Fehler). Der Zwei-Prozess-Test
besteht auf Host und Client: abwesende Figuren werden gespeichert; Reconnect
wartet während einer Host-Operation, übernimmt die Figur im aktuellen Gebiet
und erhält Inventar, HP/Mana sowie Tod. Eine neue Verbindung kann keine
reservierte Figur übernehmen und kann eine freie Figur wählen.
Der erneute Menütest besteht ebenfalls: wiederholtes Host/Join-Abbrechen,
Spielstand-Papierkorb, Backup und Schutz belegter Wiederherstellungsplätze.
Zusätzlicher Regressionstest: Eintritt ohne Wiederübernahme, gesperrte Figur
auswählen, abbrechen und normal verbinden übernimmt wieder die ursprüngliche
Figur. Anschließender frischer Beitritt kann weiterhin eine andere freie Figur
übernehmen. Host und Client bestehen; aktualisierter Build vom 8. Oktober, 20:17.
Die opt-in Reconnect-Testprozesse verwenden eigene PlayerPrefs-Schlüssel und
verändern keine regulären Rejoin-Zugangsdaten.

Der aktualisierte Build liegt unter `Builds/LocalCoop/SecretsReborn.exe`.
Editor und Player müssen beide Protokoll 13 verwenden. Ein älterer Player mit
Protokoll 11 oder 12 verursacht `NetworkConfig mismatch`; bereits gestartete EXEs
müssen nach einem Build beendet und erneut gestartet werden. Die Code-Version
ist am Zeitstempel von `SecretsReborn_Data/Managed/Assembly-CSharp.dll` erkennbar;
die EXE selbst ist ein Launcher und kann einen älteren Zeitstempel behalten.

Beim Verlassen wird die NGO-Abmeldung abgeschlossen, bevor Transport und
Session zerstört werden. Sofortiges Zerstören hatte das Disconnect-Paket
verworfen und verhinderte im Test die anschließende Verbindung. Der Test
veröffentlicht seine Prozess-Signale erst nach dem Schließen der Datei, damit
der andere Prozess sie unter Windows zuverlässig entfernen kann.
