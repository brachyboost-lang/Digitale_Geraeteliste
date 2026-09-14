# EF Core und LINQ Schritt für Schritt

Arbeitsanleitung für den Weg vom leeren `LendContext` bis zu einem funktionierenden
`ItemRepository`. Sie ergänzt den [Persistenz Leitfaden - KI erstellt.md](Persistenz%20Leitfaden%20-%20KI%20erstellt.md)
um die Syntax, die dort vorausgesetzt wird. Jeder Abschnitt erklärt ein Konzept, zeigt es an einem
Beispiel mit einer **anderen** Entität als der, die umgesetzt werden soll, und endet mit einer
Aufgabe und Leitfragen. Lösungen stehen hier bewusst nicht.

## Aktueller Stand

| Baustein | Stand |
|---|---|
| `LendContext` mit vier `DbSet`s | erledigt |
| `OnConfiguring` mit `UseSqlite` | **offen**, Schritt 1 |
| Öffentliche Konstruktoren für `Item` und `Category` | **offen**, Schritt 2a |
| Entscheidung `IsInUse` / `IsActive` | **offen**, Schritt 2b |
| Erste Migration und Sichtprüfung der Datenbank | **offen**, Schritt 3 |
| Kontext wird ins Repository übergeben | **offen**, Schritt 4 |
| `GetAllItems` mit `Include(i => i.Category)` | erledigt |
| `GetItemById` | umgesetzt, Überarbeitung empfohlen, Schritt 5 |
| `UpdateItem` mit `Update` und `SaveChanges` | umgesetzt, Hinweise in 1.5 und Schritt 5 |
| `ChangeItem` | umgesetzt, Entscheidung offen, Schritt 5 |
| `CheckInventoryNumberDuplicate` | **offen**, Schritt 5 |

**Wichtig zur Reihenfolge:** Das Repository ist schon weiter als die Schritte 1 bis 3. Solange
`OnConfiguring` leer ist und keine Migration existiert, kann keine Methode im Repository tatsächlich
ausgeführt werden. Jeder Aufruf endet mit `No database provider has been configured`. Die Methoden
lassen sich also erst prüfen, wenn Schritt 1 bis 3 erledigt sind.

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

**Aufgabe:** Wie sieht `GetItemById` aus, wenn die Abfrage direkt auf `Context.Items` läuft? Welche
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

**Erledigt:** `GetAllItems` verwendet jetzt `Include(i => i.Category)`.

**Aufgabe zum Weiterdenken:** Wenn `GetItemById` direkt auf `Context.Items` abfragt statt über
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
im Objekt.

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

Diesen Abschnitt gab es in der ersten Fassung nicht. Er ist durch `GetItemById` und `ChangeItem`
dazugekommen.

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

Die gute Nachricht: Du hast in beiden Fällen die ursprüngliche Exception als `ex` weitergegeben.
Dadurch geht die eigentliche Fehlerursache nicht verloren. Das ist der Teil, der richtig ist.

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
gehört nicht dazu, warum? Denk an 1.5 und was beim `Add` mit der `Id` passiert.

**2b: IsInUse oder IsActive?** Entscheide, was die Property bedeuten soll. Wenn sie "darf
ausgeliehen werden" meint, passt der alte Name `IsActive` besser, und die CSV-Spalte heißt auch so.
Wenn sie "ist gerade verliehen" meint, ist sie überflüssig, weil das die offene Ausleihe schon
aussagt. Die Entscheidung betrifft auch `ChangeItem`, das den Wert als Parameter entgegennimmt.

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

`ItemRepository` erzeugt im Konstruktor weiterhin `new LendContext()`. Warum das zu Problemen führt,
sobald mehrere Repositories zusammenarbeiten, folgt aus 1.5: Jeder Kontext hat sein eigenes
Gedächtnis. Lädt `ItemRepository` ein Gerät und `LendItemRepository` speichert eine Ausleihe mit
diesem Gerät über seinen eigenen Kontext, kennt der zweite Kontext das Gerät nicht und versucht es
womöglich neu anzulegen.

Das Muster am Beispiel eines Kategorie-Repositorys:

```csharp
private readonly LendContext _context;

public CategoryRepository(LendContext context)
{
    _context = context;
}
```

`readonly` bedeutet, dass das Feld nur im Konstruktor gesetzt werden kann. Damit ist ausgeschlossen,
dass eine Methode später versehentlich einen anderen Kontext hineinschreibt. Der Unterstrich im
Namen ist die übliche Kennzeichnung privater Felder in C#. Dein jetziger Name `Context` sieht aus
wie eine Property, das macht Code schwerer lesbar.

