using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Linq;
using System.Windows.Forms;
using Snake.Models;

namespace Snake.Views
{
    public partial class GameView : UserControl
    {
        private GameState _state;
        private readonly System.Windows.Forms.Timer _animTimer;
        private double _time = 0.0;

        // --- NEU: Screen Shake Variablen ---
        private float _shakeIntensity = 0f;
        private Random _rng = new Random();

        // --- NEU: CRT Cache ---
        private TextureBrush _scanlineBrush;
        private PathGradientBrush _vignetteBrush;
        private bool _resourcesInitialized = false;

        // Visual collections
        private readonly List<Particle> _particles = new List<Particle>();
        private readonly List<StarField> _stars = new List<StarField>();
        private readonly List<GrowthWave> _growthWaves = new List<GrowthWave>();

        // Startbild
        private Image _startBackgroundImage;
        private bool _startBackgroundLoaded = false;

        // Hauptmenü Hintergrundbild
        private Image _mainMenuBackgroundImage;
        private bool _mainMenuBackgroundLoaded = false;
        private bool _showMainMenu = false;
        private int _mainMenuIndex = 0;

        private int _lastSnakeCount = 0;
        private Point _lastFoodPos;
        private bool _justGrew = false;
        private int _growAnimationCounter = 0;

        // Schluck-Animation
        private float _swallowPhase = 0f;
        private bool _isSwallowing = false;
        private int _swallowSegmentIndex = 0;

        // FPS
        private int _frames = 0;
        private double _lastFpsTime = 0.0;
        private int _lastFps = 0;

        // Enhanced Theme colors - Cyber Neon Style
        private readonly Color BgTop = Color.FromArgb(10, 10, 20); // Etwas dunkler für CRT Kontrast
        private readonly Color BgBottom = Color.FromArgb(25, 10, 40);
        private readonly Color Accent = Color.FromArgb(0, 255, 180);
        private readonly Color AccentWarm = Color.FromArgb(255, 80, 120);
        private readonly Color FoodColor = Color.FromArgb(255, 50, 50);
        private readonly Color FoodGlowColor = Color.FromArgb(255, 200, 0);
        private readonly Color GridColor1 = Color.FromArgb(20, 20, 35);
        private readonly Color GridColor2 = Color.FromArgb(15, 15, 30);

        private float _pulsePhase = 0f;

        // Game Over Menu Fields - ERWEITERT
        private bool _showGameOverMenu = false;
        private int _gameOverScore = 0;
        private int _gameOverMenuIndex = 0;
        private List<GameState.HighScoreEntry> _topHighScores = new List<GameState.HighScoreEntry>();
        private bool _isNewHighScore = false;

        // EVENTS FÜR CONTROLLER - ERWEITERT
        public event KeyEventHandler KeyPressed;
        public event Action UpdateViewRequested;
        public event Action RestartRequested;
        public event Action ExitRequested;
        public event Action MainMenuRequested;
        public event Action HighScoresRequested;
        public event Action StartGameRequested;

        public GameView()
        {
            this.SetStyle(ControlStyles.AllPaintingInWmPaint |
                         ControlStyles.UserPaint |
                         ControlStyles.OptimizedDoubleBuffer, true);
            this.DoubleBuffered = true;
            this.UpdateStyles();

            _animTimer = new System.Windows.Forms.Timer { Interval = 16 };
            _animTimer.Tick += AnimTimer_Tick;

            // Maus-Events registrieren
            this.MouseClick += GameView_MouseClick;
            this.MouseMove += GameView_MouseMove;
            this.Resize += GameView_Resize; // Wichtig für Vignette-Update
            this.Cursor = Cursors.Default;

            InitializeStarField();
            LoadStartBackground();
            LoadMainMenuBackground();
        }

        // --- NEUE POWER-UP METHODEN ---
        private void DrawPowerUpItem(Graphics g, Rectangle boardRect)
        {
            if (_state?.PowerUpItemPosition == null) return;

            Point pos = _state.PowerUpItemPosition.Value;
            float cw = boardRect.Width / (float)_state.BoardWidth;
            float ch = boardRect.Height / (float)_state.BoardHeight;

            var center = new PointF(
                boardRect.Left + pos.X * cw + cw / 2f,
                boardRect.Top + pos.Y * ch + ch / 2f);

            float size = Math.Min(cw, ch) * 0.8f;
            float pulse = (float)(Math.Sin(_time * 10) * 0.1 + 1.0); // Schnelles Pulsieren

            Color color = _state.ItemOnBoard switch
            {
                GameState.PowerUpType.Magnet => Color.Cyan,
                GameState.PowerUpType.Ghost => Color.LightBlue,
                GameState.PowerUpType.DoubleScore => Color.Magenta,
                _ => Color.White
            };

            // Symbol zeichnen
            using (var brush = new SolidBrush(color))
            {
                // Diamant-Form für PowerUps
                var path = new GraphicsPath();
                path.AddPolygon(new PointF[] {
                    new PointF(center.X, center.Y - size/2 * pulse),
                    new PointF(center.X + size/2 * pulse, center.Y),
                    new PointF(center.X, center.Y + size/2 * pulse),
                    new PointF(center.X - size/2 * pulse, center.Y)
                });
                g.FillPath(brush, path);
                g.DrawPath(Pens.White, path);
            }

            // Buchstaben-Icon
            string icon = _state.ItemOnBoard switch
            {
                GameState.PowerUpType.Magnet => "M",
                GameState.PowerUpType.Ghost => "G",
                GameState.PowerUpType.DoubleScore => "2x",
                _ => "?"
            };

            using (var font = new Font("Arial", size / 2, FontStyle.Bold))
            using (var textBrush = new SolidBrush(Color.Black))
            {
                var textSize = g.MeasureString(icon, font);
                g.DrawString(icon, font, textBrush,
                    center.X - textSize.Width / 2,
                    center.Y - textSize.Height / 2);
            }
        }

        // --- NEU: Power-Up Anzeige im HUD ---
        private void DrawPowerUpHUD(Graphics g, Rectangle boardRect, GameState state)
        {
            if (state.CurrentPowerUp != GameState.PowerUpType.None)
            {
                string puText = $"{state.CurrentPowerUp} ({(state.PowerUpDuration / 10)}s)";
                using (var font = new Font("Segoe UI", 12, FontStyle.Bold))
                using (var brush = new SolidBrush(Color.Yellow))
                {
                    // Position rechts neben dem Score
                    g.DrawString(puText, font, brush, boardRect.Right - 150, boardRect.Top - 40);
                }
            }
        }

        // --- NEU: Initialisierung der CRT Ressourcen ---
        private void InitCRTResources()
        {
            if (_resourcesInitialized) return;

            // 1. Scanlines (Horizontale Streifen)
            var bmp = new Bitmap(1, 4); // Muster wiederholt sich alle 4 Pixel
            using (var g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.FromArgb(0, 0, 0, 0));
                // Eine halb-transparente schwarze Linie
                using (var pen = new Pen(Color.FromArgb(40, 0, 0, 0), 1))
                {
                    g.DrawLine(pen, 0, 0, 1, 0);
                }
            }
            _scanlineBrush = new TextureBrush(bmp);

            UpdateVignetteBrush();
            _resourcesInitialized = true;
        }

        private void UpdateVignetteBrush()
        {
            if (Width == 0 || Height == 0) return;

            // 2. Vignette (Dunkle Ecken)
            using (var path = new GraphicsPath())
            {
                path.AddEllipse(-50, -50, Width + 100, Height + 100);
                _vignetteBrush = new PathGradientBrush(path);
                _vignetteBrush.CenterColor = Color.Transparent;
                _vignetteBrush.SurroundColors = new Color[] { Color.FromArgb(220, 0, 0, 0) };
            }
        }

        private void GameView_Resize(object sender, EventArgs e)
        {
            UpdateVignetteBrush();
        }

        // --- NEU: Methode um Shake auszulösen (kann vom Controller gerufen werden) ---
        public void TriggerShake(float intensity)
        {
            _shakeIntensity = intensity;
        }

        private void GameView_MouseMove(object sender, MouseEventArgs e)
        {
            if (_showMainMenu)
            {
                HandleMainMenuMouseMove(e.Location);
            }
            else if (_showGameOverMenu)
            {
                HandleGameOverMouseMove(e.Location);
            }
        }

        private void GameView_MouseClick(object sender, MouseEventArgs e)
        {
            if (_showMainMenu)
            {
                HandleMainMenuMouseClick(e.Location);
            }
            else if (_showGameOverMenu)
            {
                HandleGameOverMouseClick(e.Location);
            }
        }

