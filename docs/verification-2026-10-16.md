# Prüfstand 16. Oktober 2026 – einheitliche Spieloberflächen

Die [einheitliche Oberfläche](unified-game-ui.md) ist in Runtime und Editor kompiliert. Windows-Build `Builds/Windows-UnifiedUI-20261016/SecretsReborn.exe`: **Succeeded, 0 Fehler, 1 bekannte Pipeline-Warnung** (optionale RuntimePipelineConfig fehlt).

## Darstellung und Datenbindungen

Inventar und Shared Stash verwenden transparente Flächen, schmale Goldrahmen, schlichte Slots und eigene ruhige Beschreibungsflächen. Der Safe-Area-Fit hält Randabstände ein. Auswahl bleibt gold, Seltenheit behält ihre Farbe. Native Prefabprüfung bestätigt **137 Slotkomponenten mit 137 gültigen Qualitätsrahmenreferenzen**. Slotindizes, Itembilder, Equipment-Symbole und Buttons bleiben erhalten. Der neue Abstimmungs-Canvas besitzt vier editierbare Teilnehmerzeilen. Speicherbuch und Zauberbeschreibung verwenden dieselben Farben und Rahmen. Namen, Öffnen-Hinweise und geschlossene Debuganzeigen überdecken geöffnete Oberflächen nicht.

Screenshots von Inventar, Ausrüstung, Lagertransfer, Zauberbeschreibung/Zauberbuch und Zustimmung wurden visuell geprüft. Die Aufnahmeroutine rendert die Welt getrennt und anschließend das tatsächliche Canvas davor; damit entsprechen Transparenz und Textüberdeckung einem Overlay, statt Welt-Sprite-Sortierungen versehentlich über die Oberfläche zu zeichnen.

## Native Prüfungen

Finale Zwei-Prozess-Prüfung: `Temp/UnifiedUI-Verified-20261016-115746/IsolatedFinal/host.txt` und `client.txt`, beide PASS. Host 1920×1080, Client 2560×1440. Screenshots liegen daneben. Geprüft:

- transparente Inventar-/Lager-/Zauberflächen und schlichte Slotrahmen;
- clientseitiges Anlegen/Ablegen von Ausrüstung und bestätigte Hostzustände;
- Ein-/Auslagern über die bestehenden UI-Aktionen;
- Rang-3-Feuerballbeschreibung einschließlich skalierter Burn-Angaben;
- vollständige benannte Teilnehmerliste und ein grüner, hostbestätigter Haken auf beiden Rechnern, während die andere Zustimmung fehlt;
- kein vorzeitiges Laden, Ablehnen erhält die aktuelle Welt, neue Abstimmung löscht alte Haken;
- erst einstimmige Zustimmung stellt den vorbereiteten Weltzustand wieder her.

Bestehende Speicherbuch-Prüfung: `Temp/UnifiedUI-Verified-20261016-115746/BookRegression/host.txt` und `client.txt`, beide PASS. Gast bedient gemeinsames Buch, Beobachter kann nicht steuern, alle drei Slots werden nur hostseitig gespeichert, Teilnehmer/Zeitstempel/Hostanker bleiben korrekt, gemeinsames Laden gelingt und Abmeldung gibt das Buch frei. Testdateien ausschließlich unter isolierten Temp-Pfaden.

Hauptmenü-Regression bei 3440×1440: `Temp/UnifiedUI-Verified-20261016-115746/Menu/result.txt`, PASS einschließlich Maus/Tastatur/Controller, Animation, allen Unterseiten ohne Scrollen und isoliertem Papierkorb-Roundtrip.

Bestehende `tests/CoopProtocolChecks.cs` mit den echten Datenklassen über `Temp/ProtocolChecks/ProtocolChecks.csproj`: PASS für Versions-/Szenenhandshake, Identität, begrenzte Bewegung, Slots/Aktionen und strikte Behandlung belegter Slots. Der DTO-Ausschnitt stammt unverändert aus SpellCaster.cs; keine Unity-API-Stubs für die geprüfte Logik. `git diff --check` für Skripte, Tests und Dokumentation besteht.

## Protokoll und Testisolation

Abstimmungsstatus wird mit Abstimmungs-ID über den bestehenden Hostkanal verteilt. Clients akzeptieren Status nur vom Host; Teilnehmerzahl, Namen und eindeutige IDs sind begrenzt. Netzwerkprotokoll **27**, NGO-Kompatibilität **17**: für Multiplayer überall denselben neuen Build verwenden. Spielstandformat unverändert.

