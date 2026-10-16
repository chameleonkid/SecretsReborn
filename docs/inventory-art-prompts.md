# Inventar-/Lagergrafiken: Herkunft und Prompts

Erstellt am 16.10.2026 mit dem eingebauten **image_gen**-Werkzeug, deckender Hintergrund. Unveränderte PNG-Ausgaben liegen im Projekt unter `Assets/Resources/InventoryUI/SlateGold/Inventory-v1.png` und `Stash-v1.png`. Keine Raster-Nachbearbeitung. Unity legt Slots, Icons, Texte, Buttons und Auswahlrahmen separat darüber.

Gestaltungsreferenz: [Secrets/Assets/Art/Inventory/Inventory.png](https://github.com/chameleonkid/Secrets/blob/99f5b1c15fa6201e84044fbd81f9397d4f9709a2/Assets/Art/Inventory/Inventory.png), Commit `99f5b1c15fa6201e84044fbd81f9397d4f9709a2`. Zusätzlich betrachtet: Inventory_Base.png, Inventory_Test1.png und PlayerInventoryBackground.png. Die neue Gestaltung übernimmt die dunklen ruhigen Steinflächen, Goldkanten und getrennten Bereiche; die alte 21-Platz-Anordnung wird nicht übernommen.

## Inventar: vollständiger Prompt

Referenzbild: die oben verlinkte alte Inventory.png.

```text
Use case: ui-mockup. Create a production-ready EMPTY INVENTORY BACKPLATE bitmap for a dark fantasy Unity RPG, inspired by the attached older Secrets inventory's quiet black slate and restrained antique gold edges. Wide landscape canvas aspect 26:15, ideally1560x900. Orthographic straight-on flat UI artwork, full bleed opaque, no perspective, no surroundings. Elegant dark charcoal basalt and almost-black worn leather interiors, fine antique brass and gold engraved borders, four small Celtic metal corner clasps, subtle crafted relief, excellent readability, sophisticated understated RPG inventory. NO green leaves, NO repeated tiles, NO text or letters or numerals, NO icons, NO items, NO character, NO individual inventory slot grid. This asset is drawn underneath LIVE Unity slots and text. Composition in virtual1040x600coordinates: outer frame x8 y8 to1032 y592 with subtle brass corners; slim header area y22-58; LEFT equipment bay x28-490 y60-525 with understated shadowed character alcove centered x255 y265 and otherwise a very dark quiet surface for equipment slots/stat text; RIGHT bag bay x510-1014 y60-318 for live10x4slots; quiet right description inset x510-1014 y385-535 with only thin recessed border; footer area y540-585. Two balanced vertical zones divided nearx497, delicate horizontal rule under header. Keep all content areas very dark and uncluttered, highest decorative detail limited to outer corners and borders. Beautiful coherent handmade game UI panel matching source palette. Flat actual game asset, not a concept screenshot.
```

Originalausgabe: `C:/Users/stefa/.codex/generated_images/01a12767-856c-7bf2-8fc0-5e726fc4c404/exec-016a6179-9811-471a-93ad-ca0481787185.png`.

## Lager: vollständiger Prompt

Referenzbild: die zuvor erzeugte Inventory-v1.png.

```text
Create the matching SHARED STASH empty backplate for this exact inventory UI set. Same landscape26:15 aspect, same black basalt, almost-black leather, antique gold thin polished edges, four Celtic gold corner clasps, quiet high-quality dark fantasy craftsmanship, same outer dimensions and matching header/footer. Fully opaque flat bitmap actual Unity game asset. No text, no letters, no icons, no items, no character, NO INDIVIDUAL SLOTS. This is a graphic UNDER live Unity slots and editable labels. Interior layout DIFFERENT from reference: two EQUAL rectangular bays side by side occupying x28..507 and x530..1012 in virtual1040x600 coordinates, y98..315. Each must accommodate10columns x4rows of live slots. No portrait alcove. A single full-width description bay x34..1006 y382..527 beneath both grids, with subtle recess and thin brass edge; uninterrupted dark quiet surface for several lines text. Overall header y20..61, quiet label strip y65..97; quiet horizontal controls area y326..366 between grids and description; footer y540..585. Symmetrical balanced matching inventory panel, delicate center divider only between top two bays, quiet dark surfaces beneath content, outer ornaments never intrude into live content. Beautiful finished understated UI matching provided reference precisely in palette/material.
```

Originalausgabe: `C:/Users/stefa/.codex/generated_images/01a12767-856c-7bf2-8fc0-5e726fc4c404/exec-043ca51c-d24a-430e-b713-4cc80ea90c02.png`.

Die tatsächlichen UI-Positionen wurden anschließend an die sichtbaren Grafikbereiche angepasst; maßgeblich ist das editierbare InventoryCanvas.prefab. Der Canvas wird proportional eingepasst, die Grafik wird nicht auf das Seitenverhältnis des Bildschirms gestreckt.
