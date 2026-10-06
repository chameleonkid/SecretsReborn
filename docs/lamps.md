# Lampen und Runensicht

Der Charakter besitzt einen eigenen Equipment-Slot `Lamp` (Index 14), unabhängig von Haupt- und Nebenhand. Die Tasche bleibt bei 10 × 4 Plätzen. Drag-and-drop und Controller-Auswahl funktionieren wie bei den anderen Slots; vom untersten Ausrüstungsslot führt „unten“ zur Lampe unter der Charaktervorschau.

`ItemDefinition.Lamp` verweist auf ein `LampDefinition`-Profil. Dort sind Farbe, Radius, Intensität und die Fähigkeit zur Runensicht einstellbar. Wanderlaterne: warmes gelbliches Licht, keine Runensicht. Runenlaterne: violettes Licht, Runensicht innerhalb ihres Enthüllungsradius. Beide sind im Charakterkatalog und als Pickup-Prefabs enthalten; im Waldheiligtum liegen sie zunächst nahe dem Startpunkt. Diese Platzierung ist ein Vorschlag für den Prototyp.

L / obere Gamepad-Taste schaltet die ausgerüstete Lampe. Ohne Lampe bleibt das Licht aus. Ablegen schaltet es aus; Wechseln bei eingeschaltetem Licht übernimmt unmittelbar das neue Profil. Das Licht bleibt beim Neuladen einer Szene ausgeschaltet, die ausgerüstete Lampe bleibt erhalten. Die Lichtschaltung ist momentan lokale Präsentation; ein späterer Netzwerkadapter muss den gewünschten Lampenzustand pro Charakter übertragen und Runeninteraktionen auf dem Host validieren.

Ein echtes URP `Light2D` mit rundem, weichem Lichtkegel folgt dem Spieler. Die bearbeitbaren Szenen erhalten bei Bedarf ein globales Umgebungslicht (0,65). Sprite- und Tilemap-Renderer mit bisherigen Standard-Unlit-Materialien erhalten das neue Sprite-Lit-Material; eigene Materialien und bestehende globale Lichter bleiben erhalten. Es gibt noch keinen Tag-/Nachtzyklus und keine automatischen Schatten an Bäumen oder Wänden.

Runen und Pfeile erscheinen ausschließlich mit eingeschalteter Runenlaterne in Reichweite. Runenkreise werden ebenfalls nur dann aktiviert, wenn die Runensicht aktiv ist. Die Wanderlaterne löst das Rätsel nicht aus.

Spielstände verwenden Schema 7. Versionen 1–6 mit 14 Ausrüstungsplätzen werden mit leerem Lampenplatz geladen, bestehende Slot-IDs bleiben erhalten. Beim nächsten Speichern werden 15 Plätze geschrieben. Ein fehlender Lampenplatz in Schema 7 wird als ungültiger Spielstand abgelehnt.

Testen: Play → `SecretsReborn → Character → Test → Equip warm lamp` / `Equip rune lamp`, dann L. Alternativ die zwei Laternen am Start mit E / A aufheben und im Inventar anlegen. Lichtfarbe, Runensicht und Ablegen vergleichen; am Buch speichern und laden. Alte Spielstände enthalten noch keine Lampe und benötigen einen der neuen Pickups oder das Testmenü.

Brightness ist im Lampenprofil von 1–10 einstellbar und wird auf die Light2D-Intensität 0,1–1 abgebildet. Radius und Helligkeit erscheinen in der Itembeschreibung.
