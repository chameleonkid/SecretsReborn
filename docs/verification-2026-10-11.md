# Prüfstand vom 11. Oktober 2026

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
