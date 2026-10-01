# Restplan: 2 Tage Programm, 2 Tage Doku

Arbeitsplan für den Abschluss des Probeprojekts unter Zeitdruck. Er legt fest, was gebaut wird, was
bewusst entfällt, und in welcher Reihenfolge. Grundlage ist der Stand vom 1. Oktober: Datenschicht
fertig und geprüft, Service-Schicht als leeres Gerüst, keine Oberfläche, keine Tests.

Details zur Umsetzung stehen in den bestehenden Leitfäden:
[Service und Tests](Service%20und%20Tests%20Leitfaden%20-%20KI%20erstellt.md),
[MVVM](MVVM%20Leitfaden%20-%20KI%20erstellt.md),
[EF Core und LINQ](EF%20Core%20und%20LINQ%20Schritt%20fuer%20Schritt%20-%20KI%20erstellt.md).

## 1. Was gebaut wird und was entfällt

Bewertet wird eine **lauffähige Anwendung, die die Anforderungen erfüllt**, plus eine Dokumentation,
die Entscheidungen begründet. Deshalb hat alles Vorrang, was in der Präsentation vorgeführt wird.

| Anforderung | Entscheidung |
|---|---|
| A5 Bestandsübersicht mit Status, Suche, Filter | **bauen**, das ist der Kern der Anwendung |
| A3 Ausleihe buchen | **bauen** |
| A4 Rückgabe buchen | **bauen** |
| A6 Überfälligkeitsliste | **bauen** |
| A7 CSV-Export | **bauen**, klein |
| R1, R2, R4 im `LendService` | **bauen**, daran hängen die Tests |
| Unit-Tests | **bauen**, fünf Tests reichen für den Nachweis |
| A1 Gerätestammdaten anlegen und bearbeiten | **bauen, wenn Zeit bleibt**, Repository und Service sind fertig, es fehlt nur der Dialog |
| A2 Mitarbeiterverwaltung | **entfällt**, Mitarbeiter kommen aus dem Import und werden nur angezeigt |
| Ausleihe nachträglich ändern im UI | **entfällt**, `ChangeLendItem` bleibt als Service-Methode mit Protokoll bestehen |
| Kategorien verwalten | **entfällt**, acht Kategorien kommen aus dem Import |

Die drei Streichungen gehören in die Dokumentation, in den Abschnitt zur Abgrenzung. Formuliert als
Entscheidung mit Begründung sind sie ein Pluspunkt, nicht eine Lücke: Der Auftrag nennt die
Kladdenablösung als Ziel, und dafür sind Stammdatenpflege und nachträgliche Korrekturen zweitrangig.

## 2. Tag 1

### 2.1 Build reparieren und Gerüst aufräumen (20 min)

Der Build ist rot:

```
EmployeeService.cs(9,26): error CS0246: The type or namespace name 'IEmployeeRepository' could not be found
```

In `EmployeeService` fehlt `using Digitale_Geraeteliste.Core.Interfaces;`. Da A2 entfällt, ist die
schnellere Lösung, die Datei zu löschen. Gleiches gilt für `CategoryService`, `ItemService` (leer),
`ICategoryService`, `IEmployeeService`, `IItemService`. Leere Klassen im Projekt fallen in der
Code-Besprechung negativ auf.

Was bleibt: `TransactionResult` (fertig), `LendService`, `ILendService`.

### 2.2 LendService mit den drei Regeln (60 min)

Siehe Service-Leitfaden, Schritte 3 bis 5. Zwei Methoden reichen:

- **Ausleihen:** Gerät existiert, nicht ausgemustert (R2), keine offene Ausleihe (R1), Dauer nicht
  negativ (R4), dann `CreateNewLendItem`.
- **Zurückgeben:** Ausleihe existiert, noch offen, Rückgabedatum nicht vor Ausgabedatum, dann
  `ReturnLendItem`.

Rückgabetyp beider Methoden ist `TransactionResult`. Die Reihenfolge der Prüfungen entscheidet, welche
Meldung der Lagerist zuerst sieht.

### 2.3 Fünf Unit-Tests (60 min)

Testprojekt nach Schritt 7 des Service-Leitfadens anlegen. Die beiden Fallen dort nicht überlesen:
Projekt **neben** den Anwendungsordner, Zielframework `net10.0-windows`.

Diese fünf Tests deckn die Prüfungsanforderung ab:

1. `IsOverdueAt`: offene Ausleihe, Stichtag nach der Frist, überfällig.
2. `IsOverdueAt`: zurückgegebene Ausleihe, Stichtag nach der Frist, nicht überfällig.
3. Ausleihen mit freiem Gerät: Erfolg (T1).
4. Ausleihen mit bereits verliehenem Gerät: Fehler, keine neue Ausleihe (T2, R1).
5. Ausleihen mit ausgemustertem Gerät: Fehler (T3, R2).

### 2.4 Statusermittlung für die Übersicht (40 min)

A5 braucht pro Gerät den Status. Naiv würde man für jedes der 120 Geräte einzeln die offene Ausleihe
abfragen, das sind 120 Datenbankabfragen. Besser: **einmal** alle offenen Ausleihen laden und nach
`ItemId` in ein Nachschlagewerk legen.

```csharp
Dictionary<int, LendItem> openByItem = openLends.ToDictionary(l => l.ItemId);
```

`ToDictionary` baut aus einer Liste eine Zuordnung, hier von Geräte-Id auf Ausleihe. Nachschlagen mit
`openByItem.TryGetValue(item.Id, out LendItem? lend)`: Die Methode liefert `true`, wenn ein Eintrag
existiert, und legt ihn in `lend` ab.

Dazu ein Aufzählungstyp für den Status und eine kleine Klasse, die eine Zeile der Übersicht beschreibt:
Inventarnummer, Name, Kategorie, Status, Entleiher, Frist. Diese Zeilenklasse ist das, was im `DataGrid`
landet.

Leitfrage: Woran erkennst du die vier Zustände? `Ausgemustert` steht am Gerät, `Verliehen` und
`Überfällig` ergeben sich aus der offenen Ausleihe und dem Stichtag, `Verfügbar` ist der Rest.

### 2.5 Hauptfenster mit Bestandsübersicht (90 min)

MVVM-Leitfaden, Abschnitt 8, Stufen 1 und 2. Reihenfolge:

1. `ViewModelBase` und `RelayCommand` schreiben (je etwa 20 Zeilen).
2. `MainViewModel` mit `ObservableCollection` der Zeilenklasse aus 2.4, gefüllt im Konstruktor.
3. In `App.OnStartup`: Repositories, Service, ViewModel und Fenster erzeugen, `DataContext` setzen,
   `StartupUri` aus `App.xaml` entfernen. Den Kontext als Feld halten und in `OnExit` freigeben.
4. `MainWindow.xaml`: `DataGrid` mit `AutoGenerateColumns="False"` und eigenen Spalten.
5. Suchfeld und Statusfilter.

**Zeitsparender Weg für Suche und Filter:** Statt `ICollectionView` einfach im ViewModel eine Methode
`Refresh()`, die die Liste neu aus dem Service lädt und dabei Suchtext und Statusfilter als Parameter
weitergibt. Bei 120 Geräten ist das unmerklich schnell und braucht kein weiteres WPF-Konzept. Die
Setter von `SearchText` und `SelectedStatus` rufen `Refresh()` auf.

**Spalten im `DataGrid`:**

```xml
<DataGrid ItemsSource="{Binding Rows}" SelectedItem="{Binding SelectedRow}"
          AutoGenerateColumns="False" IsReadOnly="True">
    <DataGrid.Columns>
        <DataGridTextColumn Header="Inventarnr." Binding="{Binding InventoryNumber}" />
        <DataGridTextColumn Header="Bezeichnung" Binding="{Binding Name}" Width="*" />
    </DataGrid.Columns>
</DataGrid>
```

`IsReadOnly` verhindert, dass im Grid direkt getippt wird. `Width="*"` verteilt den Restplatz.

**Ziel am Ende von Tag 1:** Das Fenster zeigt 120 Geräte mit Status, Suche und Filter funktionieren.
Das ist der Punkt, an dem die Anwendung vorführbar wird.

## 3. Tag 2

### 3.1 Ausleihen und Zurückgeben (90 min)

MVVM-Leitfaden, Stufe 3 und 4.

- Zwei Befehle im `MainViewModel`, gebunden an Buttons. `CanExecute` liefert nur `true`, wenn eine
  Zeile ausgewählt ist und der Status passt: Ausleihen nur bei `Verfügbar`, Rückgabe nur bei
  `Verliehen` oder `Überfällig`. Damit sieht der Lagerist die Regeln schon an den Buttons.
- Ein Dialogfenster für die Ausleihe: Mitarbeiterauswahl (`ComboBox`), vorbelegtes Rückgabedatum
  (`DatePicker`), Bemerkungsfeld für die Bauvorhabennummer.
