# EF Core und LINQ Schritt für Schritt

Arbeitsanleitung für den Weg vom `LendContext` bis zu funktionierenden Repositories und dem CSV-Import.
Sie ergänzt den [Persistenz Leitfaden - KI erstellt.md](Persistenz%20Leitfaden%20-%20KI%20erstellt.md)
um die Syntax, die dort vorausgesetzt wird. Jeder Abschnitt erklärt ein Konzept, zeigt es an einem
Beispiel mit einer **anderen** Entität als der, die umgesetzt werden soll, und endet mit einer Aufgabe
und Leitfragen. Lösungen stehen hier bewusst nicht.

## Aktueller Stand

| Baustein | Stand |
|---|---|
| `LendContext` mit vier `DbSet`s und `UseSqlite` | erledigt |
| Kontext wird an die Repositories übergeben | erledigt |
| `IsInUse` entfernt, Status kommt aus der offenen Ausleihe | erledigt |
| Migrationen `InitialCreate` und `RestrictDeleteBehaviour` | erledigt, Schema geprüft |
| Löschweitergabe auf `Restrict` für alle vier Fremdschlüssel | erledigt |
| `Employee.Email` und zweiter Konstruktor entfernt (YAGNI, Schritt 2c) | erledigt, Migration `RemoveEmployeeEmail` angewendet |
| `required` entfernt, Startwerte bzw. `= null!` | erledigt |
| Repository-Variablen in `App.xaml.cs` umbenannt | erledigt |
| `ItemRepository`: `GetAllItems`, `CreateNewItem`, `ChangeItem`, Duplikatprüfung | erledigt, offen nur der allgemeine `catch`-Block in `ChangeItem` (1.6) |
| `ItemRepository.GetItemById` | gibt `null` über `?? null!` zurück, Rückgabetyp sagt aber `Item`, siehe 1.6 |
| `EmployeeRepository`: `GetEmployeeByID`, `ChangeEmployee`, `CreateNewEmployee` | erledigt, Namensreihenfolge korrigiert |
| `LendItem` ohne `LendItemToEmployee`, Konstruktor ohne `id`, Fristberechnung im Konstruktor | erledigt |
| `CSVImporter` für alle vier Dateien, Ids aus der CSV | erledigt |
| CSV-Dateien unter `Data/Testdata`, Kopieren ins Ausgabeverzeichnis | erledigt, im Ausgabeordner geprüft |
| `ILendItemRepository` an neue Signaturen angepasst | erledigt, Build grün |
| Import beim Start in `App.OnStartup` | **erledigt und geprüft**: 8 Kategorien, 120 Geräte, 45 Mitarbeiter, 53 Ausleihen, 21 offen, Umlaute korrekt, keine Fremdschlüsselverstöße |
| Logging: Ordner beim Start, Dateiname beim Schreiben, `InnerException` | erledigt |
| `ChangeLendItem`: erst speichern, dann loggen | erledigt |
| `Include` nur noch auf Navigationseigenschaften, `Find` für einzelne Entitäten ohne Verweise | erledigt |
| `ChangeLendItem` lädt `BorrowedBy` und `LendBy` mit | erledigt im Code, **zur Laufzeit noch nicht geprüft** (zweiter Programmstart) |
| `GetLendItemById` mit `Include` für Gerät und beide Mitarbeiter | erledigt |
| `ChangeLendItem`: neue `ItemId` im Log | erledigt, Text wird jetzt nach `SaveChanges` gebaut |
| `ChangeLendItem` ohne eigenes Anlegen des `Logs`-Ordners | funktioniert nur, weil der Import vorher lief, versteckte Abhängigkeit |
| "Nicht gefunden" einheitlich als `ArgumentException` | erledigt in allen Such-Methoden, **Ausnahme:** `ReturnLendItem` tut bei unbekannter Id still nichts |
| `GetAllOverdueLendItems` mit `Include` | erledigt, filtert weiterhin im Arbeitsspeicher (1.3) |
| `GetAllLendItems` | gibt das `DbSet` ohne `Include` und ohne `ToList` zurück |
| `GetOpenLendByItemId` für R1 | umgesetzt, gibt eine Menge statt höchstens eines Elements zurück und wird nicht ausgeführt (1.2, 1.3) |

**Nächster Arbeitsschritt:** Den Laufzeittest mit zweitem Programmstart nachholen. Dann `GetOpenLendByItemId`
und `ReturnLendItem` nachziehen. Damit ist die Datenschicht fertig, und Phase 3 mit dem `LendService` beginnt.

