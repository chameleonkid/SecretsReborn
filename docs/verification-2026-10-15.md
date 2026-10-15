# Prüfstand vom 15. Oktober 2026

## Aktualisierung: DoT-Angaben an Waffen und Zaubern

Aktueller Build: `Builds/Windows-DotInfo-20261015-104526/SecretsReborn.exe`.
0 Fehler, 1 bekannte Pipelinewarnung; Protokoll 26 und Saveformat 18 unverändert.
Waffenbeschreibungen und Ringmenü/Zauberbuch zeigen jeden konfigurierten gültigen
DoT mit Typ, Schaden, Tick-Abstand, Dauer und Chance. Zauber zeigen ihren aktuellen Rang.
Der Ring-Infobereich wächst bei längeren Texten, Inventar-/Stash-Details passen ihre
Schrift innerhalb der vorhandenen Fläche an.

Nativer Host-/Client-Test besteht:
`Temp/DotInfoIntegration-20261015-104553/host.txt` und `client.txt`.
Der Client prüft die tatsächliche Rang-3-Burn-Beschreibung und die verfügbare Texthöhe.
Ein nur im Testprozess erzeugtes Waffenprofil prüft gleichzeitig Burn, Poison und
Bleed einschließlich 0,5-Sekunden-Ticks und unterschiedlichen Chancen im echten
Inventar-Detailfeld. Es wird danach zurückgesetzt; keine Itemassets werden verändert.
`SpellTargetRing.png` und `WeaponDotDetails.png` im Testordner wurden visuell geprüft:
alle Effektzeilen sind lesbar und vollständig. Die bisherigen DoT-/Rang-/Koop- und
Inventarregressionen bestehen. Beide Prozesse beendet, keine Script-Exceptions/Testfehler.

## Aktualisierung: DoTs pro Zauberrang

Aktueller Build: `Builds/Windows-DotRanks-20261015-103719/SecretsReborn.exe`.
0 Fehler, 1 bekannte Pipelinewarnung; Protokoll 26, Saveformat 18.
Die On-Hit-DoT-Liste liegt jetzt in jedem `SpellRankDefinition` statt global am
Zauberasset. Fireball-Burn tickt auf Rang 1 / 2 / 3 mit 1 / 2 / 3 Schaden.
Dauer, Abstand und Auslösechance sind ebenfalls pro Rang editierbar.
Das Editor-Foundation-Setup erzeugt diese Werte auch für neue Fireball-Assets.

Timerprüfungen und echter Host-/Client-Test bestehen:
`Temp/DotRankIntegration-20261015-103748/host.txt` und `client.txt`.
Der Host prüft die drei importierten Rangkonfigurationen mit 3 / 6 / 9 Gesamtschaden.
Ein echter Rang-3-Fireball mit garantierten direkten Crits verursacht an der Puppe
24 direkten Schaden und anschließend genau 9 normale Burn-Schadenspunkte:
HP 1.000.000 → 999.976 → 999.967. Drei gleichzeitig applizierte Typen verursachen
danach wie zuvor zwölf zusätzliche Punkte (999.955 HP). Bestehende Regressionen
bestehen; beide Prozesse sind beendet, keine Script-Exceptions/Testfehler.

## Erster DoT-Ausbau vor Rangskalierung

Ursprünglicher Development-Build:
`Builds/Windows-Dot-20261015-102322/SecretsReborn.exe`.
Unity 6000.3.25f1, 0 Fehler, 1 bekannte Warnung aus `com.unity.pipeline`
wegen fehlender optionaler RuntimePipelineConfig. Netzwerkprotokoll **26**,
Saveformat unverändert **18**. Auf allen Test-PCs denselben vollständigen Build verwenden.

## DoT-Nachweise

- `tests/DotChecks.cs` besteht: unabhängige Typen/Timer, Auffrischung ohne Stapel
  oder Tickverschiebung, Wertkopien, Chancengrenzen, ungültige Werte, Aufholen,
  letzter Tick bei Ablauf, 1.200 Ticks über 120 Sekunden und Clear während eines Ticks.
- Spiel- und Editorassembly kompilieren im laufenden Editor; nativer Windows-Build besteht.
- Zwei echte lokale Spielprozesse bestehen den erweiterten `EconomyIntegrationDriver`:
  `Temp/DotIntegration-20261015-102355/host.txt` und `client.txt`.
- Client schaltet 100 % Crits ein und trifft die Trainingspuppe mit Rang-3-Fireball.
  Zwei direkte Crit-Treffer mit 0,265 Sekunden Abstand verursachen insgesamt 24 Schaden.
  Ein aufgefrischter Burn verursacht danach genau drei normale Ticks à 1 Schaden:
  HP 1.000.000 → 999.976 → 999.973. Die Crit-Einstellung verdoppelt keine DoT-Ticks.
- Danach wirken Burn, Poison und Bleed gleichzeitig, jeweils 2 Sekunden mit
  0,5 Sekunden Tick-Abstand: genau zwölf weitere Schadenspunkte, HP 999.961.
  Client prüft drei aktive Effekttypen, die Darstellung, HP und gemeinsames Ablaufen.
- `AllDotsHost.png` und `AllDotsClient.png` im Testordner wurden visuell geprüft:
  orange Flammenlinien, grüne Blasen und rote Blutstriche sind am Ziel sichtbar.
- Der generische Einstieg verursacht beim gerüsteten Spieler korrekt mitigierten
  Schaden. Ein Bleed-Tick tötet einen normalen Gegner, trägt seinen besiegten Zustand
  ein und räumt den Effekt auf. Session-Refresh setzt die Puppe auf volle HP und
  entfernt laufende Effekte.
- Direkte Clientversuche, DoTs anzuwenden, werden abgelehnt. Nur der Host tickt.
- Bestehende Inventar-, Trankring-, Shared-Stash-, ES3-, Crit-, Fireballflug-, Mana-
  und Healprüfungen bestehen weiterhin. Beide Prozesse sind beendet; keine
  Script-Exceptions oder Testfehler. Bekannte D3D12-Start-/Cleanuphinweise bleiben.

## Noch manuell prüfen

Effekte im normalen Spiel bei mehreren bewegten Gegnern beurteilen; Zwei-PC-Test
steht aus. Lade-/Gebietswechselpfade entfernen Effekte im Code, wurden in diesem
Test nicht als eigener Ablauf durchgespielt. Waffen-On-Hit-Anbindung ist vorhanden,
aber noch keiner regulären Waffe standardmäßig zugewiesen und in diesem nativen
Durchlauf nicht per Waffenschlag getestet. Fireball-Burn ist aktiv.

[DoT-Regeln und Authoring](damage-over-time.md). Bestehende Änderungen erhalten;
kein Commit und kein Push.