        private void HandleMainMenuMouseMove(Point mousePos)
        {
            int buttonWidth = 350;
            int buttonHeight = 60;
            int buttonSpacing = 20;
            int startY = Height / 2 - 80;

            for (int i = 0; i < 4; i++)
            {
                var buttonRect = new Rectangle(
                    Width / 2 - buttonWidth / 2,
                    startY + i * (buttonHeight + buttonSpacing),
                    buttonWidth,
                    buttonHeight
                );

                if (buttonRect.Contains(mousePos))
                {
                    if (_mainMenuIndex != i)
                    {
                        _mainMenuIndex = i;
                        this.Cursor = Cursors.Hand;
                        Invalidate();
                    }
                    return;
                }
            }
            this.Cursor = Cursors.Default;
        }

        private void HandleMainMenuMouseClick(Point mousePos)
        {
            int buttonWidth = 350;
            int buttonHeight = 60;
            int buttonSpacing = 20;
            int startY = Height / 2 - 80;

            for (int i = 0; i < 4; i++)
            {
                var buttonRect = new Rectangle(
                    Width / 2 - buttonWidth / 2,
                    startY + i * (buttonHeight + buttonSpacing),
                    buttonWidth,
                    buttonHeight
                );

                if (buttonRect.Contains(mousePos))
                {
                    _mainMenuIndex = i;
                    ExecuteSelectedMainMenuOption();
                    return;
                }
            }
        }

        private void HandleGameOverMouseMove(Point mousePos)
        {
            int buttonWidth = 300;
            int buttonHeight = 50;
            int buttonSpacing = 20;

            // Berechne startY basierend auf der Position der Top-Highscores
            float highscoresY = Height * 0.15f + 120 + (_isNewHighScore ? 50 : 30);
            int startY = (int)(highscoresY + 80);

            for (int i = 0; i < 4; i++)
            {
                var buttonRect = new Rectangle(
                    Width / 2 - buttonWidth / 2,
                    startY + i * (buttonHeight + buttonSpacing),
                    buttonWidth,
                    buttonHeight
                );

                if (buttonRect.Contains(mousePos))
                {
                    if (_gameOverMenuIndex != i)
                    {
                        _gameOverMenuIndex = i;
                        this.Cursor = Cursors.Hand;
                        Invalidate();
                    }
                    return;
                }
            }
            this.Cursor = Cursors.Default;
        }

        private void HandleGameOverMouseClick(Point mousePos)
        {
            int buttonWidth = 300;
            int buttonHeight = 50;
            int buttonSpacing = 20;

            // Berechne startY basierend auf der Position der Top-Highscores
            float highscoresY = Height * 0.15f + 120 + (_isNewHighScore ? 50 : 30);
            int startY = (int)(highscoresY + 80);

            for (int i = 0; i < 4; i++)
            {
                var buttonRect = new Rectangle(
                    Width / 2 - buttonWidth / 2,
                    startY + i * (buttonHeight + buttonSpacing),
                    buttonWidth,
                    buttonHeight
                );

                if (buttonRect.Contains(mousePos))
                {
                    _gameOverMenuIndex = i;
                    ExecuteSelectedMenuOption();
                    return;
                }
            }
        }

        private void LoadStartBackground()
        {
            try
            {
                string imagePath = @"C:\Users\Administrator\source\repos\EtLasso\Snake\bin\Debug\net8.0-windows\snake.png";
                if (System.IO.File.Exists(imagePath))
                {
                    _startBackgroundImage = Image.FromFile(imagePath);
                    _startBackgroundLoaded = true;
                }
            }
            catch (Exception ex)
            {
                _startBackgroundLoaded = false;
                Console.WriteLine($"Fehler beim Laden des Startbildes: {ex.Message}");
            }
        }

        private void LoadMainMenuBackground()
        {
            try
            {
                string imagePath = @"C:\Users\Administrator\source\repos\EtLasso\Snake\bin\Debug\net8.0-windows\snake.png";
                if (System.IO.File.Exists(imagePath))
                {
                    _mainMenuBackgroundImage = Image.FromFile(imagePath);
                    _mainMenuBackgroundLoaded = true;
                }
            }
            catch (Exception ex)
            {
                _mainMenuBackgroundLoaded = false;
                Console.WriteLine($"Fehler beim Laden des Hauptmenü-Hintergrundes: {ex.Message}");
            }
        }

        private void InitializeStarField()
        {
            var rand = new Random();
            for (int i = 0; i < 50; i++)
            {
                _stars.Add(new StarField
                {
                    X = (float)rand.NextDouble(),
                    Y = (float)rand.NextDouble(),
                    Speed = (float)(rand.NextDouble() * 0.3 + 0.05),
                    Brightness = (float)(rand.NextDouble() * 0.6 + 0.3),
                    Size = (float)(rand.NextDouble() * 2.0 + 0.5)
                });
            }
        }

        private void AnimTimer_Tick(object sender, EventArgs e)
        {
            AnimTick();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            _animTimer.Start();
            InitCRTResources(); // Ressourcen laden wenn Handle da ist
            Focus();
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            _animTimer.Stop();
            base.OnHandleDestroyed(e);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (_showMainMenu)
            {
                HandleMainMenuInput(e.KeyCode);
            }
            else if (_showGameOverMenu)
            {
                HandleGameOverInput(e.KeyCode);
            }
            else
            {
                KeyPressed?.Invoke(this, e);
            }
        }

        public void UpdateView(GameState state)
        {
            SetState(state);
        }

        public void SetState(GameState state)
        {
            if (_state != null)
            {
                var newCount = SafeSnakeCount(state);
                if (newCount > _lastSnakeCount)
                {
                    // SCHLANGE HAT GEFRESSEN
                    _justGrew = true;
                    _growAnimationCounter = 10;

                    _isSwallowing = true;
                    _swallowPhase = 0f;
                    _swallowSegmentIndex = 0;

                    SpawnGrowthWave();
                    SpawnFoodBurst(_lastFoodPos);
                    TriggerShake(5.0f); // Leichter Shake beim Essen
                }
            }

            _state = state;
            _lastSnakeCount = SafeSnakeCount(state);
            _lastFoodPos = state.FoodPosition;

            if (state != null && state.GameOver)
            {
                TriggerShake(15.0f); // Starker Shake bei Game Over
            }

            Invalidate();
            UpdateViewRequested?.Invoke();
        }

        public void ShowMainMenu()
        {
            _showMainMenu = true;
            _showGameOverMenu = false;
            _state = null;
            _mainMenuIndex = 0;
            if (this.IsHandleCreated)
            {
                this.Focus();
            }
            Invalidate();
        }

        public void HideMainMenu()
        {
            _showMainMenu = false;
            Invalidate();
        }

        private void HandleMainMenuInput(Keys key)
        {
            if (!_showMainMenu) return;

            switch (key)
            {
                case Keys.Enter:
                    ExecuteSelectedMainMenuOption();
                    break;

                case Keys.Escape:
                    ExitRequested?.Invoke();
                    break;

                case Keys.Up:
                case Keys.W:
                    _mainMenuIndex = Math.Max(0, _mainMenuIndex - 1);
                    Invalidate();
                    break;

                case Keys.Down:
                case Keys.S:
                    _mainMenuIndex = Math.Min(3, _mainMenuIndex + 1);
                    Invalidate();
                    break;
            }
        }

        private void ExecuteSelectedMainMenuOption()
        {
            switch (_mainMenuIndex)
            {
                case 0:
                    StartGameRequested?.Invoke();
                    _showMainMenu = false;
                    break;
                case 1:
                    HighScoresRequested?.Invoke();
                    break;
                case 2:
                    break;
                case 3:
                    ExitRequested?.Invoke();
                    break;
            }
        }

        public void ShowGameOverMenu(int score, int speed, int snakeLength, List<GameState.HighScoreEntry> topScores, bool isNewHighScore)
        {
            _gameOverScore = score;
            _topHighScores = topScores ?? new List<GameState.HighScoreEntry>();
            _isNewHighScore = isNewHighScore;
            _showGameOverMenu = true;
            _gameOverMenuIndex = 0;
            if (this.IsHandleCreated)
            {
                this.Focus();
            }
            Invalidate();
        }

        public void ShowHighScores(List<GameState.HighScoreEntry> highScores = null)
        {
            // Diese Methode wird vom Controller aufgerufen, um die Highscore-Form anzuzeigen
            // Das Event wird NICHT hier ausgelöst, sondern vom Controller behandelt
        }

        public void HideGameOverMenu()
        {
            _showGameOverMenu = false;
            Invalidate();
        }

