# Zoo Manager Professional

Eine moderne WPF-Anwendung zur Verwaltung von Zoo-Beständen, Gehegen und Personal, entwickelt mit .NET 8.0 und MySQL.

## Voraussetzungen

*   **Visual Studio 2022** (mit installierter Workload "Desktopentwicklung mit .NET")
*   **MySQL Server** (lokal installiert, z. B. über XAMPP oder MySQL Installer)
*   **.NET 8.0 SDK**

## Einrichtung

### 1. Datenbank vorbereiten
Bevor die Anwendung gestartet werden kann, muss die Datenbankstruktur angelegt werden:
1.  Öffnen Sie Ihr MySQL-Verwaltungstool (z. B. MySQL Workbench oder Rider Database Tab).
2.  Führen Sie das beiliegende SQL-Skript `ZooManager_Setup.sql` aus.
    *   Dies erstellt die Datenbank `ZooManagerDB`.
    *   Es legt alle benötigten Tabellen (Animals, Species, Enclosures, Employees, etc.) an.
    *   Es fügt erste Testdaten hinzu.

### 2. Konfiguration anpassen
Passen Sie die Verbindungseinstellungen an Ihre lokale MySQL-Installation an:
1.  Öffnen Sie die Datei `App.config` im Hauptverzeichnis des Projekts.
2.  Ändern Sie im Bereich `<connectionStrings>` das Passwort (`Pwd=...`) und ggf. den Benutzernamen (`Uid=...`), falls dieser nicht `root` ist.

### 3. Projekt starten
1.  Öffnen Sie die Datei `ZooManager.sln` mit Visual Studio 2022.
2.  Visual Studio wird die benötigten NuGet-Pakete (z. B. `MySql.Data`) automatisch wiederherstellen.
3.  Drücken Sie **F5** oder den Start-Button, um die Anwendung zu starten.

## Umgesetzte Anforderungen

*   **ANF1 (Digitale Akte):** Chronologische Erfassung von Ereignissen pro Tier.
*   **ANF2 (Anlagen-Planung):** Validierung von Klima, Platz und Wasserzugang bei der Tierzuordnung.
*   **ANF3 (Personal):** Verwaltung von Mitarbeiter-Qualifikationen für bestimmte Tierarten.
*   **ANF4 (Dynamische Felder):** Frei konfigurierbare Zusatzfelder für jede Tierart.
*   **Dashboard:** Echtzeit-Statistiken und Fütterungsvorschau.
*   **Fütterungsplan:** Täglicher 24h-Rhythmus für die Tierversorgung.
