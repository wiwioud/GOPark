# GOPark – Parking Management System

## 1. Projektziel

GOPark ist ein im Rahmen eines Universitätsprojekts entwickeltes Parking-Management-System zur Verwaltung eines Parkhauses bzw. Parkplatzes.

Die Anwendung unterstützt die Verwaltung von Parkplätzen und Fahrzeugen sowie die Erfassung von Ein- und Ausfahrten. Zusätzlich können Reservierungen erstellt und verwaltet, Parkgebühren berechnet, Benutzer verwaltet und Aktivitäten über ein Audit-Log nachvollzogen werden.

Das Projekt wurde in C# mit .NET 8 entwickelt und verwendet eine WPF-Benutzeroberfläche nach dem MVVM-Prinzip.

---

## 2. Verwendete Technologien

Im Projekt werden mehrere Inhalte des Moduls praktisch umgesetzt:

- **C# / .NET 8**
- **WPF (Windows Presentation Foundation)** für die grafische Benutzeroberfläche
- **MVVM** zur Trennung von Oberfläche und Anwendungslogik
- **SQL Server Express LocalDB**
- **Entity Framework Core** für den Datenbankzugriff
- **Asynchrone Programmierung mit async/await**
- **.NET Worker Service** als separater Hintergrundprozess
- **Named Pipes** für die Kommunikation zwischen WPF-Anwendung und Worker Service
- **BCrypt** zur sicheren Speicherung von Benutzerpasswörtern

---

## 3. Voraussetzungen für die Ausführung

Für die bereitgestellte Release-Version wird kein installiertes .NET SDK benötigt, da die Anwendung als **self-contained Windows-x64 Release** bereitgestellt wird.

Erforderlich ist:

- Windows 10 oder Windows 11, 64-Bit
- SQL Server Express LocalDB

Die Anwendung verwendet standardmäßig die LocalDB-Instanz:

`(localdb)\MSSQLLocalDB`

---

## 4. Programm starten

Die Anwendung kann über die Datei

`start.bat`

im Hauptverzeichnis gestartet werden.

Dazu:

1. Den Abgabeordner vollständig entpacken.
2. Sicherstellen, dass SQL Server Express LocalDB installiert ist.
3. `start.bat` per Doppelklick starten.
4. GOPark wird automatisch geöffnet.
5. Der zugehörige Worker Service wird von der WPF-Anwendung automatisch im Hintergrund gestartet.

Ein separater manueller Start des Worker Service ist nicht erforderlich.

Die ausführbare Anwendung befindet sich unter:

`Release\GOPark\ParkingManagement.UI.exe`

Der Worker Service befindet sich unter:

`Release\GOPark\Worker\ParkingMonitoringService.exe`

Alle für die Release-Version notwendigen .NET-Dateien und Bibliotheken befinden sich im Release-Verzeichnis.

---

## 5. Datenbank

GOPark verwendet **SQL Server Express LocalDB** mit Entity Framework Core.

Die Verbindungszeichenfolge befindet sich in der Datei:

`Release\GOPark\appsettings.json`

Die Standardkonfiguration verwendet die Datenbank:

`ParkingManagementDB`

Beim ersten Start initialisiert die Anwendung die Datenbank automatisch. Vorhandene Entity-Framework-Core-Migrationen werden angewendet und notwendige Ausgangsdaten werden angelegt.

Zusätzlich wird für die Abgabe ein SQL-Skript bereitgestellt:

`Database\GOPark.sql`

Weitere Hinweise zur Datenbank befinden sich unter:

`Database\README_Database.txt`

Damit kann die Datenbank bei Bedarf auch manuell nachvollzogen bzw. erstellt werden.

---

## 6. Konfiguration und relative Pfade

Die Anwendung verwendet keine fest an einen bestimmten Benutzer-PC gebundenen absoluten Pfade für den Programmstart.

Der Start erfolgt relativ zum Abgabeverzeichnis über:

`start.bat`

Auch der Worker Service wird relativ zum Verzeichnis der WPF-Anwendung gesucht:

`Worker\ParkingMonitoringService.exe`

Die Datenbankverbindung kann über die jeweilige `appsettings.json` konfiguriert werden.

Dadurch kann der gesamte Abgabeordner beispielsweise auf einen anderen Computer oder in ein anderes Verzeichnis kopiert werden, ohne Quellcode ändern zu müssen.

---

## 7. Testkonten

### Administrator

Benutzername:

`admin`

Passwort:

`Admin123!`

Der Administrator besitzt Zugriff auf die normalen Parkfunktionen sowie auf:

- Benutzerverwaltung
- Einstellungen
- Audit-Log

### Mitarbeiter

Benutzername:

`employee`

Passwort:

`Employee123!`

Der Mitarbeiter besitzt Zugriff auf die für den täglichen Parkbetrieb vorgesehenen Funktionen.

Die Passwörter werden nicht im Klartext gespeichert, sondern mit BCrypt gehasht.

---

## 8. Hauptfunktionen

### Dashboard

Das Dashboard zeigt zentrale Informationen des aktuellen Parkbetriebs, unter anderem:

- Gesamtzahl der verfügbaren Parkplätze
- freie Parkplätze
- belegte Parkplätze
- aktive Parkvorgänge
- geplante Reservierungen
- Tagesumsatz
- aktuelle Aktivitäten

### Einfahrt

Bei einer Einfahrt werden Kennzeichen und Fahrzeugtyp erfasst und ein aktuell freier Parkplatz ausgewählt.

Das System:

- prüft die aktuelle Parkplatzverfügbarkeit,
- verhindert doppelte aktive Parkvorgänge eines Fahrzeugs,
- normalisiert Kennzeichen,
- erstellt einen neuen Parkvorgang,
- setzt den Parkplatz auf belegt,
- und protokolliert die Aktion im Audit-Log.

### Ausfahrt

Bei einer Ausfahrt wird ein aktiver Parkvorgang ausgewählt.

Das System:

- ermittelt die aktuelle Parkdauer,
- berechnet die Parkgebühr,
- speichert Ausfahrtszeit und Endbetrag,
- beendet den Parkvorgang,
- aktualisiert den Parkplatzstatus,
- und protokolliert die Aktion.

### Parkgebühren

Die Gebührenberechnung berücksichtigt konfigurierbare Werte wie:

- Stundenpreis
- Mindestgebühr
- Kulanzzeit
- maximale Tagesgebühr

Die Werte werden über die Anwendungseinstellungen verwaltet und in der Datenbank gespeichert.

### Reservierungen

Reservierungen können für einen bestimmten Parkplatz und ein definiertes Zeitfenster angelegt werden.

Unterstützte Zustände sind:

- Geplant
- Aktiv
- Abgelaufen
- Storniert

Zeitliche Überschneidungen werden geprüft. Geplante bzw. aktive Reservierungen werden vom Hintergrunddienst überwacht und die entsprechenden Status werden aktualisiert.

### Parkplätze

Parkplätze können angezeigt, gesucht, gefiltert und verwaltet werden.

Unterstützte Zustände sind unter anderem:

- Frei
- Belegt
- Reserviert
- Außer Betrieb

### Fahrzeuge

Fahrzeuge können verwaltet und über ihr Kennzeichen gesucht werden.

### Parkvorgänge

Vergangene und aktive Parkvorgänge können angezeigt und gefiltert werden. Abgeschlossene Parkvorgänge enthalten zusätzlich Ausfahrtszeit und berechnete Parkgebühr.

### Benutzerverwaltung

Administratoren können Benutzer des Systems verwalten.

Die Anwendung unterscheidet zwischen den Rollen:

- Administrator
- Mitarbeiter

### Einstellungen

Administratoren können zentrale Systemeinstellungen und Parkgebühren konfigurieren.

### Audit-Log

Wichtige Benutzer- und Systemaktionen werden protokolliert und können von Administratoren eingesehen werden.

---

## 9. Worker Service

Das Projekt enthält den separaten Worker Service:

`ParkingMonitoringService`

Der Worker läuft als eigener Prozess im Hintergrund.

Zu seinen Aufgaben gehört insbesondere die Überwachung zeitabhängiger Vorgänge wie Reservierungen.

Der Service wird beim Start der WPF-Anwendung automatisch gestartet und beim Beenden der von GOPark gestarteten Instanz wieder beendet.

Der aktuelle Verbindungszustand wird in der Benutzeroberfläche angezeigt:

- Grün: `Service: Online`
- Rot: `Service: Offline`

---

## 10. Kommunikation zwischen Programmen

Die Kommunikation zwischen der WPF-Anwendung und dem Worker Service erfolgt über **Named Pipes**.

Dabei fungiert der Worker Service als Server und die WPF-Anwendung als Client.

Verwendet werden unter anderem:

- `NamedPipeServerStream`
- `NamedPipeClientStream`

Der Named-Pipe-Client läuft asynchron im Hintergrund und blockiert dadurch die grafische Benutzeroberfläche nicht.

Nachrichten des Worker Service können in GOPark als Benachrichtigung angezeigt werden.

