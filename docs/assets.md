# Optionaler Asset-Fundus: Secrets

Quelle: https://github.com/chameleonkid/Secrets

Aktueller Figurenimport: 78 Originalbögen aus Retro Pixel Characters mit
Körpern, Hautvarianten, Augen, Frisuren und Equipment-Layern. Quelle und
Verwendung: [RetroPixel-Figurenimport](retro-pixel-character-import.md).

Erste lesende Sichtung am 6. Oktober 2026, Commit `99f5b1c15fa6201e84044fbd81f9397d4f9709a2`.
Der Repository-Zugriff funktioniert. Die rekursive GitHub-Dateiliste ist gekürzt;
die folgenden Angaben sind daher eine erste Auswahl, kein vollständiges Inventar.

## Kandidaten für das Waldheiligtum – Vorschläge

- `Assets/Art/Trees`: unter anderem `Tree_Type1.png`, `GlowFlowers.png` und passende Varianten.
- `Assets/Art/World/Grass Land`: Gelände und animierte Bäume.
- `Assets/Art/World/Caves`: Grafiken für die spätere Rätselhöhle.
- `Assets/Art/World/WorldTree`: ergänzende Weltgrafiken.
- `Assets/Resources/Retro Pixel Characters/Spritesheets`: Figuren-Spritesheets.
- `Assets/Art/NPCs/AnimatedNPCs`: umfangreicher Fundus animierter NPCs.
- `Assets/Art/Items`: Gegenstände für spätere Interaktionen.

Für die Auswahl zunächst wenige Dateien mit ihren Import-Metadaten und vorhandenen
Lizenzhinweisen sichten. Pixelmaßstab, Blickrichtung, Animationen und URP-Kompatibilität
prüfen. Erst anschließend passende Grafiken gezielt übernehmen. Alte Skripte,
Spielregeln und Geschichte sind keine Vorgabe für SecretsReborn.

Baum, Gelände und Wasser sind inzwischen unter `Assets/Resources/SanctuaryArt`
übernommen. Figur, Runen, Laterne und Wege verwenden weiterhin Platzhalter.

Die übernommenen Texturen erhalten eigene GUIDs, 32 PPU, Point-Filtering,
Full Rect und unkomprimierte Darstellung. Die Originale und Importbefunde
bleiben unter `docs/asset-review` zum Vergleich erhalten. Die ganzen Sheets
werden als Einzel-Sprites importiert; ausgewählte 32x32-Geländeteile und ein
16x16-Wasserteil werden im Prototyp als eigene Sprites erzeugt.
Die Bäume verwenden den vorhandenen Fußpunkt, kleinere Stammkollisionen und
dieselbe Sortierung nach Y-Position wie die Spielerfigur. Der Unity-Spieltest
für Maßstab, Ausschnitte und Verdeckung steht noch aus.

## Vorbereitete Auswahl

Die editierbare Szene wurde einschließlich Baummaßstab im Unity-Spieltest vom Nutzer bestätigt.

Fünf Originalgrafiken und ihre ursprünglichen `.meta`-Inhalte liegen unter
`docs/asset-review`. Die Metadaten sind als `.import.txt` gespeichert und werden
nicht als Unity-Importer angewendet. [Visuelle Vorschau](asset-review/index.html).

| Vorschlag | Originalpfad in Secrets | Importbefund |
| --- | --- | --- |
| Baum | `Assets/Art/Trees/Tree_Type1.png` | Einzel-Sprite, 24 PPU |
| Gelände und Steinruinen | `Assets/Art/World/Grass Land/MainLevBuild.png` | Mehrere Sprites, 32 PPU |
| Quellwasser | `Assets/Art/Water/Water.png` | Mehrere Sprites, 32 PPU |
| Figurenbasis | `Assets/Resources/Retro Pixel Characters/Spritesheets/Female/1 - Base/rpc female base.png` | Mehrere Sprites, 32 PPU |
| Optionale Leuchtblumen | `Assets/Art/Trees/GlowFlowers.png` | Mehrere Sprites, 32 PPU |

Alle fünf verwenden Point-Filtering, Transparenz und den Sprite-Texturtyp.
Die bestehende Mesh-Einstellung ist Tight; skalierte oder gekachelte Flächen
benötigen Full Rect. Die Grafiken selbst enthalten keine Materialreferenzen;
ihre Darstellung mit einem URP-Sprite-Material ist noch in Unity zu prüfen.

