# Diagramm-Vorlagen: Digitale Werkzeugausleihe

Textuelle Vorlagen für die drei UML-Diagramme der Projektdokumentation. Jeder Abschnitt ist so
formuliert, dass er sich direkt in ein Zeichenwerkzeug übertragen lässt. Grundlage ist
[Projektauftrag.md](Projektauftrag.md); die Anforderungsnummern A1 bis A7 und die Geschäftsregeln
R1 bis R5 stammen von dort.

## 1. Anwendungsfalldiagramm

### Akteure

- **Lagerist** (Primärakteur): bedient die Anwendung am Lagerplatz-PC. Einziger Benutzer des Systems.
- **Bauleitung** (Sekundärakteur): empfängt die exportierte Überfälligkeitsliste, arbeitet aber
  nicht selbst mit der Anwendung.

Systemgrenze beschriften mit: *Werkzeugausleihe*.

### Anwendungsfälle

| Nr. | Name | Anforderung |
|---|---|---|
| UC1 | Gerätestammdaten verwalten | A1 |
| UC2 | Mitarbeiterstammdaten verwalten | A2 |
| UC3 | Gerät ausleihen | A3 |
| UC4 | Gerät zurücknehmen | A4 |
| UC5 | Gerätebestand einsehen | A5 |
| UC6 | Überfällige Ausleihen anzeigen | A6 |
| UC7 | Überfälligkeitsliste exportieren | A7 |
| UC8 | Gerätestatus ermitteln | R5 |

### Beziehungen

- Der Lagerist ist mit UC1 bis UC7 verbunden.
- Die Bauleitung ist mit UC7 verbunden.
- UC5 «include» UC8: Die Bestandsübersicht kann ohne die Statusermittlung nicht angezeigt werden.
- UC6 «include» UC8: Gleiches gilt für die Überfälligkeitsliste.
- UC3 «include» UC8: Vor der Ausgabe muss geprüft werden, ob das Gerät verfügbar ist.
- UC7 «extend» UC6: Der Export ist eine optionale Erweiterung, die aus der Überfälligkeitsliste
  heraus angestoßen wird.

UC8 ist ein eingeschlossener Anwendungsfall ohne direkte Akteursverbindung. Er begründet im
Diagramm sichtbar, warum der Status nach R5 nur an einer Stelle berechnet wird.

### Ausformulierte Beschreibung von UC3 (Gerät ausleihen)

Diese Schablone eignet sich für die Dokumentation. Die übrigen Anwendungsfälle lassen sich nach
demselben Muster beschreiben.

- **Kurzbeschreibung:** Der Lagerist gibt ein Gerät an einen Mitarbeiter aus und erfasst den
  Vorgang mit geplantem Rückgabedatum.
- **Akteur:** Lagerist
- **Vorbedingung:** Das Gerät ist im Bestand erfasst, aktiv und aktuell nicht verliehen. Mindestens
  ein Mitarbeiter ist angelegt.
- **Nachbedingung (Erfolg):** Ein neuer Ausleihdatensatz ist gespeichert. Das Gerät wird in der
  Bestandsübersicht als verliehen angezeigt.
- **Nachbedingung (Fehlschlag):** Es wurde kein Datensatz gespeichert. Der Datenbestand ist
  unverändert.
- **Standardablauf:**
  1. Der Lagerist wählt in der Bestandsübersicht ein Gerät aus und startet die Ausleihe.
  2. Das System prüft, ob das Gerät aktiv und verfügbar ist.
  3. Das System zeigt den Ausleihdialog und belegt das geplante Rückgabedatum mit dem heutigen
     Datum zuzüglich der Standard-Leihdauer des Geräts vor.
  4. Der Lagerist wählt den Mitarbeiter aus, passt bei Bedarf das Rückgabedatum an und erfasst
     optional eine Bemerkung zur Baustelle.
  5. Der Lagerist bestätigt die Ausgabe.
  6. Das System prüft die Eingaben und speichert die Ausleihe.
  7. Das System aktualisiert die Bestandsübersicht.
- **Alternativablauf A1 (Gerät bereits verliehen, R1):** Das System bricht in Schritt 2 ab und
  meldet, dass das Gerät derzeit an einen anderen Mitarbeiter ausgegeben ist.
- **Alternativablauf A2 (Gerät deaktiviert, R2):** Das System bricht in Schritt 2 ab und meldet,
  dass das Gerät nicht ausleihbar ist.