---

## Teil 1: Die Werkzeuge verstehen

### 1.1 Lambda-Ausdrücke

Fast jede LINQ-Methode erwartet eine kleine Funktion als Parameter. Die kurze Schreibweise dafür ist
der Lambda-Ausdruck:

```csharp
c => c.Name == "Messtechnik"
```

Lies den Pfeil als "wird zu". Links steht ein frei gewählter Name für *ein einzelnes Element*,
rechts steht, was mit diesem Element berechnet wird. Der Ausdruck oben heißt also: "Nimm eine
Kategorie, nenne sie `c`, und liefere `true`, wenn ihr Name Messtechnik ist."

Der Name links ist beliebig. Üblich ist der Anfangsbuchstabe des Typs, und innerhalb einer
Methodenkette sollte er gleich bleiben.

**Aufgabe:** Schreibe auf Papier drei Lambda-Ausdrücke für ein `Item`, die jeweils `true` liefern,
wenn (a) das Gerät die Id 5 hat, (b) das Gerät ausgemustert ist, (c) das Gerät in Wartung ist und
nicht ausgemustert ist.

### 1.2 Die LINQ-Methoden, die du brauchst

| Methode | Liefert | Frage, die sie beantwortet |
|---|---|---|
| `Where(lambda)` | eine Menge | Welche Elemente erfüllen die Bedingung? |
| `First(lambda)` | genau ein Element, **wirft eine Exception**, wenn keines passt | Welches ist das erste? Ich bin sicher, dass es eines gibt. |
| `FirstOrDefault(lambda)` | ein Element oder `null` | Welches ist das erste, falls es eines gibt? |
| `Any(lambda)` | `bool` | Gibt es mindestens eines, das passt? |
| `OrderBy(lambda)` | eine sortierte Menge | In welcher Reihenfolge? Hier liefert das Lambda den Sortierwert. |
| `ToList()` | eine `List<T>` | Jetzt wirklich ausführen und alles einsammeln. |

Der Unterschied zwischen `First` und `FirstOrDefault` ist eine Aussage über deine Erwartung. Mit
`First` sagst du: "Wenn es keinen Treffer gibt, ist etwas grundlegend kaputt." Mit `FirstOrDefault`
sagst du: "Kein Treffer ist ein normaler Fall, um den sich der Aufrufer kümmert." Mehr dazu in 1.6.

**Aufgabe:** Welche Methode brauchst du für "Ist die Inventarnummer 1042 schon vergeben?" Welche für
"Gib mir das Gerät mit der Id 17"?

### 1.3 Der wichtigste Unterschied: Datenbank oder Arbeitsspeicher

Auf einem `DbSet` wird **nichts sofort ausgeführt**. EF sammelt die Aufrufe und übersetzt sie in
**eine** SQL-Abfrage. Erst Methoden wie `ToList()`, `First()`, `FirstOrDefault()` oder `Any()`
schicken diese Abfrage tatsächlich an die Datenbank.

```csharp
context.Categories.Where(c => c.Name == "Messtechnik").ToList();
```

wird ungefähr zu `SELECT * FROM Categories WHERE Name = 'Messtechnik'`. Nach einem `ToList()` liegt
eine normale Liste im Arbeitsspeicher, und alles Weitere läuft in C#.

Daraus folgt: **Erst filtern, dann `ToList()`.** Aktuell rufen `GetItemById` und
`CheckInventoryNumberDuplicate` zuerst `GetAllItems()` auf, das mit `ToList()` endet. Die Suche läuft
danach im Speicher über alle Geräte.

Eine zweite Folge: EF kann nur übersetzen, was es als Spalte kennt. Die berechnete Property
`LendItem.IsOverdue` existiert in der Datenbank nicht. `Where(l => l.IsOverdue)` auf dem `DbSet`
bricht zur Laufzeit mit *"could not be translated"* ab.

**Aufgabe:** Wie sieht `GetItemById` aus, wenn die Abfrage direkt auf `_context.Items` läuft? Welche
Zeile deiner jetzigen Methode fällt dann weg?

### 1.4 Include: verknüpfte Objekte mitladen

EF lädt Navigationseigenschaften standardmäßig **nicht** mit. Ein geladenes `Item` hat eine korrekte
`CategoryId`, aber `Category` ist `null`, solange nicht angefordert:

```csharp
context.LendItems.Include(l => l.BorrowedBy).ToList();
```

