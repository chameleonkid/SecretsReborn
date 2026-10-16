# Prüfstand vom 15. Oktober 2026

## Aktualisierung: Hauptmenü direkt in der Szenen-Hierarchy

Aktueller Build: `Builds/Windows-SceneMenu-20261015-115826/SecretsReborn.exe`.
0 Fehler, 1 bekannte Pipelinewarnung; Protokoll 26 und Saveformat 18 unverändert.
`MainMenuHomeCanvas` und `EventSystem` sind jetzt in `Assets/Scenes/MainMenu.unity`
gespeichert. Der Controller hat eine serialisierte Home-Referenz. Hauptseite und
EventSystem werden nicht mehr zur Laufzeit erzeugt.

Alle UI-Elemente sind außerhalb von Play in der Hierarchy editierbar. Button-
Animationswerte sind im Inspector verfügbar; eigene Grundskalierung, Image-Farbe,
Interaktivität und Button-Navigation werden beim Start nicht zurückgesetzt.
Szenen-Setup-Bericht: `Temp/MainMenuSceneReport.txt`.

Nativer Menütest besteht: `Temp/SceneMenuTest-20261015-115938/result.txt`.
Er prüft zusätzlich genau eine vorhandene Szeneninstanz, die feste Controller-
Verknüpfung und genau ein EventSystem. Bestehende Controller-, Tastatur-, Maus-,
Fokuswechsel- und Animationstests bestehen; keine Script-Exceptions/Testfehler.
Der Testprozess ist beendet. [Bearbeitung in der Hierarchy](main-menu.md).

## Aktualisierung: ruhiges Hauptmenü und grafische Buttons

Aktueller Build: `Builds/Windows-QuietMenu-20261015-114532/SecretsReborn.exe`.
0 Fehler, 1 bekannte Pipelinewarnung; Protokoll 26 und Saveformat 18 unverändert.
Neuer ruhiger Wald-/See-Hintergrund, vorhandener Rahmen mit schlichter transparenter
Innenfläche sowie vier echte Bildbuttons mit eingearbeiteten bisherigen Beschriftungen.
Editierbares Prefab: `Assets/Resources/MenuUI/QuietHome/HomeCanvas.prefab`.

Native opt-in Menütests bestehen in 1040×600 und 1280×720:
`Temp/QuietMenuFinal-20261015-114642/1040x600/result.txt` und
`Temp/QuietMenuFinal-20261015-114642/1280x720/result.txt`.

- Virtueller Controller navigiert per D-Pad und bestätigt mit der unteren Taste;
  die rechte Taste führt aus einer Unterseite zurück.
- Tastatur navigiert und bestätigt; Escape kehrt zurück. Der direkte Wechsel
  zwischen Tastaturfokus und Maus-Hover wird ebenfalls geprüft.
- Maus-Raycasts treffen die Grafikbuttons; Hover/Klick, weiches Vergrößern und
  Verkleinern sowie der einmalige Bestätigungspuls bestehen.
- Aktion erfolgt nach dem 0,32-Sekunden-Puls. Währenddessen werden keine weiteren
  Aktionen gestartet. Hauptmenübuttons enthalten keine Text-Overlay-Komponenten.
- `Home.png` und `ButtonPulse.png` wurden visuell geprüft; Rahmen, Grafiklabels
  und halbtransparente Innenfläche sind vollständig sichtbar.
- Beide finalen Prozesse sind beendet, keine Script-Exceptions/Testfehler.
  Der Test greift nicht auf Save-Slots zu und erzeugt keine Spielstände.

[Menüaufbau und Authoring](main-menu.md). Weitere Menüwünsche haben Vorrang;
danach als nächster Inhaltsschritt eine Bleed- und eine Poison-Waffe erstellen.
Dieser Menü-Arbeitsstand ist noch nicht committet oder gepusht.

## Aktualisierung: DoT-Angaben an Waffen und Zaubern

Aktueller Build: `Builds/Windows-DotInfo-20261015-104526/SecretsReborn.exe`.
0 Fehler, 1 bekannte Pipelinewarnung; Protokoll 26 und Saveformat 18 unverändert.
Waffenbeschreibungen und Ringmenü/Zauberbuch zeigen jeden konfigurierten gültigen
DoT mit Typ, Schaden, Tick-Abstand, Dauer und Chance. Zauber zeigen ihren aktuellen Rang.
Der Ring-Infobereich wächst bei längeren Texten, Inventar-/Stash-Details passen ihre
Schrift innerhalb der vorhandenen Fläche an.

