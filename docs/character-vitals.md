# HP und Mana

Jeder Charakter startet mit 100 HP und 50 Mana. `WorldSessionState.CharacterVitals(characterId)` hält die Werte in der Host-Welt über Gebietswechsel hinweg. Die Anzeige folgt dem Charakter der aktiven Kamera und blendet sich bei geöffnetem Inventar oder Speicherbuch aus.

Schaden, Heilung, Manaverbrauch und Manawiederherstellung laufen über `GameSession`. Der Solo-Adapter prüft Charakterautorität und blockiert Änderungen während des Ladens. Ein späterer Netzwerkadapter muss zusätzlich Absender, Charakterbesitz und Aktionsregeln prüfen; Mengen werden vom Host aus Angriffen, Zaubern oder Gegenständen bestimmt. Netzwerk-Koop ist noch nicht implementiert.

Schaden und Heilung bleiben zwischen 0 und dem Maximum. Mana wird nur abgezogen, wenn die vollständigen Kosten gedeckt sind und HP über 0 liegen. Heilung kann derzeit auch 0 HP wieder anheben. Es gibt noch keine Regeneration, Ausrüstungsboni oder Todes-/Respawnmechanik; Bewegung und Interaktion bleiben bei 0 HP möglich, damit man die Grundlage testen kann.

Savegame-Version 4 speichert aktuelle und maximale HP/Mana. Versionen 1–3 bleiben lesbar und erhalten volle Startwerte. Ungültige oder fehlende Werte in Version 4 werden zurückgewiesen.

## Test im Unity-Editor

Im Play-Modus unter `SecretsReborn → Character → Test`:

- `Take 25 damage`: 25 HP abziehen.
- `Heal 25 HP`: 25 HP heilen.
- `Spend 10 mana`: 10 Mana verbrauchen.
- `Restore 10 mana`: 10 Mana auffüllen.

Danach zwischen Wald und Höhle wechseln: Werte müssen erhalten bleiben. Am Buch speichern, Werte verändern und den Slot laden: gespeicherte HP und Mana müssen wiederhergestellt sein. Die Testmenüs existieren nur im Editor.

Vorschlag für den nächsten Schritt: einen einfachen Nahkampfangriff und einen Testgegner ergänzen, anschließend die Regeln für Kampfunfähigkeit und Wiederbelebung festlegen.
