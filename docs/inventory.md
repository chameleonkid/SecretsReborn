# Inventar und Ausrüstung

Die Tasche hat feste **40 Plätze (10×4)**. Leere Plätze bleiben erhalten.
Angelegte Items verlassen die Tasche und liegen in einem der 14 Ausrüstungsplätze:
Helm/Hut, Schultern, Armor, Gürtel, Hände, Beine, Stiefel, zwei Ringe,
Amulett, Seal, Cloak, Haupthand und Nebenhand.

## Bedienung

- **I / Gamepad Start** öffnet oder schließt das Inventar; Esc / B schließt es.
- **E / A** hebt außerhalb des Inventars ein nahes Item auf.
- Linksklick wählt einen Platz; Pfeiltasten oder D-Pad wechseln die Auswahl.
- **Tab / RB** wechselt zwischen Taschen- und Ausrüstungsauswahl.
- **Rechtsklick / Enter / A** legt das ausgewählte Item an oder ab.
- Drag-and-drop verschiebt oder tauscht Tascheninhalte; gleiche Items werden
  bis zur Stapelgrenze zusammengeführt. Auf einen passenden Ausrüstungsslot
  ziehen zum Anlegen; aus Ausrüstung auf einen leeren Taschenplatz zum Ablegen.
- Die Figurenvorschau zeigt die aktuellen Körper-, Kleidungs-, Haar- und Augen-Layer.
  Auswahl beziehungsweise Hover zeigt Itemname, Typ und Zweihandstatus.

Das Fenster skaliert mit dem Game-Fenster. Es blockiert Bewegung der lokalen
Figur, pausiert aber nicht die Welt. Die Darstellung verwendet derzeit IMGUI,
keine endgültigen UI-Art-Assets.

## Regeln

Einhandwaffen passen in die Haupthand, Schilde in die Nebenhand. Zweihandwaffen
belegen beide Hände; die Nebenhand zeigt dann `2H`. Beim Anlegen einer Zweihandwaffe
wandern vorherige Haupthand und Schild in die Tasche. Beim Anlegen eines Schilds
wird eine vorhandene Zweihandwaffe abgelegt. Fehlt Platz, bleibt der gesamte
Zustand unverändert. Noch kein Dual Wield. Zwei gleiche Ringe sind vorerst erlaubt.

Nur **Helm/Hut, Armor und Stiefel** beeinflussen die Figurengrafik.
`ItemDefinition` enthält Typ, Zweihandstatus, Stapelgröße, optionales Icon und
optionale Darstellung (`Armor Appearance`, auch für Kopf/Füße nutzbar).
Kopf-/Fuß-Sheets müssen ausschließlich ihren jeweiligen Bild-Layer enthalten;
alle Figuren-Sheets verwenden gleiche Frames, Fußpivot und 24 PPU.
Bislang besitzt nur die blaue Übungsrüstung eine eigene Darstellung; für weitere
Kopf-/Fußausrüstung sind passende Sheets noch zuzuweisen.

## Testgegenstände

Die bestehende blaue Übungsrüstung bleibt neben dem Startpunkt. Schwert, Bogen,
Schild und Ring sind zusätzlich als Item-Assets und Test-Pickup-Prefabs vorhanden:
`Assets/World/Sanctuary/Prefabs/training-*-pickup.prefab`.
Zum Testen aus dem Project-Fenster unter `World/Items` ziehen und frei platzieren.
Ihre Weltgrafiken sind vorläufige farbige Markierungen; die Karte wird nicht
ungefragt mit weiteren Items befüllt. Bei Duplikaten eigene World Item IDs vergeben.
Der Editor-Befehl `SecretsReborn > World > Set up equipment slot examples`
ergänzt den Player-Katalog und erzeugt fehlende Testassets, ohne die Szene zu verändern.

## Zuständigkeit und Prüfung

`InventoryState` enthält ausschließlich IDs und Mengen; Mutationen arbeiten auf
Kopien und übernehmen erst den vollständig erfolgreichen Wechsel.
`CharacterInventory` prüft den Item-Katalog und Solo-State-Authority; Eingabe
und Darstellung ändern den Zustand nicht direkt. Jeder Charakter besitzt seine
eigene Instanz. Ein späterer Netzwerk-Host muss Besitzer und Anfragen prüfen,
Zustand replizieren und Charakter-/Welt-IDs eindeutig vergeben.

Standalone-Prüfungen in `tests/InventoryStateChecks.cs`: feste Plätze,
Stapelverteilung, Verschieben, Besitz, passende Ausrüstungsslots, zwei Ringe,
Einhand/Schild, Zweihandwechsel, volle Tasche, atomare Ablehnung und getrennte
Charakterzustände. Runtime- und Editor-Code kompilieren.

Noch keine Persistenz oder Netzwerkverbindung. Neuer Play-Durchlauf startet
leer. Nächster Schritt: versioniertes Savegame für Welt-Item-IDs, Charaktere,
40 Taschenplätze und 14 Ausrüstungsplätze.
