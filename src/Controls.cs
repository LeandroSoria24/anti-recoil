using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace RecoilStudio
{
    /// <summary>
    /// Control deslizante moderno con edicion numerica directa, botones de ajuste fino y soporte bidireccional.
    /// </summary>
    public class ModernSlider : UserControl
    {
        private double _minimum = 0.0;
        private double _maximum = 10.0;
        private double _value = 0.0;
        private double _step = 0.05;
        private double _displayMultiplier = 1.0;
        private int _decimals = 2;
        private string _suffix = "";
        private bool _isBidirectional = false;
        private bool _showZeroButton = false;
        private bool _isDragging = false;
        private bool _suppressEvents = false;

        private TextBlock _lblTitle;
        private Border _badgeSemantic;
        private TextBlock _lblSemantic;
        private TextBlock _lblSubtitle;
        private TextBox _txtValue;
        private Button _btnZero;
        private Grid _trackGrid;
        private Border _trackBg;
        private Border _trackFill;
        private Border _zeroMarker;
        private Ellipse _thumbGlow;
        private Ellipse _thumb;

        public event EventHandler<double> ValueChanged;

        public Func<double, string> SemanticLabelFunc { get; set; }
        public Func<double, string> SubtitleFunc { get; set; }

        public string Title
        {
            get { return _lblTitle.Text; }
            set { _lblTitle.Text = value ?? ""; }
        }

        public double Minimum
        {
            get { return _minimum; }
            set { _minimum = value; UpdateVisuals(); }
        }

        public double Maximum
        {
            get { return _maximum; }
            set { _maximum = value; UpdateVisuals(); }
        }

        public double Step
        {
            get { return _step; }
            set { _step = value > 0.0 ? value : 0.05; }
        }

        public double DisplayMultiplier
        {
            get { return _displayMultiplier; }
            set { _displayMultiplier = value > 0.0 ? value : 1.0; UpdateTextFromValue(); }
        }

        public int Decimals
        {
            get { return _decimals; }
            set { _decimals = Math.Max(0, value); UpdateTextFromValue(); }
        }

        public string Suffix
        {
            get { return _suffix; }
            set { _suffix = value ?? ""; UpdateTextFromValue(); }
        }

        public bool IsBidirectional
        {
            get { return _isBidirectional; }
            set { _isBidirectional = value; UpdateVisuals(); }
        }

        public bool ShowZeroButton
        {
            get { return _showZeroButton; }
            set
            {
                _showZeroButton = value;
                _btnZero.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        public double Value
        {
            get { return _value; }
            set
            {
                double clamped = Clamp(value, _minimum, _maximum);
                if (Math.Abs(clamped - _value) < 0.00001)
                {
                    UpdateVisuals();
                    return;
                }
                _value = clamped;
                UpdateVisuals();
                if (!_suppressEvents && ValueChanged != null)
                {
                    ValueChanged(this, _value);
                }
            }
        }

        public ModernSlider()
        {
            BuildUI();
            SizeChanged += delegate { UpdateVisuals(); };
            Loaded += delegate { UpdateVisuals(); };
        }

        public void SetValueSilent(double value)
        {
            _suppressEvents = true;
            try
            {
                _value = Clamp(value, _minimum, _maximum);
                UpdateVisuals();
            }
            finally
            {
                _suppressEvents = false;
            }
        }

        private void BuildUI()
        {
            StackPanel root = new StackPanel();
            root.Margin = new Thickness(0, 0, 0, 12);

            // Fila superior: Titulo, badge semantico, botones y caja numerica
            Grid header = new Grid();
            header.Margin = new Thickness(0, 0, 0, 6);
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            StackPanel leftStack = new StackPanel();
            leftStack.Orientation = Orientation.Horizontal;
            leftStack.VerticalAlignment = VerticalAlignment.Center;

            _lblTitle = new TextBlock
            {
                Text = "Parametro",
                Foreground = ThemeResources.TextPrimaryBrush,
                FontSize = 12.5,
                FontWeight = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center
            };
            leftStack.Children.Add(_lblTitle);

            _badgeSemantic = new Border
            {
                Background = ThemeResources.AccentSoftBrush,
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(6, 2, 6, 2),
                Margin = new Thickness(8, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Visibility = Visibility.Collapsed
            };
            _lblSemantic = new TextBlock
            {
                Foreground = ThemeResources.AccentBrush,
                FontSize = 10.5,
                FontWeight = FontWeights.SemiBold
            };
            _badgeSemantic.Child = _lblSemantic;
            leftStack.Children.Add(_badgeSemantic);

            Grid.SetColumn(leftStack, 0);
            header.Children.Add(leftStack);

            StackPanel rightStack = new StackPanel();
            rightStack.Orientation = Orientation.Horizontal;
            rightStack.VerticalAlignment = VerticalAlignment.Center;

            _btnZero = CreateMiniButton("0", "Restablecer a 0");
            _btnZero.Visibility = Visibility.Collapsed;
            _btnZero.Margin = new Thickness(0, 0, 4, 0);
            _btnZero.Click += delegate { Value = 0.0; };
            rightStack.Children.Add(_btnZero);

            Button btnDec = CreateMiniButton("-", "Disminuir");
            btnDec.Margin = new Thickness(0, 0, 4, 0);
            btnDec.Click += delegate { Value = _value - _step; };
            rightStack.Children.Add(btnDec);

            _txtValue = new TextBox
            {
                Width = 68,
                Height = 24,
                Background = ThemeResources.BgInputBrush,
                Foreground = ThemeResources.TextPrimaryBrush,
                BorderBrush = ThemeResources.BorderSubtleBrush,
                BorderThickness = new Thickness(1),
                FontFamily = new FontFamily("Consolas"),
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                TextAlignment = TextAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center,
                CaretBrush = ThemeResources.AccentBrush,
                Padding = new Thickness(2, 0, 2, 0),
                Template = CreateRoundedTextBoxTemplate()
            };
            _txtValue.GotKeyboardFocus += delegate { _txtValue.SelectAll(); };
            _txtValue.LostKeyboardFocus += delegate { CommitTextInput(); };
            _txtValue.KeyDown += OnTextKeyDown;
            rightStack.Children.Add(_txtValue);

            Button btnInc = CreateMiniButton("+", "Aumentar");
            btnInc.Margin = new Thickness(4, 0, 0, 0);
            btnInc.Click += delegate { Value = _value + _step; };
            rightStack.Children.Add(btnInc);

            Grid.SetColumn(rightStack, 1);
            header.Children.Add(rightStack);
            root.Children.Add(header);

            // Pista del slider
            _trackGrid = new Grid
            {
                Height = 24,
                Background = Brushes.Transparent,
                Cursor = Cursors.Hand,
                Focusable = true,
                FocusVisualStyle = null
            };

            _trackBg = new Border
            {
                Height = 6,
                CornerRadius = new CornerRadius(3),
                Background = new SolidColorBrush(Color.FromRgb(34, 40, 56)),
                VerticalAlignment = VerticalAlignment.Center
            };
            _trackGrid.Children.Add(_trackBg);

            _trackFill = new Border
            {
                Height = 6,
                CornerRadius = new CornerRadius(3),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
                Background = new LinearGradientBrush(
                    ThemeResources.AccentColor,
                    ThemeResources.AccentHoverColor,
                    0.0)
            };
            _trackGrid.Children.Add(_trackFill);

            _zeroMarker = new Border
            {
                Width = 2,
                Height = 14,
                CornerRadius = new CornerRadius(1),
                Background = new SolidColorBrush(Color.FromRgb(90, 102, 128)),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
                Visibility = Visibility.Collapsed
            };
            _trackGrid.Children.Add(_zeroMarker);

            _thumbGlow = new Ellipse
            {
                Width = 22,
                Height = 22,
                Fill = new SolidColorBrush(Color.FromArgb(45, 56, 189, 248)),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
                Visibility = Visibility.Collapsed,
                IsHitTestVisible = false
            };
            _trackGrid.Children.Add(_thumbGlow);

            _thumb = new Ellipse
            {
                Width = 14,
                Height = 14,
                Fill = Brushes.White,
                Stroke = ThemeResources.AccentBrush,
                StrokeThickness = 2,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
                IsHitTestVisible = false
            };
            _trackGrid.Children.Add(_thumb);

            _trackGrid.MouseEnter += delegate { _thumbGlow.Visibility = Visibility.Visible; };
            _trackGrid.MouseLeave += delegate
            {
                if (!_isDragging) _thumbGlow.Visibility = Visibility.Collapsed;
            };
            _trackGrid.MouseLeftButtonDown += OnTrackMouseDown;
            _trackGrid.MouseMove += OnTrackMouseMove;
            _trackGrid.MouseLeftButtonUp += OnTrackMouseUp;
            _trackGrid.MouseWheel += OnTrackMouseWheel;

            root.Children.Add(_trackGrid);

            _lblSubtitle = new TextBlock
            {
                Foreground = ThemeResources.TextMutedBrush,
                FontSize = 11,
                Margin = new Thickness(0, 2, 0, 0),
                Visibility = Visibility.Collapsed
            };
            root.Children.Add(_lblSubtitle);

            Content = root;
            UpdateVisuals();
        }

        private Button CreateMiniButton(string text, string tooltip)
        {
            ControlTemplate tpl = new ControlTemplate(typeof(Button));
            FrameworkElementFactory bd = new FrameworkElementFactory(typeof(Border));
            bd.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Button.BackgroundProperty));
            bd.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(Button.BorderBrushProperty));
            bd.SetValue(Border.BorderThicknessProperty, new TemplateBindingExtension(Button.BorderThicknessProperty));
            bd.SetValue(Border.CornerRadiusProperty, new CornerRadius(5));

            FrameworkElementFactory cp = new FrameworkElementFactory(typeof(ContentPresenter));
            cp.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            cp.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            bd.AppendChild(cp);
            tpl.VisualTree = bd;

            Button btn = new Button
            {
                Content = text,
                ToolTip = tooltip,
                Width = 24,
                Height = 24,
                Background = ThemeResources.BgInputBrush,
                Foreground = ThemeResources.TextSecondaryBrush,
                BorderBrush = ThemeResources.BorderSubtleBrush,
                BorderThickness = new Thickness(1),
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Padding = new Thickness(0),
                Cursor = Cursors.Hand,
                FocusVisualStyle = null,
                Template = tpl
            };
            btn.MouseEnter += delegate
            {
                btn.Background = ThemeResources.BgInputHoverBrush;
                btn.Foreground = ThemeResources.TextPrimaryBrush;
                btn.BorderBrush = ThemeResources.BorderStrongBrush;
            };
            btn.MouseLeave += delegate
            {
                btn.Background = ThemeResources.BgInputBrush;
                btn.Foreground = ThemeResources.TextSecondaryBrush;
                btn.BorderBrush = ThemeResources.BorderSubtleBrush;
            };
            return btn;
        }

        private static ControlTemplate CreateRoundedTextBoxTemplate()
        {
            ControlTemplate tpl = new ControlTemplate(typeof(TextBox));
            FrameworkElementFactory bd = new FrameworkElementFactory(typeof(Border));
            bd.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(TextBox.BackgroundProperty));
            bd.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(TextBox.BorderBrushProperty));
            bd.SetValue(Border.BorderThicknessProperty, new TemplateBindingExtension(TextBox.BorderThicknessProperty));
            bd.SetValue(Border.CornerRadiusProperty, new CornerRadius(5));

            FrameworkElementFactory sv = new FrameworkElementFactory(typeof(ScrollViewer));
            sv.Name = "PART_ContentHost";
            sv.SetValue(ScrollViewer.MarginProperty, new Thickness(0));
            sv.SetValue(ScrollViewer.VerticalAlignmentProperty, VerticalAlignment.Center);
            bd.AppendChild(sv);

            tpl.VisualTree = bd;
            return tpl;
        }

        private void OnTrackMouseDown(object sender, MouseButtonEventArgs e)
        {
            _trackGrid.Focus();
            _isDragging = true;
            _thumbGlow.Visibility = Visibility.Visible;
            _trackGrid.CaptureMouse();
            UpdateFromPoint(e.GetPosition(_trackGrid).X);
        }

        private void OnTrackMouseMove(object sender, MouseEventArgs e)
        {
            if (_isDragging)
            {
                UpdateFromPoint(e.GetPosition(_trackGrid).X);
            }
        }

        private void OnTrackMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDragging)
            {
                _isDragging = false;
                _trackGrid.ReleaseMouseCapture();
                if (!_trackGrid.IsMouseOver)
                {
                    _thumbGlow.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void OnTrackMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (_isDragging || _trackGrid.IsKeyboardFocused || _txtValue.IsKeyboardFocused)
            {
                double delta = e.Delta > 0 ? _step : -_step;
                Value = _value + delta;
                e.Handled = true;
            }
        }

        private void OnTextKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                CommitTextInput();
                Keyboard.ClearFocus();
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                UpdateTextFromValue();
                Keyboard.ClearFocus();
                e.Handled = true;
            }
        }

        private void CommitTextInput()
        {
            string raw = (_txtValue.Text ?? "").Trim();
            if (!string.IsNullOrEmpty(_suffix))
            {
                raw = raw.Replace(_suffix.Trim(), "");
            }
            raw = raw.Replace(",", ".");

            double parsed;
            if (double.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out parsed))
            {
                Value = parsed / _displayMultiplier;
            }
            else
            {
                UpdateTextFromValue();
            }
        }

        private void UpdateFromPoint(double x)
        {
            double width = _trackGrid.ActualWidth;
            if (width <= 16.0) return;

            double usable = width - 14.0;
            double pct = Clamp((x - 7.0) / usable, 0.0, 1.0);
            double newVal = _minimum + pct * (_maximum - _minimum);

            // Ajuste suave a multiplos pequenos cuando no se presiona Shift
            if ((Keyboard.Modifiers & ModifierKeys.Shift) == 0 && _step >= 1.0)
            {
                newVal = Math.Round(newVal / _step) * _step;
            }

            Value = newVal;
        }

        public void RefreshLabels()
        {
            UpdateVisuals();
        }

        private void UpdateTextFromValue()
        {
            if (_txtValue == null || _txtValue.IsKeyboardFocused) return;
            double shown = _value * _displayMultiplier;
            string fmt = "F" + _decimals.ToString();
            _txtValue.Text = shown.ToString(fmt, CultureInfo.InvariantCulture) + _suffix;
        }

        private void UpdateVisuals()
        {
            UpdateTextFromValue();

            if (SemanticLabelFunc != null)
            {
                string sem = SemanticLabelFunc(_value);
                if (!string.IsNullOrEmpty(sem))
                {
                    _lblSemantic.Text = sem;
                    _badgeSemantic.Visibility = Visibility.Visible;
                }
                else
                {
                    _badgeSemantic.Visibility = Visibility.Collapsed;
                }
            }

            if (SubtitleFunc != null)
            {
                string sub = SubtitleFunc(_value);
                if (!string.IsNullOrEmpty(sub))
                {
                    _lblSubtitle.Text = sub;
                    _lblSubtitle.Visibility = Visibility.Visible;
                }
                else
                {
                    _lblSubtitle.Visibility = Visibility.Collapsed;
                }
            }

            double width = _trackGrid.ActualWidth;
            if (width <= 14.0) return;

            double range = _maximum - _minimum;
            if (range <= 0.0) return;

            double usable = width - 14.0;
            double pct = Clamp((_value - _minimum) / range, 0.0, 1.0);
            double thumbCenter = 7.0 + pct * usable;

            _thumb.Margin = new Thickness(thumbCenter - 7.0, 0, 0, 0);
            _thumbGlow.Margin = new Thickness(thumbCenter - 11.0, 0, 0, 0);

            if (_isBidirectional && _minimum < 0.0 && _maximum > 0.0)
            {
                double zeroPct = (0.0 - _minimum) / range;
                double zeroX = 7.0 + zeroPct * usable;

                _zeroMarker.Visibility = Visibility.Visible;
                _zeroMarker.Margin = new Thickness(zeroX - 1.0, 0, 0, 0);

                double left = Math.Min(zeroX, thumbCenter);
                double fillW = Math.Abs(thumbCenter - zeroX);
                _trackFill.Margin = new Thickness(left, 0, 0, 0);
                _trackFill.Width = Math.Max(0.0, fillW);
            }
            else
            {
                _zeroMarker.Visibility = Visibility.Collapsed;
                _trackFill.Margin = new Thickness(0);
                _trackFill.Width = Math.Max(6.0, thumbCenter);
            }
        }

        private static double Clamp(double val, double min, double max)
        {
            if (val < min) return min;
            if (val > max) return max;
            return val;
        }
    }

    /// <summary>
    /// Visualizador vectorial en tiempo real de la curva de recoil y compensacion durante 1.5 segundos.
    /// </summary>
    public class RecoilVisualizer : FrameworkElement
    {
        private WeaponProfile _profile;
        private RecoilStep[] _sortedSteps;
        private EngineTelemetry _telemetry = EngineTelemetry.Idle;

        private static readonly Brush CanvasBgBrush = FreezeBrush(Color.FromRgb(12, 15, 22));
        private static readonly Pen BorderPen = FreezePen(Color.FromRgb(37, 42, 56), 1.0);
        private static readonly Pen GridPen = FreezePen(Color.FromRgb(22, 27, 38), 1.0);
        private static readonly Pen AxisPen = FreezePen(Color.FromRgb(40, 48, 66), 1.0);
        private static readonly Pen NodeStrokePen = FreezePen(Color.FromRgb(12, 15, 22), 1.4);
        private static readonly Brush PhaseNodeBrush = FreezeBrush(Color.FromRgb(245, 158, 11));
        private static readonly Brush BulletDotBrush = FreezeBrush(Color.FromRgb(248, 250, 252));
        private static readonly Brush ActiveDotBrush = FreezeBrush(Color.FromRgb(34, 197, 94));
        private static readonly Brush ActiveGlowBrush = FreezeBrush(Color.FromArgb(85, 34, 197, 94));
        private static readonly Typeface MonoTypeface = new Typeface(new FontFamily("Consolas"), FontStyles.Normal, FontWeights.SemiBold, FontStretches.Normal);
        private static readonly Typeface UiTypeface = new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.SemiBold, FontStretches.Normal);

        public RecoilVisualizer()
        {
            ClipToBounds = true;
        }

        public void SetProfile(WeaponProfile profile)
        {
            _profile = profile != null ? profile.Clone() : null;
            _sortedSteps = ConfigService.BuildSortedSteps(_profile);
            InvalidateVisual();
        }

        public void SetTelemetry(EngineTelemetry telemetry)
        {
            EngineTelemetry next = telemetry ?? EngineTelemetry.Idle;
            bool wasActive = _telemetry != null && _telemetry.IsCompensating;
            bool isActive = next.IsCompensating;
            _telemetry = next;

            if (wasActive || isActive)
            {
                InvalidateVisual();
            }
        }

        protected override void OnRender(DrawingContext dc)
        {
            double w = ActualWidth;
            double h = ActualHeight;
            if (w < 20.0 || h < 20.0) return;

            // Fondo y borde redondeado
            dc.DrawRoundedRectangle(CanvasBgBrush, BorderPen, new Rect(0.5, 0.5, w - 1.0, h - 1.0), 8.0, 8.0);

            if (_profile == null)
            {
                return;
            }

            // Calcular duracion representativa del perfil
            double simDurationMs = 1500.0;
            if (_profile.cambia_de_lado)
            {
                if (_sortedSteps != null && _sortedSteps.Length > 0)
                {
                    RecoilStep lastStep = _sortedSteps[_sortedSteps.Length - 1];
                    if (Math.Abs(lastStep.patea_arriba) < 0.001 && Math.Abs(lastStep.se_va_lado) < 0.001)
                    {
                        simDurationMs = Clamp(lastStep.tiempo_ms, 300.0, 4000.0);
                    }
                    else
                    {
                        simDurationMs = Clamp(Math.Max(1500.0, lastStep.tiempo_ms + 300.0), 300.0, 4000.0);
                    }
                }
                else
                {
                    simDurationMs = Clamp(Math.Max(1500.0, _profile.tiempo_cambio_ms + 500.0), 500.0, 4000.0);
                }
            }

            const double tickMs = 8.0;
            int totalTicks = Math.Max(1, (int)Math.Ceiling(simDurationMs / tickMs));

            Point[] compPoints = new Point[totalTicks + 1];
            int[] phases = new int[totalTicks + 1];

            double curX = 0.0;
            double curY = 0.0;
            double minX = 0.0;
            double maxX = 0.0;
            double minY = 0.0;
            double maxY = 0.0;

            compPoints[0] = new Point(0, 0);
            phases[0] = -1;

            for (int i = 1; i <= totalTicks; i++)
            {
                double t = i * tickMs;
                double dx;
                double dy;
                int phaseIdx;
                RecoilEngine.EvaluateVector(_profile, _sortedSteps, t, out dx, out dy, out phaseIdx);

                curX += dx;
                curY += dy;

                compPoints[i] = new Point(curX, curY);
                phases[i] = phaseIdx;

                if (curX < minX) minX = curX;
                if (curX > maxX) maxX = curX;
                if (curY < minY) minY = curY;
                if (curY > maxY) maxY = curY;
            }

            double availW = Math.Max(40.0, w - 52.0);
            double availH = Math.Max(40.0, h - 56.0);
            double spanX = Math.Max(45.0, maxX - minX);
            double spanY = Math.Max(45.0, maxY - minY);
            double scale = Math.Min(availW / spanX, availH / spanY);

            double ox = w * 0.5 - ((minX + maxX) * 0.5) * scale;
            double oy = 22.0 + (availH - (maxY - minY) * scale) * 0.5 - minY * scale;

            // Cuadricula sutil alineada con el origen 1
            double stepGrid = 28.0;
            for (double x = (ox % stepGrid + stepGrid) % stepGrid; x < w - 4.0; x += stepGrid)
            {
                dc.DrawLine(GridPen, new Point(x, 6), new Point(x, h - 6));
            }
            for (double y = (oy % stepGrid + stepGrid) % stepGrid; y < h - 4.0; y += stepGrid)
            {
                dc.DrawLine(GridPen, new Point(6, y), new Point(w - 6, y));
            }

            dc.DrawLine(AxisPen, new Point(8, oy), new Point(w - 8, oy));
            dc.DrawLine(AxisPen, new Point(ox, 8), new Point(ox, h - 8));

            // Curva degradada ambar -> naranja -> rojo estilo patron numerado
            int stride = Math.Max(1, totalTicks / 75);
            List<Point> sampledPts = new List<Point>();
            sampledPts.Add(new Point(ox + compPoints[0].X * scale, oy + compPoints[0].Y * scale));
            for (int i = stride; i < totalTicks; i += stride)
            {
                sampledPts.Add(new Point(ox + compPoints[i].X * scale, oy + compPoints[i].Y * scale));
            }
            sampledPts.Add(new Point(ox + compPoints[totalTicks].X * scale, oy + compPoints[totalTicks].Y * scale));

            int segCount = sampledPts.Count - 1;
            for (int i = 0; i < segCount; i++)
            {
                double frac = (double)i / Math.Max(1, segCount - 1);
                Color col = PatternGeometry.GetColorAlongCurve(frac);

                Pen glowPen = new Pen(new SolidColorBrush(Color.FromArgb(46, col.R, col.G, col.B)), 5.4);
                glowPen.StartLineCap = PenLineCap.Round;
                glowPen.EndLineCap = PenLineCap.Round;

                Pen corePen = new Pen(new SolidColorBrush(col), 2.3);
                corePen.StartLineCap = PenLineCap.Round;
                corePen.EndLineCap = PenLineCap.Round;

                dc.DrawLine(glowPen, sampledPts[i], sampledPts[i + 1]);
                dc.DrawLine(corePen, sampledPts[i], sampledPts[i + 1]);
            }

            // Contar transiciones de fase para decidir densidad de etiquetas
            int transitionCount = 0;
            for (int i = 1; i <= totalTicks; i++)
            {
                if (phases[i] != phases[i - 1] && phases[i] >= 0) transitionCount++;
            }

            if (transitionCount <= 3)
            {
                for (int i = 1; i <= totalTicks; i++)
                {
                    if (phases[i] != phases[i - 1] && phases[i] >= 0)
                    {
                        Point pt = new Point(ox + compPoints[i].X * scale, oy + compPoints[i].Y * scale);
                        dc.DrawEllipse(PhaseNodeBrush, NodeStrokePen, pt, 3.6, 3.6);

                        string phaseTimeText = ((int)Math.Round(i * tickMs)).ToString() + "ms";
                        FormattedText ft = CreateText(phaseTimeText, MonoTypeface, 9.5, ThemeResources.WarningBrush);
                        double tx = pt.X + 7.0;
                        if (tx + ft.Width > w - 8.0) tx = pt.X - ft.Width - 7.0;
                        double ty = pt.Y - ft.Height * 0.5;
                        dc.DrawText(ft, new Point(tx, ty));
                    }
                }
            }
            else
            {
                // Hito intermedio discreto para curvas multi-fase densas
                int midIdx = totalTicks / 2;
                Point midPt = new Point(ox + compPoints[midIdx].X * scale, oy + compPoints[midIdx].Y * scale);
                dc.DrawEllipse(BulletDotBrush, NodeStrokePen, midPt, 2.8, 2.8);
            }

            // Punto de origen "1" arriba
            Point startPt = new Point(ox, oy);
            dc.DrawEllipse(BulletDotBrush, NodeStrokePen, startPt, 3.4, 3.4);
            FormattedText oneFt = CreateText("1", MonoTypeface, 10.5, ThemeResources.TextPrimaryBrush);
            dc.DrawText(oneFt, new Point(startPt.X + 7.0, Math.Max(6.0, startPt.Y - 7.0)));

            // Punto final
            Point endPt = new Point(ox + compPoints[totalTicks].X * scale, oy + compPoints[totalTicks].Y * scale);
            dc.DrawEllipse(ThemeResources.DangerBrush, NodeStrokePen, endPt, 3.4, 3.4);

            // Marcador en vivo durante la rafaga activa
            if (_telemetry != null && _telemetry.IsCompensating)
            {
                double clampedMs = Math.Min(_telemetry.ElapsedBurstMs, simDurationMs);
                int idx = (int)Math.Min(totalTicks, Math.Max(0, Math.Round(clampedMs / tickMs)));
                Point activePt = new Point(ox + compPoints[idx].X * scale, oy + compPoints[idx].Y * scale);

                dc.DrawEllipse(ActiveGlowBrush, null, activePt, 9.0, 9.0);
                dc.DrawEllipse(ActiveDotBrush, null, activePt, 4.5, 4.5);

                FormattedText liveFt = CreateText("EN VIVO", UiTypeface, 9.5, ThemeResources.SuccessBrush);
                dc.DrawRoundedRectangle(ThemeResources.SuccessSoftBrush, null, new Rect(w - liveFt.Width - 18, 8, liveFt.Width + 10, liveFt.Height + 4), 4, 4);
                dc.DrawText(liveFt, new Point(w - liveFt.Width - 13, 10));
            }

            // Metrica inferior discreta de desplazamiento total estimado
            string info = string.Format(
                CultureInfo.InvariantCulture,
                "{0:F1}s  ΔX {1:+0;-0;0}px  ΔY {2:0}px",
                simDurationMs / 1000.0,
                Math.Round(curX),
                Math.Round(curY));
            FormattedText infoFt = CreateText(info, MonoTypeface, 10.0, ThemeResources.TextMutedBrush);
            dc.DrawText(infoFt, new Point(10, h - infoFt.Height - 7));
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
            return new FormattedText(
                text,
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                typeface,
                size,
                brush,
                dpi);
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
            p.LineJoin = PenLineJoin.Round;
            p.Freeze();
            return p;
        }

        private static double Clamp(double val, double min, double max)
        {
            if (val < min) return min;
            if (val > max) return max;
            return val;
        }
    }
}
