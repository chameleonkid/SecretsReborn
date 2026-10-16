# Einheitliche Spieloberflächen

Stand: 16. Oktober 2026. Inventar und Lager verwenden jetzt eigene, deckende Schiefer-/Goldgrafiken nach der alten Secrets-Inventarvorlage. Abstimmungen, Zauberbeschreibung und Speicherbuch behalten ihre dunklen transparenten Flächen. Helle Schrift und Goldakzente verbinden die Ansichten; die früheren gekachelten Waldflächen entfallen.

## Inventar und Lager bearbeiten

`Assets/Resources/InventoryUI/InventoryCanvas.prefab` enthält InventoryPanel und SharedStashPanel unter eigenen Safe-Area-Gruppen. Referenz 1040 × 600, Expand; die Rahmen werden einschließlich Randabstand proportional eingepasst. Alle 40 Rucksack-, 15 Equipment- und 40 Lagerplätze sowie ihre Indizes und Aktionsbindungen bleiben erhalten. Tränke erscheinen weiterhin ausschließlich im Ring; im Lager bleiben sie zum Tauschen sichtbar.

- Hintergrund und Außenrahmen: jeweiliges Panel → Image → Source Image. `InventoryUI/SlateGold/Inventory-v1.png` und `Stash-v1.png`, Color Weiß mit Alpha 100 %. Die fertigen Hintergründe enthalten keine dynamischen Texte oder Slots. [Grafikquellen und Prompts](inventory-art-prompts.md).
- Slotrahmen: Quiet border → Quiet Menu Frame Graphic. Farbe, Linienstärke und Inset sind editierbar. Jeder dieser Grafikknoten benötigt einen CanvasRenderer.
- Slots: InventorySlot.prefab. Leere Plätze sind zurückhaltend, Auswahl bleibt gold, Seltenheit behält ihre Farbe. Der Qualitätsrahmen wird von InventoryCanvasSlot mit der Itemfarbe aktualisiert; Item-Icons, graue Equipment-Symbole, Mengen und Drag-and-drop bleiben erhalten.
- Beschreibungen liegen im vorgesehenen vertieften Grafikbereich. Gold steht neben der Rucksacküberschrift; veraltete Schnellzugriffsbeschriftung ist durch Laterne ersetzt. Dynamische Details dürfen bei langen Texten innerhalb der bestehenden Mindestschriftgröße kleiner werden.
- Buttons: Quiet Action Button ergänzt dezentes Hover-Wachsen und Druckfeedback; bestehende On Click-Verbindungen bleiben erhalten.

## Zauberbeschreibung

`Assets/Resources/Magic/UI/SpellIconRingCanvas.prefab`: InfoStrip und SpellBook besitzen denselben transparenten Hintergrund und Goldrahmen. InfoStrip/Title und Details legen Schrift und Abstände fest. Die Beschreibung wächst weiterhin mit ihrem Inhalt; Mana, Rang, Direktschaden und skalierte Burn-/DoT-Angaben bleiben erhalten. Die Runtime-Transparenz wird über SpellRingView/Info Background Alpha und Book Background Alpha eingestellt (je 84 %).

## Gruppenabstimmung

`Assets/Resources/Interface/GroupVoteCanvas.prefab` ist vollständig im Prefab-Modus editierbar. Zur Laufzeit erscheint GroupVoteCanvas bei der Netzwerkkomponente. Group consent enthält Überschrift, Zielgebiet, vier feste Teilnehmerzeilen, Fortschritt, Restzeit und Bestätigen/Abbrechen. Die Höhe passt sich an die tatsächliche Teilnehmerzahl an. Safe Area und Canvas Scaler (1280 × 800, Expand) passen die gesamte Ansicht ein.

Ein grüner, geometrisch gezeichneter Haken bedeutet **vom Host bestätigte Zustimmung**. Ein neutraler Kreis mit „Wartet auf Antwort“ zeigt ausstehende Antworten. Spielername und Figurenname werden zusammen angezeigt, der Host ist gekennzeichnet und die eigene Zeile trägt „Du“. Längere Namen können umbrechen oder innerhalb 12–18 Punkten kleiner werden. Der Stand und die Namen sind auf allen verbundenen Rechnern derselbe Hostzustand. Nach lokalem Bestätigen wartet die Ansicht auf die Gruppe; nach einer neuen Abstimmung sind alte Haken gelöscht.

