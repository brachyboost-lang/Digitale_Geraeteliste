# Leitfaden Geschäftslogik und Unit-Tests (Phase 3 und 7)

Arbeitsanleitung für den Schritt von der fertigen Datenschicht zu einem `LendService`, der die
Geschäftsregeln R1 bis R5 durchsetzt, und zu den Unit-Tests, die genau diese Regeln prüfen. Sie folgt
dem Aufbau der [EF Core und LINQ Schritt fuer Schritt - KI erstellt.md](EF%20Core%20und%20LINQ%20Schritt%20fuer%20Schritt%20-%20KI%20erstellt.md):
Teil 1 erklärt die Konzepte mit Syntaxbeispielen an **anderen** Klassen als denen, die umgesetzt werden,
Teil 2 ist der Arbeitsweg mit Aufgaben und Leitfragen. Lösungen stehen hier bewusst nicht.

Regeln und Testfälle stammen aus [Projektauftrag - KI erstellt.md](Projektauftrag%20-%20KI%20erstellt.md),
Abschnitt 3 und 7.

## Warum Service und Tests zusammen

Im Zeitplan des Projektauftrags liegen Geschäftslogik (Phase 3) und Unit-Tests (Phase 7) weit
auseinander. Dieser Leitfaden zieht die Tests direkt hinter den Service. Zwei Gründe: Die Regeln sind
dann noch frisch im Kopf, und die Oberfläche in Phase 4 baut auf einem Service auf, von dem feststeht,
dass er richtig entscheidet. Findet ein Test später im UI-Bau einen Fehler, weiß man sonst nicht, ob er
im ViewModel oder in der Regel steckt.

## Voraussetzungen

Die Datenschicht ist fertig und zur Laufzeit geprüft: Import, zweiter Start ohne Duplikate,
`ChangeLendItem` mit echten Namen im Log. Vor dem Start dieses Leitfadens:

1. **Testaufruf entfernen.** In `App.OnStartup` steht noch `ChangeLendItem(31, ...)`. Er ändert bei jedem
   Start die Ausleihe 31 erneut.
2. **Testdaten zurücksetzen.** Ausleihe 31 weicht durch die Tests von der CSV ab. Die Datei
   `bin/Debug/net10.0-windows/Digitale_Geraeteliste.db` löschen, beim nächsten Start baut der Import
   den Ausgangszustand neu auf. Die Tabellen legt EF über die Migrationen an, sofern `database update`
   gelaufen ist, sonst vorher `dotnet ef database update` ausführen.
3. **Commit.** Datenschicht abgeschlossen ist ein sauberer Zwischenstand.

---

## Teil 1: Die Werkzeuge verstehen

### 1.1 Was ein Service ist und was nicht

| Schicht | Beantwortet | Beispiel im Projekt |
|---|---|---|
| Repository | Wie komme ich an die Daten? | `GetOpenLendByItemId(itemId)` liefert die offene Ausleihe oder `null` |
| Service | Was ist erlaubt? | "Gerät 4 darf nicht ausgeliehen werden, weil es schon verliehen ist" |
| ViewModel | Was sieht der Benutzer? | Meldung anzeigen, Button ausgrauen |

Der Service ruft Repositories auf, entscheidet und gibt ein Ergebnis zurück. Er kennt keine Datenbank,
keinen `DbContext` und kein WPF. Deshalb liegt er in `Core/Services/` und nicht in `Data/`.

Daraus folgt eine Regel für die Repositories: Die Prüfungen, die jetzt schon in `ChangeLendItem`
stecken, etwa "zurückgegebene Ausleihe nicht ändern", sind eigentlich Geschäftsregeln. Sie dürfen dort
bleiben, solange sie nicht doppelt und widersprüchlich geprüft werden. Neue Regeln kommen ausschließlich
in den Service.

### 1.2 Ein Ergebnisobjekt statt `bool`

Ein `bool` sagt nur, **ob** etwas fehlgeschlagen ist. Der Service hat aber mehrere Gründe zu scheitern,
und das ViewModel soll dem Lageristen sagen können, welcher es war. Dafür gibt es eine kleine Klasse,
die beides trägt. In den Diagrammvorlagen heißt sie `BuchungsErgebnis`.

Beispiel mit einem allgemeinen Namen:

