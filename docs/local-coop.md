# Lokaler Koop: erste Netzwerkanbindung

Netcode for GameObjects 2.7.0 und Unity Transport 2.6.0 tragen die lokale Host-/Client-Verbindung. Noch keine Unity-Services-, Relay- oder Lobby-Abhängigkeit. Die Verbindung nutzt UDP-Port 7777 und maximal vier aktive Charaktere einschließlich Host.

## Starten und testen

1. Play beenden, Assets → Refresh. `SecretsReborn → Coop → Prepare local test` erzeugt bei Bedarf die Resources-Prefab-Variante des bestehenden Players. Die handbearbeitete Szene wird nicht neu aufgebaut.
2. Eigene Szenenänderungen speichern. `SecretsReborn → Coop → Build local Windows test player` erzeugt `Builds/LocalCoop/SecretsReborn.exe`. Der Build beginnt im Waldheiligtum, die Höhle ist ebenfalls enthalten. Builds bleiben von Git ausgeschlossen.
3. Waldheiligtum-Editable im Editor öffnen, Play starten. Über `Koop-Test · F6` → `Host starten`.
4. Test-Build separat öffnen. Im gleichen Gebiet `Koop-Test · F6` → `Beitreten`, IP `127.0.0.1`. Für zwei Rechner im LAN die IP des Hosts verwenden.
5. Jedes Fenster folgt seinem eigenen Charakter. Bewegung, Inventar und Ausrüstung sind getrennt. Andere Charaktere werden ohne lokale Eingabe dargestellt.
6. Einen Charakter durch den Gegner sterben lassen. Kein Game Over, solange der andere lebt. Der lebende Spieler hält E / Controller A drei Sekunden neben dem gefallenen Mitspieler. Beide tot: Game Over in beiden Fenstern.
7. `Sitzung verlassen` beendet die Verbindung und startet die aktuelle Solo-Szene neu. Dabei wird nicht automatisch gespeichert. Beim Verlassen eines Gasts bleibt dessen Zustand im laufenden Host-Weltzustand, aber er zählt nicht mehr zur aktiven Gruppe.

Die Editor-Test-Begleiter müssen vor dem Netzwerkstart entfernt werden. Eingaben bei Fokusverlust werden als null gesendet; der Host läuft im Hintergrund weiter. Bei ausbleibenden Eingaben stoppt er den Gast nach 0,35 Sekunden und bricht Revive ab.

## Zuständigkeit und Identitäten

- Der Host berechnet Bewegung inklusive 2D-Kollisionen, HP/Mana, Schaden, Inventartransaktionen und Revive. Clients senden keine HP-, Schaden- oder Positionswerte, sondern Eingaben und Aktionen.
- Eine Verbindung ist auf dem Host einer Charakter-ID zugeordnet. Befehle enthalten keine frei wählbare handelnde Charakter-ID. Sequenzen, begrenzte Eingaben, Slotgrenzen und Anfragenraten werden geprüft.
- Gastsysteme speichern einen lokalen Charakter-Token in PlayerPrefs. Der Host verwendet `coop-<Token>` als Schlüssel innerhalb seiner eigenen Welt. Ein anderer Host hat einen anderen Weltzustand. Gleichzeitige doppelte Tokens und abweichende Szene/Protokoll werden abgelehnt. Der Token ist im LAN-Prototyp keine authentifizierte Account-Identität; eine spätere Internet-Sitzung bindet ihn an die echte Spieleridentität.
- Aktive Gruppenmitglieder sind von historischen Charakteren im Host-Speicherstand getrennt. Beim Rejoin derselben laufenden Welt werden Inventar/Vitals wiederverwendet; eine gespeicherte freie Position im aktuellen Gebiet wird bevorzugt.
- Clients übernehmen Snapshots für Inventar, Aussehen, Vitals, Lampen, aktive Gruppe und Game Over. Sie speichern keine lokale Kopie als Host-Savegame.

Die Anbindung verwendet NGO Named Custom Messages statt transportabhängiger IDs in Gameplay-Daten. Der Host sendet zunächst komplette Welt-Snapshots und Posen mit 10 Hz; Eingaben kommen mit 20 Hz. Der Client interpoliert Spieler- und Gegnerpositionen pro Bild über 100 ms; die Kamera folgt der geglätteten Position. Er extrapoliert nicht über bestätigte Host-Positionen hinaus. Teleports über 2,5 Einheiten, Lücken über 0,5 Sekunden sowie Tod/Pause setzen die Darstellung unmittelbar auf den neuen Host-Zustand. Vorschlag für später: Delta-Replikation und lokale Vorhersage für weichere Bewegung bei höherer Latenz.

