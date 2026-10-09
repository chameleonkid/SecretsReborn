# Zaubersystem – vereinbarte Grundlage

Stand: 9. Oktober 2026. Konzept mit dem Nutzer abgestimmt; noch nicht implementiert.
Erkundung und Item-Fortschritt bleiben die Grundlage. Secret of Mana dient als
Inspiration für Ringmenü und Zielwahl, nicht als Pflicht zur Übernahme alter Logik.

## Auswahl und Ausführung

Eigenes Zaubersystem unabhängig von Waffen. Zauberstäbe sind Waffen ähnlich
einem Bogen; gelernte Zauber können auch mit anderen Waffen verwendet werden.
Ringmenü: **Element → Zauber → Zielwahl → Bestätigen**. Elemente umfassen
beispielsweise Feuer, Eis, Licht, Schatten, Wasser und Blitz.
Ein separates Zauberbuch zeigt alle erlernten Zauber, Ränge, Wirkungen,
Manakosten und Aufwertungen. Die Einstellungsmöglichkeiten des Spiels bleiben
im Optionsmenü; das Zauberbuch ist ein eigener Menübereich.

Ziele werden ausdrücklich gewählt, kein manuelles Projektilzielen.
Erste Auswahlvarianten: ein gültiges Ziel oder alle gültigen Ziele in Reichweite
und im aktuellen Gebiet. Beliebige Teilmengen sind vorerst nicht vorgesehen.
Angriffszauber wählen Gegner; Unterstützung wählt Selbst/Verbündete.
Markierungen zeigen vor Bestätigung die betroffenen Figuren.

**Zauber kollidieren nicht mit der Umgebung.** Mauern, Bäume und Gelände fangen
Zaubereffekte nicht ab. Die zuvor vorgeschlagene Sichtlinien-/Hindernisprüfung
wird für diese Magie nicht übernommen. Reichweite, Gebiet und gültige Zielart
bleiben relevant. Fernkampfwaffen sind davon unabhängig.

Die Welt läuft im Multiplayer weiter. Der auswählende Charakter steht während
der Auswahl und während des eigentlichen Wirkens; keine Schadensimmunität.
Abbrechen bleibt schnell erreichbar. Stirbt/verschwindet ein Einzelziel vor
Ausführungsbeginn, wird ohne Manaverbrauch abgebrochen. Der Zielkreis wird bei
akzeptierter Ausführung festgehalten; später hinzukommende Ziele werden nicht
automatisch aufgenommen. Detaillierte Regeln für Abbruch/Unterbrechung nach
Ausführungsbeginn und Zielverlust während mehrteiliger Effekte sind noch festzulegen.

## Wirkbudget und Ränge

Schaden/Heilung besitzen ein Gesamtbudget pro Ausführung und werden durch die
Anzahl ausgewählter Ziele geteilt. Alle Teilprojektile gehören zu diesem Budget.
Bei 12 Gesamtschaden: ein Ziel 12, zwei Ziele je 6, vier Ziele je 3.
Rüstung wird nach Verteilung pro Ziel angewendet. Heilung über dem Zielmaximum
verfällt zunächst; kein automatisches Umverteilen. Rundungsregeln für halbe
Herzen müssen vor Umsetzung festgelegt und in der Zielvorschau sichtbar werden.
Schilde, Statusdauer und Beschwörungen benötigen eigene Verteilungsregeln;
die Schadensformel darf dort nicht ungeprüft übernommen werden.

Zauber werden dauerhaft pro Weltcharakter gelernt und haben Ränge.
Beispielwerte für Feuerball, noch zu balancieren:

| Rang | Darstellung | Gesamtbudget |
|---|---|---|
| 1 | 1 Feuerball | 5 Schaden |
| 2 | 1 Feuerball | 8 Schaden |
| 3 | 2 Feuerbälle à 6 | 12 Schaden |