```csharp
public class OperationResult
{
    public bool IsSuccess { get; }
    public string ErrorMessage { get; }

    private OperationResult(bool isSuccess, string errorMessage)
    {
        IsSuccess = isSuccess;
        ErrorMessage = errorMessage;
    }

    public static OperationResult Success() => new OperationResult(true, string.Empty);
    public static OperationResult Failure(string message) => new OperationResult(false, message);
}
```

Drei Sprachmittel stecken darin:

- **Properties nur mit `get`** können nach dem Konstruktor nicht mehr verändert werden. Ein Ergebnis
  soll sich nachträglich nicht umschreiben lassen.
- **Privater Konstruktor:** Von außen lässt sich das Objekt nur über die beiden statischen Methoden
  erzeugen. Damit ist ausgeschlossen, dass jemand `IsSuccess = true` mit einer Fehlermeldung kombiniert.
- **`static`-Methoden** gehören zur Klasse, nicht zu einem Objekt. Aufruf über den Klassennamen:
  `return OperationResult.Failure("Kategorie existiert bereits.");`

Verwendung beim Aufrufer:

```csharp
OperationResult result = categoryService.Create("Messtechnik");
if (!result.IsSuccess)
{
    // result.ErrorMessage anzeigen
}
```

**Aufgabe:** Überlege, ob dein Ergebnisobjekt zusätzlich das angelegte Objekt zurückgeben soll, etwa die
neue Ausleihe mit ihrer vergebenen Id. Wann bräuchte das ViewModel sie?

### 1.3 Abhängigkeiten über Interfaces übergeben

Der Service bekommt seine Repositories im Konstruktor, wie die Repositories ihren Kontext. Der
entscheidende Unterschied: Der Parametertyp ist das **Interface**, nicht die Klasse.

```csharp
public class CategoryService
{
    private readonly ICategoryRepository _categories;

    public CategoryService(ICategoryRepository categories)
    {
        _categories = categories;
    }
}
```

In `App.OnStartup` wird das echte Repository übergeben. Im Unit-Test wird stattdessen eine kleine
Test-Implementierung übergeben, die mit einer Liste arbeitet (siehe 1.7). Der Service merkt davon
nichts. Genau das ist der Grund, warum die Interfaces seit Phase 2 existieren.

### 1.4 Guard Clauses: Regeln nacheinander prüfen

Jede Regel wird am Anfang der Methode einzeln geprüft. Ist sie verletzt, verlässt die Methode sofort mit
einem Fehlerergebnis. Der eigentliche Ablauf steht erst danach, ohne zusätzliche Einrückung.

```csharp
public OperationResult Rename(int categoryId, string newName)
{
    if (string.IsNullOrWhiteSpace(newName))
    {
        return OperationResult.Failure("Name darf nicht leer sein.");
    }

    if (_categories.NameExists(newName, categoryId))
    {
        return OperationResult.Failure("Name wird bereits verwendet.");
    }

    _categories.Rename(categoryId, newName);
    return OperationResult.Success();
}
```

Die **Reihenfolge** der Prüfungen ist eine Entscheidung. Günstige zuerst, teure zuletzt: Eine Prüfung
auf einen leeren String kostet nichts, eine Datenbankabfrage schon.

**Leitfrage:** Wenn ein Gerät gleichzeitig ausgemustert und verliehen ist, welche Meldung soll der
Lagerist sehen? Das legt die Reihenfolge von R1 und R2 fest.

### 1.5 Datumswerte ohne Uhrzeit

Beim letzten Test ist in Ausleihe 31 das Ausgabedatum `2026-09-07 18:37:57` gelandet, weil der Aufruf
`DateTime.Now.AddDays(-10)` übergeben hat. Fristen im Projekt sind aber Kalendertage.

| Ausdruck | Ergebnis |
|---|---|
| `DateTime.Now` | Datum und Uhrzeit |
| `DateTime.Today` | heutiges Datum, Uhrzeit 00:00 |
| `someDate.Date` | dasselbe Datum, Uhrzeit auf 00:00 gesetzt |

Der Service ist der richtige Ort, um das sicherzustellen: Er nimmt ein Datum entgegen und arbeitet
intern nur mit `.Date`. Dann spielt es keine Rolle mehr, ob der Aufrufer eine Uhrzeit mitschickt.

### 1.6 "Heute" gehört dem Aufrufer

