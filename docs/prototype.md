# Waldheiligtum: erster Bewegungsprototyp

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

Der Prototyp enthält noch keine Laterne, Rätsel, Inventare, XP, Speicherung oder Koop.
Diese Systeme folgen in weiteren Schritten. Die Eingabesteuerung ist zunächst nur für eine lokale Figur ausgelegt.