`Include` funktioniert **nur mit Navigationseigenschaften**, also Properties, deren Typ selbst eine
Entität ist.

**Aufgabe:** Wenn `GetItemById` direkt auf `_context.Items` abfragt, fehlt das `Include`. Brauchst du
die Kategorie beim Laden eines einzelnen Geräts? Denk an den Bearbeiten-Dialog aus A1.

### 1.5 Speichern: Add, Update, SaveChanges

Der `DbContext` merkt sich jedes Objekt, das er geladen hat (**tracked**). Änderst du danach eine
Property, sieht er das selbst, und `SaveChanges` schreibt genau diese Spalte. Ein `Update` ist dafür
nicht nötig.

Neue Objekte werden mit `Add` angemeldet. Die `Id` darf dabei 0 sein, SQLite vergibt sie, und nach
`SaveChanges` steht der Wert im Objekt. Die Zeilen `Id = 0;` in deinen Konstruktoren sind deshalb
überflüssig, `0` ist ohnehin der Startwert jedes `int`.

`Update` ist für Objekte gedacht, die der Kontext **nicht** geladen hat. Es markiert alle Spalten als
geändert, und bei `Id` 0 legt es das Objekt neu an. Deine Methode `UpdateItem` deckt dadurch neue und
bestehende Geräte ab. In `ChangeItem` ist das Gerät aber schon geladen, dort wäre `Update` nicht nötig.

**Aufgabe:** Die Methode heißt `UpdateItem`, legt aber auch neue Geräte an. Passt der Name?

### 1.6 Exceptions: wann werfen, wann nicht

"Zu dieser Id gibt es kein Gerät" ist meistens ein erwartbarer Ausgang, kein Ausnahmezustand. Der
Aufrufer sollte ihn mit `if (item == null)` behandeln können.

Deine Version von `GetItemById` macht daraus eine Exception, weil `First` wirft. Dazu kommen zwei
Punkte: `catch (Exception ex)` fängt auch "Datenbank nicht erreichbar" und versieht es mit der Meldung
*"Check for Typo"*, und `throw new Exception(...)` verwendet den allgemeinsten Typ. Benutzertexte wie
*"Contact your system administrator"* gehören außerdem nicht ins Repository, sondern später ins
ViewModel. Richtig ist, dass du die ursprüngliche Exception als `ex` weitergibst.

Die Alternative an einer Kategorie:

```csharp
public Category? GetCategoryById(int id)
{
    return _context.Categories.FirstOrDefault(c => c.Id == id);
}
```

**Aufgabe:** Welche Vorteile hat es für den Aufrufer, wenn `GetItemById` bei "nicht gefunden" `null`
liefert? Was muss sich dafür am Rückgabetyp im Interface ändern?

### 1.7 `required` und Konstruktoren

`required` heißt: **Wer ein Objekt mit `new` erzeugt, muss diese Property im Objektinitialisierer
setzen.** Der Compiler schaut dabei **nicht** in den Konstruktor hinein.

```csharp
var c1 = new Category("Messtechnik");                            // CS9035
var c2 = new Category("Messtechnik") { Name = "Messtechnik" };   // kompiliert
```

Bei dir betrifft das `Item.InventoryNumber` sowie `BorrowedBy`, `Item` und `LendBy` in `LendItem`.
Bisher fällt es nicht auf, weil noch niemand `new Item(...)` aufruft. Der Import wird der erste sein.

Drei Auswege:

- **`required` entfernen**, bei `InventoryNumber` einen Startwert wie bei `Name`, bei den
  Navigationseigenschaften `= null!;`. Der Konstruktor sorgt für vollständige Objekte.
- **`[SetsRequiredMembers]` am Konstruktor** (Namespace `System.Diagnostics.CodeAnalysis`). Der
  Compiler glaubt dem Attribut, prüft aber nicht nach.
- **Konstruktor entfernen** und nur mit Objektinitialisierern arbeiten.

**Aufgabe:** Entscheide dich. Leitfrage: Wo soll sichergestellt werden, dass ein Gerät vollständig ist,
beim Konstruktor oder beim Aufrufer?

### 1.8 Wie Migrationen funktionieren

| Datei | Aufgabe |
|---|---|
| `<Zeitstempel>_<Name>.cs` | `Up` und `Down`: was beim Anwenden bzw. Rückgängigmachen passiert |
| `<Zeitstempel>_<Name>.Designer.cs` | das Modell zum Zeitpunkt dieser Migration |
| `LendContextModelSnapshot.cs` | das Modell nach **allen** Migrationen, EFs Gedächtnis |

