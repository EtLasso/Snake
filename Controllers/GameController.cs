using System;
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

        public GameController(GameState gameState, GameView gameView)
        {
            _gameState = gameState;
            _gameView = gameView;
            _isPaused = false;
            _gameSpeed = 100;

            _gameTimer = new System.Windows.Forms.Timer();
            _gameTimer.Interval = _gameSpeed;
            _gameTimer.Tick += GameTimer_Tick;

            _gameView.KeyPressed += OnKeyPressed;

            // NEU: auf View-Events hören
            _gameView.RestartRequested += OnRestartRequested;
            _gameView.ExitRequested += OnExitRequested;
        }

        public void Start()
        {
            _gameState.Reset();
            _isPaused = false;
            _gameTimer.Start();
            _gameView.UpdateView(_gameState);
            _gameView.HideGameOverMenu(); // Menü verstecken beim Start
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
                    // Zeige In-Game Menü statt Dialog!
                    _gameView.ShowGameOverMenu(_gameState.Score);
                }
                else
                {
                    _gameView.UpdateView(_gameState);
                }
            }
        }

        private void OnKeyPressed(object sender, KeyEventArgs e)
        {
            // Bei Game Over nur Menü-Steuerung
            if (_gameState.GameOver)
            {
                _gameView.HandleGameOverInput(e.KeyCode);
                return;
            }

            switch (e.KeyCode)
            {
                // Bewegung
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

                // Spielsteuerung
                case Keys.Space:
                case Keys.Escape:
                    Pause();
                    break;

                case Keys.R:
                    Start();
                    break;

                // Geschwindigkeit
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
            }
        }

        private void DecreaseSpeed()
        {
            if (_gameSpeed < 200)
            {
                _gameSpeed += 10;
                _gameTimer.Interval = _gameSpeed;
            }
        }

        public void Dispose()
        {
            _gameTimer?.Stop();
            _gameTimer?.Dispose();
            _gameView.KeyPressed -= OnKeyPressed;

            // NEU: Events abmelden
            _gameView.RestartRequested -= OnRestartRequested;
            _gameView.ExitRequested -= OnExitRequested;
        }

        // Wird vom GameView aufgerufen
        public void OnRestartRequested()
        {
            Start();
        }

        public void OnExitRequested()
        {
            Application.Exit();
        }
    }
}