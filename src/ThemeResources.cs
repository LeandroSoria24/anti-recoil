using System;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Shapes;

namespace RecoilStudio
{
    public static class ThemeResources
    {
        // Paleta Slate / Obsidian moderna
        public static readonly Color BgCanvasColor = Color.FromRgb(13, 15, 20);       // #0D0F14
        public static readonly Color BgSidebarColor = Color.FromRgb(18, 21, 28);      // #12151C
        public static readonly Color BgCardColor = Color.FromRgb(22, 26, 35);         // #161A23
        public static readonly Color BgInputColor = Color.FromRgb(29, 34, 46);        // #1D222E
        public static readonly Color BgInputHoverColor = Color.FromRgb(36, 43, 58);   // #242B3A
        public static readonly Color BorderSubtleColor = Color.FromRgb(37, 42, 56);   // #252A38
        public static readonly Color BorderStrongColor = Color.FromRgb(49, 57, 77);   // #31394D

        public static readonly Color AccentColor = Color.FromRgb(56, 189, 248);       // #38BDF8
        public static readonly Color AccentHoverColor = Color.FromRgb(14, 165, 233);  // #0EA5E9
        public static readonly Color AccentSoftColor = Color.FromRgb(20, 38, 54);     // #142636

        public static readonly Color SuccessColor = Color.FromRgb(34, 197, 94);       // #22C55E
        public static readonly Color SuccessSoftColor = Color.FromRgb(18, 43, 30);    // #122B1E

        public static readonly Color WarningColor = Color.FromRgb(245, 158, 11);      // #F59E0B
        public static readonly Color WarningSoftColor = Color.FromRgb(45, 34, 18);    // #2D2212

        public static readonly Color DangerColor = Color.FromRgb(244, 63, 94);        // #F43F5E
        public static readonly Color DangerSoftColor = Color.FromRgb(45, 22, 30);     // #2D161E

        public static readonly Color TextPrimaryColor = Color.FromRgb(241, 245, 249); // #F1F5F9
        public static readonly Color TextSecondaryColor = Color.FromRgb(148, 163, 184); // #94A3B8
        public static readonly Color TextMutedColor = Color.FromRgb(100, 116, 139);   // #64748B

        public static readonly SolidColorBrush BgCanvasBrush = Freeze(new SolidColorBrush(BgCanvasColor));
        public static readonly SolidColorBrush BgSidebarBrush = Freeze(new SolidColorBrush(BgSidebarColor));
        public static readonly SolidColorBrush BgCardBrush = Freeze(new SolidColorBrush(BgCardColor));
        public static readonly SolidColorBrush BgInputBrush = Freeze(new SolidColorBrush(BgInputColor));
        public static readonly SolidColorBrush BgInputHoverBrush = Freeze(new SolidColorBrush(BgInputHoverColor));
        public static readonly SolidColorBrush BorderSubtleBrush = Freeze(new SolidColorBrush(BorderSubtleColor));
        public static readonly SolidColorBrush BorderStrongBrush = Freeze(new SolidColorBrush(BorderStrongColor));

        public static readonly SolidColorBrush AccentBrush = Freeze(new SolidColorBrush(AccentColor));
        public static readonly SolidColorBrush AccentHoverBrush = Freeze(new SolidColorBrush(AccentHoverColor));
        public static readonly SolidColorBrush AccentSoftBrush = Freeze(new SolidColorBrush(AccentSoftColor));

        public static readonly SolidColorBrush SuccessBrush = Freeze(new SolidColorBrush(SuccessColor));
        public static readonly SolidColorBrush SuccessSoftBrush = Freeze(new SolidColorBrush(SuccessSoftColor));

        public static readonly SolidColorBrush WarningBrush = Freeze(new SolidColorBrush(WarningColor));
        public static readonly SolidColorBrush WarningSoftBrush = Freeze(new SolidColorBrush(WarningSoftColor));

        public static readonly SolidColorBrush DangerBrush = Freeze(new SolidColorBrush(DangerColor));
        public static readonly SolidColorBrush DangerSoftBrush = Freeze(new SolidColorBrush(DangerSoftColor));

        public static readonly SolidColorBrush TextPrimaryBrush = Freeze(new SolidColorBrush(TextPrimaryColor));
        public static readonly SolidColorBrush TextSecondaryBrush = Freeze(new SolidColorBrush(TextSecondaryColor));
        public static readonly SolidColorBrush TextMutedBrush = Freeze(new SolidColorBrush(TextMutedColor));

