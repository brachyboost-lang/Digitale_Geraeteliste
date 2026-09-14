# EF Core und LINQ Schritt für Schritt

Arbeitsanleitung für den Weg vom `LendContext` bis zu funktionierenden Repositories. Sie ergänzt den
[Persistenz Leitfaden - KI erstellt.md](Persistenz%20Leitfaden%20-%20KI%20erstellt.md) um die Syntax,
die dort vorausgesetzt wird. Jeder Abschnitt erklärt ein Konzept, zeigt es an einem Beispiel mit einer
**anderen** Entität als der, die umgesetzt werden soll, und endet mit einer Aufgabe und Leitfragen.
Lösungen stehen hier bewusst nicht.

## Aktueller Stand

| Baustein | Stand |
|---|---|
| `LendContext` mit vier `DbSet`s | erledigt |
| `OnConfiguring` mit `UseSqlite` und `Path.Combine` | erledigt |
| Kontext wird an die Repositories übergeben (`private readonly _context`) | erledigt |
| Kontext und Repositories werden in `App.OnStartup` erzeugt | erledigt, Hinweise in Schritt 4 |
| Öffentliche Konstruktoren für `Item` und `Category` | erledigt, **aber** Konflikt mit `required`, Schritt 2a |
| Datenbankschema in `Digitale_Geraeteliste.db` | korrekt angelegt |
| Migrationsdateien | **fehlerhaft**, `Up` ist leer und `Email` fehlt, Schritt 3 |
| Entscheidung `IsInUse` / `IsActive` | **offen**, Schritt 2b |
| `GetAllItems` mit `Include(i => i.Category)` | erledigt |
| `GetItemById` | umgesetzt, Überarbeitung empfohlen, Schritt 5 |
| `UpdateItem` | umgesetzt, Hinweise in 1.5 |
| `ChangeItem` | umgesetzt, Entscheidung offen, Schritt 5 |
| `CheckInventoryNumberDuplicate` | **offen**, Schritt 5 |
| `CSVImporter.GetEmployeesFromCSV` | umgesetzt, **Spalten verschoben**, Schritt 5b |

**Die zwei dringendsten Punkte** sind Schritt 3 (Migrationen reparieren) und Schritt 2a (`required`
gegen Konstruktor). Beide fallen im Moment nicht auf, weil die Datenbank leer ist und noch niemand
`new Item(...)` aufruft. Beide schlagen zu, sobald der Import läuft.

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

Der Name links ist beliebig. `c`, `cat` oder `category` bedeuten dasselbe. Üblich ist der
Anfangsbuchstabe des Typs.

**Aufgabe:** Schreibe auf Papier drei Lambda-Ausdrücke für ein `Item`, die jeweils `true` liefern,
wenn (a) das Gerät die Id 5 hat, (b) das Gerät ausgemustert ist, (c) das Gerät in Wartung ist und
nicht ausgemustert ist.

Leitfrage zu (c): Welcher Operator verknüpft zwei Bedingungen, die beide gelten müssen?

### 1.2 Die LINQ-Methoden, die du brauchst

| Methode | Liefert | Frage, die sie beantwortet |
|---|---|---|
| `Where(lambda)` | eine Menge | Welche Elemente erfüllen die Bedingung? |
| `First(lambda)` | genau ein Element, **wirft eine Exception**, wenn keines passt | Welches ist das erste? Ich bin sicher, dass es eines gibt. |
| `FirstOrDefault(lambda)` | ein Element oder `null` | Welches ist das erste, falls es eines gibt? |
| `Any(lambda)` | `bool` | Gibt es mindestens eines, das passt? |
| `OrderBy(lambda)` | eine sortierte Menge | In welcher Reihenfolge? Hier liefert das Lambda den Sortierwert, nicht `true`/`false`. |
| `ToList()` | eine `List<T>` | Jetzt wirklich ausführen und alles einsammeln. |

Beispiel mit Kategorien:

```csharp
var sortiert = categories.Where(c => c.Name.StartsWith("M")).OrderBy(c => c.Name).ToList();
```

