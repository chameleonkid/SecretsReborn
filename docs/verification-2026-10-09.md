# Prüfung und Veröffentlichung – 9. Oktober 2026

Der Nutzer hat den zuletzt vereinbarten manuellen Test mit „Der Test war
erfolgreich“ bestätigt. Damit ist der letzte Fix für mehrere lokale Clients
manuell bestätigt: zwei Spieler starten, ein dritter tritt über den normalen
Join bei; getrennte lokale Client-Profile verhindern die doppelte Rejoin-Identität.
Das ist keine gesonderte Bestätigung aller zusätzlichen Grenzfälle aus der
[Übergabe](handoff-2026-10-08.md).

## Bereits vorhandene technische Nachweise

- Unity-Import und Windows-Player-Build vom 8. Oktober, 22:17: 0 Fehler,
  1 Warnung. Netzwerkprotokoll und Savegame-Schema jeweils 13.
- Der erweiterte Drei-Prozess-Test besteht auf allen Teilnehmern: gemeinsamer
  isolierter PlayerPrefs-Bereich, unterschiedliche lokale Profile, später
  normaler Beitritt mit Creator, Save-Roundtrip der Optik, Meldung an alle und
  Reconnect des dritten Clients zu seiner eigenen Figur.
- Zwei-Prozess-Reconnect-Test, Protokoll-/Reservierungschecks sowie die zuvor
  dokumentierten Menü-, Lobby-, Buch- und Multiplayer-Prüfungen bestanden.
  Details und jeweiliger Prüfstand stehen in den Fach-Dokumenten.

Heute keine erneute Build-/Spieltest-Runde: seit diesen Nachweisen wurde nur
Dokumentation aktualisiert. Git-Diff, Asset-Metadaten und Ausschlüsse werden vor
den vereinbarten strukturierten Commits und dem Push geprüft.

## Noch offen

1. Vier gleichzeitig aktive Teilnehmer und Ablehnung einer fünften Verbindung.
2. Echte Netzwerkunterbrechung sowie Host-Ende und verständliche Rückmeldungen.
3. Abbruch während gemeinsamer Buchbedienung, Abstimmung, Laden und
   Truhenpräsentation in einer Vier-Spieler-Gruppe.
4. Nach Neustart aller lokalen Test-Clients dieselbe Zuordnung prüfen;
   Clients dazu in derselben Reihenfolge verbinden. Aktuell sind die Profile
   lokale Verbindungskennungen, keine mitgebrachten Charakterdateien.

Als nächster konkreter Schritt die Netzwerk-Grenzfälle abschließen. Danach
Wiederbeschaffung wichtiger Items und Einstiegsausrüstung festlegen; Gold,
Verbrauchsgegenstände/Schnellslots und Bogen bleiben Vorschläge für den nächsten
Mechanikausbau, keine neue Implementierungsfreigabe.

## Git-Aufteilung

1. RetroPixel-Figuren und Menü-Grafiken mit Metadaten und Kleidungspaaren.
2. Zusammenhängende Spielsysteme: Menü/Creator, Host-Weltprofile, gemeinsames
   Buch/Load, Gebietswechsel, Truhen, Reconnect/laufender Join und Tests. Diese
   Komponenten referenzieren sich gegenseitig und werden gemeinsam committed.
3. Dokumentation, Übergabe und bestätigter Prüfstand.

Library, Temp, Builds, lokale Spielstände und das gekaufte Easy-Save-Plugin bleiben
ausgeschlossen. Projektinhalte und bestehende Unity-Einstellungen bleiben erhalten.