- **Alternativablauf A3 (ungültiges Rückgabedatum, R4):** Das System weist die Eingabe in Schritt 6
  zurück und kehrt zu Schritt 4 zurück.

## 2. Klassendiagramm

Das Diagramm zeigt die Fachklassen aus `Ausleihe.Core`. Die Repository-Schnittstellen gehören
ebenfalls in `Ausleihe.Core`, ihre Implementierungen liegen in `Ausleihe.Data` und werden im
Klassendiagramm nur angedeutet.

### Entitätsklassen

**Kategorie**
- Attribute: `Id: int`, `Name: string`
- Methoden: keine

**Geraet**
- Attribute: `Id: int`, `Inventarnummer: string`, `Bezeichnung: string`, `KategorieId: int`,
  `StandardLeihdauerTage: int`, `IstAktiv: bool`
- Methoden: keine. Das Gerät kennt seinen Status bewusst nicht, weil dieser nach R5 aus den
  Ausleihdatensätzen abgeleitet wird.

**Mitarbeiter**
- Attribute: `Id: int`, `Personalnummer: string`, `Vorname: string`, `Nachname: string`,
  `Abteilung: string`
- Methoden: `VollerName(): string`

**Ausleihe**
- Attribute: `Id: int`, `GeraetId: int`, `MitarbeiterId: int`, `AusgegebenAm: DateTime`,
  `GeplantesRueckgabedatum: DateTime`, `ZurueckgegebenAm: DateTime?`, `Bemerkung: string`
- Methoden:
  - `IstOffen(): bool`
  - `IstUeberfaellig(stichtag: DateTime): bool`
  - `TageUeberschritten(stichtag: DateTime): int`

Der Stichtag wird als Parameter übergeben und nicht über `DateTime.Today` im Inneren gelesen.
Nur so lässt sich die Überfälligkeit im Unit-Test ohne Systemzeitmanipulation prüfen. Dieser Punkt
ist in der Dokumentation eine Erwähnung wert.

### Aufzählung

**Geraetestatus** (Enumeration)
- Werte: `Verfuegbar`, `Verliehen`, `Ueberfaellig`, `Inaktiv`

### Logik- und Zugriffsklassen

**AusleihService**
- Attribute: `_geraete: IGeraeteRepository`, `_ausleihen: IAusleihRepository` (beide private)
- Methoden:
  - `Ausleihen(geraetId: int, mitarbeiterId: int, ausgegebenAm: DateTime, geplantesRueckgabedatum: DateTime, bemerkung: string): BuchungsErgebnis`
  - `Zuruecknehmen(ausleihId: int, zurueckgegebenAm: DateTime): BuchungsErgebnis`
  - `StatusErmitteln(geraet: Geraet, stichtag: DateTime): Geraetestatus`
  - `UeberfaelligeErmitteln(stichtag: DateTime): List<Ausleihe>`

**BuchungsErgebnis**
- Attribute: `IstErfolgreich: bool`, `Fehlermeldung: string`
- Methoden: `Erfolg(): BuchungsErgebnis`, `Fehler(meldung: string): BuchungsErgebnis` (statisch)

Die Validierungsergebnisse werden als Rückgabewert transportiert statt über Exceptions. Fehlerhafte
Benutzereingaben sind erwartbare Abläufe und keine Ausnahmezustände.

**Schnittstellen** (im Diagramm mit «interface» kennzeichnen)
- `IGeraeteRepository`: `Alle(): List<Geraet>`, `NachId(id: int): Geraet`, `Speichern(g: Geraet)`,
  `InventarnummerExistiert(nummer: string, ausserId: int): bool`
- `IAusleihRepository`: `OffeneAusleihe(geraetId: int): Ausleihe`, `AlleOffenen(): List<Ausleihe>`,
  `Speichern(a: Ausleihe)`
- `IMitarbeiterRepository`: `Alle(): List<Mitarbeiter>`, `NachId(id: int): Mitarbeiter`

### Beziehungen mit Multiplizitäten

