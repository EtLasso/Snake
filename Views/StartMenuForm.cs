using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Snake.Data; // Falls du HighscoreManager hier brauchst

namespace Snake.Views
{
    public class StartMenuForm : Form
    {
        // --- Konfiguration ---
        private readonly string[] _menuItems = { "START GAME", "SETTINGS", "HIGHSCORES", "EXIT" };
        private int _selectedIndex = 0;
        private float _pulseTime = 0f;
        private System.Windows.Forms.Timer _timer; // <--- EXPLIZIT SYSTEM.WINDOWS.FORMS.TIMER

        // Maus-Interaktion
        private Rectangle[] _menuRects;
        private Point _mousePos;

        // Grafik-Ressourcen
        private Font _titleFont;
        private Font _menuFont;

        // Daten
        public int SelectedSpeed { get; private set; } = 100;
        private HighscoreManager _highscoreManager; // Optional, falls benötigt

        public StartMenuForm()
        {
            // 1. Fenster-Einstellungen (Doppelpufferung gegen Flackern)
            this.SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw, true);
            this.DoubleBuffered = true;

            this.Text = "HungrySnake - Neon Edition";
            this.Size = new Size(800, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.BackColor = Color.FromArgb(10, 10, 20); // Dark Cyber Background

            // 2. Initialisierung
            _highscoreManager = new HighscoreManager();
            _menuRects = new Rectangle[_menuItems.Length];

            // Fonts laden (Fallback auf Standard, falls Segoe UI nicht da ist)
            _titleFont = new Font("Segoe UI", 48, FontStyle.Bold);
            _menuFont = new Font("Segoe UI", 24, FontStyle.Bold);

            // 3. Animation Timer (60 FPS)
            _timer = new System.Windows.Forms.Timer { Interval = 16 }; // <--- EXPLIZIT SYSTEM.WINDOWS.FORMS.TIMER
            _timer.Tick += (s, e) => {
                _pulseTime += 0.1f;
                Invalidate(); // Erzwingt Neuzeichnen
            };
            _timer.Start();

            // 4. Event Handler
            this.KeyDown += OnKeyDown;
            this.MouseMove += OnMouseMove;
            this.MouseClick += OnMouseClick;
        }

        // --- LOGIK ---

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            // Sound-Feedback (falls SoundManager existiert)
            // Snake.Systems.SoundManager.PlayClick(); 

            switch (e.KeyCode)
            {
                case Keys.Up:
                case Keys.W:
                    _selectedIndex--;
                    if (_selectedIndex < 0) _selectedIndex = _menuItems.Length - 1;
                    break;
                case Keys.Down:
                case Keys.S:
                    _selectedIndex++;
                    if (_selectedIndex >= _menuItems.Length) _selectedIndex = 0;
                    break;
                case Keys.Enter:
                case Keys.Space:
                    ExecuteMenuAction(_selectedIndex);
                    break;
                case Keys.Escape:
                    this.DialogResult = DialogResult.Cancel;
                    this.Close();
                    break;
            }
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            _mousePos = e.Location;
            // Prüfen, ob Maus über einem Menüpunkt ist
            for (int i = 0; i < _menuRects.Length; i++)
            {
                if (_menuRects[i].Contains(e.Location))
                {
                    if (_selectedIndex != i)
                    {
                        _selectedIndex = i;
                        // Optional: Sound abspielen
                    }
                }
            }
        }