        // Iconos vectoriales en grilla 24x24
        public const string IconCrosshairData = "M12,3 A9,9 0 1,0 12,21 A9,9 0 1,0 12,3 Z M12,1 L12,6 M12,18 L12,23 M1,12 L6,12 M18,12 L23,12 M12,10.5 A1.5,1.5 0 1,0 12,13.5 A1.5,1.5 0 1,0 12,10.5 Z";
        public const string IconSearchData = "M10.5,3.5 A7,7 0 1,0 10.5,17.5 A7,7 0 1,0 10.5,3.5 Z M15.8,15.8 L20.5,20.5";
        public const string IconPlusData = "M12,5 L12,19 M5,12 L19,12";
        public const string IconDuplicateData = "M9,9 L19,9 L19,19 L9,19 Z M5,15 L5,5 L15,5";
        public const string IconEditData = "M4,20 L8.5,19 L19,8.5 L15.5,5 L5,15.5 Z M13.5,7 L17,10.5";
        public const string IconTrashData = "M4,7 L20,7 M9,7 L9,4 L15,4 L15,7 M6.5,7 L7.5,20 L16.5,20 L17.5,7 M10,11 L10,16 M14,11 L14,16";
        public const string IconSaveData = "M5,4 L16,4 L20,8 L20,20 L4,20 L4,4 Z M8,4 L8,9.5 L15,9.5 L15,4 M8,20 L8,14 L16,14 L16,20";
        public const string IconPinData = "M9,4 L15,4 L14,10 L17,13 L17,15 L12.5,15 L12.5,21 L11.5,21 L11.5,15 L7,15 L7,13 L10,10 Z";
        public const string IconMinimizeData = "M5,12 L19,12";
        public const string IconCloseData = "M6,6 L18,18 M18,6 L6,18";
        public const string IconKeyboardData = "M3,6 L21,6 L21,18 L3,18 Z M6.5,10 L8,10 M11,10 L13,10 M16,10 L17.5,10 M7.5,14 L16.5,14";
        public const string IconSortData = "M4,7 L20,7 M4,12 L15,12 M4,17 L10,17";
        public const string IconCheckData = "M4.5,12.5 L9.5,17.5 L19.5,6.5";
        public const string IconPencilData = "M3,21 L7.5,19.8 L19.5,7.8 L16.2,4.5 L4.2,16.5 Z M14,6.7 L17.3,10";
        public const string IconPlayData = "M7,5 L19,12 L7,19 Z";
        public const string IconStopData = "M6,6 L18,6 L18,18 L6,18 Z";
        public const string IconUndoData = "M9,7 L4,12 L9,17 M4,12 L15,12 A5,5 0 1,1 15,20 L12,20";
        public const string IconFlipData = "M12,4 L12,20 M8,8 L4,12 L8,16 M16,8 L20,12 L16,16";

        private static SolidColorBrush Freeze(SolidColorBrush brush)
        {
            brush.Freeze();
            return brush;
        }

        public static UIElement CreateVectorIcon(string pathData, Brush strokeBrush, double size, double strokeThickness)
        {
            Canvas canvas = new Canvas();
            canvas.Width = 24;
            canvas.Height = 24;

            Path path = new Path();
            path.Data = Geometry.Parse(pathData);
            path.Stroke = strokeBrush;
            path.StrokeThickness = strokeThickness;
            path.StrokeStartLineCap = PenLineCap.Round;
            path.StrokeEndLineCap = PenLineCap.Round;
            path.StrokeLineJoin = PenLineJoin.Round;
            canvas.Children.Add(path);

            Viewbox viewbox = new Viewbox();
            viewbox.Width = size;
            viewbox.Height = size;
            viewbox.Stretch = Stretch.Uniform;
            viewbox.Child = canvas;
            viewbox.IsHitTestVisible = false;
            return viewbox;
        }

