using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.Json;

namespace Snake.Models
{
    public class GameState
    {
        public enum Direction { Up, Down, Left, Right }
        public enum FoodType { Normal, Bonus, Speed, Slow }
        public enum Difficulty { Easy, Medium, Hard }
        public enum GameMode { Classic, TimeAttack, Survival }
        public enum EndGameOption { NewGame, MainMenu, HighScores, Exit }

        // Events für UI-Updates
        public event Action<int> OnScoreChanged;
        public event Action<bool> OnGameOverChanged;
        public event Action<Point> OnFoodPlaced;
        public event Action OnSnakeMoved;
        public event Action OnGameReset;
        public event Action<int> OnHighScoreChanged;
        public event Action<string> OnGameMessage;
        public event Action<List<HighScoreEntry>> OnHighScoresUpdated;
        public event Action<EndGameOptions> OnShowEndGameOptions;

        // Grundlegende Properties
        public List<Point> Snake { get; private set; }
        public Point FoodPosition { get; private set; }
        public Direction CurrentDirection { get; private set; }
        public int BoardWidth { get; private set; }
        public int BoardHeight { get; private set; }
        public bool GameOver { get; private set; }
        public int Score { get; private set; }
        public int CurrentSpeed { get; set; }
        public bool JustAte { get; private set; }
        public int FoodDigestionProgress { get; private set; }

        // Neue erweiterte Properties
        public int HighScore { get; private set; }
        public int FoodsEaten { get; private set; }
        public TimeSpan GameTime { get; private set; }
        public FoodType CurrentFoodType { get; private set; }
        public int FoodLifetime { get; private set; }
        public bool IsInvincible { get; private set; }
        public Difficulty CurrentDifficulty { get; set; }
        public GameMode CurrentGameMode { get; set; }
        public bool IsPaused { get; private set; }

        // Highscore System
        public List<HighScoreEntry> HighScores { get; private set; }
        private const string HIGHSCORE_FILE = "highscores.json";

        private Random _random;
        private Direction _nextDirection;
        private DateTime _gameStartTime;
        private HashSet<Point> _snakePositions;
        private int _foodTimer;
        private int _invincibilityTimer;
        private int _consecutiveFoods;
        private TimeSpan _timeLimit;

        // Highscore Entry Klasse
        public class HighScoreEntry
        {
            public string PlayerName { get; set; }
            public int Score { get; set; }
            public Difficulty Difficulty { get; set; }
            public GameMode GameMode { get; set; }
            public DateTime Date { get; set; }
            public TimeSpan GameDuration { get; set; }
            public int SnakeLength { get; set; }

            public HighScoreEntry(string playerName, int score, Difficulty difficulty, GameMode gameMode, TimeSpan duration, int snakeLength)
            {
                PlayerName = playerName;
                Score = score;
                Difficulty = difficulty;
                GameMode = gameMode;
                Date = DateTime.Now;
                GameDuration = duration;
                SnakeLength = snakeLength;
            }
        }

        // End Game Options Klasse
        public class EndGameOptions
        {
            public int FinalScore { get; set; }
            public int HighScore { get; set; }
            public bool IsNewHighScore { get; set; }
            public TimeSpan GameDuration { get; set; }
            public int SnakeLength { get; set; }
            public List<HighScoreEntry> TopHighScores { get; set; }

            public EndGameOptions(int finalScore, int highScore, bool isNewHighScore, TimeSpan duration, int snakeLength, List<HighScoreEntry> topScores)
            {
                FinalScore = finalScore;
                HighScore = highScore;
                IsNewHighScore = isNewHighScore;
                GameDuration = duration;
                SnakeLength = snakeLength;
                TopHighScores = topScores;
            }
        }

        public GameState(int width, int height)
        {
            BoardWidth = width;
            BoardHeight = height;
            _random = new Random();
            Snake = new List<Point>();
            _snakePositions = new HashSet<Point>();
            CurrentDifficulty = Difficulty.Medium;
            CurrentGameMode = GameMode.Classic;
            HighScores = new List<HighScoreEntry>();

            LoadHighScores();
            Reset();
        }

