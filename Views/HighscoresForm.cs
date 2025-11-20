using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Snake.Data;

namespace Snake.Views
{
    public class HighscoresForm : Form
    {
        private HighscoreManager _highscoreManager;

        public HighscoresForm(HighscoreManager highscoreManager)
        {
            _highscoreManager = highscoreManager;
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            // Form Settings
            this.Text = "🏆 Highscores - Top 5";
            this.Size = new Size(650, 550);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.FromArgb(15, 20, 35);

            // Title
            var titleLabel = new Label
            {
                Text = "🏆 HIGHSCORES - TOP 5 🏆",
                Font = new Font("Segoe UI", 28, FontStyle.Bold),
                ForeColor = Color.FromArgb(255, 200, 0),
                AutoSize = false,
                Size = new Size(600, 60),
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(25, 20),
                BackColor = Color.Transparent
            };
            this.Controls.Add(titleLabel);

            // Highscores Panel
            var highscoresPanel = new Panel
            {
                Size = new Size(580, 320),
                Location = new Point(35, 90),
                BackColor = Color.FromArgb(25, 30, 45),
                BorderStyle = BorderStyle.FixedSingle,
                AutoScroll = false
            };

            var highscores = _highscoreManager.GetTop5();
            
            if (highscores.Count == 0)
            {
                var noScoresLabel = new Label
                {
                    Text = "Noch keine Highscores!\n\n🎮 Spiele dein erstes Spiel! 🎮",
                    Font = new Font("Segoe UI", 18, FontStyle.Bold),
                    ForeColor = Color.FromArgb(150, 170, 200),
                    AutoSize = false,
                    Size = new Size(560, 300),
                    TextAlign = ContentAlignment.MiddleCenter,
                    Location = new Point(10, 10),
                    BackColor = Color.Transparent
                };
                highscoresPanel.Controls.Add(noScoresLabel);
            }
            else
            {
                int yOffset = 15;
                int rank = 1;

                foreach (var entry in highscores)
                {
                    // Rank & Medal
                    string medal = rank == 1 ? "🥇" : rank == 2 ? "🥈" : rank == 3 ? "🥉" : "🏅";
                    Color rankColor = rank == 1 ? Color.FromArgb(255, 215, 0) :
                                     rank == 2 ? Color.FromArgb(192, 192, 192) :
                                     rank == 3 ? Color.FromArgb(205, 127, 50) :
                                     Color.FromArgb(100, 255, 180);

                    // Entry Panel
                    var entryPanel = new Panel
                    {
                        Size = new Size(560, 55),
                        Location = new Point(10, yOffset),
                        BackColor = Color.FromArgb(35, 40, 60)
                    };

                    // Rank Label
                    var rankLabel = new Label
                    {
                        Text = $"{medal} #{rank}",
                        Font = new Font("Segoe UI", 14, FontStyle.Bold),
                        ForeColor = rankColor,
                        AutoSize = false,
                        Size = new Size(80, 50),
                        TextAlign = ContentAlignment.MiddleCenter,
                        Location = new Point(5, 2),
                        BackColor = Color.Transparent
                    };
                    entryPanel.Controls.Add(rankLabel);

                    // Score Label
                    var scoreLabel = new Label
                    {
                        Text = $"{entry.Score} pts",
                        Font = new Font("Segoe UI", 16, FontStyle.Bold),
                        ForeColor = Color.FromArgb(255, 180, 80),
                        AutoSize = false,
                        Size = new Size(120, 25),
                        TextAlign = ContentAlignment.MiddleLeft,
                        Location = new Point(95, 5),
                        BackColor = Color.Transparent
                    };
                    entryPanel.Controls.Add(scoreLabel);

                    // Details Label
                    var detailsLabel = new Label
                    {
                        Text = $"{entry.PlayerName} | {entry.Date:dd.MM.yy} | {entry.Speed}ms | Länge: {entry.Length}",
                        Font = new Font("Segoe UI", 10),
                        ForeColor = Color.FromArgb(180, 200, 220),
                        AutoSize = false,
                        Size = new Size(450, 20),
                        TextAlign = ContentAlignment.MiddleLeft,
                        Location = new Point(95, 30),
                        BackColor = Color.Transparent
                    };
                    entryPanel.Controls.Add(detailsLabel);

                    highscoresPanel.Controls.Add(entryPanel);

                    yOffset += 60;
                    rank++;
                }
            }

            this.Controls.Add(highscoresPanel);

            // Close Button
            var closeButton = new Button
            {
                Text = "SCHLIESSEN",
                Size = new Size(200, 50),
                Location = new Point(225, 440),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(40, 80, 150, 255),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            closeButton.FlatAppearance.BorderSize = 2;
            closeButton.FlatAppearance.BorderColor = Color.FromArgb(100, 255, 180);
            closeButton.Click += (s, e) => this.Close();

            closeButton.MouseEnter += (s, e) => closeButton.BackColor = Color.FromArgb(60, 100, 255, 180);
            closeButton.MouseLeave += (s, e) => closeButton.BackColor = Color.FromArgb(40, 80, 150, 255);

            this.Controls.Add(closeButton);

            // Keyboard shortcut
            this.KeyPreview = true;
            this.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Escape || e.KeyCode == Keys.Enter)
                    this.Close();
            };
        }
    }
}
