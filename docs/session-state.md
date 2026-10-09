# Sitzungszustand der Host-Welt

`GameSession` erzeugt beim Beginn eines Play-Durchlaufs ein dauerhaftes
`Host session`-Objekt mit `DontDestroyOnLoad`. `WorldSessionState` hält den
Zustand außerhalb der Szenen. Ein Szenenwechsel oder erneutes Laden derselben
Szene erzeugt deshalb kein neues Inventar und setzt das Rätsel nicht zurück.

## Gespeichert im Arbeitsspeicher

- Inventar mit 40 Taschenplätzen und 15 Ausrüstungsplätzen inklusive Lampe pro Character ID.
- Weltgebundene Charakterprofile mit Namen und Körper-/Haar-/Augenoptik.
- HP/Mana, Position und Szene pro Charakter; Spielzeit und gespeichertes Gebiet.
- Aufgesammelte Welt-Item-IDs innerhalb der Host-Welt.
- Runenfortschritt und Abschluss pro Puzzle ID.
- Besiegte Gegner, geöffnete Truhen und gemeinsame Fund-Freischaltungen.

Die aktive Gruppe wird getrennt von den gespeicherten Figuren verwaltet.
Abwesende Charaktere bleiben im Weltzustand und werden beim nächsten Speichern
mit erfasst. Beim Disconnect entsteht kein automatischer Festplatten-Spielstand.

`CharacterInventory` bindet sich in Awake über seine ID an diesen Zustand und
stellt in Start die Ausrüstungsgrafik wieder her. `WorldItem` blendet bereits
aufgesammelte Items beim Aktivieren aus. `SanctuaryPuzzle` übernimmt beim Start
die aktivierten Kreise und stellt bei Abschluss Wasser und offenen Durchgang her.
Spring und Gate sind Darstellung des Rätselzustands, keine zusätzliche Datenquelle.

Item-Definitionen und Grafik-SOs bleiben gemeinsame Definitionen. Keine
veränderlichen Inventare werden in SO-Assets geschrieben. Der Sitzungszustand
enthält keine Szenenreferenzen, Sprites oder Eingabegeräte.

## Lebensdauer und Grenzen

Neue Welten und ihre Figuren entstehen über Hauptmenü/Lobby mit eindeutigen IDs.
Der direkte Prototyp-Szenenstart verwendet weiterhin `prototype-world` und
`solo-player`. Für unterschiedliche Charaktere eindeutige IDs zuweisen. Mehrere Spieler dürfen
nicht unbeabsichtigt dieselbe Character ID verwenden. Welt-Item- und Puzzle-IDs
müssen innerhalb der Welt eindeutig sein und dürfen bei einem späteren Umbau
nicht einfach neu vergeben werden.

Der Einstieg wird auch bei deaktiviertem Domain Reload zurückgesetzt.
Neue Spiele starten mit neuer Welt; gespeicherte Stände werden über Hauptmenü
oder Speicherbuch geladen, siehe [savegame.md](savegame.md) und
[gemeinsames Buch](shared-save-book.md). In Multiplayer entscheidet der Host über
den autoritativen Zustand; Clients erhalten Snapshots. Script-
Domain-Reload während Play ist noch kein persistenter Savegame-Ersatz.
Szene, Spielerposition, HP/Mana und Spielzeit sind im versionierten Savegame
enthalten. Gemeinsame Gebietswechsel nutzen definierte Eintrittspunkte, Load und
laufender Beitritt setzen Gäste nahe den Host. Der eingeschaltete Laternenzustand
wird live synchronisiert, ist aber keine persistente Itemdefinition. Individuelle
XP/Level-Regeln und Tag/Nacht-Weltzeit sind weitere geplante Mechaniken.

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

Easy Save 3 speichert das versionierte Datenmodell `SaveGameData` (Schema 13),
nicht Szenenobjekte oder die private Runtime-Struktur. Charakterdefinitionen und
Weltzustand bleiben von Unity-Szenenlebensdauer und Verbindungs-IDs getrennt.

Aktuelle Übergabe und offene Prüfungen: [8. Oktober](handoff-2026-10-08.md).