        public void Reset()
        {
            Snake.Clear();
            _snakePositions.Clear();

            // Start in der Mitte des Spielfelds
            var startX = BoardWidth / 2;
            var startY = BoardHeight / 2;
            var startPoint = new Point(startX, startY);

            Snake.Add(startPoint);
            _snakePositions.Add(startPoint);

            CurrentDirection = Direction.Right;
            _nextDirection = Direction.Right;
            GameOver = false;
            Score = 0;
            JustAte = false;
            FoodDigestionProgress = 0;
            FoodsEaten = 0;
            _gameStartTime = DateTime.Now;
            GameTime = TimeSpan.Zero;
            CurrentFoodType = FoodType.Normal;
            IsInvincible = false;
            _invincibilityTimer = 0;
            _consecutiveFoods = 0;
            IsPaused = false;

            // Zeitlimit basierend auf Spielmodus setzen
            _timeLimit = CurrentGameMode switch
            {
                GameMode.TimeAttack => TimeSpan.FromMinutes(3),
                GameMode.Survival => TimeSpan.FromMinutes(5),
                _ => TimeSpan.Zero // Kein Zeitlimit für Classic
            };

            PlaceFood();
            OnGameReset?.Invoke();
            OnScoreChanged?.Invoke(Score);
        }

        public void ChangeDirection(Direction newDirection)
        {
            // Verhindere 180-Grad Wendungen
            if ((CurrentDirection == Direction.Up && newDirection != Direction.Down) ||
                (CurrentDirection == Direction.Down && newDirection != Direction.Up) ||
                (CurrentDirection == Direction.Left && newDirection != Direction.Right) ||
                (CurrentDirection == Direction.Right && newDirection != Direction.Left))
            {
                _nextDirection = newDirection;
            }
        }

        public void TogglePause()
        {
            IsPaused = !IsPaused;
            OnGameMessage?.Invoke(IsPaused ? "Game Paused" : "Game Resumed");
        }

        public void Move()
        {
            if (GameOver || IsPaused) return;

            UpdateGameTime();

            // Zeitlimit prüfen
            if (CurrentGameMode == GameMode.TimeAttack && GameTime >= _timeLimit)
            {
                HandleTimeUp();
                return;
            }

            CurrentDirection = _nextDirection;
            JustAte = false;

            // Bewege den Kopf
            var head = Snake[0];
            var newHead = CalculateNewHeadPosition(head);

            // Kollisionsprüfung
            if (CheckWallCollision(newHead) && !IsInvincible)
            {
                // Für Survival-Modus: Durch Wände gehen
                if (CurrentGameMode == GameMode.Survival)
                {
                    newHead = HandleWallWrap(newHead);
                }
                else
                {
                    EndGame();
                    return;
                }
            }

            if (CheckSelfCollision(newHead) && !IsInvincible)
            {
                EndGame();
                return;
            }

            // Füge neuen Kopf hinzu
            Snake.Insert(0, newHead);
            _snakePositions.Add(newHead);

            // Prüfe auf Essen
            if (newHead == FoodPosition)
            {
                ProcessFoodCollision();
            }
            else
            {
                // Entferne Schwanz, wenn kein Essen gegessen wurde
                var tail = Snake[Snake.Count - 1];
                Snake.RemoveAt(Snake.Count - 1);
                _snakePositions.Remove(tail);
                _consecutiveFoods = 0; // Reset Combo
            }

            // Update Power-Ups und Timer
            UpdatePowerUps();
            UpdateFoodTimer();

            OnSnakeMoved?.Invoke();
        }

