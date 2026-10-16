# Hauptmenü und Multiplayer-Lobby im Unity-Editor

`Assets/Scenes/MainMenu.unity` enthält die vollständige Oberfläche als native Canvas-Hierarchie unter `HomeCanvas` mit einem EventSystem. Alle Seiten, Figurenkarten, Vorschauen und Aktionsbuttons sind außerhalb von Play editierbar. Die Szene ist vom ursprünglichen HomeCanvas-Prefab gelöst. Beim Start wird kein vollständiges Menü-Prefab instanziiert.

## Seiten ohne Scrollen

Alle Seiten passen vollständig in den sichtbaren Rahmen. Die Rahmenmaße sind je Seite im Main Menu Page View hinterlegt. Charaktererstellung nutzt Vorschau links und Name plus fünf Aussehensfelder rechts. Werte wechseln mit den Pfeilen, per Controller links/rechts oder durch Bestätigen. Es gibt keine aufklappenden Listen.

Die drei Spielstände stehen nebeneinander. Der Papierkorb zeigt vier Karten auf einer Seite; Weiter/Zurück erschließen beliebig viele Einträge. Es gibt in der gesamten Hauptmenü-/Lobby-Hierarchie keine ScrollRect-Komponenten.

## Hierarchy bearbeiten

Unter `HomeCanvas → Safe area and proportional fit → Menu frame` liegen:

- `Plain translucent inlay`: schlichter Hintergrund; Transparenz über Image/Color/Alpha.
- `Quiet gold frame`: schmale Goldlinien, Eckakzente und kleine Rauten. Quiet Menu Frame Graphic zeichnet diese geometrisch, ohne eine Rahmen-Textur zu strecken. Farbe, Linienstärke und Ecklänge sind im Inspector einstellbar.
- `Game title`, `Connection message` und `Pages`.
- `Pages/Home`: vier Hauptmenübuttons.
- Zehn Unterseiten: Solo, SoloCreate, Multiplayer, Host, Join, SoloLoad, HostLoad, Options, Trash und ConfirmDelete.
- In einer Unterseite: `Page content → Layout → Controls` mit den einzelnen Formularen/Karten. Bestätigen und Zurück liegen fest im Fußbereich.

Zur Vorschau außerhalb von Play Home deaktivieren und genau eine andere Seitengruppe aktivieren. Die Panel Size dieser Seite auf Menu frame/Rect Transform übertragen. Beim Start wählt MainMenuPagesView die passende Größe automatisch. Eigene Seitenmaße und Positionen werden in der Szene gespeichert.

`HomeCanvas → Lobby safe area and proportional fit → Multiplayer lobby` enthält den gleichen Rahmen und drei native Ansichten: `Character selection`, `Lobby character creator`, `Lobby delete confirmation`. Für eine Editorvorschau den Lobbyrahmen und eine Ansicht aktivieren; den Hauptmenürahmen ausblenden. Zur Laufzeit steuert LobbyCanvasView deren Sichtbarkeit.

Charakterkarten enthalten Name, sechs Porträt-Layer, Besitz-/Bereitschaftsstatus, Gründerhinweis und Grafikbuttons. Vier Karten und vier Lobbyaktionen sind gleichzeitig sichtbar. Statische Aktionen besitzen persistente On Click-Verbindungen zu LobbyMenuAction. Charaktererstellung und Löschbestätigung sind eigene vollständige Ansichten.

Position/Größe über Rect Transform, Bilder über Image bearbeiten. Die Schrift der Aktionsbuttons ist in den Grafiken enthalten. Menu Graphic Button steuert sanftes Wachsen auf 105,5 %, Schrumpfen und einen zusätzlichen Bestätigungspuls von 8 % über 0,32 Sekunden. Die Aktion startet nach dem Puls. Nicht verfügbare Buttons sind gedimmt. Individuelle Grundskalierungen, Farben und Button/Navigation bleiben erhalten.

## Auflösungen und Bedienung

Canvas Scaler: Scale With Screen Size, Referenz 1280 × 800, Expand. Menü, Schrift und Abstände skalieren einheitlich. Breitere Seitenverhältnisse zeigen mehr Hintergrund. MenuViewportFit berücksichtigt die Safe Area und verkleinert bei Bedarf den gesamten Rahmen proportional. Der Hintergrund füllt das Fenster mit passendem Beschnitt. Rastergrafiken gewinnen bei höherer Auflösung keine zusätzlichen Bilddetails.

Maus-Hover/Klick, Pfeile/W/S, Tab/Shift+Tab sowie Controller-D-Pad/Stick und Bestätigung bleiben verfügbar. In Aussehensfeldern wechseln links/rechts die Werte. Escape/rechte Controllertaste gehen auf Hauptmenü-Unterseiten zurück. In Lobby-Dialogen wird zunächst abgebrochen; in der normalen Lobby fokussiert Zurück den Verlassen-Button. Ein Verlassen braucht dadurch eine ausdrückliche Bestätigung dieses Buttons. Textfelder behalten die übliche Textbearbeitung.

## Netzwerk- und Speicherregeln

Darstellung und Layout verwenden weiterhin die bestehenden Session-/ES3-/Netzwerkaktionen. Host/Client-Auswahl, Bereitschaft, Freigeben und gemeinsamer Abenteuerstart bleiben hostgesteuert. Fremd belegte Figuren können nicht übernommen werden. Nur der Host kann freie Figuren löschen; die Gründerfigur bleibt geschützt. Die Canvas-Ansicht zeigt ausgerüstete Rüstung, Schuhe und Kopfbedeckung in den Porträts. Eine fehlende Canvas-Lobby fällt weiterhin auf die bisherige IMGUI-Ansicht zurück.

Saveformat und Netzwerkprotokoll bleiben erhalten. Options ist wie bisher eine leere Seite mit Zurück. Das Speicherbuch im Spiel behält seine eigene Oberfläche.

## Assets und Setup

Original-PNGs und Spriteassets: `Assets/Resources/MenuUI/QuietHome`; Herkunft: `provenance.json`. SubmenuButtons.png und LobbyButtons.png enthalten jeweils 16 Alphazellen mit eingearbeiteten Beschriftungen. Spriteassets referenzieren einzelne Bildausschnitte.

`SecretsReborn → Frontend → Prepare spacious menu and lobby` stellt vorhandene ältere Menüseiten auf die vollständigen Layouts um und ergänzt die native Lobby. Bereits eingerichtete Layouts werden bewahrt. Zuvor offene Szenen speichern und Play beenden. Die aktiven Layouts stehen in MainMenu.unity; das alte HomeCanvas-Prefab dient nur als ursprüngliches Ausgangsasset.

## Prüfung

`--main-menu-report <absoluter-reportpfad>` prüft im Development-Build native Eingaben, Animationen, Seitensichtbarkeit, Aussehensfelder, Bildschirmgrenzen, fehlende Scrollcontainer und überschneidungsfreie Bedienelemente. Save-/Papierkorbprüfungen verwenden isolierte TestSlots neben dem Report.

Der Zwei-Prozess-Test verwendet `--lobby-ui-host --lobby-ui-report <host.txt>` beziehungsweise `--lobby-ui-report <client.txt>` im selben Reportordner. Er erstellt eine nicht gespeicherte Testwelt und prüft Canvas-Lobby, vier Karten, Charaktererstellung/Abbrechen, Controller-Bestätigung, Besitzschutz, Gründerfigur, Löschen mit Bestätigung, Bereitschaft und gemeinsamen Szenenstart. Screenshots entstehen neben den Reports.