| Von | Beziehung | Nach | Multiplizität | Bedeutung |
|---|---|---|---|---|
| Kategorie | Assoziation | Geraet | 1 zu 0..* | Eine Kategorie umfasst beliebig viele Geräte |
| Geraet | Assoziation | Ausleihe | 1 zu 0..* | Ein Gerät hat eine Ausleihhistorie |
| Mitarbeiter | Assoziation | Ausleihe | 1 zu 0..* | Ein Mitarbeiter kann mehrfach entleihen |
| AusleihService | Abhängigkeit | IGeraeteRepository | 1 zu 1 | Konstruktorinjektion |
| AusleihService | Abhängigkeit | IAusleihRepository | 1 zu 1 | Konstruktorinjektion |
| AusleihService | Abhängigkeit | BuchungsErgebnis | | Rückgabetyp |
| GeraeteRepository | Realisierung | IGeraeteRepository | | gestrichelter Pfeil, in `Ausleihe.Data` |

Von Gerät zu Ausleihe darf zu jedem Zeitpunkt höchstens eine Ausleihe offen sein. Diese
Einschränkung lässt sich als Constraint am Assoziationsende notieren:
`{höchstens eine mit ZurueckgegebenAm = null}`. Das ist die grafische Entsprechung von R1.

## 3. Aktivitätsdiagramm: Gerät ausleihen

Das Diagramm bildet UC3 ab. Es eignet sich am besten von allen Anwendungsfällen, weil es drei
Entscheidungsknoten enthält.

### Verantwortungsbereiche (Swimlanes)

Drei senkrechte Bahnen, von links nach rechts: **Lagerist**, **Benutzeroberfläche**,
**Geschäftslogik**.

### Ablauf

1. **Startknoten** in der Bahn Lagerist.
2. Aktion *Gerät in der Bestandsübersicht auswählen* (Lagerist).
3. Aktion *Ausleihe starten* (Lagerist).
4. Aktion *Gerätedaten laden* (Benutzeroberfläche).
5. Aktion *Verfügbarkeit prüfen* (Geschäftslogik).
6. **Entscheidungsknoten** *Gerät aktiv?* (Geschäftslogik)
   - `[nein]` zu Aktion *Meldung: Gerät ist deaktiviert* (Benutzeroberfläche), von dort zum
     Verbindungsknoten vor dem Endknoten.
   - `[ja]` weiter zu Schritt 7.
7. **Entscheidungsknoten** *Gerät verfügbar?* (Geschäftslogik)
   - `[nein]` zu Aktion *Meldung: Gerät ist bereits verliehen* (Benutzeroberfläche), von dort zum
     Verbindungsknoten vor dem Endknoten.
   - `[ja]` weiter zu Schritt 8.
8. Aktion *Ausleihdialog öffnen und Rückgabedatum vorbelegen* (Benutzeroberfläche). Als Notiz
   ergänzen: `Ausgabedatum + StandardLeihdauerTage`.
9. Aktion *Mitarbeiter wählen, Rückgabedatum anpassen, Bemerkung erfassen* (Lagerist).
10. Aktion *Ausgabe bestätigen* (Lagerist).
11. Aktion *Eingaben validieren* (Geschäftslogik).
12. **Entscheidungsknoten** *Rückgabedatum nicht vor Ausgabedatum?* (Geschäftslogik)
    - `[nein]` zu Aktion *Meldung: Rückgabedatum ungültig* (Benutzeroberfläche) und von dort
      zurück zu Schritt 9. Das ist die einzige Rückwärtskante im Diagramm.
    - `[ja]` weiter zu Schritt 13.
13. Aktion *Ausleihe speichern* (Geschäftslogik). Als Objektknoten oder Notiz ergänzen:
    `Ausleihe [gespeichert]`.
14. Aktion *Bestandsübersicht aktualisieren* (Benutzeroberfläche).
15. **Verbindungsknoten**, in den auch die drei Fehlermeldungen münden.
16. **Endknoten**.

### Hinweise zum Zeichnen

- Entscheidungsknoten als Raute, jede ausgehende Kante mit einer Bedingung in eckigen Klammern
  beschriften. Die Bedingungen müssen sich gegenseitig ausschließen und den Wertebereich vollständig
  abdecken.
- Die Abbruchpfade nicht mit eigenen Endknoten versehen, sondern über einen gemeinsamen
  Verbindungsknoten zusammenführen. Ein Aktivitätsdiagramm mit vier Endknoten wirkt unaufgeräumt.
- Die Verzweigungen entsprechen genau R1, R2 und R4 aus dem Projektauftrag und den Testfällen T2,
  T3 und T5. Diese Zuordnung im Text der Dokumentation ausdrücklich herstellen, denn sie zeigt,
  dass Entwurf, Implementierung und Test zusammenpassen.
