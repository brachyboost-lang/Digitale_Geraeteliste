# Projektauftrag: Digitale Werkzeugausleihe

Dieses Dokument ist die Aufgabenstellung für ein Probeprojekt im Stil der IHK-Abschlussprüfung
Teil 2 (Fachinformatiker Anwendungsentwicklung). Es beschreibt den fiktiven betrieblichen
Auftrag, den Funktionsumfang, das Datenmodell und den Zeitplan. Umsetzung als
WPF-Desktopanwendung mit SQLite, veranschlagt auf 8 Stunden reine Entwicklungszeit.

## 1. Ausgangssituation

Die Elektro Brandt GmbH ist ein Handwerksbetrieb mit 45 Mitarbeitern. Im Lager stehen rund
120 hochwertige Werkzeuge und Messgeräte, die von den Monteuren für Baustelleneinsätze
entliehen werden: Isolationsmessgeräte, Kernbohrmaschinen, Laser-Nivelliergeräte,
Wärmebildkameras.

Die Ausgabe wird bisher in einer Kladde am Lagerregal handschriftlich festgehalten. Daraus
ergeben sich drei Probleme:

1. Der Lagerist kann nicht schnell beantworten, ob ein bestimmtes Gerät gerade verfügbar ist.
   Er muss die Kladde rückwärts durchblättern, bis er den letzten Eintrag zu diesem Gerät findet.
2. Überfällige Geräte fallen niemandem auf. Zwei Messgeräte sind im letzten Jahr verschwunden,
   ohne dass nachvollziehbar war, wer sie zuletzt hatte.
3. Es gibt keine Auswertung darüber, welche Geräte stark ausgelastet sind und nachbeschafft
   werden müssten.

## 2. Zielsetzung

Es soll eine Desktopanwendung für den Lagerplatz-PC entstehen, die den Gerätebestand verwaltet,
Ausleih- und Rückgabevorgänge erfasst und überfällige Ausleihen sichtbar macht.

## 3. Funktionale Anforderungen

### Muss-Anforderungen

- **A1 Gerätestammdaten:** Geräte anlegen, bearbeiten und deaktivieren. Ein Gerät hat eine
  eindeutige Inventarnummer, eine Bezeichnung, eine Kategorie und eine Standard-Leihdauer in Tagen.
- **A2 Mitarbeiterstammdaten:** Mitarbeiter anlegen und bearbeiten (Personalnummer, Name, Abteilung).
- **A3 Ausleihe buchen:** Ein Gerät an einen Mitarbeiter ausgeben. Das geplante Rückgabedatum wird
  aus der Standard-Leihdauer vorbelegt, ist aber überschreibbar.
- **A4 Rückgabe buchen:** Eine offene Ausleihe abschließen. Das Rückgabedatum wird erfasst.
- **A5 Bestandsübersicht:** Liste aller Geräte mit berechnetem Status (verfügbar, verliehen,
  überfällig), filterbar nach Status und durchsuchbar nach Inventarnummer oder Bezeichnung.
- **A6 Überfälligkeitsliste:** Alle offenen Ausleihen, deren geplantes Rückgabedatum in der
  Vergangenheit liegt, mit Angabe des Mitarbeiters und der Überschreitung in Tagen.
- **A7 CSV-Export:** Die Überfälligkeitsliste als CSV-Datei exportieren, damit der Lagerist sie
  an die Bauleitung weitergeben kann.

### Geschäftsregeln

- **R1:** Ein Gerät, das bereits verliehen ist, kann nicht erneut ausgeliehen werden.
- **R2:** Ein deaktiviertes Gerät kann nicht ausgeliehen werden.
- **R3:** Die Inventarnummer muss eindeutig sein.
- **R4:** Das geplante Rückgabedatum darf nicht vor dem Ausgabedatum liegen.
- **R5:** Der Gerätestatus wird nicht gespeichert, sondern aus den Ausleihdatensätzen berechnet.
  Damit kann er nicht mit der Ausleihhistorie in Widerspruch geraten.

### Ausdrücklich nicht im Umfang

Diese Abgrenzung ist Teil der Aufgabe. Sie hält das Projekt in acht Stunden umsetzbar:

- Keine Benutzeranmeldung, kein Rollen- und Rechtekonzept. Der Lagerplatz-PC steht in einem
  abgeschlossenen Bereich.
- Kein Mehrbenutzerbetrieb, keine Netzwerkdatenbank. Einzelplatz mit lokaler SQLite-Datei.
- Keine Barcode-Erfassung, keine Reservierung im Voraus, keine E-Mail-Benachrichtigung.
- Keine Auslastungsstatistik. Problem 3 aus Abschnitt 1 wird bewusst auf einen Folgeauftrag
  verschoben und in der Doku als Ausblick genannt.

## 4. Datenmodell

Vier Tabellen. Die Beziehungen ergeben ein ERD, das in der Dokumentation etwas hergibt, ohne
den Implementierungsaufwand aufzublähen.

**Kategorie**

| Feld | Typ | Hinweis |
|---|---|---|
| Id | INTEGER | Primärschlüssel |
| Name | TEXT | z. B. Messtechnik, Bohren, Vermessung |

**Geraet**

| Feld | Typ | Hinweis |
|---|---|---|
| Id | INTEGER | Primärschlüssel |
| Inventarnummer | TEXT | eindeutig, R3 |
| Bezeichnung | TEXT | |
| KategorieId | INTEGER | Fremdschlüssel auf Kategorie |
| StandardLeihdauerTage | INTEGER | Vorbelegung für A3 |
| IstAktiv | INTEGER | 0 oder 1, R2 |

**Mitarbeiter**