`migrations add` vergleicht das Modell mit dem **Snapshot**, nicht mit der Datenbank.
`database update` schaut in `__EFMigrationsHistory`, welche Migrationen schon gelaufen sind.

Regeln: Migrationsdateien nie einzeln löschen, sondern mit `dotnet ef migrations remove` zurücknehmen.
Nach jeder Modelländerung eine neue Migration. Bei SQLite baut EF für geänderte Fremdschlüssel die
Tabelle neu auf, deshalb die Warnung zu `PRAGMA foreign_keys = 0`. Vor Schemaänderungen an einer
Datenbank mit echten Daten die `.db`-Datei sichern.

### 1.9 Optionale Parameter statt doppelter Konstruktoren

Dieser Abschnitt ist neu und gehört zu Schritt 2c.

Ein Parameter kann einen Standardwert bekommen. Dann darf der Aufrufer ihn weglassen:

```csharp
public Category(string name, string? description = null)
```

`new Category("Messtechnik")` und `new Category("Messtechnik", "Mess- und Prüfgeräte")` rufen beide
denselben Konstruktor auf. Im ersten Fall ist `description` einfach `null`.

Optionale Parameter müssen am Ende der Parameterliste stehen. Wenn zwei Konstruktoren sich nur darin
unterscheiden, dass einer einen Wert mehr hat, ersetzt ein optionaler Parameter den zweiten
Konstruktor, und die übrigen Zuweisungen stehen nur noch einmal im Code.

**Aufgabe:** Deine beiden `Employee`-Konstruktoren unterscheiden sich nur durch `email`. Wie sähe ein
einzelner Konstruktor aus? Welche Zeilen verschwinden dadurch?

---

## Teil 2: Der Weg im Projekt

### Schritt 1: Kontext verbinden — erledigt

`OnConfiguring` baut den Pfad mit `Path.Combine(AppContext.BaseDirectory, ...)`. Die Prüfung
`if (!options.IsConfigured)` erlaubt es später, dem Kontext für Tests andere Optionen mitzugeben.
Das `using Microsoft.Extensions.Options;` wird nicht gebraucht.

### Schritt 2: Das Modell fertig machen

**2a: `required` gegen Konstruktoren** — offen und jetzt der wichtigste Punkt, siehe 1.7. Betroffen
sind `Item` und `LendItem`. Solange das nicht entschieden ist, kompiliert der Import nicht, sobald er
das erste Gerät erzeugt.

Leitfrage zum `Item`-Konstruktor: Er setzt `CategoryId = category.Id`. Beim Import aus der CSV kennst
du die `CategoryId` als Zahl. Musst du dafür erst ein `Category`-Objekt laden, oder reicht die Id?
Was bedeutet das für die Parameter des Konstruktors?

**2b: `IsInUse`** — erledigt. Die Property ist entfernt, ob ein Gerät verliehen ist, ergibt sich aus
der offenen Ausleihe. Der auskommentierte Rest in `Item.cs` Zeile 19 kann weg; die Begründung gehört
in die Doku unter "Abweichungen vom Entwurf".

**2c: `Email` bei `Employee`** — im Modell und in der Datenbank, aber nicht in `mitarbeiter.csv`.

Zunächst die Frage, ob die Email überhaupt gebraucht wird. Keine der Anforderungen A1 bis A7 nennt
sie. Der CSV-Export aus A7 enthält die **Überfälligkeitsliste**, also Gerät, Mitarbeitername und
Überschreitung in Tagen, keine Kontaktdaten. Für eine Vorbereitung auf den Export ist sie nicht nötig.

Das ist ein bekanntes Muster, das in der Softwareentwicklung einen Namen hat: **YAGNI**, "You Aren't
Gonna Need It". Funktionen, die "vielleicht später" gebraucht werden, kosten jetzt Zeit, erzeugen
Code, der getestet und dokumentiert werden muss, und werden später oft anders gebraucht als gedacht.
Bei einem Budget von acht Stunden ist das ein echtes Risiko, und in der Doku ist eine bewusste
Abgrenzung stärker als eine halb genutzte Funktion.

Zwei vertretbare Wege:

- **Email wieder entfernen.** Neue Migration, zweiter Konstruktor fällt weg, der Import wird
  einfacher. In der Doku als möglicher Folgeauftrag erwähnen, etwa für Mahnungen per Mail.
