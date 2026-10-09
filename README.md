# SecretsReborn

SecretsReborn ist ein neues 2D-Action-Adventure mit Schwerpunkt auf Erkundung und Rätseln. Das Erkundungsgefühl von A Link to the Past dient als Inspiration; Welt und Geschichte sind eigenständig.

## Grundlage

- Vollständig allein spielbar, optional Koop mit insgesamt maximal vier Spielern.
- Der Host speichert die Welt und sämtliche Charaktere dieser Welt.
- Jeder Spieler besitzt eigenes Inventar, eigene Ausrüstung, eigenes Gold und eigenen Fortschritt. Vorerst keine XP oder Charakterlevel; Fortschritt durch Erkundung und Gegenstände.
- Charaktere sind an die jeweilige Host-Welt gebunden.
- Innerhalb eines Gebiets bewegen sich Spieler frei; größere Gebietswechsel erfolgen gemeinsam.
- Secrets ist ausschließlich ein optionaler Asset-Fundus. Alte Inhalte müssen nicht übernommen werden.
- Zunächst werden Platzhaltergrafiken verwendet.

## Dokumentation

- [Item-Fortschritt und Builds: Gold, Tränke, Testausrüstung und nächste Schritte](docs/item-progression.md)
- [Items in Unity anlegen](docs/item-authoring.md)
- [Canvas-Inventar und Lager im Unity-Editor gestalten](docs/inventory-ui-authoring.md)
- [Charakterwerte, Equipment-Boni und Rüstungsformel](docs/character-stats.md)
- [Vereinbartes Zaubersystem: Ringmenü, Zielwahl, Ränge und Loot](docs/spell-system.md)
- [Erster Host-Zauberablauf und vorläufige Testtasten](docs/spell-casting.md)
- [Editierbares Zauber-Ringmenü, Zauberbuch und Controller-Bedienung](docs/spell-ring-menu.md)
- [Shared Stash: gemeinsames Lager und Transfers](docs/shared-stash.md)
- [Vision und feste Rahmenbedingungen](docs/vision.md)
- [Erster Abenteuerentwurf](docs/first-adventure.md)
- [Entwicklungsplan und technische Prüfung](docs/development-plan.md)
- [Lokaler Koop: Host, Beitritt und Zwei-Prozess-Test](docs/local-coop.md)
- [Gemeinsame Multiplayer-Gebietswechsel](docs/multiplayer-area-transitions.md)
- [Gruppenabfrage, Gäste-Runen und Multiplayer-Laden](docs/multiplayer-votes-and-load.md)
- [Persönliche Truhenbelohnung und gemeinsame Fund-Freischaltung](docs/chest-rewards.md)
- [Gemeinsamer Game-Over-Retry und Start-Checkpoint](docs/party-retry.md)
- [MainMenu, Host-Lobby und vier feste Weltcharaktere](docs/main-menu-and-lobby.md)
- [Gemeinsames Speicherbuch, drei Multiplayer-Slots und Menüführung](docs/shared-save-book.md)
- [Charakterlöschung, Gründerfigur, Papierkorb und spätere Wiederbeschaffung](docs/character-and-save-lifecycle.md)
- [Menüs, RetroPixel-Import und Charaktergestaltung](docs/menu-art-and-character-creator.md)
- [Laufender Beitritt, Reconnect und mehrere Clients auf einem Rechner](docs/reconnect.md)
- [Aktuelle Übergabe und nächste Schritte: 9. Oktober 2026](docs/handoff-2026-10-09.md)
- [LAN-Test mit vier PCs](docs/lan-test.md)
- [Manuell bestätigter Prüfstand und nächste Schritte: 9. Oktober 2026](docs/verification-2026-10-09.md)
- [Übergabe zum Tagesabschluss: 6. Oktober 2026](docs/handoff-2026-10-06.md)
- [Prüfergebnisse: 6. Oktober 2026](docs/verification-2026-10-06.md)

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
Commits und Pushes nur nach ausdrücklichem Auftrag ausführen.