Methoden lassen sich hintereinanderhängen. Jede arbeitet mit dem Ergebnis der vorigen.

Der Unterschied zwischen `First` und `FirstOrDefault` ist eine Aussage über deine Erwartung. Mit
`First` sagst du: "Wenn es keinen Treffer gibt, ist etwas grundlegend kaputt." Mit `FirstOrDefault`
sagst du: "Kein Treffer ist ein normaler Fall, um den sich der Aufrufer kümmert." Mehr dazu in 1.6.

**Aufgabe:** Welche Methode brauchst du für die Frage "Ist die Inventarnummer 1042 schon
vergeben?" Welche für "Gib mir das Gerät mit der Id 17"? Und warum wäre `Where` bei der zweiten
Frage die umständlichere Wahl?

### 1.3 Der wichtigste Unterschied: Datenbank oder Arbeitsspeicher

Dieselbe LINQ-Schreibweise funktioniert auf einer normalen Liste und auf einem `DbSet`. Was dabei
passiert, ist aber grundverschieden.

Auf einem `DbSet` wird **nichts sofort ausgeführt**. EF sammelt die Aufrufe und übersetzt sie in
**eine** SQL-Abfrage. Erst Methoden wie `ToList()`, `First()`, `FirstOrDefault()` oder `Any()`
schicken diese Abfrage tatsächlich an die Datenbank.

```csharp
context.Categories.Where(c => c.Name == "Messtechnik").ToList();
```

wird ungefähr zu `SELECT * FROM Categories WHERE Name = 'Messtechnik'`. Die Datenbank filtert, und
nur der eine Treffer kommt zurück.

Nach einem `ToList()` liegt eine normale Liste im Arbeitsspeicher, und alles Weitere läuft in C#.

Daraus folgt eine Regel: **Erst filtern, dann `ToList()`.** Die umgekehrte Reihenfolge lädt die
ganze Tabelle, um danach einen Datensatz herauszusuchen.

Genau das passiert aktuell in `GetItemById` und `CheckInventoryNumberDuplicate`: Beide rufen
`GetAllItems()` auf, und das endet mit `ToList()`. Die Suche mit `First` bzw. `Any` läuft danach
auf einer Liste im Speicher, nicht in der Datenbank.

Eine zweite Folge: EF kann nur übersetzen, was es als Spalte kennt. Eine berechnete C#-Property wie
`LendItem.IsOverdue` existiert in der Datenbank nicht. Eine Abfrage `Where(l => l.IsOverdue)` direkt
auf dem `DbSet` bricht zur Laufzeit mit *"could not be translated"* ab. In einer Abfrage muss die
Bedingung deshalb aus echten Spalten zusammengesetzt werden.

**Aufgabe:** Erkläre in einem Satz, warum `context.Items.ToList().Any(...)` und
`context.Items.Any(...)` dasselbe Ergebnis liefern, sich aber bei 100.000 Geräten sehr
unterschiedlich verhalten.

**Aufgabe:** Wie sieht `GetItemById` aus, wenn die Abfrage direkt auf `_context.Items` läuft? Welche
Zeile deiner jetzigen Methode fällt dann weg?

### 1.4 Include: verknüpfte Objekte mitladen

EF lädt Navigationseigenschaften standardmäßig **nicht** mit. Ein aus der Datenbank geladenes `Item`
hat eine korrekte `CategoryId`, aber `Category` ist `null`. Wer das Objekt braucht, fordert es an:

```csharp
context.LendItems.Include(l => l.BorrowedBy).ToList();
```

`Include` funktioniert **nur mit Navigationseigenschaften**, also Properties, deren Typ selbst eine
Entität ist. Für normale Spalten wie `string` oder `int` ist es nicht nötig und nicht erlaubt,
die werden immer geladen.

**Erledigt:** `GetAllItems` verwendet `Include(i => i.Category)`.

**Aufgabe zum Weiterdenken:** Wenn `GetItemById` direkt auf `_context.Items` abfragt statt über
`GetAllItems`, fehlt das `Include` dort. Brauchst du die Kategorie beim Laden eines einzelnen Geräts?
Denk an den Bearbeiten-Dialog aus A1, der die Kategorie anzeigt.