Nativer Host-/Client-Test besteht:
`Temp/DotInfoIntegration-20261015-104553/host.txt` und `client.txt`.
Der Client prüft die tatsächliche Rang-3-Burn-Beschreibung und die verfügbare Texthöhe.
Ein nur im Testprozess erzeugtes Waffenprofil prüft gleichzeitig Burn, Poison und
Bleed einschließlich 0,5-Sekunden-Ticks und unterschiedlichen Chancen im echten
Inventar-Detailfeld. Es wird danach zurückgesetzt; keine Itemassets werden verändert.
`SpellTargetRing.png` und `WeaponDotDetails.png` im Testordner wurden visuell geprüft:
alle Effektzeilen sind lesbar und vollständig. Die bisherigen DoT-/Rang-/Koop- und
Inventarregressionen bestehen. Beide Prozesse beendet, keine Script-Exceptions/Testfehler.

## Aktualisierung: DoTs pro Zauberrang

Aktueller Build: `Builds/Windows-DotRanks-20261015-103719/SecretsReborn.exe`.
0 Fehler, 1 bekannte Pipelinewarnung; Protokoll 26, Saveformat 18.
Die On-Hit-DoT-Liste liegt jetzt in jedem `SpellRankDefinition` statt global am
Zauberasset. Fireball-Burn tickt auf Rang 1 / 2 / 3 mit 1 / 2 / 3 Schaden.
Dauer, Abstand und Auslösechance sind ebenfalls pro Rang editierbar.
Das Editor-Foundation-Setup erzeugt diese Werte auch für neue Fireball-Assets.

Timerprüfungen und echter Host-/Client-Test bestehen:
`Temp/DotRankIntegration-20261015-103748/host.txt` und `client.txt`.
Der Host prüft die drei importierten Rangkonfigurationen mit 3 / 6 / 9 Gesamtschaden.
Ein echter Rang-3-Fireball mit garantierten direkten Crits verursacht an der Puppe
24 direkten Schaden und anschließend genau 9 normale Burn-Schadenspunkte:
HP 1.000.000 → 999.976 → 999.967. Drei gleichzeitig applizierte Typen verursachen
danach wie zuvor zwölf zusätzliche Punkte (999.955 HP). Bestehende Regressionen
bestehen; beide Prozesse sind beendet, keine Script-Exceptions/Testfehler.

## Erster DoT-Ausbau vor Rangskalierung

Ursprünglicher Development-Build:
`Builds/Windows-Dot-20261015-102322/SecretsReborn.exe`.
Unity 6000.3.25f1, 0 Fehler, 1 bekannte Warnung aus `com.unity.pipeline`
wegen fehlender optionaler RuntimePipelineConfig. Netzwerkprotokoll **26**,
Saveformat unverändert **18**. Auf allen Test-PCs denselben vollständigen Build verwenden.

## DoT-Nachweise

- `tests/DotChecks.cs` besteht: unabhängige Typen/Timer, Auffrischung ohne Stapel
  oder Tickverschiebung, Wertkopien, Chancengrenzen, ungültige Werte, Aufholen,
  letzter Tick bei Ablauf, 1.200 Ticks über 120 Sekunden und Clear während eines Ticks.
- Spiel- und Editorassembly kompilieren im laufenden Editor; nativer Windows-Build besteht.
- Zwei echte lokale Spielprozesse bestehen den erweiterten `EconomyIntegrationDriver`:
  `Temp/DotIntegration-20261015-102355/host.txt` und `client.txt`.
- Client schaltet 100 % Crits ein und trifft die Trainingspuppe mit Rang-3-Fireball.
  Zwei direkte Crit-Treffer mit 0,265 Sekunden Abstand verursachen insgesamt 24 Schaden.
  Ein aufgefrischter Burn verursacht danach genau drei normale Ticks à 1 Schaden:
  HP 1.000.000 → 999.976 → 999.973. Die Crit-Einstellung verdoppelt keine DoT-Ticks.
- Danach wirken Burn, Poison und Bleed gleichzeitig, jeweils 2 Sekunden mit
  0,5 Sekunden Tick-Abstand: genau zwölf weitere Schadenspunkte, HP 999.961.
  Client prüft drei aktive Effekttypen, die Darstellung, HP und gemeinsames Ablaufen.
