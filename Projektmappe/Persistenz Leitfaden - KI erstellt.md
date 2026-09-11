# Leitfaden Datenpersistenz (Phase 2)

Diese Datei begleitet Phase 2 des Projekts: die Anbindung einer Datenbank an die bestehenden
Modellklassen. Sie erklärt die Reihenfolge der Schritte und vor allem das Warum dahinter, damit
die Entscheidungen später in der Projektdokumentation begründet werden können. Der Code wird
selbst geschrieben, hier stehen nur Strukturen und Signaturen.

Bezug: [Projektauftrag - KI erstellt.md](Projektauftrag%20-%20KI%20erstellt.md),
Testdaten unter `Testdaten/`, Modellklassen unter `Digitale_Geraeteliste/Core/Model/`.

## 0. Zwei Blocker vorab

Beide kosten je zwei Minuten und verhindern sonst stundenlange Fehlersuche.

**Zugriffsmodifikatoren.** Entity Framework bildet per Konvention ausschließlich öffentliche
Properties ab. Aktuell sind die Klassen `public`, die Properties aber `internal`. EF würde die
Tabellen also mit einer einzigen Spalte anlegen oder gar nicht erst starten. Alle Entitäten und
ihre Properties gehören auf `public`, `Category` eingeschlossen.

**Parameterlose Konstruktoren.** `Employee` und `LendItem` haben nur Konstruktoren mit
Pflichtparametern. EF Core erzeugt Objekte beim Laden aus der Datenbank über Reflection und
braucht dafür einen parameterlosen Konstruktor. Er darf `private` sein, dann bleibt der fachliche
Konstruktor die einzige öffentliche Möglichkeit, ein Objekt zu erzeugen:

```csharp
private LendItem() { }
```

## 1. Die Entscheidung: SQLite mit EF Core

Zwei Wege wären denkbar. Die CSV-Dateien könnten direkt als Datenquelle dienen, was kurzfristig
einfacher wäre, aber weder Abfragen noch Transaktionen noch ein sinnvolles ERD hergibt. Der
Projektauftrag nennt SQLite, und eine Prüfungsdokumentation mit Entity-Relationship-Modell ohne
Datenbank dahinter wirkt inkonsistent.

Die CSV-Dateien behalten trotzdem eine Aufgabe: Sie sind die Quelle für die einmalige
Erstbefüllung der Datenbank, nicht das Speicherformat im Betrieb.

## 2. Warum eine Schicht dazwischen

Der Kern der Aufgabe ist nicht, Daten in eine Datei zu schreiben, sondern die
Persistenztechnologie von der Fachlogik fernzuhalten.

Wenn die Geschäftsregeln direkt auf einem `DbContext` arbeiten, braucht jeder Unit-Test eine
laufende Datenbank. Liegt dagegen eine Schnittstelle dazwischen, lässt sich im Test eine
einfache Listen-Implementierung einsetzen, und `IsOverdueAt` oder die Prüfung von R1 sind ohne
Datei und ohne SQL testbar. Genau das ist in der Dokumentation zu begründen, und es ist der
Grund für die Ordnerstruktur:

```
Digitale_Geraeteliste/
  Core/
    Model/          Entitäten, kennen keine Datenbank
    Interfaces/     IItemRepository, IEmployeeRepository, ILendItemRepository
  Data/
    AusleihContext.cs       EF Core
    Repositories/           Implementierungen der Interfaces
    CsvImporter.cs          Erstbefüllung
```

`Core` darf `Data` nicht kennen. `Data` kennt `Core`. Diese Richtung ist die ganze Regel.

## 3. Pakete installieren

Im Ordner `Digitale_Geraeteliste` ausführen:

```
dotnet add package Microsoft.EntityFrameworkCore.Sqlite
dotnet add package Microsoft.EntityFrameworkCore.Design
dotnet tool install --global dotnet-ef
```

Das erste Paket ist der SQLite-Anbieter und bringt EF Core mit. Das zweite wird nur zur
Entwurfszeit gebraucht, nämlich vom Migrationswerkzeug. Das dritte ist das
Kommandozeilenwerkzeug `dotnet ef` selbst, es wird einmalig global installiert.

