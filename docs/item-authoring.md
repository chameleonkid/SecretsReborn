# Gegenstände in Unity anlegen

Ein Gegenstand besteht aus einer **ItemDefinition** (Asset) und optional einem
Objekt in der Szene. Die ItemDefinition ist die unveränderliche Vorlage:
Name, Icon, Kategorie, Werte und Aussehen. Inventarstapel, Besitz und Ausrüstung
sind Laufzeit-/Savegame-Daten, keine Änderungen an diesem Asset.

Das Inventar-GameObject und sein Layout liegen separat im
`Assets/Resources/InventoryUI/InventoryCanvas.prefab`. Die Oberfläche ist jetzt
im Prefab-Modus editierbar: [Canvas-Inventar gestalten](inventory-ui-authoring.md).

## Beispiel: neue Rüstung

1. Im Unity-Project-Fenster `Assets/World/Equipment/Items` öffnen.
2. Rechtsklick → **Create → SecretsReborn → Item**. Das Asset sinnvoll benennen,
   beispielsweise `forest-guardian-armor`.
3. Im Inspector die Felder setzen:
   - **Item Id:** `forest-guardian-armor`. Weltweit eindeutig, später nicht
     umbenennen: Savegames speichern diese ID.
   - **Display Name:** `Waldwächterrüstung`.
   - **Max Stack:** `1`, **Purpose:** `Equipment`, **Kind:** `Armor`.
   - **Armor Appearance:** eine passende RetroPixel-ClothingAppearance aus
     `Assets/Resources/CharacterLooks`, zum Beispiel das Warrior-Outfit.
     Bereits angelegte Outfits enthalten die passende andere Körpervariante.
   - **Icon:** ein einzelner Sprite für das Inventar. Ohne eigenes Icon dient
     bei einer Rüstung ihr frontaler Outfit-Frame als Vorschau.
   - **Quality:** Normal / Uncommon / Rare / Epic / Legendary.
   - **Armor Value**, **Sell Value**, **Buy Price:** gewünschte Testwerte.
   - **Bonus Hearts**, **Bonus Mana:** zusätzliche Maximalwerte nur während
     das Item angelegt ist. Anlegen füllt sie nicht auf; [Stat-Regeln](character-stats.md).
     Der Kaufpreis ist mindestens so hoch wie der Verkaufswert.
   - **Description:** optionaler eigener Text. Wirksame Werte ergänzt das
     Inventar automatisch; Werte nicht erneut in den Text schreiben.
4. `Assets/World/Sanctuary/Prefabs/Player.prefab` öffnen. In **Character Inventory
   → Catalog** einen Eintrag ergänzen und das neue Item hineinziehen.
   Vorhandene Einträge erhalten; keine doppelten Item-IDs erzeugen.
   Den Katalog am Player-Prefab pflegen, damit neu gespawnte Koop-Figuren ihn
   ebenfalls erhalten.
5. Prefab speichern und schließen. **SecretsReborn → Coop → Prepare local test**
   aktualisiert das Multiplayer-Player-Prefab. Für Tests mit EXEs anschließend
   **SecretsReborn → Coop → Build local Windows test player** verwenden;
   laufende Test-EXEs vorher schließen.

Rüstungen werden durch Equipment angelegt, nicht im Charakter-Creator gewählt.
Die Rüstungsformel wird separat abgestimmt; sie wurde für den Shared Stash
nicht verändert.

## Andere Gegenstände

| Gegenstand | Einstellungen |
|---|---|
| Schwert/Axt/Streitkolben | Purpose Equipment, Kind Weapon, Weapon-Profil zuweisen; Two Handed bei Bedarf |
| Lampe | Purpose Equipment, Kind Lamp, Lamp-Profil zuweisen |
| Heiltrank | Kind None, Purpose HealthPotion, Max Stack z. B. 10, Use Amount in halben Herzen |
| Manatrank | Kind None, Purpose ManaPotion, Use Amount in Mana-Einheiten |
| Gold | Kind None, Purpose Gold; geht beim Aufheben direkt an den persönlichen Goldzähler |

Neue Profile: **Create → SecretsReborn → Weapon profile / Lamp profile**.
Ein Weapon-Profil bestimmt Angriffsbilder, Farbe, Schaden und Cooldown.
Nur ein Waffenicon zuzuweisen erzeugt keine neue Angriffsanimation.
Outfits sind animierte ClothingAppearance-Assets, keine einzelnen Inventaricons.

### Trankgrößen und Schnellzugriff

Unter `Assets/World/Equipment/Items` sind eigene Small-/Medium-/Large-Items
für HP und Mana angelegt. Vereinbarte Werte:

| Größe | Heilung | Mana | Maximaler Stapel |
|---|---|---|---|
| Klein | 1 Herz (Use Amount 2) | 10 | 10 |
| Mittel | 3 Herzen (Use Amount 6) | 25 | 10 |
| Groß | 6 Herzen (Use Amount 12) | 100 | 10 |