## Grenzen dieses Schritts

Gemeinsame Gebietswechsel sind seit dem 7. Oktober 2026 angebunden und mit zwei Prozessen geprüft; siehe [Multiplayer-Gebietswechsel](multiplayer-area-transitions.md). Netzwerkprotokoll ist jetzt Version 5: Host und Client müssen den neuen Stand verwenden. Gebietswechsel und Host-Savegame-Laden erfordern jetzt die ausdrückliche Zustimmung aller verbundenen Spieler; siehe [Abfrage und Laden](multiplayer-votes-and-load.md). Der Host kann am Buch den Weltzustand inklusive aller vorhandenen Charaktere speichern. Gemeinsamer Retry nach Game Over ist ebenfalls angebunden; siehe [Gruppen-Retry](party-retry.md).

Gegnerbewegung, Angriffe, Pickups, geöffnete Truhen und deren Weltzustand werden ebenfalls über den Host vermittelt, damit sie im Test nicht unabhängig auseinanderlaufen. Die Truhenbelohnung erscheint beim Empfänger, einschließlich Clients. Nur dieser Charakter ist bis A / X / Enter geschützt und unbeweglich; die Welt läuft weiter. Siehe [Truhenbelohnungen](chest-rewards.md). Runenkreise können jetzt von jedem lebenden, handlungsfähigen Gast mit eingeschalteter magischer Lampe aktiviert werden. Die Zeichenanzeige folgt der eigenen Lampe des jeweiligen Fensters.

## Verifikation

`tests/CoopProtocolChecks.cs` prüft Handshake, ungültige Identitäten/Versionen/Szenen, nicht endliche oder übergroße Bewegung, Slotgrenzen und ungültige Aktionen.

Der Development-Build enthält einen optionalen Zwei-Prozess-Test (`--coop-smoke-host` / `--coop-smoke-client`, `--coop-smoke-report <Pfad>`). Er prüft echten Transport, vom Host simulierte Client-Bewegung, getrenntes Equipment, Einzeltod ohne Gruppen-Game-Over, vom Client gehaltenes Revive und synchronisiertes Gruppen-Game-Over. Normale Starts fügen den Testtreiber nicht hinzu. Testberichte liegen in Temp; sie verändern keine Szene oder gespeicherte Spielstände.

Prüfstand 06.10.2026: Runtime-/Editor-Kompilierung, Protokoll-/Inventar-/Kampf-/Weltzustandstests bestanden. Native Windows-Build erfolgreich (0 Fehler; eine Warnung des installierten Unity-Pipeline-Pakets über fehlende optionale Runtime-Konfiguration). Der vollständige Zwei-Prozess-Test besteht auf Host und Client (`Temp/CoopSmooth-Host.txt`, `Temp/CoopSmooth-Client.txt`), einschließlich Abmeldung aus der aktiven Gruppe und Beibehaltung der Gastausrüstung im Host-Weltzustand. Die Testprozesse sind beendet. Vier gleichzeitige Netzwerkprozesse und Verbindungen zwischen verschiedenen Rechnern sind noch nicht interaktiv geprüft.

Regression: Der JSON-Netzwerkweg normalisiert ausschließlich leere Slots, die JsonUtility als leere Objekte/Strings zurückliefert. Die strikte Validierung von belegten Slots und von gespeicherten Spielständen wird nicht gelockert. Dieser Fall ist zusätzlich in den Protokolltests enthalten. `tests/ReplicaMotionChecks.cs` prüft kontinuierliche Darstellung zwischen Paketen, verspätete/geballte Pakete, keine Extrapolation und die Rücksetzung bei Teleport, Verbindungslücke und Tod/Pause.

Offizielle Grundlagen: [NGO NetworkManager](https://docs.unity3d.com/Packages/com.unity.netcode.gameobjects@2.7/manual/components/core/networkmanager.html), [Named Custom Messages](https://docs.unity3d.com/Packages/com.unity.netcode.gameobjects@2.7/manual/advanced-topics/message-system/custom-messages.html).
