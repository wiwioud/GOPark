# Externe Bibliotheken und NuGet-Pakete

## GOPark – Parking Management System

Stand: 23.08.2026

Dieses Dokument enthält die direkt im Projekt GOPark eingebundenen externen NuGet-Pakete.

Die Paketliste wurde mit folgendem Befehl aus der Solution ermittelt:

`dotnet list ParkingManagement.sln package`

Die für die Programmausführung erforderlichen Bibliotheken werden zusammen mit der veröffentlichten self-contained Release-Version ausgeliefert.

Transitive Abhängigkeiten, die automatisch durch NuGet eingebunden werden, werden nicht einzeln aufgeführt. Aufgeführt werden die direkt in den Projektdateien referenzierten Pakete.

---

## 1. BCrypt.Net-Next

**Version:** 4.2.0

**Verwendet in:**  
`ParkingManagement.Data`

**Hersteller / Maintainer:**  
Chris McKee / BCrypt.Net Project

**Verwendungszweck:**  
BCrypt.Net-Next wird für die sichere Speicherung von Benutzerpasswörtern verwendet.

Passwörter werden nicht im Klartext in der Datenbank gespeichert, sondern vor dem Speichern mit BCrypt gehasht. Beim Login wird das eingegebene Passwort mit dem gespeicherten Hash verglichen.

**Lizenz:**  
MIT License

**Quelle:**  
NuGet Gallery – BCrypt.Net-Next, Version 4.2.0

---

## 2. Microsoft.EntityFrameworkCore.Design

**Versionen im Projekt:**

- 8.0.8 in `ParkingManagement.Data`
- 8.0.0 in `ParkingManagement.UI`

**Hersteller:**  
Microsoft

**Verwendungszweck:**  
Das Paket stellt Design-Time-Funktionen von Entity Framework Core bereit.

Es wird insbesondere für:

- Datenbankmigrationen
- Entity-Framework-Core-Werkzeuge
- Design-Time-Erstellung des DbContext

verwendet.

**Lizenz:**  
MIT License

**Quelle:**  
NuGet Gallery – Microsoft.EntityFrameworkCore.Design

---

## 3. Microsoft.EntityFrameworkCore.SqlServer

**Version:**  
8.0.8

**Verwendet in:**

- `ParkingManagement.Data`
- `ParkingMonitoringService`

**Hersteller:**  
Microsoft

**Verwendungszweck:**  
Dieses Paket stellt den Entity-Framework-Core-Datenbankprovider für Microsoft SQL Server bereit.

GOPark verwendet diesen Provider für die Kommunikation mit SQL Server Express LocalDB.

Damit werden unter anderem folgende Daten gespeichert und verarbeitet:

- Benutzer
- Fahrzeuge
- Parkplätze
- Parkvorgänge
- Reservierungen
- Audit-Log-Einträge
- Systemeinstellungen
- Parkgebühren

**Lizenz:**  
MIT License

**Quelle:**  
NuGet Gallery – Microsoft.EntityFrameworkCore.SqlServer, Version 8.0.8

---

## 4. Microsoft.EntityFrameworkCore.Tools

**Version:**  
8.0.8

**Verwendet in:**  
`ParkingManagement.Data`

**Hersteller:**  
Microsoft

**Verwendungszweck:**  
Das Paket stellt Werkzeuge für Entity Framework Core bereit.

Es wird während der Entwicklung insbesondere für die Erstellung und Verwaltung von Datenbankmigrationen verwendet.

Beispiele:

- Erstellen von Migrationen
- Aktualisieren der Datenbank
- Verwaltung des EF-Core-Datenbankschemas

**Lizenz:**  
MIT License

**Quelle:**  
NuGet Gallery – Microsoft.EntityFrameworkCore.Tools, Version 8.0.8

---

## 5. Microsoft.Extensions.Configuration.Binder

**Version:**  
8.0.1

**Verwendet in:**  
`ParkingManagement.UI`

**Hersteller:**  
Microsoft

**Verwendungszweck:**  
Das Paket unterstützt das Binden von Konfigurationseinträgen an .NET-Objekte.

Es wird im Zusammenhang mit der konfigurierbaren Anwendungskonfiguration verwendet.

**Lizenz:**  
MIT License

**Quelle:**  
NuGet Gallery – Microsoft.Extensions.Configuration.Binder, Version 8.0.1

---

## 6. Microsoft.Extensions.Configuration.Json

**Version:**  
8.0.1

**Verwendet in:**

- `ParkingManagement.UI`
- `ParkingMonitoringService`

**Hersteller:**  
Microsoft

**Verwendungszweck:**  
Das Paket ermöglicht das Laden der Anwendungskonfiguration aus JSON-Dateien.

GOPark verwendet insbesondere:

`appsettings.json`

Darin befindet sich unter anderem die konfigurierbare SQL-Server-Verbindungszeichenfolge.

**Lizenz:**  
MIT License

**Quelle:**  
NuGet Gallery – Microsoft.Extensions.Configuration.Json, Version 8.0.1

---

## 7. Microsoft.Extensions.Hosting

**Version:**  
8.0.1

**Verwendet in:**  
`ParkingMonitoringService`

**Hersteller:**  
Microsoft

**Verwendungszweck:**  
Microsoft.Extensions.Hosting stellt die grundlegende Infrastruktur für den .NET Worker Service bereit.

Der `ParkingMonitoringService` verwendet diese Infrastruktur, um als separater Hintergrundprozess ausgeführt zu werden.

Der Worker übernimmt unter anderem:

- Hintergrundüberwachung
- Verarbeitung zeitabhängiger Reservierungszustände
- Datenbankzugriffe
- Named-Pipe-Kommunikation mit der WPF-Anwendung

**Lizenz:**  
MIT License

**Quelle:**  
NuGet Gallery – Microsoft.Extensions.Hosting, Version 8.0.1

---

## 8. Bereitstellung der Bibliotheken

Die finale Anwendung wird als self-contained Windows-x64 Release bereitgestellt.

Die benötigten Runtime-Dateien und Bibliotheken befinden sich unter:

`Release\GOPark`

Der Worker Service und dessen Abhängigkeiten befinden sich unter:

`Release\GOPark\Worker`

Dadurch müssen die oben genannten NuGet-Pakete auf dem Zielcomputer nicht manuell installiert werden.

Für den Betrieb der Anwendung wird weiterhin SQL Server Express LocalDB als Datenbanksystem benötigt.

---

## 9. Hinweis zu Microsoft .NET und WPF

Neben den oben aufgeführten NuGet-Paketen verwendet GOPark Funktionen des Microsoft-.NET-8-Frameworks sowie Windows Presentation Foundation (WPF).

Diese bilden die technische Plattform der Anwendung und werden durch die self-contained Veröffentlichung zusammen mit den für die Ausführung erforderlichen Runtime-Komponenten bereitgestellt.

---

## 10. Zusammenfassung

GOPark verwendet externe Bibliotheken ausschließlich für klar definierte technische Aufgaben wie:

- Passwort-Hashing
- SQL-Server-Datenbankzugriff
- Entity-Framework-Core-Migrationen
- JSON-Konfiguration
- Worker-Service-Infrastruktur

Die direkt referenzierten Pakete und ihre Versionen sind in diesem Dokument vollständig aufgeführt.