# Sitzungszustand der Host-Welt

`GameSession` erzeugt beim Beginn eines Play-Durchlaufs ein dauerhaftes
`Host session`-Objekt mit `DontDestroyOnLoad`. `WorldSessionState` hält den
Zustand außerhalb der Szenen. Ein Szenenwechsel oder erneutes Laden derselben
Szene erzeugt deshalb kein neues Inventar und setzt das Rätsel nicht zurück.

## Gespeichert im Arbeitsspeicher

- Inventar mit 40 Taschenplätzen und 14 Ausrüstungsplätzen pro Character ID.
- Aufgesammelte Welt-Item-IDs innerhalb der Host-Welt.
- Runenfortschritt und Abschluss pro Puzzle ID.

`CharacterInventory` bindet sich in Awake über seine ID an diesen Zustand und
stellt in Start die Ausrüstungsgrafik wieder her. `WorldItem` blendet bereits
aufgesammelte Items beim Aktivieren aus. `SanctuaryPuzzle` übernimmt beim Start
die aktivierten Kreise und stellt bei Abschluss Wasser und offenen Durchgang her.
Spring und Gate sind Darstellung des Rätselzustands, keine zusätzliche Datenquelle.

Item-Definitionen und Grafik-SOs bleiben gemeinsame Definitionen. Keine
veränderlichen Inventare werden in SO-Assets geschrieben. Der Sitzungszustand
enthält keine Szenenreferenzen, Sprites oder Eingabegeräte.

## Lebensdauer und Grenzen

Die aktuelle Solo-Welt heißt `prototype-world`, der Spieler `solo-player`.
Für unterschiedliche Charaktere eindeutige IDs zuweisen. Mehrere Spieler dürfen
nicht unbeabsichtigt dieselbe Character ID verwenden. Welt-Item- und Puzzle-IDs
müssen innerhalb der Welt eindeutig sein und dürfen bei einem späteren Umbau
nicht einfach neu vergeben werden.

Der Einstieg wird auch bei deaktiviertem Domain Reload zurückgesetzt.
Neue Sitzungen starten leer; gespeicherte Stände werden ausschließlich am Buch
geladen, siehe [savegame.md](savegame.md). Neue Welt, Welt-Auswahl und echte
Netzwerksynchronisierung folgen anschließend. Script-
Domain-Reload während Play ist noch kein persistenter Savegame-Ersatz.
Szene und Spielerposition sind inzwischen im Savegame enthalten. Laternenzustand,
HP/Mana, XP und Zeit fehlen weiterhin. Gezielte Eintrittspunkte beim regulären
Gebietswechsel werden später ergänzt.

## Prüfung

Standalone-Prüfungen in `tests/WorldSessionChecks.cs` prüfen Rebinding über IDs,
getrennte Welten/Charaktere, Ausrüstung, doppelte und reentrante Aufhebeversuche,
fehlgeschlagenen Zugang, Teil-/Abschlusszustand des Rätsels und neue Sitzungen.
Zusätzlich bestehende Inventarprüfungen und Runtime-/Editor-Kompilierung ausführen.

Für den tatsächlichen Szenentest in Play die Rüstung aufheben und anlegen,
Runen aktivieren und die Szene innerhalb desselben Play-Durchlaufs neu laden.
Hierfür `SecretsReborn > Session > Reload active scene in Play` verwenden;
der Editor-Befehl lädt die gespeicherte Szene, ohne die Play-Sitzung zu beenden.
Rüstung/Ausrüstung müssen erhalten, Pickup entfernt und Kreise aktiviert bleiben.
Nach Abschluss müssen Quelle und Tor wieder richtig dargestellt werden.

Easy Save wird später über ein versioniertes Datenmodell angebunden. Die
private Runtime-Struktur nicht ungeprüft als vollständiges Savegame serialisieren.
