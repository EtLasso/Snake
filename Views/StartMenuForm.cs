using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Snake.Views
{
    public class StartMenuForm : Form
    {
        private Panel mainPanel;
        private Label titleLabel;
        private Label subtitleLabel;
        private Panel controlsPanel;
        private Button startButton;
        private Button exitButton;

        public StartMenuForm()
        {
            // MAXIMALE Anti-Flicker-Einstellungen
            this.SetStyle(
                ControlStyles.AllPaintingInWmPaint | 
                ControlStyles.UserPaint | 
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw, 
                true);
            this.DoubleBuffered = true;
            this.UpdateStyles();
            
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            // Form Settings
            this.Text = "Snake MVC Game";
            this.Size = new Size(700, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.FromArgb(15, 20, 35);

            // Main Panel mit Anti-Flicker
            mainPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(15, 20, 35)
            };

            // Title Label
            titleLabel = new Label
            {
                Text = "🐍 SNAKE GAME",
                Font = new Font("Segoe UI", 48, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 255, 180),
                AutoSize = false,
                Size = new Size(600, 80),
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point((this.Width - 600) / 2, 50),
                BackColor = Color.Transparent
            };

            // Subtitle Label
            subtitleLabel = new Label
            {
                Text = "MVC Architecture Edition",
                Font = new Font("Segoe UI", 14, FontStyle.Italic),
                ForeColor = Color.FromArgb(200, 100, 255),
                AutoSize = false,
                Size = new Size(400, 30),
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point((this.Width - 400) / 2, 140),
                BackColor = Color.Transparent
            };

            // Controls Info Panel
            controlsPanel = CreateControlsPanel();

            // Start Button
            startButton = CreateStyledButton("START GAME", new Point(200, 480));
            startButton.Click += (s, e) =>
            {
                this.DialogResult = DialogResult.OK;
                this.Close();
            };

            // Exit Button
            exitButton = CreateStyledButton("EXIT", new Point(400, 480));
            exitButton.Click += (s, e) =>
            {
                this.DialogResult = DialogResult.Cancel;
                this.Close();
            };

            // Add all controls
            mainPanel.Controls.AddRange(new Control[] { titleLabel, subtitleLabel, controlsPanel, startButton, exitButton });
            this.Controls.Add(mainPanel);

            // Keyboard shortcuts
            this.KeyPreview = true;
            this.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Escape)
                {
                    this.DialogResult = DialogResult.Cancel;
                    this.Close();
                }
                else if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Space)
                {
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
            };
        }

        private Panel CreateControlsPanel()
        {
            var panel = new Panel
            {
                Size = new Size(600, 280),
                Location = new Point((this.Width - 600) / 2, 180),
                BackColor = Color.FromArgb(25, 30, 45),
                BorderStyle = BorderStyle.FixedSingle
            };

            // Controls Info
            var controlsInfo = new[]
            {
                "🎮 STEUERUNG:",
                "",
                "◆ Pfeiltasten oder WASD - Bewegung",
                "◆ LEERTASTE - Pause / Neustart",
                "◆ ESC - Pause",
                "◆ R - Neustart",
                "◆ Q - Beenden",
                "◆ + / - - Geschwindigkeit anpassen",
                "",
                "🎯 ZIEL:",
                "Friss das Essen und wachse!",
                "Vermeide Wände und deinen eigenen Körper!"
            };

            int yOffset = 15;
            foreach (var info in controlsInfo)
            {
                var label = new Label
                {
                    Text = info,
                    Font = info.StartsWith("🎮") || info.StartsWith("🎯") 
                        ? new Font("Segoe UI", 14, FontStyle.Bold)
                        : new Font("Segoe UI", 11),
                    ForeColor = info.StartsWith("🎮") || info.StartsWith("🎯")
                        ? Color.FromArgb(255, 180, 80)
                        : info.StartsWith("◆")
                        ? Color.FromArgb(200, 220, 240)
                        : Color.FromArgb(150, 170, 200),
                    AutoSize = false,
                    Size = new Size(580, info == "" ? 5 : 25),
                    TextAlign = info.StartsWith("◆") ? ContentAlignment.MiddleLeft : ContentAlignment.MiddleCenter,
                    Location = new Point(10, yOffset),
                    BackColor = Color.Transparent
                };

                panel.Controls.Add(label);
                yOffset += info == "" ? 5 : 25;
            }

            return panel;
        }

        private Button CreateStyledButton(string text, Point location)
        {
            var button = new Button
            {
                Text = text,
                Size = new Size(180, 60),
                Location = location,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(40, 80, 150, 255),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                Cursor = Cursors.Hand
            };

            button.FlatAppearance.BorderSize = 2;
            button.FlatAppearance.BorderColor = Color.FromArgb(100, 255, 180);

            // Simple hover effect (keine Animation)
            button.MouseEnter += (s, e) =>
            {
                button.BackColor = Color.FromArgb(60, 100, 255, 180);
            };

            button.MouseLeave += (s, e) =>
            {
                button.BackColor = Color.FromArgb(40, 80, 150, 255);
            };

            return button;
        }
    }
}