### 1.5 Speichern: Add, Update, SaveChanges

Der `DbContext` merkt sich jedes Objekt, das er geladen hat. Diesen Zustand nennt EF **tracked**.
Änderst du danach eine Property, sieht der Kontext das selbst:

```csharp
var cat = context.Categories.First(c => c.Id == 3);
cat.Name = "Bohren";
context.SaveChanges();
```

Hier steht kein `Update`. Der Kontext hat die Kategorie geladen, er weiß also, dass sich `Name`
geändert hat, und `SaveChanges` schreibt genau diese eine Spalte.

Neue Objekte kennt der Kontext noch nicht. Die werden mit `Add` angemeldet:

```csharp
context.Categories.Add(neueKategorie);
context.SaveChanges();
```

Beim `Add` darf die `Id` 0 sein. SQLite vergibt sie, und nach `SaveChanges` steht der vergebene Wert
im Objekt. Deshalb ist die Zeile `Id = 0;` in den `Employee`-Konstruktoren nicht falsch, aber
überflüssig: `0` ist ohnehin der Startwert jedes `int`.

**`Update` ist für Objekte gedacht, die der Kontext nicht kennt.** Etwa ein Objekt, das aus einem
anderen Kontext stammt oder im Code neu zusammengebaut wurde. `Update` meldet es an und markiert
**alle** Spalten als geändert, auch die, die gleich geblieben sind. Hat das Objekt noch die `Id` 0,
behandelt EF es sogar als neu und legt es an.

Deine Methode `UpdateItem` verwendet `Update` plus `SaveChanges`. Das funktioniert und deckt durch
das eben beschriebene Verhalten sowohl neue als auch bestehende Geräte ab. In `ChangeItem` lädst du
das Gerät aber vorher über den Kontext. Es ist also schon tracked, und `Update` wäre dort nicht
nötig.

**Aufgabe:** Beantworte für deine Methode `ChangeItem`: Nach `GetItemById` ist `itemToChange`
tracked. Was würde passieren, wenn in `UpdateItem` nur `SaveChanges()` stünde, ohne `Update`? Und
welchen Fall deckt `Update` ab, den `SaveChanges` allein nicht abdeckt?

**Aufgabe:** Die Methode heißt `UpdateItem`, legt aber auch neue Geräte an. Passt der Name zu dem,
was sie tut? Überlege, ob `SaveItem` den Aufrufer weniger überrascht.

### 1.6 Exceptions: wann werfen, wann nicht

Eine Exception ist für Situationen gedacht, mit denen der Code an dieser Stelle nicht rechnet und
die er nicht selbst lösen kann. "Datenbankdatei ist gesperrt" ist so ein Fall. "Zu dieser Id gibt
es kein Gerät" ist es meistens nicht: Der Lagerist hat sich vertippt, oder das Gerät wurde gerade
gelöscht. Das ist ein erwartbarer Ausgang, und der Aufrufer sollte ihn mit einem einfachen
`if (item == null)` behandeln können.

Deine jetzige Version von `GetItemById` macht aus "nicht gefunden" eine Exception, weil `First`
wirft. Dazu kommen zwei Punkte am `catch`-Block:

**`catch (Exception ex)` fängt alles.** Nicht nur "Gerät nicht gefunden", sondern auch "Datenbank
nicht erreichbar" oder "Tabelle existiert nicht". Alle drei bekommen dieselbe Meldung *"Check for
Typo or Item might not exist"*, und bei den letzten beiden führt die Meldung in die falsche Richtung.

**`throw new Exception(...)` verwendet den allgemeinsten Typ.** Der Aufrufer kann dann nicht mehr
unterscheiden, was passiert ist, außer indem er den Meldungstext auswertet. Wenn eine Exception
nötig ist, gibt es spezifischere Typen, etwa `KeyNotFoundException` oder
`InvalidOperationException`.