Die Verbindung kann nach einer Unterbrechung erneut aufgebaut werden.

---

## 11. Asynchrone Programmierung

Datenbankzugriffe und weitere länger laufende Operationen werden an zentralen Stellen asynchron ausgeführt.

Verwendet werden beispielsweise:

- `ToListAsync()`
- `FirstOrDefaultAsync()`
- `AnyAsync()`
- `CountAsync()`
- `SaveChangesAsync()`
- `async`
- `await`

Dadurch bleibt die WPF-Benutzeroberfläche während Datenbankoperationen reaktionsfähig.

---

## 12. Projektstruktur

Das Projekt besteht aus drei Hauptprojekten:

### ParkingManagement.Data

Class Library für:

- Entity-Framework-Core-Modelle
- `AppDbContext`
- Datenbankmigrationen
- Datenbankinitialisierung
- Seeding

Beispiele für Datenmodelle:

- User
- Vehicle
- ParkingSpace
- ParkingSession
- Reservation
- AuditLog
- ParkingRateSetting

### ParkingManagement.UI

WPF-Anwendung mit:

- Views
- ViewModels
- Services
- MVVM-Logik
- Benutzeroberfläche
- Datenbankzugriff
- Named-Pipe-Client

### ParkingMonitoringService

Separater .NET Worker Service für:

- Hintergrundüberwachung
- Reservierungsstatus
- Datenbankzugriffe
- Named-Pipe-Server
- Benachrichtigungen an die WPF-Anwendung

---

## 13. Projekt aus dem Quellcode öffnen

Der vollständige Quellcode wird zusätzlich unter `Source` mit abgegeben.

Für die Entwicklung aus dem Quellcode werden benötigt:

- Visual Studio 2022 oder neuer bzw. eine geeignete .NET-Entwicklungsumgebung
- .NET 8 SDK
- SQL Server Express LocalDB

Die Solution kann über folgende Datei geöffnet werden:

`ParkingManagement.sln`

Anschließend können die benötigten NuGet-Pakete wiederhergestellt und das Projekt kompiliert werden.

Für die normale Programmausführung ist dieser Schritt nicht erforderlich, da eine fertig veröffentlichte Release-Version mitgeliefert wird.

---

## 14. Externe Bibliotheken

Das Projekt verwendet externe NuGet-Pakete und Bibliotheken.

Die verwendeten Bibliotheken, Versionen, Einsatzzwecke und Quellen werden separat dokumentiert unter:

`EXTERNAL_LIBRARIES.md`

Die für die Programmausführung benötigten Bibliotheken befinden sich ebenfalls im self-contained Release-Verzeichnis.

---

## 15. Zuordnung zu den Universitätsanforderungen

### Graphische Benutzeroberfläche

**Technologie:** WPF

Die vollständige grafische Benutzeroberfläche von GOPark wurde mit Windows Presentation Foundation umgesetzt.

### SQL-Server-Anbindung

**Technologie:** SQL Server Express LocalDB und Entity Framework Core

Persistente Daten wie Benutzer, Fahrzeuge, Parkplätze, Parkvorgänge, Reservierungen, Einstellungen und Audit-Einträge werden in SQL Server gespeichert.

### Asynchrone Programmierung

**Technologie:** async/await

Asynchrone EF-Core-Methoden und Tasks werden verwendet, damit Datenbankoperationen die Benutzeroberfläche nicht blockieren.

### Dienste

**Technologie:** .NET Worker Service

`ParkingMonitoringService` läuft als separater Hintergrundprozess und übernimmt zeitabhängige Überwachungsaufgaben.

### Kommunikation zwischen Programmen

**Technologie:** Named Pipes

Die WPF-Anwendung und der Worker Service kommunizieren über Named Pipes miteinander.

Damit verwendet GOPark neben der erforderlichen grafischen Benutzeroberfläche mehrere der im Modul behandelten Technologien.

---

## 16. Abgabestruktur

Die finale Abgabe ist wie folgt aufgebaut:

GOPark_Abgabe/
- start.bat
- README.md
- EXTERNAL_LIBRARIES.md
- Release/
  - GOPark/
    - ParkingManagement.UI.exe
    - appsettings.json
    - Worker/
      - ParkingMonitoringService.exe
- Source/
  - ParkingManagement.sln
  - ParkingManagement.Data/
  - ParkingManagement.UI/
  - ParkingMonitoringService/
- Database/
  - GOPark.sql
  - README_Database.txt
- Beleg/
  - GOPark_Beleg.pdf