        private Point HandleWallWrap(Point position)
        {
            int x = position.X;
            int y = position.Y;

            if (x < 0) x = BoardWidth - 1;
            else if (x >= BoardWidth) x = 0;

            if (y < 0) y = BoardHeight - 1;
            else if (y >= BoardHeight) y = 0;

            return new Point(x, y);
        }

        private void HandleTimeUp()
        {
            if (CurrentGameMode == GameMode.TimeAttack)
            {
                OnGameMessage?.Invoke("Time's up! Final score: " + Score);
            }
            EndGame();
        }

        private Point CalculateNewHeadPosition(Point head)
        {
            return CurrentDirection switch
            {
                Direction.Up => new Point(head.X, head.Y - 1),
                Direction.Down => new Point(head.X, head.Y + 1),
                Direction.Left => new Point(head.X - 1, head.Y),
                Direction.Right => new Point(head.X + 1, head.Y),
                _ => head
            };
        }

        private bool CheckWallCollision(Point position)
        {
            return position.X < 0 || position.X >= BoardWidth ||
                   position.Y < 0 || position.Y >= BoardHeight;
        }

        private bool CheckSelfCollision(Point position)
        {
            return _snakePositions.Contains(position);
        }

        private void ProcessFoodCollision()
        {
            int oldScore = Score;
            int pointsEarned = CalculatePoints();
            Score += pointsEarned;
            FoodsEaten++;
            _consecutiveFoods++;

            JustAte = true;
            FoodDigestionProgress = 100;

            // HighScore aktualisieren
            if (Score > HighScore)
            {
                HighScore = Score;
                OnHighScoreChanged?.Invoke(HighScore);
            }

            // Power-Up Effekte anwenden
            ApplyFoodEffects();

            // Combo-Nachricht
            if (_consecutiveFoods >= 3)
            {
                OnGameMessage?.Invoke($"Combo! {_consecutiveFoods} in a row!");
            }

            PlaceFood();

            OnScoreChanged?.Invoke(Score);
            OnFoodPlaced?.Invoke(FoodPosition);
        }

        private void ApplyFoodEffects()
        {
            switch (CurrentFoodType)
            {
                case FoodType.Bonus:
                    OnGameMessage?.Invoke("Bonus Points!");
                    break;
                case FoodType.Speed:
                    CurrentSpeed = Math.Max(50, CurrentSpeed - 20);
                    OnGameMessage?.Invoke("Speed Boost!");
                    break;
                case FoodType.Slow:
                    CurrentSpeed += 30;
                    OnGameMessage?.Invoke("Slowed Down!");
                    break;
            }
        }

        private void UpdatePowerUps()
        {
            if (IsInvincible)
            {
                _invincibilityTimer--;
                if (_invincibilityTimer <= 0)
                {
                    IsInvincible = false;
                    OnGameMessage?.Invoke("Invincibility ended!");
                }
            }
        }

        private void UpdateFoodTimer()
        {
            _foodTimer++;

            // Futter verschwindet nach einer Weile (nur bei höheren Schwierigkeitsgraden)
            if (CurrentDifficulty == Difficulty.Hard && _foodTimer > 50)
            {
                OnGameMessage?.Invoke("Food disappeared!");
                PlaceFood();
            }
        }

        private void UpdateGameTime()
        {
            GameTime = DateTime.Now - _gameStartTime;
        }

        private void EndGame()
        {
            GameOver = true;

            // Highscore prüfen und hinzufügen
            bool isNewHighScore = CheckAndAddHighScore();

            // End Game Options erstellen und anzeigen
            var endGameOptions = new EndGameOptions(
                Score,
                HighScore,
                isNewHighScore,
                GameTime,
                Snake.Count,
                GetTopHighScores(5) // Top 5 Highscores anzeigen
            );

            OnGameOverChanged?.Invoke(true);
            OnGameMessage?.Invoke($"Game Over! Final Score: {Score}");
            OnShowEndGameOptions?.Invoke(endGameOptions);
        }

