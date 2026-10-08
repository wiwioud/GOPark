GOPark – Datenbankhinweise
==========================

1. Verwendetes Datenbanksystem
------------------------------

GOPark verwendet Microsoft SQL Server Express LocalDB.

Standardinstanz:

(localdb)\MSSQLLocalDB

Standard-Datenbankname:

ParkingManagementDB


2. Automatische Datenbankinitialisierung
-----------------------------------------

Beim ersten Start der GOPark-Anwendung wird die Datenbank automatisch
über Entity Framework Core initialisiert.

Die vorhandenen Entity-Framework-Core-Migrationen werden automatisch
angewendet.

Zusätzlich werden die notwendigen Ausgangsdaten durch die Anwendung
angelegt.


3. Mitgelieferte SQL-Datei
--------------------------

Für die Abgabe wird zusätzlich folgende SQL-Datei bereitgestellt:

GOPark.sql

Diese Datei wurde aus den vorhandenen Entity-Framework-Core-Migrationen
erzeugt.

Sie enthält das Datenbankschema und kann zur manuellen Erstellung bzw.
Nachvollziehbarkeit der GOPark-Datenbank verwendet werden.


4. Automatisch angelegte Ausgangsdaten
---------------------------------------

Bei der automatischen Initialisierung werden unter anderem die für den
ersten Programmstart benötigten Ausgangsdaten angelegt.

Dazu gehören insbesondere:

- Administrator-Benutzer
- Mitarbeiter-Benutzer
- initiale Parkplätze
- grundlegende Systemeinstellungen

Die Passwörter der Benutzer werden nicht im Klartext gespeichert,
sondern mit BCrypt gehasht.


5. Standard-Testkonten
----------------------

Administrator:

Benutzername: admin
Passwort: Admin123!


Mitarbeiter:

Benutzername: employee
Passwort: Employee123!


6. Datenbankverbindung
----------------------

Die Datenbankverbindung ist nicht fest im Quellcode hinterlegt.

Sie kann über die Datei:

appsettings.json

konfiguriert werden.

Die Release-Version verwendet die Datei:

Release\GOPark\appsettings.json

Der Worker Service besitzt ebenfalls eine eigene Konfigurationsdatei
unter:

Release\GOPark\Worker\appsettings.json


7. Voraussetzungen
------------------

Auf dem Zielcomputer muss Microsoft SQL Server Express LocalDB
installiert sein.

Ein installiertes .NET SDK ist für die bereitgestellte Release-Version
nicht erforderlich, da GOPark als self-contained Windows-x64
Anwendung ausgeliefert wird.


8. Manuelle Verwendung von GOPark.sql
-------------------------------------

Falls die Datenbank manuell erstellt werden soll, kann die Datei
GOPark.sql beispielsweise über SQL Server Management Studio oder ein
anderes geeignetes SQL-Server-Werkzeug ausgeführt werden.

Im normalen Betrieb ist dies nicht notwendig, da GOPark die Datenbank
beim ersten Start automatisch initialisiert.


9. Hinweis zur Abgabe
---------------------

Die Datenbankbestandteile der Abgabe befinden sich im Ordner:

Database\

Enthalten sind:

- GOPark.sql
- README_Database.txt

Zusätzlich befinden sich die verwendeten Entity-Framework-Core-
Migrationen im Quellcode des Projekts ParkingManagement.Data.