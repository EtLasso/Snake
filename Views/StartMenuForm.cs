using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Snake.Data;

namespace Snake.Views
{
    public class StartMenuForm : Form
    {
        private Panel mainPanel;
        private Button startButton;
        private Button settingsButton;
        private Button howToPlayButton;
        private Button highscoresButton;
        private Button exitButton;
        private HighscoreManager _highscoreManager;

        // Öffentliche Property für die gewählte Geschwindigkeit (in ms)
        public int SelectedSpeed { get; private set; } = 100;

        // Bild als Hintergrund
        private Image _backgroundImage;

        public StartMenuForm()
        {
            this.SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw,
                true);
            this.DoubleBuffered = true;
            this.UpdateStyles();

            _highscoreManager = new HighscoreManager();

            // Bild laden
            LoadBackgroundImage();

            InitializeComponents();
            ArrangeLayout();
        }

        private void LoadBackgroundImage()
        {
            try
            {
                if (System.IO.File.Exists("snake.png"))
                {
                    _backgroundImage = Image.FromFile("snake.png");
                }
                else
                {
                    // Fallback: Einfacher dunkler Hintergrund
                    _backgroundImage = CreateFallbackBackground();
                }
            }
            catch (Exception ex)
            {
                _backgroundImage = CreateFallbackBackground();
                System.Diagnostics.Debug.WriteLine($"Error loading background image: {ex.Message}");
            }
        }

