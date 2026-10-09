# Gemeinsames Lager

Erster Shared-Stash-Schritt, 9. Oktober 2026. Ein Weltspeicher mit 40 Plätzen,
gemeinsam für alle Figuren und unabhängig von gerade verbundenen Spielern.
Gold bleibt vorerst persönlich; der Stash nimmt Items auf.

## Bedienung

`SharedStash.prefab` liegt unter `Assets/World/Equipment/Prefabs`.
Im Waldheiligtum steht es unter `World`, nahe dem Startbereich, mit dem Label
**Lager**. Position im Editor frei anpassbar. E / A öffnet es in der Nähe.

Links der eigene 10×4-Rucksack, rechts das gemeinsame 10×4-Lager.
Maus oder Stick/Pfeile wählen einen Slot. Tab / LB / RB wechselt die Seite.
A / Enter / Rechtsklick überträgt den ganzen Stapel auf einen passenden freien
Platz. Ziehen auf die andere Seite wählt einen Zielplatz. B / Esc schließt.
Unterschiedliche belegte Slots werden nicht automatisch getauscht.

Das Menü öffnet nur für den jeweiligen Nutzer; mehrere Spieler können
gleichzeitig lagern. Die Welt wird nicht pausiert, der Spieler bekommt keine
Schadensimmunität. Bei Tod, Entfernen vom Lager oder Szenenwechsel endet der
Zugriff. Keine Ausrüstung aus belegten Equipment-Slots direkt einlagern:
zuerst ablegen, dann aus dem Rucksack übertragen.

## Autorität und Persistenz

Jeder Transfer wird als einzelne atomare Operation vom Host entschieden.
Der Request enthält Lager-ID, Quell-/Zielplatz sowie erwartete Item-ID und Menge.
Der Host prüft Besitzer der anfragenden Figur, Nähe, aktuelle Szene, Zustand,
aktuellen Stapel und Platz. Eine veraltete Auswahl nimmt keinen inzwischen
anderen Gegenstand aus demselben Slot. Wer zuerst einen Gegenstand entnimmt,
erhält ihn; weitere Anfragen auf den leeren Slot werden verworfen.
Im Client-Menü sind Anfragen erst mit dem nächsten Host-Snapshot bestätigt.

Der Stash gehört zum Weltspielstand, nicht zu einer Figur. Löschen oder
Disconnect einer Figur entfernt den Inhalt nicht. Mehrere Lagerobjekte können
auf denselben Inhalt zugreifen, benötigen aber eindeutige Container-IDs.

Saveformat **15** ergänzt den Stash, ältere Saves starten mit leerem Lager.
Netzwerkprotokoll **16**; auf allen PCs denselben vollständigen Build verwenden.

## Grenzen dieses Schritts

Ganze Stapel, kein Mengen-Dialog und kein Verschieben innerhalb einer Lagerseite.
Kein Geldtransfer, Händler oder Droppen. Diese Erweiterungen folgen bei Bedarf.
Ein persistenter Gegenstand mit unpassender/neuer unbekannter Item-ID wird
nicht übertragen: alle Figuren müssen denselben vollständigen Item-Katalog haben.

## Prüfung

`tests/SharedStashChecks.cs` prüft atomare Transfers, konkurrierendes Entnehmen,
veraltete IDs/Mengen, belegte Ziele, Kapazität, kopierte Save-Daten,
Migration aus Format 14 und Netzwerkvalidierung.
Der native Easy-Save-Dateitest prüft einen gefüllten Stash.
Der opt-in Host-/Client-Test über `EconomyIntegrationDriver` ergänzt einen
Client-Deposit, Entnahme durch den Host und erneute veraltete Entnahme.

Prüfstand: Domain-/Protokolltests bestanden. Unity-Import, native
Prefab-/Szenenplatzierung, Icon-Normalisierung und echter Easy-Save-Dateitest
mit gefülltem Stash bestanden. Terrain und Rüstungsformel beibehalten.
Der echte Zwei-Prozess-Test besteht ebenfalls: Client lagert ein, Host nimmt
heraus, eine veraltete weitere Entnahme wird verworfen, Save/Load erhält genau
einen Besitzer. Trank-/Gold-Regressionsprüfungen bestehen weiterhin.
Windows-Testbuild aktualisiert am 9. Oktober 2026 um 12:28 Uhr
(`SecretsReborn_Data/Managed/Assembly-CSharp.dll`); 0 Fehler, 1 Buildwarnung.
Die EXE selbst ist Unitys wiederverwendeter Player und kann ein älteres Datum
anzeigen. Den vollständigen Buildordner auf alle Test-PCs kopieren.

Der Nutzer bestätigt den Shared Stash anschließend als technisch funktionierend.
Inventar und Lager verwenden jetzt editierbare Canvas-Prefabs;
Layout-Anleitung: [UI-Authoring](inventory-ui-authoring.md).
Die bisherigen automatisierten Tests bleiben als Regression erhalten.
Der Canvas-Build vom 9. Oktober, 13:02:43 Uhr besteht ebenfalls im echten
Host-/Client-Test, einschließlich UI-Anlegen/Ablegen und Lager-Panelwechsel.
Der aktuelle genaue Prüfstand steht in [Verifikation](verification-2026-10-09.md).

Nach dem UI-Umbau manuell: beide Spieler am Lager öffnen, einlagern und beim anderen entnehmen;
gleichzeitige Auswahl desselben Items, volles Inventar, Controller-Navigation,
Save/Load und Disconnect prüfen. Das visuelle Ergebnis im Spiel prüfen.