- `AllDotsHost.png` und `AllDotsClient.png` im Testordner wurden visuell geprüft:
  orange Flammenlinien, grüne Blasen und rote Blutstriche sind am Ziel sichtbar.
- Der generische Einstieg verursacht beim gerüsteten Spieler korrekt mitigierten
  Schaden. Ein Bleed-Tick tötet einen normalen Gegner, trägt seinen besiegten Zustand
  ein und räumt den Effekt auf. Session-Refresh setzt die Puppe auf volle HP und
  entfernt laufende Effekte.
- Direkte Clientversuche, DoTs anzuwenden, werden abgelehnt. Nur der Host tickt.
- Bestehende Inventar-, Trankring-, Shared-Stash-, ES3-, Crit-, Fireballflug-, Mana-
  und Healprüfungen bestehen weiterhin. Beide Prozesse sind beendet; keine
  Script-Exceptions oder Testfehler. Bekannte D3D12-Start-/Cleanuphinweise bleiben.

## Noch manuell prüfen

Effekte im normalen Spiel bei mehreren bewegten Gegnern beurteilen; Zwei-PC-Test
steht aus. Lade-/Gebietswechselpfade entfernen Effekte im Code, wurden in diesem
Test nicht als eigener Ablauf durchgespielt. Waffen-On-Hit-Anbindung ist vorhanden,
aber noch keiner regulären Waffe standardmäßig zugewiesen und in diesem nativen
Durchlauf nicht per Waffenschlag getestet. Fireball-Burn ist aktiv.

[DoT-Regeln und Authoring](damage-over-time.md). Bestehende Änderungen erhalten;
kein Commit und kein Push.

## Responsive Hauptmenü und native Unterseiten (15. Oktober, abends)

Die Szene enthält HomeCanvas mit proportionalem Rahmen, Safe-Area-Fit und zehn editierbaren Unterseiten. Die Instanz ist vom ursprünglichen HomeCanvas-Prefab gelöst; Unity erlaubt sonst kein Verschieben der vorhandenen Prefab-Kinder. Gemeinsame Grafikbuttons, Formulare, Porträt, Save-Slots, Papierkorb und Bestätigung sind in der Hierarchy angelegt. Details: [Menü-Authoring](main-menu.md).

Windows-Build: `Builds/Windows-ResponsiveMenu-Verified/SecretsReborn.exe`, erfolgreich mit 0 Fehlern und einer bekannten Pipeline-Warnung wegen fehlender optionaler RuntimePipelineConfig. Native Reports: `Temp/VerifiedMenu-1920/result.txt`, `Temp/VerifiedMenu-3440/result.txt` und `Temp/ResponsiveMenuRetry/result.txt`. Full HD 1920×1080 und Ultrawide 3440×1440 bestehen. Ein angeforderter 3840×2160-Fenstertest wurde vom lokalen Desktop auf 3840×1440 begrenzt; echtes 4K ist damit noch nicht separat verifiziert. Canvas-Skalierung und proportionaler Bildaufbau unterstützen höhere Auflösungen.

Geprüft: Controller/Tastatur/Maus, grafische Beschriftungen, Fokus/Puls, Ausblenden der Home-Buttons auf Unterseiten, Creator-Dropdown und Datenbindung, Scrollfokus, Randgrenzen, geometrisch unverzerrte Rahmenabschnitte, isoliertes Löschen/Abbrechen/Papierkorb/Wiederherstellen und ungültige IP. Screenshots der korrigierten Unterseiten visuell geprüft. Wiederholte Host-/Join-Abbrüche und Save-Roundtrip mit Founder/Backup/Belegtschutz bestehen. Testprozesse beendet. Produktive Spielstände bleiben unberührt. Separate Lobby und Speicherbuch behalten ihren bisherigen UI-Aufbau. Keine Spielmechaniken geändert, kein Commit oder Push.

Abschließender Scroll-Fix: Automatisches Nachscrollen reagiert auf einen Wechsel der Auswahl und lässt manuelles Scrollen bei unveränderter Auswahl zu. Der finale Build besteht einschließlich dieser ausdrücklichen Prüfung bei 1920×1080 und 2560×1440: `Temp/FinalScrollMenu-1920/result.txt` und `Temp/FinalScrollMenu-2560/result.txt`. Temporäre Build-/Reloadhelfer entfernt. Bekannte D3D12-/ComputeBuffer-Cleanuphinweise beim Beenden der versteckten Testspieler bleiben bestehen.

