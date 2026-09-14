# EF Core und LINQ Schritt für Schritt

Arbeitsanleitung für den Weg vom leeren `LendContext` bis zu einem funktionierenden
`ItemRepository`. Sie ergänzt den [Persistenz Leitfaden - KI erstellt.md](Persistenz%20Leitfaden%20-%20KI%20erstellt.md)
um die Syntax, die dort vorausgesetzt wird. Jeder Abschnitt erklärt ein Konzept, zeigt es an einem
Beispiel mit einer **anderen** Entität als der, die umgesetzt werden soll, und endet mit einer
Aufgabe und Leitfragen. Lösungen stehen hier bewusst nicht.

Stand beim Schreiben: `LendContext` erbt von `DbContext` und hat vier `DbSet`s, `OnConfiguring` ist
leer, es gibt noch keine Migration, `ItemRepository` ist teilweise implementiert.

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

### 1.2 Die fünf LINQ-Methoden, die du brauchst

| Methode | Liefert | Frage, die sie beantwortet |
|---|---|---|
| `Where(lambda)` | eine Menge | Welche Elemente erfüllen die Bedingung? |
| `FirstOrDefault(lambda)` | ein Element oder `null` | Welches ist das erste, das passt? |
| `Any(lambda)` | `bool` | Gibt es mindestens eines, das passt? |
| `OrderBy(lambda)` | eine sortierte Menge | In welcher Reihenfolge? Hier liefert das Lambda den Sortierwert, nicht `true`/`false`. |
| `ToList()` | eine `List<T>` | Jetzt wirklich ausführen und alles einsammeln. |

Beispiel mit Kategorien:

```csharp
var sortiert = categories.Where(c => c.Name.StartsWith("M")).OrderBy(c => c.Name).ToList();
```

Methoden lassen sich hintereinanderhängen. Jede arbeitet mit dem Ergebnis der vorigen.

**Aufgabe:** Welche der fünf Methoden brauchst du für die Frage "Ist die Inventarnummer 1042 schon
vergeben?" Welche für "Gib mir das Gerät mit der Id 17"? Und warum wäre `Where` bei der zweiten
Frage die umständlichere Wahl?

### 1.3 Der wichtigste Unterschied: Datenbank oder Arbeitsspeicher

Dieselbe LINQ-Schreibweise funktioniert auf einer normalen Liste und auf einem `DbSet`. Was dabei
passiert, ist aber grundverschieden.

Auf einem `DbSet` wird **nichts sofort ausgeführt**. EF sammelt die Aufrufe und übersetzt sie in
**eine** SQL-Abfrage. Erst Methoden wie `ToList()`, `FirstOrDefault()` oder `Any()` schicken diese
Abfrage tatsächlich an die Datenbank.

```csharp
context.Categories.Where(c => c.Name == "Messtechnik").ToList();
```

wird ungefähr zu `SELECT * FROM Categories WHERE Name = 'Messtechnik'`. Die Datenbank filtert, und
nur der eine Treffer kommt zurück.

Nach einem `ToList()` liegt eine normale Liste im Arbeitsspeicher, und alles Weitere läuft in C#.

Daraus folgt eine Regel: **Erst filtern, dann `ToList()`.** Die umgekehrte Reihenfolge lädt die
ganze Tabelle, um danach einen Datensatz herauszusuchen. Genau das passiert aktuell in
`GetItemById` und `CheckInventoryNumberDuplicate`, weil beide zuerst `GetAllItems()` aufrufen.

Eine zweite Folge: EF kann nur übersetzen, was es als Spalte kennt. Eine berechnete C#-Property wie
`LendItem.IsOverdue` existiert in der Datenbank nicht. Eine Abfrage `Where(l => l.IsOverdue)` direkt
auf dem `DbSet` bricht zur Laufzeit mit *"could not be translated"* ab. In einer Abfrage muss die
Bedingung deshalb aus echten Spalten zusammengesetzt werden.

**Aufgabe:** Erkläre in einem Satz, warum `context.Items.ToList().Any(...)` und
`context.Items.Any(...)` dasselbe Ergebnis liefern, sich aber bei 100.000 Geräten sehr
unterschiedlich verhalten.

### 1.4 Include: verknüpfte Objekte mitladen

EF lädt Navigationseigenschaften standardmäßig **nicht** mit. Ein aus der Datenbank geladenes `Item`
hat eine korrekte `CategoryId`, aber `Category` ist `null`. Wer das Objekt braucht, fordert es an:

```csharp
context.LendItems.Include(l => l.BorrowedBy).ToList();
```

`Include` funktioniert **nur mit Navigationseigenschaften**, also Properties, deren Typ selbst eine
Entität ist. Für normale Spalten wie `string` oder `int` ist es nicht nötig und nicht erlaubt,
die werden immer geladen.

**Aufgabe:** In `ItemRepository.GetAllItems` steht aktuell `Include(i => i.Name)`. Das kompiliert,
wirft beim ersten Aufruf aber eine `InvalidOperationException`. Warum? Welche Property von `Item`
wäre an dieser Stelle die richtige, und brauchst du sie für die Bestandsübersicht (A5) überhaupt?

