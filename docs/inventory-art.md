# Inventargrafik

`Assets/Resources/InventoryUI/ForestInventoryAtlas.png` wurde mit Imagegen
für SecretsReborn erzeugt: dunkles grünes Leder, geschnitztes Holz, Messing
und Efeu. Der Atlas enthält Panel, normalen Slot, goldenen Auswahl-Slot und
Button. Schrift und Itembilder werden weiterhin dynamisch darüber gezeichnet.

`ForestInventorySkin` zeichnet die Atlasbereiche in neun Segmenten, damit
die Eckverzierungen bei unterschiedlichen Fenstergrößen erhalten bleiben.
`InventoryArtImport` setzt Point-Filter, deaktivierte Mipmaps und unkomprimierten
Import. Die Originalgrafik bleibt unverändert; Ausschnitte werden über UVs gewählt.

Die Grafiken sind in der vorhandenen Oberfläche eingebunden. Runtime- und
Editor-Code kompilieren. Abschließende Lesbarkeitsprüfung im Game-Fenster:
leere/belegte Slots, goldene Auswahl, Controllerbedienung und beide Buttons.
Inventarzustand, Ausrüstungsregeln und Szenenplatzierung werden nicht geändert.

Der Außenrahmen wiederholt nun seine mittleren Kantensegmente und Innenfläche
im Maßstab der Ecken; Ornamente werden nicht auf Fensterbreite gedehnt.
`EquipmentGlyphs.png` ist ein zusätzlich generierter 4×4-Atlas mit grauen
Slot-Symbolen. Leere Ausrüstungsslots zeigen diese statt seitlicher Beschriftung;
der Slotname bleibt im Detailfeld lesbar.

Item-Icons verwenden einen vermessenen Alpha-Inhaltsbereich, damit transparente
Ränder eines Animationsframes die Zentrierung nicht verschieben. Bei neuen oder
veränderten Icons `SecretsReborn > Inventory > Align item icons` ausführen.
Das verändert keine Quelldateien oder Figuren-Sprites.

`ItemDefinition.Quality` bietet Normal (hellgrau), Uncommon (grün), Rare (blau),
Epic (lila) und Legendary (orange). Qualität wird als innerer Rand plus Text im
Detailfeld dargestellt; der äußere goldene Auswahlrahmen bleibt unabhängig.
Qualität erzeugt noch keine automatischen Werte, Drop-Raten oder Tier-Regeln.
