# Waldheiligtum: erster Bewegungsprototyp

Für die weitere Kartengestaltung die neue Szene `Waldheiligtum-Editable.unity`
verwenden; siehe [World, Tilemaps und Prefabs bearbeiten](world-authoring.md).
Die unten beschriebene Laufzeiterzeugung gilt für die bisherige Prototypszene.

Die Freigabe für Code und Mechaniken wurde am 6. Oktober 2026 erteilt.

## Start in Unity

1. Falls Unity noch importiert, den Abschluss abwarten; bei Bedarf das Editorfenster fokussieren.
2. `Assets/Scenes/Waldheiligtum.unity` öffnen. Ungespeicherte Änderungen an anderen Szenen vorher sichern.
3. Play drücken und in die Game-Ansicht klicken.
4. Mit WASD, Pfeiltasten oder dem linken Gamepad-Stick beziehungsweise Steuerkreuz bewegen.

Die gelbe Figur beginnt südlich der versiegten Quelle. Kamera und Figur werden
beim Start eingerichtet; Gelände und Hindernisse bestehen aus farbigen Platzhaltern.
Die erzeugten Objekte sind nur während Play sichtbar und werden nicht in der Szene gespeichert.
Die SampleScene und ihre Build-Einstellung bleiben erhalten; für diesen Test die neue Szene direkt öffnen.

## Manuelle Prüfung

- Alle vier Richtungen und diagonale Bewegung prüfen; diagonal darf die Figur nicht schneller laufen.
- Gegen Quelle, Bäume und Gebietsgrenzen laufen; die Figur darf sie nicht durchqueren.
- Hindernisse entlanglaufen und die Kamera beobachten.
- Play beenden und erneut starten: Figur und Kamera beginnen wieder am Startpunkt.
- Bei verbundenem Gamepad Stick und Steuerkreuz prüfen.

Bewegung, Kamera, Hinderniskollision und gleichmäßige diagonale Geschwindigkeit wurden im Unity-Spieltest vom Nutzer bestätigt.

## Laterne und verborgene Zeichen

- Mit `L` oder der oberen Gamepad-Taste die Laterne ein- und ausschalten.
- Die Laterne beginnt ausgeschaltet. Ein heller Punkt neben der Figur und die Anzeige oben links zeigen ihren Zustand.
- Nur bei eingeschalteter Laterne erscheinen Zeichen innerhalb von drei Welteinheiten.
- Test: Am Start einschalten; nördlich der Figur erscheint ein blaues Pfeilzeichen. Ausschalten lässt es verschwinden.
- Zu weiteren Zeichen gehen; beim Verlassen des Radius müssen sie wieder verschwinden.
- Play neu starten: Die Laterne ist erneut aus, alle Zeichen sind verborgen.

Die Laterne und verborgenen Zeichen wurden im Unity-Spieltest vom Nutzer bestätigt. Die Laterne enthüllt Zeichen; eine Beleuchtung der Umgebung ist noch nicht umgesetzt.

## Vier Runenkreise

Der vollständige Ablauf mit Fehler-Reset, Wiederherstellung der Quelle und Öffnung des Wegs wurde vom Nutzer im Spieltest bestätigt.

Die vereinbarte Rätselregel: Vier explizite Orte in richtiger Reihenfolge betreten. Laternenzeichen führen zu den Kreisen. Eine falsche Aktivierung setzt die gesamte Folge zurück; ein Kreis muss verlassen und erneut betreten werden, bevor er nochmals aktiviert wird. Die Laterne wird zum Finden der Hinweise benötigt, nicht zum Aktivieren eines Kreises.

Vorläufige Kreispositionen (Platzhalter): `(0, -3)`, `(4, 0)`, `(3, 6)`, `(-2, 7)`. Entscheidend ist die Position der Figurenmitte innerhalb des Runenrings. Die Runen selbst sind keine Hindernisse.

- Laterne einschalten und den Pfeilen vom Start zum ersten Runenkreis folgen.
- Runenkreise sind wie die Pfeile nur bei eingeschalteter Laterne innerhalb von drei Welteinheiten sichtbar. Auch aktivierte grüne Kreise verschwinden beim Ausschalten oder Entfernen; ihr Fortschritt bleibt erhalten.
- Richtigen Kreis betreten: Runen werden grün, Fortschritt steigt um eins.
- Im Kreis stehen bleiben: keine weitere Aktivierung.
- Falschen Kreis betreten: alle Kreise verlieren ihre Aktivierung; Meldung zeigt den Neustart an.
- Nach einem Fehler den ersten Kreis verlassen und erneut betreten, dann alle vier in Reihenfolge ablaufen.
- Nach dem vierten richtigen Kreis wird das Quellbecken blau und die nördliche Sperre verschwindet. Der kurze neue Weg ist begehbar.
- Nach Lösung beliebige Kreise betreten: Quelle und Weg bleiben aktiviert.
- Play neu starten: Fortschritt ist zurückgesetzt, Quelle trocken und Durchgang geschlossen.

Der neue Weg endet vorerst an einer festen Prototyp-Grenze. Ein Gebietswechsel oder eine Rätselhöhle sind noch nicht umgesetzt.

## Erste Grafiken aus Secrets

Gras, Steinflächen, Bäume und Quellwasser verwenden die ausgewählten Originalgrafiken aus Secrets. Spielerfigur, Laterne, Runen und Wege bleiben Platzhalter. Nach dem Import Play neu starten und prüfen:

- Gras und Steine erscheinen texturiert statt einfarbig.
- Bäume zeigen Kronen und Stämme; an der Krone vorbeilaufen, der Stamm bleibt fest.
- Vor und hinter einen Baum laufen: Die Figur wird abhängig von ihrer Position verdeckt.
- Das Rätsel vollständig lösen: Quellwasser erscheint und der Weg öffnet sich weiterhin.

Die Quellwassertextur ist zunächst statisch. Asset-Herkunft und Importanpassungen sind in [assets.md](assets.md) dokumentiert.

Der Prototyp enthält noch keine Inventare, XP, Speicherung oder Koop.
Diese Systeme folgen in weiteren Schritten. Die Eingabesteuerung ist zunächst nur für eine lokale Figur ausgelegt.