        private Image CreateFallbackBackground()
        {
            var bmp = new Bitmap(800, 700);
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;

                // Einfacher dunkler Hintergrund
                using (var bgBrush = new SolidBrush(Color.FromArgb(15, 20, 35)))
                {
                    g.FillRectangle(bgBrush, 0, 0, 800, 700);
                }
            }
            return bmp;
        }

        private void InitializeComponents()
        {
            this.Text = "HungrySnake - Rico Edition";
            this.Size = new Size(800, 700);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.Black;

            // Transparentes Hauptpanel
            mainPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent
            };

            // Moderne Buttons ohne Text-Overhead
            startButton = CreateStyledButton("🚀 SPIEL STARTEN", Color.FromArgb(100, 255, 180));
            startButton.Click += (s, e) => StartGame();

            settingsButton = CreateStyledButton("⚙️ EINSTELLUNGEN", Color.FromArgb(0, 200, 255));
            settingsButton.Click += (s, e) => ShowSettings();

            howToPlayButton = CreateStyledButton("❓ HOW TO PLAY", Color.FromArgb(180, 100, 255));
            howToPlayButton.Click += (s, e) => ShowHowToPlay();

            highscoresButton = CreateStyledButton("🏆 HIGHSCORES", Color.FromArgb(255, 200, 80));
            highscoresButton.Click += (s, e) => ShowHighscores();

            exitButton = CreateStyledButton("🔴 BEENDEN", Color.FromArgb(255, 100, 100));
            exitButton.Click += (s, e) => ExitGame();

            mainPanel.Controls.AddRange(new Control[] {
                startButton, settingsButton, howToPlayButton, highscoresButton, exitButton
            });
            this.Controls.Add(mainPanel);

            this.KeyPreview = true;
            this.KeyDown += OnKeyDown;
        }

        private void ArrangeLayout()
        {
            int centerX = this.Width / 2;

            // Buttons vertikal in der Mitte - mehr Platz für das Bild
            int buttonY = this.Height / 2 - 40;
            int buttonSpacing = 12;

            startButton.Location = new Point(centerX - startButton.Width / 2, buttonY);
            settingsButton.Location = new Point(centerX - settingsButton.Width / 2, startButton.Bottom + buttonSpacing);
            howToPlayButton.Location = new Point(centerX - howToPlayButton.Width / 2, settingsButton.Bottom + buttonSpacing);
            highscoresButton.Location = new Point(centerX - highscoresButton.Width / 2, howToPlayButton.Bottom + buttonSpacing);
            exitButton.Location = new Point(centerX - exitButton.Width / 2, highscoresButton.Bottom + buttonSpacing + 15);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            // Bild als Hintergrund zeichnen (gestreckt auf Fenstergröße)
            if (_backgroundImage != null)
            {
                e.Graphics.DrawImage(_backgroundImage, 0, 0, this.Width, this.Height);
            }

            // Leichter dunkler Overlay nur hinter den Buttons für bessere Lesbarkeit
            using (var overlayBrush = new SolidBrush(Color.FromArgb(90, 0, 0, 0)))
            {
                int overlayHeight = exitButton.Bottom - startButton.Top + 40;
                e.Graphics.FillRectangle(overlayBrush,
                    startButton.Left - 20,
                    startButton.Top - 20,
                    startButton.Width + 40,
                    overlayHeight);
            }

            // Dünner Akzent-Rahmen um die Buttons
            using (var borderPen = new Pen(Color.FromArgb(60, 255, 255, 255), 1f))
            {
                int borderHeight = exitButton.Bottom - startButton.Top + 40;
                e.Graphics.DrawRectangle(borderPen,
                    startButton.Left - 20,
                    startButton.Top - 20,
                    startButton.Width + 40,
                    borderHeight);
            }
        }

        private Button CreateStyledButton(string text, Color accentColor)
        {
            var button = new Button
            {
                Text = text,
                Size = new Size(220, 45),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(200, 20, 25, 40), // Leicht transparenter Hintergrund
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Cursor = Cursors.Hand,
                TabStop = true
            };

            button.FlatAppearance.BorderSize = 2;
            button.FlatAppearance.BorderColor = accentColor;
            button.FlatAppearance.MouseOverBackColor = Color.FromArgb(230, 40, 45, 65);
            button.FlatAppearance.MouseDownBackColor = Color.FromArgb(180, 10, 15, 30);

            return button;
        }

        private void StartGame()
        {
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void ShowSettings()
        {
            using (var settingsForm = new SettingsForm(_highscoreManager))
            {
                settingsForm.SelectedSpeed = this.SelectedSpeed;

                if (settingsForm.ShowDialog(this) == DialogResult.OK)
                {
                    this.SelectedSpeed = settingsForm.SelectedSpeed;
                }
            }
        }

        private void ShowHowToPlay()
        {
            using (var howToPlayForm = new HowToPlayForm())
            {
                howToPlayForm.ShowDialog(this);
            }
        }

        private void ShowHighscores()
        {
            using (var highscoresForm = new HighscoresForm(_highscoreManager))
            {
                highscoresForm.ShowDialog(this);
            }
        }

        private void ExitGame()
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Escape:
                    ExitGame();
                    break;
                case Keys.Enter:
                case Keys.Space:
                    StartGame();
                    break;
                case Keys.S:
                    ShowSettings();
                    break;
                case Keys.H:
                    ShowHowToPlay();
                    break;
                case Keys.D1:
                    startButton.PerformClick();
                    break;
                case Keys.D2:
                    settingsButton.PerformClick();
                    break;
                case Keys.D3:
                    howToPlayButton.PerformClick();
                    break;
                case Keys.D4:
                    highscoresButton.PerformClick();
                    break;
                case Keys.D5:
                    exitButton.PerformClick();
                    break;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _backgroundImage?.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    // NEUE SettingsForm mit benutzerfreundlichen Geschwindigkeitsstufen
    public class SettingsForm : Form
    {
        private TrackBar speedTrackBar;
        private Label speedLabel;
        private Label speedValueLabel;
        private Label speedDescriptionLabel;
        private Button okButton;
        private Button cancelButton;
        public int SelectedSpeed { get; set; } = 100;

        public SettingsForm(HighscoreManager highscoreManager)
        {
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            this.Text = "Einstellungen - HungrySnake";
            this.Size = new Size(500, 350); // Höhe erhöht für zusätzliche Beschreibung
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.BackColor = Color.FromArgb(25, 30, 45);

            speedLabel = new Label
            {
                Text = "🎯 SPIELGESCHWINDIGKEIT",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 255, 180),
                Size = new Size(400, 30),
                Location = new Point(50, 30),
                TextAlign = ContentAlignment.MiddleCenter
            };

            speedTrackBar = new TrackBar
            {
                Minimum = 1, // Statt ms-Werte, verwenden wir Stufen 1-6
                Maximum = 6,
                Value = SpeedToLevel(SelectedSpeed), // Aktuelle Geschwindigkeit in Stufe umwandeln
                Size = new Size(400, 45),
                Location = new Point(50, 80),
                TickFrequency = 1,
                TickStyle = TickStyle.Both,
                BackColor = Color.FromArgb(35, 40, 60)
            };
            speedTrackBar.Scroll += SpeedTrackBar_Scroll;

            speedValueLabel = new Label
            {
                Text = GetSpeedLevelDescription(speedTrackBar.Value),
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = Color.FromArgb(255, 200, 100),
                Size = new Size(400, 30),
                Location = new Point(50, 130),
                TextAlign = ContentAlignment.MiddleCenter
            };

            speedDescriptionLabel = new Label
            {
                Text = GetSpeedDetailedDescription(speedTrackBar.Value),
                Font = new Font("Segoe UI", 10, FontStyle.Regular),
                ForeColor = Color.FromArgb(180, 200, 220),
                Size = new Size(400, 50),
                Location = new Point(50, 165),
                TextAlign = ContentAlignment.TopCenter
            };

            okButton = new Button
            {
                Text = "✔️ ÜBERNEHMEN",
                Size = new Size(120, 40),
                Location = new Point(150, 240),
                DialogResult = DialogResult.OK,
                BackColor = Color.FromArgb(60, 100, 255, 180),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat
            };
            okButton.FlatAppearance.BorderSize = 2;
            okButton.FlatAppearance.BorderColor = Color.FromArgb(100, 255, 180);

            cancelButton = new Button
            {
                Text = "❌ ABBRECHEN",
                Size = new Size(120, 40),
                Location = new Point(280, 240),
                DialogResult = DialogResult.Cancel,
                BackColor = Color.FromArgb(60, 255, 100, 100),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat
            };
            cancelButton.FlatAppearance.BorderSize = 2;
            cancelButton.FlatAppearance.BorderColor = Color.FromArgb(255, 100, 100);

            this.Controls.AddRange(new Control[] {
                speedLabel, speedTrackBar, speedValueLabel, speedDescriptionLabel,
                okButton, cancelButton
            });

            UpdateSpeedDisplay();
        }

        private void SpeedTrackBar_Scroll(object sender, EventArgs e)
        {
            SelectedSpeed = LevelToSpeed(speedTrackBar.Value);
            UpdateSpeedDisplay();
        }

        private void UpdateSpeedDisplay()
        {
            speedValueLabel.Text = GetSpeedLevelDescription(speedTrackBar.Value);
            speedDescriptionLabel.Text = GetSpeedDetailedDescription(speedTrackBar.Value);
        }

        // Wandelt Stufe (1-6) in Millisekunden um
        private int LevelToSpeed(int level)
        {
            return level switch
            {
                1 => 180, // Sehr langsam
                2 => 150, // Langsam
                3 => 120, // Mittel
                4 => 100, // Schnell
                5 => 80,  // Sehr schnell
                6 => 60,  // Extrem schnell
                _ => 100  // Default
            };
        }

        // Wandelt Millisekunden in Stufe (1-6) um
        private int SpeedToLevel(int speed)
        {
            return speed switch
            {
                <= 60 => 6,  // Extrem schnell
                <= 80 => 5,  // Sehr schnell
                <= 100 => 4, // Schnell
                <= 120 => 3, // Mittel
                <= 150 => 2, // Langsam
                _ => 1       // Sehr langsam
            };
        }

        private string GetSpeedLevelDescription(int level)
        {
            return level switch
            {
                1 => "🐌 STUFE 1 - SEHR LANGSAM",
                2 => "👶 STUFE 2 - LANGSAM",
                3 => "🐢 STUFE 3 - MITTEL",
                4 => "⚡ STUFE 4 - SCHNELL",
                5 => "💨 STUFE 5 - SEHR SCHNELL",
                6 => "🚀 STUFE 6 - EXTREM SCHNELL",
                _ => "STUFE 3 - MITTEL"
            };
        }

        private string GetSpeedDetailedDescription(int level)
        {
            return level switch
            {
                1 => "Perfekt für Anfänger\nEntspanntes Spieltempo",
                2 => "Gut für Einsteiger\nKomfortable Geschwindigkeit",
                3 => "Ausgewogene Balance\nFür die meisten Spieler ideal",
                4 => "Herausfordernd\nFür erfahrene Spieler",
                5 => "Sehr anspruchsvoll\nNur für Profis empfohlen",
                6 => "Extrem schnell\nUltimative Herausforderung!",
                _ => "Ausgewogene Balance\nFür die meisten Spieler ideal"
            };
        }
    }

    public class HowToPlayForm : Form
    {
        public HowToPlayForm()
        {
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            // Form Settings
            this.Text = "🎮 How to Play - Snake Game";
            this.Size = new Size(700, 650);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.FromArgb(20, 25, 40);
            this.Padding = new Padding(20);

            // Title
            var titleLabel = new Label
            {
                Text = "🎮 HOW TO PLAY SNAKE",
                Font = new Font("Segoe UI", 24, FontStyle.Bold),
                ForeColor = Color.FromArgb(255, 200, 0),
                AutoSize = false,
                Size = new Size(650, 50),
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(25, 20),
                BackColor = Color.Transparent
            };
            this.Controls.Add(titleLabel);

            // Content Panel
            var contentPanel = new Panel
            {
                Size = new Size(640, 450),
                Location = new Point(30, 90),
                BackColor = Color.FromArgb(30, 35, 55),
                BorderStyle = BorderStyle.FixedSingle,
                AutoScroll = true
            };

            // Instructions Text
            var instructionsText = @"
🏆 SPIELZIEL:
• Bewege die Schlange und sammle das Futter 🍎
• Werde länger und verdiene Punkte
• Vermeide Kollisionen mit den Wänden und dir selbst!

🎮 STEUERUNG:
• PFEILTASTEN: Schlange bewegen
• W, A, S, D: Alternative Steuerung
• P: Spiel pausieren
• ESC: Zum Menü zurück

⚡ SPIELMECHANIK:
• Jedes Futter gibt +1 Punkt
• Schlange wird um 1 Segment länger
• Geschwindigkeit erhöht sich langsam
• Je länger die Schlange, desto höher der Score-Multiplikator

💡 TIPPS FÜR FORTGESCHRITTENE:
• Plane deine Route voraus
• Nutze die Wände zu deinem Vorteil
• Halte genug Platz zum Manövrieren
• Übe verschiedene Bewegungsmuster

🎯 BEWERTUNGSSYSTEM:
• Geschwindigkeit: Schneller = Mehr Punkte
• Länge: Längere Schlange = Höherer Multiplikator
• Kombiniere Geschwindigkeit und Länge für Highscores!

🌟 BESONDERE FEATURES:
• Cyber Neon Design
• Partikel-Effekte
• Animierte Schlange
• Glowing Food
• Detaillierte Highscore-Liste

Viel Spaß und werde der beste Snake-Spieler! 🐍✨";

            var instructionsLabel = new Label
            {
                Text = instructionsText,
                Font = new Font("Segoe UI", 12, FontStyle.Regular),
                ForeColor = Color.FromArgb(220, 220, 240),
                AutoSize = false,
                Size = new Size(600, 800),
                Location = new Point(20, 20),
                BackColor = Color.Transparent
            };
            contentPanel.Controls.Add(instructionsLabel);
            this.Controls.Add(contentPanel);

            // Close Button
            var closeButton = new Button
            {
                Text = "SCHLIESSEN",
                Size = new Size(200, 50),
                Location = new Point(250, 560),
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