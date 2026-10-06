# Tod, Gruppen-Game-Over und Revive

`CharacterVitalsState` bleibt der Host-Zustand: 0 HP bedeutet kampfunfähig. Heilung und Herzcontainer können einen gefallenen Charakter nicht wiederbeleben. `Revive(halfHearts)` ist eine eigene Operation mit positiver, auf das Maximum begrenzter Herzmenge. `GameSession.ReviveCharacter` ist der vertrauenswürdige Host-Einstieg; Die Halte-Interaktion nutzt `CanRevive`/`HoldRevive`: Der Host prüft aktive Gruppenmitglieder, verschiedene Charakter-IDs, gemeinsame Szene, Sichtlinie, 1,6 Einheiten Reichweite, lebenden Helfer und abgeschlossene Death-Animation.

`CharacterDeath` präsentiert die Phasen Alive → Dying → Downed. Bewegung und Angriffe sind sofort bei 0 HP gesperrt. Die Laterne erlischt, Runenkreise werden nicht mehr aktiviert, und feste Charakter-Collider werden deaktiviert, damit die Körper lebenden Mitspielern nicht den Weg versperren. Der Baumgegner ignoriert tote Charaktere bereits über die Host-Vitals. Ein Revive stellt die vorherigen Collider-Zustände wieder her; Inventar und Welt werden dabei nicht zurückgesetzt.

Die vorhandenen Retro-Pixel-Frames werden für Körper, Rüstung, Haare, Augen und Accessoires gemeinsam verwendet: Take Damage/Hurt passend zur Blickrichtung, danach die liegenden Frames Sleep (57) und Dead (58). Die Dauer beträgt etwa eine Sekunde. Es werden keine fremden Waffenanimationen als Tod verwendet.

GameSession führt eine explizite aktive Gruppe über Charakter-IDs. Ein Szenenwechsel entfernt keine Mitglieder. Erst wenn alle registrierten Gruppenmitglieder 0 HP haben und die Death-Animation auslaufen konnte, erscheint der Game-Over-Screen. Eine leere Gruppe gilt nicht als besiegt. Ein erfolgreicher Revive oder ein neues lebendes Gruppenmitglied hebt Game Over auf. Beim späteren Multiplayer-Anschluss muss der Host Registrierung, Abmeldung und die Replikation der Vitals übernehmen; historische Offline-Charaktere aus dem Speicherstand werden nicht automatisch zur aktiven Gruppe gezählt.

Der vorhandene Solo-Button lädt den letzten Checkpoint. Er steht nach einem einzelnen Tod in einer lebenden Gruppe nicht zur Verfügung. Ein gemeinsamer Neustart mit mehreren Spielern bleibt für die künftige Netzwerk-Sitzungsverwaltung vorbereitet; aktuell werden dabei keine anderen Charaktere durch einen Solo-Reload gelöscht. E / Controller A drei Sekunden halten: Der Mitspieler wird mit einem Herz wiederbelebt. Fortschritt erscheint im Interaktionshinweis. Bewegung über 0,1 Einheiten oder Schaden unterbricht; die Taste muss danach neu gedrückt werden. Loslassen, Menüs, Fokusverlust, Szenen-/Weltwechsel, Reichweitenverlust oder ein bereits wiederbelebtes Ziel brechen ebenfalls ab. Helfer bleiben für Gegner angreifbar und können währenddessen nicht zuschlagen. Vorschläge für später: eigene Helferanimation, Kosten und alternative Revive-Fähigkeiten. Der Netzwerkadapter muss gehaltene Eingabe/Abbruch mit Spielerberechtigung an den Host übermitteln.

## Testen im Editor

Solo: Play → `SecretsReborn → Character → Test → Party → Down local character`. Erst Death-Animation, dann Game Over und Solo-Retry. `Revive local character` bringt den Spieler ohne Save-Rollback mit einem Herz zurück.

Gruppenregel ohne Netzwerk testen:

1. `Add living test companion` erzeugt einen zweiten Charakter mit eigener ID und eigenem Zustand; er hat keine lokale Eingabe.
2. `Down local character`: kein Game Over, der Hauptcharakter bleibt am Boden. Der Baum darf ihn nicht verfolgen.
3. `Down test companion`: nach der Animation erscheint Game Over.
4. `Revive local character`: Game Over verschwindet, Bewegung und feste Collider sind wieder vorhanden.
5. `Remove test companion` beendet den Test; der Testcharakter ist nur für diese Play-Sitzung angelegt.

Standalone-Checks prüfen eine Gruppe mit vier Mitgliedern, Einzeltod, vollständige Niederlage, leere Gruppe, Revive-Grenzen, normale Heilung und den gespeicherten toten Zustand.


Spielbare Interaktion testen: Add living test companion, dann Down test companion. Nach der Death-Animation zum Begleiter gehen und E / A drei Sekunden halten. Taste loslassen, Weglaufen und Schaden sollen Fortschritt abbrechen. Nach erfolgreichem Revive bleiben Inventar und Welt unverändert.


Die lokale Host-/Client-Anbindung für diese Regeln ist in [local-coop.md](local-coop.md) beschrieben und mit zwei unabhängigen Spielprozessen geprüft.
