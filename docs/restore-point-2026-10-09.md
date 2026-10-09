# Restore Point vor dem Zaubersystem

Git-Tag: **restore/pre-spells-2026-10-09**. Der Tag markiert den committen Stand
vor der ersten Implementierung der Magie-Grundlage.

Enthalten: manuell bestätigter Netzwerk-/LAN-Stand, Canvas-Inventar und Shared
Stash, Gold und Trank-Schnellzugriff mit Small/Medium/Large-Varianten,
RetroPixel-Testausrüstung, getrennte Basis-/Equipment-Stats und Werteanzeige.
Der Nutzer bestätigt auch den letzten Stat-Spieltest mit „Klappt“.
Saveformat 16, Netzwerkprotokoll 17. Das abgestimmte Zauberkonzept ist dokumentiert,
aber am Restore Point noch nicht implementiert.

Automatische Prüfungen: Stat-/Protokollchecks, Unity-Import und Setup,
Easy-Save-Dateitest sowie echte Host-/Client-Inventar-, Trank-, Lager- und
Equipment-Prüfung bestanden. Windows-Spielassembly zuletzt 9. Oktober 2026,
14:14:13 Uhr, Build mit 0 Fehlern und 1 Warnung. Einzelne zusätzliche Grenztests
stehen im [Prüfstand](verification-2026-10-09.md).

Wiedereinstieg: [Zauberkonzept](spell-system.md), [Charakterwerte](character-stats.md),
[UI-Authoring](inventory-ui-authoring.md), [Items](item-authoring.md).

Zum späteren Vergleichen `git show restore/pre-spells-2026-10-09` verwenden.
Für einen sicheren separaten Vergleich eine neue Branch/Worktree vom Tag
erstellen. Ein harter Reset ist nicht nötig und würde ungesicherte Arbeit gefährden.

Library, Temp, Builds, lokale Spielstände und das gekaufte Easy-Save-Plugin sind
weiterhin ignoriert. Der Tag ist ein Code-/Asset-Restore-Point, kein Backup dieser
lokalen Abhängigkeiten oder Spielstände. Easy Save muss lokal installiert sein;
einen passenden Windows-Build aus dem wiederhergestellten Stand neu erstellen.
Kein Push für diesen Auftrag angefordert.