## Hauptmenü ohne Scrollen und Canvas-Multiplayer-Lobby (15. Oktober, final)

Der aktuelle Stand ersetzt die zuvor scrollenden Layouts. Alle zehn Unterseiten sind vollständig sichtbar: zweispaltige Charaktererstellung mit fünf Inline-Auswahlfeldern, drei Spielstandkarten nebeneinander und vier Papierkorb-Karten pro Seite. Ein neuer geometrischer Goldrahmen mit schmalen Linien/Eckakzenten ersetzt den Waldrahmen. Canvas-Referenz 1280 × 800, Expand und proportionaler Safe-Area-Fit. Native Szene bestätigt: zehn Seiten, vier Lobbykarten, zwei QuietMenuFrameGraphic, **null ScrollRect**, gespeichert, außerhalb von Play, keine Compilerfehler.

Die Multiplayer-Lobby steht mit Figurenwahl, Besitzer-/Bereitschaftsstatus, ausgerüsteten Porträts, Charaktererstellung, Löschbestätigung und vier festen Lobbyaktionen direkt in MainMenu.unity. Neue Grafikbuttons: `LobbyButtons.png` mit 16 Sprites. Nicht verfügbare Aktionen werden gedimmt. Zurück fokussiert in der Lobby den Verlassen-Button; Verlassen braucht dessen ausdrückliche Bestätigung. Wechsel von Maus zu Controller-Zurück ist geprüft. Bestehende Netzwerk-/ES3-Regeln bleiben bestehen. Eine fehlende Canvas-Lobby nutzt die frühere IMGUI-Ansicht als Rückfall.

Finaler Windows-Build: `Builds/Windows-SpaciousMenu-20261015/SecretsReborn.exe`, **Succeeded, 0 Fehler, 1 bekannte Pipeline-Warnung** (optionale RuntimePipelineConfig fehlt).

Erfolgreiche native Prüfungen:

- Full HD 1920×1080: `Temp/SpaciousMenu-20261015-195347/FullHD/result.txt`.
- Finaler Build bei 2560×1440 und 3440×1440: `Temp/SpaciousMenu-Final-20261015-195837/QHD/result.txt` und `Ultrawide/result.txt`.
- Zwei echte verbundene Spielprozesse: `Temp/SpaciousMenu-Final-20261015-195837/host.txt` und `client.txt`. Host 1080p, Client 1440p. Vorheriger erfolgreicher Durchlauf zusätzlich mit Client 1366×768.
- Erneutes Host/Join und mehrfaches Verlassen/Wiederöffnen: `Temp/SpaciousMenu-Final-20261015-195837/Retry/result.txt`.

Die Menüs bestehen Maus-, Tastatur- und Controllereingaben, Fokus/Puls, Inline-Auswahl und Datenbindung, Bildschirmgrenzen und überschneidungsfreie Bedienelemente. Save-/Papierkorbprüfung nutzt isolierte TestSlots. Die Lobbyprüfung erstellt eine nicht gespeicherte Welt, füllt alle vier Figurenplätze, erstellt lokale und entfernte Figuren, prüft Abbrechen, fremden Besitz, Gründer-Schutz, Löschen/Abbrechen/Bestätigen, Bereitschaft und gemeinsamen Start bis in die Abenteuer-Szene. Controller-Bestätigung öffnet den Creator; Zurück nach Mausbedienung fokussiert sichtbar den Verlassen-Button und trennt nicht sofort.

Screenshots von Charaktererstellung, Spielstandkarten, vier Lobbyfiguren, Client-Creator und Löschbestätigung visuell geprüft. Testprozesse beendet. Keine Skript-Exceptions oder verlorenen Nutzerdaten festgestellt. Bekannte D3D12-/ComputeBuffer-Hinweise beim Beenden versteckter Testspieler bleiben. Echtes 4K war wegen der lokalen Fenster-/Desktopbegrenzung weiterhin kein separater nativer Test. Kein Commit und kein Push. Nächster Inhaltsschritt nach Abschluss der Menüwünsche bleibt: Bleed- und Poison-Waffe.
