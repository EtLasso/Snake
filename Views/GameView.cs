using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
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

        // Visual collections
        private readonly List<Particle> _particles = new List<Particle>();
        private readonly List<StarField> _stars = new List<StarField>();
        private readonly List<GrowthWave> _growthWaves = new List<GrowthWave>();

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
        private readonly Color BgTop = Color.FromArgb(15, 15, 35);
        private readonly Color BgBottom = Color.FromArgb(35, 15, 55);
        private readonly Color Accent = Color.FromArgb(0, 255, 180);
        private readonly Color AccentWarm = Color.FromArgb(255, 80, 120);
        private readonly Color FoodColor = Color.FromArgb(255, 50, 50);
        private readonly Color FoodGlowColor = Color.FromArgb(255, 200, 0);
        private readonly Color GridColor1 = Color.FromArgb(30, 30, 50);
        private readonly Color GridColor2 = Color.FromArgb(25, 25, 45);

        private float _pulsePhase = 0f;

        // Game Over Menu Fields
        private bool _showGameOverMenu = false;
        private int _gameOverScore = 0;
        private int _gameOverMenuIndex = 0;

        public event KeyEventHandler KeyPressed;
        public event Action UpdateViewRequested;
        public event Action RestartRequested;
        public event Action ExitRequested;

        public GameView()
        {
            this.SetStyle(ControlStyles.AllPaintingInWmPaint |
                         ControlStyles.UserPaint |
                         ControlStyles.OptimizedDoubleBuffer, true);
            this.DoubleBuffered = true;
            this.UpdateStyles();

            _animTimer = new System.Windows.Forms.Timer { Interval = 16 };
            _animTimer.Tick += AnimTimer_Tick;

            InitializeStarField();
        }

        private void InitializeStarField()
        {
            var rand = new Random();
            for (int i = 0; i < 50; i++) // More stars!
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
            KeyPressed?.Invoke(this, e);
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
                    _justGrew = true;
                    _growAnimationCounter = 10;

                    _isSwallowing = true;
                    _swallowPhase = 0f;
                    _swallowSegmentIndex = 0;

                    SpawnGrowthWave();
                    SpawnFoodBurst(_lastFoodPos);
                }
            }

            _state = state;
            _lastSnakeCount = SafeSnakeCount(state);
            _lastFoodPos = state.FoodPosition;

            Invalidate();
            UpdateViewRequested?.Invoke();
        }

        public void ShowGameOverMenu(int score)
        {
            _gameOverScore = score;
            _showGameOverMenu = true;
            _gameOverMenuIndex = 0;
            if (this.IsHandleCreated)
            {
                this.Focus();
            }
            Invalidate();
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
                case Keys.R:
                case Keys.Enter:
                    RestartRequested?.Invoke();
                    _showGameOverMenu = false;
                    break;

                case Keys.Escape:
                case Keys.Q:
                    ExitRequested?.Invoke();
                    break;

                case Keys.Up:
                case Keys.W:
                    _gameOverMenuIndex = Math.Max(0, _gameOverMenuIndex - 1);
                    Invalidate();
                    break;
                case Keys.Down:
                case Keys.S:
                    _gameOverMenuIndex = _gameOverMenuIndex + 1;
                    Invalidate();
                    break;
            }
        }

        private void AnimTick()
        {
            double dt = _animTimer.Interval / 1000.0;
            _time += dt;

            _pulsePhase = (float)(_time * 1.5);

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
                
                // Twinkle effect
                star.Brightness += (float)(Math.Sin(_time * 5 + i) * 0.01);
                star.Brightness = Math.Max(0.3f, Math.Min(0.9f, star.Brightness));
                
                _stars[i] = star;
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            DrawAnimatedBackground(g);
            DrawStarField(g);

            if (_state == null)
            {
                DrawEmpty(g);
                return;
            }

            var boardRect = GetBoardRect();

            DrawEnhancedGrid(g, boardRect, _state.BoardWidth, _state.BoardHeight);
            DrawBoardPlate(g, boardRect);

            DrawSnakeBody(g, boardRect, _state);
            DrawGrowthWaves(g, boardRect);

            DrawFood(g, boardRect, _state);
            DrawParticles(g, boardRect);
            DrawEnhancedHUD(g, boardRect, _state);

            if (_showGameOverMenu)
            {
                DrawGameOverMenu(g);
            }
        }

        private void DrawAnimatedBackground(Graphics g)
        {
            // Multi-layer animated background
            using (var bg = new LinearGradientBrush(ClientRectangle, BgTop, BgBottom, 90f))
            {
                g.FillRectangle(bg, ClientRectangle);
            }

            // Animated gradient overlay
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

                // Star glow
                using (var glowBrush = new SolidBrush(Color.FromArgb(alpha / 3, 150, 200, 255)))
                {
                    g.FillEllipse(glowBrush, x - star.Size * 2, y - star.Size * 2, star.Size * 4, star.Size * 4);
                }

                // Star core
                using (var brush = new SolidBrush(Color.FromArgb(alpha, 220, 240, 255)))
                {
                    g.FillEllipse(brush, x - star.Size / 2, y - star.Size / 2, star.Size, star.Size);
                }
            }
        }

        private void DrawEmpty(Graphics g)
        {
            using (var f = new Font("Segoe UI", 24, FontStyle.Bold))
            using (var glow = new SolidBrush(Color.FromArgb(100, Accent)))
            using (var brush = new SolidBrush(Accent))
            {
                const string s = "?? READY TO PLAY";
                var sz = g.MeasureString(s, f);
                float x = (Width - sz.Width) / 2f;
                float y = (Height - sz.Height) / 2f;
                
                // Glow effect
                g.DrawString(s, f, glow, x + 2, y + 2);
                g.DrawString(s, f, brush, x, y);
            }
        }

        private Rectangle GetBoardRect()
        {
            int pad = 30;
            return new Rectangle(pad, pad + 60, Width - pad * 2, Height - pad * 2 - 60);
        }

        private void DrawEnhancedGrid(Graphics g, Rectangle r, int cols, int rows)
        {
            float cw = r.Width / (float)cols;
            float ch = r.Height / (float)rows;

            // Draw 3D cells with shadows and highlights
            for (int x = 0; x < cols; x++)
            {
                for (int y = 0; y < rows; y++)
                {
                    var baseColor = (x + y) % 2 == 0 ? GridColor1 : GridColor2;
                    var cellRect = new RectangleF(
                        r.Left + x * cw,
                        r.Top + y * ch,
                        cw, ch);

                    // Cell base
                    using (var brush = new SolidBrush(baseColor))
                    {
                        g.FillRectangle(brush, cellRect);
                    }

                    // 3D highlight (top-left)
                    using (var highlight = new SolidBrush(Color.FromArgb(20, 255, 255, 255)))
                    {
                        g.FillRectangle(highlight, cellRect.X, cellRect.Y, cellRect.Width, 2);
                        g.FillRectangle(highlight, cellRect.X, cellRect.Y, 2, cellRect.Height);
                    }

                    // 3D shadow (bottom-right)
                    using (var shadow = new SolidBrush(Color.FromArgb(30, 0, 0, 0)))
                    {
                        g.FillRectangle(shadow, cellRect.X, cellRect.Bottom - 2, cellRect.Width, 2);
                        g.FillRectangle(shadow, cellRect.Right - 2, cellRect.Y, 2, cellRect.Height);
                    }

                    // Subtle inner glow for depth
                    using (var innerGlow = new SolidBrush(Color.FromArgb(5, Accent)))
                    {
                        var innerRect = new RectangleF(
                            cellRect.X + 2, cellRect.Y + 2,
                            cellRect.Width - 4, cellRect.Height - 4);
                        g.FillRectangle(innerGlow, innerRect);
                    }
                }
            }

            // Grid lines with neon glow
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
            // Outer glow
            using (var path = RoundedPath(new Rectangle(r.X - 3, r.Y - 3, r.Width + 6, r.Height + 6), 18))
            using (var glowPen = new Pen(Color.FromArgb(80, Accent), 6f))
            {
                g.DrawPath(glowPen, path);
            }

            // Main border
            using (var path = RoundedPath(r, 15))
            using (var pen = new Pen(Accent, 3f))
            {
                g.DrawPath(pen, path);
            }

            // Inner highlight
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

            float maxThickness = Math.Min(cw, ch) * 0.92f;
            float minThickness = maxThickness * 0.45f;

            // Create smooth path through all points
            using (var bodyPath = new GraphicsPath())
            {
                // Build smooth bezier path
                if (pts.Count >= 2)
                {
                    bodyPath.StartFigure();
                    
                    for (int i = 0; i < pts.Count - 1; i++)
                    {
                        PointF p0 = i > 0 ? pts[i - 1] : pts[i];
                        PointF p1 = pts[i];
                        PointF p2 = pts[i + 1];
                        PointF p3 = (i + 2 < pts.Count) ? pts[i + 2] : p2;

                        // Create smooth control points
                        PointF c1 = new PointF(
                            p1.X + (p2.X - p0.X) / 6f,
                            p1.Y + (p2.Y - p0.Y) / 6f
                        );
                        PointF c2 = new PointF(
                            p2.X - (p3.X - p1.X) / 6f,
                            p2.Y - (p3.Y - p1.Y) / 6f
                        );

                        if (i == 0)
                            bodyPath.AddLine(p1, p1);
                        
                        bodyPath.AddBezier(p1, c1, c2, p2);
                    }
                }

                // Calculate colors for gradient
                Color startColor = AccentWarm;
                Color endColor = Accent;

                // Get bounds for gradient
                var bounds = bodyPath.GetBounds();
                if (bounds.Width == 0 || bounds.Height == 0)
                {
                    DrawSnakeHead(g, pts[0], Math.Min(cw, ch) * 0.48f);
                    return;
                }

                float avgThickness = (maxThickness + minThickness) / 2f;

                // Draw layered body with smooth gradient
                
                // Outer glow
                using (var glowPen = new Pen(Color.FromArgb(30, startColor), avgThickness + 16f))
                {
                    glowPen.LineJoin = LineJoin.Round;
                    glowPen.EndCap = LineCap.Round;
                    glowPen.StartCap = LineCap.Round;
                    g.DrawPath(glowPen, bodyPath);
                }

                // Shadow
                var shadowMatrix = new System.Drawing.Drawing2D.Matrix();
                shadowMatrix.Translate(2, 2);
                using (var shadowPath = (GraphicsPath)bodyPath.Clone())
                {
                    shadowPath.Transform(shadowMatrix);
                    using (var shadowPen = new Pen(Color.FromArgb(120, 0, 0, 0), avgThickness + 4f))
                    {
                        shadowPen.LineJoin = LineJoin.Round;
                        shadowPen.EndCap = LineCap.Round;
                        shadowPen.StartCap = LineCap.Round;
                        g.DrawPath(shadowPen, shadowPath);
                    }
                }

                // Dark base layer
                using (var baseBrush = new LinearGradientBrush(
                    new PointF(bounds.Left, bounds.Top),
                    new PointF(bounds.Right, bounds.Bottom),
                    ControlPaint.Dark(startColor, 0.35f),
                    ControlPaint.Dark(endColor, 0.35f)))
                using (var basePen = new Pen(baseBrush, avgThickness))
                {
                    basePen.LineJoin = LineJoin.Round;
                    basePen.EndCap = LineCap.Round;
                    basePen.StartCap = LineCap.Round;
                    g.DrawPath(basePen, bodyPath);
                }

                // Main body with smooth gradient from warm to cool
                using (var mainBrush = new LinearGradientBrush(
                    new PointF(bounds.Left, bounds.Top),
                    new PointF(bounds.Right, bounds.Bottom),
                    ControlPaint.Light(startColor, 0.45f),
                    ControlPaint.Light(endColor, 0.45f)))
                using (var mainPen = new Pen(mainBrush, avgThickness * 0.9f))
                {
                    mainPen.LineJoin = LineJoin.Round;
                    mainPen.EndCap = LineCap.Round;
                    mainPen.StartCap = LineCap.Round;
                    g.DrawPath(mainPen, bodyPath);
                }

                // Top highlight stripe
                using (var highlightPen = new Pen(Color.FromArgb(90, 255, 255, 255), avgThickness * 0.4f))
                {
                    highlightPen.LineJoin = LineJoin.Round;
                    highlightPen.EndCap = LineCap.Round;
                    highlightPen.StartCap = LineCap.Round;
                    g.DrawPath(highlightPen, bodyPath);
                }

                // Sharp specular highlight
                using (var specPen = new Pen(Color.FromArgb(120, 255, 255, 255), avgThickness * 0.2f))
                {
                    specPen.LineJoin = LineJoin.Round;
                    specPen.EndCap = LineCap.Round;
                    specPen.StartCap = LineCap.Round;
                    g.DrawPath(specPen, bodyPath);
                }

                // Subtle outline
                using (var outlineBrush = new LinearGradientBrush(
                    new PointF(bounds.Left, bounds.Top),
                    new PointF(bounds.Right, bounds.Bottom),
                    ControlPaint.Dark(startColor, 0.45f),
                    ControlPaint.Dark(endColor, 0.45f)))
                using (var outlinePen = new Pen(outlineBrush, 1.8f))
                {
                    outlinePen.LineJoin = LineJoin.Round;
                    outlinePen.EndCap = LineCap.Round;
                    outlinePen.StartCap = LineCap.Round;
                    g.DrawPath(outlinePen, bodyPath);
                }
            }

            // Draw head on top
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

            // Match body thickness - slightly larger for the head
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

            // Use same colors as body - gradient from AccentWarm to Accent
            Color headStartColor = AccentWarm;
            Color headEndColor = Accent;

            // Multi-layer glow (outermost)
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

            // Shadow
            using (var shadowBrush = new SolidBrush(Color.FromArgb(140, 0, 0, 0)))
            {
                var shadowRect = new RectangleF(
                    headRect.X + 3, headRect.Y + 3,
                    headRect.Width, headRect.Height);
                g.FillEllipse(shadowBrush, shadowRect);
            }

            // Dark base layer (for 3D depth)
            using (var darkBase = new SolidBrush(ControlPaint.Dark(headStartColor, 0.35f)))
            {
                g.FillEllipse(darkBase, headRect);
            }

            // Main head with vertical gradient matching body style
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

            // Main highlight (large, diffuse)
            using (var highlightBrush = new SolidBrush(Color.FromArgb(90, 255, 255, 255)))
            {
                var highlightRect = new RectangleF(
                    center.X - headWidth * 0.25f,
                    center.Y - headHeight * 0.35f,
                    headWidth * 0.5f,
                    headHeight * 0.5f);
                g.FillEllipse(highlightBrush, highlightRect);
            }

            // Specular highlight (small, sharp)
            using (var specBrush = new SolidBrush(Color.FromArgb(140, 255, 255, 255)))
            {
                var specRect = new RectangleF(
                    center.X - headWidth * 0.15f,
                    center.Y - headHeight * 0.3f,
                    headWidth * 0.25f,
                    headHeight * 0.22f);
                g.FillEllipse(specBrush, specRect);
            }

            // Subtle outline for definition
            using (var outlinePen = new Pen(ControlPaint.Dark(headStartColor, 0.45f), 2f))
            {
                g.DrawEllipse(outlinePen, headRect);
            }

            // Inner rim light (top)
            using (var rimPen = new Pen(Color.FromArgb(60, 255, 255, 255), 1.5f))
            {
                var rimRect = new RectangleF(
                    headRect.X + 3, headRect.Y + 3,
                    headRect.Width - 6, headRect.Height - 6);
                g.DrawArc(rimPen, rimRect, 180, 180);
            }

            // Eyes - larger and more expressive
            float eyeOffsetX = headWidth * 0.22f;
            float eyeOffsetY = headHeight * 0.18f;
            float eyeSize = Math.Max(5f, headHeight * 0.28f);

            // Eye whites with subtle shadow
            using (var eyeShadow = new SolidBrush(Color.FromArgb(40, 0, 0, 0)))
            using (var white = new SolidBrush(Color.FromArgb(250, 255, 255, 255)))
            using (var pupil = new SolidBrush(Color.FromArgb(20, 20, 30)))
            using (var gloss = new SolidBrush(Color.FromArgb(180, 255, 255, 255)))
            using (var iris = new SolidBrush(Color.FromArgb(60, 200, 255)))
            {
                // Left eye
                var leftEyeRect = new RectangleF(
                    center.X - eyeOffsetX - eyeSize,
                    center.Y - eyeOffsetY - eyeSize,
                    eyeSize * 2, eyeSize * 2);
                g.FillEllipse(eyeShadow, leftEyeRect.X + 1, leftEyeRect.Y + 1, leftEyeRect.Width, leftEyeRect.Height);
                g.FillEllipse(white, leftEyeRect);

                // Iris
                g.FillEllipse(iris,
                    center.X - eyeOffsetX - eyeSize * 0.6f,
                    center.Y - eyeOffsetY - eyeSize * 0.6f,
                    eyeSize * 1.2f, eyeSize * 1.2f);

                // Pupil
                g.FillEllipse(pupil,
                    center.X - eyeOffsetX - eyeSize * 0.4f,
                    center.Y - eyeOffsetY - eyeSize * 0.4f,
                    eyeSize * 0.8f, eyeSize * 0.8f);

                // Dual gloss highlights
                g.FillEllipse(gloss,
                    center.X - eyeOffsetX - eyeSize * 0.65f,
                    center.Y - eyeOffsetY - eyeSize * 0.7f,
                    eyeSize * 0.5f, eyeSize * 0.5f);
                g.FillEllipse(gloss,
                    center.X - eyeOffsetX - eyeSize * 0.1f,
                    center.Y - eyeOffsetY - eyeSize * 0.2f,
                    eyeSize * 0.25f, eyeSize * 0.25f);

                // Right eye
                var rightEyeRect = new RectangleF(
                    center.X + eyeOffsetX - eyeSize,
                    center.Y - eyeOffsetY - eyeSize,
                    eyeSize * 2, eyeSize * 2);
                g.FillEllipse(eyeShadow, rightEyeRect.X + 1, rightEyeRect.Y + 1, rightEyeRect.Width, rightEyeRect.Height);
                g.FillEllipse(white, rightEyeRect);

                // Iris
                g.FillEllipse(iris,
                    center.X + eyeOffsetX - eyeSize * 0.6f,
                    center.Y - eyeOffsetY - eyeSize * 0.6f,
                    eyeSize * 1.2f, eyeSize * 1.2f);

                // Pupil
                g.FillEllipse(pupil,
                    center.X + eyeOffsetX - eyeSize * 0.4f,
                    center.Y - eyeOffsetY - eyeSize * 0.4f,
                    eyeSize * 0.8f, eyeSize * 0.8f);

                // Dual gloss highlights
                g.FillEllipse(gloss,
                    center.X + eyeOffsetX - eyeSize * 0.65f,
                    center.Y - eyeOffsetY - eyeSize * 0.7f,
                    eyeSize * 0.5f, eyeSize * 0.5f);
                g.FillEllipse(gloss,
                    center.X + eyeOffsetX - eyeSize * 0.1f,
                    center.Y - eyeOffsetY - eyeSize * 0.2f,
                    eyeSize * 0.25f, eyeSize * 0.25f);
            }

            // Animated tongue with forked tip - matching body color scheme
            float tongueWave = (float)Math.Sin(_time * 8) * 0.05f;
            using (var tongueShadow = new SolidBrush(Color.FromArgb(100, 0, 0, 0)))
            using (var tongueBrush = new LinearGradientBrush(
                new PointF(center.X + headWidth * 0.3f, center.Y),
                new PointF(center.X + headWidth * 0.7f, center.Y),
                Color.FromArgb(255, 140, 120),
                Color.FromArgb(220, 100, 80)))
            {
                // Shadow
                var shadowTip = new PointF(center.X + headWidth * 0.7f + 2, center.Y + 2);
                var shadowLeft = new PointF(center.X + headWidth * 0.3f + 2, center.Y - headHeight * 0.08f + 2);
                var shadowRight = new PointF(center.X + headWidth * 0.3f + 2, center.Y + headHeight * 0.08f + 2);
                g.FillPolygon(tongueShadow, new[] { shadowLeft, shadowTip, shadowRight });

                // Main tongue
                var tongueTip = new PointF(center.X + headWidth * 0.7f, center.Y + tongueWave * headHeight);
                var tongueLeft = new PointF(center.X + headWidth * 0.3f, center.Y - headHeight * 0.08f);
                var tongueRight = new PointF(center.X + headWidth * 0.3f, center.Y + headHeight * 0.08f);
                g.FillPolygon(tongueBrush, new[] { tongueLeft, tongueTip, tongueRight });

                // Fork splits
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
            // Animation disabled - food travels through body naturally
            // No visual effect needed
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

            // Multiple glow layers
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

            // Main food as star shape
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

            // Highlight
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

                // Glow
                using (var glow = new SolidBrush(Color.FromArgb((int)(lifeFactor * 80), particle.Color)))
                {
                    g.FillEllipse(glow, pixelPos.X - radius * 2, pixelPos.Y - radius * 2, radius * 4, radius * 4);
                }

                // Core
                using (var brush = new SolidBrush(Color.FromArgb((int)(lifeFactor * 250), particle.Color)))
                {
                    g.FillEllipse(brush, pixelPos.X - radius, pixelPos.Y - radius, radius * 2, radius * 2);
                }
            }
        }

        private void DrawEnhancedHUD(Graphics g, Rectangle boardRect, GameState state)
        {
            var hudRect = new Rectangle(boardRect.Left, boardRect.Top - 50, boardRect.Width, 45);

            // HUD background with glow
            using (var path = RoundedPath(hudRect, 12))
            {
                // Glow
                using (var glowBrush = new SolidBrush(Color.FromArgb(60, Accent)))
                {
                    var glowRect = new Rectangle(hudRect.X - 2, hudRect.Y - 2, hudRect.Width + 4, hudRect.Height + 4);
                    using (var glowPath = RoundedPath(glowRect, 14))
                    {
                        g.FillPath(glowBrush, glowPath);
                    }
                }

                // Background
                using (var bgBrush = new SolidBrush(Color.FromArgb(180, 20, 20, 40)))
                {
                    g.FillPath(bgBrush, path);
                }

                // Border
                using (var pen = new Pen(Accent, 2f))
                {
                    g.DrawPath(pen, path);
                }
            }

            // Score
            using (var font = new Font("Segoe UI", 18, FontStyle.Bold))
            using (var textBrush = new SolidBrush(Accent))
            using (var shadowBrush = new SolidBrush(Color.FromArgb(100, 0, 0, 0)))
            {
                string scoreText = $"? {state.Score}";
                var textSize = g.MeasureString(scoreText, font);
                float x = hudRect.Right - textSize.Width - 20;
                float y = hudRect.Top + (hudRect.Height - textSize.Height) / 2f;
                
                g.DrawString(scoreText, font, shadowBrush, x + 2, y + 2);
                g.DrawString(scoreText, font, textBrush, x, y);
            }

            // Length indicator
            using (var font = new Font("Segoe UI", 16, FontStyle.Bold))
            using (var textBrush = new SolidBrush(AccentWarm))
            using (var shadowBrush = new SolidBrush(Color.FromArgb(100, 0, 0, 0)))
            {
                string lengthText = $"?? {state.Snake.Count}";
                float x = hudRect.Left + 20;
                float y = hudRect.Top + (hudRect.Height - font.Height) / 2f;
                
                g.DrawString(lengthText, font, shadowBrush, x + 2, y + 2);
                g.DrawString(lengthText, font, textBrush, x, y);
            }

            // FPS (bottom right corner)
            using (var smallFont = new Font("Consolas", 10))
            using (var infoBrush = new SolidBrush(Color.FromArgb(200, 180, 200, 220)))
            {
                string infoText = $"FPS: {_lastFps}";
                g.DrawString(infoText, smallFont, infoBrush, boardRect.Right - 80, boardRect.Bottom + 8);
            }
        }

        private void DrawGameOverMenu(Graphics g)
        {
            var overlay = new Rectangle(0, 0, Width, Height);
            using (var bg = new SolidBrush(Color.FromArgb(200, 10, 10, 20)))
            {
                g.FillRectangle(bg, overlay);
            }

            using (var titleFont = new Font("Segoe UI", 32, FontStyle.Bold))
            using (var font = new Font("Segoe UI", 16, FontStyle.Bold))
            using (var small = new Font("Segoe UI", 14, FontStyle.Regular))
            {
                // Title with glow
                string title = "GAME OVER";
                var ts = g.MeasureString(title, titleFont);
                float titleX = (Width - ts.Width) / 2f;
                float titleY = Height * 0.3f;
                
                using (var glowBrush = new SolidBrush(Color.FromArgb(150, AccentWarm)))
                {
                    g.DrawString(title, titleFont, glowBrush, titleX + 3, titleY + 3);
                }
                using (var titleBrush = new SolidBrush(AccentWarm))
                {
                    g.DrawString(title, titleFont, titleBrush, titleX, titleY);
                }

                // Score
                string scoreText = $"FINAL SCORE: {_gameOverScore}";
                var ss = g.MeasureString(scoreText, font);
                using (var scoreBrush = new SolidBrush(Accent))
                {
                    g.DrawString(scoreText, font, scoreBrush, (Width - ss.Width) / 2f, titleY + ts.Height + 20);
                }

                // Instructions
                string hint = "Press R to Restart  •  ESC to Exit";
                var hs = g.MeasureString(hint, small);
                using (var hintBrush = new SolidBrush(Color.FromArgb(240, 255, 255, 255)))
                {
                    g.DrawString(hint, small, hintBrush, (Width - hs.Width) / 2f, titleY + ts.Height + ss.Height + 50);
                }
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

        private Color LerpColor(Color a, Color b, float t)
        {
            t = Math.Max(0f, Math.Min(1f, t));
            return Color.FromArgb(
                (int)(a.R * (1 - t) + b.R * t),
                (int)(a.G * (1 - t) + b.G * t),
                (int)(a.B * (1 - t) + b.B * t)
            );
        }

        private int SafeSnakeCount(GameState state)
        {
            return state.Snake == null ? 0 : state.Snake.Count;
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