Zu `ChangeItem`: Die Meldung *"Contact your system administrator"* ist ein Text für den Benutzer.
Das Repository weiß aber nicht, wer oder was es aufruft, vielleicht ein Unit-Test, vielleicht der
CSV-Import. Benutzertexte gehören in die Schicht, die mit dem Benutzer spricht, also später ins
ViewModel.

Richtig ist, dass du in beiden Fällen die ursprüngliche Exception als `ex` weitergibst. Dadurch geht
die eigentliche Fehlerursache nicht verloren.

Ein Beispiel für die Alternative, an einer Kategorie:

```csharp
public Category? GetCategoryById(int id)
{
    return _context.Categories.FirstOrDefault(c => c.Id == id);
}
```

Kein `try`, kein `catch`. Gibt es die Kategorie nicht, kommt `null` zurück. Tritt ein echter
Datenbankfehler auf, fliegt die Original-Exception von EF mit ihrer eigenen, zutreffenden Meldung
nach oben.

**Aufgabe:** Welche Vorteile hat es für den Aufrufer, wenn `GetItemById` bei "nicht gefunden" `null`
liefert statt zu werfen? Was muss sich dafür am Rückgabetyp im Interface ändern?

**Aufgabe:** Gibt es in deinem Repository eine Stelle, an der ein `try`/`catch` wirklich etwas
Sinnvolles tun könnte, also den Fehler behandelt statt ihn nur umzuverpacken?

### 1.7 `required` und Konstruktoren

Dieser Abschnitt ist neu. Du hast `InventoryNumber`, `BorrowedBy`, `Item` und `LendBy` mit
`required` markiert. Damit sind die Compiler-Warnungen CS8618 verschwunden, aber das Schlüsselwort
bedeutet mehr, als es auf den ersten Blick scheint.

`required` heißt: **Wer ein Objekt mit `new` erzeugt, muss diese Property im Objektinitialisierer
setzen**, also in den geschweiften Klammern nach dem Konstruktoraufruf. Der Compiler prüft das an
jeder Stelle, an der `new` steht. Und er schaut dabei **nicht** in den Konstruktor hinein. Dass dein
`Item`-Konstruktor `InventoryNumber` zuweist, weiß er nicht.

Am Beispiel einer Kategorie, angenommen `Name` wäre `required` und der Konstruktor setzt ihn:

```csharp
var c1 = new Category("Messtechnik");                            // CS9035
var c2 = new Category("Messtechnik") { Name = "Messtechnik" };   // kompiliert
```

Die erste Zeile scheitert mit `CS9035: Required member 'Category.Name' must be set in the object
initializer or attribute constructor`. Die zweite kompiliert, setzt den Namen aber doppelt.

Bei dir fällt das noch nicht auf, weil es im ganzen Projekt noch keinen Aufruf von `new Item(...)`
oder `new LendItem(...)` gibt. Der erste kommt mit dem CSV-Import.

Es gibt drei Wege aus diesem Widerspruch:

- **`required` wieder entfernen** und die Warnung anders lösen, bei `InventoryNumber` mit einem
  Startwert wie bei `Name`, bei den Navigationseigenschaften mit `= null!;`. Der Konstruktor bleibt
  dann die Stelle, die für vollständige Objekte sorgt.
- **Den Konstruktor mit dem Attribut `[SetsRequiredMembers]` kennzeichnen.** Damit sagst du dem
  Compiler ausdrücklich: "Dieser Konstruktor setzt alle Pflichtfelder." Das Attribut liegt im
  Namespace `System.Diagnostics.CodeAnalysis`. Der Compiler prüft allerdings nicht nach, ob das
  stimmt.
- **Den Konstruktor entfernen** und nur mit Objektinitialisierern arbeiten. Dann erzwingt `required`
  die Vollständigkeit, aber fachliche Logik im Konstruktor ist nicht mehr möglich.

**Aufgabe:** Entscheide dich für einen Weg. Leitfragen: Wo soll in deinem Projekt sichergestellt
werden, dass ein Gerät vollständig ist, beim Konstruktor oder beim Aufrufer? Welcher Weg lässt sich
in der Dokumentation in zwei Sätzen begründen?

### 1.8 Wie Migrationen funktionieren