Ränge durch weitere Bücher/besondere Aufwertungen, vorerst kein Training durch
ständiges Wirken und keine Charakter-XP. Mana und individuelle Cooldowns
begrenzen Ausführungen; eine kurze gemeinsame Ausführungszeit verhindert
gleichzeitiges Spammen. Exakte Zeiten/Kosten bleiben Balancing-Fragen.
Heal heilt lebende Figuren; Revive bleibt zunächst ein eigener Vorgang.
Ein Schild absorbiert eine begrenzte Menge und hat eine maximale Dauer.

## Erkundung, Händler und wiederholbare Bosse

Lernquellen: besondere Truhen, Lehrer/Heiligtümer, Rätsel und Bossbelohnungen.
Nur unverzichtbare Quest-/Fortschrittsgegenstände und notwendige Fähigkeiten
können nach gemeinsamer Freischaltung bei einem besonderen NPC wiederbeschafft
werden. Notwendige Fortschrittszauber dürfen nicht hinter einem Zufallsdrop liegen.
Ein normaler Händler verkauft gewöhnliche Waffen, Rüstungen und Verbrauchsgüter;
ein Rare-Fund schaltet nicht automatisch einen Kauf desselben Items frei.

Bosse sind wiederholbar und haben einen Lootpool, etwa optionale Zauberbücher,
Rare-Waffen und Rüstungen. Erster Abschluss gibt garantierten Fortschritt;
Wiederholungen liefern zufällige Ausrüstung. Als angenommene Arbeitsgrundlage
persönliche Belohnungen je teilnehmender Figur, anschließend über den Stash
tauschbar. Zusätzlich garantierte Bosswährung als Schutz gegen dauerhaftes
Drop-Pech; konkrete Teilnahmebedingungen, Preise und Poolgewichte noch offen.
Dungeon-Neustart bewusst außerhalb des Dungeons: Gegner/Boss zurücksetzen,
gelöste Welträtsel und einmalige Truhen erhalten. Ein einfacher Szenenwechsel
ist kein automatischer Belohnungsreset.

## Technische Grundlage und nächste Schritte

Zauberdefinitionen enthalten stabile IDs, Element, Zielregeln und Rangdaten.
Charakterdaten speichern erlernte IDs/Ränge. Der Host prüft Besitz, Ziele,
Reichweite, Mana, Cooldown und Zustand; Clients zeigen Auswahl und Effekte.
Bewegungssperre, Tod, Disconnect, Load und Szenenwechsel müssen Ausführungen
geordnet beenden. Persistente Lerndaten bleiben getrennt von laufenden Effekten.

Vorgeschlagene Umsetzungsreihenfolge:

1. Vorhandene Secrets-Zauberprefabs sichten: Icons/Effekte von alter Spiellogik
   trennen, Herkunft dokumentieren. Keine Wiederverwendbarkeit ungeprüft behaupten.
2. Definitionen/Ränge, persistenter Lernstatus und eine Host-Cast-Zustandsmaschine
   mit Bewegungssperre bauen. Rundung und Unterbrechungsregeln dafür konkretisieren.
3. Editierbares Canvas-Ringmenü, Zauberbuch und Zielmarkierungen anbinden.
4. Feuerball und Heal als erste durchgängige Testfälle: Gegner/Verbündete,
   Einzelziel/alle, Rangwechsel, Mana/Cooldown, Save/Load und Host/Client.
5. Frostbolt/Frostshield ergänzen; anschließend Sturm/Beschwörungen.
6. Lernbelohnungen, Wiederbeschaffungs-NPC, Händler und Dungeon-Reset schrittweise
   anbinden. Bossloot und Handel ersetzen nicht die erste Magie-Grundlagenprüfung.

Schnellzugriff für häufige Zauber bleibt ein Vorschlag. Controller-Tasten werden
vor UI-Implementierung gegen Angriff, Lampe, Tränke und Interaktion abgestimmt.
