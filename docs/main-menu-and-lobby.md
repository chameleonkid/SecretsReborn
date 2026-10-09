# MainMenu und Host-Lobby

Stand: 8. Oktober 2026. Erste funktionale Oberfläche; Gestaltung und ausführliche
Controller-Navigation können anschließend verfeinert werden.

## Festgelegtes Modell

- Keine vier lokalen Multiplayer-Profile. Eine Multiplayer-Welt gehört dem Host.
- Ein Multiplayer-Speicherplatz enthält Weltzustand und maximal vier benannte
  Charakterplätze mit festen IDs, Inventar, Ausrüstung und persönlichem Fortschritt.
- Bis zu vier verbundene Spieler einschließlich Host wählen diese Figuren in der Lobby.
- Spielername bezeichnet die verbundene Person, Charaktername die gespeicherte Figur.
- Jeder darf einen freien vorhandenen Charakter übernehmen. Eine Figur kann nur
  einmal gleichzeitig ausgewählt werden. Keine Accounts oder feste Personenbindung.
- Abwesende Charaktere bleiben gespeichert und werden nicht gespawnt. Sie zählen
  nicht für Game Over, Revive oder Gebietsabstimmungen.
- Ein freier Charakterplatz kann mit einem Namen belegt werden. Sind vier Figuren
  vorhanden, muss eine bestehende freie Figur übernommen oder eine nicht ausgewählte
  Figur vom Host gelöscht werden. Die Gründerfigur bleibt geschützt.
- Die Charaktererstellung verwendet zunächst die vorhandene Figurenoptik.

## Ablauf

Die eigene Szene `Assets/Scenes/MainMenu.unity` enthält Startmenü und Lobby-Ansicht.
Eine separate Lobby-Szene ist nicht erforderlich. Sie ist die erste Szene im Build.
Abenteuer-Szenen wurden durch das Setup nicht verändert.

Singleplayer: neues Abenteuer oder einen der drei lokalen Slots laden.
Multiplayer: Spielername eingeben, neue Welt hosten, den Multiplayer-Spielstand
fortsetzen oder per Host-IP einer Lobby beitreten. Zunächst direkter LAN-Beitritt
über UDP 7777; keine öffentliche Serverliste oder Vermittlungsplattform.

In der Lobby Figur auswählen oder einen freien Platz erstellen. „Charakterauswahl
freigeben“ löst die Reservierung; „Nicht bereit“ hält nur den gemeinsamen Start an.
Alle Spieler müssen bereit sein. Der Host startet die Gruppe. Beim Fortsetzen
werden die ausgewählten Figuren gemeinsam am gespeicherten Host-Punkt platziert,
auch wenn heute eine andere Figur vom Host übernommen wird.

Neue Verbindungen werden während des gemeinsamen Starts und nach dem Lobby-Start
abgewiesen. Wiederbeitritt während des Abenteuers ist der nächste auszubauende Ablauf.
Das bestehende F6-Testpanel bleibt für direkte Editor-Tests in Abenteuer-Szenen erhalten.

## Speicherung und Übernahme

Savegame-Schema 9 ergänzt Weltmodus, benannte Charakterplätze und die gespeicherte
Host-Charakter-ID. Versionen 1–8 bleiben lesbar. Beim Laden einer älteren
Multiplayer-Welt werden bis zu vier vorhandene Figuren als Charakterplätze angeboten;
weitere alte Charakterdaten bleiben im Weltzustand erhalten, ohne einen Lobby-Platz
zu belegen. Standardnamen lauten zunächst „Charakter 1“ usw.

Singleplayer-Dateien `SecretsReborn/slot-1.es3` bis `slot-3.es3` bleiben erhalten.
Multiplayer verwendet drei eigene Slots: `SecretsReborn/multiplayer.es3`,
`multiplayer-slot-2.es3` und `multiplayer-slot-3.es3`. Das Buch zeigt alle drei
Speicherplätze. Siehe [gemeinsames Speicherbuch](shared-save-book.md).
Neue Welt/Charaktere existieren zunächst in der Session: gespeichert wird weiterhin
am Buch. Das Erstellen einer Lobby löst kein automatisches Speichern aus.

Die Charakterplätze werden bei späterem Gruppen-Load/Retry bewahrt, wenn ein
aktiver Charakter erst nach dem verwendeten Checkpoint erstellt wurde. Sein nicht
gespeicherter Fortschritt wird dabei zurückgesetzt. Inventar und Weltzustand bleiben
host-autoritativ; eine Transport-ID oder ein Client-Token bestimmt keine gespeicherte Figur.

## Prüfen

Editor: `Assets/Scenes/MainMenu.unity` öffnen und Play starten.
Build: `Builds/LocalCoop/SecretsReborn.exe` startet automatisch im MainMenu.

1. Editor als Host starten, Build als Client per `127.0.0.1` beitreten.
2. Beide Figuren erstellen/auswählen, Namen und gegenseitige Reservierung prüfen.
3. Nur einen Spieler bereit setzen: Start darf nicht erfolgen.
4. Beide bereit setzen und als Host starten. Beide Fenster müssen im Abenteuer sein.
5. Am Buch speichern, beide Sessions beenden, Multiplayer-Spielstand fortsetzen.
6. Mit weniger Spielern fortsetzen: übrige Figuren bleiben in der Lobby-Auswahl erhalten.
7. Andere Figur übernehmen und den gemeinsamen gespeicherten Startpunkt prüfen.

Automatische Prüfungen: Domain-Test `tests/WorldSessionChecks.cs` prüft vier Plätze,
Schema-Roundtrip, doppelte IDs und Migration alter Figuren. Der opt-in Development-
Test `LobbyIntegrationDriver` prüft zwei echte Prozesse, Spielername, Reservierung,
Client-Charaktererstellung, gemeinsamen Start und Erhalt abwesender Figuren.
Die temporären Testberichte liegen unter `Temp/LobbyFinal-Host.txt` und
`Temp/LobbyFinal-Client.txt`; es werden keine vorhandenen Spielstände verwendet.

Prüfergebnis: Beide Lobby-Prozesse bestehen, einschließlich Erstellen der vierten
Figur durch den Client. Die Domain- und Protokolltests bestehen ebenfalls.
Der bestehende Zwei-Prozess-Koop-Test besteht im neuen Build auf beiden Seiten
(`Temp/LobbyRegression-Host.txt`, `Temp/LobbyRegression-Client.txt`): Ausrüstung,
Truhen, Runen, Szenenwechsel, Save/Load, Revive und gemeinsamer Retry bleiben funktionsfähig.
Alle Testprozesse sind beendet. Native Unity-Kompilierung und Windows-Build bestehen
mit 0 Fehlern und einer bekannten Warnung zur optionalen Pipeline-Konfiguration.
Netzwerkprotokoll 9 erfordert den neuen Build auf beiden Seiten.

Beim Abbruch einer Host-/Join-Lobby zerstört der Adapter die alte Session und
lädt MainMenu erneut. MainMenu erstellt jetzt ausdrücklich eine neue Session mit
neuem Transport; die einmaligen Runtime-Startcallbacks werden beim Szenenwechsel
nicht erneut aufgerufen. Wiederholtes Hosten oder Joinen benötigt keinen Spielneustart.
Der opt-in Test `MenuRetryIntegrationDriver` besteht im neuen Build
(`Temp/MenuRetry.txt`): Host–Abbruch–Host, Join–Abbruch–Join und erneuter Wechsel
zu Host erzeugen jeweils eine frische Session und einen funktionsfähigen Transport.
Manuelle Prüfung von Menü, Controller-Bedienung und Fortsetzen steht noch aus.