Auch dieser Abschnitt ist neu, weil deine Migrationsdateien nicht zum Stand der Datenbank passen.

Eine Migration besteht aus drei Teilen, und jeder hat eine eigene Aufgabe:

| Datei | Aufgabe |
|---|---|
| `<Zeitstempel>_<Name>.cs` | Die Anweisungen `Up` und `Down`: was beim Anwenden bzw. Rückgängigmachen passiert |
| `<Zeitstempel>_<Name>.Designer.cs` | Das Modell, wie es zum Zeitpunkt dieser Migration aussah |
| `<Kontextname>ModelSnapshot.cs` | Das Modell nach **allen** Migrationen, also EFs Gedächtnis über den aktuellen Stand |

Bei `dotnet ef migrations add` vergleicht EF dein aktuelles C#-Modell mit dem **Snapshot**, nicht
mit der Datenbank. Die Unterschiede werden zur neuen `Up`-Methode, und der Snapshot wird aktualisiert.

Bei `dotnet ef database update` schaut EF in die Tabelle `__EFMigrationsHistory` der Datenbank,
welche Migrationen dort schon eingetragen sind, und führt nur die fehlenden aus.

Daraus ergeben sich zwei Regeln:

- **Eine Migrationsdatei nie einfach löschen, wenn der Snapshot bleibt.** Dann hält EF den alten
  Stand weiterhin für bekannt, und die nächste Migration enthält die Änderungen nicht mehr.
- **Nach jeder Modelländerung eine neue Migration**, auch bei einer einzelnen neuen Property.

**Aufgabe:** In deiner `__EFMigrationsHistory` stehen zwei Einträge mit dem Namen `InitialCreate`,
aber im Ordner `Migrations` liegt nur einer, und dessen `Up`-Methode ist leer. Erkläre anhand der
Tabelle oben, wie das zustande gekommen ist. Was würde passieren, wenn jemand dein Repository klont
und `dotnet ef database update` auf einem Rechner ohne Datenbankdatei ausführt?

---

## Teil 2: Der Weg im Projekt

### Schritt 1: Den Kontext mit der Datenbank verbinden — erledigt

`OnConfiguring` baut den Pfad mit `Path.Combine(AppContext.BaseDirectory, ...)` und ruft `UseSqlite`
auf. Die Datenbank liegt damit im Ausgabeordner neben der EXE.

Die Prüfung `if (!options.IsConfigured)` ist dabei eine gute Wahl. Sie sorgt dafür, dass ein
Aufrufer dem Kontext von außen andere Optionen mitgeben kann, etwa eine Test-Datenbank, und
`OnConfiguring` diese dann nicht überschreibt. Das wird in Phase 7 nützlich.

Das `using Microsoft.Extensions.Options;` in `LendContext.cs` wird nicht verwendet und kann weg.

### Schritt 2: Das Modell fertig machen

**2a: Konstruktoren** — `Item` und `Category` haben jetzt öffentliche Konstruktoren. Beim `Item`
kollidiert der Konstruktor aber mit den `required`-Properties, siehe 1.7. Das gilt genauso für die
beiden `LendItem`-Konstruktoren. Diese Entscheidung sollte vor der neuen Migration fallen, weil sie
bestimmt, wie der Import die Objekte erzeugt.

Leitfrage zum `Item`-Konstruktor: Er setzt `Category`, aber nicht `CategoryId`. Ist das ein Problem?
Denk daran, was EF beim `Add` mit einer gesetzten Navigationseigenschaft macht. Und was ist, wenn der
Import nur die `CategoryId` aus der CSV kennt, aber kein `Category`-Objekt?

**2b: IsInUse oder IsActive?** — weiterhin offen. Wenn die Property "darf ausgeliehen werden" meint,
passt der alte Name `IsActive` besser, und die CSV-Spalte heißt auch so. Wenn sie "ist gerade
verliehen" meint, ist sie überflüssig, weil das die offene Ausleihe schon aussagt. Die Entscheidung
betrifft das Datenbankschema und gehört deshalb ebenfalls vor die neue Migration.

