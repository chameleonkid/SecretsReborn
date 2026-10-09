# Menüs und Charaktergestaltung

Die Lobby zeigt vier nebeneinanderliegende Charakterkarten und eine gemeinsame
Aktionsleiste. Die Referenz bestimmt die Anordnung; Hintergrund, Rahmen und
Spielfiguren gehören zum eigenen Artstil. Es werden keine erfundenen Statistikbalken
angezeigt. Belegte, verfügbare und leere Plätze bleiben unterscheidbar.

Der neue Waldhintergrund wurde für SecretsReborn generiert. Er liegt unter
`Assets/Resources/MenuUI/ForestMenuBackdrop.png`; Point-Filterung, keine Mipmaps
und keine Texturkompression erhalten die Pixelstruktur. Die bestehenden
Inventarrahmen werden segmentiert beziehungsweise gekachelt gezeichnet.

Jeder neue Singleplayer- oder Multiplayercharakter erhält einen Creator mit Namen,
zwei Körpervarianten, je acht Hautvarianten, je 13 Frisuren, sechs Haarfarben und
sieben originalen Augenfarben aus Retro Pixel Characters. Die Vorschau zeigt die vorhandenen
Animationslayer frontal; die Lobby berücksichtigt gespeicherte Rüstung, Kopfbedeckung
und Schuhe. Die Augenfarbe verändert nur farbige Irispixel, nicht Pupillen und
Lichtreflexe. Neue Figuren nutzen die originalen Augen-Spritesheets; die bisherige
Iris-Umfärbung bleibt für bereits gespeicherte Figuren erhalten. Im Spiel steht der gespeicherte Charaktername über der Figur.

Alle gewählten Körper-, Haar- und Augenlayer enthalten 128 passende Frames.
Outfits, Helme und Kronen stehen ausschließlich für Equipment zur Verfügung;
der Creator verändert weder Ausrüstung noch Werte. Elfenvarianten sind zunächst
kosmetisch und führen keine Rassenfähigkeiten ein.

Charakterfarben sind Daten des Host-Weltprofils. Clients schicken die Auswahl an
den Host; dieser validiert die Indizes, erlaubten Asset-IDs und passende Körperfamilien.
Save-Schema 13 übernimmt ältere Spielstände mit unveränderten ursprünglichen
Figurenfarben und Layern. Mit der Reconnect-Erweiterung verlangt Netzwerkprotokoll
14 denselben Build auf Host und Clients (explizite Auswahl bei jedem Rejoin).

Prüfung: Domainchecks für Profil-Roundtrip, alte Spielstände, ungültige Farbindizes
und Kopienisolation; Lobby-Zwei-Prozess-Test für Charaktererstellung und
übertragene Darstellung. Ein automatischer Screenshot-Versuch im versteckten
Testfenster lieferte keine Bilder; die optische Endkontrolle erfolgt in Unity
oder im interaktiven Build.

Nächste manuelle Prüfung: neue Figur erstellen, Lobby verlassen und erneut öffnen,
mit zwei Spielern starten und nach Save/Load Namen sowie Farben vergleichen.
Vier-Spieler-Test und Rejoin bleiben die nächsten Schritte an den Mechaniken.
