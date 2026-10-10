# Inventar und Lager im Unity-Editor gestalten

Seit 9. Oktober 2026 verwenden Inventar, Shared Stash sowie Revive-HUD
Canvas/uGUI statt OnGUI. Die Item-, Savegame- und Host-Transferlogik bleibt
getrennt von der Darstellung. Hauptmenü, Speicherbuch und andere bisherige
Menüs sind damit noch nicht auf Canvas umgestellt.

## Das richtige Prefab öffnen

Im Project-Fenster **Assets → Resources → InventoryUI → InventoryCanvas**
doppelklicken. Im Prefab-Modus sind diese Gruppen editierbar:

- **InventoryPanel:** Ausrüstung, Rucksack, Gold, frontale
  Charaktervorschau, Beschreibung und Buttons.
- **SharedStashPanel:** eigener Rucksack links, gemeinsames Lager rechts.
  Für die Vorschau InventoryPanel deaktivieren und SharedStashPanel aktivieren.
- **PotionAndReviveHUD:** situationsabhängiger Revive-Hinweis; der alte Trank-HUD ist deaktiviert.

Position und Größe über **Rect Transform**, Rahmen über **Image**, Texte und
Schriftgrößen über **Text** ändern. Die Canvas-Referenzauflösung ist 1040 × 600;
Canvas Scaler passt die Oberfläche an andere Auflösungen an. Rahmen verwenden
Sprite-Borders und gekachelte Flächen, damit die Ornamente nicht als gesamtes
Bild gestreckt werden. Grafikimporte bleiben Point, ohne Kompression/Mipmaps.

Das Canvas wird zur Laufzeit für die lokal gesteuerte Figur aus Resources
instanziiert. Es muss nicht zusätzlich in jede Gebietsszene gelegt werden.
In Play erscheint es als **InventoryCanvas** in der Hierarchy. Dauerhafte
Layoutänderungen am Prefab außerhalb von Play vornehmen.

## Slots und Bilder

**InventorySlot.prefab** im selben Ordner ist die gemeinsame Slot-Vorlage.
ItemIcon ist zentriert; seine konfigurierte Größe bestimmt die maximale
Icon-Fläche. Der sichtbare Item-Ausschnitt wird darin mit erhaltenem
Seitenverhältnis eingepasst. EmptyGlyph zeigt bei Equipment leere graue Icons,
Selection den ausgewählten Slot und Quality die Seltenheitsfarbe.
Einzelne Canvas-Slots besitzen Overrides, etwa ihr Equipment-Icon und ihre Position.

Die sechs PortraitLayer bilden Körper, Augen, Haare und Equipment frontal ab.
Ihr Sprite und ihre Farbe kommen aus dem CharacterAppearance-System; Änderungen
an diesen Laufzeitwerten im Prefab werden deshalb überschrieben. Position und
die Größe der gemeinsamen Vorschaufläche über PortraitLayer-0 konfigurieren.

## Verknüpfungen erhalten

Slot-Index und Referenzen in **InventoryCanvasView** / **InventoryCanvasSlot**
nicht beim Verschieben ändern oder entfernen. Die Indizes sind fachlich fest:

| Bereich | Index |
|---|---|
| Rucksack | 0–39 |
| Equipment | 40–54, einschließlich Lampe |
| Frühere HP-/Mana-Schnellplätze (inaktiv, nicht navigierbar) | 55–56 |
| Lageransicht: eigener Rucksack / Shared Stash | 0–39 / 40–79 |

Buttons behalten ihre On Click-Verknüpfungen zu ActivateSelected bzw. Close.
Controller-Navigation und Aktionen verarbeitet InventoryInteraction; das
EventSystem verarbeitet Maus/Drag-and-drop. So lösen A/Enter keine doppelten
Aktionen aus. Im geöffneten Inventar bleiben Bereichswechsel, goldene Auswahl,
Anlegen/Ablegen und das direkte Benutzen von Tränken erhalten.

**SecretsReborn → UI → Prepare editable inventory canvas** erstellt fehlende
Grund-Prefabs und prüft Slot-Referenzen. Vorhandene Prefabs werden beibehalten;
das Menü ist kein Zurücksetzen des eigenen Layouts.

Item-Vorlagen selbst werden weiterhin gemäß [Item-Anleitung](item-authoring.md)
angelegt. Für EXE-Tests nach Änderungen den vollständigen Windows-Testbuild
aktualisieren und auf allen PCs dieselbe Version verwenden.

Trank-Schnellslots sind zugunsten des [gemeinsamen Rings](spell-ring-menu.md)
deaktiviert. **SecretsReborn → UI → Prepare consumable ring** deaktiviert diese
alten Elemente im bestehenden Prefab, ohne Ausrüstung, Lager oder das übrige
Layout aufzubauen. Verbrauchsgegenstände bleiben im Rucksack und erscheinen
im Ring mit Menge und Wirkung.
