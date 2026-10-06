# Kampf, Beute und Truhen

## Erste Regeln – anpassbare Vorschläge

Normale Gegner haben eine eigene Beutetabelle ausschließlich für Gold, Verbrauchsgüter und Pfeile. Ausrüstungsgegenstände sind dort ausgeschlossen. Die Tabellen für Bosse und Truhen dürfen Rüstung und Waffen enthalten. Die erste Baum-Beute ist fest: 5 Gold, 1 Heiltrank und 5 Pfeile. Die Testtruhe enthält eine Übungsrüstung und das rote Übungsschwert. Zufallsgewichte und ein Bossgegner sind noch nicht implementiert; BossEquipment ist die vorbereitete Tabelle.

Gold und Pfeile belegen vorerst stapelbare Taschenplätze. Händler und Bogenverbrauch folgen später. Der Heiltrank heilt ein Herz und wird mit A / Enter / Rechtsklick in der Tasche benutzt. Bei voller Gesundheit bleibt er erhalten. ItemDefinition bietet ebenfalls einen ManaPotion-Typ; ein Mana-Trank-Asset ist noch nicht angelegt.

## Persistenz

Jede Truhe braucht eine weltweit eindeutige Chest ID. Erst eine vollständig erfolgreiche Belohnung schreibt `chest:<id>` in den bestehenden Host-Weltzustand. Bei voller Tasche bleiben Inhalt und Truhe erhalten. Eine Truhe wird nur einmal pro Welt geleert, nicht einmal pro Charakter. Die Belohnung erhält der interagierende Charakter.

Gegnerbeute ist als EnemyLoot-Gruppe außerhalb des Gegnerobjekts angelegt. Der Gegner-Tod aktiviert die Pickups. Enemy ID, Drop-IDs `loot:<enemy-id>:<index>`, Position und Inhalte sind fest in der Szene hinterlegt. Besiegte Gegner und eingesammelte Drop-IDs werden bereits gespeichert; nicht eingesammelte Drops werden nach Laden oder Gebietswechsel wieder sichtbar. Bei einem neuen Gegner muss auch die Beutegruppe kopiert und mit einer eigenen Enemy ID und eigenen World Item IDs versehen werden.

Die Holztruhe stammt aus Secrets, Retro Pixel Dungeons: `Assets/RetroPackages/Retro Pixel Dungeons/Tileset/rpd_tileset.psd`, Commit 99f5b1c. Original-Frames `wood_chest_1` bis `wood_chest_4`, jeweils 32 × 32; Pixelmaßstab 32 PPU, Fuß-Pivot (0,5 / 0,25). Öffnen spielt alle vier Frames ab; Laden einer geöffneten Truhe zeigt unmittelbar Frame 4. Das neue TreasureChest-Prefab verwendet die ursprüngliche Grafik, neue Laufzeitlogik und einen Collider an der Basis. Alte Secrets-Skripte und SO-Speicherlogik werden nicht übernommen.

## Rüstung und Tod

Vorschlag für die erste Rüstungsformel: eingehender Schaden × 100 / (100 + Summe der angelegten Rüstungswerte). Auf halbe Herzen aufrunden; mindestens ein halbes Herz bleibt als Schaden. 100 Rüstung halbiert größere Treffer. Ein Treffer von einem halben Herz bleibt deshalb auch mit Rüstung ein halbes Herz. Die vorhandene Übungsrüstung erhält zunächst 25 Rüstung. Werte und Formel bleiben anpassbar.

Bei null Herzen stoppen Bewegung, Angriffe, Aufheben, Truhen, Speicherbuch und Gebietswechsel. Enter / A oder der Retry-Button kehrt im Solo-Prototyp zum letzten erfolgreich gespeicherten oder geladenen Zustand zurück, mit vollen Herzen und Mana. Fortschritt seit diesem Zustand wird zurückgesetzt. Vor dem ersten Speichern gilt der initiale Szenenstart als Rückkehrpunkt. Diese Wiederholung schreibt keine Speicherdatei und lädt den Host-Snapshot neu. Für eine spätere Koop-Sitzung ist eine eigene Wiederbelebungsregel nötig; der komplette Welt-Rollback wird bei mehreren Charakteren blockiert.

## Prüfen in Play

1. Die Truhe nahe dem Start mit E / A öffnen; Rüstung und Schwert im Inventar kontrollieren. Erneutes Öffnen gibt keine weiteren Items.
2. Mit voller Tasche versuchen: keine Teilbelohnung, Truhe bleibt geschlossen.
3. Den Baum besiegen: Gold, Heiltrank und Pfeile aufheben. Am Buch speichern und laden; bereits eingesammelte Beute erscheint nicht erneut.
4. Einen Heiltrank verletzt und bei voller Gesundheit ausprobieren.
5. Am Buch speichern, Schaden bis null Herzen nehmen und Enter / A drücken. Position, Inventar, Rätsel und Beute sollten dem Speicherstand entsprechen; Herzen und Mana sind voll.

Beim Öffnen pausiert der Solo-Prototyp die Welt während der Beuteanzeige. Die Truhenanimation verwendet Echtzeit; jedes erhaltene Item erscheint für 1,2 Sekunden über der Truhe, bei mehreren Items nacheinander. Farbe und zugeschnittener Icon-Bereich stammen vom Item. Bewegung, Kampf, Laterne, Inventar und Speicherbuch sind währenddessen gesperrt. Abbruch, Deaktivieren und Szenenwechsel geben die Pause wieder frei. Ein späterer Netzwerkadapter muss eine gemeinsame Pause explizit replizieren.
Die Truhe verwendet weiterhin das Retro-Pixel-Dungeons-Sheet. Seine eingebetteten halbtransparenten Schatten werden nur im Truhenmaterial ausgeblendet (Alpha-Cutoff 0,99); die opaken Pixel bleiben unverändert und weiterhin beleuchtet. Das Prefab und die vorhandene Truhe werden auf Faktor 1,35 skaliert. Die geprüften übrigen Sheets lieferten keine eindeutig zuordenbare schattenfreie Holztruhenvariante; die neuere Animationsreferenz 614f9fee ist in den geprüften Quellen nicht auflösbar.

Aktualisierung: Der Game-Over-Screen erscheint erst nach Abschluss der Death-Animation und nur bei vollständiger Niederlage der aktiven Gruppe. Details und Revive-Testmenüs: [Tod und Gruppe](party-death.md).