Vorschlag für die erste Übernahme: Baum, wenige Gras-/Steinteile und eine kleine
Wasserfläche. Die Figurenbasis anschließend mit passenden Bewegungsframes und
Blickrichtungen einbinden. Der Baum hat einen anderen ursprünglichen PPU-Wert;
Maßstab und Fußpunkt müssen zur Figur passen. Kollisionsflächen getrennt von
Kronen und Dekoration anlegen.

Ein eigenständiger Quellen-Sprite wurde in dieser Auswahl nicht gefunden. Vorschlag:
ein Steinbecken aus Geländeteilen mit Wasser aus dem Wassersheet kombinieren.
Die konkreten Nutzungsbedingungen der fünf Kandidaten sind noch nicht zugeordnet;
die bisherigen Dateilisten zeigen Lizenztexte für andere Asset-Unterordner, die
nicht automatisch für diese Auswahl gelten.

## Zusätzliche Importe – Vorschläge

Vier weitere Baumkandidaten sind importiert: `tree-red.png` (rote Variante des
bisherigen Baums), `tree-round.png` und `tree-tall.png` (rote Animationsbögen
mit unterschiedlichen Kronen) sowie `tree-marsh.png` (knorriger Sumpfbaum).
Originalpfade stehen in `asset-review/tree-sources.json`; die Vorschau zeigt
die vollständigen Bögen. Frames lassen sich im Project-Fenster aufklappen.
Neue Baum-Prefabs und Animationen sind noch nicht angelegt. Bei der roten
Einzelgrafik den ursprünglichen 24-PPU-Maßstab an die vorhandenen 32 PPU anpassen.

Die geprüften Spieler-Prefabs aus Secrets unterstützen getrennte Körper- und
Rüstungsdarstellung. Die feste Anforderung und die vorgeschlagene Umsetzung sind
in [character-appearance.md](character-appearance.md) dokumentiert.

Sieben weitere Kandidaten sind unter `Assets/Art/Proposals` abgelegt und in der
[Vorschau](asset-review/index.html) sichtbar. Sie sind nicht in die Spielszene
eingebunden und legen keine weiteren Spielmechaniken fest.

| Datei | Vorschlag | Importzustand |
| --- | --- | --- |
| `ranger-outfit.png` | Waldläuferkleidung über der Figurenbasis | Mehrere Frames, 32 PPU |
| `lantern.png` | Grafik für die Spieler-Laterne | Einzel-Sprite, 64 PPU |
| `cave-entrance.png` | Grasbewachsener Höhlenzugang | Mehrere Sprites, 32 PPU |
| `cave.png` | Felswände und Höhlenboden | Ganzer Bogen, 100 PPU; vor Tilemap-Nutzung schneiden |
| `props.png` | Säulen, Ruinen, Felsen und Wurzeln | Mehrere Sprites, 32 PPU |
| `sword-layer.png` | Optionale spätere Ausrüstungsdarstellung | Mehrere Frames, 32 PPU |
| `light-effect.png` | Visuelle Rückmeldung für Runen oder Quelle | Einzelmotiv, 100 PPU |

Die Originalpfade stehen in `asset-review/additional-sources.json`; alle stammen
vom oben genannten Commit. Originalbilder und Metadaten bleiben für den Vergleich
erhalten. Die Vorschlagsimporte verwenden neue GUIDs, Full Rect, Point-Filtering
und keine Mipmaps. Die ursprünglichen Schnitte, Pivot-Punkte und PPU-Werte wurden
beibehalten. Beim Einbau insbesondere die Laternen- und Effektgrößen abstimmen.

Vorhandene Lizenztexte aus den Unterordnern Caves und DecorativeProps sind unter
`asset-review` mit abgelegt; für die übrigen Kandidaten ist noch kein passender
Lizenztext zugeordnet.

Vorschlag für die nächste Übernahme: Figurenbasis mit Waldläuferkleidung und
richtungsabhängigen Animationen, anschließend Laterne und Höhleneingang. Kleidung
und Waffen sind pro Figur darzustellen; lokale Eingabe und Kamera sowie gemeinsame
Welt- und Rätselzustände brauchen weiterhin getrennte Zuständigkeiten. Diese
Grafikimporte ersetzen noch keine Netzwerk-Anbindung.