Bei den ersten UI-Testläufen war der Rejoin-Schlüssel für 127.0.0.1 nicht isoliert; ein zuvor vorhandener lokaler Wiederbeitritts-Merker konnte dadurch ersetzt werden. Spielstände und Rejoin-Schlüssel anderer Host-Adressen wurden nicht verändert. Game-UI- und Lobby-UI-Testrollen benutzen jetzt eigene Namensräume. Der finale Durchlauf bestätigt die Trennung: Anzahl eigener GameUI-Testmerker stieg von 0 auf 1; keine Tokenwerte ausgegeben. Ein alter lokaler Spieler kann seine vorhandene Figur bei Bedarf neu auswählen.

Abschließend: keine Compilerfehler, Editor außerhalb von Play, MainMenu-Szene gespeichert und ohne offene Änderungen. Testprozesse beendet. Keine Script-Exceptions in den erfolgreichen Durchläufen. Bekannte D3D12-/ComputeBuffer-Cleanuphinweise der versteckten Testspieler bleiben. Echte Vier-PC-/4K-Tests bleiben ausstehend; native Prüfungen verwendeten zwei lokale Prozesse. Vorhandene Änderungen erhalten; kein Commit oder Push. Als nächster Inhaltsschritt bleiben nach den Menüwünschen eine Bleed- und eine Poison-Waffe vorgemerkt.

## Nachbesserung: deckendes Inventar und Lager (16.10.2026)

Die erste transparente Gestaltung wurde vom Nutzer verworfen. Als Referenz wurde das Inventar des älteren chameleonkid/Secrets-Repositories (HEAD 99f5b1c15fa6201e84044fbd81f9397d4f9709a2) geprüft. Zwei neue deckende Schiefer-/Goldgrafiken sind in InventoryCanvas.prefab eingebunden; sämtliche Slots, Texte und Aktionen bleiben native editierbare Objekte. Quellen und vollständige Imagegen-Prompts: inventory-art-prompts.md.

Zwei Darstellungsfehler behoben:
- QuietMenuFrameGraphic, ConsentMarkGraphic und MenuFrameGraphic fehlte teilweise der CanvasRenderer. RequireComponent ergänzt und vorhandene Resources-Prefabs sowie die zuvor saubere offene MainMenu-Szene repariert. Keine fehlenden UI-Renderer mehr.
- ConsumableRingSetup deaktivierte irrtümlich auch gültige Lagerindizes 55–79. Die Regel betrifft jetzt ausschließlich die alten Inventar-Trankplätze; alle 80 Lageransicht-Slots (40 + 40) sind aktiv.

Die erste Build-Prüfung deckte zusätzlich die bisher globale Default-Texturimportregel auf; SlateGold-Backplates werden nun gezielt als Sprites importiert. Die zwei Zwischenläufe waren FEHLGESCHLAGEN und gelten nicht als Nachweis. Die vorherigen Tests prüften nur aktive Komponenten, keine sichtbare Auswahl; das war unzureichend.

Finaler Windows-Build: Builds/Windows-SlateInventory-20261016/SecretsReborn.exe. Build erfolgreich, 0 Fehler, 3 bestehende Warnungen: optionale Pipeline-Runtimekonfiguration fehlt; zweimal Easy Save 3 Light.cookieSize veraltet. Keine Plugin-Dateien dafür geändert.

Finale Prüfordner: Temp/SlateUi-20261016-123640. host.txt (1920x1080) und client.txt (2560x1440) beide PASS. Gerenderte Auswahl bei Inventarindex 0, Equipmentindex 40, letztem Rucksackplatz 39, linkem Lagerindex 0, rechtem Lagerindex 79 sowie Transferauswahl 40 sichtbar. Pixelprüfung: 1008–1080 goldene Pixel auf Host, 2028–2160 auf Client. Jeweils genau ein aktiver Auswahlrahmen. 80 sichtbare Lageransicht-Slots geprüft. Anlegen/Ablegen, Ein-/Auslagern, Burn-Beschreibung, replizierte Zustimmung, Abbruch, Zurücksetzen der Haken und einstimmiges Laden bestanden. Native Bildschirmaufnahmen von Inventar, beiden Lagerhälften und grünem Zustimmungs-Haken zusätzlich betrachtet. Testdaten und Rejoin-Merker bleiben isoliert.

Hauptmenü-Regression: MainMenu/result.txt im selben Prüfordner, PASS bei 3440x1440 einschließlich zehn Unterseiten, keinen Scrollcontainern, Maus/Tastatur/Controller, Buttonpuls, Figureneditor und isoliertem Papierkorb-Roundtrip. Keine Script-Exceptions in den erfolgreichen Spielprüfungen.

Editor abschließend außerhalb von Play, keine Compilerfehler, aktive Szene gespeichert/sauber. Vorhandene Änderungen erhalten, kein Commit/Push. Optische Nutzerabnahme bleibt offen.
