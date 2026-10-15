# Prüfstand vom 11. Oktober 2026

## Aktualisierung: Trainingspuppe, Crit-Schalter und Flugvariation (17:23 Uhr)

Aktueller Build: `Builds/Windows-20261011-172310/SecretsReborn.exe`,
0 Fehler und 2 Buildwarnungen. Protokoll 25; Saveformat weiterhin 18.

- Normale Schadenszahlen sind gelb (Größe 26), Crits bleiben orange (Größe 40).
- Im Editor/Development-Build schaltet F5 oder der Button unten links 100 % Crits
  für die eigene Figur ein/aus. F6 bleibt dem Koop-Menü vorbehalten.
  Der Override wird vom Host bestätigt und verändert keine Itemwerte oder Saves.
- Stationäre Trainingspuppe mit 1.000.000 HP nahe Startpunkt/Testtruhen des
  Waldheiligtums, ohne Angriffe oder Beute; HP-Anzeige auch auf Clients.
- Mehrfach-Feuerbälle variieren auf dem Host Bogenweite, Auffächerungszeit und
  Flugtempo. Zielbudget und Crit-Multiplikator bleiben unverändert.
- Nativer Zwei-Prozess-Test des Builds 17:20 besteht:
  `Temp/CombatTrainingTest-20261011-172102/host.txt` und `client.txt`.
  Client schaltet den Testoverride an/aus bei unveränderter Equipment-Critchance
  von 0 %. Rang-3-Feuerball reduziert die HP 1.000.000 → 999.988 → 999.976,
  mit 0,284 Sekunden Abstand zwischen den beiden Treffern. Client prüft die
  replizierte HP-Anzeige. Puppe bleibt stehen und kann keinen Kontaktschaden geben.
- Vorhandene Inventar-/Stash-/ES3-/Mana-/Heal-/Crit-Regressionen bestehen.
  `TrainingDummy.png` und `YellowDamageNumber.png` wurden visuell geprüft.
  Beide Testprozesse sind beendet, keine Script-Exceptions/Testfehler.
- Abschließender Build 17:23 übernimmt das kollisionsfreie F5-Kürzel und kompiliert
  erfolgreich; der Zwei-Prozess-Test lief mit derselben Kampf-/Netzwerklogik.

Manuell offen: F5/Button und mehrere Projektile im normalen Spiel beurteilen,
ebenso Koop über zwei getrennte PCs. Keine Commits oder Pushes.

## Aktualisierung: Critchance und Schadenszahlen (17:02 Uhr)

Aktuell: `Builds/Windows-20261011-170225/SecretsReborn.exe`, Spielassembly
17:02:31 Uhr. Erfolgreicher Build mit 0 Fehlern und 2 Buildwarnungen.
Netzwerkprotokoll 24, Saveformat weiterhin 18. Alle Test-PCs benötigen denselben
vollständigen Build. Dieser Ausbau einschließlich Auffächerung ist noch lokal.

- `tests/CharacterStatsChecks.cs` besteht einschließlich additiver Critchance,
  100-%-Grenze, 0-/100-%-Treffern, Rollgrenzen, ungültigen Eingaben,
  doppeltem Schaden, Überlaufschutz und Rüstung nach Crit-Multiplikator.
- `tests/CombatChecks.cs` besteht weiterhin. Unity kompiliert den Laufzeitcode.
- Echter Host-/Client-Test besteht:
  `Temp/CriticalDamageTest-20261011-170311/host.txt` und `client.txt`.
  Waffenprofil mit 5 %, Waffenitem mit +3 % und Rüstung mit +7 % ergeben 15 %;
  der Waffenbonus im Rucksack wird ausgeschlossen. Anschließend erzwingt die
  Testausrüstung 100 % und 0 %, und echte Client-Nahkampfanfragen verursachen
  entsprechend 2 beziehungsweise 1 Schaden mit der unbewaffneten Figur.