## 4. Der DbContext

Ein `DbContext` ist zweierlei: die Verbindung zur Datenbank und ein Gedächtnis. Er merkt sich
jedes geladene Objekt und erkennt beim Speichern selbst, was sich geändert hat. Deshalb gibt es
kein `Update`, sondern nur ein `SaveChanges`.

```csharp
public class AusleihContext : DbContext
{
    public DbSet<Item> Items => Set<Item>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<LendItem> LendItems => Set<LendItem>();

    protected override void OnConfiguring(DbContextOptionsBuilder options) { }
    protected override void OnModelCreating(ModelBuilder modelBuilder) { }
}
```

Jedes `DbSet` wird zu einer Tabelle. In `OnConfiguring` wird der Verbindungsstring gesetzt, in
`OnModelCreating` alles, was über die Konventionen hinausgeht.

**Wo liegt die Datenbankdatei?** Nicht im Projektordner, denn der ist zur Laufzeit nicht
zuverlässig beschreibbar. Der übliche Ort ist das Anwendungsdatenverzeichnis des Benutzers,
ermittelt über `Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)`. Für
die Entwicklung reicht auch `AppContext.BaseDirectory`, also der Ausgabeordner neben der EXE.
Die Entscheidung gehört in die Dokumentation, weil sie die Installierbarkeit betrifft.

## 5. Beziehungen konfigurieren

Hier liegt der Stolperstein dieses Datenmodells. `LendItem` hat **zwei** Verweise auf
`Employee`: `BorrowedBy` und `LendBy`. EF Core kann bei zwei Beziehungen zwischen denselben
beiden Entitäten nicht erraten, welche Fremdschlüsselspalte zu welcher Navigation gehört, und
meldet beim ersten Start einen Fehler. Diese Beziehungen müssen in `OnModelCreating` explizit
beschrieben werden.

Der praktischere Weg ist, die Fremdschlüssel zusätzlich als eigene Properties zu führen:

```csharp
public int ItemId { get; set; }
public Item Item { get; set; }
public int BorrowedById { get; set; }
public Employee BorrowedBy { get; set; }
public int LendById { get; set; }
public Employee LendBy { get; set; }
```

Das hat drei Vorteile: EF erkennt die Zuordnung ohne weitere Konfiguration, der CSV-Import kann
die Ids direkt setzen ohne die zugehörigen Objekte zu laden, und in der Bestandsübersicht lässt
sich nach `ItemId` filtern, ohne das ganze Gerät nachzuladen.

`Item.Category` ist als `Category?` deklariert und damit eine optionale Beziehung. Das ist eine
bewusste Aussage: Ein Gerät darf ohne Kategorie existieren. Falls das nicht gewollt ist, sollte
die Property nicht nullable sein und eine `CategoryId` bekommen.

## 6. Migration erzeugen

```
dotnet ef migrations add InitialCreate
dotnet ef database update
```

Der erste Befehl vergleicht das aktuelle Modell mit dem letzten bekannten Stand und erzeugt im
Ordner `Migrations` eine Klasse mit `Up` und `Down`. Dort steht in C#, welche Tabellen und
Spalten angelegt werden. Diese Datei ist lesenswert, sie ist der beste Beleg dafür, ob das
Mapping so verstanden wurde wie beabsichtigt, und eignet sich als Auszug für die Dokumentation.

Der zweite Befehl führt die Migration gegen die Datenbank aus und legt die Datei an. EF merkt
sich in der Tabelle `__EFMigrationsHistory`, welche Migrationen bereits gelaufen sind.

**Wichtig:** Nach jeder Änderung am Modell braucht es eine neue Migration. Wird das vergessen,
läuft die Anwendung gegen ein veraltetes Schema und meldet fehlende Spalten.

## 7. Vor dem Weiterbauen: nachsehen

An dieser Stelle nicht direkt weiterprogrammieren, sondern die erzeugte `.db`-Datei öffnen, zum
Beispiel mit *DB Browser for SQLite*. Wenn vier Tabellen mit den erwarteten Spalten zu sehen
sind, stimmt das Mapping. Wenn Spalten fehlen, liegt es fast immer an den
Zugriffsmodifikatoren aus Abschnitt 0.

