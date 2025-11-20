using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Snake.Data;

namespace Snake.Views
{
    public class StartMenuForm : Form
    {
        // --- Konfiguration ---
        // Wir lassen den Text für Index 1 leer, da er dynamisch generiert wird
        private readonly string[] _baseMenuItems = { "START GAME", "SPEED", "HIGHSCORES", "EXIT" };
        private int _selectedIndex = 0;

        // Speed Einstellungen direkt hier
        private int[] _speeds = { 150, 120, 100, 80, 60, 40 };
        private string[] _speedLabels = { "SNAIL", "SLOW", "NORMAL", "FAST", "HYPER", "GODLIKE" };
        private int _currentSpeedIndex = 2; // Standard: Normal (100ms)

        // Öffentliche Property für Program.cs
        public int SelectedSpeed => _speeds[_currentSpeedIndex];

        // Animation & Grafik
        private float _pulseTime = 0f;
        private System.Windows.Forms.Timer _timer;
        private Rectangle[] _menuRects;
        private Font _titleFont;
        private Font _menuFont;
        private HighscoreManager _highscoreManager;

        public StartMenuForm()
        {
            // 1. Fenster-Setup
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            this.DoubleBuffered = true;
            this.Text = "HungrySnake - Neon Edition";
            this.Size = new Size(800, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.BackColor = Color.FromArgb(10, 10, 20);

            // 2. Init
            _highscoreManager = new HighscoreManager();
            _menuRects = new Rectangle[_baseMenuItems.Length];
            _titleFont = new Font("Segoe UI", 48, FontStyle.Bold);
            _menuFont = new Font("Segoe UI", 24, FontStyle.Bold);

            // 3. Timer
            _timer = new System.Windows.Forms.Timer { Interval = 16 };
            _timer.Tick += (s, e) => { _pulseTime += 0.1f; Invalidate(); };
            _timer.Start();

            // 4. Events
            this.KeyDown += OnKeyDown;
            this.MouseMove += OnMouseMove;
            this.MouseClick += OnMouseClick;
        }

        // --- LOGIK ---

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            // Snake.Systems.SoundManager.PlayClick(); // Sound hier einfügen wenn gewünscht

            switch (e.KeyCode)
            {
                case Keys.Up:
                case Keys.W:
                    _selectedIndex--;
                    if (_selectedIndex < 0) _selectedIndex = _baseMenuItems.Length - 1;
                    break;

                case Keys.Down:
                case Keys.S:
                    _selectedIndex++;
                    if (_selectedIndex >= _baseMenuItems.Length) _selectedIndex = 0;
                    break;

                // Links/Rechts ändert Speed, WENN wir auf dem Speed-Eintrag (Index 1) sind
                case Keys.Left:
                case Keys.A:
                    if (_selectedIndex == 1) ChangeSpeed(-1);
                    break;

                case Keys.Right:
                case Keys.D:
                    if (_selectedIndex == 1) ChangeSpeed(1);
                    break;

                case Keys.Enter:
                case Keys.Space:
                    if (_selectedIndex == 1) ChangeSpeed(1); // Enter cycle auch durch Speed
                    else ExecuteMenuAction(_selectedIndex);
                    break;

                case Keys.Escape:
                    this.DialogResult = DialogResult.Cancel;
                    this.Close();
                    break;
            }
            Invalidate();
        }

        private void ChangeSpeed(int direction)
        {
            _currentSpeedIndex += direction;

            // Loop durch die Liste
            if (_currentSpeedIndex < 0) _currentSpeedIndex = _speeds.Length - 1;
            if (_currentSpeedIndex >= _speeds.Length) _currentSpeedIndex = 0;

            // Optional: Anderen Sound für Einstellung
            // Snake.Systems.SoundManager.PlayClick();
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            for (int i = 0; i < _menuRects.Length; i++)
            {
                if (_menuRects[i].Contains(e.Location))
                {
                    if (_selectedIndex != i)
                    {
                        _selectedIndex = i;
                        // SoundManager.PlayHover();
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
                        // Wenn auf Speed geklickt wird -> ändern, sonst ausführen
                        if (i == 1) ChangeSpeed(1);
                        else ExecuteMenuAction(i);
                        return;
                    }
                }
            }
            // Rechtsklick für Speed zurück
            else if (e.Button == MouseButtons.Right && _selectedIndex == 1)
            {
                ChangeSpeed(-1);
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

                case 1: // SPEED (wird direkt geändert, hier passiert nichts)
                    break;

                case 2: // HIGHSCORES
                    using (var hsForm = new HighscoresForm(_highscoreManager))
                    {
                        hsForm.ShowDialog(this);
                    }
                    break;

                case 3: // EXIT
                    this.DialogResult = DialogResult.Cancel;
                    this.Close();
                    break;
            }
        }

