## Vereinbartes Rückgabedatum statt Leihdauer

Ursprünglich übergaben die Methoden zum Anlegen und Ändern einer Ausleihe die Leihdauer als Anzahl von
Tagen. Im Laufe der Umsetzung wurde diese Schnittstelle auf ein vereinbartes Rückgabedatum des Typs
`DateTime?` umgestellt. Ausschlaggebend war die Sicht des Anwenders: Der Lagerist vereinbart mit dem
Monteur einen Rückgabetag und wählt diesen im Ausleihdialog über ein Kalenderfeld aus. Eine Leihdauer in
Tagen hätte an der Oberfläche in ein Datum und in der Fachlogik wieder zurück in eine Anzahl von Tagen
umgerechnet werden müssen, was eine zusätzliche Fehlerquelle ohne fachlichen Nutzen gewesen wäre.

Der Datentyp ist bewusst als `DateTime?` gewählt. Liegt kein Datum vor, berechnet die Anwendung die
Frist weiterhin aus der am Gerät hinterlegten Standard-Leihdauer. Damit bleibt die Vorbelegung des
Rückgabedatums erhalten, ohne dass die Oberfläche die Berechnung selbst durchführen muss. Zusätzlich
lässt sich Regel R4 dadurch als unmittelbarer Vergleich zweier Kalendertage prüfen: Das vereinbarte
Rückgabedatum darf nicht vor dem Ausgabedatum liegen. Bei einer Leihdauer wäre an dieser Stelle nur eine
Prüfung auf ein negatives Vorzeichen möglich gewesen, die den fachlichen Sachverhalt weniger genau
abbildet.
