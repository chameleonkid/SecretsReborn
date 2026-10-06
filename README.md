# SecretsReborn

SecretsReborn ist ein neues 2D-Action-Adventure mit Schwerpunkt auf Erkundung und Rätseln. Das Erkundungsgefühl von A Link to the Past dient als Inspiration; Welt und Geschichte sind eigenständig.

## Grundlage

- Vollständig allein spielbar, optional Koop mit insgesamt maximal vier Spielern.
- Der Host speichert die Welt und sämtliche Charaktere dieser Welt.
- Jeder Spieler besitzt eigenes Inventar, eigene XP und eigenen Fortschritt.
- Charaktere sind an die jeweilige Host-Welt gebunden.
- Innerhalb eines Gebiets bewegen sich Spieler frei; größere Gebietswechsel erfolgen gemeinsam.
- Secrets ist ausschließlich ein optionaler Asset-Fundus. Alte Inhalte müssen nicht übernommen werden.
- Zunächst werden Platzhaltergrafiken verwendet.

## Dokumentation

- [Vision und feste Rahmenbedingungen](docs/vision.md)
- [Erster Abenteuerentwurf](docs/first-adventure.md)
- [Entwicklungsplan und technische Prüfung](docs/development-plan.md)

Der Abenteuerentwurf bleibt anpassbar. Zusätzliche Ideen sind ausdrücklich als Vorschläge gekennzeichnet und keine beschlossenen Anforderungen.

## Lokales Projekt

- Unity 6.3 LTS: **6000.3.25f1** (maßgeblich: `ProjectSettings/ProjectVersion.txt`).
- Projektordner: `D:\SecretsReborn\SecretsReborn`.
- Editor: `C:\Program Files\Unity\Hub\Editor\6000.3.25f1\Editor\Unity.exe`.
- Unity-Projektstruktur: `Assets`, `Packages`, `ProjectSettings`.

Vor einem CLI-Import laufende Editorprozesse und deren Projektpfade prüfen. Bei geöffnetem Projekt den bestehenden Editor verwenden; keinen parallelen Editor für dieselbe Welt starten. Generierte IDE-Dateien werden nicht von Hand gepflegt.

Code und Mechaniken sind freigegeben. Der erste Bewegungsprototyp liegt in
`Assets/Scenes/Waldheiligtum.unity`; siehe [Start und Spieltest](docs/prototype.md).
Für die Kartengestaltung steht die [editierbare Welt mit Tilemaps und Prefabs](docs/world-authoring.md) bereit.
Der optionale [Asset-Fundus Secrets](docs/assets.md) ist erreichbar und wurde erstmals gesichtet.
Keine Commits oder Pushes ausführen.
