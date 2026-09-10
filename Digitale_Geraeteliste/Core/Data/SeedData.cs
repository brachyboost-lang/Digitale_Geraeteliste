using System.Collections.Generic;
using Digitale_Geraeteliste.Core.Model;

namespace Digitale_Geraeteliste.Core.Data
{
    internal static class SeedData
    {
        public static List<Item> CreateItems()
        {
            return new List<Item>
            {
                new Instrument
                {
                    Id = 1,
                    Name = "Isolationsmessgeraet Fluke 1507",
                    Description = "Isolationswiderstandsmessung bis 1000 V, Koffer mit Pruefspitzen",
                    Type = "Messtechnik",
                    standardLendDuration = 3
                },
                new Instrument
                {
                    Id = 2,
                    Name = "Installationstester Profitest MTECH",
                    Description = "Pruefung ortsfester Anlagen nach DIN VDE 0100-600",
                    Type = "Messtechnik",
                    standardLendDuration = 5
                },
                new Instrument
                {
                    Id = 3,
                    Name = "Zangenamperemeter Fluke 376 FC",
                    Description = "Echteffektivwert bis 1000 A, mit flexibler Stromzange",
                    Type = "Messtechnik",
                    standardLendDuration = 7
                },
                new Instrument
                {
                    Id = 4,
                    Name = "Waermebildkamera Testo 872",
                    Description = "320 x 240 Pixel, fuer Thermografie an Schaltanlagen",
                    Type = "Messtechnik",
                    standardLendDuration = 2
                },
                new Instrument
                {
                    Id = 5,
                    Name = "Netzanalysator Fluke 1748",
                    Description = "Dreiphasen-Netzqualitaetsanalyse fuer Langzeitmessungen",
                    Type = "Messtechnik",
                    standardLendDuration = 14
                },
                new Instrument
                {
                    Id = 6,
                    Name = "Digitalmultimeter Fluke 87V",
                    Description = "Handmultimeter, Standardausruestung fuer Stoerungssuche",
                    Type = "Messtechnik",
                    standardLendDuration = 7
                },
                new Instrument
                {
                    Id = 7,
                    Name = "Leitungssucher Amprobe AT-6030",
                    Description = "Ortung stromfuehrender und spannungsloser Leitungen",
                    Type = "Ortung",
                    standardLendDuration = 3
                },
                new Instrument
                {
                    Id = 8,
                    Name = "Laserentfernungsmesser Leica Disto D2",
                    Description = "Messbereich bis 100 m, fuer Aufmass beim Kunden",
                    Type = "Vermessung",
                    standardLendDuration = 5
                },
                new Tool
                {
                    Id = 9,
                    Name = "Kernbohrgeraet Hilti DD 150-U",
                    Description = "Diamantkernbohren bis 162 mm, inklusive Wasserfangring",
                    Type = "Bohren",
                    standardLendDuration = 2
                },
                new Tool
                {
                    Id = 10,
                    Name = "Bohrhammer Hilti TE 60-ATC",
                    Description = "SDS-Max Kombihammer fuer Durchbrueche",
                    Type = "Bohren",
                    standardLendDuration = 5
                },
                new Tool
                {
                    Id = 11,
                    Name = "Akku-Bohrschrauber Makita DDF484",
                    Description = "18 V, zwei Akkus und Ladegeraet im Koffer",
                    Type = "Bohren",
                    standardLendDuration = 10
                },
                new Tool
                {
                    Id = 12,
                    Name = "Winkelschleifer Bosch GWS 18V-10",
                    Description = "125 mm Akku-Winkelschleifer mit Absaughaube",
                    Type = "Trennen",
                    standardLendDuration = 5
                },
                new Tool
                {
                    Id = 13,
                    Name = "Kabelzugset Katimex Kabelmax",
                    Description = "Einziehband 60 m mit Zubehoersatz",
                    Type = "Kabelverlegung",
                    standardLendDuration = 3
                },
                new Tool
                {
                    Id = 14,
                    Name = "Rotationslaser Bosch GRL 300 HV",
                    Description = "Horizontal und vertikal, mit Stativ und Messlatte",
                    Type = "Vermessung",
                    standardLendDuration = 3
                },
                new Tool
                {
                    Id = 15,
                    Name = "Stemmhammer Bosch GSH 5 CE",
                    Description = "Abbruchhammer 5 kg fuer Schlitzarbeiten",
                    Type = "Bohren",
                    standardLendDuration = 2
                }
            };
        }

        public static List<Employee> CreateEmployees()
        {
            return new List<Employee>
            {
                new Employee
                {
                    Id = 1,
                    FirstName = "Thomas",
                    LastName = "Krueger",
                    Name = "Krueger, Thomas",
                    Department = "Elektroinstallation"
                },
                new Employee
                {
                    Id = 2,
                    FirstName = "Sandra",
                    LastName = "Weber",
                    Name = "Weber, Sandra",
                    Department = "Schaltanlagenbau"
                },
                new Employee
                {
                    Id = 3,
                    FirstName = "Emre",
                    LastName = "Yilmaz",
                    Name = "Yilmaz, Emre",
                    Department = "Elektroinstallation"
                },
                new Employee
                {
                    Id = 4,
                    FirstName = "Lukas",
                    LastName = "Hoffmann",
                    Name = "Hoffmann, Lukas",
                    Department = "Kundendienst"
                },
                new Employee
                {
                    Id = 5,
                    FirstName = "Nadine",
                    LastName = "Bauer",
                    Name = "Bauer, Nadine",
                    Department = "Bauleitung"
                },
                new Employee
                {
                    Id = 6,
                    FirstName = "Michael",
                    LastName = "Schneider",
                    Name = "Schneider, Michael",
                    Department = "Elektroinstallation"
                },
                new Employee
                {
                    Id = 7,
                    FirstName = "Irina",
                    LastName = "Petrova",
                    Name = "Petrova, Irina",
                    Department = "Kundendienst"
                },
                new Employee
                {
                    Id = 8,
                    FirstName = "Jonas",
                    LastName = "Wagner",
                    Name = "Wagner, Jonas",
                    Department = "Lager"
                }
            };
        }
    }
}