- **Email behalten, aber einfach halten.** Ein Konstruktor mit optionalem Parameter nach 1.9 statt
  zwei Konstruktoren. Der Import prüft **nicht**, ob es eine Spalte gibt, und ruft keinen
  unterschiedlichen Konstruktor auf, sondern übergibt die Email nur dann, wenn die Datei sie liefert.

Warum die Idee mit `values[3]` so nicht funktioniert: In `mitarbeiter.csv` steht an Index 3 der
`FullName`, und diese Spalte ist in jeder Zeile gefüllt. Die Prüfung "ist `values[3]` vorhanden" wäre
also immer wahr, und der volle Name landete als Email in der Datenbank. Dazu kommt eine Eigenheit von
`Split`: Eine Zeile `a;b;c;` ergibt **vier** Elemente, das letzte ist ein leerer String. "Vorhanden"
und "gefüllt" sind zwei verschiedene Fragen.

Eine Kleinigkeit: `Email` ist als `string?` deklariert, hat aber den Startwert `string.Empty`. Damit
gibt es zwei Arten, "keine Email" auszudrücken, `null` und `""`. Entscheide dich für eine.

**Aufgabe:** Entscheide dich für einen der beiden Wege. Leitfrage: Kannst du in einem Satz sagen,
welche Anforderung die Email braucht?

### Schritt 3: Migrationen — erledigt

`InitialCreate` legt vier Tabellen an, `RestrictDeleteBehaviour` setzt alle vier Fremdschlüssel auf
`ON DELETE RESTRICT`. Geprüft in der Datenbank: Spalten stimmen, `IsInUse` ist weg, `Email` ist da,
`PRAGMA foreign_key_check` meldet keine Verstöße.

Für jede weitere Modelländerung, etwa aus 2a oder 2c, gilt der normale Weg:

```
dotnet ef migrations add <SprechenderName>
dotnet ef database update
```

Danach die neue `Up`-Methode lesen und prüfen, ob sie das enthält, was du geändert hast.

### Schritt 4: Kontext übergeben und zusammensetzen — erledigt

Offene Kleinigkeiten in `App.xaml.cs`: Die Variablen `employeeContext`, `itemContext` und
`lendItemContext` sind Repositories und sollten auch so heißen. Die Freigabe des Kontexts in `OnExit`
und das Festhalten der Repositories für das ViewModel folgen in Phase 4, siehe MVVM-Leitfaden
Abschnitt 6.

### Schritt 5: ItemRepository Methode für Methode

**GetAllItems** — erledigt.

**GetItemById** — drei Überarbeitungen empfohlen: direkt auf `_context.Items` abfragen (1.3), das
`Include` übernehmen (1.4), "nicht gefunden" als `null` statt Exception (1.6).

**UpdateItem** — funktionsfähig, Fragen aus 1.5.

**ChangeItem** — Entscheidung offen: behalten (Repository kennt den Bearbeitungsvorgang) oder
streichen (Aufrufer setzt Properties und speichert). Unabhängig davon lädt die Methode ein Gerät, das
ihr schon übergeben wurde, ein zweites Mal.

**CheckInventoryNumberDuplicate** — offen. Direkt auf `_context.Items` abfragen, und das gerade
bearbeitete Gerät ausnehmen, sonst meldet das Speichern eines unveränderten Geräts ein Duplikat.
Leitfrage: Wie sieht ein `Any`-Lambda aus, das "gleiche Nummer **und** andere Id" prüft?

### Schritt 6: Der CSV-Import

Voraussetzung: 2a und 2c sind entschieden.

**6a: Mitarbeiter reparieren.** `GetEmployeesFromCSV` erzeugt direkt `Employee`-Objekte, das ist der
richtige Ansatz. Die Spaltenzuordnung stimmt aber nicht. Die Kopfzeile lautet:

```
Id;LastName;FirstName;FullName;Department
```

**Aufgabe:** Schreib für `1;Krüger;Thomas;Krüger, Thomas;Elektroinstallation` auf, welcher Wert mit
deinem jetzigen Code in welcher Property landen würde. Welche Indizes wären die richtigen?

**6b: Die Id-Frage.** Das ist die wichtigste Entscheidung beim Import. `ausleihen.csv` verweist über
`BorrowedById` und `LendById` auf die Ids aus `mitarbeiter.csv`. Vergibt SQLite beim Import neue Ids,
stimmen diese Verweise nicht mehr, sobald die Reihenfolge abweicht oder einmal etwas schiefgeht.

Zwei Wege:

