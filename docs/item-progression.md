# Item-Fortschritt und Builds

Festgelegt am 9. Oktober 2026. Leitbild: Erkundung und gezielt platzierte
Werkzeuge wie in A Link to the Past, kombiniert mit der Vielfalt wechselbarer
Ausrüstung und Builds wie in Terraria. Eigene Welt, Geschichte und Gestaltung.

## Verbindliche Richtung

- Vorerst keine XP und keine Charakterlevel. Fortschritt entsteht durch
  Gegenstände, Fähigkeiten und erschlossene Gebiete.
- Herzcontainer und Manakristalle erhöhen dauerhaft die Grundkapazität.
  Sie sind selten und bevorzugt bewusst in der Welt platziert. Zufällige
  Drops bleiben ein Vorschlag, kein verpflichtender Fortschrittsweg.
- Warrior, Mage, Ranger und Summoner beschreiben Builds. Keine feste
  Klassenauswahl: Ausrüstung und Fähigkeiten bestimmen den Spielstil.
- Aussehen durch Rüstungen kommt ausschließlich durch angelegte Ausrüstung;
  Körper, Haare und Augen bleiben Teil der Charaktererstellung.
- Persönliches Inventar, Ausrüstung, Gold und Kapazitäten gehören zur Figur
  in der Host-Welt. Der Host entscheidet über Änderungen und speichert auch
  gerade nicht verbundene Figuren.
- Normale Gegner liefern Verbrauchsgüter, Gold und Munition. Truhen und
  Bosse können Ausrüstung liefern. Eine Truhe enthält genau einen Gegenstand.

## Erster Umsetzungsschritt

Gold ist ein eigener Zähler pro Charakter (Grenze 99.999.999), kein belegter
Rucksackplatz. Bestehende Goldstapel werden beim Laden alter Spielstände
einmalig in diesen Zähler überführt. Gold bleibt persönlich, bis später
explizite Handels-/Transferregeln eingeführt werden.

Heil- und Manatränke werden im [gemeinsamen Ring](spell-ring-menu.md) ausgewählt.
Unterschiedliche Größen bleiben eigene Item-IDs; das Icon zeigt die gesamte
Anzahl im Rucksack. Der Host prüft den Verbrauch; Tränke haben keinen Cooldown.
Bei vollen Werten wird kein Trank verbraucht.
Tote Figuren können keine Tränke benutzen. Frühere Schnellplätze sind deaktiviert.

Bedienung: M/View öffnet den Ring, oben/unten wechselt zu Verbrauchsgegenständen,
links/rechts wählt, A/Enter verwendet. Tränke bleiben Inventargegenstände und
können weiterhin direkt im Rucksack benutzt sowie über den Shared Stash getauscht
werden. Die Welt läuft während der Auswahl weiter.

Items haben getrennte Verkaufswerte und Kaufpreise. Waffen besitzen bereits
Schaden und Angriffscooldown, defensive Ausrüstung wirksame Rüstungswerte.
Weitere Boni werden erst angezeigt, wenn sie tatsächlich wirken.

Testmaterial unter `Assets/World/Equipment`: drei RetroPixel-Rüstungen mit
10, 60 und 100 Rüstung, jeweils eine Truhe, Stapel von Heil-/Manatränken,
Gold und ein Baumgegner mit 30 HP. Das Setup ergänzt einmalig
`World/EquipmentTests` im Waldheiligtum und baut vorhandenes Terrain nicht
neu auf. Platzierung ist eine Testanordnung und im Editor frei anpassbar.
Jede weitere Instanz eines Pickups, Gegners oder einer Truhe benötigt eine
eigene persistente ID; duplizierte IDs teilen sich sonst ihren Weltzustand.

Saveformat 14 ergänzt Gold und Trankverweise; Format 15 ergänzt den Shared Stash.
Alte Formate bleiben lesbar. Netzwerkprotokoll 16: Host und Clients müssen
denselben neuen Build verwenden. Bedienung: [Shared Stash](shared-stash.md).

## Nächste Schritte, in dieser Reihenfolge

1. Diese Grundlage im Solo- und LAN-Spiel prüfen und Werte abstimmen.
2. Shared Stash an einem festen Ort (erster Schritt umgesetzt): vom Host gespeicherter Weltspeicher,
   atomare Ein-/Auslagerung, konkurrierende Zugriffe ohne Item-Duplikation.
   Er dient zunächst auch zum Tauschen.
3. Gemeinsame Stat-Berechnung für Ausrüstung: Armor, HP-/Mana-Boni und
   gezielt wenige offensive Werte. Ausrüsten darf nicht kostenlos heilen;
   Ablegen reduziert aktuelle Werte nur bis zum neuen Maximum.
