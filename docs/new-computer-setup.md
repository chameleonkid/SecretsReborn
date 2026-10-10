# Neuer Windows-Rechner: Installation und Einstieg für den nächsten Chat

Stand: 10. Oktober 2026. Diese Anleitung beschreibt die Entwicklungsumgebung.
Spielkonzept, Implementierungsstand und offene Tests stehen in
[der Übergabe](handoff-2026-10-10.md). Ein neuer Chat soll beide Dateien lesen.

## 1. Auf dem bisherigen Rechner sichern

- Projektstand ist auf `origin/main`: Implementierung `e27a3a0`, Übergabe `0533754`.
  Neuere Dokumentationsänderungen müssen vor dem Umzug ebenfalls übertragen werden.
- Spielstände separat sichern:
  `%USERPROFILE%\AppData\LocalLow\DefaultCompany\SecretsReborn`.
- Bei Bedarf Videos, originale Asset-Dateien und lokale Builds separat kopieren.
- Für den bisherigen Chat VS Code/Codex beenden und `%USERPROFILE%\.codex`
  privat sichern. Der Ordner enthält auch Anmeldedaten; nicht ins Repository legen.
  Eine Wiederherstellung des Chats ist nicht garantiert und für die Projektfortsetzung
  nicht notwendig. Auf dem neuen Rechner regulär neu anmelden.

## 2. Programme installieren