        private void OnMouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                for (int i = 0; i < _menuRects.Length; i++)
                {
                    if (_menuRects[i].Contains(e.Location))
                    {
                        ExecuteMenuAction(i);
                        return;
                    }
                }
            }
        }

        private void ExecuteMenuAction(int index)
        {
            switch (index)
            {
                case 0: // START
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                    break;

                case 1: // SETTINGS
                    ShowSettings();
                    break;

                case 2: // HIGHSCORES
                    ShowHighscores();
                    break;

                case 3: // EXIT
                    this.DialogResult = DialogResult.Cancel;
                    this.Close();
                    break;
            }
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

        private void ShowHighscores()
        {
            using (var hsForm = new HighscoresForm(_highscoreManager))
            {
                hsForm.ShowDialog(this);
            }
        }

        // --- RENDERING (Hier passiert die Magie) ---

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

            // 1. Hintergrund Animation (Gitter bewegen)
            DrawAnimatedBackground(g);

            // 2. Titel zeichnen ("HUNGRY SNAKE")
            DrawTitle(g);

            // 3. Menü-Liste zeichnen
            DrawMenu(g);
        }

        private void DrawAnimatedBackground(Graphics g)
        {
            // Leichter Farbverlauf im Hintergrund
            using (var brush = new LinearGradientBrush(ClientRectangle,
                Color.FromArgb(10, 10, 20), Color.FromArgb(20, 10, 40), 90f))
            {
                g.FillRectangle(brush, ClientRectangle);
            }

            // Gitter-Effekt (Bewegt sich leicht)
            float offset = (_pulseTime * 10) % 40;
            using (var pen = new Pen(Color.FromArgb(30, 0, 255, 200), 1))
            {
                for (int x = 0; x < Width; x += 40)
                    g.DrawLine(pen, x, 0, x, Height);

                for (float y = offset; y < Height; y += 40)
                    g.DrawLine(pen, 0, y, Width, y);
            }
        }

        private void DrawTitle(Graphics g)
        {
            string title = "HUNGRY SNAKE";
            var size = g.MeasureString(title, _titleFont);
            float x = (Width - size.Width) / 2;
            float y = 80;

            // 3D Schatten Effekt
            using (var brush = new SolidBrush(Color.FromArgb(100, 255, 0, 100)))
            {
                g.DrawString(title, _titleFont, brush, x + 4, y + 4);
            }

            // Haupttext mit Gradient
            using (var brush = new LinearGradientBrush(
                new Rectangle((int)x, (int)y, (int)size.Width, (int)size.Height),
                Color.Cyan, Color.Magenta, LinearGradientMode.Horizontal))
            {
                g.DrawString(title, _titleFont, brush, x, y);
            }
        }

        private void DrawMenu(Graphics g)
        {
            float startY = 250;
            float spacing = 70;

            for (int i = 0; i < _menuItems.Length; i++)
            {
                bool isSelected = (i == _selectedIndex);
                string text = _menuItems[i];

                // Wenn ausgewählt: Füge Pfeile hinzu und pulsiere
                if (isSelected) text = $"> {text} <";

                var size = g.MeasureString(text, _menuFont);
                float x = (Width - size.Width) / 2;
                float y = startY + (i * spacing);

                // Klickbereich speichern für Maus-Support
                _menuRects[i] = new Rectangle((int)x, (int)y, (int)size.Width, (int)size.Height);

                // Farbe berechnen
                Color color;
                if (isSelected)
                {
                    // Pulsierendes Neon
                    int alpha = 150 + (int)(Math.Sin(_pulseTime) * 100); // 50-250
                    color = Color.FromArgb(255, alpha, 255, 100); // Gelb-Grünlich
                }
                else
                {
                    color = Color.FromArgb(100, 200, 200, 200); // Grau transparent
                }

                // Glüheffekt (nur wenn selektiert)
                if (isSelected)
                {
                    using (var glowBrush = new SolidBrush(Color.FromArgb(50, color)))
                    {
                        g.FillEllipse(glowBrush, x - 20, y, size.Width + 40, size.Height);
                    }
                }

                using (var brush = new SolidBrush(color))
                {
                    g.DrawString(text, _menuFont, brush, x, y);
                }
            }

            // Kleiner Hinweis unten
            string hint = "Navigiere mit W/S oder Pfeiltasten • ENTER zum Wählen";
            using (var f = new Font("Consolas", 10))
                g.DrawString(hint, f, Brushes.Gray, 10, Height - 25);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _timer?.Stop();
                _timer?.Dispose();
                _titleFont?.Dispose();
                _menuFont?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}