using System;
using System.Windows.Forms;
using Snake.Controllers;
using Snake.Models;
using Snake.Views;

namespace Snake
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();

            // Haupt-Game-Loop: Zeige Menü, spiele, zurück zum Menü
            bool exitGame = false;

            while (!exitGame)
            {
                // Zeige Startmenü
                using (var startMenu = new StartMenuForm())
                {
                    var result = startMenu.ShowDialog();

                    if (result == DialogResult.OK)
                    {
                        // Starte das Spiel mit der gewählten Geschwindigkeit
                        var gameResult = StartGame(startMenu.SelectedSpeed);
                        
                        // Wenn das Spiel EXIT zurückgibt, beende die Game Loop
                        if (gameResult == GameResult.Exit)
                        {
                            exitGame = true;
                        }
                        // Bei GameResult.MainMenu geht die Loop weiter und zeigt das Menü erneut
                    }
                    else
                    {
                        // Benutzer hat im Menü auf EXIT geklickt oder ESC gedrückt
                        exitGame = true;
                    }
                }
            }
        }

        static GameResult StartGame(int initialSpeed)
        {
            GameResult gameResult = GameResult.MainMenu;

            // Erstelle die MVC-Komponenten
            var gameState = new GameState(20, 15); // 20x15 Spielfeld
            var gameView = new GameView();
            var gameController = new GameController(gameState, gameView, initialSpeed);

            // Hauptformular erstellen
            var mainForm = new Form
            {
                Text = "HungrySnake - Rico Edition",
                Size = new System.Drawing.Size(1000, 800),
                StartPosition = FormStartPosition.CenterScreen,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                BackColor = System.Drawing.Color.FromArgb(15, 20, 35)
            };

            // GameView zum Formular hinzufügen
            gameView.Dock = DockStyle.Fill;
            mainForm.Controls.Add(gameView);

            // Event-Handler für MainMenu und Exit Requests
            gameController.MainMenuRequested += () =>
            {
                gameResult = GameResult.MainMenu;
                mainForm.Close();
            };

            gameController.ExitRequested += () =>
            {
                gameResult = GameResult.Exit;
                mainForm.Close();
            };

            // Stelle sicher, dass die GameView den Fokus bekommt
            mainForm.Shown += (s, e) => gameView.Focus();

            // Controller aufräumen beim Schließen
            mainForm.FormClosed += (s, e) =>
            {
                gameController.Dispose();
            };

            // Spiel starten
            gameController.Start();

            // Form als modalen Dialog anzeigen
            mainForm.ShowDialog();

            return gameResult;
        }
    }

    // Enum für Game-Rückgabewerte
    public enum GameResult
    {
        MainMenu,  // Zurück zum Hauptmenü
        Exit       // Spiel komplett beenden
    }
}