        public void HandleGameOverInput(Keys key)
        {
            if (!_showGameOverMenu) return;

            switch (key)
            {
                case Keys.Enter:
                    ExecuteSelectedMenuOption();
                    break;

                case Keys.Escape:
                    ExitRequested?.Invoke();
                    break;

                case Keys.Up:
                case Keys.W:
                    _gameOverMenuIndex = Math.Max(0, _gameOverMenuIndex - 1);
                    Invalidate();
                    break;

                case Keys.Down:
                case Keys.S:
                    _gameOverMenuIndex = Math.Min(3, _gameOverMenuIndex + 1);
                    Invalidate();
                    break;

                case Keys.R:
                    RestartRequested?.Invoke();
                    _showGameOverMenu = false;
                    break;

                case Keys.H:
                    HighScoresRequested?.Invoke();
                    break;

                case Keys.M:
                    MainMenuRequested?.Invoke();
                    break;
            }
        }

        private void ExecuteSelectedMenuOption()
        {
            switch (_gameOverMenuIndex)
            {
                case 0:
                    RestartRequested?.Invoke();
                    _showGameOverMenu = false;
                    break;
                case 1:
                    HighScoresRequested?.Invoke();
                    break;
                case 2:
                    MainMenuRequested?.Invoke();
                    break;
                case 3:
                    ExitRequested?.Invoke();
                    break;
            }
        }

        private void AnimTick()
        {
            double dt = _animTimer.Interval / 1000.0;
            _time += dt;

            _pulsePhase = (float)(_time * 1.5);

            // Update Shake
            if (_shakeIntensity > 0)
            {
                _shakeIntensity *= 0.9f; // Shake klingt exponentiell ab
                if (_shakeIntensity < 0.5f) _shakeIntensity = 0f;
            }

            if (_growAnimationCounter > 0)
            {
                _growAnimationCounter--;
                if (_growAnimationCounter == 0)
                {
                    _justGrew = false;
                }
            }

            UpdateParticles(dt);
            UpdateStarField(dt);
            UpdateGrowthWaves(dt);
            UpdateSwallowAnimation(dt);

            _frames++;
            if (_time - _lastFpsTime >= 1.0)
            {
                _lastFps = _frames;
                _frames = 0;
                _lastFpsTime = _time;
            }

            Invalidate();
        }

        private void UpdateSwallowAnimation(double dt)
        {
            if (_isSwallowing)
            {
                _swallowPhase += (float)dt * 5f;

                if (_swallowPhase >= 1f)
                {
                    _swallowPhase = 0f;
                    _swallowSegmentIndex++;

                    if (_state != null && _swallowSegmentIndex >= _state.Snake.Count)
                    {
                        _isSwallowing = false;
                        _swallowSegmentIndex = 0;
                    }
                }
            }
        }

        private void SpawnGrowthWave()
        {
            _growthWaves.Add(new GrowthWave
            {
                Progress = 0f,
                Life = 2.0f,
                MaxLife = 2.0f,
                Intensity = 1.0f
            });
        }

        private void UpdateGrowthWaves(double dt)
        {
            for (int i = _growthWaves.Count - 1; i >= 0; i--)
            {
                var wave = _growthWaves[i];
                wave.Life -= (float)dt;
                wave.Progress += (float)dt * 0.5f;

                if (wave.Life <= 0 || wave.Progress > 1.2f)
                {
                    _growthWaves.RemoveAt(i);
                }
                else
                {
                    _growthWaves[i] = wave;
                }
            }
        }

        private void UpdateStarField(double dt)
        {
            for (int i = 0; i < _stars.Count; i++)
            {
                var star = _stars[i];
                star.Y += star.Speed * (float)dt * 0.03f;
                if (star.Y > 1.0f) star.Y = 0f;

                star.Brightness += (float)(Math.Sin(_time * 5 + i) * 0.01);
                star.Brightness = Math.Max(0.3f, Math.Min(0.9f, star.Brightness));

                _stars[i] = star;
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            // Wichtig: Kein base.OnPaint(e) um Flackern zu vermeiden, wir zeichnen alles selbst.
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            // 1. SCREEN SHAKE ANWENDEN
            // Wir verschieben den gesamten Koordinatenursprung zufällig
            if (_shakeIntensity > 0)
            {
                float dx = (float)(_rng.NextDouble() * _shakeIntensity * 2 - _shakeIntensity);
                float dy = (float)(_rng.NextDouble() * _shakeIntensity * 2 - _shakeIntensity);
                g.TranslateTransform(dx, dy);
            }

            // 2. Standard Zeichnen
            DrawAnimatedBackground(g);
            DrawStarField(g);

            if (_showMainMenu)
            {
                DrawMainMenu(g);
                DrawCRTOverlay(g); // Overlay auch im Menü
                return;
            }

            if (_state == null)
            {
                DrawStartScreen(g);
                DrawCRTOverlay(g);
                return;
            }

            // 3. SPIEL ZEICHNEN
            var boardRect = GetBoardRect();

            // Grid & Board
            DrawEnhancedGrid(g, boardRect, _state.BoardWidth, _state.BoardHeight);
            DrawBoardPlate(g, boardRect);

            // Game Elements
            DrawSnakeBody(g, boardRect, _state);
            DrawGrowthWaves(g, boardRect);
            DrawFood(g, boardRect, _state);
            DrawPowerUpItem(g, boardRect); // <- POWER-UPS ZEICHNEN
            DrawParticles(g, boardRect);
            DrawUserFriendlyHUD(g, boardRect, _state);
            DrawPowerUpHUD(g, boardRect, _state); // <- POWER-UP HUD ANZEIGE

            if (_showGameOverMenu)
            {
                DrawGameOverMenu(g);
            }

            // 3. RETRO OVERLAY (Ganz am Ende zeichnen)
            g.ResetTransform(); // Shake für das Overlay entfernen (Overlay wackelt nicht mit)
            DrawCRTOverlay(g);
        }

        // --- NEU: Die CRT Zeichen-Methoden ---
        private void DrawCRTOverlay(Graphics g)
        {
            if (!_resourcesInitialized) InitCRTResources();

            // 1. Scanlines (optional - kannst du behalten)
            g.FillRectangle(_scanlineBrush, ClientRectangle);

            // 2. RGB Split (optional)
            using (var redPen = new Pen(Color.FromArgb(40, 255, 0, 0), 2))
            using (var bluePen = new Pen(Color.FromArgb(40, 0, 0, 255), 2))
            {
                g.DrawLine(redPen, 0, 0, 0, Height);
                g.DrawLine(bluePen, Width - 2, 0, Width - 2, Height);
            }

            // 3. VIGNETTE AUSKOMMENTIERT - der komische dunkle Kreis in der Mitte verschwindet!
            // if (_vignetteBrush != null)
            // {
            //     g.FillRectangle(_vignetteBrush, ClientRectangle);
            // }

            // 4. Monitor-Glanz (optional)
            using (var glossBrush = new LinearGradientBrush(
                new Point(0, 0), new Point(200, 200),
                Color.FromArgb(30, 255, 255, 255), Color.Transparent))
            {
                g.FillEllipse(glossBrush, -50, -50, 300, 200);
            }
        }

        private void DrawMainMenu(Graphics g)
        {
            if (_mainMenuBackgroundLoaded && _mainMenuBackgroundImage != null)
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.CompositingQuality = CompositingQuality.HighQuality;

                float imageAspect = (float)_mainMenuBackgroundImage.Width / _mainMenuBackgroundImage.Height;
                float screenAspect = (float)Width / Height;

                int drawWidth, drawHeight;
                int drawX, drawY;

                if (screenAspect > imageAspect)
                {
                    drawWidth = Width;
                    drawHeight = (int)(Width / imageAspect);
                    drawX = 0;
                    drawY = (Height - drawHeight) / 2;
                }
                else
                {
                    drawHeight = Height;
                    drawWidth = (int)(Height * imageAspect);
                    drawX = (Width - drawWidth) / 2;
                    drawY = 0;
                }

                g.DrawImage(_mainMenuBackgroundImage,
                           new Rectangle(drawX, drawY, drawWidth, drawHeight),
                           new Rectangle(0, 0, _mainMenuBackgroundImage.Width, _mainMenuBackgroundImage.Height),
                           GraphicsUnit.Pixel);

                g.InterpolationMode = InterpolationMode.Default;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.PixelOffsetMode = PixelOffsetMode.Default;
                g.CompositingQuality = CompositingQuality.Default;
            }
            else
            {
                DrawAnimatedBackground(g);
                DrawStarField(g);
            }

            using (var overlay = new SolidBrush(Color.FromArgb(120, 10, 10, 20)))
            {
                g.FillRectangle(overlay, 0, 0, Width, Height);
            }

            using (var titleFont = new Font("Segoe UI", 48, FontStyle.Bold))
            using (var shadowBrush = new SolidBrush(Color.FromArgb(180, 0, 0, 0)))
            using (var titleBrush = new LinearGradientBrush(
                new Point(0, 0),
                new Point(0, 80),
                Color.FromArgb(255, 0, 255, 180),
                Color.FromArgb(255, 255, 80, 120)))
            {
                string title = "SNAKE GAME";
                var titleSize = g.MeasureString(title, titleFont);
                float titleX = (Width - titleSize.Width) / 2f;
                float titleY = 60f;

                g.DrawString(title, titleFont, shadowBrush, titleX + 3, titleY + 3);
                g.DrawString(title, titleFont, shadowBrush, titleX + 3, titleY + 3);
                g.DrawString(title, titleFont, shadowBrush, titleX + 3, titleY + 3);

                g.DrawString(title, titleFont, titleBrush, titleX, titleY);
            }

            int buttonWidth = 350;
            int buttonHeight = 60;
            int buttonSpacing = 20;
            int startY = Height / 2 - 80;

            var menuItems = new[]
            {
                "SPIEL STARTEN",
                "BESTENLISTE",
                "EINSTELLUNGEN",
                "VERLASSEN"
            };

            for (int i = 0; i < menuItems.Length; i++)
            {
                var buttonRect = new Rectangle(
                    Width / 2 - buttonWidth / 2,
                    startY + i * (buttonHeight + buttonSpacing),
                    buttonWidth,
                    buttonHeight
                );

                bool isSelected = _mainMenuIndex == i;
                Color bgColor = isSelected ? GetMainMenuButtonColor(i) : Color.FromArgb(180, 40, 45, 70);
                Color textColor = isSelected ? Color.Black : Color.White;

                using (var buttonBg = new SolidBrush(bgColor))
                using (var buttonText = new SolidBrush(textColor))
                using (var buttonFont = new Font("Segoe UI", 16, FontStyle.Bold))
                {
                    using (var path = RoundedPath(buttonRect, 12))
                    {
                        using (var shadowPath = RoundedPath(
                            new Rectangle(buttonRect.X + 2, buttonRect.Y + 2, buttonRect.Width, buttonRect.Height), 12))
                        using (var shadowBrush = new SolidBrush(Color.FromArgb(80, 0, 0, 0)))
                        {
                            g.FillPath(shadowBrush, shadowPath);
                        }

                        g.FillPath(buttonBg, path);

                        using (var borderPen = new Pen(isSelected ? Color.White : GetMainMenuButtonColor(i), 2f))
                        {
                            g.DrawPath(borderPen, path);
                        }
                    }

                    var textSize = g.MeasureString(menuItems[i], buttonFont);
                    g.DrawString(menuItems[i], buttonFont, buttonText,
                        buttonRect.X + (buttonRect.Width - textSize.Width) / 2,
                        buttonRect.Y + (buttonRect.Height - textSize.Height) / 2);
                }
            }

            using (var hintFont = new Font("Segoe UI", 12, FontStyle.Regular))
            using (var hintBrush = new SolidBrush(Color.FromArgb(220, 255, 255, 255)))
            using (var hintBgBrush = new SolidBrush(Color.FromArgb(120, 0, 0, 0)))
            {
                string hint = "Verwende ↑↓ oder W/S zum Navigieren • ENTER zum Auswählen • ESC zum Verlassen";
                var hintSize = g.MeasureString(hint, hintFont);
                float hintX = (Width - hintSize.Width) / 2f;
                float hintY = Height - 60f;

                g.FillRectangle(hintBgBrush, hintX - 10, hintY - 5, hintSize.Width + 20, hintSize.Height + 10);

                g.DrawString(hint, hintFont, hintBrush, hintX, hintY);
            }
        }