### 1.5 Speichern: Add, Änderungen verfolgen, SaveChanges

Der `DbContext` merkt sich jedes Objekt, das er geladen hat. Änderst du danach eine Property, sieht
er das selbst. Es gibt kein `Update` im üblichen Sinn.

```csharp
var cat = context.Categories.First(c => c.Id == 3);
cat.Name = "Bohren";
context.SaveChanges();
```

Neue Objekte kennt der Kontext noch nicht. Die werden mit `Add` angemeldet:

```csharp
context.Categories.Add(neueKategorie);
context.SaveChanges();
```

Beim `Add` darf die `Id` 0 sein. SQLite vergibt sie, und nach `SaveChanges` steht der vergebene Wert
im Objekt.

**Aufgabe:** Ein Repository hat eine Methode `Save(Item item)`. Sie soll für neue **und** bestehende
Geräte funktionieren. Woran erkennst du im Code, ob ein übergebenes Gerät neu ist? Was muss in
beiden Fällen passieren, und was nur in einem?

---

## Teil 2: Der Weg im Projekt

Die Schritte bauen aufeinander auf. Nach jedem Schritt sollte das Projekt kompilieren.

### Schritt 1: Den Kontext mit der Datenbank verbinden

`OnConfiguring` ist leer. Ohne Anbieter und Pfad weiß EF nicht, wohin es schreiben soll, und die
Migration bricht ab.

Die Form des Aufrufs ist:

```csharp
options.UseSqlite("Data Source=<Pfad zur Datei>");
```

**Aufgabe:** Baue den Pfad so, dass die Datei `geraeteliste.db` im Ausgabeordner der Anwendung
liegt. Die Bausteine sind `AppContext.BaseDirectory` und `Path.Combine`.

Leitfragen: Warum ist `Path.Combine` besser als zwei Strings mit `+` zu verbinden? Was steht in
`AppContext.BaseDirectory`, wenn du die Anwendung aus Visual Studio startest?

Den leeren öffentlichen Konstruktor von `LendContext` kannst du dabei entfernen.

### Schritt 2: Das Modell vor der Migration fertig machen

Eine Migration hält den Stand des Modells fest. Alles, was vorher noch geändert wird, spart später
eine zweite Migration.

**2a: Wer darf Objekte erzeugen?** `Item` und `Category` haben **nur** einen privaten
Konstruktor. EF kommt damit zurecht, aber sonst niemand: Weder der CSV-Import noch das Anlegen
eines neuen Geräts nach A1 kann ein `Item` erzeugen. Beide Klassen brauchen zusätzlich einen
öffentlichen Konstruktor mit den fachlich nötigen Werten, so wie `Employee` ihn schon hat.

Leitfrage: Welche Werte muss ein Gerät mindestens haben, damit es fachlich sinnvoll ist? Die `Id`
gehört nicht dazu, warum?

**2b: IsInUse oder IsActive?** Entscheide, was die Property bedeuten soll. Wenn sie "darf
ausgeliehen werden" meint, passt der alte Name `IsActive` besser, und die CSV-Spalte heißt auch so.
Wenn sie "ist gerade verliehen" meint, ist sie überflüssig, weil das die offene Ausleihe schon
aussagt.

**2c: Die Warnungen CS8618.** Der Compiler warnt, dass `InventoryNumber`, `Item`, `BorrowedBy` und
`LendBy` nach dem privaten Konstruktor `null` sein können.

Für `InventoryNumber` ist die Lösung dieselbe wie bei `Name`: ein Startwert.

Für die drei Navigationseigenschaften ist die übliche Schreibweise bei EF-Entitäten:

```csharp
public Employee BorrowedBy { get; set; } = null!;
```

Das `null!` sagt dem Compiler: "Ich weiß, dass hier zunächst nichts steht, EF füllt es beim Laden."
Es ändert nichts am Verhalten, es dokumentiert nur eine Annahme. Die Annahme stimmt allerdings nur,
wenn mit `Include` geladen wird, siehe 1.4.

### Schritt 3: Migration erzeugen und nachsehen

Im Ordner `Digitale_Geraeteliste`:

```
dotnet ef migrations add InitialCreate
dotnet ef database update
```

Dann die Datei `bin/Debug/net10.0-windows/geraeteliste.db` mit *DB Browser for SQLite* öffnen.

**Prüfliste:**

- Vier Tabellen plus `__EFMigrationsHistory`.
- Die Tabelle `LendItems` hat die Spalten `ItemId`, `BorrowedById` und `LendById`.
- Es gibt **keine** zusätzliche Spalte wie `EmployeeId` oder `EmployeeId1`. Taucht eine auf, hat EF
  die zwei Beziehungen zu `Employee` nicht deinen Fremdschlüsseln zugeordnet und eine eigene
  angelegt.
- `IsOverdue` taucht **nicht** als Spalte auf. Leitfrage: Warum nicht?

Zusätzlich die erzeugte Datei im Ordner `Migrations` öffnen und die `Up`-Methode lesen. Jede Zeile
dort entspricht einer Tabelle, Spalte oder Beziehung.

