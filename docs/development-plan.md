# Entwicklungsplan

## Aktueller Umfang

Das Waldheiligtum wurde zusätzlich als dauerhaft editierbare Szene mit Grid,
drei Tilemaps, drei Tile-Assets und sechs Prefabs gespeichert und erneut geladen.
Siehe [Kartengestaltung](world-authoring.md). Der Spieltest der neuen Szene steht noch aus.

Code und Mechaniken sind seit dem 6. Oktober 2026 freigegeben. Der erste Solo-Bewegungsprototyp mit Kamera, Kollision und Platzhaltergrafik ist angelegt; siehe [Spieltest](prototype.md). Nichts committen oder pushen.

## Technische Bestandsaufnahme – 6. Oktober 2026

- Projektordner und Git-Wurzel: `D:\SecretsReborn\SecretsReborn`.
- Projektversion und installierter Editor stimmen mit `6000.3.25f1` überein.
- `Assets`, `Packages` und `ProjectSettings` sind vorhanden; enthalten sind eine SampleScene, URP und ein 2D-Renderer.
- Die `.gitignore` schließt unter anderem `Library`, `Temp`, `Logs`, `UserSettings` und generierte IDE-Projektdateien aus.
- Vor der Dokumenterstellung: keine Änderungen an versionierten Dateien; `.vscode/` ist unversioniert.
- Unity ist bereits mit diesem Projekt geöffnet. Ein weiterer CLI-Editor wird deshalb nicht gestartet.
- Im vorhandenen Editorlog ist der initiale Asset-Pipeline-Refresh abgeschlossen. Bei der gezielten Suche wurden keine C#-Compilerfehler, Meldungen über fehlgeschlagene Kompilierung oder Exception-Meldungen gefunden. Dies ersetzt keinen neuen isolierten CLI-Import oder einen interaktiven Funktionstest.
- VS Code ist für das Projekt geöffnet. Unity-Erweiterung, C#, C# Dev Kit und .NET-Runtime-Erweiterung sind installiert. Die vorhandene Attach-Konfiguration verwendet `vstuc`; die Standard-Solution ist `SecretsReborn.slnx`.
- `com.unity.ide.visualstudio` ist in Version `2.0.26` vorhanden.
- Die alten ignorierten IDE-Dateien wurden entfernt und in Unity neu generiert. Die veralteten Verweise auf `HubForceResolve.cs` und `D:\UnityTemp` waren anschließend entfernt. Die damals leere Solution passte zum Projekt ohne eigene Skripte; nach Import der neuen Skripte die IDE-Dateien bei Bedarf erneut generieren.
- VS Code ist als externer Script-Editor ausgewählt. Der Debugger-Attach zu SecretsReborn wurde anhand der laufenden Threads und der VS-Code-Statusanzeige bestätigt. Ein Breakpoint-Test steht noch aus.
- Der Zugriff auf das optionale Repository Secrets funktioniert. Erste Asset-Kandidaten sind in [assets.md](assets.md) dokumentiert.

## Nächster konkreter Schritt

Die Szene `Assets/Scenes/Waldheiligtum.unity` im bestehenden Editor öffnen und den manuellen Bewegungstest durchführen. Die drei neuen Skripte wurden separat gegen die lokalen Unity- und Input-System-Assemblies erfolgreich kompiliert. Ein Unity-Play-Test ist noch nicht bestätigt. Anschließend wenige passende Wald- und Figurenassets aus Secrets sichten und den nächsten Interaktionsschritt konkretisieren.

Ein separater CLI-Import kommt erst infrage, wenn kein Editor mehr auf dieses Projekt zugreift. Einen laufenden Editor nicht automatisch schließen, da ungespeicherte Arbeit vorhanden sein kann.

## Weitere Entwicklung – Vorschlag zur Reihenfolge

Die folgende Reihenfolge bleibt ein Vorschlag. Code und Mechaniken sind grundsätzlich freigegeben; die offenen inhaltlichen Entscheidungen bleiben anpassbar.

1. Technische Grundlage und IDE-Integration abschließen; die offenen Entscheidungen zu Gebieten und Fortschritt konkretisieren.
2. Ein kleines Gebiet mit Platzhaltergrafiken für Bewegung und Interaktion erstellen.
3. Den Solo-Ablauf des Waldheiligtums mit Laterne, Zeichen, Rätselhöhle und Quelle umsetzen.
4. Weltzustand und Charakterdaten getrennt speichern; Charakterbindung an die Host-Welt und eigenes Inventar, XP und Fortschritt berücksichtigen.
5. Optionalen Koop für insgesamt maximal vier Spieler ergänzen; freie Bewegung innerhalb eines Gebiets und gemeinsame größere Gebietswechsel prüfen.
6. Solo- und Koop-Durchläufe, Laden und erneuten Beitritt erproben; anschließend Inhalte und Grafik weiterentwickeln.

## Zusätzliche technische Ideen – Vorschläge

- Vorschlag: Zuständigkeit des Hosts für gemeinsame Weltänderungen bereits beim Solo-Prototyp berücksichtigen, ohne jetzt ein Netzwerkpaket auszuwählen.
- Vorschlag: Für den ersten Spielausschnitt nur jene Systeme entwickeln, die den vorgegebenen Abenteuerablauf unterstützen.
- Vorschlag: Für Speichern und Koop später gezielte Prüfungen mit getrennten Charakterdaten und gemeinsamem Weltzustand vorsehen.
