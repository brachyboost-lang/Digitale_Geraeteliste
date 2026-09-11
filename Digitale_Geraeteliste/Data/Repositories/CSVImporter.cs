using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace Digitale_Geraeteliste.Data.Repositories
{
    public class CSVImporter
    {

        public static List<string[]> ImportCSV(string filePath)
        {
            var data = new List<string[]>();
            using (var reader = new StreamReader(filePath))
            {
                reader.ReadLine(); // skips the header line - ghetto fix for my testdata csv, could be improved by checking if the first line is a header
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    if (line != null)
                    {
                        var values = line.Split(';');
                        data.Add(values);
                    }
                }
            }
            return data;
        }
    }
}
