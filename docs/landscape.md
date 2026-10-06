# Felswände, Baumtypen und Treppenausgang

Die Landschaftsanpassung betrifft `Waldheiligtum-Editable` und wird im Editor
gespeichert. Im Spiel wird nichts zufällig neu aufgebaut.

## Felswände

Die Terrain-Tilemap verwendet zusammenhängende Ausschnitte aus dem Terrain-Sheet:
dreizeilige Frontflächen mit Sockel, mittlerem Fels und oberer Kante sowie
zweispaltige Seitenflächen. Die Tiles liegen als `Cliff_00` bis `Cliff_12` unter
`Assets/World/Sanctuary/Tiles`. Sie behalten Grid-Kollisionen und lassen sich
wie andere Tiles im Editor malen. Horizontale Motive bestehen jeweils aus
3x3 zusammengehörigen Tiles; Seitenteile aus zwei nebeneinanderliegenden Tiles.

Die zuvor erzeugten einteiligen Stone-Grenzen werden ersetzt. Andere Terrain-
Tiles bleiben erhalten. Die neuen Wände erstrecken sich nach außen, damit der
Innenraum mit Spieler und Rätselorten erhalten bleibt. Der nördliche Durchgang
bleibt drei Zellen breit.

## Bäume

Neue Prefabs: `CanopyTree`, `TieredTree`, `TealTree`. Sie verwenden jeweils den
ersten Frame weiterer Baumtypen aus Secrets; ihre Animationsbögen bleiben als
Originale unter `Assets/Art/Proposals` verfügbar. Neue Originalpfade stehen in
`docs/asset-review/forest-tree-sources.json`.

Die fünf bisherigen Bauminstanzen werden durch die drei Typen ersetzt, ihre
Positionen und Drehungen bleiben erhalten. Import: 32 PPU, native Spritegröße,
Scale 1, Fußpunkt auf 18 % der Bildhöhe. Die Stammkollision ist 0.7x0.7 Einheiten
groß. Kronen bleiben durchlässig; die Verdeckung richtet sich nach dem Fußpunkt.
Die zusätzlichen Farbvarianten und knorrigen Bäume bleiben weitere Vorschläge.

## Ausgang

`ExitStairs.prefab` enthält eine vollständige 4x5-Zellen-Steintreppe aus dem
Terrain-Sheet. Sie liegt unter `World/Sanctuary/Exit stairs`. Ihre Seiten besitzen
eigene Collider, die Treppenmitte bleibt frei. Das vorhandene Rätseltor verschließt
den Weg bis zur Wiederherstellung der Quelle und öffnet ihn anschließend wie bisher.
Der Prototyp endet weiterhin hinter der Treppe; ein Gebietswechsel folgt später.

## Spieltest

- Wände besitzen zusammenhängende Felsflächen statt identischer Einzelsteine.
- Baumtypen, Stammkollision und Verdeckung prüfen.
- Treppenweg ist vor Lösung geschlossen.
- Nach dem vierten Kreis lässt sich die Treppe durch die Mitte betreten.
- Vor und nach Play bleiben Tilemap und Prefab-Positionen identisch.

Der Editor-Befehl `SecretsReborn > World > Upgrade cliffs trees and stairs`
wendet diese Landschaftsplatzierung erneut an. Ersetzt bekannte Stone-/Cliff-
Grenzen und Bauminstanzen unter Props; für spätere manuelle Kartenänderungen
stattdessen die vorhandenen Prefabs und Tile-Assets direkt bearbeiten.