- Der Dialog wird mit `ShowDialog()` geöffnet. Er liefert über `DialogResult` zurück, ob bestätigt oder
  abgebrochen wurde.
- Nach Erfolg `Refresh()` aufrufen, bei `IsSuccess == false` die `ErrorMessage` des
  `TransactionResult` in einer `MessageBox` zeigen.

Die Rückgabe braucht keinen eigenen Dialog: Eine Bestätigung mit `MessageBox.Show(..., MessageBoxButton.YesNo)`
genügt, Rückgabedatum ist heute.

### 3.2 Überfälligkeitsliste und CSV-Export (60 min)

- Zweite Ansicht, am einfachsten als zweiter Tab (`TabControl`) im Hauptfenster: ein `DataGrid`,
  gebunden an die überfälligen Ausleihen mit Gerät, Entleiher, Frist und Tagen Überschreitung.
  `GetAllOverdueLendItems` lädt die Verweise schon mit.
- Export-Button. Dateiauswahl über `SaveFileDialog` aus `Microsoft.Win32`:

```csharp
var dialog = new SaveFileDialog { FileName = "ueberfaellig.csv", Filter = "CSV-Datei|*.csv" };
if (dialog.ShowDialog() == true)
{
    File.WriteAllLines(dialog.FileName, lines, Encoding.UTF8);
}
```

`lines` ist eine `IEnumerable<string>`: erste Zeile die Spaltentitel, danach je Ausleihe eine Zeile mit
Semikolon als Trennzeichen, wie bei den Importdateien. Die bisher leere Klasse `CSVExport` ist der
richtige Ort für das Zusammenbauen der Zeilen, das Anzeigen des Dialogs gehört ins ViewModel.

Leitfrage: Was passiert, wenn eine Bemerkung ein Semikolon enthält? Entscheide, ob du das abfängst oder
in der Doku als bekannte Grenze nennst.

### 3.3 Gerätestammdaten, falls Zeit bleibt (60 min)

Dialog zum Anlegen und Bearbeiten eines Geräts. `ItemRepository.CreateNewItem`, `ChangeItem` und
`CheckInventoryNumberDuplicate` sind fertig, es fehlt nur die Oberfläche plus eine Service-Methode, die
R3 prüft und ein `TransactionResult` liefert. Deaktivieren heißt `IsRetired = true` setzen.

### 3.4 Abnahme und Material für die Doku (60 min)

Die acht Testfälle T1 bis T8 aus dem Projektauftrag von Hand durchspielen und protokollieren: Nummer,
Vorbedingung, Aktion, erwartetes und tatsächliches Ergebnis. Das ist das Testprotokoll der
Dokumentation, und du brauchst es ohnehin.

Dabei **Screenshots** machen: Bestandsübersicht, Ausleihdialog, abgelehnte Doppelausleihe mit Meldung,
Überfälligkeitsliste, exportierte CSV in Excel, grüner Testexplorer. Diese sechs Bilder decken die
halbe Dokumentation ab, und nachträglich kostet das Nachstellen Zeit.

Zum Schluss `dotnet ef migrations has-pending-model-changes`, Build ohne Fehler, Commit.

## 4. Die zwei Doku-Tage

Was schon fertig vorliegt und nur noch zusammengesetzt werden muss:

| Doku-Abschnitt | Quelle |
|---|---|
| Ausgangssituation, Zielsetzung, Anforderungen | [Projektauftrag](Projektauftrag%20-%20KI%20erstellt.md) |
| Anwendungsfall- und Aktivitätsdiagramm | [Diagramme](Diagramme%20-%20KI%20erstellt.md) |
| ERD und Klassendiagramm | Entwurf plus Migration `InitialCreate` als Belegstelle |
| Wirtschaftlichkeitsbetrachtung | Projektauftrag, Abschnitt 8 |
| Testprotokoll | T1 bis T8 aus 3.4 plus die fünf Unit-Tests |
| Entwurfsentscheidungen | die Leitfäden: berechneter Status statt Flag, Repository gegen Service, `TransactionResult`, Löschweitergabe auf `Restrict`, Stichtag als Parameter |
| Abweichungen vom Entwurf | A2, Kategorienverwaltung und Ausleihänderung entfallen, `IsInUse` entfernt, Protokollierung ergänzt |

Das Klassendiagramm zeichnest du am Ende nach dem tatsächlichen Code, nicht vorher. Die Unterschiede
zum Entwurf sind genau der Stoff für den Abweichungsabschnitt.