        private Color GetMainMenuButtonColor(int index)
        {
            return index switch
            {
                0 => Color.FromArgb(255, 100, 255, 180),
                1 => Color.FromArgb(255, 100, 180, 255),
                2 => Color.FromArgb(255, 255, 200, 100),
                3 => Color.FromArgb(255, 255, 100, 100),
                _ => Accent
            };
        }

        private void DrawStartScreen(Graphics g)
        {
            if (_startBackgroundLoaded && _startBackgroundImage != null)
            {
                float scale = Math.Min((float)Width / _startBackgroundImage.Width, (float)Height / _startBackgroundImage.Height);
                int scaledWidth = (int)(_startBackgroundImage.Width * scale);
                int scaledHeight = (int)(_startBackgroundImage.Height * scale);
                int x = (Width - scaledWidth) / 2;
                int y = (Height - scaledHeight) / 2;

                g.DrawImage(_startBackgroundImage, x, y, scaledWidth, scaledHeight);
            }

            using (var titleFont = new Font("Segoe UI", 32, FontStyle.Bold))
            using (var shadowBrush = new SolidBrush(Color.FromArgb(150, 0, 0, 0)))
            using (var textBrush = new SolidBrush(Color.White))
            {
                string title = "READY TO PLAY";
                var titleSize = g.MeasureString(title, titleFont);
                float titleX = (Width - titleSize.Width) / 2f;
                float titleY = 50f;

                g.DrawString(title, titleFont, shadowBrush, titleX + 3, titleY + 3);
                g.DrawString(title, titleFont, textBrush, titleX, titleY);
            }

            using (var hintFont = new Font("Segoe UI", 16, FontStyle.Regular))
            using (var hintBrush = new SolidBrush(Color.FromArgb(220, 255, 255, 255)))
            {
                string hint = "Drücke eine beliebige Taste zum Starten";
                var hintSize = g.MeasureString(hint, hintFont);
                float hintX = (Width - hintSize.Width) / 2f;
                float hintY = Height - 100f;

                g.DrawString(hint, hintFont, hintBrush, hintX, hintY);
            }
        }

        private void DrawAnimatedBackground(Graphics g)
        {
            using (var bg = new LinearGradientBrush(ClientRectangle, BgTop, BgBottom, 90f))
            {
                g.FillRectangle(bg, ClientRectangle);
            }

            float wave1 = (float)Math.Sin(_time * 0.5) * 0.1f;
            float wave2 = (float)Math.Cos(_time * 0.3) * 0.1f;

            using (var overlay = new LinearGradientBrush(
                ClientRectangle,
                Color.FromArgb(15, Accent),
                Color.FromArgb(10, AccentWarm),
                45f + wave1 * 180))
            {
                g.FillRectangle(overlay, ClientRectangle);
            }
        }

        private void DrawStarField(Graphics g)
        {
            foreach (var star in _stars)
            {
                float x = star.X * Width;
                float y = star.Y * Height;
                int alpha = (int)(star.Brightness * 220);

                using (var glowBrush = new SolidBrush(Color.FromArgb(alpha / 3, 150, 200, 255)))
                {
                    g.FillEllipse(glowBrush, x - star.Size * 2, y - star.Size * 2, star.Size * 4, star.Size * 4);
                }

                using (var brush = new SolidBrush(Color.FromArgb(alpha, 220, 240, 255)))
                {
                    g.FillEllipse(brush, x - star.Size / 2, y - star.Size / 2, star.Size, star.Size);
                }
            }
        }

        private Rectangle GetBoardRect()
        {
            int pad = 30;
            return new Rectangle(pad, pad + 80, Width - pad * 2, Height - pad * 2 - 100);
        }

        private void DrawEnhancedGrid(Graphics g, Rectangle r, int cols, int rows)
        {
            float cw = r.Width / (float)cols;
            float ch = r.Height / (float)rows;

            for (int x = 0; x < cols; x++)
            {
                for (int y = 0; y < rows; y++)
                {
                    var baseColor = (x + y) % 2 == 0 ? GridColor1 : GridColor2;
                    var cellRect = new RectangleF(
                        r.Left + x * cw,
                        r.Top + y * ch,
                        cw, ch);

                    using (var brush = new SolidBrush(baseColor))
                    {
                        g.FillRectangle(brush, cellRect);
                    }

                    using (var highlight = new SolidBrush(Color.FromArgb(20, 255, 255, 255)))
                    {
                        g.FillRectangle(highlight, cellRect.X, cellRect.Y, cellRect.Width, 2);
                        g.FillRectangle(highlight, cellRect.X, cellRect.Y, 2, cellRect.Height);
                    }

                    using (var shadow = new SolidBrush(Color.FromArgb(30, 0, 0, 0)))
                    {
                        g.FillRectangle(shadow, cellRect.X, cellRect.Bottom - 2, cellRect.Width, 2);
                        g.FillRectangle(shadow, cellRect.Right - 2, cellRect.Y, 2, cellRect.Height);
                    }

                    using (var innerGlow = new SolidBrush(Color.FromArgb(5, Accent)))
                    {
                        var innerRect = new RectangleF(
                            cellRect.X + 2, cellRect.Y + 2,
                            cellRect.Width - 4, cellRect.Height - 4);
                        g.FillRectangle(innerGlow, innerRect);
                    }
                }
            }

            using (var gridPen = new Pen(Color.FromArgb(40, Accent), 1f))
            {
                for (int x = 0; x <= cols; x++)
                {
                    float posX = r.Left + x * cw;
                    g.DrawLine(gridPen, posX, r.Top, posX, r.Bottom);
                }
                for (int y = 0; y <= rows; y++)
                {
                    float posY = r.Top + y * ch;
                    g.DrawLine(gridPen, r.Left, posY, r.Right, posY);
                }
            }
        }