        private bool CheckAndAddHighScore()
        {
            // Prüfe ob der Score hoch genug für die Bestenliste ist
            if (HighScores.Count < 10 || Score > HighScores.Last().Score)
            {
                // Hier würde normalerweise eine UI für die Namenseingabe erscheinen
                // Für jetzt verwenden wir einen Standardnamen
                AddHighScore("Player", Score);
                return true;
            }
            return false;
        }

        public void AddHighScore(string playerName, int score)
        {
            var entry = new HighScoreEntry(
                playerName,
                score,
                CurrentDifficulty,
                CurrentGameMode,
                GameTime,
                Snake.Count
            );

            HighScores.Add(entry);

            // Nach Score sortieren (absteigend)
            HighScores = HighScores
                .OrderByDescending(hs => hs.Score)
                .ThenBy(hs => hs.GameDuration)
                .Take(10) // Nur Top 10 behalten
                .ToList();

            SaveHighScores();
            OnHighScoresUpdated?.Invoke(HighScores);
        }

        private List<HighScoreEntry> GetTopHighScores(int count)
        {
            return HighScores
                .OrderByDescending(hs => hs.Score)
                .Take(count)
                .ToList();
        }

        private void PlaceFood()
        {
            var freePositions = GetFreePositions();

            if (freePositions.Count == 0)
            {
                HandleWinCondition();
                return;
            }

            CurrentFoodType = DetermineFoodType();
            FoodPosition = freePositions[_random.Next(freePositions.Count)];
            _foodTimer = 0;

            OnFoodPlaced?.Invoke(FoodPosition);
        }

        private List<Point> GetFreePositions()
        {
            var freePositions = new List<Point>();

            for (int x = 0; x < BoardWidth; x++)
            {
                for (int y = 0; y < BoardHeight; y++)
                {
                    var pos = new Point(x, y);
                    if (!_snakePositions.Contains(pos))
                    {
                        freePositions.Add(pos);
                    }
                }
            }

            return freePositions;
        }

        private FoodType DetermineFoodType()
        {
            int chance = _random.Next(100);

            if (chance < 5) // 5% Chance für Bonus
                return FoodType.Bonus;
            else if (chance < 10) // 5% Chance für Speed
                return FoodType.Speed;
            else if (chance < 15) // 5% Chance für Slow
                return FoodType.Slow;

            return FoodType.Normal;
        }

        private void HandleWinCondition()
        {
            OnGameMessage?.Invoke("You Win! Board completed!");
            AddHighScore("Player", Score + 1000); // Bonus für Gewinnen
            GameOver = true;
            OnGameOverChanged?.Invoke(true);
        }

        private int CalculatePoints()
        {
            int basePoints = CurrentFoodType switch
            {
                FoodType.Normal => 100,
                FoodType.Bonus => 300,
                FoodType.Speed => 150,
                FoodType.Slow => 200,
                _ => 100
            };

            float multiplier = CalculateMultiplier();

            // Combo-Bonus
            if (_consecutiveFoods >= 3)
            {
                multiplier *= 1.0f + (_consecutiveFoods * 0.1f);
            }

            // Schwierigkeits-Bonus
            multiplier *= CurrentDifficulty switch
            {
                Difficulty.Easy => 0.7f,
                Difficulty.Medium => 1.0f,
                Difficulty.Hard => 1.5f,
                _ => 1.0f
            };

            int points = (int)(basePoints * multiplier);

            // Schnellkeits-Bonus
            if (_foodTimer < 30)
            {
                points = (int)(points * 1.3f);
            }

            return points;
        }

        private float CalculateMultiplier()
        {
            float speedMultiplier = CurrentSpeed switch
            {
                <= 50 => 3.0f,
                <= 70 => 2.5f,
                <= 90 => 2.0f,
                <= 100 => 1.5f,
                <= 130 => 1.0f,
                <= 160 => 0.75f,
                _ => 0.5f
            };

            float lengthMultiplier = 1.0f + (Snake.Count / 10.0f) * 0.5f;
            float timeMultiplier = 1.0f + (float)GameTime.TotalMinutes * 0.1f;

            return speedMultiplier * lengthMultiplier * timeMultiplier;
        }