Eine Regel wie "Rückgabedatum darf nicht in der Zukunft liegen" braucht das heutige Datum. Liest der
Service `DateTime.Today` selbst, ist sie nur an dem Tag testbar, an dem der Test geschrieben wurde. Das
ist dasselbe Problem wie bei der Überfälligkeit, das `IsOverdueAt(DateTime)` löst: Der Stichtag wird als
Parameter übergeben.

**Leitfrage:** Welche Methoden deines Service brauchen ein "heute"? Für welche reicht das übergebene
Ausgabe- oder Rückgabedatum?

### 1.7 Unit-Tests mit xUnit

**Aufbau eines Tests: Arrange, Act, Assert.**

```csharp
public class OperationResultTests
{
    [Fact]
    public void Failure_SetsMessageAndIsNotSuccess()
    {
        // Arrange
        string message = "Fehler";

        // Act
        OperationResult result = OperationResult.Failure(message);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(message, result.ErrorMessage);
    }
}
```

- `[Fact]` markiert eine Methode als Test. Sie ist `public void` und hat keine Parameter.
- Der Methodenname beschreibt, **was** passiert und **was erwartet** wird. Bei einem roten Test steht
  der Name im Testexplorer, und er sollte ohne Blick in den Code verraten, was kaputt ist.
- Die drei Kommentare trennen Vorbereitung, Aufruf und Prüfung. Das ist eine verbreitete Konvention und
  in der Dokumentation gut erklärbar.

**Die wichtigsten `Assert`-Methoden:**

| Methode | Prüft |
|---|---|
| `Assert.True(x)` / `Assert.False(x)` | Wahrheitswert |
| `Assert.Equal(erwartet, tatsächlich)` | Gleichheit, erwarteter Wert **zuerst** |
| `Assert.Null(x)` / `Assert.NotNull(x)` | `null` oder nicht |
| `Assert.Single(liste)` | Liste hat genau ein Element |
| `Assert.Throws<ArgumentException>(() => ...)` | Aufruf wirft genau diese Exception |

**Test-Implementierung eines Repositorys (Fake).** Der Service erwartet ein Interface. Im Test übergibst
du eine Klasse, die dieses Interface mit einer einfachen Liste erfüllt:

```csharp
internal class FakeCategoryRepository : ICategoryRepository
{
    public List<Category> Categories { get; } = new List<Category>();

    public bool NameExists(string name, int exceptId)
    {
        return Categories.Any(c => c.Name == name && c.Id != exceptId);
    }

    public void Rename(int id, string newName)
    {
        Categories.First(c => c.Id == id).Name = newName;
    }
}
```

Im Arrange-Teil füllst du die Liste genau mit den Daten, die der Testfall braucht, und übergibst die
Fake-Klasse an den Service. Methoden, die ein Test nicht braucht, dürfen
`throw new NotImplementedException();` enthalten. Ruft der Service sie unerwartet auf, fällt das sofort
auf.

**Leitfrage:** Warum ist ein Fake mit einer Liste für die Regeltests besser als die echte
SQLite-Datenbank mit den Testdaten? Denk an die 6 überfälligen Ausleihen, aus denen nach sechs Tagen 18
wurden.

---

## Teil 2: Der Weg im Projekt

### Schritt 1: Voraussetzungen erledigen

Siehe Abschnitt "Voraussetzungen" oben.

### Schritt 2: Das Ergebnisobjekt

Ordner `Core/Services/` anlegen und darin die Ergebnisklasse nach 1.2 schreiben. Name nach eigener
Wahl, im Klassendiagramm-Entwurf steht `BuchungsErgebnis`.

Kontrolle: kompiliert, und es gibt keinen Weg, von außen ein Ergebnis mit `IsSuccess == true` und
gefüllter Fehlermeldung zu erzeugen.

### Schritt 3: Das Gerüst des `LendService`

- Klasse `LendService` in `Core/Services/`, `public`.
- Konstruktor nach 1.3. **Leitfrage:** Welche Interfaces braucht der Service für das Ausleihen, welche
  für das Zurückgeben? Nimm nur die, die er tatsächlich verwendet.
- Noch keine Methoden. Kompilieren.

**Kontrolle:** Die Datei hat kein einziges `using` auf `Microsoft.EntityFrameworkCore`,
`Digitale_Geraeteliste.Data` oder `System.Windows`. Taucht eines davon auf, kennt der Service eine
Schicht, die er nicht kennen darf.