4. Händler und Verkauf: getrennte Preise, gemeinsamer Entdeckungskatalog
   für Truhengegenstände, damit weitere Spieler sie erwerben können.
5. Ein erstes Zauber-/Fähigkeitssystem mit Mana, Cooldown und validierter
   Host-Ausführung. Zunächst eine kleine Auswahl statt vier kompletter Klassen.
6. Blocken, Handschuh-Tragstufen, Dash, Unsichtbarkeit, Spellshield und
   Ranger-/Summoner-Boni schrittweise ergänzen.

## Vorschläge und offene Entscheidungen

- Items droppen: sinnvoll, aber erst nach Shared Stash. Spieler-Drops brauchen
  Mengenwahl, eindeutige IDs, Save/Load und sichere Wiederaufnahme. Wichtige
  Weltwerkzeuge dürfen dadurch keine Sackgassen erzeugen.
- Keine feste Klassenzuordnung und zunächst keine vollständige zufällige
  Stat-Erzeugung. Kleine, nachvollziehbare Kombinationen lassen sich besser
  auf Erkundung und Koop abstimmen.
- Cooldown-Reduktion später begrenzen, Fähigkeiten nicht unbegrenzt stapeln.
- Nicht jede Rüstung bekommt sofort alle denkbaren Werte. Die Magierrobe
  hat im ersten Test nur Armor; Spellpower folgt mit einem wirksamen Zaubersystem.
- Ob seltene Container zufällig droppen, ob sie handelbar sind und ob
  Behälterboni pro Welt begrenzt werden, wird vor ihrer Loot-Integration entschieden.

## Prüfung

Automatisch: Goldgrenzen, atomare Belohnungen bei vollem Inventar,
Tranktyp-Zuordnung, Referenzen nach Verschieben, Save-Daten und alte Goldstapel,
Controller-Navigation sowie ungültige Netzwerkbefehle.
Der native Easy-Save-Dateitest prüft zusätzlich Gold und Trankreferenzen.

Manuell mit neuem Build: beide Tränke aufnehmen, bewegen, benutzen und
gemeinsamen Cooldown prüfen; volle HP/Mana verbrauchen nichts. Gold belegt
keinen Slot. Rüstung aus jeder Testtruhe anlegen und Aussehen/Armor vergleichen.
Mit mehreren Spielern getrennte Gold-/Trankbestände, Truhen, Save/Load und
Disconnect/Rejoin prüfen. Diese manuellen Tests stehen für diesen Schritt aus.

### Automatischer Prüfstand vom 9. Oktober 2026

- `tests/EconomyChecks.cs`: bestanden, einschließlich eines zwischenzeitlich
  gespeicherten, abwesenden Charakters mit alten Goldstapeln.
- `tests/InventoryStateChecks.cs`, `WorldSessionChecks.cs` und
  `CoopProtocolChecks.cs`: bestanden.
- Unity-Import und natives Prefab-Setup: bestanden. Die Szene wurde ausschließlich
  um die Testgruppe ergänzt; vorhandene Grid-/Terrain-Inhalte wurden beibehalten.
- Nativer Easy-Save-Dateitest: bestanden, Gold und Trankreferenzen bleiben erhalten.
- Isolierter Zwei-Prozess-Test mit dem echten Development-Build: bestanden.
  Der Client benutzt HP-/Mana-Tränke über Netzwerkbefehle; der Host prüft Verbrauch,
  persönliche Goldbestände und einen echten Datei-Roundtrip. Beide Tränke teilen
  ihren Cooldown; ein Heiltrank bei vollen Herzen verbraucht nichts.
- Windows-Build `Builds/LocalCoop/SecretsReborn.exe`: erfolgreich, 0 Fehler,
  1 Warnung. Die aktualisierte Spielassembly stammt vom 9. Oktober, 11:19 Uhr.
  Für Mehrspielertests denselben vollständigen Buildordner auf allen PCs verwenden.
  Dies war der Build der ersten Grundlage; der neuere Shared-Stash-Build und
  dessen Prüfstand stehen in [Shared Stash](shared-stash.md).

Der opt-in Testtreiber `EconomyIntegrationDriver` läuft ausschließlich in einem
Development-Build mit `--economy-role host|client --economy-report <Dateipfad>`.
Beide Berichte müssen im selben separaten Testordner liegen. Er verwendet
Test-Spielstände und eigene Verbindungsdaten; Berichte/Builds bleiben in `Temp`/
`Builds` und werden nicht versioniert. Das ersetzt keinen manuellen UI-/LAN-Test.