        // --- RENDERING ---

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

            DrawAnimatedBackground(g);
            DrawTitle(g);
            DrawMenu(g);
        }

        private void DrawAnimatedBackground(Graphics g)
        {
            using (var brush = new LinearGradientBrush(ClientRectangle,
                Color.FromArgb(10, 10, 20), Color.FromArgb(20, 10, 40), 90f))
            {
                g.FillRectangle(brush, ClientRectangle);
            }

            float offset = (_pulseTime * 10) % 40;
            using (var pen = new Pen(Color.FromArgb(20, 0, 255, 200), 1))
            {
                for (int x = 0; x < Width; x += 40) g.DrawLine(pen, x, 0, x, Height);
                for (float y = offset; y < Height; y += 40) g.DrawLine(pen, 0, y, Width, y);
            }
        }

        private void DrawTitle(Graphics g)
        {
            string title = "HUNGRY SNAKE";
            var size = g.MeasureString(title, _titleFont);
            float x = (Width - size.Width) / 2;
            float y = 80;

            // Neon Glow
            using (var brush = new SolidBrush(Color.FromArgb(50, 0, 255, 255)))
                g.DrawString(title, _titleFont, brush, x + 4, y + 4);

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

            for (int i = 0; i < _baseMenuItems.Length; i++)
            {
                bool isSelected = (i == _selectedIndex);
                string text = _baseMenuItems[i];

                // SPEZIALFALL: Speed Anzeige generieren
                if (i == 1)
                {
                    string speedLabel = _speedLabels[_currentSpeedIndex];
                    // Wenn ausgewählt, Pfeile anzeigen: < NORMAL >
                    if (isSelected) text = $"< SPEED: {speedLabel} >";
                    else text = $"SPEED: {speedLabel}";
                }
                else if (isSelected)
                {
                    text = $"> {text} <";
                }

                var size = g.MeasureString(text, _menuFont);
                float x = (Width - size.Width) / 2;
                float y = startY + (i * spacing);

                _menuRects[i] = new Rectangle((int)x, (int)y, (int)size.Width, (int)size.Height);

                Color color = isSelected
                    ? Color.FromArgb(255, 150 + (int)(Math.Sin(_pulseTime) * 100), 255, 100)
                    : Color.FromArgb(100, 200, 200, 200);

                // Bei Speed eine spezielle Farbe je nach Schwierigkeit
                if (i == 1 && isSelected)
                {
                    color = _currentSpeedIndex < 2 ? Color.Lime : (_currentSpeedIndex < 4 ? Color.Yellow : Color.Red);
                }

                if (isSelected)
                {
                    using (var glow = new SolidBrush(Color.FromArgb(30, color)))
                        g.FillEllipse(glow, x - 20, y, size.Width + 40, size.Height);
                }

                using (var brush = new SolidBrush(color))
                {
                    g.DrawString(text, _menuFont, brush, x, y);
                }
            }

            // Hinweis aktualisiert für Speed-Steuerung
            string hint = "W/S: Navigieren • A/D: Speed ändern • ENTER: Wählen";
            using (var f = new Font("Consolas", 10))
                g.DrawString(hint, f, Brushes.Gray, (Width - g.MeasureString(hint, f).Width) / 2, Height - 30);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _timer?.Stop(); _timer?.Dispose();
                _titleFont?.Dispose(); _menuFont?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}