### Schritt 4: Das Repository bekommt den Kontext

Aktuell erzeugt `ItemRepository` im Konstruktor `new LendContext()`. Warum das zu Problemen führt,
sobald mehrere Repositories zusammenarbeiten, steht in 1.5: Jeder Kontext hat sein eigenes
Gedächtnis.

Das Muster am Beispiel eines Kategorie-Repositorys:

```csharp
private readonly LendContext _context;

public CategoryRepository(LendContext context)
{
    _context = context;
}
```

`readonly` bedeutet, dass das Feld nur im Konstruktor gesetzt werden kann. Damit ist ausgeschlossen,
dass eine Methode später versehentlich einen anderen Kontext hineinschreibt.

**Aufgabe:** Baue den Konstruktor von `ItemRepository` entsprechend um. Leitfrage: Wo im Programm
wird der Kontext dann erzeugt, und wie viele Instanzen davon gibt es?

### Schritt 5: ItemRepository Methode für Methode

Reihenfolge nach Schwierigkeit. Nach jeder Methode kompilieren.

**GetAllItems** — Leitfragen: Welches `DbSet` ist die Tabelle? Brauchst du ein `Include`, und wenn
ja, welches? Wo muss `ToList()` stehen?

**GetItemById** — Frage direkt das `DbSet` ab, nicht `GetAllItems()`. Leitfrage: Was liefert
`FirstOrDefault`, wenn es keinen Treffer gibt? Passt dazu der Rückgabetyp `Item` im Interface, oder
müsste er `Item?` heißen? Die Warnung CS8603 in Zeile 21 ist genau dieser Hinweis.

**CheckInventoryNumberDuplicate** — Das Interface braucht einen zweiten Parameter, die Id des
gerade bearbeiteten Geräts. Leitfragen: Warum meldet die aktuelle Version ein Duplikat, wenn man ein
bestehendes Gerät speichert, ohne die Nummer zu ändern? Wie sieht ein `Any`-Lambda aus, das "gleiche
Nummer **und** andere Id" prüft? Welchen Wert übergibt man bei einem neuen Gerät, das noch keine
Id hat?

**SaveItem** — Die Aufgabe aus 1.5 umsetzen.

**ChangeItem** — Streichen, aus Interface und Klasse. Das Ändern passiert am Objekt: laden,
Properties setzen, `SaveItem` aufrufen. Wer das koordiniert, ist später Service oder ViewModel.

### Schritt 6: Selbstkontrolle

Bevor der CSV-Import steht, ist die Datenbank leer. Zum Prüfen reicht ein vorübergehender Test im
Startcode, etwa in `App.xaml.cs`, der ein Gerät speichert, wieder lädt und das Ergebnis mit
`System.Diagnostics.Debug.WriteLine` ins Ausgabefenster schreibt.

Prüffragen, die dieser Test beantworten sollte:

- Hat das Gerät nach `SaveItem` eine Id größer 0?
- Liefert `GetItemById` mit dieser Id das Gerät zurück, und ist `Category` dabei gefüllt?
- Meldet `CheckInventoryNumberDuplicate` für dieselbe Nummer mit einer **anderen** Id `true` und mit
  **seiner eigenen** Id `false`?

Wenn alle drei stimmen, sind Kontext, Mapping und Repository korrekt. Den Testcode danach wieder
entfernen.

### Schritt 7: Übertragen auf LendItemRepository

Hier kommen die Konzepte zusammen. Zwei Methoden als Übung:

**Offene Ausleihe zu einem Gerät** — Diese Methode fehlt noch im Interface, R1 hängt an ihr.
Leitfragen: Welche zwei Bedingungen machen eine Ausleihe zu "offen für Gerät X"? Welche LINQ-Methode
aus 1.2 passt, wenn es höchstens eine geben darf?

**GetAllOverdueLendItems** — Leitfragen: Warum darfst du hier nicht `Where(l => l.IsOverdue)`
schreiben (siehe 1.3)? Aus welchen zwei echten Spalten setzt sich "überfällig" zusammen? Welche
Navigationseigenschaften braucht die Überfälligkeitsliste nach A6 zum Anzeigen, und wie viele
`Include`-Aufrufe ergibt das?

---

## Fehlerbilder zur Laufzeit

| Meldung | Bedeutung |
|---|---|
| `The expression '...' is invalid inside an 'Include' operation` | `Include` auf eine normale Spalte statt auf eine Navigationseigenschaft |
| `could not be translated` | Bedingung nutzt eine berechnete C#-Property, die es in der Datenbank nicht gibt |
| `No database provider has been configured` | `OnConfiguring` ruft kein `UseSqlite` auf |
| `SQLite Error 1: 'no such table'` | Migration nicht ausgeführt oder falscher Dateipfad |
| `NullReferenceException` auf `item.Category.Name` | `Include(i => i.Category)` fehlt |
| `The instance of entity type cannot be tracked because another instance with the same key` | zwei Kontexte oder dasselbe Objekt zweimal geladen, siehe Schritt 4 |
