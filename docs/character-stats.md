# Charakterwerte und Equipment

Die gemeinsame Stat-Berechnung summiert Armor, Bonus-Hearts und Bonus-Mana
aller angelegten Items. Gegenstände im Rucksack geben keine Boni.
Schaden und Angriffscooldown kommen aus dem Weapon-Profil der Hauptwaffe;
ohne Profil gelten wie bisher 1 Schaden und 0,45 Sekunden.

Im InventoryCanvas zeigt **InventoryPanel/CharacterStats** die Gesamtrüstung,
rechnerische Schadensreduktion, maximale Herzen/Mana samt Basiswert und
Waffenschaden/Cooldown. Position, Schrift und Fläche bleiben im Prefab editierbar.

## Basis und vorübergehende Boni

Start: 3 Herzen und 50 Mana. Heartcontainer erhöhen BaseMaxHealth dauerhaft;
AddManaCrystal ist der Host-Einstieg für spätere dauerhafte Mana-Upgrades.
Ein eigenes Crystal-Pickup ist noch nicht Bestandteil dieses Schritts.
ItemDefinition bietet **Bonus Hearts** (ganze Herzen) und **Bonus Mana**.
Beide Werte werden automatisch in der Beschreibung angezeigt.
Maximal 20 Herzen einschließlich Equipment; Mana hat eine technische Grenze
von 10000. Weitere Build-Werte wie Spellpower folgen mit dem Zaubersystem.

Anlegen erhöht nur die Obergrenze und heilt/füllt nicht auf. Ablegen klemmt
aktuelle Werte auf die niedrigere Obergrenze. Wiederholtes Wechseln erzeugt
keine kostenlose Heilung; ein toter Charakter bleibt tot. Nur die Basiswerte
sind dauerhafter Fortschritt. Saveformat 16 speichert Basis und effektive Werte
getrennt; alte Spielstände ohne Basisfelder übernehmen ihre bisherigen Maxima
als Basis. Der Host berechnet Boni aus dem Katalog und Equipment. Clients
übernehmen HP/Mana aus dem Snapshot und berechnen die Werteanzeige aus demselben
Item-Katalog. Protokoll 17 verhindert das Mischen mit älteren Builds.

## Rüstung

Die bestehende Formel bleibt: ceil(Rohschaden × 100 / (100 + Armor)), mindestens
1 Schadenseinheit = ein halbes Herz. Kein positiver Treffer wird vollständig
neutralisiert. 100 Armor bedeutet rechnerisch 50 Prozent Reduktion; die
Rundung auf halbe Herzen bestimmt das tatsächliche Ergebnis.
Beispiele: 4 halbe Herzen werden bei 60 Armor zu 3, bei 100 Armor zu 2.
Ein ohnehin nur halbherziger Treffer bleibt auch mit Rüstung ein halbes Herz.
Das ist eine wichtige Balancing-Grenze bei schwachen Kontaktgegnern.

## Vorläufige Testboni

| Testitem | Armor | Herzen | Mana |
|---|---|---|---|
| Wächterrüstung | 60 | +1 | +0 |
| Waldmagierrobe | 10 | +0 | +30 |
| Quellenhüterrüstung | 100 | +2 | +10 |

Diese Werte sind Testvorschläge, kein endgültiges Klassen-Balancing.
Bestehende Testtruhen liefern dieselben Items. **SecretsReborn → Equipment →
Prepare character stats** ergänzt das Panel und setzt diese drei Testboni.
Vorhandene Layoutpositionen des Stat-Panels bleiben erhalten; das Menü setzt
die Testitem-Boni bei erneutem Aufruf erneut.

## Tests

CharacterStatsChecks prüft Basis/Equipment-Trennung, Wechsel ohne Heilung,
Ablegen, dauerhafte Upgrades, Tod, Grenzwerte, alte Vitals-Daten und Rüstung.
Das native Setup prüft zusätzlich eine echte Easy-Save-Datei samt Entfernen
des geladenen Bonus. Der Zwei-Prozess-Test prüft Anlegen/Ablegen auf dem Client
und die vom Host übertragenen maximalen/aktuellen Herzen.

Manuell: alle drei Testausrüstungen vergleichen, verletzten Spieler ausrüsten,
auffüllen, ablegen und erneut anlegen; dann Save/Load und Gebietswechsel prüfen.
Vorlage für weitere Build-Boni ist dieser Ablauf, nicht eine zusätzliche
unabhängige Stat-Berechnung in jedem Kampf-/Zauberscript.