- **Ids aus der CSV übernehmen.** Das Objekt bekommt vor dem `Add` die `Id` aus der Datei. SQLite
  akzeptiert einen vorgegebenen Primärschlüssel. Die Verweise bleiben gültig.
- **Neue Ids vergeben und eine Zuordnung merken.** Beim Import ein `Dictionary<int, Employee>` von
  alter Id auf neues Objekt führen und beim Ausleihen-Import darüber nachschlagen.

Leitfrage: Welcher Weg ist für einen einmaligen Import einfacher, und welcher wäre robuster, wenn
später Daten aus einem zweiten Lager dazukommen?

**6c: Reihenfolge und Speichern.** Kategorien, dann Geräte, dann Mitarbeiter, dann Ausleihen. Die
Importer-Methoden liefern Listen. Wer diese Listen in die Datenbank schreibt, der Importer selbst
oder die Repositories? Leitfrage: Welche Klasse kennt den `LendContext` bereits?

**6d: Nur bei leerer Datenbank.** Der Import darf nur laufen, wenn noch keine Daten da sind, sonst
entstehen Duplikate. Leitfrage: Mit welcher LINQ-Methode aus 1.2 prüfst du "gibt es schon Kategorien"?

**6e: Datumswerte in `ausleihen.csv`.** Format `yyyy-MM-dd`, lesen mit `DateTime.ParseExact` und
`CultureInfo.InvariantCulture`. `ActualReturnDate` ist bei offenen Ausleihen ein leerer String und
muss zu `null` werden.

Kleinigkeiten im Importer: `using System.Security.RightsManagement;` wird nicht gebraucht, und der
Kommentar zur Kopfzeile beschreibt eine Annahme über die Datei. Das ist eine gute Stelle für einen
Kommentar, weil man sie dem Code nicht ansieht.

### Schritt 7: Selbstkontrolle

Nach dem Import ins Ausgabefenster schreiben, wie viele Datensätze jede Tabelle hat. Erwartet werden
8 Kategorien, 120 Geräte, 45 Mitarbeiter und 53 Ausleihen, davon 21 offene und 6 überfällige.

Zusätzlich prüfen:

- Hat Mitarbeiter 1 den Nachnamen Krüger und die Abteilung Elektroinstallation?
- Liefert `GetItemById` für eine nicht vorhandene Id das Verhalten, das du in 1.6 entschieden hast?
- Meldet `CheckInventoryNumberDuplicate` für eine vorhandene Nummer mit **anderer** Id `true` und mit
  **eigener** Id `false`?

### Schritt 8: Übertragen auf LendItemRepository

**Offene Ausleihe zu einem Gerät** — fehlt noch im Interface, R1 hängt daran. Leitfragen: Welche zwei
Bedingungen machen eine Ausleihe zu "offen für Gerät X"? Welche LINQ-Methode passt, wenn keine auch
ein normaler Fall ist?

**GetAllOverdueLendItems** — Leitfragen: Warum nicht `Where(l => l.IsOverdue)` (1.3)? Aus welchen
zwei Spalten setzt sich "überfällig" zusammen? Wie viele `Include`-Aufrufe braucht die Liste für A6?

---

## Fehlerbilder

| Meldung | Bedeutung |
|---|---|
| `CS9035: Required member ... must be set in the object initializer` | `required`-Property wird nur im Konstruktor gesetzt, siehe 1.7 |
| `IndexOutOfRangeException` im Importer | Zeile hat weniger Spalten als der Index erwartet, siehe 2c |
| `SQLite Error 19: 'FOREIGN KEY constraint failed'` | Verweis auf eine Id, die nicht existiert, oder Löschen eines Datensatzes, auf den noch verwiesen wird (`Restrict`) |
| `SQLite Error 19: 'UNIQUE constraint failed: Employees.Id'` | Import zweimal gelaufen, siehe 6d |
| `String '...' was not recognized as a valid DateTime` | Datum ohne `ParseExact` und `InvariantCulture` gelesen, siehe 6e |
| `SQLite Error 1: 'no such column'` | Modell geändert, aber keine neue Migration |
| `Sequence contains no matching element` | `First` ohne Treffer, siehe 1.6 |
| `could not be translated` | berechnete C#-Property in einer Datenbankabfrage, siehe 1.3 |
| `NullReferenceException` auf `item.Category.Name` | `Include(i => i.Category)` fehlt |
| `The instance of entity type cannot be tracked because another instance with the same key` | zwei Kontexte oder dasselbe Objekt zweimal angemeldet |
