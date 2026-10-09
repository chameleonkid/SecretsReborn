# Erster Host-Zauberablauf

Erster Host-Ablauf: SpellCaster verarbeitet Feuerball und Heal
aus dem Resources-Katalog `Assets/Resources/Magic/Spells`.
Native Einrichtung: **SecretsReborn → Magic → Prepare spell foundation**;
vorhandene Definitionen werden unter Erhalt ihrer GUID dorthin verschoben.
SpellCaster wird an Spieler angelegt, auch an neu gespawnte Netzwerkfiguren.

## Regeln dieser ersten Ausführung

- Der Host leitet Rang, Kosten, Budget, Reichweite und Zeiten aus Definition und
  Lernstatus ab. Ein Client sendet nur Zauber-ID, Ziel-ID und Einzelziel/alle.
- Gegner/lebende Verbündete müssen aktiv, im selben Gebiet und in Reichweite sein.
  Es gibt keine Sichtlinienprüfung oder Umgebungskollision.
- Mana und Ziele werden vor Beginn geprüft. Ohne gültige Ziele/ausreichendes
  Mana wird nichts verbraucht. Beim Start wird Mana einmal abgezogen.
- Während der Wirkzeit steht die Figur, auch physikalisches Schieben ist gesperrt.
  Sie erhält keine Immunität. Nahkampf und parallele Aktionen werden gesperrt.
- Cooldown beginnt nach der Wirkzeit; normale Treffer brechen vorerst nicht ab.
  Tod, Deaktivieren, Save/Load, Gebiet/Host-Weltwechsel oder Escape brechen ab.
  Nach akzeptiertem Start gibt es keine Mana-Rückerstattung; Cooldown bleibt bei
  manuellem Abbruch bestehen. Laden einer neuen Welt verwirft temporäre Cast-Timer.
- Die Zielliste wird beim Start fixiert und nach Ziel-ID sortiert. Budget wird
  ganzzahlig geteilt; die ersten IDs bekommen die Restpunkte. So bleiben Summe
  und Unterschied von höchstens einer Einheit erhalten. Kein garantierter
  Mindestanteil vervielfacht ein zu kleines Budget. Fireball-Budget betrifft
  Gegner-HP, Heal-Budget halbe Herzen.
- Ein verlorenes, gestorbenes oder außer Reichweite geratenes Ziel bekommt seinen
  Anteil nicht; er verfällt. Kein neues Ziel ersetzt es. Overheal verfällt ebenfalls.
- Caststatus und verbleibende Wirkzeit werden in Host-Snapshots übertragen.
  Lernstatus bleibt im Saveformat 17, Cast-Timer/Effekte werden nicht gespeichert.
  Netzwerkprotokoll 20 seit der Ringmenü-Anbindung: auf allen Test-PCs den
  neuen vollständigen Build verwenden.

## Vorläufiger Tastatur-Testzugriff

Dieser Entwicklungszugriff bleibt zusätzlich zum regulären
[Ringmenü mit Controller-Bedienung](spell-ring-menu.md) verfügbar:

| Taste | Aktion |
|---|---|
| F9 am Host (Development-Build/Editor) | Alle aktiven Figuren lernen Fireball/Heal bzw. steigen einen Rang auf, bis Rang 3 |
| F10 | Ziel im zuletzt gewählten Zielbereich wechseln |
| F7 | Feuerball auf das gewählte Gegnerziel |
| F8 | Heal auf das gewählte lebende Verbündetenziel |
| Shift + F7/F8 | Alle gültigen Gegner/Verbündeten in Reichweite |
| Escape während des Wirkens | Abbrechen |

Rot/cyan markiert das lokale Testziel. Die erste Zielwahl ist die erste gültige
Ziel-ID; nach einem Wechsel von Angriff zu Heilung gilt deren Zielliste.
Noch keine Feuerball-Spriteanimation, Flugphase oder Rang-3-Projektilvisualisierung.
Schaden/Heilung werden am Ende der Wirkzeit vom Host angewendet. Vorhandene
Secrets-Effekte werden später getrennt von alter Projektilphysik angebunden.
F9 ist ausdrücklich ein Test-Lernzugriff; er verändert den Lernstatus der
aktuellen Welt. Eine eigene Testwelt verwenden, wenn das nicht gespeichert werden soll.

## Prüfung und nächster Ausbau

Abschließender nativer Windows-Build vom 9. Oktober 2026: Spielassembly
**19:24:56 Uhr**, 0 Fehler/1 Warnung. Beide Testprozesse bestehen; keine
Exceptions oder Netzwerkfehler in ihren Logs. Die Tests sind beendet.

SpellCastChecks prüft die Budgetverteilung einschließlich kleiner Budgets und
ungültiger Werte. Der Zwei-Prozess-Test ergänzt Feuerball durch eine Testwand,
einmaligen Manaverbrauch trotz zweiter Anfrage während des Wirkens, Bewegungssperre
und Heal auf zwei Figuren mit geteilter Wirkung.
Zusätzlich prüft der Host nach dem Wirken die Cooldown-Sperre: Eine erneute
Heilung wird abgelehnt und verbraucht kein weiteres Mana.

Das editierbare Canvas-Ringmenü ist anschließend ergänzt; Zielvorschau und
Kosten/Cooldown werden angezeigt. Die Vorschau benutzt dieselbe Sortierung
und Rundung. Nächster Ausbau: Effekte und Projektile mit Schaden beim Eintreffen,
danach reguläre Zauberbuch-Pickups anbinden.