- Client prüft goldorange Critzahlen mit Schriftgröße 40, weiße normale Zahlen
  mit Schriftgröße 26 und Schadenszahlen beim verletzten Spieler.
  Ein garantierter Rang-3-Feuerball-Crit verdoppelt den Zielanteil auf 24 und
  zeigt beim ersten Projektil 12 kritisch an. Der Gegner mit 6 HP stirbt;
  der Anteil des anschließend verlorenen zweiten Projektils verfällt wie bisher.
- `CriticalDamageNumber.png`, `CriticalFireballNumber.png` und
  `EquippedStatsCanvas.png` im Testordner wurden visuell geprüft: Farbe,
  größere Schrift und vierzeiliges Stat-Panel sind sichtbar und lesbar.
- Inventar, Stash, echte ES3-Saves, Feuerballauffächerung, Mana und Heal bestehen
  weiterhin. Testboni werden nur im isolierten Testprozess gesetzt; keine
  Itemassets werden dabei verändert. Beide Prozesse sind beendet; keine
  Script-Exceptions oder Testfehler. D3D12-Start-/Cleanuphinweise bleiben wie zuvor.

Manuell offen: Schadenszahlen im normalen Spiel bei verschiedenen Zoomstufen
und mehreren gleichzeitigen Treffern beurteilen. Kontaktgegner critten bislang
nicht, Heal ebenfalls nicht. DoT-Effekte sind noch nicht implementiert.
[Crit-Konfiguration und Regeln](character-stats.md).

## Aktualisierung: aufgefächerte Feuerbälle (15:59 Uhr)

Aktuell: `Builds/Windows-20261011-155939/SecretsReborn.exe`, Spielassembly
15:59:45 Uhr. Erfolgreicher Build mit 0 Fehlern und 2 Buildwarnungen.
Saveformat 18 und Netzwerkprotokoll 23 bleiben unverändert.

Mehrere Feuerbälle starten dicht am Charakter, fächern auf kubischen Kurven
quer zur Zielrichtung auseinander und gehen nach etwa 0,3 Sekunden in den
Zielanflug über. Kurze Zieldistanzen verkleinern den Bogen. Sprites richten sich
an der tatsächlichen Bewegung aus; Zielwahl und Schadensbudget bleiben erhalten.

- `tests/SpellCastChecks.cs` besteht weiterhin.
- Echter Host-/Client-Test besteht:
  `Temp/FireballSpreadTest-20261011-160015/host.txt` und `client.txt`.
  Der Client prüft beide sichtbaren Projektile und nach 0,18 Sekunden mindestens
  0,5 Welteinheiten Abstand. `ReplicatedFireballs.png` wurde visuell geprüft.
  Die bisherigen Schadens-, Mana-, Heal-, Inventar-, Beute- und Saveprüfungen bestehen.
- Optisches Feintuning im normalen Spiel, besonders bei bewegten und nahen Zielen,
  bleibt der nächste manuelle Schritt. Dieser Ausbau ist noch nicht committed/gepusht.

## Aktualisierung: Beute am Todesort (13:56 Uhr)

Aktuell: `Builds/Windows-20261011-135608/SecretsReborn.exe`, Spielassembly
13:56:16 Uhr, erfolgreicher Build mit 0 Fehlern und 1 bekannter Pipelinewarnung.
Netzwerkprotokoll 23, Saveformat 18. Der weiter unten dokumentierte Effektbuild
bleibt ein historischer Prüfstand.

Die Gegnerbeute wird jetzt um den Abstand vom ursprünglichen Spawn zur tatsächlichen
Todesposition verschoben. Diese Position wird gespeichert und vom Host übertragen;
alte Saves ohne Todesposition bleiben lesbar und behalten ihre bisherigen Drop-Orte.

- `tests/EnemyLootPositionChecks.cs` besteht: Roundtrip, Kopien ohne Rückwirkung,
  unveränderte Position bei doppeltem Todesereignis, Migration von Version 17,
  Ablehnung ungültiger, doppelter oder nicht besiegten Gegnern zugeordneter Positionen.
