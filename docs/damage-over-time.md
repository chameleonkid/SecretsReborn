# Damage over Time

Burn, Poison und Bleed nutzen denselben technischen Ablauf, bleiben aber getrennte
Effekte. `DotState` hält je Typ einen eigenen Tick-Timer und ein eigenes Ablaufdatum.
Die drei Typen können gleichzeitig auf Gegnern und Spielfiguren aktiv sein.

## Anwendung und Regeln

`DotApplication` definiert Typ, Auslösechance in Prozent, Dauer, Tick-Abstand und
Schaden pro Tick. `GameSession.ApplyDot(target, definition)` ist der vertrauenswürdige
Host-Einstieg für Fähigkeiten. `ApplyOnHitDots` wendet die Effektliste einer Quelle an.
Waffenprofile und die einzelnen Ränge eines Zauberassets haben dafür im Inspector
die Liste **On Hit Dots**. Schaden, Chance, Dauer und Tick-Abstand sind pro Rang frei
einstellbar; es gibt keinen fest eingebauten Schaden pro Sekunde.
Waffenbeschreibungen im Inventar und Shared Stash sowie Zauberbeschreibungen
im Ringmenü/Zauberbuch nennen jeden gültigen DoT mit Schaden, Tick-Abstand,
Dauer und Chance, etwa `Burn: 3 Schaden/s für 3 s · 100 % Chance`.
Zauber verwenden dabei die Werte ihres aktuell erlernten Rangs. Der Ring-Infobereich
wächst mit dem Text; Inventar-Details passen die Schrift an ihre vorhandene Fläche an.
Die Liste wird nur nach einem angenommenen direkten Treffer ausgewertet; tödliche
Treffer hinterlassen keinen DoT auf einem bereits toten Ziel.

- Ein neuer Effekt tickt erstmals nach seinem Tick-Abstand, nicht sofort.
- Derselbe Typ erzeugt auf einem Ziel keine zusätzlichen Stapel, auch nicht durch
  zwei Projektile oder mehrere Angreifer. Ein erneuter erfolgreicher Proc verlängert
  das Ablaufdatum mindestens bis `jetzt + Dauer`; der nächste Tick bleibt bestehen.
  Die zuletzt erfolgreiche Quelle liefert Schaden und Abstand der folgenden Ticks.
- Ein Tick genau am Ablaufdatum zählt noch. Langsame Frames holen fällige Ticks nach.
- DoTs würfeln keine Crits und lösen keine weiteren On-Hit-DoTs aus. F5 beeinflusst
  nur direkte Treffer. Tick-Schaden verwendet die üblichen Schadenseinheiten;
  bei Spielern ist 1 Schaden ein halbes Herz. Spielerrüstung gilt wie bisher.
- DoT-Ticks verursachen keinen zusätzlichen Gegner-Knockback oder Hitstun.
- Ein bereits angewendeter DoT bleibt bestehen, wenn sein Angreifer stirbt oder
  die Verbindung verliert. Er gehört dem Ziel, nicht dem Projektil/Zaubernden.
- Tod, Deaktivierung, Session-Refresh, Laden, Welt- und Gebietswechsel entfernen
  die Laufzeit-Effekte. Sie werden nicht gespeichert; Saveformat bleibt **18**.
  Savebook, Gebietsabstimmungen und pausierte Spielzeit halten die Timer an.
  Spieler-DoTs pausieren außerdem während einer gesperrten Belohnungspräsentation.

Dauer ist auf 120 Sekunden begrenzt; Tick-Abstand muss mindestens 0,1 Sekunden
betragen und darf die Dauer nicht überschreiten. Ungültige Werte werden verworfen.

## Fireball und Optik

Das Fireball-Asset löst bei jedem überlebten Projektiltreffer zu **100 % Burn** aus:
**3 Sekunden**, **1 Sekunde Tick-Abstand**. Der Schaden pro Tick beträgt auf
**Rang 1 / 2 / 3 jeweils 1 / 2 / 3**, also insgesamt 3 / 6 / 9 Schaden für einen
vollständigen Burn. Mehrfachprojektile
frischen den Effekt auf und behalten ihr unverändertes direktes Schadensbudget.
Diese Werte sind ein erster konfigurierbarer Teststand.

`DotEffectsView` zeigt laufende Effekte direkt am Ziel und folgt dessen Position:

| Typ | Darstellung |
| --- | --- |
| Burn | Orange-gelbe, flackernde Flammenlinien steigen auf |
| Poison | Grüne, aufsteigende Blasenringe |
| Bleed | Rote, fallende Blutstriche |

Form und Bewegung unterscheiden die Typen zusätzlich zur Farbe. Die kleinen
LineRenderer-Effekte sind im Projekt erzeugte Geometrie und brauchen keine neuen
Grafikpakete. Schadenszahlen bleiben normale gelbe Zahlen; Crits bleiben orange.
Poison und Bleed sind technisch und optisch vorhanden, aber derzeit noch keiner
regulären Waffe oder Gegnerfähigkeit als Standard zugewiesen.

## Netzwerk und Prüfung

**Protokoll 26** überträgt die aktiven Typen als Bitmaske mit Spieler-/Gegnerposes.
Nur der Host würfelt Anwendungen und berechnet Ticks. Clients zeigen die Zustände
und die vorhandenen zuverlässigen `sr.damage`-Ereignisse an; sie erzeugen keinen
eigenen Tick-Schaden. Endende Effekte verschwinden mit dem nächsten Snapshot.
Alle PCs benötigen denselben vollständigen Build.

`tests/DotChecks.cs` prüft Koexistenz, unabhängiges Ablaufen, Auffrischen ohne
Tickverschiebung, Chancen, Wertkopien, Tick am Ablaufdatum, Aufholen, Grenzwerte
und Abbruch nach Tod/Clear. Der opt-in `EconomyIntegrationDriver` prüft zusätzlich
Fireball-Burn an der Trainingspuppe, drei gleichzeitige Typen, Spieler-Schaden,
DoT-Tod, Session-Cleanup und Client-Replikation in echten Spielprozessen.

Manuell: Im Development-Build eine Testwelt öffnen, am Host mit F9 Fireball lernen,
die Trainingspuppe im Ringmenü auswählen und treffen. Burn-Optik und drei gelbe
Tickzahlen beobachten; F5 darf den Tick-Schaden nicht verdoppeln. Erneut treffen,
während Burn läuft: Effekt verlängert sich, ohne einen zweiten Burn anzulegen.
