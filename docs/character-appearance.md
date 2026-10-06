# Figuren und angelegte Rüstung

## Festgelegte Anforderung

Angelegte Rüstung verändert das Aussehen der jeweiligen Spielerfigur. Das muss
auch im späteren Koop für alle Mitspieler sichtbar sein. Die Ausrüstung gehört
zum eigenen Charakter und wird vom Host innerhalb seiner Welt gespeichert.

## Befund aus Secrets

Geprüft wurden `Assets/Prefabs/Characters/Player.prefab` und
`Assets/Prefabs/Characters/MainPlayer_V1.prefab` am Commit `99f5b1c`.
Die Prüfsnapshots liegen als Text unter `docs/asset-review` und sind keine
ausführbaren Prefabs im neuen Projekt.

`MainPlayer_V1` enthält unter anderem getrennte Referenzen `bodySkin`,
`armorSkin`, `eyesSkin` sowie Standard-Rüstungstexturen. `InventoryArmor.cs`
speichert Rüstungstexturen für zwei Figurenvarianten. `SpriteSkinRPC.cs`
ersetzt Körper- oder Kleidungsframes anhand einer gemeinsamen Frame-Nummer;
Kind-Layer folgen dabei dem Körperframe. Die vorhandenen Prefabs referenzieren
weitere alte Komponenten und Assets. Sie werden daher nicht ungeprüft als
vollständige Spielersteuerung übernommen.

## Umsetzung – vorgeschlagene Schritte

1. Animierte Körperbasis mit einheitlicher Frame-Zuordnung für Bewegung und Blickrichtung einrichten.
2. Rüstung als getrennten visuellen Layer hinzufügen. Alle Layer verwenden denselben Animationsframe, Fußpunkt und Maßstab.
3. Rüstungsdefinitionen erhalten stabile IDs und passende visuelle Varianten. Figur und Ausrüstung bleiben getrennte Daten.
4. Beim Anlegen/Ablegen wechselt die Darstellung; ohne angelegte Rüstung greift eine definierte Grundkleidung.
5. Der Host prüft Ausrüstungsänderungen und speichert beziehungsweise synchronisiert die Ausrüstungs-IDs. Jeder Client stellt daraus dieselben Layer dar; Sprite- oder Texturdateien werden nicht als Spielzustand übertragen.
6. Kamera und Eingabe bleiben lokal; auch entfernte Figuren bekommen ihre eigene, aus dem Charakterzustand abgeleitete Darstellung. Darstellungsskripte greifen nicht auf ein globales lokales Inventar zu.

Die Animationsbasis mit getrenntem Waldläuferkleidungs-Layer ist inzwischen
umgesetzt; siehe [character-and-ground.md](character-and-ground.md).
`CharacterAppearance.Equip` ist die Darstellungsschnittstelle für spätere
bestätigte Ausrüstungsänderungen. Noch keine Inventar-, Netzwerk- oder
automatische Rüstungswechselmechanik implementiert. Der Waffen-Layer bleibt
ein Asset-Vorschlag.