1. [GitHub Desktop](https://desktop.github.com/) installieren und mit dem
   GitHub-Konto anmelden, das Zugriff auf `chameleonkid/SecretsReborn` hat.
2. [Git für Windows](https://git-scm.com/downloads/win) installieren, damit
   `git` auch im Terminal verfügbar ist. Mit `git --version` prüfen.
   GitHub Desktop allein garantiert keinen Git-Befehl im PATH.
3. [Unity Hub](https://unity.com/download) installieren und mit dem Unity-Konto
   für die gekauften Asset-Store-Inhalte anmelden.
4. Über Hub exakt **Unity 6000.3.25f1** installieren. Falls die Version nicht
   direkt angeboten wird, über das Unity-Downloadarchiv in Hub öffnen.
   Windows-Build-Unterstützung installieren; für IL2CPP zusätzlich passende
   Visual-Studio-C++-Werkzeuge/Windows-SDK. Den tatsächlich verwendeten
   Scripting Backend im Projekt prüfen, bevor zusätzliche Werkzeuge nötig werden.
5. [Visual Studio Code](https://code.visualstudio.com/) installieren.
   Für das Projekt Microsofts **Unity**-Erweiterung
   (`visualstudiotoolsforunity.vstuc`) und ihre benötigten C#-Abhängigkeiten
   installieren. Außerdem die offizielle OpenAI-Codex-Erweiterung installieren
   und mit dem bisherigen OpenAI-Konto anmelden.

Bisherige Umgebung: Windows, PowerShell, Projekt unter
`D:\SecretsReborn\SecretsReborn`, Unity unter
`C:\Program Files\Unity\Hub\Editor\6000.3.25f1\Editor\Unity.exe`.
Ein anderer Laufwerks-/Benutzerpfad ist möglich; der nächste Chat muss ihn prüfen.

## 3. Repository klonen

1. GitHub Desktop: **File → Clone repository → URL**.
2. URL: `https://github.com/chameleonkid/SecretsReborn.git`.
3. Als endgültigen lokalen Projektordner möglichst
   `D:\SecretsReborn\SecretsReborn` wählen und klonen.
4. Branch `main` auswählen, **Fetch origin** und bei Bedarf **Pull origin**.
5. In VS Code **File → Open Folder** verwenden und die Projektwurzel öffnen,
   in der `Assets`, `Packages` und `ProjectSettings` liegen.
6. Optional Secrets separat als Asset-Fundus klonen:
   `https://github.com/chameleonkid/Secrets.git`. Den tatsächlichen Pfad mitteilen;
   alte Secrets-Spiellogik nicht ungeprüft in SecretsReborn importieren.

## 4. Unity importieren und VS Code verbinden

1. Projekt in Unity Hub hinzufügen und mit **6000.3.25f1** öffnen.
   Ersten Import und Paketauflösung vollständig abwarten.
2. **Easy Save 3** aus dem eigenen Asset-Store-Konto importieren.
   `Assets/Plugins/Easy Save 3` ist absichtlich von Git ausgeschlossen.
   Fehlende ES3-Typen beim ersten Import sind daher zunächst eine fehlende Abhängigkeit.
3. Falls Unity fehlende TMP Essential Resources meldet:
   **Window → Text Mesh Pro → Import TMP Essential Resources**.
   Vorhandene Projektressourcen zuerst prüfen; Beispiele sind nicht erforderlich.
4. **Edit → Preferences → External Tools**: VS Code als External Script Editor
   wählen und Projektdateien neu generieren.
5. Das Projekt enthält `.vscode/launch.json` mit **Attach to Unity**.
   In VS Code **Run and Debug → Attach to Unity** starten und den passenden
   laufenden Editor auswählen. Anhängen und später einen echten Breakpoint prüfen.
6. `.csproj`, `.sln`, `.slnx` werden von Unity erzeugt und sind ignoriert.
   Keine alten Pfade manuell hineinschreiben. `Library` kann neu entstehen.

Pakete werden aus `Packages/manifest.json` und Lockdatei aufgelöst, nicht wahllos
aktualisiert. Aktuell unter anderem URP 17.3.0, Input System 1.20.0,
Netcode for GameObjects 2.7.0 und Visual Studio Editor 2.0.26.

## 5. Codex die nötigen Zugriffe geben

- Den geöffneten eigenen Projektordner in VS Code als vertrauenswürdig bestätigen.
- Codex für lokale Arbeit verwenden und Schreibzugriff auf die Projektwurzel
  sowie Ausführung der benötigten Git-/Unity-/Testbefehle erlauben.
- In den verfügbaren Berechtigungseinstellungen mit auf den Workspace begrenztem
  Zugriff beginnen. Konkrete zusätzliche Zugriffe bei Bedarf freigeben:
  `.git` schreiben für Commits, GitHub-Netzwerkzugriff für Fetch/Push,
  Unity starten und lokale Test-Builds ausführen.
- Die genaue Oberfläche hängt von der installierten Codex-Version ab.
  Bei blockierten Aktionen soll der nächste Chat die konkrete Meldung und
  erforderliche Freigabe erklären. Pauschaler Zugriff auf den ganzen Rechner
  oder dauerhafte Administratorrechte sind für die normale Projektarbeit nicht nötig.
- GitHub Desktop-Anmeldung und Git-Terminal-Anmeldung gegebenenfalls getrennt
  prüfen. Passwörter/Tokens nicht im Chat posten; reguläre Anmeldedialoge verwenden.
- Für LAN-Tests Windows-Firewallzugriff des Builds im privaten Netzwerk erlauben,
  sofern benötigt; die Firewall nicht insgesamt abschalten.

## 6. Auftrag für den nächsten Chat

> Richte mit mir SecretsReborn auf diesem Windows-Rechner ein. Lies zuerst
> docs/new-computer-setup.md und docs/handoff-2026-10-10.md. Prüfe Projektpfad,
> Git-Status, origin/main, Unity-Version und installierte Werkzeuge. Führe mich
> durch fehlende Installationen, Easy-Save-Import, VS-Code-Integration und
> erforderliche konkrete Berechtigungen. Prüfe laufende Unity-Prozesse vor CLI-Starts.
> Erhalte vorhandene Nutzeränderungen. Sobald der Import funktioniert, erstelle
> den aktuellen Testbuild und prüfe cooldownfreie Tränke sowie Ringfenster/HUD
> mit Host und Client. Danach folgen Cast-Animationen und Feuerball-Projektile.
> Nichts ohne ausdrücklichen Auftrag committen oder pushen.

Erfolgskriterien: Git-Zugriff funktioniert, richtige Unity-Version, keine
fehlenden Abhängigkeiten/Compilerfehler, VS Code erkennt die Projektverweise,
Debugger lässt sich anhängen und derselbe aktuelle Build verbindet Host und Client.

## Offizielle Anleitungen

- [Repository mit GitHub Desktop klonen](https://docs.github.com/en/desktop/adding-and-cloning-repositories/cloning-and-forking-repositories-from-github-desktop)
- [Unity-Integration in VS Code](https://code.visualstudio.com/docs/other/unity)
- [Codex IDE-Erweiterung](https://learn.chatgpt.com/docs/codex/ide)
- [Codex-Berechtigungen und Sandbox](https://learn.chatgpt.com/docs/security)
