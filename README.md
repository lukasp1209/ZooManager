# ZooManager

ZooManager ist eine moderne WPF-Anwendung zur effizienten Verwaltung von Tierbeständen, Gehegen und Mitarbeiterqualifikationen. Das System ist auf einfache Weitergabe ausgelegt: **Es ist keine separate Datenbankinstallation (z. B. MySQL/SQL Server) erforderlich.**

## Funktionen

- **Dashboard:** Statistiken zu Tierbestand, anstehenden Fütterungen und Zoo-Events.
- **Digitale Tierakte:** Chronologie von Ereignissen (z. B. Tierarztbesuche, Transporte) pro Tier.
- **Gehege-Validierung:** Beim Anlegen von Tieren werden automatisch nur passende Gehege (Klima/Wasserbedarf der Tierart) vorgeschlagen.
- **Mitarbeiterverwaltung:** Verwaltung von Qualifikationen für spezifische Tierarten.
- **Dynamische Attribute:** Unterstützung tierartspezifischer Zusatzfelder.

## Technik-Stack

- **Framework:** .NET 8.0 (WPF)
- **Sprache:** C# 12.0
- **Datenhaltung:** SQLite (lauffähig ohne zusätzliche Installation)
- **Architektur:** Interface-basierter Persistence-Layer (`IPersistenceService`)

## Installation & Start

1. Repository klonen oder ZIP-Archiv entpacken.
2. Projekt in **JetBrains Rider** oder Visual Studio öffnen.
3. **NuGet-Pakete wiederherstellen**.
4. Projekt starten.

> [!NOTE]
> Beim ersten Start erstellt die Anwendung automatisch eine Datei `zoo.db` im Programmverzeichnis. Diese enthält bereits Testdaten, sodass die Anwendung sofort ausprobiert werden kann.

## Anmeldung (Demo-Zugangsdaten)

Für die Anmeldung stehen Beispielbenutzer zur Verfügung:

- **Manager-Login:** `manager` → `password`
- **Mitarbeiter-Login (Beispiel):** `max.mustermann` → `password`

> [!TIP]
> Falls die Demo-Zugangsdaten nicht funktionieren, die Anwendung einmal neu starten bzw. prüfen, ob `zoo.db` korrekt erstellt wurde.

## Projektstruktur

- **`ZooManager.Core`**: Business-Logik und Datenmodelle (`Animal`, `Species`, `Enclosure`).
- **`ZooManager.Infrastructure`**: `SqlitePersistenceService` inkl. Logik zur Datenbank-Initialisierung.
- **`ZooManager.UI`**: Benutzeroberfläche (UserControls, Styles).

## Portabilität (SQLite/JSON)

Durch die interface-basierte Architektur kann die Datenhaltung flexibel erweitert werden:

- Aktuell: **SQLite** für SQL-Funktionalität ohne Serverbetrieb.
- Optional: Umstieg auf **JSON** durch Implementierung eines weiteren `IPersistenceService`.
