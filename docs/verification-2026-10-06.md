# Prüfstand – 6. Oktober 2026

Dieser Bericht hält die Ergebnisse zum Tagesabschluss fest. Die ausführlichen
lokalen Logs liegen in ignorierten Temp-/Build-Verzeichnissen.

| Prüfung | Ergebnis |
| --- | --- |
| Runtime-/Editor-Kompilierung und nativer Unity-Import | Bestanden |
| Nativer Windows-Development-Build | Erfolgreich, 0 Fehler; eine Warnung zur optionalen Unity-Pipeline-Runtime-Konfiguration |
| Inventartests | Slotregeln, Lampe, getrennte Charaktere und atomare Transaktionen bestanden |
| Kampftests | Cooldowns, Trefferbedingungen, Unverwundbarkeit und Rüstungsminderung bestanden |
| Weltzustandstests | Vitals/Herzen, Migration, aktive Gruppe, Tod und Revive bestanden |
| `tests/CoopProtocolChecks.cs` | Handshake, Eingaben/Slotgrenzen und JSON-Leerslot-Normalisierung bestanden |
| `tests/ReplicaMotionChecks.cs` | Kontinuierliche Darstellung, Paketlücken, keine Extrapolation und Rücksetzung bestanden |
| Echter Zwei-Prozess-Test nach Bewegungsfix | Host und Client bestanden |

Der letzte Zwei-Prozess-Test (`Temp/CoopSmooth-Host.txt` und
`Temp/CoopSmooth-Client.txt`) prüfte echte Transportverbindung, clientseitige
Eingaben mit hostseitiger Bewegung, unabhängige Ausrüstung, Ablehnung ungültiger
Equipment-Aktionen, Einzeltod ohne Game Over, gehaltenes Revive vom Client und
synchronisiertes Game Over nach Tod aller aktiven Figuren. Beim Disconnect
wurde der Gast aus der aktiven Gruppe entfernt und seine Ausrüstung erhalten.
Beide Testprozesse wurden beendet. Der Test schreibt keine Savegame-Dateien.
Der Testtreiber wird nur mit expliziten Development-Testargumenten aktiviert.

Manuell vom Nutzer bestätigt: Truhen-Save/Load, Öffnen ohne vorher eingesammelte
Lampe, Death-/Revive-Ablauf und Netzwerkbewegung nach dem Glättungsfix.
Die letzte Rückmeldung zur Bewegung lautete: „Läuft wie geschmiert.“

Noch nicht geprüft bzw. noch nicht implementiert sind vier gleichzeitige
Netzwerkprozesse, verschiedene LAN-Rechner, Internetbetrieb, gemeinsame
Szenenwechsel, Multiplayer-Load/Gruppen-Retry und vollständige Koop-Rätselinteraktion.
Diese Ergebnisse sind kein Nachweis für diese noch offenen Abläufe.