### Schritt 4: Ausleihen mit R1, R2 und R4

Eine Methode, die eine neue Ausleihe anlegt und dabei die Regeln durchsetzt. Rückgabetyp ist die
Ergebnisklasse aus Schritt 2.

Prüfungen, jeweils als Guard Clause nach 1.4:

| Regel | Frage an die Daten | Werkzeug |
|---|---|---|
| Gerät existiert | Gibt es ein Gerät mit dieser Id? | `IItemRepository.GetItemById`, wirft bei unbekannter Id |
| R2 | Ist das Gerät ausgemustert? | `Item.IsRetired` |
| R1 | Hat das Gerät eine offene Ausleihe? | `ILendItemRepository.GetOpenLendByItemId`, liefert `null` oder die Ausleihe |
| R4 | Liegt das Rückgabedatum vor dem Ausgabedatum? | Datumsvergleich nach 1.5 |

Leitfragen:

1. `GetItemById` wirft eine `ArgumentException`, wenn es die Id nicht gibt. Soll der Service diese
   Exception mit `try`/`catch` in ein Fehlerergebnis umwandeln oder durchreichen? Denk an deine
   Unterscheidung "erwartbar" gegen "sollte nicht vorkommen".
2. `CreateNewLendItem` im Repository nimmt eine **Dauer** in Tagen, nicht ein Rückgabedatum. Wie kann R4
   dann überhaupt verletzt werden? Reicht die Prüfung "Dauer darf nicht negativ sein", oder soll der
   Service lieber ein Rückgabedatum entgegennehmen, weil der Lagerist im Dialog ein Datum auswählt?
3. `NeedsMaintenance` steht in keiner Regel. Soll ein Gerät in Wartung ausleihbar sein? Wenn nein, ist
   das eine neue Regel, die in den Projektauftrag gehört.
4. Der `LendItem`-Konstruktor berechnet die Frist aus der Standard-Leihdauer. Ist das eine
   Geschäftsregel, die in den Service gehört, oder eine Eigenschaft des Objekts? Beides ist vertretbar,
   aber es sollte nur an **einer** Stelle stehen.

### Schritt 5: Zurückgeben

Eine Methode für die Rückgabe. Prüfungen:

- Ausleihe existiert.
- Ausleihe ist noch offen. Die Repository-Methode `ReturnLendItem` prüft das bisher nicht, ein zweiter
  Aufruf würde das Rückgabedatum überschreiben.
- Rückgabedatum liegt nicht vor dem Ausgabedatum.

**Leitfrage:** `ReturnLendItem` im Repository liefert `false`, wenn die Id nicht existiert. Der Service
prüft die Existenz aber schon vorher. Wofür steht `false` dann noch?

### Schritt 6: Ändern einer Ausleihe

`ChangeLendItem` im Repository kann das Gerät einer laufenden Ausleihe austauschen. Dabei kann R1
verletzt werden: Das neue Gerät ist vielleicht schon verliehen.

**Leitfrage:** `GetOpenLendByItemId` für das neue Gerät liefert eine offene Ausleihe. In welchem Fall ist
das trotzdem **kein** Verstoß? (Hinweis: Was passiert, wenn das Gerät gar nicht gewechselt wird?)

### Schritt 7: Testprojekt anlegen

Das Testprojekt liegt **neben** dem Anwendungsprojekt, nicht darin. Ein SDK-Projekt kompiliert jede
`.cs`-Datei in seinem Ordner und allen Unterordnern mit. Ein Testprojekt unter `Digitale_Geraeteliste/`
würde deshalb in die Anwendung hineinkompiliert.

Im Ordner `G:\C# Projects\IHK Probeprojekt`:

```
dotnet new xunit -n Digitale_Geraeteliste.Tests
dotnet add Digitale_Geraeteliste.Tests reference Digitale_Geraeteliste/Digitale_Geraeteliste.csproj
dotnet sln Digitale_Geraeteliste/Digitale_Geraeteliste.slnx add Digitale_Geraeteliste.Tests/Digitale_Geraeteliste.Tests.csproj
```

