using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace RecoilStudio
{
    public enum PatternEditMode
    {
        Draw = 0,
        EditNodes = 1
    }

    /// <summary>
    /// Lienzo vectorial interactivo estilo curva numerada (1 -> 10 -> 20 -> 30) para dibujar o editar nodos.
    /// </summary>
    public class PatternCanvas : FrameworkElement
    {
        private const double DrawBaseScale = 0.85;

        private PatternEditMode _editMode = PatternEditMode.Draw;
        private List<PatternNode> _nodes = new List<PatternNode>();
        private int _bulletCount = 30;
        private double _totalDurationMs = 2400.0;
        private int _targetSegments = 10;
        private double _simulationTimeMs = -1.0;

        // Transformacion de vista
        private double _viewScale = 0.85;
        private double _originX = 280.0;
        private double _originY = 65.0;

        // Estado de interaccion
        private bool _isDrawing = false;
        private readonly List<Point> _freehandPoints = new List<Point>();
        private int _dragNodeIndex = -1;
        private int _hoverNodeIndex = -1;
        private int _hoverSegIndex = -1;

        private static readonly Brush CanvasBgBrush = FreezeBrush(Color.FromRgb(11, 13, 19));
        private static readonly Pen BorderPen = FreezePen(Color.FromRgb(37, 42, 56), 1.0);
        private static readonly Pen GridPen = FreezePen(Color.FromRgb(21, 26, 38), 1.0);
        private static readonly Pen AxisPen = FreezePen(Color.FromRgb(38, 46, 64), 1.0);
        private static readonly Pen NodeStrokePen = FreezePen(Color.FromRgb(11, 13, 19), 1.5);
        private static readonly Pen SelectedNodePen = FreezePen(Color.FromRgb(56, 189, 248), 2.0);
        private static readonly Brush NodeFillBrush = FreezeBrush(Color.FromRgb(226, 232, 240));
        private static readonly Brush HoverNodeBrush = FreezeBrush(Color.FromRgb(56, 189, 248));
        private static readonly Brush BulletDotBrush = FreezeBrush(Color.FromRgb(248, 250, 252));
        private static readonly Brush LabelTextBrush = FreezeBrush(Color.FromRgb(241, 245, 249));
        private static readonly Brush LabelShadowBrush = FreezeBrush(Color.FromArgb(200, 11, 13, 19));
        private static readonly Brush SimDotBrush = FreezeBrush(Color.FromRgb(34, 197, 94));
        private static readonly Brush SimGlowBrush = FreezeBrush(Color.FromArgb(90, 34, 197, 94));
        private static readonly Typeface LabelTypeface = new Typeface(new FontFamily("Consolas"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal);
        private static readonly Typeface UiTypeface = new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.SemiBold, FontStretches.Normal);

        public event EventHandler PatternModified;
        public event EventHandler StrokeStarted;

        public PatternEditMode EditMode
        {
            get { return _editMode; }
            set
            {
                _editMode = value;
                _hoverNodeIndex = -1;
                _hoverSegIndex = -1;
                Cursor = value == PatternEditMode.Draw ? Cursors.Cross : Cursors.Arrow;
                InvalidateVisual();
            }
        }

        public List<PatternNode> Nodes
        {
            get { return _nodes; }
        }

        public int BulletCount
        {
            get { return _bulletCount; }
            set { _bulletCount = Math.Max(5, Math.Min(60, value)); InvalidateVisual(); }
        }

        public double TotalDurationMs
        {
            get { return _totalDurationMs; }
            set { _totalDurationMs = Math.Max(200.0, value); InvalidateVisual(); }
        }

        public int TargetSegments
        {
            get { return _targetSegments; }
            set { _targetSegments = Math.Max(4, Math.Min(24, value)); }
        }

        public double SimulationTimeMs
        {
            get { return _simulationTimeMs; }
            set { _simulationTimeMs = value; InvalidateVisual(); }
        }

        public PatternCanvas()
        {
            ClipToBounds = true;
            Cursor = Cursors.Cross;
            SizeChanged += delegate { AutoFitView(); };
        }

        public void SetNodes(List<PatternNode> nodes, bool autoFit)
        {
            _nodes = new List<PatternNode>();
            if (nodes != null)
            {
                for (int i = 0; i < nodes.Count; i++)
                {
                    if (nodes[i] != null)
                    {
                        _nodes.Add(nodes[i].Clone());
                    }
                }
            }
            if (autoFit)
            {
                AutoFitView();
            }
            InvalidateVisual();
        }

        public void ClearPattern()
        {
            _nodes = new List<PatternNode>();
            _viewScale = DrawBaseScale;
            _originX = (ActualWidth > 40 ? ActualWidth : 560) * 0.5;
            _originY = (ActualHeight > 40 ? ActualHeight : 500) * 0.13;
            InvalidateVisual();
        }

        public void AutoFitView()
        {
            double w = ActualWidth > 40 ? ActualWidth : 560.0;
            double h = ActualHeight > 40 ? ActualHeight : 500.0;

            if (_nodes == null || _nodes.Count < 2)
            {
                _viewScale = DrawBaseScale;
                _originX = w * 0.5;
                _originY = h * 0.13;
                InvalidateVisual();
                return;
            }

            double minX = 0.0;
            double maxX = 0.0;
            double minY = 0.0;
            double maxY = 0.0;

            for (int i = 0; i < _nodes.Count; i++)
            {
                if (_nodes[i].X < minX) minX = _nodes[i].X;
                if (_nodes[i].X > maxX) maxX = _nodes[i].X;
                if (_nodes[i].Y < minY) minY = _nodes[i].Y;
                if (_nodes[i].Y > maxY) maxY = _nodes[i].Y;
            }

            double spanX = Math.Max(140.0, maxX - minX);
            double spanY = Math.Max(180.0, maxY - minY);

            double sx = (w * 0.74) / spanX;
            double sy = (h * 0.74) / spanY;
            _viewScale = Clamp(Math.Min(sx, sy), 0.25, 3.2);

            double midX = (minX + maxX) * 0.5;
            double midY = (minY + maxY) * 0.5;
            _originX = w * 0.5 - midX * _viewScale;
            _originY = h * 0.5 - midY * _viewScale;

            InvalidateVisual();
        }

        private Point ModelToScreen(double mx, double my)
        {
            return new Point(_originX + mx * _viewScale, _originY + my * _viewScale);
        }

        private Point ScreenToModel(Point sp)
        {
            double s = _viewScale > 0.01 ? _viewScale : 1.0;
            return new Point((sp.X - _originX) / s, (sp.Y - _originY) / s);
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            Point pos = e.GetPosition(this);

            if (_editMode == PatternEditMode.Draw)
            {
                _isDrawing = true;
                _freehandPoints.Clear();
                _freehandPoints.Add(pos);
                CaptureMouse();
                InvalidateVisual();
                return;
            }

            // Modo Editar Nodos
            int hitIdx = HitTestNode(pos, 11.0);
            if (hitIdx >= 1)
            {
                if (StrokeStarted != null) StrokeStarted(this, EventArgs.Empty);
                _dragNodeIndex = hitIdx;
                CaptureMouse();
                InvalidateVisual();
                return;
            }

            // Insertar nodo sobre segmento si no se toco un nodo existente
            Point insertPt;
            int segIdx = HitTestSegment(pos, 10.0, out insertPt);
            if (segIdx >= 0 && segIdx < _nodes.Count - 1)
            {
                if (StrokeStarted != null) StrokeStarted(this, EventArgs.Empty);
                PatternNode a = _nodes[segIdx];
                PatternNode b = _nodes[segIdx + 1];
                double len = Distance(ModelToScreen(a.X, a.Y), ModelToScreen(b.X, b.Y));
                double frac = len > 1.0 ? Distance(ModelToScreen(a.X, a.Y), insertPt) / len : 0.5;
                frac = Clamp(frac, 0.1, 0.9);

                Point m = ScreenToModel(insertPt);
                double t = Math.Round(a.TimeMs + (b.TimeMs - a.TimeMs) * frac);
                if (t <= a.TimeMs + 8.0) t = a.TimeMs + 8.0;
                if (t >= b.TimeMs - 8.0) t = Math.Max(a.TimeMs + 8.0, b.TimeMs - 8.0);

                _nodes.Insert(segIdx + 1, new PatternNode(m.X, m.Y, t));
                _dragNodeIndex = segIdx + 1;
                CaptureMouse();
                InvalidateVisual();
                if (PatternModified != null) PatternModified(this, EventArgs.Empty);
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            Point pos = e.GetPosition(this);

            if (_isDrawing)
            {
                Point last = _freehandPoints[_freehandPoints.Count - 1];
                if (Distance(pos, last) >= 3.0)
                {
                    _freehandPoints.Add(pos);
                    InvalidateVisual();
                }
                return;
            }

            if (_dragNodeIndex >= 1 && _dragNodeIndex < _nodes.Count)
            {
                Point m = ScreenToModel(pos);
                _nodes[_dragNodeIndex].X = m.X;
                _nodes[_dragNodeIndex].Y = m.Y;
                InvalidateVisual();
                if (PatternModified != null) PatternModified(this, EventArgs.Empty);
                return;
            }

            if (_editMode == PatternEditMode.EditNodes)
            {
                int prevNode = _hoverNodeIndex;
                int prevSeg = _hoverSegIndex;
                _hoverNodeIndex = HitTestNode(pos, 11.0);
                if (_hoverNodeIndex < 0)
                {
                    Point dummy;
                    _hoverSegIndex = HitTestSegment(pos, 9.0, out dummy);
                }
                else
                {
                    _hoverSegIndex = -1;
                }

                if (_hoverNodeIndex >= 1) Cursor = Cursors.SizeAll;
                else if (_hoverSegIndex >= 0) Cursor = Cursors.Hand;
                else Cursor = Cursors.Arrow;

                if (prevNode != _hoverNodeIndex || prevSeg != _hoverSegIndex)
                {
                    InvalidateVisual();
                }
            }
        }

        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonUp(e);

            if (_isDrawing)
            {
                _isDrawing = false;
                ReleaseMouseCapture();
                Point pos = e.GetPosition(this);
                if (_freehandPoints.Count > 0 && Distance(pos, _freehandPoints[_freehandPoints.Count - 1]) >= 1.0)
                {
                    _freehandPoints.Add(pos);
                }

                if (_freehandPoints.Count >= 2)
                {
                    if (StrokeStarted != null) StrokeStarted(this, EventArgs.Empty);

                    // Escalar el trazo en pantalla a unidades de modelo coherentes
                    List<Point> scaled = new List<Point>(_freehandPoints.Count);
                    Point p0 = _freehandPoints[0];
                    for (int i = 0; i < _freehandPoints.Count; i++)
                    {
                        scaled.Add(new Point(
                            (_freehandPoints[i].X - p0.X) / DrawBaseScale,
                            (_freehandPoints[i].Y - p0.Y) / DrawBaseScale));
                    }

                    _nodes = PatternGeometry.ProcessFreehandStroke(scaled, _totalDurationMs, _targetSegments);
                    AutoFitView();
                    if (PatternModified != null) PatternModified(this, EventArgs.Empty);
                }
                _freehandPoints.Clear();
                InvalidateVisual();
                return;
            }

            if (_dragNodeIndex >= 0)
            {
                _dragNodeIndex = -1;
                ReleaseMouseCapture();
                InvalidateVisual();
            }
        }

        protected override void OnMouseRightButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseRightButtonDown(e);
            if (_editMode != PatternEditMode.EditNodes || _nodes == null || _nodes.Count <= 2) return;

            Point pos = e.GetPosition(this);
            int hitIdx = HitTestNode(pos, 11.0);
            if (hitIdx >= 1)
            {
                if (StrokeStarted != null) StrokeStarted(this, EventArgs.Empty);
                _nodes.RemoveAt(hitIdx);
                _hoverNodeIndex = -1;
                InvalidateVisual();
                if (PatternModified != null) PatternModified(this, EventArgs.Empty);
            }
        }

        private int HitTestNode(Point screenPt, double radius)
        {
            if (_nodes == null) return -1;
            for (int i = _nodes.Count - 1; i >= 0; i--)
            {
                Point p = ModelToScreen(_nodes[i].X, _nodes[i].Y);
                if (Distance(screenPt, p) <= radius)
                {
                    return i;
                }
            }
            return -1;
        }

        private int HitTestSegment(Point screenPt, double maxDist, out Point closestPt)
        {
            closestPt = screenPt;
            if (_nodes == null || _nodes.Count < 2) return -1;

            double best = maxDist;
            int bestIdx = -1;
            for (int i = 0; i < _nodes.Count - 1; i++)
            {
                Point a = ModelToScreen(_nodes[i].X, _nodes[i].Y);
                Point b = ModelToScreen(_nodes[i + 1].X, _nodes[i + 1].Y);
                Point proj = ProjectPointOnSegment(screenPt, a, b);
                double d = Distance(screenPt, proj);
                if (d < best)
                {
                    best = d;
                    bestIdx = i;
                    closestPt = proj;
                }
            }
            return bestIdx;
        }

        protected override void OnRender(DrawingContext dc)
        {
            double w = ActualWidth;
            double h = ActualHeight;
            if (w < 20.0 || h < 20.0) return;

            dc.DrawRoundedRectangle(CanvasBgBrush, BorderPen, new Rect(0.5, 0.5, w - 1.0, h - 1.0), 8.0, 8.0);

            // Cuadricula tactica
            double step = 32.0;
            for (double x = (_originX % step + step) % step; x < w - 4.0; x += step)
            {
                dc.DrawLine(GridPen, new Point(x, 6), new Point(x, h - 6));
            }
            for (double y = (_originY % step + step) % step; y < h - 4.0; y += step)
            {
                dc.DrawLine(GridPen, new Point(6, y), new Point(w - 6, y));
            }

            dc.DrawLine(AxisPen, new Point(8, _originY), new Point(w - 8, _originY));
            dc.DrawLine(AxisPen, new Point(_originX, 8), new Point(_originX, h - 8));

            // Trazo vivo mientras el usuario dibuja
            if (_isDrawing && _freehandPoints.Count >= 2)
            {
                DrawGradientPolyline(dc, _freehandPoints.ToArray(), 2.8, 6.5);
                DrawBulletLabel(dc, _freehandPoints[0], "1", new Point(10, -8));
                return;
            }

            if (_nodes == null || _nodes.Count < 2)
            {
                FormattedText emptyFt = CreateText(
                    "Haz clic y arrastra hacia abajo para dibujar tu patrón\no elige una plantilla a la derecha",
                    UiTypeface,
                    12.5,
                    ThemeResources.TextMutedBrush);
                emptyFt.TextAlignment = TextAlignment.Center;
                dc.DrawText(emptyFt, new Point(w * 0.5, h * 0.48 - emptyFt.Height * 0.5));
                return;
            }

            // Construir puntos suaves en pantalla para el degradado ambar -> naranja -> rojo
            List<Point> densePts = BuildDenseScreenPolyline(_nodes);
            DrawGradientPolyline(dc, densePts.ToArray(), 2.6, 6.2);

            // Marcadores de balas (1, 10, 20, 30...)
            DrawBulletMarkers(dc, w, h);

            // Manejadores de nodos en modo Edicion
            if (_editMode == PatternEditMode.EditNodes)
            {
                for (int i = 0; i < _nodes.Count; i++)
                {
                    Point p = ModelToScreen(_nodes[i].X, _nodes[i].Y);
                    bool hovered = (i == _hoverNodeIndex || i == _dragNodeIndex);
                    double r = (i == 0) ? 5.0 : (hovered ? 6.5 : 4.5);
                    Brush fill = (i == 0) ? BulletDotBrush : (hovered ? HoverNodeBrush : NodeFillBrush);
                    Pen pen = hovered ? SelectedNodePen : NodeStrokePen;
                    dc.DrawEllipse(fill, pen, p, r, r);
                }
            }

            // Indicador de simulacion animada
            if (_simulationTimeMs >= 0.0)
            {
                Point simModel = PatternGeometry.EvaluatePositionAtTime(_nodes, _simulationTimeMs);
                Point simPt = ModelToScreen(simModel.X, simModel.Y);
                dc.DrawEllipse(SimGlowBrush, null, simPt, 11.0, 11.0);
                dc.DrawEllipse(SimDotBrush, NodeStrokePen, simPt, 5.0, 5.0);

                int bulletNum = 1 + (int)Math.Floor((_simulationTimeMs / Math.Max(1.0, _totalDurationMs)) * Math.Max(1, _bulletCount - 1));
                bulletNum = Math.Max(1, Math.Min(_bulletCount, bulletNum));
                string simText = string.Format(
                    CultureInfo.InvariantCulture,
                    "Bala {0}/{1} · {2:F2}s",
                    bulletNum,
                    _bulletCount,
                    _simulationTimeMs / 1000.0);
                FormattedText simFt = CreateText(simText, LabelTypeface, 11.0, ThemeResources.SuccessBrush);
                dc.DrawRoundedRectangle(ThemeResources.SuccessSoftBrush, null, new Rect(12, 12, simFt.Width + 14, simFt.Height + 8), 5, 5);
                dc.DrawText(simFt, new Point(19, 16));
            }
        }

        private List<Point> BuildDenseScreenPolyline(List<PatternNode> nodes)
        {
            List<Point> pts = new List<Point>();
            const int subSteps = 6;
            for (int i = 0; i < nodes.Count - 1; i++)
            {
                Point a = ModelToScreen(nodes[i].X, nodes[i].Y);
                Point b = ModelToScreen(nodes[i + 1].X, nodes[i + 1].Y);
                if (i == 0) pts.Add(a);
                for (int s = 1; s <= subSteps; s++)
                {
                    double f = (double)s / subSteps;
                    pts.Add(new Point(a.X + (b.X - a.X) * f, a.Y + (b.Y - a.Y) * f));
                }
            }
            return pts;
        }

        private void DrawGradientPolyline(DrawingContext dc, Point[] pts, double coreWidth, double glowWidth)
        {
            if (pts == null || pts.Length < 2) return;
            int total = pts.Length - 1;

            for (int i = 0; i < total; i++)
            {
                double t = (double)i / Math.Max(1, total - 1);
                Color c = PatternGeometry.GetColorAlongCurve(t);

                Pen glowPen = new Pen(new SolidColorBrush(Color.FromArgb(48, c.R, c.G, c.B)), glowWidth);
                glowPen.StartLineCap = PenLineCap.Round;
                glowPen.EndLineCap = PenLineCap.Round;

                Pen corePen = new Pen(new SolidColorBrush(c), coreWidth);
                corePen.StartLineCap = PenLineCap.Round;
                corePen.EndLineCap = PenLineCap.Round;

                dc.DrawLine(glowPen, pts[i], pts[i + 1]);
                dc.DrawLine(corePen, pts[i], pts[i + 1]);
            }
        }

        private void DrawBulletMarkers(DrawingContext dc, double w, double h)
        {
            List<int> milestones = new List<int>();
            milestones.Add(1);
            for (int b = 10; b <= _bulletCount; b += 10)
            {
                milestones.Add(b);
            }
            if (_bulletCount > 1 && milestones[milestones.Count - 1] != _bulletCount)
            {
                milestones.Add(_bulletCount);
            }

            double endT = _nodes[_nodes.Count - 1].TimeMs;
            double span = endT > 0.0 ? endT : _totalDurationMs;

            for (int i = 0; i < milestones.Count; i++)
            {
                int b = milestones[i];
                double frac = _bulletCount > 1 ? (double)(b - 1) / (_bulletCount - 1) : 0.0;
                double t = frac * span;
                Point modelPt = PatternGeometry.EvaluatePositionAtTime(_nodes, t);
                Point screenPt = ModelToScreen(modelPt.X, modelPt.Y);

                // Estimar direccion de la curva para ubicar la etiqueta sin pisar la linea
                Point modelPrev = PatternGeometry.EvaluatePositionAtTime(_nodes, Math.Max(0.0, t - span * 0.04));
                Point modelNext = PatternGeometry.EvaluatePositionAtTime(_nodes, Math.Min(span, t + span * 0.04));
                Point screenPrev = ModelToScreen(modelPrev.X, modelPrev.Y);
                Point screenNext = ModelToScreen(modelNext.X, modelNext.Y);

                Point offset = new Point(10, -7);
                if (b == 1)
                {
                    offset = new Point(9, -8);
                }
                else if (b == _bulletCount)
                {
                    double bx = screenPt.X - screenPrev.X;
                    double by = screenPt.Y - screenPrev.Y;
                    double blen = Math.Sqrt(bx * bx + by * by);
                    if (blen > 0.5)
                    {
                        offset = new Point((bx / blen) * 14.0 - 7.0, (by / blen) * 14.0 - 7.0);
                    }
                }
                else
                {
                    double tx = screenNext.X - screenPrev.X;
                    double ty = screenNext.Y - screenPrev.Y;
                    double len = Math.Sqrt(tx * tx + ty * ty);
                    if (len > 0.5)
                    {
                        double nx = -ty / len;
                        double ny = tx / len;
                        offset = new Point(nx * 14.0 - 7.0, ny * 14.0 - 7.0);
                    }
                }

                // Mantener etiqueta dentro del lienzo
                if (screenPt.X + offset.X > w - 26) offset.X = -22;
                if (screenPt.X + offset.X < 8) offset.X = 10;
                if (screenPt.Y + offset.Y > h - 20) offset.Y = -18;
                if (screenPt.Y + offset.Y < 8) offset.Y = 10;

                DrawBulletLabel(dc, screenPt, b.ToString(), offset);
            }
        }

        private void DrawBulletLabel(DrawingContext dc, Point pt, string text, Point offset)
        {
            dc.DrawEllipse(BulletDotBrush, NodeStrokePen, pt, 3.2, 3.2);
            FormattedText shadow = CreateText(text, LabelTypeface, 11.5, LabelShadowBrush);
            FormattedText ft = CreateText(text, LabelTypeface, 11.5, LabelTextBrush);
            Point labelPt = new Point(pt.X + offset.X, pt.Y + offset.Y);
            dc.DrawText(shadow, new Point(labelPt.X + 1, labelPt.Y + 1));
            dc.DrawText(ft, labelPt);
        }

        private FormattedText CreateText(string text, Typeface typeface, double size, Brush brush)
        {
            double dpi = 1.0;
            try
            {
                dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;
            }
            catch
            {
                dpi = 1.0;
            }
            return new FormattedText(text, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, typeface, size, brush, dpi);
        }

        private static Point ProjectPointOnSegment(Point p, Point a, Point b)
        {
            double abx = b.X - a.X;
            double aby = b.Y - a.Y;
            double len2 = abx * abx + aby * aby;
            if (len2 < 0.0001) return a;
            double t = ((p.X - a.X) * abx + (p.Y - a.Y) * aby) / len2;
            t = Clamp(t, 0.0, 1.0);
            return new Point(a.X + abx * t, a.Y + aby * t);
        }

        private static double Distance(Point a, Point b)
        {
            double dx = b.X - a.X;
            double dy = b.Y - a.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        private static double Clamp(double val, double min, double max)
        {
            if (val < min) return min;
            if (val > max) return max;
            return val;
        }

        private static SolidColorBrush FreezeBrush(Color c)
        {
            SolidColorBrush b = new SolidColorBrush(c);
            b.Freeze();
            return b;
        }

        private static Pen FreezePen(Color c, double thickness)
        {
            SolidColorBrush b = FreezeBrush(c);
            Pen p = new Pen(b, thickness);
            p.StartLineCap = PenLineCap.Round;
            p.EndLineCap = PenLineCap.Round;
            p.Freeze();
            return p;
        }
    }

    /// <summary>
    /// Ventana modal interactiva para dibujar, editar y simular patrones de recoil estilo curva numerada.
    /// </summary>
    public class PatternEditorWindow : Window
    {
        private readonly string _profileName;
        private readonly WeaponProfile _workingProfile;
        private readonly Stack<List<PatternNode>> _undoStack = new Stack<List<PatternNode>>();
        private readonly DispatcherTimer _simTimer;
        private DateTime _simStartTime;

        private PatternCanvas _canvas;
        private Button _tabDraw;
        private Button _tabEdit;
        private Button _btnSimulate;
        private TextBlock _lblHint;

        private ModernSlider _sliderDuration;
        private ModernSlider _sliderBullets;
        private ModernSlider _sliderScale;
        private ModernSlider _sliderSegments;
        private CheckBox _chkStopAtEnd;

        private TextBlock _lblStatPhases;
        private TextBlock _lblStatPeakVert;
        private TextBlock _lblStatMaxHoriz;
        private TextBlock _lblStatTotalDisp;

        private bool _isSyncing = false;

        public WeaponProfile ResultProfile
        {
            get { return _workingProfile; }
        }

        public PatternEditorWindow(Window owner, string profileName, WeaponProfile sourceProfile)
        {
            _profileName = profileName ?? "perfil";
            _workingProfile = sourceProfile != null ? sourceProfile.Clone() : new WeaponProfile();

            if (owner != null)
            {
                Owner = owner;
                Resources.MergedDictionaries.Add(ThemeResources.CreateThemeDictionary());
            }
            else
            {
                Resources.MergedDictionaries.Add(ThemeResources.CreateThemeDictionary());
            }

            _simTimer = new DispatcherTimer();
            _simTimer.Interval = TimeSpan.FromMilliseconds(16);
            _simTimer.Tick += OnSimulationTick;

            ConfigureWindow(owner);
            BuildUI();
            LoadInitialPattern();

            Closing += delegate
            {
                _simTimer.Stop();
            };
        }

        private void ConfigureWindow(Window owner)
        {
            Title = "Editor Visual de Patrón";
            Width = 1000;
            Height = 720;
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            ResizeMode = ResizeMode.NoResize;
            ShowInTaskbar = false;
            WindowStartupLocation = owner != null ? WindowStartupLocation.CenterOwner : WindowStartupLocation.CenterScreen;
            FontFamily = new FontFamily("Segoe UI");
            Foreground = ThemeResources.TextPrimaryBrush;
        }

        private void BuildUI()
        {
            Border outer = new Border
            {
                Margin = new Thickness(12),
                Background = ThemeResources.BgCanvasBrush,
                BorderBrush = ThemeResources.BorderStrongBrush,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                ClipToBounds = true,
                Effect = new DropShadowEffect
                {
                    Color = Colors.Black,
                    BlurRadius = 22,
                    ShadowDepth = 3,
                    Opacity = 0.65
                }
            };

            Grid root = new Grid();
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(46) });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(54) });

            // 1. Barra superior del editor
            Border header = new Border
            {
                Background = ThemeResources.BgSidebarBrush,
                BorderBrush = ThemeResources.BorderSubtleBrush,
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(16, 0, 12, 0)
            };
            header.MouseLeftButtonDown += OnHeaderDrag;

            Grid headerGrid = new Grid();
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            StackPanel titleStack = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            titleStack.Children.Add(ThemeResources.CreateVectorIcon(ThemeResources.IconPencilData, ThemeResources.AccentBrush, 15, 1.8));
            titleStack.Children.Add(new TextBlock
            {
                Text = "Editor Visual de Patrón",
                Foreground = ThemeResources.TextPrimaryBrush,
                FontSize = 13.5,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(9, 0, 10, 0),
                VerticalAlignment = VerticalAlignment.Center
            });
            Border profBadge = new Border
            {
                Background = ThemeResources.AccentSoftBrush,
                CornerRadius = new CornerRadius(5),
                Padding = new Thickness(8, 2, 8, 2),
                VerticalAlignment = VerticalAlignment.Center,
                Child = new TextBlock
                {
                    Text = _profileName.ToUpperInvariant(),
                    Foreground = ThemeResources.AccentBrush,
                    FontFamily = new FontFamily("Consolas"),
                    FontSize = 11,
                    FontWeight = FontWeights.Bold
                }
            };
            titleStack.Children.Add(profBadge);
            Grid.SetColumn(titleStack, 0);
            headerGrid.Children.Add(titleStack);

            Button btnClose = new Button
            {
                Style = (Style)Resources["TitleBarCloseButton"],
                Content = ThemeResources.CreateVectorIcon(ThemeResources.IconCloseData, ThemeResources.TextSecondaryBrush, 12, 2.0),
                IsCancel = true
            };
            Grid.SetColumn(btnClose, 1);
            headerGrid.Children.Add(btnClose);

            header.Child = headerGrid;
            Grid.SetRow(header, 0);
            root.Children.Add(header);

            // 2. Cuerpo: Lienzo a la izquierda, Parametros y Plantillas a la derecha
            Grid body = new Grid { Margin = new Thickness(16, 12, 16, 10) };
            body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(14) });
            body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(334) });

            // Columna Izquierda: Toolbar + Lienzo + Hint
            Grid leftCol = new Grid();
            leftCol.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            leftCol.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            leftCol.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            Grid toolbar = new Grid { Margin = new Thickness(0, 0, 0, 10) };
            toolbar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            toolbar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            toolbar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            Border modeBox = new Border
            {
                Background = ThemeResources.BgSidebarBrush,
                BorderBrush = ThemeResources.BorderSubtleBrush,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(7),
                Padding = new Thickness(2)
            };
            StackPanel modeTabs = new StackPanel { Orientation = Orientation.Horizontal };
            _tabDraw = new Button { Style = (Style)Resources["SegmentTabButton"], Content = "Dibujar (Lápiz)" };
            _tabEdit = new Button { Style = (Style)Resources["SegmentTabButton"], Content = "Editar Nodos" };
            _tabDraw.Click += delegate { SetEditMode(PatternEditMode.Draw); };
            _tabEdit.Click += delegate { SetEditMode(PatternEditMode.EditNodes); };
            modeTabs.Children.Add(_tabDraw);
            modeTabs.Children.Add(_tabEdit);
            modeBox.Child = modeTabs;
            Grid.SetColumn(modeBox, 0);
            toolbar.Children.Add(modeBox);

            StackPanel toolActions = new StackPanel { Orientation = Orientation.Horizontal };
            _btnSimulate = CreateToolButton("Simular", ThemeResources.IconPlayData, ThemeResources.SuccessBrush);
            _btnSimulate.Margin = new Thickness(0, 0, 6, 0);
            _btnSimulate.Click += delegate { ToggleSimulation(); };
            toolActions.Children.Add(_btnSimulate);

            Button btnFlip = CreateToolButton("Invertir X", ThemeResources.IconFlipData, ThemeResources.TextSecondaryBrush);
            btnFlip.Margin = new Thickness(0, 0, 6, 0);
            btnFlip.Click += delegate
            {
                PushUndoState();
                PatternGeometry.MirrorHorizontal(_canvas.Nodes);
                _canvas.AutoFitView();
                RefreshStats();
            };
            toolActions.Children.Add(btnFlip);

            Button btnUndo = CreateToolButton("Deshacer", ThemeResources.IconUndoData, ThemeResources.TextSecondaryBrush);
            btnUndo.Margin = new Thickness(0, 0, 6, 0);
            btnUndo.Click += delegate { Undo(); };
            toolActions.Children.Add(btnUndo);

            Button btnClear = CreateToolButton("Limpiar", ThemeResources.IconTrashData, ThemeResources.DangerBrush);
            btnClear.Click += delegate
            {
                StopSimulation();
                PushUndoState();
                _canvas.ClearPattern();
                RefreshStats();
            };
            toolActions.Children.Add(btnClear);

            Grid.SetColumn(toolActions, 2);
            toolbar.Children.Add(toolActions);
            Grid.SetRow(toolbar, 0);
            leftCol.Children.Add(toolbar);

            _canvas = new PatternCanvas();
            _canvas.StrokeStarted += delegate { StopSimulation(); PushUndoState(); };
            _canvas.PatternModified += delegate { RefreshStats(); };
            Grid.SetRow(_canvas, 1);
            leftCol.Children.Add(_canvas);

            _lblHint = new TextBlock
            {
                Foreground = ThemeResources.TextMutedBrush,
                FontSize = 11,
                Margin = new Thickness(2, 8, 2, 0)
            };
            Grid.SetRow(_lblHint, 2);
            leftCol.Children.Add(_lblHint);

            Grid.SetColumn(leftCol, 0);
            body.Children.Add(leftCol);

            // Columna Derecha: Parametros, Plantillas y Resumen
            ScrollViewer rightScroll = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Padding = new Thickness(0, 0, 4, 0)
            };
            StackPanel rightStack = new StackPanel();
            rightStack.Children.Add(BuildParametersCard());
            rightStack.Children.Add(BuildTemplatesCard());
            rightStack.Children.Add(BuildStatsCard());
            rightScroll.Content = rightStack;

            Grid.SetColumn(rightScroll, 2);
            body.Children.Add(rightScroll);

            Grid.SetRow(body, 1);
            root.Children.Add(body);

            // 3. Footer modal
            Border footer = new Border
            {
                Background = ThemeResources.BgSidebarBrush,
                BorderBrush = ThemeResources.BorderSubtleBrush,
                BorderThickness = new Thickness(0, 1, 0, 0),
                Padding = new Thickness(16, 0, 16, 0)
            };
            Grid footerGrid = new Grid();
            footerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            footerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            footerGrid.Children.Add(new TextBlock
            {
                Text = "El trazo se convierte automáticamente en pasos Multi-Fase guardados en el perfil.",
                Foreground = ThemeResources.TextMutedBrush,
                FontSize = 11.5,
                VerticalAlignment = VerticalAlignment.Center
            });

            StackPanel footerBtns = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center
            };
            Button btnCancel = new Button
            {
                Style = (Style)Resources["SecondaryButton"],
                Content = "Cancelar",
                IsCancel = true,
                MinWidth = 92,
                Height = 32,
                Margin = new Thickness(0, 0, 8, 0)
            };
            Button btnApply = new Button
            {
                Style = (Style)Resources["PrimaryButton"],
                Content = "Aplicar al perfil",
                IsDefault = true,
                MinWidth = 132,
                Height = 32
            };
            btnApply.Click += OnApplyClicked;
            footerBtns.Children.Add(btnCancel);
            footerBtns.Children.Add(btnApply);

            Grid.SetColumn(footerBtns, 1);
            footerGrid.Children.Add(footerBtns);
            footer.Child = footerGrid;
            Grid.SetRow(footer, 2);
            root.Children.Add(footer);

            outer.Child = root;
            Content = outer;

            SetEditMode(PatternEditMode.Draw);
        }

        private Border BuildParametersCard()
        {
            Border card = CreateCard();
            StackPanel stack = new StackPanel();
            stack.Children.Add(CreateSectionTitle("PARÁMETROS DE CONVERSIÓN"));

            _sliderDuration = new ModernSlider
            {
                Title = "Duración total",
                Margin = new Thickness(0, 0, 0, -6),
                Minimum = 500.0,
                Maximum = 4000.0,
                Step = 50.0,
                Decimals = 0,
                Suffix = " ms",
                SemanticLabelFunc = delegate(double v)
                {
                    int bullets = _sliderBullets != null ? (int)Math.Round(_sliderBullets.Value) : 30;
                    double rpm = (bullets / Math.Max(0.2, v / 1000.0)) * 60.0;
                    return string.Format(CultureInfo.InvariantCulture, "~{0:0} RPM", rpm);
                }
            };
            _sliderDuration.ValueChanged += delegate(object s, double v)
            {
                if (_isSyncing) return;
                _canvas.TotalDurationMs = v;
                PatternGeometry.RescaleDuration(_canvas.Nodes, v);
                _canvas.InvalidateVisual();
                RefreshStats();
            };
            stack.Children.Add(_sliderDuration);

            _sliderBullets = new ModernSlider
            {
                Title = "Balas (hitos)",
                Margin = new Thickness(0, 0, 0, -6),
                Minimum = 10.0,
                Maximum = 50.0,
                Step = 1.0,
                Decimals = 0,
                SemanticLabelFunc = delegate(double v) { return ((int)Math.Round(v)).ToString() + " u."; }
            };
            _sliderBullets.ValueChanged += delegate(object s, double v)
            {
                if (_isSyncing) return;
                _canvas.BulletCount = (int)Math.Round(v);
                _sliderDuration.RefreshLabels();
            };
            stack.Children.Add(_sliderBullets);

            _sliderScale = new ModernSlider
            {
                Title = "Intensidad / Escala",
                Margin = new Thickness(0, 0, 0, -6),
                Minimum = 0.20,
                Maximum = 3.00,
                Step = 0.05,
                Decimals = 2,
                Suffix = "x",
                SemanticLabelFunc = delegate(double v)
                {
                    if (v < 0.85) return "Suave";
                    if (v <= 1.25) return "Normal";
                    return "Fuerte";
                }
            };
            _sliderScale.ValueChanged += delegate
            {
                if (!_isSyncing) RefreshStats();
            };
            stack.Children.Add(_sliderScale);

            _sliderSegments = new ModernSlider
            {
                Title = "Detalle al dibujar",
                Margin = new Thickness(0, 0, 0, -6),
                Minimum = 4.0,
                Maximum = 24.0,
                Step = 1.0,
                Decimals = 0,
                SemanticLabelFunc = delegate(double v) { return ((int)Math.Round(v)).ToString() + " tramos"; }
            };
            _sliderSegments.ValueChanged += delegate(object s, double v)
            {
                _canvas.TargetSegments = (int)Math.Round(v);
            };
            stack.Children.Add(_sliderSegments);

            _chkStopAtEnd = new CheckBox
            {
                Style = (Style)Resources["DarkCheckBox"],
                Content = "Detener compensación al finalizar el cargador",
                FontSize = 11.5,
                Margin = new Thickness(0, 2, 0, 0)
            };
            _chkStopAtEnd.Checked += delegate { if (!_isSyncing) RefreshStats(); };
            _chkStopAtEnd.Unchecked += delegate { if (!_isSyncing) RefreshStats(); };
            stack.Children.Add(_chkStopAtEnd);

            card.Child = stack;
            return card;
        }

        private Border BuildTemplatesCard()
        {
            Border card = CreateCard();
            StackPanel stack = new StackPanel();
            stack.Children.Add(CreateSectionTitle("PLANTILLAS BASE (ESTILO SPRAY)"));

            Grid grid = new Grid();
            for (int c = 0; c < 3; c++)
            {
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            }
            for (int r = 0; r < 3; r++)
            {
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            }

            for (int i = 0; i < PatternGeometry.TemplateNames.Length; i++)
            {
                string tplName = PatternGeometry.TemplateNames[i];
                Button btn = new Button
                {
                    Style = (Style)Resources["SecondaryButton"],
                    Content = tplName,
                    Height = 28,
                    FontSize = 11,
                    FontWeight = FontWeights.SemiBold,
                    Margin = new Thickness(i % 3 == 0 ? 0 : 4, i / 3 == 0 ? 0 : 5, 0, 0),
                    Padding = new Thickness(4, 2, 4, 2)
                };
                btn.Click += delegate { LoadTemplate(tplName); };
                Grid.SetRow(btn, i / 3);
                Grid.SetColumn(btn, i % 3);
                grid.Children.Add(btn);
            }

            stack.Children.Add(grid);
            card.Child = stack;
            return card;
        }

        private Border BuildStatsCard()
        {
            Border card = CreateCard();
            card.Margin = new Thickness(0);
            StackPanel stack = new StackPanel();
            stack.Children.Add(CreateSectionTitle("RESUMEN DEL PATRÓN"));

            Grid g = new Grid();
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            g.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            g.RowDefinitions.Add(new RowDefinition { Height = new GridLength(8) });
            g.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            _lblStatPhases = CreateStatTile(g, 0, 0, "Fases", "0");
            _lblStatPeakVert = CreateStatTile(g, 2, 0, "Vertical pico", "0.00");
            _lblStatMaxHoriz = CreateStatTile(g, 0, 2, "Desvío máx.", "0.00");
            _lblStatTotalDisp = CreateStatTile(g, 2, 2, "Recorrido", "0 / 0 px");

            stack.Children.Add(g);
            card.Child = stack;
            return card;
        }

        private TextBlock CreateStatTile(Grid parent, int col, int row, string label, string value)
        {
            Border tile = new Border
            {
                Background = ThemeResources.BgInputBrush,
                BorderBrush = ThemeResources.BorderSubtleBrush,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(8, 6, 8, 6)
            };
            StackPanel inner = new StackPanel();
            inner.Children.Add(new TextBlock
            {
                Text = label,
                Foreground = ThemeResources.TextMutedBrush,
                FontSize = 10
            });
            TextBlock val = new TextBlock
            {
                Text = value,
                Foreground = ThemeResources.TextPrimaryBrush,
                FontFamily = new FontFamily("Consolas"),
                FontSize = 11.5,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 2, 0, 0)
            };
            inner.Children.Add(val);
            tile.Child = inner;
            Grid.SetColumn(tile, col);
            Grid.SetRow(tile, row);
            parent.Children.Add(tile);
            return val;
        }

        private Button CreateToolButton(string text, string iconData, Brush iconBrush)
        {
            Button btn = new Button
            {
                Style = (Style)Resources["SecondaryButton"],
                Height = 28,
                Padding = new Thickness(9, 2, 10, 2)
            };
            StackPanel sp = new StackPanel { Orientation = Orientation.Horizontal };
            sp.Children.Add(ThemeResources.CreateVectorIcon(iconData, iconBrush, 11.5, 1.9));
            sp.Children.Add(new TextBlock
            {
                Text = text,
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(5, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center
            });
            btn.Content = sp;
            return btn;
        }

        private Border CreateCard()
        {
            return new Border
            {
                Background = ThemeResources.BgCardBrush,
                BorderBrush = ThemeResources.BorderSubtleBrush,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(9),
                Padding = new Thickness(11, 8, 11, 8),
                Margin = new Thickness(0, 0, 0, 7)
            };
        }

        private TextBlock CreateSectionTitle(string text)
        {
            return new TextBlock
            {
                Text = text,
                Foreground = ThemeResources.TextMutedBrush,
                FontSize = 10.5,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 9)
            };
        }

        private void OnHeaderDrag(object sender, MouseButtonEventArgs e)
        {
            if (e.Handled ||
                e.ChangedButton != MouseButton.Left ||
                e.ButtonState != MouseButtonState.Pressed ||
                Mouse.LeftButton != MouseButtonState.Pressed)
            {
                return;
            }

            DependencyObject cur = e.OriginalSource as DependencyObject;
            while (cur != null)
            {
                if (cur is Button) return;
                cur = VisualTreeHelper.GetParent(cur);
            }

            e.Handled = true;
            try
            {
                DragMove();
            }
            catch (InvalidOperationException)
            {
            }
        }

        private void LoadInitialPattern()
        {
            double detectedDuration;
            bool detectedStop;
            List<PatternNode> nodes = PatternGeometry.ProfileToNodes(_workingProfile, 2000.0, out detectedDuration, out detectedStop);

            _isSyncing = true;
            try
            {
                _sliderDuration.SetValueSilent(Math.Max(500.0, Math.Min(4000.0, detectedDuration)));
                _sliderBullets.SetValueSilent(30.0);
                _sliderScale.SetValueSilent(1.0);
                _sliderSegments.SetValueSilent(10.0);
                _chkStopAtEnd.IsChecked = detectedStop;

                _canvas.TotalDurationMs = _sliderDuration.Value;
                _canvas.BulletCount = 30;
                _canvas.TargetSegments = 10;
                _canvas.SetNodes(nodes, true);
            }
            finally
            {
                _isSyncing = false;
            }

            RefreshStats();
        }

        public void LoadTemplate(string templateName)
        {
            StopSimulation();
            PushUndoState();

            int recBullets;
            double dur = _sliderDuration.Value;
            List<PatternNode> nodes = PatternGeometry.GetTemplateNodes(templateName, dur, out recBullets);

            _isSyncing = true;
            try
            {
                _sliderBullets.SetValueSilent((double)recBullets);
                _canvas.BulletCount = recBullets;
                _sliderDuration.RefreshLabels();
                _chkStopAtEnd.IsChecked = true;
            }
            finally
            {
                _isSyncing = false;
            }

            _canvas.SetNodes(nodes, true);
            RefreshStats();
        }

        private void SetEditMode(PatternEditMode mode)
        {
            _canvas.EditMode = mode;
            _tabDraw.Tag = mode == PatternEditMode.Draw ? "Active" : null;
            _tabDraw.Foreground = mode == PatternEditMode.Draw ? ThemeResources.AccentBrush : ThemeResources.TextSecondaryBrush;
            _tabEdit.Tag = mode == PatternEditMode.EditNodes ? "Active" : null;
            _tabEdit.Foreground = mode == PatternEditMode.EditNodes ? ThemeResources.AccentBrush : ThemeResources.TextSecondaryBrush;

            _lblHint.Text = mode == PatternEditMode.Draw
                ? "Modo Lápiz: haz clic y arrastra hacia abajo para trazar la curva desde el punto 1."
                : "Modo Nodos: arrastra puntos para ajustar, clic izquierdo sobre la línea para insertar, clic derecho para borrar.";
        }

        private void PushUndoState()
        {
            if (_canvas.Nodes == null || _canvas.Nodes.Count == 0) return;
            List<PatternNode> copy = new List<PatternNode>(_canvas.Nodes.Count);
            for (int i = 0; i < _canvas.Nodes.Count; i++)
            {
                copy.Add(_canvas.Nodes[i].Clone());
            }
            _undoStack.Push(copy);
        }

        private void Undo()
        {
            StopSimulation();
            if (_undoStack.Count == 0) return;
            List<PatternNode> prev = _undoStack.Pop();
            _canvas.SetNodes(prev, true);
            RefreshStats();
        }

        private void ToggleSimulation()
        {
            if (_simTimer.IsEnabled)
            {
                StopSimulation();
                return;
            }

            if (_canvas.Nodes == null || _canvas.Nodes.Count < 2) return;
            _simStartTime = DateTime.UtcNow;
            _canvas.SimulationTimeMs = 0.0;
            _simTimer.Start();
            UpdateSimulateButton(true);
        }

        private void StopSimulation()
        {
            _simTimer.Stop();
            _canvas.SimulationTimeMs = -1.0;
            UpdateSimulateButton(false);
        }

        private void UpdateSimulateButton(bool running)
        {
            StackPanel sp = new StackPanel { Orientation = Orientation.Horizontal };
            sp.Children.Add(ThemeResources.CreateVectorIcon(
                running ? ThemeResources.IconStopData : ThemeResources.IconPlayData,
                running ? ThemeResources.WarningBrush : ThemeResources.SuccessBrush,
                11.5,
                1.9));
            sp.Children.Add(new TextBlock
            {
                Text = running ? "Detener" : "Simular",
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(5, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center
            });
            _btnSimulate.Content = sp;
        }

        private void OnSimulationTick(object sender, EventArgs e)
        {
            double elapsed = (DateTime.UtcNow - _simStartTime).TotalMilliseconds;
            double endT = _canvas.Nodes != null && _canvas.Nodes.Count > 1
                ? _canvas.Nodes[_canvas.Nodes.Count - 1].TimeMs
                : _canvas.TotalDurationMs;

            if (elapsed >= endT)
            {
                StopSimulation();
                return;
            }

            _canvas.SimulationTimeMs = elapsed;
        }

        private void RefreshStats()
        {
            List<PatternNode> nodes = _canvas.Nodes;
            if (nodes == null || nodes.Count < 2)
            {
                _lblStatPhases.Text = "0";
                _lblStatPeakVert.Text = "0.00";
                _lblStatMaxHoriz.Text = "0.00";
                _lblStatTotalDisp.Text = "0 / 0 px";
                return;
            }

            WeaponProfile preview = _workingProfile.Clone();
            double scale = _sliderScale.Value;
            bool stop = _chkStopAtEnd.IsChecked == true;
            PatternGeometry.NodesToProfile(nodes, scale, stop, preview);

            double peakV = preview.patea_arriba;
            double maxH = Math.Abs(preview.se_va_lado);
            for (int i = 0; i < preview.secuencias.Count; i++)
            {
                RecoilStep st = preview.secuencias[i];
                if (st.patea_arriba > peakV) peakV = st.patea_arriba;
                if (Math.Abs(st.se_va_lado) > maxH) maxH = Math.Abs(st.se_va_lado);
            }

            PatternNode last = nodes[nodes.Count - 1];
            _lblStatPhases.Text = (preview.secuencias.Count + 1).ToString();
            _lblStatPeakVert.Text = peakV.ToString("F2", CultureInfo.InvariantCulture);
            _lblStatMaxHoriz.Text = maxH.ToString("F2", CultureInfo.InvariantCulture);
            _lblStatTotalDisp.Text = string.Format(
                CultureInfo.InvariantCulture,
                "{0:+0;-0;0} / {1:0} px",
                Math.Round(last.X * scale),
                Math.Round(last.Y * scale));
        }

        private void OnApplyClicked(object sender, RoutedEventArgs e)
        {
            StopSimulation();
            if (_canvas.Nodes == null || _canvas.Nodes.Count < 2)
            {
                ProfileDialog.ShowAlert(this, "Patrón vacío", "Dibuja una curva o selecciona una plantilla antes de aplicar.");
                return;
            }

            PatternGeometry.NodesToProfile(_canvas.Nodes, _sliderScale.Value, _chkStopAtEnd.IsChecked == true, _workingProfile);
            DialogResult = true;
            Close();
        }
    }
}