**Aufgabe:** Baue den Konstruktor von `ItemRepository` entsprechend um. Leitfrage: Wo im Programm
wird der Kontext dann erzeugt, und wie viele Instanzen davon gibt es?

### Schritt 5: ItemRepository Methode für Methode

**GetAllItems** — erledigt. Nichts zu tun.

**GetItemById** — umgesetzt, drei Überarbeitungen empfohlen:

1. Die Abfrage direkt auf `Context.Items` stellen statt über `GetAllItems`, siehe 1.3.
2. Das `Include` aus `GetAllItems` übernehmen, wenn die Kategorie gebraucht wird, siehe 1.4.
3. Entscheiden, ob "nicht gefunden" eine Exception oder `null` sein soll, siehe 1.6. Wenn `null`:
   `FirstOrDefault`, Rückgabetyp `Item?` im Interface und in der Klasse, `try`/`catch` entfällt.

Leitfrage: Die Compiler-Warnung CS8603 aus der vorigen Fassung ist verschwunden. Warum? Hat sich das
zugrunde liegende Problem damit gelöst, oder nur verlagert?

**UpdateItem** — umgesetzt und funktionsfähig. Zu prüfen sind nur die Fragen aus 1.5: Name, und ob
`Update` in allen Aufrufsituationen nötig ist.

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

Und zum `catch`-Block: siehe 1.6, Benutzertexte gehören nicht ins Repository.

**CheckInventoryNumberDuplicate** — noch offen. Zwei Punkte:

1. Wie bei `GetItemById` direkt auf `Context.Items` abfragen, siehe 1.3.
2. Der fachliche Fehler: Beim Bearbeiten eines Geräts, dessen Nummer unverändert bleibt, findet `Any`
   das Gerät selbst und meldet ein Duplikat. Das Interface braucht einen zweiten Parameter, die Id
   des gerade bearbeiteten Geräts.

Leitfragen: Wie sieht ein `Any`-Lambda aus, das "gleiche Nummer **und** andere Id" prüft? Welchen
Wert übergibt man bei einem neuen Gerät, das noch keine Id hat?

### Schritt 6: Selbstkontrolle

Voraussetzung: Schritte 1 bis 4 sind erledigt.

Bevor der CSV-Import steht, ist die Datenbank leer. Zum Prüfen reicht ein vorübergehender Test im
Startcode, etwa in `App.xaml.cs`, der eine Kategorie und ein Gerät speichert, wieder lädt und das
Ergebnis mit `System.Diagnostics.Debug.WriteLine` ins Ausgabefenster schreibt. Dafür braucht es die
öffentlichen Konstruktoren aus Schritt 2a.

Prüffragen, die dieser Test beantworten sollte:

- Hat das Gerät nach dem Speichern eine Id größer 0?
- Liefert `GetItemById` mit dieser Id das Gerät zurück, und ist `Category` dabei gefüllt?
- Was passiert bei `GetItemById` mit einer Id, die es nicht gibt? Ist das Verhalten das, was du in
  1.6 entschieden hast?
- Meldet `CheckInventoryNumberDuplicate` für dieselbe Nummer mit einer **anderen** Id `true` und mit
  **seiner eigenen** Id `false`?

Wenn alle vier stimmen, sind Kontext, Mapping und Repository korrekt. Den Testcode danach wieder
entfernen.

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

## Fehlerbilder zur Laufzeit

| Meldung | Bedeutung |
|---|---|
| `No database provider has been configured` | `OnConfiguring` ruft kein `UseSqlite` auf |
| `SQLite Error 1: 'no such table'` | Migration nicht ausgeführt oder falscher Dateipfad |
| `Sequence contains no matching element` | `First` hat keinen Treffer gefunden, siehe 1.2 und 1.6 |
| `The expression '...' is invalid inside an 'Include' operation` | `Include` auf eine normale Spalte statt auf eine Navigationseigenschaft |
| `could not be translated` | Bedingung nutzt eine berechnete C#-Property, die es in der Datenbank nicht gibt |
| `NullReferenceException` auf `item.Category.Name` | `Include(i => i.Category)` fehlt bei dieser Abfrage |
| `The instance of entity type cannot be tracked because another instance with the same key` | zwei Kontexte oder dasselbe Objekt zweimal angemeldet, siehe Schritt 4 |
