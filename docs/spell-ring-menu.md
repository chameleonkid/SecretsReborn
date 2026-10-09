# Icon-Ring und Zauberbuch

Das aktive, editierbare uGUI-Prefab ist
`Assets/Resources/Magic/UI/SpellIconRingCanvas.prefab`.
Im Prefab-Modus sind InfoStrip, Controls, Ring, IconTemplate,
FixedSelectionMarker und SpellBook editierbar. `SpellRingView` hält deren
Inspector-Verweise. Radius und Drehgeschwindigkeit sind dort einstellbar.
Der Ring folgt der lokalen Figur; an Bildschirmrändern wird er in den
sichtbaren Bereich verschoben. Seine Icons werden zur Laufzeit aus dem Template
erstellt und positioniert. Die Positionen dieser Laufzeitkopien nicht einzeln
bearbeiten. Das alte `SpellRingCanvas.prefab` bleibt als vorheriger Entwurf erhalten.

**SecretsReborn → UI → Prepare spell icon ring** erstellt das neue Prefab
einmalig und prüft die Verweise. Ein vorhandenes Layout wird erhalten.
Die Quellgrafiken stammen aus Secrets; Herkunft steht in
[spell-icon-sources.json](spell-icon-sources.json). Import: Point, keine
Kompression, keine Mipmaps, Full Rect. Separate Sprite-Assets schneiden den
transparenten Rand ab; Original-PNGs bleiben unverändert.

## Aufbau nach der SoM-Referenz

1. Erlernte Elemente erscheinen als freistehende Icons um die Figur.
2. Nach Bestätigung erscheinen die erlernten Zauber dieses Elements im Ring.
3. Alle Icons drehen gemeinsam zur festen Auswahlmarkierung oben. Die Zahl ist
   nicht auf drei oder acht begrenzt; viele Einträge können optisch eng werden.
4. Nach der Zauberwahl verschwindet der Ring. Gültige Ziele werden direkt in der
   Welt markiert: orange für Gegner, cyan für Verbündete.
5. Ziel wählen und anschließend den Cast bestätigen.

Ein eigener Info-Streifen zeigt Auswahl, Mana, Rang, Kosten, Wirkzeit, Cooldown
und Wirkung. Es gibt keine Rahmen oder Textfelder um jedes Ringicon.
Das separate Zauberbuch zeigt sämtliche erlernten Zauber und Ränge, acht pro Seite.
Aktuell sind Feuerball und Heilung spielbar; die übrigen Elementicons bereiten
weitere Elemente vor, schalten aber keine zusätzlichen Zauber frei.

## Bedienung

| Tastatur | Controller | Funktion |
|---|---|---|
| M | View / Select | Öffnen / schließen |
| Pfeile | Stick / D-Pad | Ring drehen oder Ziel wechseln |
| Enter | A / Cross | Auswahl und danach Cast bestätigen |
| T | X / Square | Bei der Zielwahl Einzelziel / Alle umschalten |
| Escape | B / Circle | Eine Stufe zurück; am Anfang schließen |
| Tab | Y / Triangle | Separates Zauberbuch |
| — | LB / RB | Im Buch Seite wechseln, im Ring einen Eintrag wechseln |

Mausklicks wählen Ringicons und Buchzeilen. Bei der Zielwahl kann ein Ziel in
der Welt angeklickt und danach mit Enter bestätigt werden. „Alle“ ist ebenfalls
über Pfeile erreichbar. Auch bei nur einem Ziel ist eine Bestätigung erforderlich.
F9 am Host lernt/erhöht die Testzauber im Development-Build.

## Multiplayer und bisherige Grenzen

Nur die lokal gesteuerte Figur öffnet den Ring. Während Auswahl und Wirken ist
sie bewegungslos, auch gegen physikalisches Verschieben, aber verwundbar.
Die Welt und andere Spieler laufen weiter. Andere Aktionen sind gesperrt.
Tod, Save/Load, Weltwechsel und Gebietswechsel-Abfrage schließen die Auswahl.
Der Host validiert weiterhin Rang, Mana, Cooldown, Besitz, Reichweite und Ziele.
Protokoll 20 und Saveformat 17 bleiben unverändert.

Feuerball-Schaden entsteht derzeit noch am Ende der Wirkzeit. Nächster Schritt:
Cast-/Treffereffekte und zielverfolgende Feuerbälle mit Schaden erst beim Eintreffen.
Sie ignorieren Umgebungskollision. Zwei Projektile teilen das feste Budget;
der Anteil eines vorher gestorbenen Ziels verfällt. Heilung kann am Ende der
Wirkzeit mit einem Heileffekt eintreten.

Automatische Prüfung und Build-Datum: [Verifikation](verification-2026-10-09.md).
Physische Controller-Bedienung, Rotationstempo und Lesbarkeit im normalen Spiel
bleiben der anschließende manuelle Test.