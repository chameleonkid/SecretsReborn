# Icon-Ring und Zauberbuch

## Gemeinsamer Ring – Verbrauchsgegenstände und Zauber

Restore Point vor diesem Umbau: Commit `f741399`, Tag
`restore/pre-consumable-ring-2026-10-09` (lokal, nicht gepusht).

Oben/unten wechselt auf jeder Ringebene zwischen **Verbrauchsgegenständen** und
**Zaubern**; eine begonnene Zielwahl wird dabei verworfen. Links dreht den Ring
gegen den Uhrzeigersinn, rechts im Uhrzeigersinn – auch bei zwei Einträgen und
beim Übergang über den ersten/letzten Eintrag. Die feste Auswahlmarkierung bleibt oben.
Eine gehaltene vertikale Stickrichtung wechselt den Menütyp nur einmal, bis
der Stick losgelassen oder in eine andere Richtung bewegt wird. Horizontale
Navigation wiederholt nach kurzer Verzögerung.

Verbrauchsgegenstände werden pro Item-ID zusammengefasst und nach Typ/Wirkung
geordnet. Kleine, mittlere und große Tränke bleiben separate Einträge. Die Zahl
am Icon zählt alle Rucksackstapel dieses Gegenstands. A/Enter verwendet einen
Gegenstand; der Ring bleibt offen, damit die restliche Menge sichtbar wird.
Tränke haben keinen Cooldown und können unmittelbar
nacheinander benutzt werden. Volle HP/Mana verhindern weiterhin Verbrauch.
Die Figur bleibt währenddessen bewegungslos und verwundbar.
Trank-Icons bleiben immer farbig, auch bei vollen Werten.
Die Verwendungsprüfung ist unabhängig von ihrer Darstellung.

Trank-Schnellslots und ihre Tasten 1/2 bzw. LB/RB außerhalb des Inventars entfallen.
Das Inventar enthält weiterhin die tatsächlichen Gegenstände; Benutzen dort bleibt
möglich. Die früheren Bindungsdaten bleiben aus Save-Kompatibilitätsgründen erhalten,
bestimmen aber keine sichtbaren Schnellslots mehr.

Zauber mit zu wenig Mana oder laufendem Cooldown sind grau dargestellt und
können weiterhin zur Anzeige ihrer Kosten ausgewählt, aber nicht bestätigt werden.
Der Host prüft jeden Verbrauch anhand der Item-ID und seines eigenen Inventars;
ein veralteter Taschenindex kann daher keinen anderen Trank verbrauchen.
**Netzwerkprotokoll 21**, Saveformat weiterhin 17. Auf allen PCs denselben Build nutzen.

Anti-Gift ist eine vorgesehene Erweiterung, sobald Vergiftung als Zustand existiert.
Aktuell sind HP-/Mana-Tränke die implementierten Verbrauchsgegenstände.
Weitere Elemente und genannte Zauber wie Feuerlanze bleiben geplante Inhalte.

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
5. Das Ziel oder „Alle“ mit A/Enter bestätigen: Der Cast beginnt unmittelbar.
   Es gibt keine zusätzliche Bestätigungsebene. Ablauf: **Element → Zauber → Ziel**.

Ein eigener Info-Streifen zeigt Auswahl, Rang, Zauberkosten, Wirkzeit, Zauber-Cooldown
und Wirkung. Es gibt keine Rahmen oder Textfelder um jedes Ringicon.
Der aktuelle Mana-Vorrat steht ausschließlich im normalen HUD, nicht noch
einmal als MP im Fenster; Zauberkosten werden weiterhin angezeigt. Der
Info-Streifen steht unter dem reservierten HP-/Mana-Bereich, einschließlich
zweiter Herzreihe, und hat einen halbtransparenten Hintergrund. In
`SpellRingView` sind `Info Background Alpha` und `Book Background Alpha`
anpassbar. Die Texte bleiben deckend und lesbar.
Das separate Zauberbuch zeigt sämtliche erlernten Zauber und Ränge, acht pro Seite.
Aktuell sind Feuerball und Heilung spielbar; die übrigen Elementicons bereiten
weitere Elemente vor, schalten aber keine zusätzlichen Zauber frei.

## Bedienung

| Tastatur | Controller | Funktion |
|---|---|---|
| M | View / Select | Öffnen / schließen |
| Links/rechts | Stick / D-Pad links/rechts | Ring drehen oder Ziel wechseln |
| Oben/unten | Stick / D-Pad oben/unten | Menütyp wechseln |
| Enter | A / Cross | Element/Zauber wählen; auf der Zielebene direkt wirken |
| T | X / Square | Bei der Zielwahl Einzelziel / Alle umschalten |
| Escape | B / Circle | Eine Stufe zurück; am Anfang schließen |
| Tab | Y / Triangle | Separates Zauberbuch |
| — | LB / RB | Im Buch Seite wechseln, im Ring einen Eintrag wechseln |

Mausklicks wählen Ringicons und Buchzeilen. Bei der Zielwahl startet ein Klick
auf ein gültiges Ziel in der Welt direkt den Cast. „Alle“ ist ebenfalls
über Pfeile erreichbar. Auch bei nur einem Ziel wird dieses mit A/Enter bestätigt;
diese Eingabe startet bereits den Cast.
F9 am Host lernt/erhöht die Testzauber im Development-Build.

## Multiplayer und bisherige Grenzen

Nur die lokal gesteuerte Figur öffnet den Ring. Während Auswahl und Wirken ist
sie bewegungslos, auch gegen physikalisches Verschieben, aber verwundbar.
Die Welt und andere Spieler laufen weiter. Andere Aktionen sind gesperrt.
Tod, Save/Load, Weltwechsel und Gebietswechsel-Abfrage schließen die Auswahl.
Der Host validiert weiterhin Rang, Mana, Cooldown, Besitz, Reichweite und Ziele.
Protokoll 21 und Saveformat 17 gelten für diesen Stand.

Feuerball-Schaden entsteht derzeit noch am Ende der Wirkzeit. Nächster Schritt:
Cast-/Treffereffekte und zielverfolgende Feuerbälle mit Schaden erst beim Eintreffen.
Sie ignorieren Umgebungskollision. Zwei Projektile teilen das feste Budget;
der Anteil eines vorher gestorbenen Ziels verfällt. Heilung kann am Ende der
Wirkzeit mit einem Heileffekt eintreten.

Automatische Prüfung und Build-Datum: [Verifikation](verification-2026-10-09.md).
Physische Controller-Bedienung, Rotationstempo und Lesbarkeit im normalen Spiel
bleiben der anschließende manuelle Test.
