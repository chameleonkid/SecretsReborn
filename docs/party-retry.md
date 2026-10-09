# Gemeinsamer Retry nach Game Over

Stand: 7. Oktober 2026.

## Ablauf

Game Over erscheint erst nach den Death-Animationen, wenn alle aktiven Charaktere
gefallen sind. Historische Charaktere im Savegame zählen nicht zur aktiven Gruppe.

Der Host wählt „Gemeinsam neu starten“ oder drückt Enter / Controller A.
Alle verbundenen Spieler einschließlich des Hosts erhalten eine Abfrage.
Nur bei einstimmiger Zustimmung wird der Checkpoint gemeinsam geladen.
Nein, fehlende Zustimmung oder Disconnect bricht die Anfrage ab; der
Game-Over-Zustand und der aktuelle Weltzustand bleiben erhalten.
Clients können selbst keinen gemeinsamen Retry auslösen.

Der Rückkehrpunkt ist der zuletzt erfolgreich gespeicherte oder geladene
Host-Zustand. Fortschritt danach wird zurückgesetzt. Ohne vorherigen Save/Load
gilt der initiale Szenenstart der laufenden Session als Start-Checkpoint;
das Game-Over-Menü kennzeichnet diesen Fall ausdrücklich.

Verbindungen und aktive Charakter-IDs bleiben erhalten. Die ganze aktive Gruppe
startet gemeinsam am Host-Punkt mit vollen Herzen und Mana. Inventare, Ausrüstung,
Runen, Truhen, Fund-Freischaltungen und Gegnerzustand stammen vom Checkpoint.
Ein Gast, der im Checkpoint noch nicht vorkam, beginnt mit seinem Standardzustand.
Historische, aktuell nicht verbundene Figuren werden nicht gespawnt oder wiederbelebt.

Der Retry schreibt keine Savegame-Datei und lädt nicht automatisch einen
beliebigen älteren Slot. Es wird der Rückkehrpunkt der laufenden Host-Session
verwendet. Ein normaler Buch-Load stellt weiterhin die gespeicherten Vitals her;
nur der Retry füllt Herzen und Mana der aktiven Gruppe vollständig auf.

## Prüfung

Der native Windows-Build besteht mit 0 Fehlern und einer bekannten Warnung zur
optionalen Unity-Pipeline-Konfiguration. Netzwerkprotokoll ist Version 5;
Host und Client benötigen den neuen Stand.

Der Zwei-Prozess-Test besteht auf Host und Client:
`Temp/CoopRetryV5-Host.txt` und `Temp/CoopRetryV5-Client.txt`.
Geprüft sind Start-Checkpoint ohne Save, Ablehnung ohne Zustandsänderung,
gemeinsamer Retry nach Save/Load, erhaltene Charakter-IDs und Ausrüstung,
Rücksetzen späterer Weltänderungen sowie volle Herzen/Mana am Host-Punkt.
Beide Prozesse sind beendet; die Logs enthalten keine Testfehler oder Exceptions.
Der visuelle manuelle Spieltest steht noch aus. Bestehende Szenen wurden nicht verändert.

## Manueller Test

1. Host und Client starten und einen Spielstand am Buch speichern.
2. Nur einen Spieler sterben lassen: kein Game Over; Revive bleibt möglich.
3. Beide sterben lassen: Game Over in beiden Fenstern.
4. Der Host startet die Anfrage. Zunächst Nein wählen: beide bleiben gefallen.
5. Erneut anfragen und in beiden Fenstern bestätigen: gleiche Identitäten,
   Checkpoint-Ausrüstung, volle Herzen/Mana und gemeinsamer Spawn.
6. In einer frischen Session ohne Save/Load ebenfalls beide sterben lassen:
   der Start-Checkpoint muss angeboten werden und funktionieren.
