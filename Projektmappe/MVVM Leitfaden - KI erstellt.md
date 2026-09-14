# Leitfaden MVVM (Phase 4 und 5)

Diese Datei erklärt das MVVM-Muster am konkreten Projekt, für die Phasen, in denen die Oberfläche
entsteht. Sie setzt voraus, dass Persistenz und Repositories stehen
([Persistenz Leitfaden - KI erstellt.md](Persistenz%20Leitfaden%20-%20KI%20erstellt.md)). Wie dort
stehen hier nur Strukturen, Signaturen und das Warum; der Code wird selbst geschrieben.

## 1. Welches Problem MVVM löst

Ohne MVVM landet die Logik im Code-Behind: Ein Klick auf "Ausleihen" ruft in
`MainWindow.xaml.cs` eine Methode auf, die Textfelder ausliest, die Datenbank abfragt, prüft und
eine `MessageBox` zeigt. Das funktioniert, hat aber zwei Folgen. Die Logik ist nur testbar, indem
man das Fenster startet und klickt. Und Oberfläche und Ablauf sind so verflochten, dass jede
Änderung am Layout den Ablauf berührt.

MVVM trennt das: Das Fenster weiß nur, *was* es anzeigt. *Welche* Daten das sind und *was* ein
Klick bewirkt, steht in einer normalen C#-Klasse, die kein WPF-Element kennt.

## 2. Die drei Rollen im Projekt

| Rolle | Im Projekt | Weiß von |
|---|---|---|
| **Model** | `Item`, `Employee`, `LendItem`, `Category` | nichts außer sich selbst |
| **ViewModel** | z. B. `MainViewModel`, `LendDialogViewModel` | Models und Services, aber **keine** Views |
| **View** | `MainWindow.xaml`, ein Ausleihdialog | nur ihr ViewModel, und das nur über Bindings |

Die wichtigste Regel steckt in der letzten Spalte: Ein ViewModel darf kein `TextBox`, kein
`Window` und keine `MessageBox` kennen. Sobald dort `using System.Windows.Controls` steht, ist die
Trennung aufgehoben.

## 3. Das Gesamtbild über alle Schichten

MVVM beschreibt nur die oberen Schichten. Zusammen mit der Persistenz ergibt sich:

```
View (XAML)
  │  Binding
ViewModel            hält Zustand für die Anzeige, reagiert auf Befehle
  │  ruft auf
Service              Geschäftsregeln: darf ausgeliehen werden? (R1, R2, R4)
  │  ruft auf
Repository           Datenzugriff: offene Ausleihe zu Gerät X laden
  │
DbContext / SQLite
```

Die **Service-Schicht** ist der Teil, der in Unterrichtsbeispielen oft fehlt. Sie beantwortet die
Frage "Was ist erlaubt?", während das Repository nur "Wie komme ich an die Daten?" beantwortet.
Ein `LendService` mit einer Methode wie `Lend(itemId, employeeId, lendDate, expectedReturnDate)`
fragt das Repository nach einer offenen Ausleihe, prüft `IsActive`, prüft die Datumsreihenfolge
und speichert erst dann. Das ViewModel ruft nur den Service auf und zeigt das Ergebnis an.

Genau dort, im Service, entstehen die Unit-Tests für R1, R2 und R4.

## 4. Die fünf Bausteine

### DataContext

Jedes WPF-Element hat eine Eigenschaft `DataContext`. Sie legt fest, auf welches Objekt sich die
Bindings darin beziehen. Sie vererbt sich nach unten: Wird sie am Fenster gesetzt, gilt sie für
alles im Fenster. Gesetzt wird sie einmal, etwa beim Erzeugen des Fensters:

```csharp
window.DataContext = mainViewModel;
```

### Binding

`{Binding Items}` im XAML heißt: Suche im DataContext eine öffentliche Property namens `Items` und
zeige ihren Wert. Mit `Mode=TwoWay` fließen Änderungen auch zurück, etwa aus einem Textfeld in die
Property. Die Binding-Engine arbeitet über Reflection, deshalb müssen gebundene Properties
`public` sein.

### INotifyPropertyChanged

Ein Binding liest eine Property einmal. Ändert sich der Wert danach im ViewModel, erfährt die View
davon nur, wenn das ViewModel es meldet. Dafür implementiert es `INotifyPropertyChanged` und löst
im Setter das Ereignis `PropertyChanged` mit dem Namen der Property aus.

Die übliche Lösung ist eine kleine Basisklasse, von der alle ViewModels erben:

```csharp
public abstract class ViewModelBase : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) { }
}
```

`[CallerMemberName]` setzt den Namen der aufrufenden Property automatisch ein, sodass im Setter nur
`OnPropertyChanged();` steht.

### ObservableCollection

Für Listen gilt dasselbe auf Ebene der Elemente. Eine `List<Item>` meldet nicht, wenn ein Eintrag
hinzukommt oder verschwindet; das `DataGrid` bleibt dann unverändert. Eine
`ObservableCollection<Item>` meldet genau das. Für alles, was an ein `DataGrid` oder eine `ListBox`
gebunden wird, ist sie die richtige Wahl.

### ICommand

Buttons werden nicht an Click-Ereignisse gehängt, sondern an Befehle:
`Command="{Binding LendCommand}"`. Ein Befehl ist ein Objekt mit zwei Methoden: `Execute` führt die
Aktion aus, `CanExecute` sagt, ob sie gerade möglich ist. Liefert `CanExecute` false, graut WPF den
Button automatisch aus, etwa solange kein Gerät ausgewählt ist.