Es wird höchstens bis zum persönlichen Maximum geheilt/aufgefüllt. Auch große
Tränke haben keinen Cooldown. Bei vollen Werten wird kein
Trank verbraucht. Es gibt genau drei Größen pro Trankart.
Die vorhandenen `health-potion`-/`mana-potion`-IDs bleiben für alte Saves erhalten
und zählen zur kleinen Größe. Ihre Mengen werden im Ring mit den jeweiligen
Small-Items zusammengefasst und aus beiden Beständen verbraucht.

Im Waldheiligtum liegen unter **World → PotionTests** sechs Pickups, jeweils
ein Trank pro Flasche. Erste Reihe HP, zweite Reihe Mana; von links Small, Medium, Large.
Die Flaschen verwenden die bisherigen Icons und sind in der Welt je nach Größe
unterschiedlich groß. Im regulären Inventar sind Tränke ausgeblendet.
Die zugehörigen Pickup-Prefabs liegen unter `Assets/World/Equipment/Prefabs`.

Tränke liegen weiterhin im Rucksack. Der [gemeinsame Ring](spell-ring-menu.md)
zeigt sie pro Größe mit der gesamten Anzahl und ihrer Wirkung. M/View öffnen,
oben/unten zu Verbrauchsgegenständen wechseln, links/rechts wählen und A/Enter
benutzen. Kleine/mittlere/große Varianten sind eigene Einträge. Die früheren
Schnellplätze und deren Tasten entfallen. Lagertransfers erfolgen über die
Rucksackstapel; alte Item-IDs und Save-Bindungsdaten bleiben kompatibel.

**SecretsReborn → Equipment → Configure three potion sizes** aktualisiert
Namen und Wirkwerte der Trankdefinitionen und setzt die Mengen der sechs
Pickup-Prefabs auf einen Trank. Szenen und GUIDs bleiben erhalten.

**SecretsReborn → Equipment → Prepare potion variants** ergänzt fehlende Items,
Pickup-Prefabs, Katalogeinträge und die Testgruppe. Vorhandene Trankvorlagen und
eine vorhandene Testgruppe werden beibehalten. Beim ersten Durchlauf werden
die Schnellplätze samt Labels auf die Equipment-Seite verschoben; weitere
Durchläufe erhalten dieses Layout. Diese frühere Schnellplatz-Positionierung
ist inzwischen abgelöst: **UI → Prepare consumable ring** deaktiviert die
alten Plätze. Ihre Prefab-Verweise bleiben für die bestehenden Editor-Tools erhalten.

## Icons sauber anzeigen

PNG-Import: Texture Type **Sprite**, **Point (no filter)**, Compression **None**,
Mipmaps aus. Bei Sheets zuerst einzelne Sprites korrekt schneiden.
Das Item-Icon muss auf den passenden Einzel-Sprite zeigen.

**SecretsReborn → Equipment → Normalize item icon bounds** misst die sichtbaren
Pixel innerhalb des bisherigen Icon-Ausschnitts und aktualisiert **Icon Content**.
Die Bilddatei wird dabei nicht verändert. Alle Inventar- und Lagerslots verwenden
dieselbe zentrierte Icon-Fläche und behalten das Seitenverhältnis bei.
Nach Austausch eines Icons dessen **Icon Content** zunächst auf `0, 0, 1, 1`
setzen und erneut normalisieren, damit kein alter Ausschnitt erhalten bleibt.

## In einer Testtruhe oder als Pickup platzieren

Eine **Loot table** über Create → SecretsReborn anlegen, Source **Chest**,
Entries genau **ein** Eintrag, Count **1**, Item zuweisen. Ein Testtruhen-Prefab
unter `Assets/World/Equipment/Prefabs` in die Szene ziehen und in **Treasure Chest**
die Loot-Tabelle sowie eine **eigene Chest Id** setzen.

Ein Pickup benutzt **World Item** mit Item, Count und eigener **World Item Id**.
Beim Duplizieren von Truhen/Pickups/Gegnern immer ihre persistente ID ändern.
Gleiche IDs bedeuten absichtlich denselben Weltzustand.

Gegnerbeute verwendet die gespeicherte Todesposition des zugeordneten Gegners.
Die platzierten Pickups definieren weiterhin die Streuabstände relativ zum
Gegner-Spawn. `EnemyLoot` verschiebt die Gruppe um Todesposition minus Spawnposition;
wiederholte Aktualisierungen addieren den Versatz nicht erneut.
Saveformat 18 speichert diese Positionen, Netzwerkprotokoll 23 überträgt sie vom Host.
Alte Spielstände bleiben lesbar; bereits besiegte Gegner ohne gespeicherte
Todesposition behalten die bisherigen Drop-Orte. Neue Tode verwenden den aktuellen Ort.

Der Shared Stash besitzt keine Loot-Tabelle: Sein Inhalt entsteht durch
Einlagern und liegt einmal pro Host-Welt im Spielstand.
