# 🐾 ZooManager

Der ZooManager ist eine moderne WPF-Anwendung zur effizienten Verwaltung von Tierbeständen, Gehegen und Mitarbeiterqualifikationen. Das System wurde speziell für eine einfache Weitergabe entwickelt: **Es ist keine Datenbankinstallation (MySQL/SQL-Server) erforderlich.**

## 🚀 Features

- **Dashboard:** Echtzeit-Statistiken über den Tierbestand, anstehende Fütterungen und Zoo-Events.
- **Digitale Tierakte:** Vollständige Chronologie von Ereignissen (Tierarztbesuche, Transporte) pro Tier.
- **Intelligente Gehege-Validierung:** Beim Anlegen von Tieren werden automatisch nur Gehege vorgeschlagen, die zum benötigten Klima und Wasserbedarf der Tierart passen.
- **Mitarbeiterverwaltung:** Management von Qualifikationen für spezifische Tierarten.
- **Dynamische Attribute:** Unterstützung für tierartspezifische Zusatzfelder.

## 🛠 Technik-Stack

- **Framework:** .NET 8.0 (WPF)
- **Sprache:** C# 12.0
- **Datenbank:** SQLite (Lauffähig ohne zusätzliche Installation)
- **Architektur:** Interface-basierte Persistence-Layer (IPersistenceService)

## 📦 Installation & Start

Da das Projekt **SQLite** verwendet, ist der Start extrem einfach:

1. **Repository klonen** oder ZIP-Archiv entpacken.
2. Das Projekt in **JetBrains Rider** oder Visual Studio öffnen.
3. **NuGet-Pakete wiederherstellen**.
4. Das Projekt starten.

> [!NOTE]
> Beim allerersten Start erstellt die Anwendung automatisch eine Datei namens `zoo.db` im Programmverzeichnis. Diese enthält bereits einige Testdaten (Löwen, Pinguine), damit das System sofort erkundet werden kann.

## 📂 Projektstruktur

- **`ZooManager.Core`**: Enthält die Business-Logik und die Datenmodelle (`Animal`, `Species`, `Enclosure`).
- **`ZooManager.Infrastructure`**: Beinhaltet den `SqlitePersistenceService`. Hier liegt die Logik für die automatische Datenbank-Initialisierung.
- **`ZooManager.UI`**: Die Benutzeroberfläche bestehend aus modernen UserControls und Styles.

## 💡 Portabilität (JSON/SQL)

Dank der interface-basierten Architektur kann das System jederzeit umgestellt werden:

- Aktuell wird **SQLite** für volle SQL-Power ohne Server genutzt.
- Ein Umstieg auf **JSON**-Dateien ist durch Implementierung eines neuen `IPersistenceService` möglich.

---
Entwickelt für moderne Zooverwaltung – Einfach kopieren, starten und loslegen! 🦁🐧