        private void DrawBoardPlate(Graphics g, Rectangle r)
        {
            using (var path = RoundedPath(new Rectangle(r.X - 3, r.Y - 3, r.Width + 6, r.Height + 6), 18))
            using (var glowPen = new Pen(Color.FromArgb(80, Accent), 6f))
            {
                g.DrawPath(glowPen, path);
            }

            using (var path = RoundedPath(r, 15))
            using (var pen = new Pen(Accent, 3f))
            {
                g.DrawPath(pen, path);
            }

            using (var path = RoundedPath(new Rectangle(r.X + 2, r.Y + 2, r.Width - 4, r.Height - 4), 13))
            using (var pen = new Pen(Color.FromArgb(60, 255, 255, 255), 1f))
            {
                g.DrawPath(pen, path);
            }
        }

        private void DrawSnakeBody(Graphics g, Rectangle boardRect, GameState state)
        {
            if (state?.Snake == null || state.Snake.Count == 0) return;

            float cw = boardRect.Width / (float)state.BoardWidth;
            float ch = boardRect.Height / (float)state.BoardHeight;

            var pts = state.Snake.Select(seg => new PointF(
                boardRect.Left + seg.X * cw + cw / 2f,
                boardRect.Top + seg.Y * ch + ch / 2f)).ToList();

            if (pts.Count == 1)
            {
                DrawSnakeHead(g, pts[0], Math.Min(cw, ch) * 0.46f);
                return;
            }

            float segmentThickness = Math.Min(cw, ch) * 0.88f;
            Color bodyColor = AccentWarm;

            // --- NEU: GHOST POWER-UP VISUAL EFFECT ---
            if (state.CurrentPowerUp == GameState.PowerUpType.Ghost)
            {
                bodyColor = Color.FromArgb(200, 220, 255); // Hellblau-transparent
                // Flackern kurz vor Ende
                if (state.PowerUpDuration < 30 && (_time * 10) % 2 > 1)
                    bodyColor = AccentWarm;
            }

            // --- NEU: MAGNET POWER-UP VISUAL EFFECT ---
            if (state.CurrentPowerUp == GameState.PowerUpType.Magnet)
            {
                bodyColor = Color.Cyan;
            }

            for (int i = 1; i < pts.Count; i++)
            {
                if (i == pts.Count - 1) continue;

                var p1 = pts[i];
                var p2 = pts[i + 1];

                float radius1 = (segmentThickness / 2f) * (1.0f - (i / (float)pts.Count) * 0.3f);
                float radius2 = (segmentThickness / 2f) * (1.0f - ((i + 1) / (float)pts.Count) * 0.3f);
                float avgRadius = (radius1 + radius2) / 2f;

                using (var connectBrush = new LinearGradientBrush(p1, p2,
                    ControlPaint.Light(bodyColor, 0.3f),
                    ControlPaint.Dark(bodyColor, 0.1f)))
                using (var connectPen = new Pen(connectBrush, avgRadius * 2f))
                {
                    connectPen.StartCap = LineCap.Round;
                    connectPen.EndCap = LineCap.Round;
                    g.DrawLine(connectPen, p1, p2);
                }
            }

            for (int i = 1; i < pts.Count; i++)
            {
                var center = pts[i];
                float radius = segmentThickness / 2f;
                float sizeFactor = 1.0f - (i / (float)pts.Count) * 0.3f;
                float actualRadius = radius * sizeFactor;

                using (var glowBrush = new SolidBrush(Color.FromArgb(40, bodyColor)))
                {
                    float glowRadius = actualRadius + 8f;
                    g.FillEllipse(glowBrush,
                        center.X - glowRadius, center.Y - glowRadius,
                        glowRadius * 2, glowRadius * 2);
                }

                using (var shadowBrush = new SolidBrush(Color.FromArgb(100, 0, 0, 0)))
                {
                    g.FillEllipse(shadowBrush,
                        center.X - actualRadius + 2, center.Y - actualRadius + 2,
                        actualRadius * 2, actualRadius * 2);
                }

                using (var darkBase = new SolidBrush(ControlPaint.Dark(bodyColor, 0.3f)))
                {
                    g.FillEllipse(darkBase,
                        center.X - actualRadius, center.Y - actualRadius,
                        actualRadius * 2, actualRadius * 2);
                }

                using (var bodyBrush = new LinearGradientBrush(
                    new PointF(center.X, center.Y - actualRadius),
                    new PointF(center.X, center.Y + actualRadius),
                    ControlPaint.Light(bodyColor, 0.4f),
                    ControlPaint.Dark(bodyColor, 0.1f)))
                {
                    float mainRadius = actualRadius * 0.95f;
                    g.FillEllipse(bodyBrush,
                        center.X - mainRadius, center.Y - mainRadius,
                        mainRadius * 2, mainRadius * 2);
                }

                using (var highlightBrush = new SolidBrush(Color.FromArgb(80, 255, 255, 255)))
                {
                    float hlRadius = actualRadius * 0.4f;
                    g.FillEllipse(highlightBrush,
                        center.X - hlRadius, center.Y - actualRadius * 0.5f,
                        hlRadius * 2, hlRadius * 1.5f);
                }

                using (var specBrush = new SolidBrush(Color.FromArgb(120, 255, 255, 255)))
                {
                    float specRadius = actualRadius * 0.2f;
                    g.FillEllipse(specBrush,
                        center.X - specRadius, center.Y - actualRadius * 0.4f,
                        specRadius * 2, specRadius * 2);
                }

                using (var outlinePen = new Pen(ControlPaint.Dark(bodyColor, 0.5f), 1.5f))
                {
                    g.DrawEllipse(outlinePen,
                        center.X - actualRadius, center.Y - actualRadius,
                        actualRadius * 2, actualRadius * 2);
                }
            }

            if (state.JustAte && state.FoodDigestionProgress > 0 && pts.Count > 1)
            {
                float progressNormalized = state.FoodDigestionProgress / 100f;
                int targetSegment = Math.Min((int)(progressNormalized * (pts.Count - 1)), pts.Count - 1);

                if (targetSegment >= 0 && targetSegment < pts.Count)
                {
                    var foodCenter = pts[targetSegment];
                    float foodRadius = segmentThickness * 0.35f;

                    float pulse = 1.0f + (float)Math.Sin(_time * 12) * 0.15f;
                    float animRadius = foodRadius * pulse;

                    for (int i = 3; i >= 0; i--)
                    {
                        float glowSize = animRadius * (1.8f + i * 0.4f);
                        int alpha = 50 / (i + 1);
                        using (var glowBrush = new SolidBrush(Color.FromArgb(alpha, FoodGlowColor)))
                        {
                            g.FillEllipse(glowBrush,
                                foodCenter.X - glowSize, foodCenter.Y - glowSize,
                                glowSize * 2, glowSize * 2);
                        }
                    }

                    using (var foodBrush = new SolidBrush(FoodColor))
                    {
                        g.FillEllipse(foodBrush,
                            foodCenter.X - animRadius, foodCenter.Y - animRadius,
                            animRadius * 2, animRadius * 2);
                    }

                    using (var hlBrush = new SolidBrush(Color.FromArgb(180, 255, 255, 150)))
                    {
                        float hlSize = animRadius * 0.4f;
                        g.FillEllipse(hlBrush,
                            foodCenter.X - hlSize, foodCenter.Y - animRadius * 0.5f,
                            hlSize * 2, hlSize * 2);
                    }
                }
            }

            DrawSnakeHead(g, pts[0], Math.Min(cw, ch) * 0.46f);
        }

