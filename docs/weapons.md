# Waffenprofile am Item

Ein `ItemDefinition` vom Typ Weapon referenziert ein `WeaponDefinition`-Asset.
Dieses beschreibt Schwert, Axt oder Streitkolben und hält Spriteframes, Farbton,
Schaden, Reichweite, Angriffsdauer, Abklingzeit, Trefferzeitpunkt und Knockback.
Die Abklingzeit ist mindestens so lang wie die Angriffsdauer.

Der Host liest das Profil aus dem tatsächlich angelegten Item. Kampfwerte werden
beim Angriffsbeginn übernommen; die Darstellung nutzt dasselbe Profil und dieselbe
Angriffsdauer. Ein Waffenwechsel während einer bereits gestarteten Attacke ändert
diese Attacke nicht. Waffen ohne Nahkampfprofil, beispielsweise der Übungsbogen,
können nicht über den Nahkampfeinstieg angreifen. Ohne angelegte Waffe bleibt der
unbewaffnete Testschlag erhalten.

## Normal und rötlich

`Assets/World/Combat/Weapons/TrainingSword.asset` und `RedTrainingSword.asset`
verwenden dieselben korrigierten Schwertframes, aber unterschiedliche Farbtöne.
Die zugehörigen Items heißen `training-sword` und `red-training-sword`.
Angriff und Inventaricon berücksichtigen den Farbton. Die einfache Färbung wirkt
auf die ganze Grafik, einschließlich Griff und Schlagspuren. Eine spätere Farbmaske
kann gezielt nur die Klinge ändern.

Im Play-Modus unter `SecretsReborn → Character → Test` zwischen `Equip training sword`
und `Equip red training sword` wechseln. Die normalen Inventaroperationen nehmen
das Testitem auf und legen es in der Haupthand an. Das vorherige Item wandert ins
Inventar; dessen Kapazitätsregeln gelten weiterhin. Speichern und Laden brauchen
keine neue Savegame-Version, da weiterhin die Item-ID gespeichert wird.

## Weitere Nahkampfwaffen

Über `Create → SecretsReborn → Weapon profile` ein Profil anlegen und am Item
referenzieren. Für Axt und Streitkolben den Typ einstellen, eigene Darstellungsframes
zuweisen und Kampfwerte anpassen. Noch sind keine Axt-/Streitkolben-Grafiken importiert.
Die aktuelle Figurenbasis verwendet vier Attackenframes je Blickrichtung, Indizes
67–70, 83–86, 99–102 und 115–118 innerhalb eines Arrays mit 128 Einträgen. Waffenframes
müssen die Spieleranker und diese Zuordnung einhalten; übergroße Sprites sind möglich.

## Späterer Glow

Das Profil bietet `Visual Material`, `Emission Mask` und eine HDR-`Emission Color`.
Die Maske muss zur Textur und ihren Sprite-Rechtecken passen: Schwarz für nicht
leuchtende Bereiche, Weiß für leuchtende Bereiche. `PlayerMelee` übergibt die Maske
und die Emissionsfarbe über einen MaterialPropertyBlock, wenn der gewählte Shader
`_EmissionMap` und `_EmissionColor` unterstützt. Ein gewöhnliches Sprite-Unlit-Material
stellt diese Emission noch nicht dar. Ein passender Shader und HDR/Bloom sind der
nächste Schritt für sichtbaren Glow; sie sind hier noch nicht implementiert.

Die optionale Maske im Waffenprofil ist noch keine in Unity registrierte Sprite-
Secondary-Texture. Diese Zuordnung kann später mit dem gewählten Shader ergänzt
werden. Materialien werden pro Figur nicht global umgefärbt. Für Netzwerk-Koop
müssen Item-ID und Aktionszustand repliziert werden; die Assets bleiben lokale
Darstellungsdaten und die Trefferentscheidung beim Host.

Der Schadenswert wird im Inventar angezeigt (Einheit: Halbherzen). Der Host liest Damage vom Profil des tats�chlich angelegten Waffen-Items; die beiden �bungsschwerter verursachen derzeit jeweils 2 Halbherzen Schaden.

Cooldown ist pro Waffenprofil unabh�ngig vom Schaden konfigurierbar (mindestens die Attack Duration). Der Host erzwingt diesen Abstand bereits zwischen Angriffen. Ein st�rkeres Axtprofil kann beispielsweise einen l�ngeren Cooldown erhalten; die Anzeige liest stets den tats�chlich wirksamen Wert aus dem Profil.
