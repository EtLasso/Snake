using Snake.Controllers;
using Snake.Models;
using Snake.Views;

namespace Snake
{
    public partial class Form1 : Form
    {
        private GameState _gameState;
        private GameView _gameView;
        private GameController _gameController;

        public Form1()
        {
            InitializeComponent();
            InitializeGame();
        }

        private void InitializeGame()
        {
            // Form-Einstellungen
            this.Text = "🐍 HungrySnake - Rico Edition";
            this.BackColor = Color.FromArgb(8, 10, 18);
            this.ClientSize = new Size(900, 900);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;

            // MVC-Komponenten erstellen
            int boardWidth = 25;
            int boardHeight = 25;

            _gameState = new GameState(boardWidth, boardHeight);
            
            _gameView = new GameView
            {
                Dock = DockStyle.Fill,
                TabIndex = 0
            };
            this.Controls.Add(_gameView);

            _gameController = new GameController(_gameState, _gameView);
            _gameController.Start();

            _gameView.Focus();
        }

        public void RestartGame()
        {
            _gameController.Start();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _gameController?.Stop();
            _gameController?.Dispose();
            base.OnFormClosing(e);
        }
    }
}