        private void DrawSnakeHead(Graphics g, PointF center, float baseRadius)
        {
            PointF dir = new PointF(1, 0);
            if (_state?.Snake != null && _state.Snake.Count >= 2)
            {
                var head = _state.Snake[0];
                var next = _state.Snake[1];
                float dx = (head.X - next.X);
                float dy = (head.Y - next.Y);
                Rectangle boardRect = GetBoardRect();
                float cw = boardRect.Width / (float)_state.BoardWidth;
                float ch = boardRect.Height / (float)_state.BoardHeight;
                dir = new PointF(dx * cw, dy * ch);
                var len = (float)Math.Sqrt(dir.X * dir.X + dir.Y * dir.Y);
                if (len > 0.001f) { dir.X /= len; dir.Y /= len; }
                else dir = new PointF(1, 0);
            }

            float headWidth = baseRadius * 1.85f;
            float headHeight = baseRadius * 1.85f;
            float angle = (float)(Math.Atan2(dir.Y, dir.X) * 180.0 / Math.PI);
            float breathe = 1f + 0.03f * (float)Math.Sin(_time * 6f);

            var m = g.Transform;
            g.TranslateTransform(center.X, center.Y);
            g.RotateTransform(angle);
            g.TranslateTransform(-center.X, -center.Y);

            var headRect = new RectangleF(
                center.X - headWidth / 2f * breathe,
                center.Y - headHeight / 2f * breathe,
                headWidth * breathe,
                headHeight * breathe);

            Color headStartColor = AccentWarm;
            Color headEndColor = Accent;

            // --- NEU: GHOST POWER-UP HEAD EFFECT ---
            if (_state?.CurrentPowerUp == GameState.PowerUpType.Ghost)
            {
                headStartColor = Color.FromArgb(200, 220, 255);
                headEndColor = Color.FromArgb(180, 200, 255);
            }

            // --- NEU: MAGNET POWER-UP HEAD EFFECT ---
            if (_state?.CurrentPowerUp == GameState.PowerUpType.Magnet)
            {
                headStartColor = Color.Cyan;
                headEndColor = Color.LightBlue;
            }

            for (int i = 3; i >= 0; i--)
            {
                float glowExpand = 8f + i * 4f;
                int alpha = 30 / (i + 1);
                using (var glowBrush = new SolidBrush(Color.FromArgb(alpha, headStartColor)))
                {
                    var glowRect = new RectangleF(
                        headRect.X - glowExpand,
                        headRect.Y - glowExpand,
                        headRect.Width + glowExpand * 2,
                        headRect.Height + glowExpand * 2);
                    g.FillEllipse(glowBrush, glowRect);
                }
            }

            using (var shadowBrush = new SolidBrush(Color.FromArgb(140, 0, 0, 0)))
            {
                var shadowRect = new RectangleF(
                    headRect.X + 3, headRect.Y + 3,
                    headRect.Width, headRect.Height);
                g.FillEllipse(shadowBrush, shadowRect);
            }

            using (var darkBase = new SolidBrush(ControlPaint.Dark(headStartColor, 0.35f)))
            {
                g.FillEllipse(darkBase, headRect);
            }

            using (var headBrush = new LinearGradientBrush(
                new PointF(headRect.Left, headRect.Top),
                new PointF(headRect.Left, headRect.Bottom),
                ControlPaint.Light(headStartColor, 0.45f),
                ControlPaint.Dark(headStartColor, 0.15f)))
            {
                var mainRect = new RectangleF(
                    headRect.X + 2, headRect.Y + 2,
                    headRect.Width - 4, headRect.Height - 4);
                g.FillEllipse(headBrush, mainRect);
            }

            using (var highlightBrush = new SolidBrush(Color.FromArgb(90, 255, 255, 255)))
            {
                var highlightRect = new RectangleF(
                    center.X - headWidth * 0.25f,
                    center.Y - headHeight * 0.35f,
                    headWidth * 0.5f,
                    headHeight * 0.5f);
                g.FillEllipse(highlightBrush, highlightRect);
            }

            using (var specBrush = new SolidBrush(Color.FromArgb(140, 255, 255, 255)))
            {
                var specRect = new RectangleF(
                    center.X - headWidth * 0.15f,
                    center.Y - headHeight * 0.3f,
                    headWidth * 0.25f,
                    headHeight * 0.22f);
                g.FillEllipse(specBrush, specRect);
            }

            using (var outlinePen = new Pen(ControlPaint.Dark(headStartColor, 0.45f), 2f))
            {
                g.DrawEllipse(outlinePen, headRect);
            }

            using (var rimPen = new Pen(Color.FromArgb(60, 255, 255, 255), 1.5f))
            {
                var rimRect = new RectangleF(
                    headRect.X + 3, headRect.Y + 3,
                    headRect.Width - 6, headRect.Height - 6);
                g.DrawArc(rimPen, rimRect, 180, 180);
            }

            float eyeOffsetX = headWidth * 0.22f;
            float eyeOffsetY = headHeight * 0.18f;
            float eyeSize = Math.Max(5f, headHeight * 0.28f);

            using (var eyeShadow = new SolidBrush(Color.FromArgb(40, 0, 0, 0)))
            using (var white = new SolidBrush(Color.FromArgb(250, 255, 255, 255)))
            using (var pupil = new SolidBrush(Color.FromArgb(20, 20, 30)))
            using (var gloss = new SolidBrush(Color.FromArgb(180, 255, 255, 255)))
            using (var iris = new SolidBrush(Color.FromArgb(60, 200, 255)))
            {
                var leftEyeRect = new RectangleF(
                    center.X - eyeOffsetX - eyeSize,
                    center.Y - eyeOffsetY - eyeSize,
                    eyeSize * 2, eyeSize * 2);
                g.FillEllipse(eyeShadow, leftEyeRect.X + 1, leftEyeRect.Y + 1, leftEyeRect.Width, leftEyeRect.Height);
                g.FillEllipse(white, leftEyeRect);

                g.FillEllipse(iris,
                    center.X - eyeOffsetX - eyeSize * 0.6f,
                    center.Y - eyeOffsetY - eyeSize * 0.6f,
                    eyeSize * 1.2f, eyeSize * 1.2f);

                g.FillEllipse(pupil,
                    center.X - eyeOffsetX - eyeSize * 0.4f,
                    center.Y - eyeOffsetY - eyeSize * 0.4f,
                    eyeSize * 0.8f, eyeSize * 0.8f);

                g.FillEllipse(gloss,
                    center.X - eyeOffsetX - eyeSize * 0.65f,
                    center.Y - eyeOffsetY - eyeSize * 0.7f,
                    eyeSize * 0.5f, eyeSize * 0.5f);
                g.FillEllipse(gloss,
                    center.X - eyeOffsetX - eyeSize * 0.1f,
                    center.Y - eyeOffsetY - eyeSize * 0.2f,
                    eyeSize * 0.25f, eyeSize * 0.25f);

                var rightEyeRect = new RectangleF(
                    center.X + eyeOffsetX - eyeSize,
                    center.Y - eyeOffsetY - eyeSize,
                    eyeSize * 2, eyeSize * 2);
                g.FillEllipse(eyeShadow, rightEyeRect.X + 1, rightEyeRect.Y + 1, rightEyeRect.Width, rightEyeRect.Height);
                g.FillEllipse(white, rightEyeRect);

                g.FillEllipse(iris,
                    center.X + eyeOffsetX - eyeSize * 0.6f,
                    center.Y - eyeOffsetY - eyeSize * 0.6f,
                    eyeSize * 1.2f, eyeSize * 1.2f);

                g.FillEllipse(pupil,
                    center.X + eyeOffsetX - eyeSize * 0.4f,
                    center.Y - eyeOffsetY - eyeSize * 0.4f,
                    eyeSize * 0.8f, eyeSize * 0.8f);

                g.FillEllipse(gloss,
                    center.X + eyeOffsetX - eyeSize * 0.65f,
                    center.Y - eyeOffsetY - eyeSize * 0.7f,
                    eyeSize * 0.5f, eyeSize * 0.5f);
                g.FillEllipse(gloss,
                    center.X + eyeOffsetX - eyeSize * 0.1f,
                    center.Y - eyeOffsetY - eyeSize * 0.2f,
                    eyeSize * 0.25f, eyeSize * 0.25f);
            }

            float tongueWave = (float)Math.Sin(_time * 8) * 0.05f;
            using (var tongueShadow = new SolidBrush(Color.FromArgb(100, 0, 0, 0)))
            using (var tongueBrush = new LinearGradientBrush(
                new PointF(center.X + headWidth * 0.3f, center.Y),
                new PointF(center.X + headWidth * 0.7f, center.Y),
                Color.FromArgb(255, 140, 120),
                Color.FromArgb(220, 100, 80)))
            {
                var shadowTip = new PointF(center.X + headWidth * 0.7f + 2, center.Y + 2);
                var shadowLeft = new PointF(center.X + headWidth * 0.3f + 2, center.Y - headHeight * 0.08f + 2);
                var shadowRight = new PointF(center.X + headWidth * 0.3f + 2, center.Y + headHeight * 0.08f + 2);
                g.FillPolygon(tongueShadow, new[] { shadowLeft, shadowTip, shadowRight });

                var tongueTip = new PointF(center.X + headWidth * 0.7f, center.Y + tongueWave * headHeight);
                var tongueLeft = new PointF(center.X + headWidth * 0.3f, center.Y - headHeight * 0.08f);
                var tongueRight = new PointF(center.X + headWidth * 0.3f, center.Y + headHeight * 0.08f);
                g.FillPolygon(tongueBrush, new[] { tongueLeft, tongueTip, tongueRight });

                using (var forkPen = new Pen(Color.FromArgb(220, 90, 70), 1.5f))
                {
                    g.DrawLine(forkPen,
                        tongueTip.X, tongueTip.Y,
                        tongueTip.X + headWidth * 0.08f, tongueTip.Y - headHeight * 0.06f);
                    g.DrawLine(forkPen,
                        tongueTip.X, tongueTip.Y,
                        tongueTip.X + headWidth * 0.08f, tongueTip.Y + headHeight * 0.06f);
                }
            }

            g.Transform = m;
        }

