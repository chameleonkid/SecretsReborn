# Speichern am Buch

Speichern und Laden sind nur an einem aktiven Speicherbuch in Reichweite
möglich. Globales F5/F9, automatische Speicherung bei Stop und automatisches
Laden beim Start wurden entfernt. Stop beendet die Sitzung ohne zusätzlich
zu speichern. Das gilt auch beim Beenden des Spiels.

## Bedienung

Der Schreibtisch mit offenem Buch steht zunächst bei (-2, -6) in
Waldheiligtum-Editable. In Reichweite **E / Gamepad A** zum Öffnen drücken.
Drei Speicherplätze stehen bereit. Modus **Speichern/Laden** per Schaltfläche,
Tab oder LB/RB wechseln. Slot per Maus, Pfeiltasten oder D-Pad auswählen und
mit Enter/A bestätigen. Belegte Slots müssen vor Überschreiben nochmals
bestätigt werden. Esc/B schließt das Buch.

Das Buch blockiert die Bewegung der bedienenden Figur und pausiert nicht die
Welt. Inventar und Buch können nicht gleichzeitig geöffnet werden. Die
Zustandsoperation prüft Buch, Entfernung und State Authority erneut.
Die Solo-Zuständigkeit ist noch keine Netzwerk-Autorisierung.

Die Grafiken Desk_type1.png und SavePanel.png stammen aus Secrets,
Commit 99f5b1c, Assets/Art/Objects/SavePoint. Alte Scripts/UI-Abhängigkeiten
werden nicht übernommen. SaveBookDesk.prefab kann frei im Editor platziert werden.

## Daten und Laden

SaveGameData Version 4 speichert auch aktuelle und maximale HP/Mana pro Charakter.
Slots der Versionen 1–3 erhalten beim Laden volle Startwerte. Details und Tests:
[HP und Mana](character-vitals.md).

Seit Version 3 enthält der Spielstand zusätzlich Spielzeit (Sekunden) und eine
Szenenübersicht für die Slotanzeige. Die Spielzeit zählt fokussierte Spielzeit,
auch am Buch/Inventar solange die Welt läuft; Ladezeiten und Pause zählen nicht.
Geladene Spielstände setzen die Spielzeit auf ihren gespeicherten Wert.
Ältere Slots bleiben lesbar, zeigen aber „Spielzeit unbekannt“.

Der Snapshot speichert World ID, alle Charaktere samt Inventar,
Ausrüstung, Szene und Position sowie aufgesammelte Item-IDs und Runenfortschritt.
Szenenpfade müssen stabil bleiben. Version 1 wird ohne Positionsdaten gelesen.
Die Slots liegen unter Application.persistentDataPath als
SecretsReborn/slot-1.es3 bis slot-3.es3; Backups erhalten .bac.
Das bisherige world.es3 bleibt unangetastet und wird nicht automatisch geladen.

Beim Laden wird bei Bedarf die gespeicherte Szene geöffnet, danach Position,
Inventar, Ausrüstung und Weltzustand hergestellt. Im Editor können gespeicherte
Szenen direkt geladen werden; im fertigen Spiel müssen alle Zielgebiete in
Build Profiles enthalten sein. Es wird derzeit die Szene des bedienenden
Charakters geladen; kooperative, gemeinsame Gebietswechsel folgen separat.

SaveGameStore schreibt einen Snapshot zunächst in .pending, liest und prüft
 ihn, sichert den vorherigen Slot als .bac und übernimmt die Datei. Der Backup-
Test arbeitet ausschließlich mit separaten Temp-Dateien. Snapshot-Prüfungen
lehnen unbekannte Versionen und ungültige Dimensionen/Positionen ab.

## Spieltest

Rüstung aufheben und anlegen, zum Buch gehen, Slot 1 speichern. Anschließend
Ausrüstung ablegen und weiterlaufen. Zum Buch zurückkehren, Laden wählen,
Slot 1 laden: Ausrüstung, Runen und gespeicherte Position müssen zurückkehren.
Nach Stop/Play zum Buch gehen und denselben Slot laden. Ungespeicherte Änderungen
bleiben verworfen. Slots 2/3 müssen Slot 1 unabhängig erhalten.

Das Menü verwendet jetzt ForestSaveBook.png, eine mit Imagegen erzeugte Grafik mit Pergament, Leder, Messing und Efeu. Das ursprüngliche SavePanel wird im Menü nicht mehr benutzt. Bild und Menü behalten das Seitenverhältnis der neuen Grafik.

## Abhängigkeit nach dem Klonen

Easy Save 3 ist eine gekaufte Abhängigkeit und wird nicht im öffentlichen
Repository verteilt. Vor dem Kompilieren im Unity Package Manager unter
`My Assets` mit dem berechtigten Asset-Store-Konto herunterladen und in das
Projekt importieren. Erwarteter Ordner: `Assets/Plugins/Easy Save 3`.
Die eigenen Savegame-Scripts und die TMP-Ressourcen sind versioniert.
