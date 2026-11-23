using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace RecoilStudio
{
    public class ProfileListItem
    {
        public string Name { get; set; }
        public string VerticalText { get; set; }
        public string ModeTag { get; set; }
        public Visibility ModeVisibility { get; set; }
        public Visibility MacroVisibility { get; set; }
    }

    public class MainWindow : Window
    {
        private enum CaptureTarget
        {
            None = 0,
            ToggleKey = 1,
            MacroKey = 2
        }

        private readonly string _configPath;
        private RecoilConfig _config;
        private RecoilEngine _engine;

        private readonly DispatcherTimer _tickTimer;
        private readonly DispatcherTimer _saveDebounceTimer;

        private bool _isSyncing = false;
        private bool _hasPendingSave = false;
        private bool _isPinned = false;

        // Estado de hotkeys (deteccion por flanco de subida)
        private bool _prevToggleDown = false;
        private bool _prevMacroDown = false;
        private CaptureTarget _captureTarget = CaptureTarget.None;
        private bool _captureWaitRelease = false;
        private bool _lastVisualCompensating = false;
        private bool _lastVisualEnabled = true;
        private int _lastVisualToggleKey = -1;

        // Coleccion filtrada de perfiles
        private readonly ObservableCollection<ProfileListItem> _profileItems = new ObservableCollection<ProfileListItem>();
        private ListBox _listProfiles;
        private TextBox _txtSearch;
        private Button _btnClearSearch;
        private TextBlock _lblProfileCount;

        // Barra de titulo y pildora de estado
        private Border _pillEngineStatus;
        private Ellipse _dotEngineStatus;
        private TextBlock _lblEngineStatus;
        private TextBlock _lblEngineHotkeyChip;
        private Button _btnPin;

        // Cabecera del perfil activo
        private TextBlock _lblActiveName;
        private Border _badgeActiveMode;
        private TextBlock _lblActiveMode;
        private TextBlock _lblDirectionSummary;
        private TextBlock _lblSaveState;

        // Controles de calibracion
        private ModernSlider _sliderVertical;
        private ModernSlider _sliderHorizontal;
        private ModernSlider _sliderHumanize;

        // Selector segmentado de fases
        private Button _tabLinear;
        private Button _tabTwoPhase;
        private Button _tabMultiPhase;
        private Border _panelLinearInfo;
        private StackPanel _panelTwoPhase;
        private StackPanel _panelMultiPhase;
        private ModernSlider _sliderSwitchTime;
        private ModernSlider _sliderPhase2Side;
        private StackPanel _multiPhaseRowsPanel;

        // Atajos globales y macro
        private Button _btnToggleKey;
        private Button _btnMacroKey;
        private ComboBox _comboMacroB;

        // Visualizador y telemetria
        private RecoilVisualizer _visualizer;
        private TextBlock _lblTelState;
        private TextBlock _lblTelBurst;
        private TextBlock _lblTelVector;
        private TextBlock _lblTelPhase;

        public MainWindow()
        {
            _configPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");
            Resources.MergedDictionaries.Add(ThemeResources.CreateThemeDictionary());

            _config = ConfigService.Load(_configPath);
            _engine = new RecoilEngine(ConfigService.CreateSnapshot(_config));

            ConfigureWindow();
            BuildLayout();

            _saveDebounceTimer = new DispatcherTimer();
            _saveDebounceTimer.Interval = TimeSpan.FromMilliseconds(350);
            _saveDebounceTimer.Tick += delegate
            {
                _saveDebounceTimer.Stop();
                FlushSave();
            };

            _tickTimer = new DispatcherTimer();
            _tickTimer.Interval = TimeSpan.FromMilliseconds(25);
            _tickTimer.Tick += OnMonitorTick;

            RefreshProfileList();
            SyncMacroBCombo();
            SyncActiveProfileToUI();
            UpdateEngineStatusPill(true);

            _engine.Start();
            _tickTimer.Start();

            Loaded += delegate
            {
                if (_listProfiles.SelectedItem != null)
                {
                    _listProfiles.ScrollIntoView(_listProfiles.SelectedItem);
                }
            };
            Closing += OnWindowClosing;
        }

        private void ConfigureWindow()
        {
            Title = "Recoil Studio";
            Width = 1040;
            Height = 690;
            MinWidth = 920;
            MinHeight = 610;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            ResizeMode = ResizeMode.CanResizeWithGrip;
            FontFamily = new FontFamily("Segoe UI");
            Foreground = ThemeResources.TextPrimaryBrush;
        }

        private void BuildLayout()
        {
            Border rootFrame = new Border
            {
                Background = ThemeResources.BgCanvasBrush,
                BorderBrush = ThemeResources.BorderStrongBrush,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                ClipToBounds = true
            };

            Grid mainGrid = new Grid();
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(48) });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            // 1. Barra de Titulo
            Border titleBar = BuildTitleBar();
            Grid.SetRow(titleBar, 0);
            mainGrid.Children.Add(titleBar);

            // 2. Contenido (Sidebar izquierda + Espacio de trabajo derecha)
            Grid contentGrid = new Grid();
            contentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(282) });
            contentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            Border sidebar = BuildSidebar();
            Grid.SetColumn(sidebar, 0);
            contentGrid.Children.Add(sidebar);

            Grid workspace = BuildWorkspace();
            Grid.SetColumn(workspace, 1);
            contentGrid.Children.Add(workspace);

            Grid.SetRow(contentGrid, 1);
            mainGrid.Children.Add(contentGrid);

            rootFrame.Child = mainGrid;
            Content = rootFrame;
        }

        private Border BuildTitleBar()
        {
            Border bar = new Border
            {
                Background = ThemeResources.BgSidebarBrush,
                BorderBrush = ThemeResources.BorderSubtleBrush,
                BorderThickness = new Thickness(0, 0, 0, 1),
                CornerRadius = new CornerRadius(10, 10, 0, 0),
                Padding = new Thickness(16, 0, 12, 0)
            };

            Grid grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // Marca izquierda (zona arrastrable manejada por burbujeo en bar)
            StackPanel brand = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center,
                Background = Brushes.Transparent
            };

            Border logoBox = new Border
            {
                Width = 28,
                Height = 28,
                CornerRadius = new CornerRadius(7),
                Background = ThemeResources.AccentSoftBrush,
                BorderBrush = new SolidColorBrush(Color.FromArgb(80, 56, 189, 248)),
                BorderThickness = new Thickness(1),
                Margin = new Thickness(0, 0, 10, 0),
                Child = ThemeResources.CreateVectorIcon(ThemeResources.IconCrosshairData, ThemeResources.AccentBrush, 16, 1.8)
            };
            brand.Children.Add(logoBox);

            StackPanel titleStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            titleStack.Children.Add(new TextBlock
            {
                Text = "Recoil Studio",
                Foreground = ThemeResources.TextPrimaryBrush,
                FontSize = 13.5,
                FontWeight = FontWeights.Bold
            });
            titleStack.Children.Add(new TextBlock
            {
                Text = "Compensador de Perfil",
                Foreground = ThemeResources.TextMutedBrush,
                FontSize = 10.5
            });
            brand.Children.Add(titleStack);

            Grid.SetColumn(brand, 0);
            grid.Children.Add(brand);

            // Pildora interactiva de estado del motor
            _pillEngineStatus = new Border
            {
                CornerRadius = new CornerRadius(14),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(10, 4, 10, 4),
                Margin = new Thickness(0, 0, 14, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Cursor = Cursors.Hand,
                ToolTip = "Clic para activar o pausar el motor de compensación"
            };
            _pillEngineStatus.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e)
            {
                e.Handled = true;
                ToggleEngineEnabled();
            };

            StackPanel pillStack = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            _dotEngineStatus = new Ellipse
            {
                Width = 8,
                Height = 8,
                Margin = new Thickness(0, 0, 7, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            pillStack.Children.Add(_dotEngineStatus);

            _lblEngineStatus = new TextBlock
            {
                FontSize = 11.5,
                FontWeight = FontWeights.Bold,
                VerticalAlignment = VerticalAlignment.Center
            };
            pillStack.Children.Add(_lblEngineStatus);

            Border sep = new Border
            {
                Width = 1,
                Height = 12,
                Background = ThemeResources.BorderStrongBrush,
                Margin = new Thickness(8, 0, 8, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            pillStack.Children.Add(sep);

            _lblEngineHotkeyChip = new TextBlock
            {
                Foreground = ThemeResources.TextSecondaryBrush,
                FontFamily = new FontFamily("Consolas"),
                FontSize = 11,
                VerticalAlignment = VerticalAlignment.Center
            };
            pillStack.Children.Add(_lblEngineHotkeyChip);

            _pillEngineStatus.Child = pillStack;
            Grid.SetColumn(_pillEngineStatus, 1);
            grid.Children.Add(_pillEngineStatus);

            // Botones de ventana: Pin, Minimizar, Cerrar
            StackPanel winBtns = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center
            };

            _btnPin = new Button
            {
                Style = (Style)Resources["TitleBarButton"],
                Content = ThemeResources.CreateVectorIcon(ThemeResources.IconPinData, ThemeResources.TextSecondaryBrush, 13, 1.8),
                ToolTip = "Mantener ventana siempre encima",
                Margin = new Thickness(0, 0, 4, 0)
            };
            _btnPin.Click += delegate
            {
                _isPinned = !_isPinned;
                Topmost = _isPinned;
                _btnPin.Background = _isPinned ? ThemeResources.AccentSoftBrush : Brushes.Transparent;
                _btnPin.Content = ThemeResources.CreateVectorIcon(
                    ThemeResources.IconPinData,
                    _isPinned ? ThemeResources.AccentBrush : ThemeResources.TextSecondaryBrush,
                    13,
                    1.8);
            };
            winBtns.Children.Add(_btnPin);

            Button btnMin = new Button
            {
                Style = (Style)Resources["TitleBarButton"],
                Content = ThemeResources.CreateVectorIcon(ThemeResources.IconMinimizeData, ThemeResources.TextSecondaryBrush, 12, 2.0),
                ToolTip = "Minimizar",
                Margin = new Thickness(0, 0, 4, 0)
            };
            btnMin.Click += delegate { WindowState = WindowState.Minimized; };
            winBtns.Children.Add(btnMin);

            Button btnClose = new Button
            {
                Style = (Style)Resources["TitleBarCloseButton"],
                Content = ThemeResources.CreateVectorIcon(ThemeResources.IconCloseData, ThemeResources.TextSecondaryBrush, 12, 2.0),
                ToolTip = "Cerrar"
            };
            btnClose.Click += delegate { Close(); };
            winBtns.Children.Add(btnClose);

            Grid.SetColumn(winBtns, 2);
            grid.Children.Add(winBtns);

            bar.MouseLeftButtonDown += OnTitleBarDrag;
            bar.Child = grid;
            return bar;
        }

        private void OnTitleBarDrag(object sender, MouseButtonEventArgs e)
        {
            if (e.Handled ||
                e.ChangedButton != MouseButton.Left ||
                e.ButtonState != MouseButtonState.Pressed ||
                Mouse.LeftButton != MouseButtonState.Pressed)
            {
                return;
            }

            DependencyObject src = e.OriginalSource as DependencyObject;
            if (src != null && IsInsideInteractiveControl(src))
            {
                return;
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

        private bool IsInsideInteractiveControl(DependencyObject obj)
        {
            DependencyObject cur = obj;
            while (cur != null)
            {
                if (cur is Button || cur == _pillEngineStatus)
                {
                    return true;
                }
                cur = VisualTreeHelper.GetParent(cur);
            }
            return false;
        }

        private Border BuildSidebar()
        {
            Border sidebar = new Border
            {
                Background = ThemeResources.BgSidebarBrush,
                BorderBrush = ThemeResources.BorderSubtleBrush,
                BorderThickness = new Thickness(0, 0, 1, 0),
                Padding = new Thickness(12, 14, 12, 12)
            };

            Grid grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Encabezado sidebar
            Grid topRow = new Grid { Margin = new Thickness(2, 0, 2, 10) };
            topRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            topRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            StackPanel titlePack = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            titlePack.Children.Add(new TextBlock
            {
                Text = "PERFILES",
                Foreground = ThemeResources.TextMutedBrush,
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                VerticalAlignment = VerticalAlignment.Center
            });

            Border countPill = new Border
            {
                Background = ThemeResources.BgInputBrush,
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(7, 1, 7, 1),
                Margin = new Thickness(7, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            _lblProfileCount = new TextBlock
            {
                Text = "0",
                Foreground = ThemeResources.TextSecondaryBrush,
                FontFamily = new FontFamily("Consolas"),
                FontSize = 10.5,
                FontWeight = FontWeights.SemiBold
            };
            countPill.Child = _lblProfileCount;
            titlePack.Children.Add(countPill);
            Grid.SetColumn(titlePack, 0);
            topRow.Children.Add(titlePack);

            Button btnNew = new Button
            {
                Style = (Style)Resources["PrimaryButton"],
                Padding = new Thickness(9, 4, 10, 4),
                Height = 26
            };
            StackPanel newBtnContent = new StackPanel { Orientation = Orientation.Horizontal };
            newBtnContent.Children.Add(ThemeResources.CreateVectorIcon(ThemeResources.IconPlusData, new SolidColorBrush(Color.FromRgb(9, 16, 23)), 11, 2.4));
            newBtnContent.Children.Add(new TextBlock { Text = "Nuevo", Margin = new Thickness(5, 0, 0, 0), FontSize = 11.5, FontWeight = FontWeights.Bold });
            btnNew.Content = newBtnContent;
            btnNew.Click += delegate { CreateNewProfile(); };
            Grid.SetColumn(btnNew, 1);
            topRow.Children.Add(btnNew);

            Grid.SetRow(topRow, 0);
            grid.Children.Add(topRow);

            // Buscador
            Grid searchBox = new Grid { Margin = new Thickness(0, 0, 0, 10) };
            _txtSearch = new TextBox
            {
                Style = (Style)Resources["DarkTextBox"],
                Height = 32,
                Padding = new Thickness(28, 4, 26, 4),
                FontSize = 12
            };
            _txtSearch.TextChanged += delegate
            {
                _btnClearSearch.Visibility = string.IsNullOrEmpty(_txtSearch.Text) ? Visibility.Collapsed : Visibility.Visible;
                RefreshProfileList();
            };
            searchBox.Children.Add(_txtSearch);

            // Placeholder + lupa
            UIElement searchIcon = ThemeResources.CreateVectorIcon(ThemeResources.IconSearchData, ThemeResources.TextMutedBrush, 13, 1.8);
            FrameworkElement searchIconFe = (FrameworkElement)searchIcon;
            searchIconFe.HorizontalAlignment = HorizontalAlignment.Left;
            searchIconFe.VerticalAlignment = VerticalAlignment.Center;
            searchIconFe.Margin = new Thickness(9, 0, 0, 0);
            searchBox.Children.Add(searchIconFe);

            TextBlock searchPlaceholder = new TextBlock
            {
                Text = "Buscar arma...",
                Foreground = ThemeResources.TextMutedBrush,
                FontSize = 12,
                IsHitTestVisible = false,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(30, 0, 0, 0)
            };
            _txtSearch.TextChanged += delegate
            {
                searchPlaceholder.Visibility = string.IsNullOrEmpty(_txtSearch.Text) ? Visibility.Visible : Visibility.Collapsed;
            };
            searchBox.Children.Add(searchPlaceholder);

            _btnClearSearch = new Button
            {
                Content = ThemeResources.CreateVectorIcon(ThemeResources.IconCloseData, ThemeResources.TextMutedBrush, 10, 2.0),
                Width = 22,
                Height = 22,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 5, 0),
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand,
                Visibility = Visibility.Collapsed,
                FocusVisualStyle = null
            };
            _btnClearSearch.Click += delegate
            {
                _txtSearch.Text = "";
                _txtSearch.Focus();
            };
            searchBox.Children.Add(_btnClearSearch);

            Grid.SetRow(searchBox, 1);
            grid.Children.Add(searchBox);

            // Lista de perfiles virtualizada
            _listProfiles = new ListBox
            {
                ItemsSource = _profileItems,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                ItemContainerStyle = (Style)Resources["ProfileListItemStyle"],
                ItemTemplate = BuildProfileItemTemplate(),
                Margin = new Thickness(0, 0, 0, 10)
            };
            _listProfiles.SetValue(ScrollViewer.HorizontalScrollBarVisibilityProperty, ScrollBarVisibility.Disabled);
            _listProfiles.SelectionChanged += OnProfileListSelectionChanged;
            Grid.SetRow(_listProfiles, 2);
            grid.Children.Add(_listProfiles);

            // Botones de accion inferiores
            Grid actionsGrid = new Grid();
            actionsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            actionsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(5) });
            actionsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.15, GridUnitType.Star) });
            actionsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(5) });
            actionsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.9, GridUnitType.Star) });

            Button btnDup = CreateSidebarActionButton("Duplicar", ThemeResources.IconDuplicateData, false);
            btnDup.Click += delegate { DuplicateCurrentProfile(); };
            Grid.SetColumn(btnDup, 0);
            actionsGrid.Children.Add(btnDup);

            Button btnRen = CreateSidebarActionButton("Renombrar", ThemeResources.IconEditData, false);
            btnRen.Click += delegate { RenameCurrentProfile(); };
            Grid.SetColumn(btnRen, 2);
            actionsGrid.Children.Add(btnRen);

            Button btnDel = CreateSidebarActionButton("Borrar", ThemeResources.IconTrashData, true);
            btnDel.Click += delegate { DeleteCurrentProfile(); };
            Grid.SetColumn(btnDel, 4);
            actionsGrid.Children.Add(btnDel);

            Grid.SetRow(actionsGrid, 3);
            grid.Children.Add(actionsGrid);

            sidebar.Child = grid;
            return sidebar;
        }

        private Button CreateSidebarActionButton(string label, string iconData, bool isDanger)
        {
            Button btn = new Button
            {
                Style = (Style)Resources[isDanger ? "DangerGhostButton" : "SecondaryButton"],
                Height = 30,
                Padding = new Thickness(4, 2, 4, 2),
                ToolTip = label
            };
            StackPanel stack = new StackPanel { Orientation = Orientation.Horizontal };
            Brush iconBrush = isDanger ? ThemeResources.DangerBrush : ThemeResources.TextSecondaryBrush;
            stack.Children.Add(ThemeResources.CreateVectorIcon(iconData, iconBrush, 11, 1.8));
            stack.Children.Add(new TextBlock
            {
                Text = label,
                FontSize = 11,
                Margin = new Thickness(3, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center
            });
            btn.Content = stack;
            return btn;
        }

        private DataTemplate BuildProfileItemTemplate()
        {
            string xaml =
                "<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>" +
                "  <Grid Margin='1,1'>" +
                "    <Grid.ColumnDefinitions>" +
                "      <ColumnDefinition Width='*'/>" +
                "      <ColumnDefinition Width='Auto'/>" +
                "    </Grid.ColumnDefinitions>" +
                "    <TextBlock Text='{Binding Name}' Foreground='#F1F5F9' FontSize='12.5' FontWeight='Medium' TextTrimming='CharacterEllipsis' VerticalAlignment='Center'/>" +
                "    <StackPanel Grid.Column='1' Orientation='Horizontal' VerticalAlignment='Center' Margin='6,0,0,0'>" +
                "      <Border Visibility='{Binding MacroVisibility}' Background='#2D2212' CornerRadius='4' Padding='5,1' Margin='0,0,4,0'>" +
                "        <TextBlock Text='B' Foreground='#F59E0B' FontSize='10' FontWeight='Bold'/>" +
                "      </Border>" +
                "      <Border Visibility='{Binding ModeVisibility}' Background='#142636' CornerRadius='4' Padding='5,1' Margin='0,0,4,0'>" +
                "        <TextBlock Text='{Binding ModeTag}' Foreground='#38BDF8' FontSize='10' FontWeight='SemiBold'/>" +
                "      </Border>" +
                "      <TextBlock Text='{Binding VerticalText}' Foreground='#64748B' FontFamily='Consolas' FontSize='11' VerticalAlignment='Center'/>" +
                "    </StackPanel>" +
                "  </Grid>" +
                "</DataTemplate>";
            return (DataTemplate)XamlReader.Parse(xaml);
        }

        private Grid BuildWorkspace()
        {
            Grid ws = new Grid { Margin = new Thickness(18, 14, 18, 14) };
            ws.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            ws.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            // Cabecera del perfil activo
            Grid header = new Grid { Margin = new Thickness(0, 0, 0, 14) };
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            StackPanel headerLeft = new StackPanel();
            StackPanel nameRow = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            _lblActiveName = new TextBlock
            {
                Text = "PERFIL",
                Foreground = ThemeResources.TextPrimaryBrush,
                FontSize = 20,
                FontWeight = FontWeights.Bold,
                VerticalAlignment = VerticalAlignment.Center
            };
            nameRow.Children.Add(_lblActiveName);

            _badgeActiveMode = new Border
            {
                Background = ThemeResources.AccentSoftBrush,
                BorderBrush = new SolidColorBrush(Color.FromArgb(75, 56, 189, 248)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(5),
                Padding = new Thickness(8, 2, 8, 2),
                Margin = new Thickness(10, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            _lblActiveMode = new TextBlock
            {
                Text = "Lineal",
                Foreground = ThemeResources.AccentBrush,
                FontSize = 11,
                FontWeight = FontWeights.SemiBold
            };
            _badgeActiveMode.Child = _lblActiveMode;
            nameRow.Children.Add(_badgeActiveMode);
            headerLeft.Children.Add(nameRow);

            _lblDirectionSummary = new TextBlock
            {
                Text = "",
                Foreground = ThemeResources.TextSecondaryBrush,
                FontSize = 11.5,
                Margin = new Thickness(0, 3, 0, 0)
            };
            headerLeft.Children.Add(_lblDirectionSummary);

            Grid.SetColumn(headerLeft, 0);
            header.Children.Add(headerLeft);

            StackPanel headerRight = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center
            };
            _lblSaveState = new TextBlock
            {
                Text = "Guardado",
                Foreground = ThemeResources.SuccessBrush,
                FontSize = 11.5,
                FontWeight = FontWeights.Medium,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 12, 0)
            };
            headerRight.Children.Add(_lblSaveState);

            Button btnSaveNow = new Button
            {
                Style = (Style)Resources["SecondaryButton"],
                Height = 30,
                Padding = new Thickness(12, 4, 12, 4)
            };
            StackPanel saveContent = new StackPanel { Orientation = Orientation.Horizontal };
            saveContent.Children.Add(ThemeResources.CreateVectorIcon(ThemeResources.IconSaveData, ThemeResources.AccentBrush, 13, 1.8));
            saveContent.Children.Add(new TextBlock
            {
                Text = "Guardar",
                Margin = new Thickness(6, 0, 0, 0),
                FontSize = 12,
                FontWeight = FontWeights.SemiBold
            });
            btnSaveNow.Content = saveContent;
            btnSaveNow.Click += delegate
            {
                _saveDebounceTimer.Stop();
                FlushSave();
                _lblSaveState.Text = "Guardado en disco";
                _lblSaveState.Foreground = ThemeResources.SuccessBrush;
            };
            headerRight.Children.Add(btnSaveNow);

            Grid.SetColumn(headerRight, 1);
            header.Children.Add(headerRight);

            Grid.SetRow(header, 0);
            ws.Children.Add(header);

            // Cuerpo principal en 2 columnas: Controles (*) | Visualizador y Telemetria (285)
            Grid body = new Grid();
            body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(14) });
            body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(285) });

            ScrollViewer controlsScroll = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Padding = new Thickness(0, 0, 6, 0)
            };
            StackPanel controlsStack = new StackPanel();
            controlsStack.Children.Add(BuildCalibrationCard());
            controlsStack.Children.Add(BuildBurstPhaseCard());
            controlsStack.Children.Add(BuildHotkeysCard());
            controlsScroll.Content = controlsStack;

            Grid.SetColumn(controlsScroll, 0);
            body.Children.Add(controlsScroll);

            StackPanel rightColumn = new StackPanel();
            rightColumn.Children.Add(BuildVisualizerCard());
            rightColumn.Children.Add(BuildTelemetryCard());

            Grid.SetColumn(rightColumn, 2);
            body.Children.Add(rightColumn);

            Grid.SetRow(body, 1);
            ws.Children.Add(body);

            return ws;
        }

        private Border BuildCalibrationCard()
        {
            Border card = CreateCard();
            StackPanel stack = new StackPanel();

            stack.Children.Add(CreateSectionTitle("CALIBRACIÓN PRINCIPAL"));

            _sliderVertical = new ModernSlider
            {
                Title = "Fuerza Vertical (bajada)",
                Minimum = 0.0,
                Maximum = 30.0,
                Step = 0.05,
                Decimals = 2,
                SemanticLabelFunc = delegate(double v)
                {
                    if (v <= 0.02) return "Nada";
                    if (v <= 2.5) return "Suave";
                    if (v <= 6.0) return "Medio";
                    if (v <= 14.0) return "Fuerte";
                    return "Extremo";
                }
            };
            _sliderVertical.ValueChanged += delegate { OnControlEdited(); };
            stack.Children.Add(_sliderVertical);

            _sliderHorizontal = new ModernSlider
            {
                Title = "Desvío Horizontal",
                Minimum = -10.0,
                Maximum = 10.0,
                Step = 0.05,
                Decimals = 2,
                IsBidirectional = true,
                ShowZeroButton = true,
                SemanticLabelFunc = delegate(double v)
                {
                    if (v < -0.02) return "Izquierda";
                    if (v > 0.02) return "Derecha";
                    return "Recto";
                },
                SubtitleFunc = delegate(double v)
                {
                    if (v < -0.02) return "El arma patea a la izquierda · el motor compensa a la derecha →";
                    if (v > 0.02) return "← El arma patea a la derecha · el motor compensa a la izquierda";
                    return "Sin desvío horizontal base";
                }
            };
            _sliderHorizontal.ValueChanged += delegate { OnControlEdited(); };
            stack.Children.Add(_sliderHorizontal);

            _sliderHumanize = new ModernSlider
            {
                Title = "Suavizado / Micro-variación",
                Minimum = 0.0,
                Maximum = 3.0,
                Step = 0.05,
                DisplayMultiplier = 100.0,
                Decimals = 0,
                Suffix = "%",
                SemanticLabelFunc = delegate(double v)
                {
                    if (v <= 0.05) return "Apagado (Raw)";
                    if (v <= 0.50) return "Muy sutil";
                    if (v <= 1.25) return "Moderado";
                    if (v <= 2.00) return "Alto";
                    return "Intenso";
                }
            };
            _sliderHumanize.ValueChanged += delegate { OnControlEdited(); };
            stack.Children.Add(_sliderHumanize);

            card.Child = stack;
            return card;
        }

        private Border BuildBurstPhaseCard()
        {
            Border card = CreateCard();
            StackPanel stack = new StackPanel();

            // Header con selector segmentado
            Grid header = new Grid { Margin = new Thickness(0, 0, 0, 12) };
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            TextBlock title = CreateSectionTitle("CURVA DE RÁFAGA (FASES)");
            title.Margin = new Thickness(0);
            title.VerticalAlignment = VerticalAlignment.Center;
            Grid.SetColumn(title, 0);
            header.Children.Add(title);

            Border segmented = new Border
            {
                Background = ThemeResources.BgCanvasBrush,
                BorderBrush = ThemeResources.BorderSubtleBrush,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(7),
                Padding = new Thickness(2)
            };
            StackPanel segTabs = new StackPanel { Orientation = Orientation.Horizontal };
            _tabLinear = CreateSegmentButton("Lineal");
            _tabTwoPhase = CreateSegmentButton("2 Fases");
            _tabMultiPhase = CreateSegmentButton("Multi-Fase");

            _tabLinear.Click += delegate { SwitchBurstMode(BurstMode.Linear); };
            _tabTwoPhase.Click += delegate { SwitchBurstMode(BurstMode.TwoPhase); };
            _tabMultiPhase.Click += delegate { SwitchBurstMode(BurstMode.MultiPhase); };

            segTabs.Children.Add(_tabLinear);
            segTabs.Children.Add(_tabTwoPhase);
            segTabs.Children.Add(_tabMultiPhase);
            segmented.Child = segTabs;

            Grid.SetColumn(segmented, 1);
            header.Children.Add(segmented);
            stack.Children.Add(header);

            // Panel Lineal
            _panelLinearInfo = new Border
            {
                Background = ThemeResources.BgInputBrush,
                BorderBrush = ThemeResources.BorderSubtleBrush,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(12, 10, 12, 10)
            };
            _panelLinearInfo.Child = new TextBlock
            {
                Text = "Compensación constante durante toda la ráfaga. Selecciona 2 Fases o Multi-Fase si el arma cambia de dirección tras los primeros disparos.",
                Foreground = ThemeResources.TextSecondaryBrush,
                FontSize = 11.5,
                TextWrapping = TextWrapping.Wrap,
                LineHeight = 16
            };
            stack.Children.Add(_panelLinearInfo);

            // Panel 2 Fases
            _panelTwoPhase = new StackPanel { Visibility = Visibility.Collapsed };
            _sliderSwitchTime = new ModernSlider
            {
                Title = "Tiempo de transición",
                Minimum = 100.0,
                Maximum = 2000.0,
                Step = 10.0,
                Decimals = 0,
                Suffix = " ms",
                SemanticLabelFunc = delegate(double v)
                {
                    return (v / 1000.0).ToString("F2", CultureInfo.InvariantCulture) + " s";
                }
            };
            _sliderSwitchTime.ValueChanged += delegate { OnControlEdited(); };
            _panelTwoPhase.Children.Add(_sliderSwitchTime);

            _sliderPhase2Side = new ModernSlider
            {
                Title = "Desvío Horizontal en Fase 2",
                Minimum = -10.0,
                Maximum = 10.0,
                Step = 0.05,
                Decimals = 2,
                IsBidirectional = true,
                ShowZeroButton = true,
                SemanticLabelFunc = delegate(double v)
                {
                    if (v < -0.02) return "Izquierda";
                    if (v > 0.02) return "Derecha";
                    return "Recto";
                },
                SubtitleFunc = delegate(double v)
                {
                    if (v < -0.02) return "En fase 2 se desvía a la izquierda → compensa a la derecha";
                    if (v > 0.02) return "← En fase 2 se desvía a la derecha · compensa a la izquierda";
                    return "En fase 2 mantiene desplazamiento recto";
                }
            };
            _sliderPhase2Side.ValueChanged += delegate { OnControlEdited(); };
            _panelTwoPhase.Children.Add(_sliderPhase2Side);
            stack.Children.Add(_panelTwoPhase);

            // Panel Multi-Fase
            _panelMultiPhase = new StackPanel { Visibility = Visibility.Collapsed };
            Grid multiToolbar = new Grid { Margin = new Thickness(0, 0, 0, 10) };
            multiToolbar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            multiToolbar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            TextBlock lblMultiHint = new TextBlock
            {
                Text = "Secuencias aplicadas según tiempo transcurrido:",
                Foreground = ThemeResources.TextSecondaryBrush,
                FontSize = 11.5,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(lblMultiHint, 0);
            multiToolbar.Children.Add(lblMultiHint);

            StackPanel multiBtns = new StackPanel { Orientation = Orientation.Horizontal };
            Button btnDrawPattern = new Button
            {
                Style = (Style)Resources["SecondaryButton"],
                Height = 26,
                Padding = new Thickness(9, 2, 9, 2),
                Margin = new Thickness(0, 0, 6, 0),
                ToolTip = "Abrir editor visual para dibujar la curva"
            };
            StackPanel drawContent = new StackPanel { Orientation = Orientation.Horizontal };
            drawContent.Children.Add(ThemeResources.CreateVectorIcon(ThemeResources.IconPencilData, ThemeResources.AccentBrush, 11, 2.0));
            drawContent.Children.Add(new TextBlock { Text = "Dibujar", FontSize = 11, Margin = new Thickness(5, 0, 0, 0) });
            btnDrawPattern.Content = drawContent;
            btnDrawPattern.Click += delegate { OpenPatternEditor(); };
            multiBtns.Children.Add(btnDrawPattern);

            Button btnSortPhases = new Button
            {
                Style = (Style)Resources["SecondaryButton"],
                Height = 26,
                Padding = new Thickness(9, 2, 9, 2),
                Margin = new Thickness(0, 0, 6, 0),
                ToolTip = "Ordenar fases cronológicamente"
            };
            StackPanel sortContent = new StackPanel { Orientation = Orientation.Horizontal };
            sortContent.Children.Add(ThemeResources.CreateVectorIcon(ThemeResources.IconSortData, ThemeResources.TextSecondaryBrush, 11, 2.0));
            sortContent.Children.Add(new TextBlock { Text = "Ordenar", FontSize = 11, Margin = new Thickness(5, 0, 0, 0) });
            btnSortPhases.Content = sortContent;
            btnSortPhases.Click += delegate { SortMultiPhases(); };
            multiBtns.Children.Add(btnSortPhases);

            Button btnAddPhase = new Button
            {
                Style = (Style)Resources["PrimaryButton"],
                Height = 26,
                Padding = new Thickness(10, 2, 10, 2)
            };
            StackPanel addContent = new StackPanel { Orientation = Orientation.Horizontal };
            addContent.Children.Add(ThemeResources.CreateVectorIcon(ThemeResources.IconPlusData, new SolidColorBrush(Color.FromRgb(9, 16, 23)), 11, 2.4));
            addContent.Children.Add(new TextBlock { Text = "Añadir Fase", FontSize = 11, FontWeight = FontWeights.Bold, Margin = new Thickness(4, 0, 0, 0) });
            btnAddPhase.Content = addContent;
            btnAddPhase.Click += delegate { AddMultiPhaseStep(); };
            multiBtns.Children.Add(btnAddPhase);

            Grid.SetColumn(multiBtns, 1);
            multiToolbar.Children.Add(multiBtns);
            _panelMultiPhase.Children.Add(multiToolbar);

            _multiPhaseRowsPanel = new StackPanel();
            ScrollViewer multiScroll = new ScrollViewer
            {
                MaxHeight = 225,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Content = _multiPhaseRowsPanel
            };
            _panelMultiPhase.Children.Add(multiScroll);
            stack.Children.Add(_panelMultiPhase);

            card.Child = stack;
            return card;
        }

        private Button CreateSegmentButton(string text)
        {
            return new Button
            {
                Style = (Style)Resources["SegmentTabButton"],
                Content = text
            };
        }

        private Border BuildHotkeysCard()
        {
            Border card = CreateCard();
            card.Margin = new Thickness(0);
            StackPanel stack = new StackPanel();
            stack.Children.Add(CreateSectionTitle("ATAJOS GLOBALES Y MACRO"));

            Grid grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.2, GridUnitType.Star) });

            // Columna 0: Toggle Key
            StackPanel col0 = new StackPanel();
            col0.Children.Add(new TextBlock
            {
                Text = "Activar / Pausar",
                Foreground = ThemeResources.TextSecondaryBrush,
                FontSize = 11.5,
                Margin = new Thickness(0, 0, 0, 6)
            });
            _btnToggleKey = CreateHotkeyButton();
            _btnToggleKey.ToolTip = "Clic y presiona una tecla o botón lateral del mouse (Esc cancela)";
            _btnToggleKey.Click += delegate { BeginKeyCapture(CaptureTarget.ToggleKey); };
            col0.Children.Add(_btnToggleKey);
            Grid.SetColumn(col0, 0);
            grid.Children.Add(col0);

            // Columna 2: Macro Swap Key
            StackPanel col1 = new StackPanel();
            col1.Children.Add(new TextBlock
            {
                Text = "Alternar Perfil A/B",
                Foreground = ThemeResources.TextSecondaryBrush,
                FontSize = 11.5,
                Margin = new Thickness(0, 0, 0, 6)
            });
            _btnMacroKey = CreateHotkeyButton();
            _btnMacroKey.ToolTip = "Clic y presiona una tecla (Esc desactiva el atajo)";
            _btnMacroKey.Click += delegate { BeginKeyCapture(CaptureTarget.MacroKey); };
            col1.Children.Add(_btnMacroKey);
            Grid.SetColumn(col1, 2);
            grid.Children.Add(col1);

            // Columna 4: Macro Profile B
            StackPanel col2 = new StackPanel();
            col2.Children.Add(new TextBlock
            {
                Text = "Perfil Secundario (B)",
                Foreground = ThemeResources.TextSecondaryBrush,
                FontSize = 11.5,
                Margin = new Thickness(0, 0, 0, 6)
            });
            _comboMacroB = new ComboBox
            {
                Style = (Style)Resources["DarkComboBox"],
                Height = 32,
                FontSize = 12
            };
            _comboMacroB.SelectionChanged += delegate
            {
                if (_isSyncing || _comboMacroB.SelectedItem == null) return;
                _config.macro_profile_b = _comboMacroB.SelectedItem.ToString();
                RefreshProfileList();
                ScheduleSave();
            };
            col2.Children.Add(_comboMacroB);
            Grid.SetColumn(col2, 4);
            grid.Children.Add(col2);

            stack.Children.Add(grid);
            card.Child = stack;
            return card;
        }

        private Button CreateHotkeyButton()
        {
            return new Button
            {
                Style = (Style)Resources["SecondaryButton"],
                Height = 32,
                FontFamily = new FontFamily("Consolas"),
                FontSize = 12,
                FontWeight = FontWeights.SemiBold
            };
        }

        private Border BuildVisualizerCard()
        {
            Border card = CreateCard();
            StackPanel stack = new StackPanel();

            Grid header = new Grid { Margin = new Thickness(0, 0, 0, 10) };
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            StackPanel titleCol = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            TextBlock title = CreateSectionTitle("VISTA PREVIA DE PATRÓN");
            title.Margin = new Thickness(0);
            titleCol.Children.Add(title);

            StackPanel legend = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 3, 0, 0) };
            legend.Children.Add(CreateLegendDot(new SolidColorBrush(Color.FromRgb(251, 191, 36)), "Inicio 1"));
            legend.Children.Add(CreateLegendDot(ThemeResources.DangerBrush, "Fin"));
            titleCol.Children.Add(legend);

            Grid.SetColumn(titleCol, 0);
            header.Children.Add(titleCol);

            Button btnDraw = new Button
            {
                Style = (Style)Resources["SecondaryButton"],
                Height = 26,
                Padding = new Thickness(9, 2, 10, 2),
                BorderBrush = new SolidColorBrush(Color.FromArgb(90, 56, 189, 248)),
                Background = ThemeResources.AccentSoftBrush,
                Foreground = ThemeResources.AccentBrush,
                ToolTip = "Dibujar o editar el patrón visualmente"
            };
            StackPanel drawBtnContent = new StackPanel { Orientation = Orientation.Horizontal };
            drawBtnContent.Children.Add(ThemeResources.CreateVectorIcon(ThemeResources.IconPencilData, ThemeResources.AccentBrush, 11.5, 1.9));
            drawBtnContent.Children.Add(new TextBlock { Text = "Dibujar", FontSize = 11.5, FontWeight = FontWeights.SemiBold, Margin = new Thickness(5, 0, 0, 0) });
            btnDraw.Content = drawBtnContent;
            btnDraw.Click += delegate { OpenPatternEditor(); };
            Grid.SetColumn(btnDraw, 1);
            header.Children.Add(btnDraw);
            stack.Children.Add(header);

            _visualizer = new RecoilVisualizer
            {
                Height = 274,
                Cursor = Cursors.Hand,
                ToolTip = "Doble clic o usa 'Dibujar' para abrir el editor visual de patrón"
            };
            _visualizer.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e)
            {
                if (e.ClickCount == 2)
                {
                    OpenPatternEditor();
                }
            };
            stack.Children.Add(_visualizer);

            card.Child = stack;
            return card;
        }

        private StackPanel CreateLegendDot(Brush color, string text)
        {
            StackPanel p = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(8, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            p.Children.Add(new Ellipse
            {
                Width = 7,
                Height = 7,
                Fill = color,
                Margin = new Thickness(0, 0, 4, 0),
                VerticalAlignment = VerticalAlignment.Center
            });
            p.Children.Add(new TextBlock
            {
                Text = text,
                Foreground = ThemeResources.TextMutedBrush,
                FontSize = 10.5
            });
            return p;
        }

        private Border BuildTelemetryCard()
        {
            Border card = CreateCard();
            card.Margin = new Thickness(0);
            StackPanel stack = new StackPanel();
            stack.Children.Add(CreateSectionTitle("TELEMETRÍA EN VIVO"));

            Grid tiles = new Grid();
            tiles.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            tiles.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
            tiles.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            tiles.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            tiles.RowDefinitions.Add(new RowDefinition { Height = new GridLength(8) });
            tiles.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            _lblTelState = CreateMetricTile(tiles, 0, 0, "Estado", "En espera");
            _lblTelBurst = CreateMetricTile(tiles, 2, 0, "Ráfaga", "0.00 s");
            _lblTelVector = CreateMetricTile(tiles, 0, 2, "Vector X / Y", "+0.0 / +0.0");
            _lblTelPhase = CreateMetricTile(tiles, 2, 2, "Fase", "Base");

            stack.Children.Add(tiles);

            stack.Children.Add(new TextBlock
            {
                Text = "Mantén Clic Der + Clic Izq para activar la compensación en juego.",
                Foreground = ThemeResources.TextMutedBrush,
                FontSize = 11,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 10, 0, 0),
                LineHeight = 15
            });

            card.Child = stack;
            return card;
        }

        private TextBlock CreateMetricTile(Grid parent, int col, int row, string label, string initialValue)
        {
            Border tile = new Border
            {
                Background = ThemeResources.BgInputBrush,
                BorderBrush = ThemeResources.BorderSubtleBrush,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(9, 7, 9, 7)
            };

            StackPanel inner = new StackPanel();
            inner.Children.Add(new TextBlock
            {
                Text = label,
                Foreground = ThemeResources.TextMutedBrush,
                FontSize = 10.5
            });
            TextBlock val = new TextBlock
            {
                Text = initialValue,
                Foreground = ThemeResources.TextPrimaryBrush,
                FontFamily = new FontFamily("Consolas"),
                FontSize = 12,
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

        private Border CreateCard()
        {
            return new Border
            {
                Background = ThemeResources.BgCardBrush,
                BorderBrush = ThemeResources.BorderSubtleBrush,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(15, 13, 15, 13),
                Margin = new Thickness(0, 0, 0, 12)
            };
        }

        private TextBlock CreateSectionTitle(string text)
        {
            return new TextBlock
            {
                Text = text,
                Foreground = ThemeResources.TextMutedBrush,
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 10)
            };
        }

        // =========================================================================
        // Sincronizacion de Perfiles, Controles y Fases
        // =========================================================================

        private WeaponProfile GetActiveProfile()
        {
            if (_config == null || string.IsNullOrEmpty(_config.active_profile) || _config.profiles == null)
            {
                return null;
            }
            if (!_config.profiles.ContainsKey(_config.active_profile))
            {
                return null;
            }
            return _config.profiles[_config.active_profile];
        }

        private void RefreshProfileList()
        {
            if (_config == null || _config.profiles == null) return;

            string query = _txtSearch != null ? (_txtSearch.Text ?? "").Trim() : "";
            string active = _config.active_profile ?? "";

            _isSyncing = true;
            try
            {
                _profileItems.Clear();
                ProfileListItem toSelect = null;

                foreach (KeyValuePair<string, WeaponProfile> kv in _config.profiles)
                {
                    if (!string.IsNullOrEmpty(query) &&
                        kv.Key.IndexOf(query, StringComparison.OrdinalIgnoreCase) < 0)
                    {
                        continue;
                    }

                    WeaponProfile p = kv.Value;
                    BurstMode mode = p != null ? p.GetBurstMode() : BurstMode.Linear;
                    string modeTag = mode == BurstMode.MultiPhase ? "MF" : (mode == BurstMode.TwoPhase ? "2F" : "");

                    ProfileListItem item = new ProfileListItem
                    {
                        Name = kv.Key,
                        VerticalText = "↓" + (p != null ? p.patea_arriba : 0.0).ToString("F2", CultureInfo.InvariantCulture),
                        ModeTag = modeTag,
                        ModeVisibility = string.IsNullOrEmpty(modeTag) ? Visibility.Collapsed : Visibility.Visible,
                        MacroVisibility = string.Equals(kv.Key, _config.macro_profile_b, StringComparison.Ordinal) ? Visibility.Visible : Visibility.Collapsed
                    };

                    _profileItems.Add(item);
                    if (string.Equals(kv.Key, active, StringComparison.Ordinal))
                    {
                        toSelect = item;
                    }
                }

                _lblProfileCount.Text = _config.profiles.Count.ToString();
                if (toSelect != null)
                {
                    _listProfiles.SelectedItem = toSelect;
                    _listProfiles.ScrollIntoView(toSelect);
                }
            }
            finally
            {
                _isSyncing = false;
            }
        }

        private void SyncMacroBCombo()
        {
            if (_config == null || _config.profiles == null || _comboMacroB == null) return;

            _isSyncing = true;
            try
            {
                _comboMacroB.Items.Clear();
                foreach (string name in _config.profiles.Keys)
                {
                    _comboMacroB.Items.Add(name);
                }

                if (!string.IsNullOrEmpty(_config.macro_profile_b) && _config.profiles.ContainsKey(_config.macro_profile_b))
                {
                    _comboMacroB.SelectedItem = _config.macro_profile_b;
                }
                else if (_comboMacroB.Items.Count > 0)
                {
                    _comboMacroB.SelectedIndex = _comboMacroB.Items.Count > 1 ? 1 : 0;
                    _config.macro_profile_b = _comboMacroB.SelectedItem.ToString();
                }
            }
            finally
            {
                _isSyncing = false;
            }
        }

        private void OnProfileListSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isSyncing) return;
            ProfileListItem selected = _listProfiles.SelectedItem as ProfileListItem;
            if (selected == null) return;

            if (!string.Equals(_config.active_profile, selected.Name, StringComparison.Ordinal))
            {
                _config.active_profile = selected.Name;
                SyncActiveProfileToUI();
                PushEngineSnapshot();
                ScheduleSave();
            }
        }

        private void SyncActiveProfileToUI()
        {
            WeaponProfile p = GetActiveProfile();
            if (p == null) return;

            _isSyncing = true;
            try
            {
                _lblActiveName.Text = _config.active_profile.ToUpperInvariant();

                _sliderVertical.SetValueSilent(p.patea_arriba);
                _sliderHorizontal.SetValueSilent(p.se_va_lado);
                _sliderHumanize.SetValueSilent(p.humanizacion_fuerza);
                _sliderSwitchTime.SetValueSilent(p.tiempo_cambio_ms);
                _sliderPhase2Side.SetValueSilent(p.nuevo_lado);

                BurstMode mode = p.GetBurstMode();
                UpdateBurstModeView(mode);
                RebuildMultiPhaseRows(p);
                UpdateHeaderSummary(p, mode);
                UpdateHotkeyButtonsText();

                _visualizer.SetProfile(p);
                UpdateTelemetryReadout(EngineTelemetry.Idle);
            }
            finally
            {
                _isSyncing = false;
            }
        }

        private void UpdateHeaderSummary(WeaponProfile p, BurstMode mode)
        {
            string modeLabel = mode == BurstMode.MultiPhase ? "Multi-Fase" : (mode == BurstMode.TwoPhase ? "2 Fases" : "Lineal");
            _lblActiveMode.Text = modeLabel;

            string horizDir = "Recto";
            if (p.se_va_lado < -0.02) horizDir = string.Format(CultureInfo.InvariantCulture, "Izq {0:F2}", Math.Abs(p.se_va_lado));
            else if (p.se_va_lado > 0.02) horizDir = string.Format(CultureInfo.InvariantCulture, "Der {0:F2}", p.se_va_lado);

            _lblDirectionSummary.Text = string.Format(
                CultureInfo.InvariantCulture,
                "Vertical {0:F2} · Horizontal {1} · Suavizado {2:0}%",
                p.patea_arriba,
                horizDir,
                p.humanizacion_fuerza * 100.0);
        }

        private void UpdateBurstModeView(BurstMode mode)
        {
            StyleTabButton(_tabLinear, mode == BurstMode.Linear);
            StyleTabButton(_tabTwoPhase, mode == BurstMode.TwoPhase);
            StyleTabButton(_tabMultiPhase, mode == BurstMode.MultiPhase);

            _panelLinearInfo.Visibility = mode == BurstMode.Linear ? Visibility.Visible : Visibility.Collapsed;
            _panelTwoPhase.Visibility = mode == BurstMode.TwoPhase ? Visibility.Visible : Visibility.Collapsed;
            _panelMultiPhase.Visibility = mode == BurstMode.MultiPhase ? Visibility.Visible : Visibility.Collapsed;
        }

        private void StyleTabButton(Button btn, bool active)
        {
            btn.Tag = active ? "Active" : null;
            btn.Foreground = active ? ThemeResources.AccentBrush : ThemeResources.TextSecondaryBrush;
        }

        private void SwitchBurstMode(BurstMode targetMode)
        {
            WeaponProfile p = GetActiveProfile();
            if (p == null) return;

            BurstMode current = p.GetBurstMode();
            if (current == targetMode) return;

            if (targetMode == BurstMode.TwoPhase && current == BurstMode.MultiPhase && p.secuencias != null && p.secuencias.Count > 0)
            {
                bool confirmed = ProfileDialog.ShowConfirm(
                    this,
                    "Cambiar a modo 2 Fases",
                    "Al cambiar a 2 Fases se eliminarán las secuencias multi-fase configuradas para este perfil. ¿Deseas continuar?",
                    "Cambiar",
                    true);
                if (!confirmed) return;

                p.secuencias.Clear();
            }

            if (targetMode == BurstMode.Linear)
            {
                p.cambia_de_lado = false;
            }
            else if (targetMode == BurstMode.TwoPhase)
            {
                p.cambia_de_lado = true;
            }
            else if (targetMode == BurstMode.MultiPhase)
            {
                p.cambia_de_lado = true;
                if (p.secuencias == null) p.secuencias = new List<RecoilStep>();
                if (p.secuencias.Count == 0)
                {
                    p.secuencias.Add(new RecoilStep
                    {
                        tiempo_ms = p.tiempo_cambio_ms >= 100.0 ? p.tiempo_cambio_ms : 400.0,
                        se_va_lado = p.nuevo_lado,
                        patea_arriba = p.patea_arriba
                    });
                }
            }

            UpdateBurstModeView(p.GetBurstMode());
            RebuildMultiPhaseRows(p);
            UpdateHeaderSummary(p, p.GetBurstMode());
            _visualizer.SetProfile(p);
            RefreshProfileList();
            PushEngineSnapshot();
            ScheduleSave();
        }

        private void RebuildMultiPhaseRows(WeaponProfile p)
        {
            _multiPhaseRowsPanel.Children.Clear();
            if (p == null || p.secuencias == null || p.secuencias.Count == 0) return;

            for (int i = 0; i < p.secuencias.Count; i++)
            {
                int index = i;
                RecoilStep step = p.secuencias[i];
                if (step == null) continue;

                Border row = new Border
                {
                    Background = ThemeResources.BgInputBrush,
                    BorderBrush = ThemeResources.BorderSubtleBrush,
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(7),
                    Padding = new Thickness(10, 7, 8, 7),
                    Margin = new Thickness(0, 0, 0, 7)
                };

                Grid g = new Grid();
                g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(48) });
                g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(28) });

                Border badge = new Border
                {
                    Background = ThemeResources.AccentSoftBrush,
                    CornerRadius = new CornerRadius(5),
                    Padding = new Thickness(6, 3, 6, 3),
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Center
                };
                badge.Child = new TextBlock
                {
                    Text = "#" + (index + 1).ToString(),
                    Foreground = ThemeResources.AccentBrush,
                    FontFamily = new FontFamily("Consolas"),
                    FontSize = 11.5,
                    FontWeight = FontWeights.Bold
                };
                Grid.SetColumn(badge, 0);
                g.Children.Add(badge);

                g.Children.Add(CreatePhaseInputField(1, "Ms", step.tiempo_ms.ToString("F0", CultureInfo.InvariantCulture), delegate(double v)
                {
                    step.tiempo_ms = Math.Max(0.0, v);
                    OnPhaseStepEdited();
                }));

                g.Children.Add(CreatePhaseInputField(2, "Vert", step.patea_arriba.ToString("F2", CultureInfo.InvariantCulture), delegate(double v)
                {
                    step.patea_arriba = Math.Max(-15.0, Math.Min(30.0, v));
                    OnPhaseStepEdited();
                }));

                g.Children.Add(CreatePhaseInputField(3, "Horiz", step.se_va_lado.ToString("F2", CultureInfo.InvariantCulture), delegate(double v)
                {
                    step.se_va_lado = Math.Max(-20.0, Math.Min(20.0, v));
                    OnPhaseStepEdited();
                }));

                Button btnDel = new Button
                {
                    Style = (Style)Resources["DangerGhostButton"],
                    Content = ThemeResources.CreateVectorIcon(ThemeResources.IconCloseData, ThemeResources.DangerBrush, 10, 2.2),
                    Width = 26,
                    Height = 26,
                    Padding = new Thickness(0),
                    ToolTip = "Eliminar fase"
                };
                btnDel.Click += delegate
                {
                    p.secuencias.RemoveAt(index);
                    if (p.secuencias.Count == 0)
                    {
                        UpdateBurstModeView(BurstMode.TwoPhase);
                    }
                    RebuildMultiPhaseRows(p);
                    UpdateHeaderSummary(p, p.GetBurstMode());
                    _visualizer.SetProfile(p);
                    RefreshProfileList();
                    PushEngineSnapshot();
                    ScheduleSave();
                };
                Grid.SetColumn(btnDel, 4);
                g.Children.Add(btnDel);

                row.Child = g;
                _multiPhaseRowsPanel.Children.Add(row);
            }
        }

        private StackPanel CreatePhaseInputField(int column, string caption, string initialText, Action<double> onValidNumber)
        {
            StackPanel sp = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 8, 0)
            };
            sp.Children.Add(new TextBlock
            {
                Text = caption,
                Foreground = ThemeResources.TextMutedBrush,
                FontSize = 11,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 5, 0)
            });

            TextBox tb = new TextBox
            {
                Style = (Style)Resources["DarkTextBox"],
                Text = initialText,
                Width = 62,
                Height = 26,
                FontFamily = new FontFamily("Consolas"),
                FontSize = 11.5,
                TextAlignment = TextAlignment.Center,
                Padding = new Thickness(2, 0, 2, 0),
                Background = ThemeResources.BgCardBrush
            };
            tb.GotKeyboardFocus += delegate { tb.SelectAll(); };
            tb.TextChanged += delegate
            {
                if (_isSyncing) return;
                double val;
                string normalized = (tb.Text ?? "").Trim().Replace(",", ".");
                if (double.TryParse(normalized, NumberStyles.Any, CultureInfo.InvariantCulture, out val))
                {
                    onValidNumber(val);
                }
            };
            sp.Children.Add(tb);
            Grid.SetColumn(sp, column);
            return sp;
        }

        private void AddMultiPhaseStep()
        {
            WeaponProfile p = GetActiveProfile();
            if (p == null) return;

            if (p.secuencias == null) p.secuencias = new List<RecoilStep>();

            double nextTime = 400.0;
            double nextHoriz = p.nuevo_lado;
            double nextVert = p.patea_arriba;

            if (p.secuencias.Count > 0)
            {
                RecoilStep last = p.secuencias[p.secuencias.Count - 1];
                nextTime = last.tiempo_ms + 200.0;
                nextHoriz = last.se_va_lado;
                nextVert = last.patea_arriba;
            }

            p.secuencias.Add(new RecoilStep
            {
                tiempo_ms = nextTime,
                se_va_lado = nextHoriz,
                patea_arriba = nextVert
            });

            RebuildMultiPhaseRows(p);
            UpdateHeaderSummary(p, p.GetBurstMode());
            _visualizer.SetProfile(p);
            PushEngineSnapshot();
            ScheduleSave();
        }

        private void SortMultiPhases()
        {
            WeaponProfile p = GetActiveProfile();
            if (p == null || p.secuencias == null || p.secuencias.Count < 2) return;

            p.secuencias.Sort(delegate(RecoilStep a, RecoilStep b)
            {
                return a.tiempo_ms.CompareTo(b.tiempo_ms);
            });

            RebuildMultiPhaseRows(p);
            _visualizer.SetProfile(p);
            PushEngineSnapshot();
            ScheduleSave();
        }

        private void OpenPatternEditor()
        {
            WeaponProfile current = GetActiveProfile();
            if (current == null || _config == null || string.IsNullOrEmpty(_config.active_profile)) return;

            PatternEditorWindow dlg = new PatternEditorWindow(this, _config.active_profile, current);
            if (dlg.ShowDialog() == true && dlg.ResultProfile != null)
            {
                _config.profiles[_config.active_profile] = dlg.ResultProfile.Clone();
                SyncActiveProfileToUI();
                RefreshProfileList();
                PushEngineSnapshot();
                ScheduleSave();
            }
        }

        private void OnPhaseStepEdited()
        {
            WeaponProfile p = GetActiveProfile();
            if (p == null) return;
            _visualizer.SetProfile(p);
            PushEngineSnapshot();
            ScheduleSave();
        }

        private void OnControlEdited()
        {
            if (_isSyncing) return;
            WeaponProfile p = GetActiveProfile();
            if (p == null) return;

            p.patea_arriba = _sliderVertical.Value;
            p.se_va_lado = _sliderHorizontal.Value;
            p.humanizacion_fuerza = _sliderHumanize.Value;
            p.suavizado_activo = p.humanizacion_fuerza > 0.05;
            p.tiempo_cambio_ms = Math.Round(_sliderSwitchTime.Value / 10.0) * 10.0;
            p.nuevo_lado = _sliderPhase2Side.Value;

            UpdateHeaderSummary(p, p.GetBurstMode());
            _visualizer.SetProfile(p);
            UpdateActiveListItemBadge(p);
            PushEngineSnapshot();
            ScheduleSave();
        }

        private void UpdateActiveListItemBadge(WeaponProfile p)
        {
            ProfileListItem selected = _listProfiles.SelectedItem as ProfileListItem;
            if (selected == null || !string.Equals(selected.Name, _config.active_profile, StringComparison.Ordinal))
            {
                return;
            }

            string newVert = "↓" + p.patea_arriba.ToString("F2", CultureInfo.InvariantCulture);
            if (selected.VerticalText != newVert)
            {
                int idx = _profileItems.IndexOf(selected);
                if (idx >= 0)
                {
                    selected.VerticalText = newVert;
                    _isSyncing = true;
                    try
                    {
                        _profileItems[idx] = new ProfileListItem
                        {
                            Name = selected.Name,
                            VerticalText = newVert,
                            ModeTag = selected.ModeTag,
                            ModeVisibility = selected.ModeVisibility,
                            MacroVisibility = selected.MacroVisibility
                        };
                        _listProfiles.SelectedIndex = idx;
                    }
                    finally
                    {
                        _isSyncing = false;
                    }
                }
            }
        }

        // =========================================================================
        // Operaciones CRUD de Perfiles
        // =========================================================================

        private void CreateNewProfile()
        {
            bool copyCurrent;
            string rawName = ProfileDialog.ShowTextInput(
                this,
                "Nuevo perfil de arma",
                "Ingresa un identificador corto (ej. ak12_acog):",
                "",
                "Crear perfil",
                true,
                out copyCurrent);

            if (string.IsNullOrEmpty(rawName)) return;
            string name = NormalizeProfileName(rawName);
            if (string.IsNullOrEmpty(name)) return;

            if (_config.profiles.ContainsKey(name))
            {
                ProfileDialog.ShowAlert(this, "Nombre duplicado", "Ya existe un perfil llamado '" + name + "'. Elige otro nombre.");
                return;
            }

            WeaponProfile current = GetActiveProfile();
            WeaponProfile created = (copyCurrent && current != null) ? current.Clone() : new WeaponProfile
            {
                patea_arriba = 3.5,
                se_va_lado = 0.0,
                cambia_de_lado = false,
                tiempo_cambio_ms = 400.0,
                nuevo_lado = 0.0,
                suavizado_activo = true,
                humanizacion_fuerza = 1.0
            };

            _config.profiles[name] = created;
            _config.active_profile = name;

            RefreshProfileList();
            SyncMacroBCombo();
            SyncActiveProfileToUI();
            PushEngineSnapshot();
            FlushSave();
        }

        private void DuplicateCurrentProfile()
        {
            WeaponProfile current = GetActiveProfile();
            if (current == null) return;

            bool unused;
            string rawName = ProfileDialog.ShowTextInput(
                this,
                "Duplicar perfil",
                "Nombre para la copia de '" + _config.active_profile + "':",
                _config.active_profile + "_copia",
                "Duplicar",
                false,
                out unused);

            if (string.IsNullOrEmpty(rawName)) return;
            string name = NormalizeProfileName(rawName);
            if (string.IsNullOrEmpty(name)) return;

            if (_config.profiles.ContainsKey(name))
            {
                ProfileDialog.ShowAlert(this, "Nombre duplicado", "Ya existe un perfil llamado '" + name + "'.");
                return;
            }

            _config.profiles[name] = current.Clone();
            _config.active_profile = name;

            RefreshProfileList();
            SyncMacroBCombo();
            SyncActiveProfileToUI();
            PushEngineSnapshot();
            FlushSave();
        }

        private void RenameCurrentProfile()
        {
            string oldName = _config.active_profile;
            if (string.IsNullOrEmpty(oldName) || !_config.profiles.ContainsKey(oldName)) return;

            bool unused;
            string rawName = ProfileDialog.ShowTextInput(
                this,
                "Renombrar perfil",
                "Nuevo nombre para '" + oldName + "':",
                oldName,
                "Renombrar",
                false,
                out unused);

            if (string.IsNullOrEmpty(rawName)) return;
            string newName = NormalizeProfileName(rawName);
            if (string.IsNullOrEmpty(newName) || string.Equals(newName, oldName, StringComparison.Ordinal)) return;

            if (_config.profiles.ContainsKey(newName))
            {
                ProfileDialog.ShowAlert(this, "Nombre duplicado", "Ya existe un perfil llamado '" + newName + "'.");
                return;
            }

            // Preservar el orden aproximado reconstruyendo el diccionario
            Dictionary<string, WeaponProfile> reordered = new Dictionary<string, WeaponProfile>();
            foreach (KeyValuePair<string, WeaponProfile> kv in _config.profiles)
            {
                if (string.Equals(kv.Key, oldName, StringComparison.Ordinal))
                {
                    reordered[newName] = kv.Value;
                }
                else
                {
                    reordered[kv.Key] = kv.Value;
                }
            }
            _config.profiles = reordered;
            _config.active_profile = newName;

            if (string.Equals(_config.macro_profile_a, oldName, StringComparison.Ordinal)) _config.macro_profile_a = newName;
            if (string.Equals(_config.macro_profile_b, oldName, StringComparison.Ordinal)) _config.macro_profile_b = newName;

            RefreshProfileList();
            SyncMacroBCombo();
            SyncActiveProfileToUI();
            PushEngineSnapshot();
            FlushSave();
        }

        private void DeleteCurrentProfile()
        {
            if (_config.profiles.Count <= 1)
            {
                ProfileDialog.ShowAlert(this, "Acción no permitida", "Debe permanecer al menos un perfil registrado.");
                return;
            }

            string target = _config.active_profile;
            bool confirmed = ProfileDialog.ShowConfirm(
                this,
                "Eliminar perfil",
                "¿Seguro que deseas eliminar permanentemente el perfil '" + target + "'?",
                "Eliminar",
                true);
            if (!confirmed) return;

            _config.profiles.Remove(target);
            foreach (string key in _config.profiles.Keys)
            {
                _config.active_profile = key;
                break;
            }

            if (string.Equals(_config.macro_profile_a, target, StringComparison.Ordinal)) _config.macro_profile_a = "";
            if (string.Equals(_config.macro_profile_b, target, StringComparison.Ordinal)) _config.macro_profile_b = "";

            RefreshProfileList();
            SyncMacroBCombo();
            SyncActiveProfileToUI();
            PushEngineSnapshot();
            FlushSave();
        }

        private static string NormalizeProfileName(string input)
        {
            return (input ?? "").Trim().Replace(" ", "_").ToLowerInvariant();
        }

        // =========================================================================
        // Motor, Hotkeys y Telemetria
        // =========================================================================

        private void BeginKeyCapture(CaptureTarget target)
        {
            _captureTarget = target;
            _captureWaitRelease = true;

            if (target == CaptureTarget.ToggleKey)
            {
                _btnToggleKey.Content = "Presiona tecla...";
                _btnToggleKey.Background = ThemeResources.AccentSoftBrush;
                _btnToggleKey.Foreground = ThemeResources.AccentBrush;
                _btnToggleKey.BorderBrush = ThemeResources.AccentBrush;
                ResetHotkeyButtonStyle(_btnMacroKey, NativeInput.GetKeyDisplayName(_config.profile_macro_key));
            }
            else if (target == CaptureTarget.MacroKey)
            {
                _btnMacroKey.Content = "Presiona tecla...";
                _btnMacroKey.Background = ThemeResources.AccentSoftBrush;
                _btnMacroKey.Foreground = ThemeResources.AccentBrush;
                _btnMacroKey.BorderBrush = ThemeResources.AccentBrush;
                ResetHotkeyButtonStyle(_btnToggleKey, NativeInput.GetKeyDisplayName(_config.toggle_key));
            }
        }

        private void UpdateHotkeyButtonsText()
        {
            if (_config == null) return;
            ResetHotkeyButtonStyle(_btnToggleKey, NativeInput.GetKeyDisplayName(_config.toggle_key > 0 ? _config.toggle_key : NativeInput.VK_CAPITAL));
            ResetHotkeyButtonStyle(_btnMacroKey, NativeInput.GetKeyDisplayName(_config.profile_macro_key));
        }

        private void ResetHotkeyButtonStyle(Button btn, string text)
        {
            if (btn == null) return;
            btn.Content = text;
            btn.Background = ThemeResources.BgInputBrush;
            btn.Foreground = ThemeResources.TextPrimaryBrush;
            btn.BorderBrush = ThemeResources.BorderSubtleBrush;
        }

        private void OnMonitorTick(object sender, EventArgs e)
        {
            if (_config == null) return;

            if (_captureTarget != CaptureTarget.None)
            {
                PollKeyCapture();
            }
            else
            {
                int tKey = _config.toggle_key > 0 ? _config.toggle_key : NativeInput.VK_CAPITAL;
                bool tDown = NativeInput.IsKeyDown(tKey);
                if (tDown && !_prevToggleDown)
                {
                    ToggleEngineEnabled();
                }
                _prevToggleDown = tDown;

                if (_config.profile_macro_key > 0)
                {
                    bool mDown = NativeInput.IsKeyDown(_config.profile_macro_key);
                    if (mDown && !_prevMacroDown)
                    {
                        ToggleMacroProfiles();
                    }
                    _prevMacroDown = mDown;
                }
                else
                {
                    _prevMacroDown = false;
                }
            }

            EngineTelemetry tel = _engine.GetTelemetry();
            _visualizer.SetTelemetry(tel);
            UpdateTelemetryReadout(tel);
            UpdateEngineStatusPill(false);
        }

        private void PollKeyCapture()
        {
            bool anyPressed = false;
            int detectedVk = 0;

            for (int vk = 0x04; vk <= 0xFE; vk++)
            {
                // Saltar variantes L/R especificas para detectar Shift/Ctrl/Alt unificados
                if (vk >= 0xA0 && vk <= 0xA5) continue;
                if (NativeInput.IsKeyDown(vk))
                {
                    anyPressed = true;
                    detectedVk = vk;
                    break;
                }
            }

            if (_captureWaitRelease)
            {
                if (!anyPressed)
                {
                    _captureWaitRelease = false;
                }
                return;
            }

            if (!anyPressed || detectedVk == 0) return;

            if (_captureTarget == CaptureTarget.ToggleKey)
            {
                if (detectedVk != NativeInput.VK_ESCAPE)
                {
                    _config.toggle_key = detectedVk;
                    ScheduleSave();
                }
            }
            else if (_captureTarget == CaptureTarget.MacroKey)
            {
                _config.profile_macro_key = (detectedVk == NativeInput.VK_ESCAPE) ? 0 : detectedVk;
                ScheduleSave();
            }

            _prevToggleDown = NativeInput.IsKeyDown(_config.toggle_key);
            _prevMacroDown = _config.profile_macro_key > 0 && NativeInput.IsKeyDown(_config.profile_macro_key);
            _captureTarget = CaptureTarget.None;

            UpdateHotkeyButtonsText();
            UpdateEngineStatusPill(true);
        }

        private void ToggleEngineEnabled()
        {
            _config.engine_enabled = !_config.engine_enabled;
            PushEngineSnapshot();
            ScheduleSave();
            UpdateEngineStatusPill(true);
        }

        private void ToggleMacroProfiles()
        {
            if (string.IsNullOrEmpty(_config.macro_profile_b) || !_config.profiles.ContainsKey(_config.macro_profile_b))
            {
                return;
            }

            string current = _config.active_profile;
            string target = null;

            if (!string.Equals(current, _config.macro_profile_b, StringComparison.Ordinal))
            {
                _config.macro_profile_a = current;
                target = _config.macro_profile_b;
            }
            else if (!string.IsNullOrEmpty(_config.macro_profile_a) && _config.profiles.ContainsKey(_config.macro_profile_a))
            {
                target = _config.macro_profile_a;
            }
            else
            {
                foreach (string k in _config.profiles.Keys)
                {
                    if (!string.Equals(k, _config.macro_profile_b, StringComparison.Ordinal))
                    {
                        target = k;
                        break;
                    }
                }
            }

            if (!string.IsNullOrEmpty(target) && _config.profiles.ContainsKey(target))
            {
                _config.active_profile = target;
                RefreshProfileList();
                SyncActiveProfileToUI();
                PushEngineSnapshot();
                ScheduleSave();
            }
        }

        private void UpdateEngineStatusPill(bool force)
        {
            EngineTelemetry tel = _engine.GetTelemetry();
            bool compensating = tel != null && tel.IsCompensating;
            bool enabled = _config != null && _config.engine_enabled;
            int tKey = (_config != null && _config.toggle_key > 0) ? _config.toggle_key : NativeInput.VK_CAPITAL;

            if (!force && compensating == _lastVisualCompensating && enabled == _lastVisualEnabled && tKey == _lastVisualToggleKey)
            {
                return;
            }

            _lastVisualCompensating = compensating;
            _lastVisualEnabled = enabled;
            _lastVisualToggleKey = tKey;

            _lblEngineHotkeyChip.Text = NativeInput.GetKeyDisplayName(tKey);

            if (!enabled)
            {
                _pillEngineStatus.Background = ThemeResources.DangerSoftBrush;
                _pillEngineStatus.BorderBrush = new SolidColorBrush(Color.FromArgb(110, 244, 63, 94));
                _dotEngineStatus.Fill = ThemeResources.DangerBrush;
                _lblEngineStatus.Text = "PAUSADO";
                _lblEngineStatus.Foreground = ThemeResources.DangerBrush;
            }
            else if (compensating)
            {
                _pillEngineStatus.Background = ThemeResources.WarningSoftBrush;
                _pillEngineStatus.BorderBrush = new SolidColorBrush(Color.FromArgb(130, 245, 158, 11));
                _dotEngineStatus.Fill = ThemeResources.WarningBrush;
                _lblEngineStatus.Text = "COMPENSANDO";
                _lblEngineStatus.Foreground = ThemeResources.WarningBrush;
            }
            else
            {
                _pillEngineStatus.Background = ThemeResources.SuccessSoftBrush;
                _pillEngineStatus.BorderBrush = new SolidColorBrush(Color.FromArgb(110, 34, 197, 94));
                _dotEngineStatus.Fill = ThemeResources.SuccessBrush;
                _lblEngineStatus.Text = "ACTIVO";
                _lblEngineStatus.Foreground = ThemeResources.SuccessBrush;
            }
        }

        private void UpdateTelemetryReadout(EngineTelemetry tel)
        {
            if (tel != null && tel.IsCompensating)
            {
                _lblTelState.Text = "Compensando";
                _lblTelState.Foreground = ThemeResources.SuccessBrush;
                _lblTelBurst.Text = (tel.ElapsedBurstMs / 1000.0).ToString("F2", CultureInfo.InvariantCulture) + " s";
                _lblTelVector.Text = string.Format(CultureInfo.InvariantCulture, "{0:+0.00;-0.00;0.00} / {1:+0.00;-0.00;0.00}", tel.CurrentDx, tel.CurrentDy);
                _lblTelPhase.Text = tel.ActivePhaseIndex < 0 ? "Base" : ("Fase " + (tel.ActivePhaseIndex + 1).ToString());
                return;
            }

            if (_config != null && !_config.engine_enabled)
            {
                _lblTelState.Text = "Pausado";
                _lblTelState.Foreground = ThemeResources.DangerBrush;
            }
            else
            {
                _lblTelState.Text = "En espera";
                _lblTelState.Foreground = ThemeResources.TextPrimaryBrush;
            }

            WeaponProfile p = GetActiveProfile();
            double baseX = p != null ? -p.se_va_lado : 0.0;
            double baseY = p != null ? p.patea_arriba : 0.0;

            _lblTelBurst.Text = "0.00 s";
            _lblTelVector.Text = string.Format(CultureInfo.InvariantCulture, "{0:+0.00;-0.00;0.00} / {1:+0.00;-0.00;0.00}", baseX, baseY);
            _lblTelPhase.Text = "Base";
        }

        private void PushEngineSnapshot()
        {
            if (_engine == null || _config == null) return;
            _engine.UpdateSnapshot(ConfigService.CreateSnapshot(_config));
        }

        private void ScheduleSave()
        {
            _hasPendingSave = true;
            _lblSaveState.Text = "Guardando...";
            _lblSaveState.Foreground = ThemeResources.WarningBrush;
            _saveDebounceTimer.Stop();
            _saveDebounceTimer.Start();
        }

        private void FlushSave()
        {
            try
            {
                ConfigService.SaveAtomic(_configPath, _config);
                _hasPendingSave = false;
                _lblSaveState.Text = "Guardado";
                _lblSaveState.Foreground = ThemeResources.SuccessBrush;
            }
            catch
            {
                _lblSaveState.Text = "Error al guardar";
                _lblSaveState.Foreground = ThemeResources.DangerBrush;
            }
        }

        private void OnWindowClosing(object sender, CancelEventArgs e)
        {
            _tickTimer.Stop();
            _saveDebounceTimer.Stop();
            if (_hasPendingSave)
            {
                FlushSave();
            }
            if (_engine != null)
            {
                _engine.Dispose();
            }
        }
    }
}
