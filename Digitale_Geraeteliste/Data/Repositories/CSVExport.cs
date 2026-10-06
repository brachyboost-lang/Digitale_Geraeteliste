using Digitale_Geraeteliste.Core.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Digitale_Geraeteliste.Data.Repositories
{
    public static class CSVExport
    {
        public static int WriteOverdueList(IEnumerable<InventoryOverviewRow> rows, string filePath, DateTime dateToCheck)
        {
            List<string> lines = new List<string>
            {
                "Inventarnummer;Bezeichnung;Kategorie;Entleiher;Ausgegeben von;Ausgabedatum;Rueckgabe bis;Tage ueberfaellig;Vertragsnummer"
            };

            foreach (InventoryOverviewRow row in rows)
            {
                int daysOverdue = (dateToCheck.Date - row.ExpectedReturnDate!.Value.Date).Days;
                lines.Add(string.Join(";",
                    Clean(row.InventoryNumber),
                    Clean(row.Name),
                    Clean(row.CategoryName),
                    Clean(row.BorrowedByEmployeeName),
                    Clean(row.LendByEmployeeName),
                    row.LendDate?.ToString("dd.MM.yyyy") ?? string.Empty,
                    row.ExpectedReturnDate.Value.ToString("dd.MM.yyyy"),
                    daysOverdue.ToString(),
                    Clean(row.ContractNumber)));
            }
            File.WriteAllLines(filePath, lines, Encoding.UTF8);
            return lines.Count - 1;
        }

        private static string Clean(string? value) => (value ?? string.Empty).Replace(";", ",");
    }
}