        public static ResourceDictionary CreateThemeDictionary()
        {
            StringBuilder xaml = new StringBuilder();
            xaml.AppendLine("<ResourceDictionary xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'");
            xaml.AppendLine("                    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>");

            // Colores reutilizables
            xaml.AppendLine("  <SolidColorBrush x:Key='BgCanvas' Color='#0D0F14'/>");
            xaml.AppendLine("  <SolidColorBrush x:Key='BgSidebar' Color='#12151C'/>");
            xaml.AppendLine("  <SolidColorBrush x:Key='BgCard' Color='#161A23'/>");
            xaml.AppendLine("  <SolidColorBrush x:Key='BgInput' Color='#1D222E'/>");
            xaml.AppendLine("  <SolidColorBrush x:Key='BgInputHover' Color='#242B3A'/>");
            xaml.AppendLine("  <SolidColorBrush x:Key='BorderSubtle' Color='#252A38'/>");
            xaml.AppendLine("  <SolidColorBrush x:Key='BorderStrong' Color='#31394D'/>");
            xaml.AppendLine("  <SolidColorBrush x:Key='Accent' Color='#38BDF8'/>");
            xaml.AppendLine("  <SolidColorBrush x:Key='AccentHover' Color='#0EA5E9'/>");
            xaml.AppendLine("  <SolidColorBrush x:Key='AccentSoft' Color='#142636'/>");
            xaml.AppendLine("  <SolidColorBrush x:Key='Danger' Color='#F43F5E'/>");
            xaml.AppendLine("  <SolidColorBrush x:Key='DangerSoft' Color='#2D161E'/>");
            xaml.AppendLine("  <SolidColorBrush x:Key='TextPrimary' Color='#F1F5F9'/>");
            xaml.AppendLine("  <SolidColorBrush x:Key='TextSecondary' Color='#94A3B8'/>");
            xaml.AppendLine("  <SolidColorBrush x:Key='TextMuted' Color='#64748B'/>");

            // ScrollBar delgada oscura
            xaml.AppendLine("  <Style x:Key='DarkScrollBarThumb' TargetType='Thumb'>");
            xaml.AppendLine("    <Setter Property='OverridesDefaultStyle' Value='True'/>");
            xaml.AppendLine("    <Setter Property='IsTabStop' Value='False'/>");
            xaml.AppendLine("    <Setter Property='Template'>");
            xaml.AppendLine("      <Setter.Value>");
            xaml.AppendLine("        <ControlTemplate TargetType='Thumb'>");
            xaml.AppendLine("          <Border x:Name='ThumbBorder' CornerRadius='3' Background='#334155' Margin='1'/>");
            xaml.AppendLine("          <ControlTemplate.Triggers>");
            xaml.AppendLine("            <Trigger Property='IsMouseOver' Value='True'>");
            xaml.AppendLine("              <Setter TargetName='ThumbBorder' Property='Background' Value='#475569'/>");
            xaml.AppendLine("            </Trigger>");
            xaml.AppendLine("            <Trigger Property='IsDragging' Value='True'>");
            xaml.AppendLine("              <Setter TargetName='ThumbBorder' Property='Background' Value='#38BDF8'/>");
            xaml.AppendLine("            </Trigger>");
            xaml.AppendLine("          </ControlTemplate.Triggers>");
            xaml.AppendLine("        </ControlTemplate>");
            xaml.AppendLine("      </Setter.Value>");
            xaml.AppendLine("    </Setter>");
            xaml.AppendLine("  </Style>");

            xaml.AppendLine("  <Style TargetType='ScrollBar'>");
            xaml.AppendLine("    <Setter Property='OverridesDefaultStyle' Value='True'/>");
            xaml.AppendLine("    <Setter Property='Width' Value='7'/>");
            xaml.AppendLine("    <Setter Property='MinWidth' Value='7'/>");
            xaml.AppendLine("    <Setter Property='Background' Value='Transparent'/>");
            xaml.AppendLine("    <Setter Property='Template'>");
            xaml.AppendLine("      <Setter.Value>");
            xaml.AppendLine("        <ControlTemplate TargetType='ScrollBar'>");
            xaml.AppendLine("          <Grid Background='{TemplateBinding Background}'>");
            xaml.AppendLine("            <Track x:Name='PART_Track' IsDirectionReversed='True'>");
            xaml.AppendLine("              <Track.DecreaseRepeatButton><RepeatButton Opacity='0' Command='ScrollBar.PageUpCommand' IsTabStop='False'/></Track.DecreaseRepeatButton>");
            xaml.AppendLine("              <Track.Thumb><Thumb Style='{StaticResource DarkScrollBarThumb}'/></Track.Thumb>");
            xaml.AppendLine("              <Track.IncreaseRepeatButton><RepeatButton Opacity='0' Command='ScrollBar.PageDownCommand' IsTabStop='False'/></Track.IncreaseRepeatButton>");
            xaml.AppendLine("            </Track>");
            xaml.AppendLine("          </Grid>");
            xaml.AppendLine("        </ControlTemplate>");
            xaml.AppendLine("      </Setter.Value>");
            xaml.AppendLine("    </Setter>");
            xaml.AppendLine("  </Style>");

            // TextBox oscuro
            xaml.AppendLine("  <Style x:Key='DarkTextBox' TargetType='TextBox'>");
            xaml.AppendLine("    <Setter Property='Background' Value='{StaticResource BgInput}'/>");
            xaml.AppendLine("    <Setter Property='Foreground' Value='{StaticResource TextPrimary}'/>");
            xaml.AppendLine("    <Setter Property='BorderBrush' Value='{StaticResource BorderSubtle}'/>");
            xaml.AppendLine("    <Setter Property='BorderThickness' Value='1'/>");
            xaml.AppendLine("    <Setter Property='CaretBrush' Value='{StaticResource Accent}'/>");
            xaml.AppendLine("    <Setter Property='SelectionBrush' Value='#0284C7'/>");
            xaml.AppendLine("    <Setter Property='Padding' Value='8,5'/>");
            xaml.AppendLine("    <Setter Property='VerticalContentAlignment' Value='Center'/>");
            xaml.AppendLine("    <Setter Property='Template'>");
            xaml.AppendLine("      <Setter.Value>");
            xaml.AppendLine("        <ControlTemplate TargetType='TextBox'>");
            xaml.AppendLine("          <Border x:Name='Bd' Background='{TemplateBinding Background}' BorderBrush='{TemplateBinding BorderBrush}' BorderThickness='{TemplateBinding BorderThickness}' CornerRadius='6'>");
            xaml.AppendLine("            <ScrollViewer x:Name='PART_ContentHost' Margin='0' VerticalAlignment='Center'/>");
            xaml.AppendLine("          </Border>");
            xaml.AppendLine("          <ControlTemplate.Triggers>");
            xaml.AppendLine("            <Trigger Property='IsMouseOver' Value='True'>");
            xaml.AppendLine("              <Setter TargetName='Bd' Property='BorderBrush' Value='{StaticResource BorderStrong}'/>");
            xaml.AppendLine("            </Trigger>");
            xaml.AppendLine("            <Trigger Property='IsKeyboardFocused' Value='True'>");
            xaml.AppendLine("              <Setter TargetName='Bd' Property='BorderBrush' Value='{StaticResource Accent}'/>");
            xaml.AppendLine("              <Setter TargetName='Bd' Property='Background' Value='#222836'/>");
            xaml.AppendLine("            </Trigger>");
            xaml.AppendLine("          </ControlTemplate.Triggers>");
            xaml.AppendLine("        </ControlTemplate>");
            xaml.AppendLine("      </Setter.Value>");
            xaml.AppendLine("    </Setter>");
            xaml.AppendLine("  </Style>");

            // Botones
            xaml.AppendLine("  <Style x:Key='PrimaryButton' TargetType='Button'>");
            xaml.AppendLine("    <Setter Property='Background' Value='{StaticResource Accent}'/>");
            xaml.AppendLine("    <Setter Property='Foreground' Value='#091017'/>");
            xaml.AppendLine("    <Setter Property='FontWeight' Value='SemiBold'/>");
            xaml.AppendLine("    <Setter Property='FontSize' Value='12'/>");
            xaml.AppendLine("    <Setter Property='Padding' Value='12,6'/>");
            xaml.AppendLine("    <Setter Property='Cursor' Value='Hand'/>");
            xaml.AppendLine("    <Setter Property='FocusVisualStyle' Value='{x:Null}'/>");
            xaml.AppendLine("    <Setter Property='Template'>");
            xaml.AppendLine("      <Setter.Value>");
            xaml.AppendLine("        <ControlTemplate TargetType='Button'>");
            xaml.AppendLine("          <Border x:Name='Bd' Background='{TemplateBinding Background}' CornerRadius='6' Padding='{TemplateBinding Padding}'>");
            xaml.AppendLine("            <ContentPresenter HorizontalAlignment='Center' VerticalAlignment='Center'/>");
            xaml.AppendLine("          </Border>");
            xaml.AppendLine("          <ControlTemplate.Triggers>");
            xaml.AppendLine("            <Trigger Property='IsMouseOver' Value='True'>");
            xaml.AppendLine("              <Setter TargetName='Bd' Property='Background' Value='#7DD3FC'/>");
            xaml.AppendLine("            </Trigger>");
            xaml.AppendLine("            <Trigger Property='IsPressed' Value='True'>");
            xaml.AppendLine("              <Setter TargetName='Bd' Property='Background' Value='#0EA5E9'/>");
            xaml.AppendLine("            </Trigger>");
            xaml.AppendLine("            <Trigger Property='IsEnabled' Value='False'>");
            xaml.AppendLine("              <Setter TargetName='Bd' Property='Opacity' Value='0.45'/>");
            xaml.AppendLine("            </Trigger>");
            xaml.AppendLine("          </ControlTemplate.Triggers>");
            xaml.AppendLine("        </ControlTemplate>");
            xaml.AppendLine("      </Setter.Value>");
            xaml.AppendLine("    </Setter>");
            xaml.AppendLine("  </Style>");

            xaml.AppendLine("  <Style x:Key='SecondaryButton' TargetType='Button'>");
            xaml.AppendLine("    <Setter Property='Background' Value='{StaticResource BgInput}'/>");
            xaml.AppendLine("    <Setter Property='Foreground' Value='{StaticResource TextPrimary}'/>");
            xaml.AppendLine("    <Setter Property='BorderBrush' Value='{StaticResource BorderSubtle}'/>");
            xaml.AppendLine("    <Setter Property='BorderThickness' Value='1'/>");
            xaml.AppendLine("    <Setter Property='FontWeight' Value='Medium'/>");
            xaml.AppendLine("    <Setter Property='FontSize' Value='12'/>");
            xaml.AppendLine("    <Setter Property='Padding' Value='10,5'/>");
            xaml.AppendLine("    <Setter Property='Cursor' Value='Hand'/>");
            xaml.AppendLine("    <Setter Property='FocusVisualStyle' Value='{x:Null}'/>");
            xaml.AppendLine("    <Setter Property='Template'>");
            xaml.AppendLine("      <Setter.Value>");
            xaml.AppendLine("        <ControlTemplate TargetType='Button'>");
            xaml.AppendLine("          <Border x:Name='Bd' Background='{TemplateBinding Background}' BorderBrush='{TemplateBinding BorderBrush}' BorderThickness='{TemplateBinding BorderThickness}' CornerRadius='6' Padding='{TemplateBinding Padding}'>");
            xaml.AppendLine("            <ContentPresenter HorizontalAlignment='Center' VerticalAlignment='Center'/>");
            xaml.AppendLine("          </Border>");
            xaml.AppendLine("          <ControlTemplate.Triggers>");
            xaml.AppendLine("            <Trigger Property='IsMouseOver' Value='True'>");
            xaml.AppendLine("              <Setter TargetName='Bd' Property='Background' Value='{StaticResource BgInputHover}'/>");
            xaml.AppendLine("              <Setter TargetName='Bd' Property='BorderBrush' Value='{StaticResource BorderStrong}'/>");
            xaml.AppendLine("            </Trigger>");
            xaml.AppendLine("            <Trigger Property='IsPressed' Value='True'>");
            xaml.AppendLine("              <Setter TargetName='Bd' Property='Background' Value='#2C3447'/>");
            xaml.AppendLine("            </Trigger>");
            xaml.AppendLine("            <Trigger Property='IsEnabled' Value='False'>");
            xaml.AppendLine("              <Setter TargetName='Bd' Property='Opacity' Value='0.45'/>");
            xaml.AppendLine("            </Trigger>");
            xaml.AppendLine("          </ControlTemplate.Triggers>");
            xaml.AppendLine("        </ControlTemplate>");
            xaml.AppendLine("      </Setter.Value>");
            xaml.AppendLine("    </Setter>");
            xaml.AppendLine("  </Style>");

            xaml.AppendLine("  <Style x:Key='DangerGhostButton' TargetType='Button'>");
            xaml.AppendLine("    <Setter Property='Background' Value='{StaticResource BgInput}'/>");
            xaml.AppendLine("    <Setter Property='Foreground' Value='{StaticResource Danger}'/>");
            xaml.AppendLine("    <Setter Property='BorderBrush' Value='{StaticResource BorderSubtle}'/>");
            xaml.AppendLine("    <Setter Property='BorderThickness' Value='1'/>");
            xaml.AppendLine("    <Setter Property='FontWeight' Value='Medium'/>");
            xaml.AppendLine("    <Setter Property='FontSize' Value='12'/>");
            xaml.AppendLine("    <Setter Property='Padding' Value='10,5'/>");
            xaml.AppendLine("    <Setter Property='Cursor' Value='Hand'/>");
            xaml.AppendLine("    <Setter Property='FocusVisualStyle' Value='{x:Null}'/>");
            xaml.AppendLine("    <Setter Property='Template'>");
            xaml.AppendLine("      <Setter.Value>");
            xaml.AppendLine("        <ControlTemplate TargetType='Button'>");
            xaml.AppendLine("          <Border x:Name='Bd' Background='{TemplateBinding Background}' BorderBrush='{TemplateBinding BorderBrush}' BorderThickness='{TemplateBinding BorderThickness}' CornerRadius='6' Padding='{TemplateBinding Padding}'>");
            xaml.AppendLine("            <ContentPresenter HorizontalAlignment='Center' VerticalAlignment='Center'/>");
            xaml.AppendLine("          </Border>");
            xaml.AppendLine("          <ControlTemplate.Triggers>");
            xaml.AppendLine("            <Trigger Property='IsMouseOver' Value='True'>");
            xaml.AppendLine("              <Setter TargetName='Bd' Property='Background' Value='{StaticResource DangerSoft}'/>");
            xaml.AppendLine("              <Setter TargetName='Bd' Property='BorderBrush' Value='{StaticResource Danger}'/>");
            xaml.AppendLine("            </Trigger>");
            xaml.AppendLine("            <Trigger Property='IsPressed' Value='True'>");
            xaml.AppendLine("              <Setter TargetName='Bd' Property='Background' Value='#4C1D2A'/>");
            xaml.AppendLine("            </Trigger>");
            xaml.AppendLine("          </ControlTemplate.Triggers>");
            xaml.AppendLine("        </ControlTemplate>");
            xaml.AppendLine("      </Setter.Value>");
            xaml.AppendLine("    </Setter>");
            xaml.AppendLine("  </Style>");

            xaml.AppendLine("  <Style x:Key='DangerSolidButton' TargetType='Button'>");
            xaml.AppendLine("    <Setter Property='Background' Value='{StaticResource Danger}'/>");
            xaml.AppendLine("    <Setter Property='Foreground' Value='#FFFFFF'/>");
            xaml.AppendLine("    <Setter Property='FontWeight' Value='SemiBold'/>");
            xaml.AppendLine("    <Setter Property='FontSize' Value='12'/>");
            xaml.AppendLine("    <Setter Property='Padding' Value='12,6'/>");
            xaml.AppendLine("    <Setter Property='Cursor' Value='Hand'/>");
            xaml.AppendLine("    <Setter Property='FocusVisualStyle' Value='{x:Null}'/>");
            xaml.AppendLine("    <Setter Property='Template'>");
            xaml.AppendLine("      <Setter.Value>");
            xaml.AppendLine("        <ControlTemplate TargetType='Button'>");
            xaml.AppendLine("          <Border x:Name='Bd' Background='{TemplateBinding Background}' CornerRadius='6' Padding='{TemplateBinding Padding}'>");
            xaml.AppendLine("            <ContentPresenter HorizontalAlignment='Center' VerticalAlignment='Center'/>");
            xaml.AppendLine("          </Border>");
            xaml.AppendLine("          <ControlTemplate.Triggers>");
            xaml.AppendLine("            <Trigger Property='IsMouseOver' Value='True'>");
            xaml.AppendLine("              <Setter TargetName='Bd' Property='Background' Value='#FB7185'/>");
            xaml.AppendLine("            </Trigger>");
            xaml.AppendLine("          </ControlTemplate.Triggers>");
            xaml.AppendLine("        </ControlTemplate>");
            xaml.AppendLine("      </Setter.Value>");
            xaml.AppendLine("    </Setter>");
            xaml.AppendLine("  </Style>");

            xaml.AppendLine("  <Style x:Key='TitleBarButton' TargetType='Button'>");
            xaml.AppendLine("    <Setter Property='Background' Value='Transparent'/>");
            xaml.AppendLine("    <Setter Property='Foreground' Value='{StaticResource TextSecondary}'/>");
            xaml.AppendLine("    <Setter Property='Width' Value='30'/>");
            xaml.AppendLine("    <Setter Property='Height' Value='28'/>");
            xaml.AppendLine("    <Setter Property='Cursor' Value='Hand'/>");
            xaml.AppendLine("    <Setter Property='FocusVisualStyle' Value='{x:Null}'/>");
            xaml.AppendLine("    <Setter Property='Template'>");
            xaml.AppendLine("      <Setter.Value>");
            xaml.AppendLine("        <ControlTemplate TargetType='Button'>");
            xaml.AppendLine("          <Border x:Name='Bd' Background='{TemplateBinding Background}' CornerRadius='6'>");
            xaml.AppendLine("            <ContentPresenter HorizontalAlignment='Center' VerticalAlignment='Center'/>");
            xaml.AppendLine("          </Border>");
            xaml.AppendLine("          <ControlTemplate.Triggers>");
            xaml.AppendLine("            <Trigger Property='IsMouseOver' Value='True'>");
            xaml.AppendLine("              <Setter TargetName='Bd' Property='Background' Value='{StaticResource BgInputHover}'/>");
            xaml.AppendLine("            </Trigger>");
            xaml.AppendLine("          </ControlTemplate.Triggers>");
            xaml.AppendLine("        </ControlTemplate>");
            xaml.AppendLine("      </Setter.Value>");
            xaml.AppendLine("    </Setter>");
            xaml.AppendLine("  </Style>");

            xaml.AppendLine("  <Style x:Key='TitleBarCloseButton' TargetType='Button' BasedOn='{StaticResource TitleBarButton}'>");
            xaml.AppendLine("    <Setter Property='Template'>");
            xaml.AppendLine("      <Setter.Value>");
            xaml.AppendLine("        <ControlTemplate TargetType='Button'>");
            xaml.AppendLine("          <Border x:Name='Bd' Background='{TemplateBinding Background}' CornerRadius='6'>");
            xaml.AppendLine("            <ContentPresenter HorizontalAlignment='Center' VerticalAlignment='Center'/>");
            xaml.AppendLine("          </Border>");
            xaml.AppendLine("          <ControlTemplate.Triggers>");
            xaml.AppendLine("            <Trigger Property='IsMouseOver' Value='True'>");
            xaml.AppendLine("              <Setter TargetName='Bd' Property='Background' Value='#F43F5E'/>");
            xaml.AppendLine("            </Trigger>");
            xaml.AppendLine("          </ControlTemplate.Triggers>");
            xaml.AppendLine("        </ControlTemplate>");
            xaml.AppendLine("      </Setter.Value>");
            xaml.AppendLine("    </Setter>");
            xaml.AppendLine("  </Style>");

            xaml.AppendLine("  <Style x:Key='SegmentTabButton' TargetType='Button'>");
            xaml.AppendLine("    <Setter Property='Background' Value='Transparent'/>");
            xaml.AppendLine("    <Setter Property='Foreground' Value='{StaticResource TextSecondary}'/>");
            xaml.AppendLine("    <Setter Property='Height' Value='24'/>");
            xaml.AppendLine("    <Setter Property='Padding' Value='10,0'/>");
            xaml.AppendLine("    <Setter Property='FontSize' Value='11.5'/>");
            xaml.AppendLine("    <Setter Property='FontWeight' Value='SemiBold'/>");
            xaml.AppendLine("    <Setter Property='Cursor' Value='Hand'/>");
            xaml.AppendLine("    <Setter Property='FocusVisualStyle' Value='{x:Null}'/>");
            xaml.AppendLine("    <Setter Property='Template'>");
            xaml.AppendLine("      <Setter.Value>");
            xaml.AppendLine("        <ControlTemplate TargetType='Button'>");
            xaml.AppendLine("          <Border x:Name='Bd' Background='{TemplateBinding Background}' CornerRadius='5' Padding='{TemplateBinding Padding}'>");
            xaml.AppendLine("            <ContentPresenter HorizontalAlignment='Center' VerticalAlignment='Center'/>");
            xaml.AppendLine("          </Border>");
            xaml.AppendLine("          <ControlTemplate.Triggers>");
            xaml.AppendLine("            <Trigger Property='IsMouseOver' Value='True'>");
            xaml.AppendLine("              <Setter TargetName='Bd' Property='Background' Value='#1D222E'/>");
            xaml.AppendLine("            </Trigger>");
            xaml.AppendLine("            <Trigger Property='Tag' Value='Active'>");
            xaml.AppendLine("              <Setter TargetName='Bd' Property='Background' Value='#142636'/>");
            xaml.AppendLine("              <Setter Property='Foreground' Value='#38BDF8'/>");
            xaml.AppendLine("            </Trigger>");
            xaml.AppendLine("          </ControlTemplate.Triggers>");
            xaml.AppendLine("        </ControlTemplate>");
            xaml.AppendLine("      </Setter.Value>");
            xaml.AppendLine("    </Setter>");
            xaml.AppendLine("  </Style>");

            // ComboBoxItem y ComboBox oscuro completo
            xaml.AppendLine("  <Style x:Key='DarkComboBoxItem' TargetType='ComboBoxItem'>");
            xaml.AppendLine("    <Setter Property='Foreground' Value='{StaticResource TextPrimary}'/>");
            xaml.AppendLine("    <Setter Property='Background' Value='Transparent'/>");
            xaml.AppendLine("    <Setter Property='Padding' Value='10,6'/>");
            xaml.AppendLine("    <Setter Property='Cursor' Value='Hand'/>");
            xaml.AppendLine("    <Setter Property='Template'>");
            xaml.AppendLine("      <Setter.Value>");
            xaml.AppendLine("        <ControlTemplate TargetType='ComboBoxItem'>");
            xaml.AppendLine("          <Border x:Name='Bd' Background='{TemplateBinding Background}' Padding='{TemplateBinding Padding}' CornerRadius='4' Margin='2,1'>");
            xaml.AppendLine("            <ContentPresenter/>");
            xaml.AppendLine("          </Border>");
            xaml.AppendLine("          <ControlTemplate.Triggers>");
            xaml.AppendLine("            <Trigger Property='IsHighlighted' Value='True'>");
            xaml.AppendLine("              <Setter TargetName='Bd' Property='Background' Value='#242B3A'/>");
            xaml.AppendLine("            </Trigger>");
            xaml.AppendLine("            <Trigger Property='IsSelected' Value='True'>");
            xaml.AppendLine("              <Setter TargetName='Bd' Property='Background' Value='#142636'/>");
            xaml.AppendLine("              <Setter Property='Foreground' Value='#38BDF8'/>");
            xaml.AppendLine("            </Trigger>");
            xaml.AppendLine("          </ControlTemplate.Triggers>");
            xaml.AppendLine("        </ControlTemplate>");
            xaml.AppendLine("      </Setter.Value>");
            xaml.AppendLine("    </Setter>");
            xaml.AppendLine("  </Style>");

            xaml.AppendLine("  <Style x:Key='DarkComboBox' TargetType='ComboBox'>");
            xaml.AppendLine("    <Setter Property='Foreground' Value='{StaticResource TextPrimary}'/>");
            xaml.AppendLine("    <Setter Property='Background' Value='{StaticResource BgInput}'/>");
            xaml.AppendLine("    <Setter Property='BorderBrush' Value='{StaticResource BorderSubtle}'/>");
            xaml.AppendLine("    <Setter Property='BorderThickness' Value='1'/>");
            xaml.AppendLine("    <Setter Property='Padding' Value='10,6'/>");
            xaml.AppendLine("    <Setter Property='Cursor' Value='Hand'/>");
            xaml.AppendLine("    <Setter Property='FocusVisualStyle' Value='{x:Null}'/>");
            xaml.AppendLine("    <Setter Property='ItemContainerStyle' Value='{StaticResource DarkComboBoxItem}'/>");
            xaml.AppendLine("    <Setter Property='Template'>");
            xaml.AppendLine("      <Setter.Value>");
            xaml.AppendLine("        <ControlTemplate TargetType='ComboBox'>");
            xaml.AppendLine("          <Grid>");
            xaml.AppendLine("            <ToggleButton x:Name='ToggleButton' Focusable='False' ClickMode='Press'");
            xaml.AppendLine("                          IsChecked='{Binding IsDropDownOpen, Mode=TwoWay, RelativeSource={RelativeSource TemplatedParent}}'>");
            xaml.AppendLine("              <ToggleButton.Template>");
            xaml.AppendLine("                <ControlTemplate TargetType='ToggleButton'>");
            xaml.AppendLine("                  <Border x:Name='Bd' Background='#1D222E' BorderBrush='#252A38' BorderThickness='1' CornerRadius='6'>");
            xaml.AppendLine("                    <Grid>");
            xaml.AppendLine("                      <Grid.ColumnDefinitions>");
            xaml.AppendLine("                        <ColumnDefinition Width='*'/>");
            xaml.AppendLine("                        <ColumnDefinition Width='24'/>");
            xaml.AppendLine("                      </Grid.ColumnDefinitions>");
            xaml.AppendLine("                      <Path Grid.Column='1' Data='M0,0 L4,4 L8,0' Stroke='#94A3B8' StrokeThickness='1.5' StrokeStartLineCap='Round' StrokeEndLineCap='Round' HorizontalAlignment='Center' VerticalAlignment='Center'/>");
            xaml.AppendLine("                    </Grid>");
            xaml.AppendLine("                  </Border>");
            xaml.AppendLine("                  <ControlTemplate.Triggers>");
            xaml.AppendLine("                    <Trigger Property='IsMouseOver' Value='True'>");
            xaml.AppendLine("                      <Setter TargetName='Bd' Property='Background' Value='#242B3A'/>");
            xaml.AppendLine("                      <Setter TargetName='Bd' Property='BorderBrush' Value='#31394D'/>");
            xaml.AppendLine("                    </Trigger>");
            xaml.AppendLine("                    <Trigger Property='IsChecked' Value='True'>");
            xaml.AppendLine("                      <Setter TargetName='Bd' Property='BorderBrush' Value='#38BDF8'/>");
            xaml.AppendLine("                    </Trigger>");
            xaml.AppendLine("                  </ControlTemplate.Triggers>");
            xaml.AppendLine("                </ControlTemplate>");
            xaml.AppendLine("              </ToggleButton.Template>");
            xaml.AppendLine("            </ToggleButton>");
            xaml.AppendLine("            <ContentPresenter x:Name='ContentSite' IsHitTestVisible='False'");
            xaml.AppendLine("                              Content='{TemplateBinding SelectionBoxItem}'");
            xaml.AppendLine("                              ContentTemplate='{TemplateBinding SelectionBoxItemTemplate}'");
            xaml.AppendLine("                              Margin='10,5,28,5' VerticalAlignment='Center' HorizontalAlignment='Left'/>");
            xaml.AppendLine("            <Popup x:Name='Popup' Placement='Bottom' IsOpen='{TemplateBinding IsDropDownOpen}' AllowsTransparency='True' Focusable='False' PopupAnimation='Fade'>");
            xaml.AppendLine("              <Border x:Name='DropDownBorder' MinWidth='{TemplateBinding ActualWidth}' MaxHeight='240' Margin='0,4,0,0'");
            xaml.AppendLine("                      Background='#161A23' BorderBrush='#31394D' BorderThickness='1' CornerRadius='6' Padding='2'>");
            xaml.AppendLine("                <ScrollViewer SnapsToDevicePixels='True' VerticalScrollBarVisibility='Auto'>");
            xaml.AppendLine("                  <StackPanel IsItemsHost='True' KeyboardNavigation.DirectionalNavigation='Contained'/>");
            xaml.AppendLine("                </ScrollViewer>");
            xaml.AppendLine("              </Border>");
            xaml.AppendLine("            </Popup>");
            xaml.AppendLine("          </Grid>");
            xaml.AppendLine("        </ControlTemplate>");
            xaml.AppendLine("      </Setter.Value>");
            xaml.AppendLine("    </Setter>");
            xaml.AppendLine("  </Style>");

            // ListBoxItem limpio para la lista de perfiles
            xaml.AppendLine("  <Style x:Key='ProfileListItemStyle' TargetType='ListBoxItem'>");
            xaml.AppendLine("    <Setter Property='HorizontalContentAlignment' Value='Stretch'/>");
            xaml.AppendLine("    <Setter Property='Padding' Value='0'/>");
            xaml.AppendLine("    <Setter Property='Margin' Value='0,2'/>");
            xaml.AppendLine("    <Setter Property='Cursor' Value='Hand'/>");
            xaml.AppendLine("    <Setter Property='FocusVisualStyle' Value='{x:Null}'/>");
            xaml.AppendLine("    <Setter Property='Template'>");
            xaml.AppendLine("      <Setter.Value>");
            xaml.AppendLine("        <ControlTemplate TargetType='ListBoxItem'>");
            xaml.AppendLine("          <Border x:Name='ItemBd' Background='Transparent' BorderBrush='Transparent' BorderThickness='1' CornerRadius='7' Padding='8,7'>");
            xaml.AppendLine("            <Grid>");
            xaml.AppendLine("              <Border x:Name='AccentBar' Width='3' HorizontalAlignment='Left' CornerRadius='1.5' Background='#38BDF8' Visibility='Collapsed' Margin='-4,1,0,1'/>");
            xaml.AppendLine("              <ContentPresenter Margin='6,0,0,0'/>");
            xaml.AppendLine("            </Grid>");
            xaml.AppendLine("          </Border>");
            xaml.AppendLine("          <ControlTemplate.Triggers>");
            xaml.AppendLine("            <Trigger Property='IsMouseOver' Value='True'>");
            xaml.AppendLine("              <Setter TargetName='ItemBd' Property='Background' Value='#1A1F2B'/>");
            xaml.AppendLine("            </Trigger>");
            xaml.AppendLine("            <Trigger Property='IsSelected' Value='True'>");
            xaml.AppendLine("              <Setter TargetName='ItemBd' Property='Background' Value='#182434'/>");
            xaml.AppendLine("              <Setter TargetName='ItemBd' Property='BorderBrush' Value='#264B66'/>");
            xaml.AppendLine("              <Setter TargetName='AccentBar' Property='Visibility' Value='Visible'/>");
            xaml.AppendLine("            </Trigger>");
            xaml.AppendLine("          </ControlTemplate.Triggers>");
            xaml.AppendLine("        </ControlTemplate>");
            xaml.AppendLine("      </Setter.Value>");
            xaml.AppendLine("    </Setter>");
            xaml.AppendLine("  </Style>");

            // CheckBox oscuro
            xaml.AppendLine("  <Style x:Key='DarkCheckBox' TargetType='CheckBox'>");
            xaml.AppendLine("    <Setter Property='Foreground' Value='{StaticResource TextSecondary}'/>");
            xaml.AppendLine("    <Setter Property='Cursor' Value='Hand'/>");
            xaml.AppendLine("    <Setter Property='FocusVisualStyle' Value='{x:Null}'/>");
            xaml.AppendLine("    <Setter Property='Template'>");
            xaml.AppendLine("      <Setter.Value>");
            xaml.AppendLine("        <ControlTemplate TargetType='CheckBox'>");
            xaml.AppendLine("          <StackPanel Orientation='Horizontal' VerticalAlignment='Center'>");
            xaml.AppendLine("            <Border x:Name='Box' Width='16' Height='16' CornerRadius='4' Background='#1D222E' BorderBrush='#31394D' BorderThickness='1' Margin='0,0,8,0'>");
            xaml.AppendLine("              <Path x:Name='CheckMark' Data='M3.5,8 L6.8,11.5 L12.5,4.5' Stroke='#091017' StrokeThickness='2' StrokeStartLineCap='Round' StrokeEndLineCap='Round' Visibility='Collapsed'/>");
            xaml.AppendLine("            </Border>");
            xaml.AppendLine("            <ContentPresenter VerticalAlignment='Center'/>");
            xaml.AppendLine("          </StackPanel>");
            xaml.AppendLine("          <ControlTemplate.Triggers>");
            xaml.AppendLine("            <Trigger Property='IsChecked' Value='True'>");
            xaml.AppendLine("              <Setter TargetName='Box' Property='Background' Value='#38BDF8'/>");
            xaml.AppendLine("              <Setter TargetName='Box' Property='BorderBrush' Value='#38BDF8'/>");
            xaml.AppendLine("              <Setter TargetName='CheckMark' Property='Visibility' Value='Visible'/>");
            xaml.AppendLine("            </Trigger>");
            xaml.AppendLine("            <Trigger Property='IsMouseOver' Value='True'>");
            xaml.AppendLine("              <Setter TargetName='Box' Property='BorderBrush' Value='#38BDF8'/>");
            xaml.AppendLine("            </Trigger>");
            xaml.AppendLine("          </ControlTemplate.Triggers>");
            xaml.AppendLine("        </ControlTemplate>");
            xaml.AppendLine("      </Setter.Value>");
            xaml.AppendLine("    </Setter>");
            xaml.AppendLine("  </Style>");

            xaml.AppendLine("</ResourceDictionary>");

            return (ResourceDictionary)XamlReader.Parse(xaml.ToString());
        }
    }
}