        private void DrawGrowthWaves(Graphics g, Rectangle boardRect)
        {
            // Animation disabled
        }

        private void DrawFood(Graphics g, Rectangle boardRect, GameState state)
        {
            var foodPos = state.FoodPosition;
            float cw = boardRect.Width / (float)state.BoardWidth;
            float ch = boardRect.Height / (float)state.BoardHeight;

            float pulse = (float)(Math.Sin(_pulsePhase * 4) * 0.15 + 1.0);
            float rotation = (float)(_time * 50);
            float foodSize = Math.Min(cw, ch) * 0.7f * pulse;

            var center = new PointF(
                boardRect.Left + foodPos.X * cw + cw / 2f,
                boardRect.Top + foodPos.Y * ch + ch / 2f);

            var m = g.Transform;
            g.TranslateTransform(center.X, center.Y);
            g.RotateTransform(rotation);
            g.TranslateTransform(-center.X, -center.Y);

            for (int i = 4; i >= 0; i--)
            {
                float glowSize = foodSize * (1.5f + i * 0.3f);
                int alpha = 40 / (i + 1);
                using (var glowBrush = new SolidBrush(Color.FromArgb(alpha, FoodGlowColor)))
                {
                    g.FillEllipse(glowBrush,
                        center.X - glowSize / 2, center.Y - glowSize / 2,
                        glowSize, glowSize);
                }
            }

            DrawStar(g, center, foodSize * 0.5f, 5, FoodColor, FoodGlowColor);

            g.Transform = m;
        }

        private void DrawStar(Graphics g, PointF center, float radius, int points, Color fillColor, Color outlineColor)
        {
            var starPoints = new List<PointF>();
            float angleStep = (float)(2 * Math.PI / points);
            float innerRadius = radius * 0.4f;

            for (int i = 0; i < points * 2; i++)
            {
                float angle = i * angleStep / 2 - (float)Math.PI / 2;
                float r = (i % 2 == 0) ? radius : innerRadius;
                starPoints.Add(new PointF(
                    center.X + (float)Math.Cos(angle) * r,
                    center.Y + (float)Math.Sin(angle) * r));
            }

            using (var fillBrush = new SolidBrush(fillColor))
            using (var outlinePen = new Pen(outlineColor, 3f))
            {
                g.FillPolygon(fillBrush, starPoints.ToArray());
                g.DrawPolygon(outlinePen, starPoints.ToArray());
            }

            using (var highlight = new SolidBrush(Color.FromArgb(150, 255, 255, 255)))
            {
                g.FillEllipse(highlight, center.X - radius * 0.2f, center.Y - radius * 0.2f, radius * 0.4f, radius * 0.4f);
            }
        }

        private void SpawnFoodBurst(Point cell)
        {
            if (_state == null) return;

            var random = new Random();
            for (int i = 0; i < 15; i++)
            {
                float direction = (float)(random.NextDouble() * Math.PI * 2);
                float speed = (float)(random.NextDouble() * 80 + 30);
                float life = (float)(random.NextDouble() * 0.6 + 0.3);

                _particles.Add(new Particle
                {
                    Cell = cell,
                    Pos = new PointF(0, 0),
                    Dir = new PointF((float)Math.Cos(direction) * speed,
                        (float)Math.Sin(direction) * speed),
                    Life = life,
                    MaxLife = life,
                    Color = i % 3 == 0 ? Accent : (i % 3 == 1 ? AccentWarm : FoodGlowColor),
                    Radius = (float)(random.NextDouble() * 3 + 1)
                });
            }

            while (_particles.Count > 150)
            {
                _particles.RemoveAt(0);
            }
        }

        private void UpdateParticles(double dt)
        {
            for (int i = _particles.Count - 1; i >= 0; i--)
            {
                var particle = _particles[i];
                particle.Life -= (float)dt;

                if (particle.Life <= 0)
                {
                    _particles.RemoveAt(i);
                    continue;
                }

                particle.Pos = new PointF(
                    particle.Pos.X + particle.Dir.X * (float)dt,
                    particle.Pos.Y + particle.Dir.Y * (float)dt
                );

                particle.Dir = new PointF(
                    particle.Dir.X * 0.95f,
                    particle.Dir.Y * 0.95f + 25f * (float)dt
                );

                _particles[i] = particle;
            }
        }

        private void DrawParticles(Graphics g, Rectangle boardRect)
        {
            if (_state == null) return;

            float cw = boardRect.Width / (float)_state.BoardWidth;
            float ch = boardRect.Height / (float)_state.BoardHeight;

            foreach (var particle in _particles)
            {
                var baseCenter = new PointF(
                    boardRect.Left + particle.Cell.X * cw + cw / 2f,
                    boardRect.Top + particle.Cell.Y * ch + ch / 2f
                );

                var pixelPos = new PointF(
                    baseCenter.X + particle.Pos.X * 0.01f,
                    baseCenter.Y + particle.Pos.Y * 0.01f
                );

                float lifeFactor = particle.Life / particle.MaxLife;
                float radius = particle.Radius * lifeFactor;

                using (var glow = new SolidBrush(Color.FromArgb((int)(lifeFactor * 80), particle.Color)))
                {
                    g.FillEllipse(glow, pixelPos.X - radius * 2, pixelPos.Y - radius * 2, radius * 4, radius * 4);
                }

                using (var brush = new SolidBrush(Color.FromArgb((int)(lifeFactor * 250), particle.Color)))
                {
                    g.FillEllipse(brush, pixelPos.X - radius, pixelPos.Y - radius, radius * 2, radius * 2);
                }
            }
        }

        private void DrawUserFriendlyHUD(Graphics g, Rectangle boardRect, GameState state)
        {
            var hudRect = new Rectangle(boardRect.Left, 20, boardRect.Width, 60);

            using (var path = RoundedPath(hudRect, 15))
            {
                using (var bgBrush = new SolidBrush(Color.FromArgb(220, 20, 25, 40)))
                {
                    g.FillPath(bgBrush, path);
                }

                using (var pen = new Pen(Accent, 3f))
                {
                    g.DrawPath(pen, path);
                }
            }

            using (var scoreFont = new Font("Segoe UI", 24, FontStyle.Bold))
            using (var scoreBrush = new SolidBrush(Accent))
            using (var shadowBrush = new SolidBrush(Color.FromArgb(150, 0, 0, 0)))
            {
                string scoreText = $"{state.Score} PUNKTE";
                var scoreSize = g.MeasureString(scoreText, scoreFont);
                float scoreX = hudRect.Left + (hudRect.Width - scoreSize.Width) / 2;
                float scoreY = hudRect.Top + (hudRect.Height - scoreSize.Height) / 2;

                g.DrawString(scoreText, scoreFont, shadowBrush, scoreX + 2, scoreY + 2);
                g.DrawString(scoreText, scoreFont, scoreBrush, scoreX, scoreY);
            }

            using (var lengthFont = new Font("Segoe UI", 14, FontStyle.Bold))
            using (var lengthBrush = new SolidBrush(AccentWarm))
            {
                string lengthText = $"LÄNGE: {state.Snake.Count}";
                float lengthX = hudRect.Left + 20;
                float lengthY = hudRect.Top + (hudRect.Height - lengthFont.Height) / 2;

                g.DrawString(lengthText, lengthFont, lengthBrush, lengthX, lengthY);
            }

            using (var speedFont = new Font("Segoe UI", 14, FontStyle.Bold))
            using (var speedBrush = new SolidBrush(Color.FromArgb(255, 200, 100)))
            {
                string speedLevel = GetSpeedLevelDescription(state.CurrentSpeed);
                string speedText = $"GESCHWINDIGKEIT: {speedLevel}";
                var speedSize = g.MeasureString(speedText, speedFont);
                float speedX = hudRect.Right - speedSize.Width - 20;
                float speedY = hudRect.Top + (hudRect.Height - speedFont.Height) / 2;

                g.DrawString(speedText, speedFont, speedBrush, speedX, speedY);
            }

            DrawMultiplierDisplay(g, boardRect, state);
        }

        private string GetSpeedLevelDescription(int speed)
        {
            return speed switch
            {
                <= 60 => "EXTREM",
                <= 80 => "SEHR SCHNELL",
                <= 100 => "SCHNELL",
                <= 120 => "MITTEL",
                <= 150 => "LANGSAM",
                _ => "SEHR LANGSAM"
            };
        }