**2c: Email bei Employee** — neu hinzugekommen, aber nicht im Snapshot und nicht in der Datenbank.
Außerdem hat `mitarbeiter.csv` keine Email-Spalte. Leitfragen: Braucht eine der Anforderungen A1 bis
A7 die Email? Wenn nicht, ist sie Umfang, der Zeit kostet. Wenn ja, müssen Testdaten und Migration
nachgezogen werden.

### Schritt 3: Migrationen neu aufsetzen

Das **Schema** in der Datenbankdatei ist korrekt. Die Prüfung ergab:

- vier Tabellen plus `__EFMigrationsHistory`
- in `LendItems` die Spalten `ItemId`, `BorrowedById` und `LendById`, jeweils als Fremdschlüssel
- keine überzählige Spalte wie `EmployeeId`, die zwei Beziehungen zu `Employee` sind also richtig
  zugeordnet
- keine Spalte `IsOverdue`

Die **Migrationsdateien** passen aber nicht mehr dazu: Die vorhandene `Up`-Methode ist leer, und
`Email` fehlt im Snapshot. Die Erklärung steht in 1.8.

Da die Datenbank noch keine Daten enthält, ist der einfachste und sauberste Weg ein Neuanfang:

1. Die Entscheidungen aus Schritt 2 treffen und im Code umsetzen.
2. Den Ordner `Migrations` vollständig löschen, **einschließlich** `LendContextModelSnapshot.cs`.
3. Die Datei `bin/Debug/net10.0-windows/Digitale_Geraeteliste.db` löschen.
4. `dotnet ef migrations add InitialCreate`
5. Die neue `Up`-Methode öffnen und prüfen, dass sie **nicht** leer ist, sondern vier
   `CreateTable`-Aufrufe enthält.
6. `dotnet ef database update`

Leitfrage: Warum ist dieser Neuanfang jetzt unproblematisch, wäre es aber nicht mehr, sobald der
Lagerist echte Ausleihen erfasst hat? Was wäre dann der richtige Weg für eine Modelländerung?

**Ein Befund im Schema, den du bewusst entscheiden solltest:** Alle vier Fremdschlüssel stehen auf
`ON DELETE CASCADE`. Das ist EFs Standard für Pflichtbeziehungen. Die Folge: Wird ein Mitarbeiter
gelöscht, löscht die Datenbank automatisch **alle** seine Ausleihen mit, und ein gelöschtes Gerät
nimmt seine gesamte Ausleihhistorie mit. Für die Überfälligkeitsliste und die Nachvollziehbarkeit
aus der Ausgangssituation ist das genau das Falsche.

Leitfragen: Soll dein Programm Geräte und Mitarbeiter überhaupt löschen können, oder nur
deaktivieren bzw. ausmustern? Wenn nie gelöscht wird, reicht es, das in der Doku festzuhalten. Wenn
doch, lässt sich das Löschverhalten in `OnModelCreating` ändern, das Stichwort ist `OnDelete` mit
`DeleteBehavior.Restrict`.

Zur Einordnung des `Category?` in `Item`: Die Datenbank hat `CategoryId` als `NOT NULL` angelegt,
weil die Fremdschlüssel-Property ein `int` ist und kein `int?`. Die Beziehung ist also Pflicht, auch
wenn die Navigationseigenschaft nullable ist. Das Fragezeichen an `Category` sagt nur, dass das
Objekt ohne `Include` nicht geladen sein muss.

### Schritt 4: Kontext übergeben und zusammensetzen — erledigt

Alle drei Repositories bekommen den `LendContext` im Konstruktor und speichern ihn in
`private readonly LendContext _context`. In `App.OnStartup` werden sie mit derselben Kontextinstanz
erzeugt.

Drei Hinweise für `App.xaml.cs`:

- Die Repositories sind **lokale Variablen** in `OnStartup`. Sie existieren nur bis zum Ende der
  Methode. Für Schritt 6 reicht das, für Phase 4 nicht mehr, weil dann das ViewModel sie braucht.
  Siehe Abschnitt 6 im MVVM-Leitfaden.
