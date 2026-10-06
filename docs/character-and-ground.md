# Animierte Figur und variierter Boden

Der Unity-Import und Editor-Umbau sind abgeschlossen: Das Player-Prefab enthält
128 Körperframes und 128 Kleidungsframes; zwölf Grasvarianten wurden erstellt
und 520 vorhandene Ground-Zellen in der Szene gespeichert. Kompilierung und
ein separater Test mit 900 Zellen einschließlich negativer Koordinaten sind
erfolgreich. Der visuelle Unity-Spieltest steht noch aus.

## Spielerfigur

Das Player-Prefab bekommt die vorhandene Figurenbasis und Waldläuferkleidung aus
Secrets. Körper und Kleidung bleiben getrennte SpriteRenderer und verwenden
denselben Frame. Vier Blickrichtungen mit Laufanimationen und jeweils einer
Ruhepose ersetzen den gelben Platzhalter. Fußpunkt, Stammverdeckung und
Laternenbedienung bleiben berücksichtigt.

`CharacterAppearance` stellt die Figur dar. Über `Equip(ClothingAppearance)` kann
später eine vom Inventar beziehungsweise Host bestätigte Rüstungsdarstellung
zugewiesen werden. Die `ClothingAppearance`-Assets enthalten eine stabile
Darstellungs-ID und die Frames. Der Inspector erlaubt bereits den Wechsel der
Darstellung; aktuell ist nur die Waldläuferkleidung eingerichtet.

Die Darstellung liest keine Tastatur und kein globales Inventar. Momentan nutzt
sie die Bewegung des zugehörigen Rigidbody2D. Über `SetPresentedMotion` können
später empfangene Bewegungsdaten eine entfernte Figur animieren. Die Übertragung,
Host-Prüfung und Speicherung sind noch nicht implementiert.

Der Frame-Aufbau wurde mit den ursprünglichen RPC-Laufclips geprüft: Down in
Zeile 0, Right in Zeile 1, Left in Zeile 2, Up in Zeile 3; Laufbilder 1 bis 6.
Die Körper- und Kleidungsbilder haben identische 32x32-Ausschnitte und Fußpunkte.

## Ground-Variation

Die Kartenform wird nicht zufällig erzeugt. Nur vorhandene Gras-Zellen im Ground
werden im Editor variiert. Die Auswahl verwendet den festen Seed `1729` und
zwölf Grastiles, angeordnet als drei zusammenhängende 2x2-Motive aus derselben
Grasfläche des Original-Sheets. Jeder 2x2-Block bekommt ein vollständiges Motiv;
die Teile eines Motivs werden weder gedreht noch zufällig durcheinandergeworfen.
Auch außerhalb dieser Blöcke bleibt die Grundfarbe aus demselben Grasbereich.

Wege, Terrain, leere Zellen und andere selbst gemalte Ground-Tiles bleiben
erhalten. Die Variation wird in der Szene gespeichert und bei Play nicht neu
gewürfelt. Sie kann somit später von allen Clients als dieselbe Karte geladen
werden. Die zwölf neuen Tiles lassen sich außerdem von Hand in der Palette malen.

Für eine Wiederholung im Editor: `SecretsReborn > World > Upgrade character and
ground`. Vorher Play beenden und offene Szenenänderungen speichern. Der Befehl
ändert das Player-Prefab und variiert bekannte Grastiles in der editierbaren Szene;
die Körper-/Kleidungsframes und Motive werden als normale Assets gespeichert.

## Spieltest

- In allen vier Richtungen laufen und anhalten: passende Lauf- und Ruhepose.
- Körper und Kleidung dürfen nicht gegeneinander versetzt sein.
- Kollision am Fußpunkt und Verdeckung vor/hinter Bäumen prüfen.
- Laterne, falsche Reihenfolge und vollständige Rätsellösung erneut testen.
- Vor und nach Play: Ground-Muster bleibt identisch und ist in der Scene editierbar.

Inventar, automatische Ausrüstungswechsel und Multiplayer bleiben spätere Schritte.
