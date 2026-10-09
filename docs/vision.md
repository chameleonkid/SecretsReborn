# Vision

## Verbindliche Grundlage

SecretsReborn wird ein eigenständiges 2D-Action-Adventure. Das Erkundungsgefühl von A Link to the Past inspiriert die Ausrichtung; Welt und Geschichte werden neu entwickelt. Erkundung und Rätsel bilden den Schwerpunkt.

Das gesamte Spiel muss allein vollständig spielbar sein. Koop ist optional und umfasst einschließlich Host maximal vier Spieler. Rätsel dürfen keine zusätzlichen Mitspieler voraussetzen.

## Welt und Charaktere

Angelegte Rüstung verändert das Aussehen der jeweiligen Spielerfigur. Diese
Darstellung muss auch für Mitspieler sichtbar sein; Details und geprüfte
Asset-Grundlagen stehen in [character-appearance.md](character-appearance.md).

Der Host speichert die Welt und alle zugehörigen Charaktere. Jeder Spieler hat ein eigenes Inventar, eigene Ausrüstung, eigenes Gold und eigenen Fortschritt. Charaktere gehören ausschließlich zur jeweiligen Host-Welt. Vorerst gibt es keine XP oder Charakterlevel: Erkundung, Gegenstände und seltene Herzcontainer/Manakristalle tragen den Fortschritt. Ausrüstung und Fähigkeiten ermöglichen wechselbare Builds statt fester Klassen. Details: [Item-Fortschritt](item-progression.md).

Festlegung vom 8. Oktober 2026: Der Multiplayer-Spielstand enthält maximal vier
feste Charakterplätze. Lokale Multiplayer-Charakterprofile entfallen. In der Lobby
wählen Host und Clients eine freie gespeicherte Figur oder erstellen auf einem
freien Platz eine neue. Abwesende Figuren bleiben gespeichert; eine Übernahme
durch andere Mitspieler ist erlaubt. Spielernamen und Charaktername sind getrennt.
Singleplayer speichert vollständig lokal. Siehe [MainMenu und Lobby](main-menu-and-lobby.md).

Eine Gründerfigur bleibt vor Löschung geschützt, ihre Teilnahme ist frei.
Löschungen, Wiederherstellung und spätere Absicherung gegen Sackgassen sind in
[Charakter- und Spielstand-Lebenszyklus](character-and-save-lifecycle.md) festgehalten.

Innerhalb eines Gebiets ist freie Bewegung möglich. Größere Gebietswechsel erfolgen
gemeinsam nach Zustimmung aller aktiven Spieler.

## Magie und Belohnungen

Magie erhält ein eigenes Ringmenü mit Elementen, Zielwahl und Zauberrängen.
Zauber ignorieren Umgebungskollisionen; Wirken sperrt die eigene Bewegung.
Unverzichtbare freigeschaltete Gegenstände sind wiederbeschaffbar, gewöhnliche
Waren kaufbar und optionale Bossausrüstung über wiederholbare Dungeons erhältlich.
Die abgestimmte Grundlage und offenen Detailregeln stehen im [Zauberkonzept](spell-system.md).

## Eigenständigkeit und Assets

Secrets dient nur bei Bedarf als Asset-Fundus. Es besteht keine Pflicht, alte Inhalte, Geschichte oder Mechaniken zu übernehmen. Für die erste Entwicklung werden Platzhaltergrafiken eingesetzt.

## Noch offene Entscheidungen

- Konkrete Welt, Figuren und Geschichte über den ersten Abenteuerentwurf hinaus.
- Balancing von Ausrüstung, Fähigkeiten und seltenen Kapazitätserweiterungen.
- Wiederbeitritt während eines laufenden Abenteuers und Umgang mit Verbindungsabbrüchen.
- Internet-Beitritt und weitere Zielplattformen über den aktuellen Windows/LAN-Test hinaus.

## Zusätzliche Ideen – Vorschläge

- Vorschlag: Persönlichen Fortschritt und gemeinsame Veränderungen der Welt getrennt modellieren, damit ein gelöstes Welträtsel und individuelle Charakterdaten eindeutig gespeichert werden können.
- Vorschlag: Koop soll gemeinsame Entdeckungen erleichtern; sämtliche notwendigen Rätselschritte bleiben von einer einzelnen Figur ausführbar.

Diese Vorschläge legen noch keine technische Implementierung fest.
