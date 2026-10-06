# Waldheiligtum im Editor bearbeiten

Die editierbare Szene ist `Assets/Scenes/Waldheiligtum-Editable.unity`.
Sie wird einmal durch `SanctuaryAuthoring` mit Unity-APIs erstellt und als normale
Szene gespeichert. Im Spiel werden keine Weltobjekte oder Tiles erzeugt.
Die bisherige Szene `Waldheiligtum.unity` bleibt als alter Prototyp erhalten.

## Erster Aufbau

Die Szene und alle sechs Prefabs wurden im laufenden Unity-Editor erstellt und
erfolgreich gespeichert und erneut geladen. Für einen Neuaufbau bei noch nicht
vorhandener Szene: `SecretsReborn > World > Create editable sanctuary`.
Der Befehl überschreibt keine bereits vorhandene editierbare Szene und keine
vorhandenen Prefabs. Er öffnet die neue Szene kurz zusätzlich, speichert und
prüft sie und stellt anschließend die zuvor aktive Szene wieder her.
Bei einer unbenannten Szene diese zunächst speichern. Play muss beendet sein.

Anschließend `Waldheiligtum-Editable` im Project-Fenster öffnen. Die Änderungen
in der alten Szene vorher bei Bedarf speichern. Die neue Szene enthält:

```text
World
├── Grid
│   ├── Ground
│   ├── Paths
│   └── Terrain
├── Props
│   └── Tree (Prefab-Instanzen)
├── Sanctuary
│   ├── Spring
│   └── Sealed passage
└── Puzzle
    ├── SanctuaryPuzzle (Komponente)
    ├── Rune circle 1–4
    └── Lantern arrow (mehrere Instanzen)
Player
Main Camera
```

## Tiles malen

Die Felsgrenzen verwenden jetzt mehrteilige Cliff-Tiles; weitere Baumtypen und
ein Treppen-Prefab sind ergänzt. Siehe [Landschaft bearbeiten](landscape.md).

Die animierte Spielerfigur und die gespeicherte Ground-Variation sind ergänzt;
siehe [Figur und Bodenmuster](character-and-ground.md). Zwölf zusätzliche
GrassPatch-Tiles können ebenfalls in die Palette aufgenommen werden.

- `Window > 2D > Tile Palette` öffnen und eine Palette anlegen.
- Die Tile-Assets aus `Assets/World/Sanctuary/Tiles` in die Palette ziehen.
- Unter Active Tilemap `Ground`, `Paths` oder `Terrain` wählen und in der Scene-Ansicht malen.
- Ground ist Gras; Paths enthält den vorläufigen Weg. Beide haben keine Kollision.
- Terrain hat einen TilemapCollider2D; Stone-Tiles kollidieren über die gesamte Grid-Zelle.
- Die Grid-Zellgröße ist eine Welteinheit, die importierten Sprites verwenden 32 PPU.
- Szene nach Änderungen speichern. Play-Änderungen werden wie üblich nicht dauerhaft gespeichert.

## Prefabs und Szenenobjekte

Unter `Assets/World/Sanctuary/Prefabs` liegen `Tree`, `Spring`, `SealedPassage`,
`Player`, `RuneCircle` und `LanternArrow`. Instanzen lassen sich frei verschieben;
Änderungen am Prefab wirken auf Instanzen ohne entsprechende Overrides.

Die Runen und Pfeile sind außerhalb von Play sichtbar, damit ihre Platzierung
bearbeitet werden kann. In Play erscheinen sie nur mit Laterne in der Nähe.
Der Kreisradius lässt sich im Inspector anpassen; bei ausgewähltem Kreis zeigen
Gizmos seinen Aktivierungsbereich. Die Pfeile lassen sich über ihre Z-Rotation ausrichten.

Am Objekt `World/Puzzle` enthält `SanctuaryPuzzle` eine geordnete Liste der vier
Kreise sowie Referenzen auf Spieler-Laterne, Quelle und Durchgang. Diese Liste
bestimmt die Rätselreihenfolge. Beim Verschieben eines Kreises die zugehörigen
Pfeile ebenfalls anpassen. Neue Kreise allein werden nicht automatisch ins Rätsel aufgenommen.
`HiddenSign` verwendet die aktive lokale Spieler-Laterne, wenn keine Referenz
zugewiesen ist; optional kann sie im Inspector ausdrücklich zugewiesen werden.

Die Wasserfläche des Spring-Prefabs ist anfangs deaktiviert. Nach Lösung wird
sie aktiviert und der separate Durchgang deaktiviert. Die Spielerkollision und
Kamerafolge sind ebenfalls als Komponenten und Referenzen gespeichert.

## Prüfung nach Umbau

1. Vor Play sind Welt, Tiles, Bäume und Runen sichtbar und editierbar.
2. Eine Bauminstanz verschieben, Szene speichern und Play starten: Position bleibt erhalten.
3. Bewegung, Kamera, Terrain- und Stammkollision testen.
4. Laterne aus: Runen und Pfeile unsichtbar. An: nur nahe Objekte sichtbar.
5. Falsche Folge und anschließend korrekte Folge testen; Quelle und Weg prüfen.
6. Play beenden: die gespeicherte Ausgangsszene mit trockener Quelle bleibt erhalten.

Die Build-Szenenliste bleibt unverändert. Für diesen Test die editierbare Szene
direkt öffnen. Speicherung und Koop sind weiterhin nicht implementiert.