Dieser Zwischenschritt spart erfahrungsgemäß die meiste Zeit, weil ein falsches Mapping sonst
erst drei Schichten später auffällt.

## 8. Erstbefüllung aus den CSV-Dateien

Die Reihenfolge ist durch die Fremdschlüssel vorgegeben: zuerst `kategorien.csv`, dann
`geraete.csv`, dann `mitarbeiter.csv`, zuletzt `ausleihen.csv`. Ein Gerät kann nicht auf eine
Kategorie verweisen, die es noch nicht gibt.

Der Import sollte nur laufen, wenn die Datenbank leer ist, sonst entstehen bei jedem Programmstart
Duplikate. Eine Prüfung auf `if (!context.Categories.Any())` genügt.

Zum Format: Trennzeichen ist das Semikolon, Zeichensatz UTF-8 ohne BOM, die erste Zeile ist die
Kopfzeile und muss übersprungen werden. Beim Einlesen ist auf zwei Dinge zu achten. Die Datumswerte
stehen im Format `yyyy-MM-dd` und sollten mit `DateTime.ParseExact` und
`CultureInfo.InvariantCulture` gelesen werden, sonst hängt das Ergebnis von den Regionaleinstellungen
des Rechners ab. Und `ActualReturnDate` ist bei offenen Ausleihen leer, ein leeres Feld muss also zu
`null` werden und nicht zu einem Fehler.

## 9. Repositories

Erst jetzt die Schnittstellen, und bewusst klein. Nur das, was A1 bis A7 tatsächlich brauchen:

```csharp
public interface IItemRepository
{
    List<Item> GetAll();
    Item? GetById(int id);
    bool InventoryNumberExists(int number, int exceptId);
    void Save(Item item);
}

public interface ILendItemRepository
{
    LendItem? GetOpenLend(int itemId);
    List<LendItem> GetAllOpen();
    void Save(LendItem lend);
}
```

`GetOpenLend` ist die Methode, an der R1 hängt: Liefert sie einen Treffer, ist das Gerät verliehen
und darf nicht erneut ausgegeben werden. `GetAllOpen` ist die Grundlage für A6.

Eine Falle bei EF Core: Verknüpfte Objekte werden standardmäßig **nicht** mitgeladen. Eine Ausleihe
kommt ohne ihr `Item` und ohne ihren `Employee` zurück, beide sind `null`. Wer sie braucht, muss sie
mit `.Include(l => l.Item)` anfordern. In der Überfälligkeitsliste stehen Gerätename und
Mitarbeitername, also wird dort `Include` gebraucht.

## 10. Prüfen ohne Oberfläche

Das UI kommt erst in Phase 4. Bis dahin lässt sich die Persistenz im Startcode der Anwendung
prüfen: Daten laden, Anzahl und ein paar Datensätze mit `System.Diagnostics.Debug.WriteLine`
ausgeben und im Ausgabefenster von Visual Studio ansehen. Erwartet werden 8 Kategorien, 120 Geräte,
45 Mitarbeiter und 53 Ausleihen, davon 21 offene und 6 überfällige.

Stimmen diese Zahlen, ist Phase 2 abgeschlossen.

## Häufige Fehlerbilder

| Symptom | Ursache |
|---|---|
| Tabellen ohne Spalten oder EF startet nicht | Properties sind `internal` statt `public` |
| `No suitable constructor found` | kein parameterloser Konstruktor |
| Fehler über mehrdeutige Beziehungen zu `Employee` | die zwei Verweise aus Abschnitt 5 sind nicht konfiguriert |
| Verknüpfte Objekte sind `null` | `Include` fehlt |
| `SQLite Error: no such column` | Modell geändert, aber keine neue Migration erzeugt |
| Datumswerte verschoben oder Parse-Fehler | ohne `InvariantCulture` gelesen |

## Zeitrahmen

Phase 2 ist im Projektauftrag mit einer Stunde veranschlagt. Realistisch ist das, wenn Abschnitt 0
vorher erledigt ist und beim ersten Mapping-Fehler nicht lange gesucht wird. Der Zwischenschritt
aus Abschnitt 7 ist deshalb keine verlorene Zeit.