**Wichtig, sonst kompiliert der Verweis nicht:** Die Anwendung zielt auf `net10.0-windows`, weil sie
WPF verwendet. Die Vorlage erzeugt ein Testprojekt mit `net10.0`. Ein Projekt ohne `-windows` kann kein
Projekt mit `-windows` referenzieren. In der `.csproj` des Testprojekts muss deshalb
`<TargetFramework>net10.0-windows</TargetFramework>` stehen.

Die Repositories sind `internal`, die Interfaces, Modelle und der Service `public`. Für die Tests des
Service reicht das, weil die Tests nur mit Interfaces, Fakes, Modellen und dem Service arbeiten.

Ausführen mit:

```
dotnet test
```

oder in Visual Studio über **Test > Test-Explorer**.

### Schritt 8: Der erste Test ohne Fakes

Beginne mit `LendItem.IsOverdueAt`. Die Methode braucht weder Repository noch Service, nur ein
`LendItem` und ein Datum. Das ist der leichteste Einstieg, um Projektaufbau und Syntax aus 1.7 zu
prüfen, bevor Fakes dazukommen.

Mindestens drei Tests:

- Offene Ausleihe, Stichtag **nach** der Frist: überfällig.
- Offene Ausleihe, Stichtag **genau am** Fristtag: nicht überfällig.
- Zurückgegebene Ausleihe, Stichtag nach der Frist: nicht überfällig.

**Leitfrage:** Der Konstruktor von `LendItem` verlangt ein `Employee` und ein `Item`. Wie erzeugst du im
Test die einfachsten gültigen Objekte dafür?

### Schritt 9: Service-Tests mit Fakes

Für `ILendItemRepository` und `IItemRepository` je eine Fake-Klasse nach 1.7 im Testprojekt. Dann für
jeden Abnahme-Testfall aus dem Projektauftrag, der eine Regel betrifft, einen Test:

| Testfall | Regel | Erwartetes Ergebnis im Service |
|---|---|---|
| T1 | Gerät verfügbar | Erfolg, Repository hat eine neue Ausleihe erhalten |
| T2 | R1, Gerät verliehen | Fehler, keine neue Ausleihe im Fake |
| T3 | R2, Gerät ausgemustert | Fehler, keine neue Ausleihe im Fake |
| T5 | R4, Rückgabe vor Ausgabe | Fehler |
| T6 | Rückgabe einer offenen Ausleihe | Erfolg, Rückgabedatum gesetzt |
| T7 | Überfälligkeit | abgedeckt durch Schritt 8 |

T4 (Inventarnummer doppelt, R3) und T8 (CSV-Export) gehören zu anderen Klassen und kommen später.

**Leitfrage:** Wie stellst du bei T2 fest, dass der Service das Repository **nicht** zum Anlegen
aufgerufen hat? Tipp: Der Fake kann mitzählen oder die angelegten Ausleihen in einer Liste sammeln.

### Schritt 10: Selbstkontrolle

- `dotnet test` zeigt nur grüne Tests.
- Absichtlich eine Regel im Service auskommentieren, zum Beispiel R1, und erneut testen. Mindestens ein
  Test muss rot werden. Wenn nicht, prüft kein Test diese Regel. Danach die Änderung zurücknehmen.
- Der Service hat keinen Verweis auf `Data`, EF Core oder WPF (Schritt 3).

Damit ist die Geschäftslogik fertig und abgesichert, und Phase 4 beginnt mit dem
[MVVM Leitfaden - KI erstellt.md](MVVM%20Leitfaden%20-%20KI%20erstellt.md).

---

## Fehlerbilder

| Meldung | Bedeutung |
|---|---|
| `NU1201: Project Digitale_Geraeteliste is not compatible with net10.0` | Testprojekt zielt auf `net10.0` statt `net10.0-windows`, Schritt 7 |
| Anwendung kompiliert plötzlich Testklassen oder findet `Xunit` nicht | Testprojekt liegt im Ordner der Anwendung, Schritt 7 |
| Test-Explorer zeigt keine Tests | Methode nicht `public`, fehlendes `[Fact]` oder Projekt nicht gebaut |
| `CS0122: '...' is inaccessible due to its protection level` im Test | Test greift auf eine `internal`-Klasse zu, etwa ein echtes Repository statt eines Fakes |
| `NotImplementedException` im Test | Service ruft eine Fake-Methode auf, mit der der Test nicht gerechnet hat |
| Test wird nach einigen Tagen rot | Test verwendet `DateTime.Today` statt eines festen Datums, siehe 1.6 |
