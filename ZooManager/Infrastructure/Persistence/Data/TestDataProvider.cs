using Microsoft.Data.Sqlite;
using ZooManager.Core.Interfaces;

namespace ZooManager.Infrastructure.Persistence.Data
{
    public class TestDataProvider : ITestDataProvider
    {
        public void InsertSpecies(SqliteConnection connection)
        {
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = @"
                    INSERT INTO Species (Name, RequiredClimate, NeedsWater, MinSpacePerAnimal) VALUES 
                    ('Löwe', 'Savanne', 0, 150.0),
                    ('Pinguin', 'Arktis', 1, 10.0),
                    ('Elefant', 'Tropisch', 1, 300.0),
                    ('Eisbär', 'Arktis', 1, 200.0),
                    ('Giraffe', 'Savanne', 0, 100.0),
                    ('Seehund', 'Arktis', 1, 25.0),
                    ('Affe', 'Tropisch', 0, 30.0),
                    ('Zebra', 'Savanne', 0, 80.0),
                    ('Känguru', 'Gemäßigt', 0, 50.0),
                    ('Flamingo', 'Tropisch', 1, 5.0);";
                cmd.ExecuteNonQuery();
            }
        }

        public void InsertEnclosures(SqliteConnection connection)
        {
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = @"
                    INSERT INTO Enclosures (Name, ClimateType, HasWaterAccess, TotalArea, MaxCapacity) VALUES 
                    ('Afrikanische Savanne', 'Savanne', 1, 2500.0, 15),
                    ('Arktischer Lebensraum', 'Arktis', 1, 800.0, 25),
                    ('Tropisches Paradies', 'Tropisch', 1, 1200.0, 20),
                    ('Australisches Outback', 'Gemäßigt', 0, 900.0, 12),
                    ('Aquatische Zone', 'Arktis', 1, 500.0, 30);";
                cmd.ExecuteNonQuery();
            }
        }

        public void InsertEmployees(SqliteConnection connection)
        {
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = @"
                    INSERT INTO Employees (FirstName, LastName) VALUES 
                    ('Sarah', 'Johnson'),
                    ('Michael', 'Schmidt'),
                    ('Emma', 'Müller'),
                    ('James', 'Weber'),
                    ('Lisa', 'Wagner'),
                    ('Robert', 'Fischer'),
                    ('Amy', 'Becker');";
                cmd.ExecuteNonQuery();
            }
        }

        public void InsertAnimals(SqliteConnection connection)
        {
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = @"
                    INSERT INTO Animals (Name, SpeciesId, EnclosureId, NextFeedingTime) VALUES 
                    ('Simba', 1, 1, datetime('now', '+2 hours')),
                    ('Nala', 1, 1, datetime('now', '+2 hours')),
                    ('Mufasa', 1, 1, datetime('now', '+2 hours')),
                    ('Happy', 2, 2, datetime('now', '+1 hour')),
                    ('Pingu', 2, 2, datetime('now', '+1 hour')),
                    ('Eisberg', 2, 2, datetime('now', '+1 hour')),
                    ('Dumbo', 3, 3, datetime('now', '+3 hours')),
                    ('Ellie', 3, 3, datetime('now', '+3 hours')),
                    ('Nanook', 4, 2, datetime('now', '+4 hours')),
                    ('Schneeball', 4, 2, datetime('now', '+4 hours')),
                    ('Langer Hals', 5, 1, datetime('now', '+2 hours')),
                    ('Tupfen', 5, 1, datetime('now', '+2 hours')),
                    ('Flipper', 6, 5, datetime('now', '+1 hour')),
                    ('Platscher', 6, 5, datetime('now', '+1 hour')),
                    ('Georg', 7, 3, datetime('now', '+1 hour')),
                    ('Neugierig', 7, 3, datetime('now', '+1 hour')),
                    ('Streifen', 8, 1, datetime('now', '+2 hours')),
                    ('Flecken', 8, 1, datetime('now', '+2 hours')),
                    ('Hüpfer', 9, 4, datetime('now', '+2 hours')),
                    ('Rosa', 10, 3, datetime('now', '+1 hour'));";
                cmd.ExecuteNonQuery();
            }
        }

        public void InsertEmployeeQualifications(SqliteConnection connection)
        {
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = @"
                    INSERT INTO EmployeeQualifications (EmployeeId, SpeciesId) VALUES 
                    (1, 1), (1, 5), (1, 8),
                    (2, 2), (2, 4), (2, 6),
                    (3, 3), (3, 7), (3, 10),
                    (4, 1), (4, 2), (4, 9),
                    (5, 3), (5, 4), (5, 5),
                    (6, 6), (6, 7), (6, 8),
                    (7, 9), (7, 10), (7, 1);";
                cmd.ExecuteNonQuery();
            }
        }

        public void InsertAnimalEvents(SqliteConnection connection)
        {
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = @"
                    INSERT INTO AnimalEvents (AnimalId, EventDate, EventType, Description) VALUES 
                    (1, datetime('now', '-7 days'), 'Gesundheitscheck', 'Jährliche tierärztliche Untersuchung erfolgreich abgeschlossen'),
                    (1, datetime('now', '-3 days'), 'Fütterung', 'Premium Fleischdiät gefüttert - 8kg verzehrt'),
                    (2, datetime('now', '-5 days'), 'Pflege', 'Fellbürsten und Krallenschneiden abgeschlossen'),
                    (3, datetime('now', '-1 day'), 'Bewegung', 'Verlängerte Außenzeit im Hauptgehege'),
                    (4, datetime('now', '-2 days'), 'Gesundheitscheck', 'Schwimmfußuntersuchung - alles normal'),
                    (5, datetime('now', '-4 days'), 'Fütterung', 'Fischdiät - 2kg Hering und Sardinen'),
                    (6, datetime('now', '-1 day'), 'Verhalten', 'Soziale Interaktion mit Kolonie beobachtet'),
                    (7, datetime('now', '-6 days'), 'Gesundheitscheck', 'Rüssel- und Stoßzahnuntersuchung abgeschlossen'),
                    (8, datetime('now', '-2 days'), 'Baden', 'Schlammbad und Wassersprühsitzung durchgeführt'),
                    (9, datetime('now', '-3 days'), 'Gesundheitscheck', 'Gewichtskontrolle - 520kg registriert'),
                    (10, datetime('now', '-1 day'), 'Bewegung', 'Schwimmsitzung im arktischen Becken'),
                    (11, datetime('now', '-4 days'), 'Fütterung', 'Akazienblätter und Heu - 15kg verzehrt'),
                    (12, datetime('now', '-2 days'), 'Gesundheitscheck', 'Halsbeweglichkeit und Gelenkprüfung durchgeführt'),
                    (13, datetime('now', '-5 days'), 'Fütterung', 'Frische Fischauswahl - Makrele und Lachs'),
                    (14, datetime('now', '-1 day'), 'Training', 'Reaktion auf Pflegerbefehle trainiert'),
                    (15, datetime('now', '-3 days'), 'Bereicherung', 'Neue Kletterstrukturen eingeführt'),
                    (16, datetime('now', '-2 days'), 'Soziales', 'Gruppeninteraktion mit Truppenmitgliedern beobachtet'),
                    (17, datetime('now', '-4 days'), 'Gesundheitscheck', 'Streifenmuster-Dokumentation aktualisiert'),
                    (18, datetime('now', '-1 day'), 'Fütterung', 'Gras- und Getreidemischung - 12kg verzehrt'),
                    (19, datetime('now', '-6 days'), 'Bewegung', 'Hüpf- und Sprungaktivitäten durchgeführt'),
                    (20, datetime('now', '-2 days'), 'Fütterung', 'Algen- und Kleinfischdiät');";
                cmd.ExecuteNonQuery();
            }
        }

        public void InsertZooEvents(SqliteConnection connection)
        {
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = @"
                    INSERT INTO ZooEvents (Title, Description, Start) VALUES 
                    ('Löwenfütterung', 'Beobachten Sie unsere majestätischen Löwen bei ihrer Nachmittagsfütterung', datetime('now', '+1 day', '14:00')),
                    ('Pinguinvortrag', 'Lehrreiche Präsentation über arktisches Pinguinverhalten und Naturschutz', datetime('now', '+2 days', '11:00')),
                    ('Elefantenbadezeit', 'Begleiten Sie uns, wenn unsere Elefanten ihr tägliches Bad und Schlammspa genießen', datetime('now', '+3 days', '15:30')),
                    ('Tierpfleger für einen Tag', 'Hinter-den-Kulissen-Erfahrung mit professionellen Tierpflegern', datetime('now', '+5 days', '09:00')),
                    ('Nacht-Safari-Abenteuer', 'Entdecken Sie, wie sich nachtaktive Tiere nach Einbruch der Dunkelheit verhalten', datetime('now', '+7 days', '19:00')),
                    ('Naturschutz-Workshop', 'Lernen Sie über globale Wildtierschutzbemühungen und wie Sie helfen können', datetime('now', '+10 days', '13:00')),
                    ('Tiertraining-Demo', 'Sehen Sie, wie positives Verstärkungstraining bei der Tierpflege hilft', datetime('now', '+14 days', '16:00')),
                    ('Fotografie-Workshop', 'Professionelle Wildtierfotografie-Tipps und -Techniken', datetime('now', '+21 days', '10:00'));";
                cmd.ExecuteNonQuery();
            }
        }

        public void InsertUsers(SqliteConnection connection, Func<string, string> hashGenerator)
        {
            string hash = hashGenerator("password");

            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = @"
                    INSERT INTO Users (Username, PasswordHash, Role, EmployeeId, CreatedAt, IsActive) VALUES 
                    (@manager, @hash, 1, NULL, datetime('now'), 1),
                    (@sarah, @hash, 2, 1, datetime('now'), 1),
                    (@michael, @hash, 2, 2, datetime('now'), 1),
                    (@emma, @hash, 2, 3, datetime('now'), 1),
                    (@james, @hash, 2, 4, datetime('now'), 1),
                    (@lisa, @hash, 2, 5, datetime('now'), 1),
                    (@robert, @hash, 2, 6, datetime('now'), 1),
                    (@amy, @hash, 2, 7, datetime('now'), 1);";

                cmd.Parameters.AddWithValue("@manager", "manager");
                cmd.Parameters.AddWithValue("@sarah", "sarah.johnson");
                cmd.Parameters.AddWithValue("@michael", "michael.schmidt");
                cmd.Parameters.AddWithValue("@emma", "emma.mueller");
                cmd.Parameters.AddWithValue("@james", "james.weber");
                cmd.Parameters.AddWithValue("@lisa", "lisa.wagner");
                cmd.Parameters.AddWithValue("@robert", "robert.fischer");
                cmd.Parameters.AddWithValue("@amy", "amy.becker");
                cmd.Parameters.AddWithValue("@hash", hash);

                cmd.ExecuteNonQuery();
            }
        }
    }
}