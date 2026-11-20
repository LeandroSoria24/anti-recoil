using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace RecoilStudio
{
    public static class ProfileDialog
    {
        public static string ShowTextInput(
            Window owner,
            string title,
            string subtitle,
            string initialText,
            string confirmText,
            bool showDuplicateOption,
            out bool duplicateCurrent)
        {
            duplicateCurrent = false;
            bool resultCopy = false;
            string enteredText = null;

            Window win = CreateBaseModal(owner, title, 410);
            StackPanel body = GetBodyPanel(win);

            TextBlock lblSub = new TextBlock
            {
                Text = subtitle,
                Foreground = ThemeResources.TextSecondaryBrush,
                FontSize = 12,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 10)
            };
            body.Children.Add(lblSub);

            TextBox txtInput = new TextBox
            {
                Text = initialText ?? "",
                Height = 34,
                FontSize = 13
            };
            if (owner != null && owner.Resources.Contains("DarkTextBox"))
            {
                txtInput.Style = (Style)owner.Resources["DarkTextBox"];
            }
            body.Children.Add(txtInput);

            CheckBox chkCopy = null;
            if (showDuplicateOption)
            {
                chkCopy = new CheckBox
                {
                    Content = "Copiar calibracion del perfil actual",
                    IsChecked = true,
                    Margin = new Thickness(0, 12, 0, 0),
                    FontSize = 12
                };
                if (owner != null && owner.Resources.Contains("DarkCheckBox"))
                {
                    chkCopy.Style = (Style)owner.Resources["DarkCheckBox"];
                }
                body.Children.Add(chkCopy);
            }

            StackPanel footer = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 18, 0, 0)
            };

            Button btnCancel = new Button
            {
                Content = "Cancelar",
                IsCancel = true,
                MinWidth = 88,
                Height = 32,
                Margin = new Thickness(0, 0, 8, 0)
            };
            if (owner != null && owner.Resources.Contains("SecondaryButton"))
            {
                btnCancel.Style = (Style)owner.Resources["SecondaryButton"];
            }

            Button btnOk = new Button
            {
                Content = confirmText ?? "Aceptar",
                IsDefault = true,
                MinWidth = 96,
                Height = 32
            };
            if (owner != null && owner.Resources.Contains("PrimaryButton"))
            {
                btnOk.Style = (Style)owner.Resources["PrimaryButton"];
            }

            btnOk.Click += delegate
            {
                string val = (txtInput.Text ?? "").Trim();
                if (string.IsNullOrEmpty(val))
                {
                    txtInput.Focus();
                    return;
                }
                enteredText = val;
                resultCopy = chkCopy != null && chkCopy.IsChecked == true;
                win.DialogResult = true;
                win.Close();
            };

            footer.Children.Add(btnCancel);
            footer.Children.Add(btnOk);
            body.Children.Add(footer);

            win.Loaded += delegate
            {
                txtInput.Focus();
                txtInput.SelectAll();
            };

            bool? dlg = win.ShowDialog();
            if (dlg == true)
            {
                duplicateCurrent = resultCopy;
                return enteredText;
            }
            return null;
        }

        public static bool ShowConfirm(Window owner, string title, string message, string confirmText, bool isDanger)
        {
            Window win = CreateBaseModal(owner, title, 400);
            StackPanel body = GetBodyPanel(win);

            TextBlock lblMsg = new TextBlock
            {
                Text = message,
                Foreground = ThemeResources.TextSecondaryBrush,
                FontSize = 12.5,
                TextWrapping = TextWrapping.Wrap,
                LineHeight = 18
            };
            body.Children.Add(lblMsg);

            StackPanel footer = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 20, 0, 0)
            };

            Button btnCancel = new Button
            {
                Content = "Cancelar",
                IsCancel = true,
                MinWidth = 88,
                Height = 32,
                Margin = new Thickness(0, 0, 8, 0)
            };
            if (owner != null && owner.Resources.Contains("SecondaryButton"))
            {
                btnCancel.Style = (Style)owner.Resources["SecondaryButton"];
            }

            Button btnConfirm = new Button
            {
                Content = confirmText ?? "Confirmar",
                IsDefault = true,
                MinWidth = 98,
                Height = 32
            };
            string btnStyleKey = isDanger ? "DangerSolidButton" : "PrimaryButton";
            if (owner != null && owner.Resources.Contains(btnStyleKey))
            {
                btnConfirm.Style = (Style)owner.Resources[btnStyleKey];
            }

            btnConfirm.Click += delegate
            {
                win.DialogResult = true;
                win.Close();
            };

            footer.Children.Add(btnCancel);
            footer.Children.Add(btnConfirm);
            body.Children.Add(footer);

            return win.ShowDialog() == true;
        }

        public static void ShowAlert(Window owner, string title, string message)
        {
            Window win = CreateBaseModal(owner, title, 380);
            StackPanel body = GetBodyPanel(win);

            TextBlock lblMsg = new TextBlock
            {
                Text = message,
                Foreground = ThemeResources.TextSecondaryBrush,
                FontSize = 12.5,
                TextWrapping = TextWrapping.Wrap,
                LineHeight = 18
            };
            body.Children.Add(lblMsg);

            StackPanel footer = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 18, 0, 0)
            };

            Button btnOk = new Button
            {
                Content = "Entendido",
                IsDefault = true,
                IsCancel = true,
                MinWidth = 92,
                Height = 32
            };
            if (owner != null && owner.Resources.Contains("PrimaryButton"))
            {
                btnOk.Style = (Style)owner.Resources["PrimaryButton"];
            }
            btnOk.Click += delegate
            {
                win.DialogResult = true;
                win.Close();
            };

            footer.Children.Add(btnOk);
            body.Children.Add(footer);

            win.ShowDialog();
        }

        private static Window CreateBaseModal(Window owner, string title, double width)
        {
            Window win = new Window
            {
                Owner = owner,
                Width = width,
                SizeToContent = SizeToContent.Height,
                WindowStyle = WindowStyle.None,
                AllowsTransparency = true,
                Background = Brushes.Transparent,
                ResizeMode = ResizeMode.NoResize,
                ShowInTaskbar = false,
                WindowStartupLocation = owner != null ? WindowStartupLocation.CenterOwner : WindowStartupLocation.CenterScreen
            };

            Border outer = new Border
            {
                Margin = new Thickness(12),
                Background = ThemeResources.BgCardBrush,
                BorderBrush = ThemeResources.BorderStrongBrush,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Effect = new DropShadowEffect
                {
                    Color = Colors.Black,
                    BlurRadius = 18,
                    ShadowDepth = 2,
                    Opacity = 0.55
                }
            };

            StackPanel root = new StackPanel();

            // Header modal arrastrable
            Border header = new Border
            {
                Background = ThemeResources.BgSidebarBrush,
                BorderBrush = ThemeResources.BorderSubtleBrush,
                BorderThickness = new Thickness(0, 0, 0, 1),
                CornerRadius = new CornerRadius(10, 10, 0, 0),
                Padding = new Thickness(16, 11, 12, 11)
            };
            header.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e)
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
                    win.DragMove();
                }
                catch (InvalidOperationException)
                {
                }
            };

            Grid headerGrid = new Grid();
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            TextBlock lblTitle = new TextBlock
            {
                Text = title,
                Foreground = ThemeResources.TextPrimaryBrush,
                FontSize = 13.5,
                FontWeight = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(lblTitle, 0);
            headerGrid.Children.Add(lblTitle);

            Button btnClose = new Button
            {
                Content = ThemeResources.CreateVectorIcon(ThemeResources.IconCloseData, ThemeResources.TextSecondaryBrush, 12, 2.0),
                Width = 26,
                Height = 26,
                IsCancel = true
            };
            if (owner != null && owner.Resources.Contains("TitleBarCloseButton"))
            {
                btnClose.Style = (Style)owner.Resources["TitleBarCloseButton"];
            }
            Grid.SetColumn(btnClose, 1);
            headerGrid.Children.Add(btnClose);

            header.Child = headerGrid;
            root.Children.Add(header);

            StackPanel body = new StackPanel
            {
                Margin = new Thickness(18, 16, 18, 18),
                Name = "ModalBody"
            };
            root.Children.Add(body);

            outer.Child = root;
            win.Content = outer;
            return win;
        }

        private static StackPanel GetBodyPanel(Window win)
        {
            Border outer = (Border)win.Content;
            StackPanel root = (StackPanel)outer.Child;
            return (StackPanel)root.Children[1];
        }
    }
}
