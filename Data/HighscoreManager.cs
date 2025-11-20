using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Snake.Data
{
    public class HighscoreEntry
    {
        public int Score { get; set; }
        public string PlayerName { get; set; }
        public DateTime Date { get; set; }
        public int Speed { get; set; } // ms
        public int Length { get; set; } // Schlangenlänge

        public HighscoreEntry()
        {
            PlayerName = "Player";
            Date = DateTime.Now;
        }

        public override string ToString()
        {
            return $"{Score} | {PlayerName} | {Date:dd.MM.yyyy} | {Speed}ms | Länge: {Length}";
        }
    }

    public class HighscoreManager
    {
        private const string HIGHSCORE_FILE = "highscores.dat";
        private const int MAX_HIGHSCORES = 5;
        private List<HighscoreEntry> _highscores;

        public HighscoreManager()
        {
            _highscores = new List<HighscoreEntry>();
            LoadHighscores();
        }

        /// <summary>
        /// Lädt Highscores aus der Datei
        /// </summary>
        private void LoadHighscores()
        {
            try
            {
                if (File.Exists(HIGHSCORE_FILE))
                {
                    var lines = File.ReadAllLines(HIGHSCORE_FILE);
                    _highscores.Clear();

                    foreach (var line in lines)
                    {
                        var parts = line.Split('|');
                        if (parts.Length >= 5)
                        {
                            var entry = new HighscoreEntry
                            {
                                Score = int.Parse(parts[0].Trim()),
                                PlayerName = parts[1].Trim(),
                                Date = DateTime.Parse(parts[2].Trim()),
                                Speed = int.Parse(parts[3].Trim()),
                                Length = int.Parse(parts[4].Trim())
                            };
                            _highscores.Add(entry);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Bei Fehler einfach leere Liste
                _highscores.Clear();
                System.Diagnostics.Debug.WriteLine($"Error loading highscores: {ex.Message}");
            }
        }

        /// <summary>
        /// Speichert Highscores in die Datei
        /// </summary>
        private void SaveHighscores()
        {
            try
            {
                var lines = _highscores.Select(h => 
                    $"{h.Score}|{h.PlayerName}|{h.Date:yyyy-MM-dd HH:mm:ss}|{h.Speed}|{h.Length}");
                File.WriteAllLines(HIGHSCORE_FILE, lines);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving highscores: {ex.Message}");
            }
        }

        /// <summary>
        /// Fügt einen neuen Score hinzu und prüft ob er in die Top 5 kommt
        /// </summary>
        /// <returns>Position in der Highscore-Liste (1-5) oder 0 wenn nicht in Top 5</returns>
        public int AddScore(int score, string playerName, int speed, int length)
        {
            var entry = new HighscoreEntry
            {
                Score = score,
                PlayerName = string.IsNullOrWhiteSpace(playerName) ? "Player" : playerName,
                Date = DateTime.Now,
                Speed = speed,
                Length = length
            };

            _highscores.Add(entry);
            _highscores = _highscores.OrderByDescending(h => h.Score).ToList();

            // Nur Top 5 behalten
            if (_highscores.Count > MAX_HIGHSCORES)
            {
                _highscores = _highscores.Take(MAX_HIGHSCORES).ToList();
            }

            SaveHighscores();

            // Position zurückgeben (1-basiert)
            int position = _highscores.IndexOf(entry) + 1;
            return position <= MAX_HIGHSCORES ? position : 0;
        }

        /// <summary>
        /// Prüft ob ein Score in die Top 5 kommt
        /// </summary>
        public bool IsHighscore(int score)
        {
            if (_highscores.Count < MAX_HIGHSCORES)
                return true;

            return score > _highscores.Last().Score;
        }

        /// <summary>
        /// Gibt die Top 5 Highscores zurück
        /// </summary>
        public List<HighscoreEntry> GetTop5()
        {
            return _highscores.Take(MAX_HIGHSCORES).ToList();
        }

        /// <summary>
        /// Löscht alle Highscores
        /// </summary>
        public void ClearHighscores()
        {
            _highscores.Clear();
            SaveHighscores();
        }
    }
}