| Feld | Typ | Hinweis |
|---|---|---|
| Id | INTEGER | Primärschlüssel |
| Personalnummer | TEXT | eindeutig |
| Vorname | TEXT | |
| Nachname | TEXT | |
| Abteilung | TEXT | |

**Ausleihe**

| Feld | Typ | Hinweis |
|---|---|---|
| Id | INTEGER | Primärschlüssel |
| GeraetId | INTEGER | Fremdschlüssel |
| MitarbeiterId | INTEGER | Fremdschlüssel |
| AusgegebenAm | TEXT | Datum |
| GeplantesRueckgabedatum | TEXT | Datum, R4 |
| ZurueckgegebenAm | TEXT | NULL solange offen |
| Bemerkung | TEXT | optional, z. B. Baustelle |

Eine Ausleihe mit `ZurueckgegebenAm IS NULL` ist offen. Genau daran hängen R1, A5 und A6.

## 5. Architektur

Drei Schichten in getrennten Projekten einer Solution:

- `Ausleihe.Core`: Entitäten, Geschäftslogik (Ausleihe- und Rückgabeprüfung, Statusberechnung,
  Überfälligkeitsermittlung), Repository-Schnittstellen. Kennt weder WPF noch Entity Framework.
- `Ausleihe.Data`: EF Core mit SQLite, DbContext, Repository-Implementierungen, Migrationen,
  Seed-Daten.
- `Ausleihe.App`: WPF mit MVVM. Views, ViewModels, Value Converter für die Statusanzeige.
- `Ausleihe.Tests`: Unit-Tests der Geschäftslogik aus `Ausleihe.Core`.

Die Trennung ist kein Selbstzweck: Sie ist der Grund, warum die Geschäftsregeln ohne laufende
Datenbank und ohne GUI testbar sind. Genau das ist in der Dokumentation zu begründen.

## 6. Zeitplan (8 Stunden)

| Phase | Inhalt | Dauer |
|---|---|---|
| 1 | Solution anlegen, Projekte, EF Core einrichten, Entitäten und Migration | 1,0 h |
| 2 | DbContext, Repositories, Seed-Daten (5 Kategorien, 15 Geräte, 8 Mitarbeiter) | 1,0 h |
| 3 | Geschäftslogik: Ausleihe, Rückgabe, Statusberechnung, Überfälligkeit | 1,5 h |
| 4 | Hauptfenster mit Bestandsübersicht, Filter und Suche | 1,5 h |
| 5 | Dialoge für Gerätestammdaten sowie Ausleihe und Rückgabe | 1,5 h |
| 6 | Überfälligkeitsansicht und CSV-Export | 0,75 h |
| 7 | Unit-Tests der Geschäftsregeln R1 bis R4 | 0,5 h |
| 8 | Puffer, Abnahme gegen die Testfälle | 0,25 h |

Wenn die Zeit knapp wird, fällt Phase 6 zuerst, danach die Mitarbeiterpflege aus A2 (dann nur
Seed-Daten). Die Geschäftslogik und die Tests fallen nie, weil daran die Bewertung hängt.

## 7. Abnahme-Testfälle

| Nr. | Vorbedingung | Aktion | Erwartetes Ergebnis |
|---|---|---|---|
| T1 | Gerät verfügbar | Ausleihe buchen | Ausleihe angelegt, Status wechselt auf verliehen |
| T2 | Gerät bereits verliehen | Ausleihe buchen | Ablehnung mit Meldung, keine zweite Ausleihe (R1) |
| T3 | Gerät deaktiviert | Ausleihe buchen | Ablehnung mit Meldung (R2) |
| T4 | Inventarnummer existiert bereits | Gerät anlegen | Ablehnung mit Meldung (R3) |
| T5 | Rückgabedatum vor Ausgabedatum | Ausleihe buchen | Ablehnung mit Meldung (R4) |
| T6 | Offene Ausleihe | Rückgabe buchen | Rückgabedatum gesetzt, Status wieder verfügbar |
| T7 | Offene Ausleihe, Rückgabedatum gestern | Übersicht öffnen | Gerät erscheint als überfällig mit 1 Tag |
| T8 | Zwei überfällige Ausleihen | CSV exportieren | Datei mit Kopfzeile und zwei Datenzeilen |

## 8. Wirtschaftlichkeitsbetrachtung

Grundlage für den entsprechenden Abschnitt der Dokumentation.

**Kosten:** 8 Stunden Entwicklung zu einem internen Stundensatz von 60 Euro ergeben 480 Euro
einmalig. Keine Lizenzkosten, da SQLite und .NET kostenfrei sind.

**Nutzen:** Der Lagerist sucht nach eigener Schätzung dreimal täglich rund fünf Minuten in der
Kladde, das sind etwa 55 Stunden im Jahr. Bei 35 Euro Stundensatz entspricht das rund 1.900 Euro
jährlich. Hinzu kommt der vermiedene Geräteverlust: zwei verschwundene Messgeräte im Vorjahr
entsprechen etwa 1.400 Euro Wiederbeschaffungswert.

**Amortisation:** Allein über die eingesparte Suchzeit rechnet sich das Projekt nach etwa
drei Monaten.

## 9. Was zur vollständigen Prüfungssimulation noch dazugehört

Die reine Programmierung ist nur ein Teil. Für ein realistisches Probeprojekt kommt danach:

- Projektdokumentation mit Ist-Analyse, Soll-Konzept, ERD, Klassendiagramm, Auszügen aus dem
  Quellcode mit Erläuterung, Testprotokoll und Fazit.
- Eine Präsentation von etwa 15 Minuten, die den Nutzen für den Auftraggeber in den Mittelpunkt
  stellt und nicht den Code.
