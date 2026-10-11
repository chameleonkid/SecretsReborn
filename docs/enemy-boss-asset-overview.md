# Gegner- und Bossgrafiken

Bestandsaufnahme vom 11. Oktober 2026 im ursprünglichen Secrets-Repository,
Commit `99f5b1c15fa6201e84044fbd81f9397d4f9709a2`.
Lokale Arbeitskopie: `Temp/Secrets-assets-20261011` (nicht versioniert).
Die Pfade sind Quellpfade; diese Gegner und Bosse sind noch nicht im neuen Spiel eingerichtet.

| Gruppe | Quellpfad / Inhalt |
|---|---|
| Blobs | `Assets/Art/Enemys/Blobs`: mehrere Farben und Größen |
| Augenmonster | `Assets/Art/Enemys/BigManyEyes`, `OneEye`, `FloatingEye.png` |
| Golem | `Assets/Art/Enemys/Golem/BigGolemSprites.png` |
| Echsenkrieger | `Assets/Art/Enemys/LizardWarrior.png` |
| Krabbenboss | `Assets/Art/Enemys/Bosses/CrabBoss*.png`: Körper, Klauen, Zauber, Laufen |
| Retro-Bosse | `Assets/RetroPackages/Retro Pixel Bosses/Sprites`: Bossframes und Angriffseffekte |
| Flame Knight | `Assets/Animations/Bosses/FlameKnight`: Idle, Sleeping, Charge-Vorbereitung |
| Rock Giant | `Assets/Animations/Bosses/RockGiant`: Appear, Idle, zwei Angriffe, Earthquake, Hurt, Death |
| Swamp Witch | `Assets/Animations/Bosses/SwampWitch`: Appear, Idle, zwei Angriffe, TeleportStart, Hurt, Death |
| Knight | `Assets/Animations/Bosses/Knight`: FlowerAttack und Controller |

Die Clipdateien belegen vorhandene Animationsarbeit, aber noch keine vollständige
Anbindung im neuen Projekt. Vor jedem Import Spritebezüge und Frame-Reihenfolge prüfen.
Die bisherigen Baumgegner bleiben als funktionierender Kampftest erhalten.
Als nächster Gegner bietet sich ein Blob mit überschaubarem Bewegungs- und
Angriffsmuster an; anschließend ein Boss mit klaren Phasen und Vorwarnungen.
Grafiken gezielt übernehmen und neue hostgesteuerte Logik anbinden.
