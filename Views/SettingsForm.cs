using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Snake.Data;

namespace Snake.Views
{
    public class SettingsForm : Form
    {
        public int SelectedSpeed { get; set; } = 100;

        private float _pulseTime = 0f;
        private System.Windows.Forms.Timer _timer;
        private Font _titleFont;
        private Font _valueFont;

        // Mögliche Geschwindigkeiten (kleiner = schneller)
        private int[] _speeds = { 150, 120, 100, 80, 60, 40 };
        private string[] _speedLabels = { "SNAIL", "SLOW", "NORMAL", "FAST", "HYPER", "GODLIKE" };
        private int _currentSpeedIndex = 2; // Start bei NORMAL (100ms)

        public SettingsForm(HighscoreManager manager)
        {
            // Setup wie beim StartMenu
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            this.DoubleBuffered = true;
            this.Text = "Settings";
            this.Size = new Size(600, 400);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.FromArgb(10, 10, 20);

            _titleFont = new Font("Segoe UI", 36, FontStyle.Bold);
            _valueFont = new Font("Segoe UI", 24, FontStyle.Bold);

            _timer = new System.Windows.Forms.Timer { Interval = 16 };
            _timer.Tick += (s, e) => { _pulseTime += 0.1f; Invalidate(); };
            _timer.Start();

            this.KeyDown += OnKeyDown;
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            // Versuche, den aktuellen Index basierend auf der übergebenen SelectedSpeed zu finden
            for (int i = 0; i < _speeds.Length; i++)
            {
                if (_speeds[i] == SelectedSpeed)
                {
                    _currentSpeedIndex = i;
                    break;
                }
            }
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            // Snake.Systems.SoundManager.PlayClick(); // Optional Sound

            switch (e.KeyCode)
            {
                case Keys.Left:
                case Keys.A:
                    _currentSpeedIndex--;
                    if (_currentSpeedIndex < 0) _currentSpeedIndex = 0;
                    break;

                case Keys.Right:
                case Keys.D:
                    _currentSpeedIndex++;
                    if (_currentSpeedIndex >= _speeds.Length) _currentSpeedIndex = _speeds.Length - 1;
                    break;

                case Keys.Enter:
                case Keys.Space:
                    // Speichern und schließen
                    SelectedSpeed = _speeds[_currentSpeedIndex];
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                    break;

                case Keys.Escape:
                    this.DialogResult = DialogResult.Cancel;
                    this.Close();
                    break;
            }
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

            // 1. Hintergrund
            using (var brush = new LinearGradientBrush(ClientRectangle, Color.FromArgb(10, 10, 20), Color.FromArgb(20, 10, 40), 90f))
            {
                g.FillRectangle(brush, ClientRectangle);
            }

            // 2. Titel
            string title = "GAME SPEED";
            var titleSize = g.MeasureString(title, _titleFont);
            float x = (Width - titleSize.Width) / 2;

            using (var brush = new SolidBrush(Color.Cyan))
            {
                g.DrawString(title, _titleFont, brush, x, 40);
            }

            // 3. Wert Anzeige (< WERT >)
            string label = _speedLabels[_currentSpeedIndex];
            string display = $"<  {label}  >";
            var valSize = g.MeasureString(display, _valueFont);
            float valX = (Width - valSize.Width) / 2;
            float valY = 160;

            // Farbe je nach Geschwindigkeit (Grün -> Rot)
            Color valColor = _currentSpeedIndex < 2 ? Color.Lime : (_currentSpeedIndex < 4 ? Color.Yellow : Color.Red);

            // Pulsieren
            int alpha = 150 + (int)(Math.Sin(_pulseTime * 0.5) * 100);
            using (var glow = new SolidBrush(Color.FromArgb(50, valColor)))
            {
                g.FillEllipse(glow, valX - 20, valY, valSize.Width + 40, valSize.Height);
            }

            using (var brush = new SolidBrush(valColor))
            {
                g.DrawString(display, _valueFont, brush, valX, valY);
            }

            // 4. Anleitung
            string hint = "Links/Rechts wählen • ENTER bestätigen • ESC abbrechen";
            using (var f = new Font("Consolas", 10))
            {
                var hintSize = g.MeasureString(hint, f);
                g.DrawString(hint, f, Brushes.Gray, (Width - hintSize.Width) / 2, Height - 40);
            }
        }
    }
}