WPF bringt keine fertige Implementierung mit. Die übliche Lösung ist eine eigene Klasse
`RelayCommand`, die im Konstruktor zwei Delegates entgegennimmt:

```csharp
public class RelayCommand : ICommand
{
    public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null) { }
}
```

Das *CommunityToolkit.Mvvm* nimmt einem diese Klasse und die Basisklasse ab, erzeugt dafür aber
Code per Quellgenerator. Da das Projekt vollständig von Hand geschrieben sein soll, sind die eigene
`ViewModelBase` und der eigene `RelayCommand` die passende Wahl. Beide sind je etwa 20 Zeilen und
lassen sich in der Dokumentation gut erklären.

## 5. Ein Ablauf durch alle Schichten: Gerät ausleihen

1. Das `DataGrid` in `MainWindow.xaml` ist an `MainViewModel.Items` gebunden, die Auswahl an
   `MainViewModel.SelectedItem`.
2. Der Lagerist wählt eine Zeile. Der Setter von `SelectedItem` wird aufgerufen und meldet die
   Änderung. `LendCommand.CanExecute` liefert jetzt true, der Button wird aktiv.
3. Klick auf "Ausleihen" ruft `LendCommand.Execute` auf. Das ViewModel öffnet den Ausleihdialog.
4. Im Dialog bestätigt der Lagerist. Das `LendDialogViewModel` ruft
   `LendService.Lend(...)` auf.
5. Der Service fragt `ILendItemRepository.GetOpenLend(itemId)`. Gibt es einen Treffer, liefert er
   einen Fehler zurück (R1). Sonst legt er ein `LendItem` an und speichert.
6. Das ViewModel bekommt das Ergebnis, zeigt bei Fehler eine Meldung oder aktualisiert bei Erfolg
   den Status in der Liste.

Kein Schritt davon steht in einer `.xaml.cs`-Datei.

## 6. Wie das ViewModel an seine Services kommt

Das ViewModel erzeugt seine Services nicht selbst, sondern bekommt sie im Konstruktor übergeben:

```csharp
public MainViewModel(IItemRepository items, LendService lendService) { }
```

Zusammengesetzt wird alles an genau einer Stelle, beim Programmstart in `App.xaml.cs`: DbContext
erzeugen, Repositories erzeugen, Services erzeugen, ViewModel erzeugen, Fenster erzeugen,
DataContext setzen, Fenster anzeigen. Dafür wird in `App.xaml` das `StartupUri` entfernt und
stattdessen `OnStartup` überschrieben.

Stand im Projekt: `App.OnStartup` erzeugt bereits den `LendContext` und die drei Repositories. Für
Phase 4 sind dort noch drei Dinge zu ergänzen:

- Die Repositories sind derzeit lokale Variablen und verschwinden am Ende von `OnStartup`. Sobald
  ViewModel und Fenster in derselben Methode erzeugt werden, reicht das, weil das ViewModel die
  Repositories im Konstruktor übernimmt und festhält.
- Der `LendContext` gehört dagegen in ein Feld, denn er muss beim Beenden in `OnExit` mit `Dispose`
  freigegeben werden. Dafür muss `OnExit` an ihn herankommen.
- `StartupUri="MainWindow.xaml"` in `App.xaml` entfernen, sonst öffnet WPF ein zweites Fenster ohne
  ViewModel.

Ein Dependency-Injection-Container ist dafür nicht nötig. Bei vier Repositories und zwei Services
ist das Zusammensetzen von Hand übersichtlicher und in der Dokumentation leichter zu zeigen.

## 7. Ordnerstruktur

```
Digitale_Geraeteliste/
  Core/
    Model/
    Interfaces/
    Services/          LendService
  Data/
  ViewModels/          ViewModelBase, RelayCommand, MainViewModel, LendDialogViewModel
  Views/               MainWindow.xaml, LendDialog.xaml
```

## 8. Reihenfolge zum Lernen

Nicht alles auf einmal bauen. Jede Stufe ist für sich lauffähig:

1. `ViewModelBase` und ein `MainViewModel` mit einer `ObservableCollection<Item>`, die im
   Konstruktor aus dem Repository gefüllt wird. Im Fenster nur ein `DataGrid` mit
   `ItemsSource="{Binding Items}"`. Ziel: 120 Geräte sichtbar.
2. `SelectedItem` ergänzen und daneben ein paar `TextBlock`s, die Details des ausgewählten Geräts
   zeigen. Ziel: Auswahl wirkt sich sichtbar aus. Hier wird `INotifyPropertyChanged` verstanden.
3. `RelayCommand` schreiben und einen ersten Befehl binden, der noch nichts Fachliches tut, etwa
   die Liste neu lädt. Ziel: Button graut aus und wird aktiv.
4. Erst jetzt Filter, Suche und den Ausleihdialog.

## Häufige Fehlerbilder

| Symptom | Ursache |
|---|---|
| DataGrid bleibt leer, kein Fehler | DataContext nicht gesetzt, oder Property nicht `public` |
| Wert ändert sich im ViewModel, Anzeige nicht | `OnPropertyChanged` im Setter fehlt |
| Neuer Datensatz erscheint nicht in der Liste | `List<T>` statt `ObservableCollection<T>` |
| Button bleibt immer grau oder immer aktiv | `CanExecuteChanged` wird nicht ausgelöst |
| Unklar, warum ein Binding nicht greift | Ausgabefenster von Visual Studio lesen, dort steht jeder Binding-Fehler mit Property-Namen |