        private void DrawMultiplierDisplay(Graphics g, Rectangle boardRect, GameState state)
        {
            float speedMultiplier = GetSpeedMultiplier(state.CurrentSpeed);
            float lengthBonus = 1.0f + (state.Snake.Count / 10.0f) * 0.5f;
            float totalMultiplier = speedMultiplier * lengthBonus;

            var multiRect = new Rectangle(
                boardRect.Left,
                boardRect.Bottom + 10,
                boardRect.Width,
                30);

            using (var multiFont = new Font("Segoe UI", 12, FontStyle.Bold))
            using (var multiBrush = new SolidBrush(Color.FromArgb(255, 200, 255, 100)))
            using (var bgBrush = new SolidBrush(Color.FromArgb(150, 30, 35, 55)))
            {
                string multiText = $"MULTIPLIKATOR: x{totalMultiplier:F1} (Geschwindigkeit: x{speedMultiplier:F1} + Länge: x{lengthBonus:F1})";
                var multiSize = g.MeasureString(multiText, multiFont);
                float multiX = multiRect.Left + (multiRect.Width - multiSize.Width) / 2;

                g.FillRectangle(bgBrush, multiX - 10, multiRect.Top, multiSize.Width + 20, multiRect.Height);
                g.DrawString(multiText, multiFont, multiBrush, multiX, multiRect.Top + 5);
            }
        }

        private float GetSpeedMultiplier(int speed)
        {
            return speed switch
            {
                <= 60 => 3.0f,
                <= 80 => 2.5f,
                <= 100 => 2.0f,
                <= 120 => 1.5f,
                <= 150 => 1.0f,
                _ => 0.75f
            };
        }

        private void DrawGameOverMenu(Graphics g)
        {
            var overlay = new Rectangle(0, 0, Width, Height);
            using (var bg = new SolidBrush(Color.FromArgb(220, 10, 10, 20)))
            {
                g.FillRectangle(bg, overlay);
            }

            using (var titleFont = new Font("Segoe UI", 32, FontStyle.Bold))
            using (var scoreFont = new Font("Segoe UI", 20, FontStyle.Bold))
            using (var highscoreFont = new Font("Segoe UI", 16, FontStyle.Bold))
            using (var hintFont = new Font("Segoe UI", 14, FontStyle.Regular))
            using (var buttonFont = new Font("Segoe UI", 16, FontStyle.Bold))
            using (var highscoreSmallFont = new Font("Segoe UI", 10, FontStyle.Regular))
            {
                string title = "GAME OVER";
                var titleSize = g.MeasureString(title, titleFont);
                float titleX = (Width - titleSize.Width) / 2f;
                float titleY = Height * 0.15f;

                using (var titleBrush = new SolidBrush(AccentWarm))
                {
                    g.DrawString(title, titleFont, titleBrush, titleX, titleY);
                }

                string scoreText = $"ERREICHTE PUNKTE: {_gameOverScore}";
                var scoreSize = g.MeasureString(scoreText, scoreFont);
                float scoreY = titleY + titleSize.Height + 20;

                using (var scoreBrush = new SolidBrush(Accent))
                {
                    g.DrawString(scoreText, scoreFont, scoreBrush, (Width - scoreSize.Width) / 2f, scoreY);
                }

                if (_isNewHighScore)
                {
                    string highscoreText = "NEUER BESTWERT!";
                    var highscoreSize = g.MeasureString(highscoreText, highscoreFont);
                    using (var highscoreBrush = new SolidBrush(Color.Gold))
                    {
                        g.DrawString(highscoreText, highscoreFont, highscoreBrush,
                            (Width - highscoreSize.Width) / 2f, scoreY + scoreSize.Height + 10);
                    }
                }

                float highscoresY = scoreY + scoreSize.Height + (_isNewHighScore ? 50 : 30);
                DrawTopHighScores(g, highscoresY, highscoreSmallFont);

                int buttonWidth = 300;
                int buttonHeight = 50;
                int buttonSpacing = 20;
                int startY = (int)(highscoresY + 80);

                var buttons = new[]
                {
                    new { Text = "NEUES SPIEL STARTEN", Index = 0 },
                    new { Text = "BESTENLISTE ANZEIGEN", Index = 1 },
                    new { Text = "ZUM HAUPTMENÜ", Index = 2 },
                    new { Text = "SPIEL VERLASSEN", Index = 3 }
                };

                for (int i = 0; i < buttons.Length; i++)
                {
                    var button = new Rectangle(
                        Width / 2 - buttonWidth / 2,
                        startY + i * (buttonHeight + buttonSpacing),
                        buttonWidth,
                        buttonHeight
                    );

                    bool isSelected = _gameOverMenuIndex == i;
                    Color bgColor = isSelected ? GetButtonColor(i) : Color.FromArgb(200, 60, 65, 85);
                    Color textColor = isSelected ? Color.Black : Color.White;

                    using (var buttonBg = new SolidBrush(bgColor))
                    using (var buttonText = new SolidBrush(textColor))
                    {
                        g.FillRectangle(buttonBg, button);

                        using (var borderPen = new Pen(GetButtonColor(i), 2f))
                        {
                            g.DrawRectangle(borderPen, button);
                        }

                        var buttonTextSize = g.MeasureString(buttons[i].Text, buttonFont);
                        g.DrawString(buttons[i].Text, buttonFont, buttonText,
                            button.X + (button.Width - buttonTextSize.Width) / 2,
                            button.Y + (button.Height - buttonTextSize.Height) / 2);
                    }
                }

                string hint = "Verwende ↑↓ oder W/S zum Navigieren • ENTER zum Auswählen";
                var hintSize = g.MeasureString(hint, hintFont);
                using (var hintBrush = new SolidBrush(Color.FromArgb(240, 255, 255, 255)))
                {
                    g.DrawString(hint, hintFont, hintBrush,
                        (Width - hintSize.Width) / 2f,
                        startY + buttons.Length * (buttonHeight + buttonSpacing) + 30);
                }
            }
        }

        private Color GetButtonColor(int index)
        {
            return index switch
            {
                0 => Color.FromArgb(255, 100, 255, 180),
                1 => Color.FromArgb(255, 100, 180, 255),
                2 => Color.FromArgb(255, 255, 200, 100),
                3 => Color.FromArgb(255, 255, 100, 100),
                _ => Accent
            };
        }

        private void DrawTopHighScores(Graphics g, float startY, Font font)
        {
            if (_topHighScores.Count == 0) return;

            using (var titleFont = new Font(font.FontFamily, 12, FontStyle.Bold))
            using (var titleBrush = new SolidBrush(Color.FromArgb(255, 200, 200, 255)))
            {
                string title = "TOP 3 BESTWERTE:";
                var titleSize = g.MeasureString(title, titleFont);
                g.DrawString(title, titleFont, titleBrush, (Width - titleSize.Width) / 2f, startY);
            }

            float y = startY + 25;
            for (int i = 0; i < Math.Min(3, _topHighScores.Count); i++)
            {
                var entry = _topHighScores[i];
                string scoreText = $"{i + 1}. {entry.PlayerName} - {entry.Score} Punkte";

                Color textColor = i switch
                {
                    0 => Color.Gold,
                    1 => Color.Silver,
                    2 => Color.FromArgb(255, 205, 127),
                    _ => Color.White
                };

                using (var scoreBrush = new SolidBrush(textColor))
                {
                    var textSize = g.MeasureString(scoreText, font);
                    g.DrawString(scoreText, font, scoreBrush, (Width - textSize.Width) / 2f, y);
                }

                y += 18;
            }
        }

        private GraphicsPath RoundedPath(Rectangle r, int radius)
        {
            int diameter = radius * 2;
            var path = new GraphicsPath();
            path.AddArc(r.Left, r.Top, diameter, diameter, 180, 90);
            path.AddArc(r.Right - diameter, r.Top, diameter, diameter, 270, 90);
            path.AddArc(r.Right - diameter, r.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(r.Left, r.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }

        private int SafeSnakeCount(GameState state)
        {
            return state.Snake == null ? 0 : state.Snake.Count;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _animTimer?.Stop();
                _animTimer?.Dispose();
                _startBackgroundImage?.Dispose();
                _mainMenuBackgroundImage?.Dispose();
                _scanlineBrush?.Dispose();
                _vignetteBrush?.Dispose();
            }
            base.Dispose(disposing);
        }

        private struct Particle
        {
            public Point Cell;
            public PointF Pos;
            public PointF Dir;
            public float Life;
            public float MaxLife;
            public Color Color;
            public float Radius;
        }

        private struct StarField
        {
            public float X;
            public float Y;
            public float Speed;
            public float Brightness;
            public float Size;
        }

        private struct GrowthWave
        {
            public float Progress;
            public float Life;
            public float MaxLife;
            public float Intensity;
        }
    }
}