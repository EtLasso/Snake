using System;
using System.Linq;
using System.Windows.Forms;
using Snake.Models;
using Snake.Views;

namespace Snake.Controllers
{
    public class GameController
    {
        private readonly GameState _gameState;
        private readonly GameView _gameView;
        private readonly System.Windows.Forms.Timer _gameTimer;
        private bool _isPaused;
        private int _gameSpeed;

        // NEUE EVENTS für Navigation
        public event Action MainMenuRequested;
        public event Action ExitRequested;
        public event Action HighScoresRequested;

        public GameController(GameState gameState, GameView gameView, int initialSpeed = 100)
        {
            _gameState = gameState;
            _gameView = gameView;
            _isPaused = false;
            _gameSpeed = initialSpeed;

            _gameTimer = new System.Windows.Forms.Timer();
            _gameTimer.Interval = _gameSpeed;
            _gameTimer.Tick += GameTimer_Tick;

            _gameView.KeyPressed += OnKeyPressed;
            _gameView.RestartRequested += OnRestartRequested;
            _gameView.ExitRequested += OnExitRequested;
            _gameView.MainMenuRequested += OnMainMenuRequested;
            _gameView.HighScoresRequested += OnHighScoresRequested;

            UpdateGameSpeed();
        }

        public void Start()
        {
            _gameState.Reset();
            _isPaused = false;
            _gameTimer.Start();
            _gameView.UpdateView(_gameState);
            _gameView.HideGameOverMenu();
        }

        public void Stop()
        {
            _gameTimer.Stop();
        }

        public void Pause()
        {
            if (!_gameState.GameOver)
            {
                _isPaused = !_isPaused;
                if (_isPaused)
                {
                    _gameTimer.Stop();
                }
                else
                {
                    _gameTimer.Start();
                }
            }
        }

        private void GameTimer_Tick(object sender, EventArgs e)
        {
            if (!_isPaused && !_gameState.GameOver)
            {
                _gameState.Move();

                if (_gameState.GameOver)
                {
                    _gameTimer.Stop();
                    
                    // Speichere Highscore bevor das Game Over Menu angezeigt wird
                    SaveHighScore();
                    
                    // Get top high scores from the public HighScores property
                    var topScores = _gameState.HighScores
                        .OrderByDescending(hs => hs.Score)
                        .Take(5)
                        .ToList();
                    
                    // Prüfe ob es ein neuer Highscore ist
                    bool isNewHighScore = topScores.Count == 0 || _gameState.Score >= topScores[0].Score;

                    _gameView.ShowGameOverMenu(
                        _gameState.Score,
                        _gameState.CurrentSpeed,
                        _gameState.Snake.Count,
                        topScores,
                        isNewHighScore
                    );
                }
                else
                {
                    _gameView.UpdateView(_gameState);
                }
            }
        }

        private void SaveHighScore()
        {
            // Automatisch Highscore speichern wenn das Spiel endet
            if (_gameState.Score > 0)
            {
                // Prüfe ob Score hoch genug für Top 10 ist
                var currentHighScores = _gameState.HighScores.OrderByDescending(hs => hs.Score).ToList();
                
                if (currentHighScores.Count < 10 || _gameState.Score > currentHighScores.Last().Score)
                {
                    // Standardname, könnte später durch Benutzereingabe ersetzt werden
                    string playerName = "Player";
                    
                    // Prüfe ob es bereits einen Eintrag mit diesem Score gibt
                    if (!currentHighScores.Any(hs => hs.Score == _gameState.Score))
                    {
                        _gameState.AddHighScore(playerName, _gameState.Score);
                    }
                }
            }
        }

        private void OnKeyPressed(object sender, KeyEventArgs e)
        {
            if (_gameState.GameOver)
            {
                _gameView.HandleGameOverInput(e.KeyCode);
                return;
            }

            switch (e.KeyCode)
            {
                case Keys.Up:
                case Keys.W:
                    _gameState.ChangeDirection(GameState.Direction.Up);
                    break;

                case Keys.Down:
                case Keys.S:
                    _gameState.ChangeDirection(GameState.Direction.Down);
                    break;

                case Keys.Left:
                case Keys.A:
                    _gameState.ChangeDirection(GameState.Direction.Left);
                    break;

                case Keys.Right:
                case Keys.D:
                    _gameState.ChangeDirection(GameState.Direction.Right);
                    break;

                case Keys.Space:
                case Keys.Escape:
                    Pause();
                    break;

                case Keys.R:
                    Start();
                    break;

                case Keys.Add:
                case Keys.Oemplus:
                    IncreaseSpeed();
                    break;

                case Keys.Subtract:
                case Keys.OemMinus:
                    DecreaseSpeed();
                    break;
            }
        }

        private void IncreaseSpeed()
        {
            if (_gameSpeed > 50)
            {
                _gameSpeed -= 10;
                _gameTimer.Interval = _gameSpeed;
                UpdateGameSpeed();
            }
        }

        private void DecreaseSpeed()
        {
            if (_gameSpeed < 200)
            {
                _gameSpeed += 10;
                _gameTimer.Interval = _gameSpeed;
                UpdateGameSpeed();
            }
        }

        private void UpdateGameSpeed()
        {
            _gameState.CurrentSpeed = _gameSpeed;
        }

        public void Dispose()
        {
            _gameTimer?.Stop();
            _gameTimer?.Dispose();
            _gameView.KeyPressed -= OnKeyPressed;
            _gameView.RestartRequested -= OnRestartRequested;
            _gameView.ExitRequested -= OnExitRequested;
            _gameView.MainMenuRequested -= OnMainMenuRequested;
            _gameView.HighScoresRequested -= OnHighScoresRequested;
        }

        public void OnRestartRequested()
        {
            Start();
        }

        public void OnExitRequested()
        {
            // Event auslösen statt direkt Application.Exit zu verwenden
            ExitRequested?.Invoke();
        }

        public void OnMainMenuRequested()
        {
            // Event auslösen um zurück zum Hauptmenü zu gehen
            Stop();
            MainMenuRequested?.Invoke();
        }

        public void OnHighScoresRequested()
        {
            // Event auslösen, damit Form1 die HighscoresForm öffnen kann
            // Wir rufen NICHT _gameView.ShowHighScores() auf, um die Rekursion zu vermeiden
            HighScoresRequested?.Invoke();
        }
    }
}
