# Gelände im Editor bearbeiten

Der Editor-Befehl `SecretsReborn > World > Add terrain authoring layers` ergänzt
die gespeicherte Szene `Waldheiligtum-Editable`. Play muss beendet und die Szene
gespeichert sein. Bestehende Boden-, Pfad- und Gelände-Tiles bleiben erhalten;
die Ausgangstreppe wird einmalig in einzelne Tiles zerlegt und am Grid ausgerichtet.

| Tilemap unter World/Grid | Zeichenreihenfolge | Verwendung | Kollision |
| --- | ---: | --- | --- |
| Ground | -10 | Grundfläche | keine |
| GroundDetails | -9 | Grasflecken und Bodendetails | keine |
| Paths | -8 | Wege und Übergänge | keine |
| GroundOverlay | -7 | Stufen und Steinplatten | keine |
| Terrain | 0 | Felsflächen und Treppenränder | TilemapCollider2D |
| TerrainDetails | 1 | Details auf Felsen | keine |
| Foreground | 1000 | Grafiken vor Figuren | keine |

In der Tile Palette die gewünschte Tilemap als **Active Tilemap** auswählen.
Die zusätzlichen Detailmaps sind zunächst leer. Weitere Tilemaps lassen sich
bei Bedarf ergänzen. Bäume bleiben Prefabs unter World/Props mit Sortierung
nach Fußpunkt; Foreground liegt ausdrücklich auch vor Baumgrafiken.

Die Treppe besteht aus zwanzig `Stair_00` bis `Stair_19` Tile-Assets:
zehn mittlere Stufentiles auf GroundOverlay und zehn seitliche Randtiles auf
Terrain. Nur Randtiles haben Grid-Kollision. Das Rätseltor bleibt ein eigenes
Objekt. Das frühere ExitStairs-Prefab bleibt als Referenz im Projekt erhalten;
für diese Szene die geteilten Tiles verwenden.

Die Ebenen bilden sichtbare Höhen ab. Begehbare Stockwerke, Brücken mit einem
Weg darunter und synchronisierte Höhenwechsel benötigen später eigene
Spielregeln; diese Tilemaps führen noch keine solche Mechanik ein.

Nach dem Umbau prüfen: Boden sichtbar, Spieler nicht von Bodenüberlagerungen
verdeckt, Treppenmitte nach Rätsellösung begehbar, Ränder blockieren.