- Die bestehende `tests/WorldSessionChecks.cs` besteht.
- Erweiterter echter Host-/Client-Test besteht:
  `Temp/LootPositionTest-20261011-135636/host.txt` und `client.txt`.
  Host verschiebt den Gegner zwei Einheiten vor dem Töten und prüft aktive Pickups
  am neuen Ort, wiederholtes Refresh ohne Drift und den echten ES3-Dateiroundtrip.
  Der Client prüft die empfangene Todesposition und stabiles Refresh der Beutegruppe.
  Die bisherigen Inventar-, Stash-, Feuerball- und Heal-Prüfungen bestehen weiterhin.
- Beide Testspieler sind beendet; keine Script-Exceptions oder Testfehler.
- Temporärer Buildhelfer entfernt; bestehende Änderungen erhalten, kein Commit/Push.

## Vorheriger Prüfstand: Zaubereffekte (12:50 Uhr)

Projekt: `D:\SecretsReborn\SecretsReborn`, Unity `6000.3.25f1`.
Aktueller Windows-Development-Build:
`Builds/Windows-20261011-125049/SecretsReborn.exe`.
Spielassembly: 12:50:54 Uhr. Ergebnis: erfolgreich, 0 Fehler, 2 Buildwarnungen.
Im Editorlog steht weiterhin die bekannte Warnung des Pakets `com.unity.pipeline`
wegen fehlender RuntimePipelineConfig; diese Meldung betrifft nicht den URP-Renderer.
Netzwerkprotokoll 22, Saveformat 17; für Koop denselben vollständigen Build verwenden.

## Automatisch nachgewiesen

- Unity kompiliert die Spiel- und Editorassembly.
- Nativer Spriteimport: 38 Frames, davon 2 Flug-, 11 Treffer- und 25 Healframes.
  Bericht: `Temp/SpellEffectsReport.txt`.
- `tests/SpellCastChecks.cs` besteht: fixe Budgetverteilung und anschließende
  Aufteilung jedes Zielanteils auf 1–8 Projektile erhalten das Gesamtbudget,
  einschließlich kleiner Budgets und ungültiger Eingaben.
- Zwei echte lokale Windows-Spielprozesse bestehen den erweiterten
  `EconomyIntegrationDriver`. Berichte und Logs:
  `Temp/SpellFlightTest-20261011-125122/host.txt` und `client.txt`.
- Der Host sieht nach Ende der Wirkzeit zwei Rang-3-Feuerbälle und einen noch
  unbeschädigten Gegner. Der Schaden trifft erst nach dem Flug ein, auch durch
  die Testwand. Mana wird nur einmal berechnet; Bewegungssperre und Cooldown gelten.
- Der Client rendert beide replizierten Feuerball-Sprites. Das tatsächliche
  Kamerabild `ReplicatedFireballs.png` im Testordner wurde visuell geprüft.
- Heal verteilt sein Budget auf zwei lebende Figuren. Clientanfragen ändern
  den Zustand ausschließlich über den Host.
- Inventar-/Ringregression: keine Tränke im normalen Inventar, sechs Ringvarianten
  einschließlich zusammengefasster Altbestände, Anlegen/Ablegen von Rüstung,
  Shared-Stash-Handel und echter Easy-Save-Dateiroundtrip bestehen.
- Beide Testprozesse sind beendet. Keine Script-Exceptions oder Netzwerkfehler;
  D3D12 gibt beim Start und Beenden der versteckten Testspieler Geräte-/Cleanuphinweise aus.

## Noch manuell prüfen

Aura, vorhandene Angriffsgeste während des Wirkens und Treffer-/Heileffekte
im normalen Spiel optisch beurteilen. Es gibt noch keinen eigenen Cast-Spritesatz.
Zielverlust während des Flugs, Tod des Zaubernden, Disconnect, Welt-/Gebietswechsel
und Save/Load sind im Code behandelt, aber nicht als eigenständige Szenarien
in diesem Zwei-Prozess-Test nachgewiesen. Ebenso steht ein Test über zwei PCs aus.

Vorhandene Änderungen wurden erhalten. Kein Commit und kein Push.
