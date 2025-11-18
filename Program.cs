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
            // Application configuration
            ApplicationConfiguration.Initialize();

            // Zeige Startmenü
            using (var startMenu = new StartMenuForm())
            {
                var result = startMenu.ShowDialog();

                // Wenn "START GAME" geklickt wurde
                if (result == DialogResult.OK)
                {
                    // Starte das Spiel
                    Application.Run(new Form1());
                }
                // Bei "EXIT" oder ESC wird das Programm einfach beendet
            }
        }
    }
}
