# Charakterwerte und Equipment

Die gemeinsame Stat-Berechnung summiert Armor, Bonus-Hearts und Bonus-Mana
aller angelegten Items. Gegenstände im Rucksack geben keine Boni.
Zusätzlich werden Critchance-Boni aller angelegten Items und die Critchance
des Hauptwaffenprofils addiert (Prozentpunkte, auf 100 % begrenzt).
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

## Kritische Treffer und Schadenszahlen

`WeaponDefinition` bietet **Crit Chance Percent** (Standard 5 %).
`ItemDefinition` bietet **Bonus Crit Chance Percent** (Standard 0 %) für Waffen
und jede andere Ausrüstung. Beispiel: 5 % Waffenprofil + 3 % Waffenitem + 7 %
Rüstung ergeben 15 % Critchance. Nur angelegte Gegenstände zählen; das Profil
einer Hauptwaffe zählt einmal. Die gesamte Chance steht im Stat-Panel.

Crits verursachen zunächst den doppelten Rohschaden. Danach greift gegebenenfalls
die Rüstungsformel. Nahkampf würfelt auf dem Host pro getroffenem Gegner;
Feuerball übernimmt die Critchance beim Caststart, würfelt pro Ziel und teilt
den ggf. verdoppelten Zielanteil auf seine Projektile. Beide Bälle desselben
Ziels sind damit gemeinsam kritisch oder normal. Heal crittet nicht.
Kontaktgegner haben bislang keine eigene Critchance.

`GameSession.ApplyEnemyDamage` erhält bereits aufgelösten Schaden und Critstatus;
`ApplyDamage` für Spieler erhält Rohschaden und wendet bei gesetztem Critstatus
Multiplikator und Rüstung an. Das sind die gemeinsamen Host-Einstiege auch für
spätere Effekte. Ein späterer DoT kann normalen Schaden ohne neue Critwürfe anwenden.

Jeder angenommene Treffer zeigt eine Zahl am Trefferort: gelb mit Schriftgröße
26, kritisch goldorange mit Schriftgröße 40. Angezeigt wird der berechnete Schaden
nach Rüstung; er kann die restlichen HP übersteigen. Spieler-HP verwenden weiterhin
halbe Herzen als Schadenseinheit. Zahlen steigen auf und verschwinden nach einer
Sekunde, bleiben beim Tod sichtbar und werden beim Szenenwechsel entfernt.
Sie sind reine Präsentation, keine gespeicherten Effekte.

Koop-Protokoll 26 enthält das zuverlässige Host-Ereignis `sr.damage` mit Sequenz,
Gebiet, Welt und Szene. Clients prüfen diese Werte und zeigen die Zahl genau einmal;
sie würfeln keine Crits und ändern dadurch keine HP. Saveformat bleibt 18.
Im Editor und Development-Build schaltet **F5** oder der Button unten links
**100 % Crits** für die eigene Figur ein und aus. Der Host entscheidet im Koop;
der Client erhält den bestätigten Schalterzustand. Nahkampf und Fireball nutzen
den Testwert, Heal bleibt ohne Crit. Ausrüstungswerte und Saves werden dafür nicht
verändert. Das Inventar zeigt bei aktivem Schalter `100% · CRIT ×2 (TEST)`.

Eine stationäre **Trainingspuppe mit 1.000.000 HP** steht im Waldheiligtum rechts
unterhalb des Startpunkts bei den Testtruhen. Sie ist ein reguläres Zauber-/Nahkampfziel,
greift nicht an und gibt keine Beute. Verbleibende HP erscheinen über der Puppe,
auch auf Koop-Clients. Neue Szeneninstanzen sowie ein Session-Refresh setzen die
HP zurück; ihr Tod wird nicht gespeichert. Prefab:
`Assets/World/Combat/Prefabs/TrainingDummy.prefab`. Der idempotente Editorzugriff
**SecretsReborn → Combat → Prepare training dummy** legt Prefab und Szeneinstanz an,
wenn sie fehlen, und verlangt zuvor gespeicherte offene Szenen.

## Rüstungsberechnung

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
