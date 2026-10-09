# LAN-Test mit vier PCs

Der Nutzer bestätigt den LAN-Test am 9. Oktober 2026 mit „Funktioniert perfekt.“
Der Host lauscht auf
`0.0.0.0`, UDP-Port `7777`; Clients geben seine LAN-IPv4 im Join-Menü ein.
Alle Teilnehmer benötigen denselben Build mit Netzwerkprotokoll 14.

1. Den gesamten Ordner `Builds/LocalCoop` auf jeden PC kopieren. Neben der EXE
   werden insbesondere der Datenordner und die mitgelieferten DLLs benötigt.
   Unity muss auf den anderen PCs nicht installiert sein.
2. Alle PCs mit demselben Heimnetz verbinden; kein isoliertes Gäste-WLAN verwenden.
3. Auf dem Host-PC mit `ipconfig` die IPv4-Adresse des verwendeten Ethernet- oder
   WLAN-Adapters ermitteln, etwa `192.168.178.20`.
4. Auf diesem PC Multiplayer → Host Game → New Game oder Load Game wählen.
5. Auf den drei anderen PCs Multiplayer → Join Game wählen, die Host-IPv4 und
   einen Spielernamen eingeben und Connect drücken. `127.0.0.1` würde jeweils
   auf den eigenen PC zeigen und ist hier ungeeignet.
6. Falls die Windows-Firewall fragt, die Anwendung im privaten Heimnetz zulassen.
   Bei blockierter Verbindung am Host die eingehende Freigabe für die Anwendung
   beziehungsweise UDP 7777 im privaten Netzwerk prüfen. Die Firewall bleibt aktiv.
   Im normalen gemeinsamen LAN ist keine Router-Portweiterleitung nötig.
7. Figuren auswählen, bestätigen und gemeinsam starten. Bewegung, Kampf,
   Inventar, Runen, Truhen, gemeinsames Buch und Gebietswechsel prüfen.

## Echte Unterbrechung

Dieser gezielte Unterbrechungstest ist noch nicht einzeln bestätigt.

Nur bei einem Client das LAN-Kabel ziehen oder WLAN ausschalten, während sein
Spiel weiterläuft. Der Host und die anderen Clients bleiben verbunden.
Die Figur muss verschwinden und frei werden, sobald der Host den Verbindungsverlust
über den Transport-Timeout erkennt. Das ist bei einem abrupten Kabelverlust nicht
zwingend sofort der Fall. Ihre Daten müssen weiterhin in der Host-Welt vorhanden sein.

Danach die Verbindung wiederherstellen und normal joinen. Der Spieler muss eine
freie Figur ausdrücklich bestätigen; Inventar, HP/Mana und Tod dürfen sich durch
den Rejoin nicht zurücksetzen. Falls ein anderer Spieler diese Figur inzwischen
übernommen hat, bleibt sie belegt. Zusätzlich das Speichern während seiner
Abwesenheit und anschließendes Laden prüfen.

Host-Netzwerkverlust und Host-Prozessabsturz separat prüfen: Clients müssen die
Verbindung nach Erkennung verlassen. Eine automatische Host-Migration ist nicht
vorgesehen. Die Bestätigung des geordneten Host-Endes ersetzt diese beiden Tests nicht.
