# RetroPixel-Figurenimport, 8. Oktober 2026

Quelle: ausschließlich `Assets/Resources/Retro Pixel Characters/Spritesheets`
aus Secrets, Commit `99f5b1c15fa6201e84044fbd81f9397d4f9709a2`.
Das originale Paket-ReadMe liegt neben den Grafiken. Das
[Quellenmanifest](asset-review/retro-pixel-character-sources.json) enthält alle
Originalpfade, Größen und Git-Blob-SHAs. Die 78 heruntergeladenen PNG-Dateien wurden
gegen diese SHAs geprüft; es wurden keine Figuren neu generiert oder umgezeichnet.

| Inhalt | Bögen | Verwendung |
| --- | ---: | --- |
| Körper und Hautvarianten | 16 | Creator, beide Körperfamilien |
| Augenfarben | 14 | Sieben Originalfarben pro Familie |
| Frisuren | 26 | 13 pro Familie, zusätzlich bestehende Farbauswahl |
| Outfits und tintbare Kleidung | 18 | Equipment-Layer und Grundkleidung |
| Helme und Krone | 4 | Equipment; keine Frisurauswahl |

Grafiken: `Assets/Art/RetroPixelCharacters`.
Animierte Layerdaten: `Assets/Resources/CharacterLooks`.
Visuelle Layer-Prefabs: `Assets/World/CharacterLooks/Prefabs`.
Die Layer-Prefabs zeigen den frontalen Ausgangsframe; die eigentliche Animation
übernimmt `CharacterAppearance` synchron mit der Spielfigur.

Unity schneidet jeden 512×256-Bogen in 128 Frames von 32×32 Pixeln, mit Fußpivot,
Full Rect, Point-Filtering und ohne Kompression/Mipmaps. 24 PPU entsprechen dem
bereits bestätigten Figurenmaßstab; Terrain bleibt bei 32 PPU. Outfits erhalten
ihre passende zweite Körpervariante. Bestehende Ranger-Grundkleidung und
Übungsrüstung berücksichtigen diese Variante ebenfalls.

Neue Figuren speichern stabile IDs für Körper, Haare und Augen. Der Host prüft
IDs und Familienzugehörigkeit; Helm-/Kronen-IDs können nicht als Frisur eingesendet
werden. Die Assets dienen ausschließlich der Darstellung. Rüstungswerte,
Loot-Verteilung und neue Rüstungsgegenstände werden damit nicht festgelegt.
Ein Item kann im Inspector über `Armor Appearance` mit einem importierten
Outfit verbunden werden; dessen Körpervariante wird automatisch gewählt.

Validierung: native Unity-Importprüfung aller 78 Bögen, Git-Blob-Hashvergleich,
Save-Migration und ID-Validierung sowie Zwei-Prozess-Lobbytest inklusive
Easy-Save-Roundtrip und Client-Darstellung. Manuell prüfen: unterschiedliche
Körper/Frisuren erstellen, laufen/angreifen/sterben und Rüstung an-/ablegen.
Die selbst bearbeiteten Abenteuer-Szenen werden durch den Import nicht umgebaut.

Korrektur nach Sichtprüfung: 128 automatisch erzeugte Tight-Sprites sind kein
Nachweis für ein korrektes Raster. Der Import erzwingt Namen `Frame_000` bis
`Frame_127`, exakte 32×32-Rechtecke, Fußpivot, Point und Uncompressed bei jedem
Lauf. Bestehende Sprite-IDs werden anhand ihrer Rasterzelle übernommen. Layerdaten
und visuelle Prefabs werden aktualisiert. Creator-, Lobby-, Inventar- und
Buchvorschauen verwenden einen gemeinsamen ganzzahligen Bildschirm-Pixelmaßstab.