        // Highscore Management
        private void LoadHighScores()
        {
            try
            {
                if (System.IO.File.Exists(HIGHSCORE_FILE))
                {
                    var json = System.IO.File.ReadAllText(HIGHSCORE_FILE);
                    HighScores = JsonSerializer.Deserialize<List<HighScoreEntry>>(json) ?? new List<HighScoreEntry>();
                }
            }
            catch (Exception ex)
            {
                HighScores = new List<HighScoreEntry>();
                Console.WriteLine($"Error loading highscores: {ex.Message}");
            }
        }

        private void SaveHighScores()
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(HighScores, options);
                System.IO.File.WriteAllText(HIGHSCORE_FILE, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving highscores: {ex.Message}");
            }
        }

        public List<HighScoreEntry> GetHighScores()
        {
            return new List<HighScoreEntry>(HighScores);
        }

        public void ClearHighScores()
        {
            HighScores.Clear();
            SaveHighScores();
            OnHighScoresUpdated?.Invoke(HighScores);
        }

        // Menu Actions - Diese Methoden werden aufgerufen, wenn der User eine Option wählt
        public void StartNewGame()
        {
            Reset();
        }

        public void ReturnToMainMenu()
        {
            // Hier könnte man zum Hauptmenü zurückkehren
            OnGameMessage?.Invoke("Returning to Main Menu...");
        }

        public void ExitGame()
        {
            // Hier könnte man das Spiel beenden
            OnGameMessage?.Invoke("Exiting Game...");
            SaveHighScores();
        }

        // Die ShowHighScores Methode wurde entfernt - verwende stattdessen das Event

        // Power-Up Methoden
        public void ActivateInvincibility(int duration)
        {
            IsInvincible = true;
            _invincibilityTimer = duration;
            OnGameMessage?.Invoke("Invincibility Activated!");
        }

        // Save/Load Methoden
        public class GameSaveData
        {
            public int Score { get; set; }
            public int HighScore { get; set; }
            public List<Point> Snake { get; set; }
            public Point FoodPosition { get; set; }
            public Direction CurrentDirection { get; set; }
            public int FoodsEaten { get; set; }
            public TimeSpan GameTime { get; set; }
            public Difficulty CurrentDifficulty { get; set; }
            public GameMode CurrentGameMode { get; set; }
        }

        public GameSaveData SaveGame()
        {
            return new GameSaveData
            {
                Score = this.Score,
                HighScore = this.HighScore,
                Snake = new List<Point>(this.Snake),
                FoodPosition = this.FoodPosition,
                CurrentDirection = this.CurrentDirection,
                FoodsEaten = this.FoodsEaten,
                GameTime = this.GameTime,
                CurrentDifficulty = this.CurrentDifficulty,
                CurrentGameMode = this.CurrentGameMode
            };
        }

        public void LoadGame(GameSaveData saveData)
        {
            this.Score = saveData.Score;
            this.HighScore = saveData.HighScore;
            this.Snake = new List<Point>(saveData.Snake);
            this.FoodPosition = saveData.FoodPosition;
            this.CurrentDirection = saveData.CurrentDirection;
            this._nextDirection = saveData.CurrentDirection;
            this.FoodsEaten = saveData.FoodsEaten;
            this.GameTime = saveData.GameTime;
            this.CurrentDifficulty = saveData.CurrentDifficulty;
            this.CurrentGameMode = saveData.CurrentGameMode;

            // Snake Positions HashSet aktualisieren
            _snakePositions.Clear();
            foreach (var segment in Snake)
            {
                _snakePositions.Add(segment);
            }

            OnGameReset?.Invoke();
            OnScoreChanged?.Invoke(Score);
        }
    }
}