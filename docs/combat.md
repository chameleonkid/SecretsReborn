# Erster Nahkampf und Eichen-Baumgegner

Angriff: **Leertaste oder J**, am Controller **X / westliche Taste**. Körper, Kleidung, Haare und Augen nutzen die Angriffsframes der Figurenbasis. Bei angelegtem Schwert läuft synchron das originale `rpc female sword.png` aus Secrets. Der Spieler hält für 0,35 Sekunden an; der Host prüft Treffer nach 0,14 Sekunden im Schlagmoment. Blickrichtung bleibt während des Schlags fest. Der einfache goldene Bogen wurde entfernt.

Der Host entscheidet über Angriffe, Abklingzeit, Reichweite, Blickrichtung und freie Sicht. Nahkampf trifft innerhalb von 1,6 Welteinheiten in einem vorderen 120-Grad-Bereich. Wände, Bäume und andere feste Hindernisse blockieren Treffer. Angriffe sind beim Laden, bei 0 Herzen oder geöffnetem Inventar/Speicherbuch gesperrt.

Ohne Waffe verursacht der Testschlag einen Schadenspunkt; eine angelegte Nahkampfwaffe zwei. Schwerttreffer stoßen den Gegner für 0,18 Sekunden zurück, während seine KI und sein Kontaktschaden aussetzen. Kollisionen begrenzen den Rückstoß an Wänden. Der bestehende Übungsbogen greift hier nicht im Nahkampf an. Allgemeine Waffenprofile, Fernkampf, Ausdauer, Blocken und Zauber folgen später.

## Gegner

`OakMeleeEnemy.prefab` verwendet das ursprüngliche `log_oak.png` aus Secrets, Commit `99f5b1c15fa6201e84044fbd81f9397d4f9709a2`, Pfad `Assets/Resources/Retro Pixel Monsters/Spritesheets/log_oak.png`. Ursprünglicher Maßstab: 16 PPU, 32×32 Frames. Grafik und Original-Metadaten wurden geprüft; alte Gegner-Scripts und Manager-Abhängigkeiten werden nicht übernommen.

Der Setup-Schritt ergänzt den ersten Gegner unter `World/Enemies`, bevorzugt bei (-6, 2). Ist dort Gelände blockiert, prüft er einige alternative Plätze im Wald. Er erkennt Spieler innerhalb von fünf Einheiten, verfolgt langsam und bleibt nahe seinem Ausgangspunkt. Er greift durch Berührung an; eine Vorwarnung oder Schlaganimation gibt es für den Log nicht. Erst echte Kollision der festen Collider verursacht ein halbes Herz Schaden. Der Spieler erhält 0,8 Sekunden Schutz vor weiteren Gegnertreffern. Die Laufschleife nutzt die Frames 1–2–3–2 und enthält den Idle-Frame 0 nicht. Der Gegner hat sechs Schadenspunkte: drei Schwerttreffer oder sechs unbewaffnete Treffer besiegen ihn.

Die Rigidbody-Kollision stoppt den Gegner an Hindernissen; eine Wegsuche um komplexe Hindernisse ist noch nicht implementiert. Bei 0 Herzen bleiben Buch/Inventar für Tests erreichbar; vollständige Todes-/Respawnregeln sind weiterhin offen.

Jede platzierte Gegnerinstanz braucht eine eindeutige `Enemy Id` innerhalb der Host-Welt. Die erste Instanz heißt `sanctuary-oak-01`. Besiegte Gegner bleiben bei Gebietswechseln und nach Save/Load besiegt. Nicht besiegte Gegner beginnen nach dem Laden oder Gebietswechsel wieder mit voller Gesundheit. Savegame-Version 6 speichert die besiegten IDs; ältere Spielstände bleiben lesbar.

## Prüfen

Das Schwert-Visual liegt als `Assets/World/Combat/Prefabs/PlayerSwordVisual.prefab`
vor und wird am `PlayerMelee`-Component des Spieler-Prefabs referenziert. Es wird
nur bei angelegter Nahkampfwaffe angezeigt. Zum gezielten Testen kann im Play-Modus
`SecretsReborn → Character → Test → Equip training sword` das Übungsschwert
über die normalen Inventaroperationen aufnehmen und in der Haupthand anlegen.

Die Log-Frames werden nach ihrer Position im Sheet sortiert, unabhängig von
Namen wie `OakEnemy_1` und `OakEnemy_10`. Zugeschnittene Rechtecke behalten einen
gemeinsamen Fußpunkt aus dem ursprünglichen 32-Pixel-Raster, damit der Baum
beim Framewechsel nicht springt. Manuelle Sprite-Schnitte bleiben erhalten.

Die Richtungszeilen des Logs wurden anhand der ursprünglichen Laufclips geprüft:
oben im Sheet zuerst unten blickend, dann oben, rechts und links.
Das Schwert benutzt die ursprünglichen unterschiedlich großen Frame-Rechtecke
und Anker aus `player-sword.import.txt`. Einige Frames ragen aus ihren
32×32-Zellen heraus, beispielsweise Frame 68 mit 49 Pixeln Höhe. Diese Frames
dürfen nicht als regelmäßiges Raster geschnitten werden. Die ursprünglichen
Zellmitten-Anker werden auf den Fußpunkt des aktuellen Spieler-Prefabs umgerechnet.

1. Den Baum annähern, ohne ihn zu berühren: kein Schaden. Beim Laufen darf der Idle-Frame nicht auftauchen.
2. Eine Berührung zulassen: ein halbes Herz weniger, keine Trefferflut.
3. In seine Richtung blicken und angreifen; Schläge hinter der Figur oder durch Hindernisse dürfen nicht treffen.
4. Gegner besiegen, am Buch speichern und Gebiet wechseln. Nach Rückkehr und nach Laden muss er verschwunden bleiben.
5. Übungsschwert in der Haupthand anlegen: Schwert und Körperpose müssen synchron laufen. Ein Treffer stößt den Log zurück; währenddessen darf er keinen Kontaktschaden verursachen.

`SecretsReborn → Combat → Set up oak melee enemy` erstellt den Import, das Prefab und die erste Instanz einmalig. Bestehende Wald-Tiles und manuelle Änderungen werden nicht neu aufgebaut. Zukünftiger Netzwerk-Koop muss Senderbesitz, replizierte Gegner und Aktionen an die Host-Einstiege anbinden; das ist noch keine Netzwerk-Implementierung.
