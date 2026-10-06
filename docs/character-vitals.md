# HP und Mana

Jeder Charakter startet mit drei vollen Herzen und 50 Mana. Ein Herz besteht aus zwei Gesundheitseinheiten; maximal 20 Herzcontainer sind möglich. `WorldSessionState.CharacterVitals(characterId)` hält die Werte in der Host-Welt über Gebietswechsel hinweg. Die Anzeige folgt dem Charakter der aktiven Kamera und blendet sich bei geöffnetem Inventar oder Speicherbuch aus. Rote Pixelherzen zeigen volle, halbe und leere Herzen in Reihen mit jeweils zehn Plätzen. Mana bleibt eine grüne Bar.

Schaden, Heilung, Manaverbrauch und Manawiederherstellung laufen über `GameSession`. Der Solo-Adapter prüft Charakterautorität und blockiert Änderungen während des Ladens. Ein späterer Netzwerkadapter muss zusätzlich Absender, Charakterbesitz und Aktionsregeln prüfen; Mengen werden vom Host aus Angriffen, Zaubern oder Gegenständen bestimmt. Netzwerk-Koop ist noch nicht implementiert.

Schaden und Heilung bleiben zwischen 0 und dem Maximum. Mana wird nur abgezogen, wenn die vollständigen Kosten gedeckt sind und HP über 0 liegen. Heilung kann derzeit auch 0 HP wieder anheben. Es gibt noch keine Regeneration, Ausrüstungsboni oder Todes-/Respawnmechanik; Bewegung und Interaktion bleiben bei 0 HP möglich, damit man die Grundlage testen kann.

Savegame-Version 5 speichert aktuelle halbe Herzen, die Containerkapazität und Mana. Versionen 1–3 bleiben lesbar und erhalten drei volle Herzen. Version 4 wird von der alten 100-HP-Basis umgerechnet: 100 HP entsprechen drei Containern; der Füllstand wird auf das nächste halbe Herz aufgerundet, sodass ein lebender Charakter beim Laden nicht kampfunfähig wird. Mana bleibt erhalten. Ungültige Werte oder Containerkapazitäten werden zurückgewiesen.

`GameSession.AddHeartContainer(actor)` erweitert die Kapazität um ein Herz und heilt ein Herz bis zum Maximum. Bei 20 Containern wird die Änderung abgewiesen. Diese Grundlage kann später von einem einmalig eingesammelten Herzcontainer-Item aufgerufen werden; ein solches Welt-Item ist noch nicht platziert.

## Test im Unity-Editor

Das HUD sitzt oben links. Jedes Herz wird als zusammenhängende, mit Point-Filter
gezeichnete Textur dargestellt; die ganzzahlige Skalierung verhindert Fugen
zwischen Pixeln. Die permanente Steuerungsbox ist standardmäßig deaktiviert.
`Show Tutorial Controls` am Rätsel-/Prototyp-Component kann sie für eine
Tutorial-Szene gezielt aktivieren.

Im Play-Modus unter `SecretsReborn → Character → Test`:

- `Take half-heart damage`: ein halbes Herz abziehen.
- `Heal one heart`: ein Herz heilen.
- `Add heart container`: Kapazität um ein Herz erhöhen, bis maximal 20.
- `Spend 10 mana`: 10 Mana verbrauchen.
- `Restore 10 mana`: 10 Mana auffüllen.

Danach zwischen Wald und Höhle wechseln: Werte müssen erhalten bleiben. Am Buch speichern, Werte verändern und den Slot laden: gespeicherte HP und Mana müssen wiederhergestellt sein. Die Testmenüs existieren nur im Editor.

Vorschlag für den nächsten Schritt: einen einfachen Nahkampfangriff und einen Testgegner ergänzen, anschließend die Regeln für Kampfunfähigkeit und Wiederbelebung festlegen.