- Die Variablen heißen `employeeContext`, `itemContext` und `lendItemContext`, sind aber
  Repositories. Es gibt nur einen Kontext.
- Der Kontext hält die Datenbankdatei offen und sollte beim Beenden freigegeben werden. Das
  Gegenstück zu `OnStartup` ist `OnExit`.

### Schritt 5: ItemRepository Methode für Methode

**GetAllItems** — erledigt.

**GetItemById** — umgesetzt, drei Überarbeitungen empfohlen:

1. Die Abfrage direkt auf `_context.Items` stellen statt über `GetAllItems`, siehe 1.3.
2. Das `Include` aus `GetAllItems` übernehmen, wenn die Kategorie gebraucht wird, siehe 1.4.
3. Entscheiden, ob "nicht gefunden" eine Exception oder `null` sein soll, siehe 1.6. Wenn `null`:
   `FirstOrDefault`, Rückgabetyp `Item?` im Interface und in der Klasse, `try`/`catch` entfällt.

**UpdateItem** — umgesetzt und funktionsfähig. Zu prüfen sind nur die Fragen aus 1.5.

**ChangeItem** — umgesetzt. Hier ist eine Entscheidung offen, die du bewusst treffen und in der Doku
begründen solltest:

- **Variante A: Behalten.** Dann ist das Repository mehr als Datenzugriff, es kennt auch den
  Bearbeitungsvorgang. Das ist in kleinen Projekten verbreitet und vertretbar.
- **Variante B: Streichen.** Dann lädt der Aufrufer das Gerät, setzt die Properties selbst und ruft
  `UpdateItem` bzw. `SaveItem` auf. Das Repository bleibt reiner Datenzugriff.

Unabhängig davon enthält die jetzige Fassung eine Doppelung. Die Methode bekommt ein fertiges `item`
übergeben, lädt dann aber über `GetItemById(item.Id)` ein zweites Mal dasselbe Gerät. Leitfrage: Wenn
`item` aus demselben Kontext geladen wurde, sind `item` und `itemToChange` dann zwei Objekte oder
dasselbe? Was folgt daraus für die Signatur der Methode, reicht vielleicht die Id?

**CheckInventoryNumberDuplicate** — noch offen. Zwei Punkte:

1. Wie bei `GetItemById` direkt auf `_context.Items` abfragen, siehe 1.3.
2. Der fachliche Fehler: Beim Bearbeiten eines Geräts, dessen Nummer unverändert bleibt, findet `Any`
   das Gerät selbst und meldet ein Duplikat. Das Interface braucht einen zweiten Parameter, die Id
   des gerade bearbeiteten Geräts.

Leitfragen: Wie sieht ein `Any`-Lambda aus, das "gleiche Nummer **und** andere Id" prüft? Welchen
Wert übergibt man bei einem neuen Gerät, das noch keine Id hat?

### Schritt 5b: CSV-Import der Mitarbeiter

`GetEmployeesFromCSV` erzeugt jetzt direkt `Employee`-Objekte, das ist der richtige Ansatz. Die
Spaltenzuordnung stimmt aber nicht mit der Datei überein.

Die Kopfzeile von `mitarbeiter.csv` lautet:

```
Id;LastName;FirstName;FullName;Department
```

Nach `Split(';')` steht also an Index 0 die `Id`. Dein Code liest `values[0]` als Nachname,
`values[1]` als Vorname, `values[2]` als Abteilung und `values[3]` als Email.

**Aufgabe:** Schreib für die erste Datenzeile `1;Krüger;Thomas;Krüger, Thomas;Elektroinstallation`
auf, welcher Wert in welcher Property landen würde. Welche Indizes wären die richtigen?

Leitfragen: Soll die `Id` aus der CSV übernommen werden, oder soll SQLite neue Ids vergeben? Denk an
`ausleihen.csv`, die über `BorrowedById` und `LendById` auf genau diese Ids verweist. Was passiert mit
diesen Verweisen, wenn die Datenbank beim Import andere Ids vergibt? Und brauchst du die Spalte
`FullName` aus der Datei, wenn der Konstruktor den Namen ohnehin selbst zusammensetzt?