Die bestehende Regel bleibt: Alle müssen zustimmen; Nein, Trennung oder Fristablauf brechen ab. Nach Zustimmung aller startet der bestehende Lade-/Gebietswechsel. Ablehnen lädt keinen alten Weltzustand. Statuspakete sind an die Abstimmungs-ID gebunden und werden auf Clients nur vom Host angenommen. Das Netzwerkprotokoll ist jetzt 27 (NGO-Kompatibilitätsgrenze 17): Auf allen PCs denselben neuen Build verwenden. Das Spielstandformat bleibt unverändert.

## Speicherbuch und Überlagerungen

Auch die Buchansicht verwendet jetzt transparente Flächen und Goldlinien statt des gemalten Papierhintergrunds. Ihre gemeinsamen Slots, Bedienrechte und Speicher-/Ladeaktionen bleiben erhalten. Die bestehende OnGUI-Darstellung bleibt; Farben und Zeichnung sind in GameUiStyle zusammengefasst. Namensschilder, der Buch-Öffnen-Hinweis und geschlossene Debug-Anzeigen überdecken geöffnete Spieloberflächen nicht mehr.

## Setup und Prüfungen

`SecretsReborn → UI → Apply unified game UI` aktualisiert die vorhandenen Prefabs und erzeugt bei Bedarf das Abstimmungs-Prefab. `Apply illustrated inventory and stash` wendet gezielt die beiden deckenden Hintergründe und das passende Slotlayout an. Dabei werden die definierten Stilwerte und Layoutpositionen angewendet; eigene Stilabweichungen daher vorher sichern. Ungespeicherte Szenenänderungen werden nicht überschrieben. Es erfolgt kein automatischer Neuaufbau beim Spielstart. Vorhandene Slot-Indizes, Ausrüstungsreferenzen und Buttonaktionen werden bewahrt.

Die nachträglich ergänzten Rahmen und Haken besaßen zunächst keine CanvasRenderer; dadurch blieben Auswahl und Haken unsichtbar. Die Komponenten fordern den Renderer jetzt per RequireComponent an; die vorhandenen Prefabs und die offene Hauptmenüszene sind ebenfalls repariert. Die Slot-Auswahl erhält zusätzlich einen kräftigen Goldrand und eine leichte Goldtönung. Die Prüfung zählt sichtbare goldene Pixel innerhalb des ausgewählten Slots bei Rucksack, Equipment und beiden Lagerhälften; ein aktives GameObject allein gilt nicht mehr als erfolgreicher Darstellungstest.

Außerdem deaktivierte das frühere ConsumableRingSetup alle Canvas-Slots ab Index 55, einschließlich 25 gültiger Lagerplätze. Es beschränkt sich jetzt auf die früheren Inventar-Schnellplätze. Die Lageransicht enthält wieder 80 aktive Slots (40 eigener Rucksack, 40 gemeinsames Lager), einschließlich des letzten auswählbaren Index 79. Der Grafikimporter behandelt nur die neuen SlateGold-Backplates als bilinear gefilterte Sprites; die bisherigen Atlanten und Point-Itemgrafiken behalten ihre Importart.

Der Development-Test `--game-ui-role host|client --game-ui-report <absoluter-pfad>` läuft mit zwei Spielprozessen; beide Reports müssen im selben Ordner liegen. Er verwendet eine nicht gespeicherte Testwelt, getrennte TestSlot-Pfade und eigene Rejoin-Schlüssel unter SecretsReborn.Test.GameUI. Auch die Lobby-UI-Prüfung besitzt einen eigenen Rejoin-Namensraum. Geprüft werden Inventar/Ausrüstung, Ein-/Auslagern, Burn-Beschreibung, replizierte Zustimmung, Abbruch und einstimmiges Laden. Screenshots rendern zuerst die Welt und danach das tatsächliche Canvas mit der Weltaufnahme dahinter; so können Sprite-Sortierwerte beim Aufnehmen keine UI-Texte verdecken.