Zwei kleinere Punkte: `using System.Security.RightsManagement;` wird auch hier nicht gebraucht. Und
der `StreamReader` liest ohne Angabe eines Zeichensatzes. Bei .NET ist UTF-8 der Standard, das passt
zu den Testdaten. Wenn du es in der Doku ausdrücklich zeigen willst, nimmt der Konstruktor ein
`Encoding.UTF8` als zweiten Parameter.

### Schritt 6: Selbstkontrolle

Voraussetzung: Schritt 3 ist neu aufgesetzt und Schritt 2a entschieden.

Zum Prüfen reicht ein vorübergehender Test in `App.OnStartup`, der eine Kategorie und ein Gerät
speichert, wieder lädt und das Ergebnis mit `System.Diagnostics.Debug.WriteLine` ins Ausgabefenster
schreibt.

Prüffragen, die dieser Test beantworten sollte:

- Hat das Gerät nach dem Speichern eine Id größer 0?
- Liefert `GetItemById` mit dieser Id das Gerät zurück, und ist `Category` dabei gefüllt?
- Was passiert bei `GetItemById` mit einer Id, die es nicht gibt? Ist das Verhalten das, was du in
  1.6 entschieden hast?
- Meldet `CheckInventoryNumberDuplicate` für dieselbe Nummer mit einer **anderen** Id `true` und mit
  **seiner eigenen** Id `false`?

Wenn alle vier stimmen, sind Kontext, Mapping und Repository korrekt. Den Testcode und die
Testdatensätze danach wieder entfernen.

### Schritt 7: Übertragen auf LendItemRepository

Hier kommen die Konzepte zusammen. Zwei Methoden als Übung:

**Offene Ausleihe zu einem Gerät** — Diese Methode fehlt noch im Interface, R1 hängt an ihr.
Leitfragen: Welche zwei Bedingungen machen eine Ausleihe zu "offen für Gerät X"? Welche LINQ-Methode
aus 1.2 passt, wenn es höchstens eine geben darf, und keine auch ein normaler Fall ist?

**GetAllOverdueLendItems** — Leitfragen: Warum darfst du hier nicht `Where(l => l.IsOverdue)`
schreiben (siehe 1.3)? Aus welchen zwei echten Spalten setzt sich "überfällig" zusammen? Welche
Navigationseigenschaften braucht die Überfälligkeitsliste nach A6 zum Anzeigen, und wie viele
`Include`-Aufrufe ergibt das?

---

## Fehlerbilder

| Meldung | Bedeutung |
|---|---|
| `CS9035: Required member ... must be set in the object initializer` | `required`-Property wird nur im Konstruktor gesetzt, siehe 1.7 |
| `No database provider has been configured` | `OnConfiguring` ruft kein `UseSqlite` auf |
| `SQLite Error 1: 'no such table'` | Migration nicht ausgeführt, leere `Up`-Methode oder falscher Dateipfad |
| `SQLite Error 1: 'no such column: e.Email'` | Property im Modell ergänzt, aber keine neue Migration, siehe 1.8 |
| Neue Migration ist leer, obwohl sich das Modell geändert hat | Snapshot kennt die Änderung schon, oder eine Migrationsdatei wurde ohne Snapshot gelöscht |
| `Sequence contains no matching element` | `First` hat keinen Treffer gefunden, siehe 1.2 und 1.6 |
| `The expression '...' is invalid inside an 'Include' operation` | `Include` auf eine normale Spalte statt auf eine Navigationseigenschaft |
| `could not be translated` | Bedingung nutzt eine berechnete C#-Property, die es in der Datenbank nicht gibt |
| `NullReferenceException` auf `item.Category.Name` | `Include(i => i.Category)` fehlt bei dieser Abfrage |
| `The instance of entity type cannot be tracked because another instance with the same key` | zwei Kontexte oder dasselbe Objekt zweimal angemeldet |
| `FOREIGN KEY constraint failed` beim Import | Ausleihe verweist auf eine Geräte- oder Mitarbeiter-Id, die in der Datenbank nicht existiert, siehe 5b |
