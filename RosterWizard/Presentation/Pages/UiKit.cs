using System.Windows.Input;

namespace Kronos.RosterWizard.Presentation.Pages;

internal static class UiKit
{
    public static readonly Color Ink = Color.FromArgb("#203532");
    public static readonly Color Muted = Color.FromArgb("#71817E");
    public static readonly Color Accent = Color.FromArgb("#276C66");
    public static readonly Color AccentLight = Color.FromArgb("#E5F3F0");
    public static readonly Color Surface = Colors.White;
    public static readonly Color Canvas = Color.FromArgb("#F4F7F6");
    public static readonly Color Line = Color.FromArgb("#DCE4E2");

    public static Label Heading(string text, double size = 24) => new()
    {
        Text = text,
        FontSize = size,
        FontAttributes = FontAttributes.Bold,
        TextColor = Ink
    };

    public static Label Caption(string text) => new()
    {
        Text = text,
        FontSize = 13,
        TextColor = Muted
    };

    public static Button Action(string text, ICommand? command = null) => new()
    {
        Text = text,
        Command = command,
        BackgroundColor = Accent,
        TextColor = Colors.White,
        CornerRadius = 8,
        Padding = new Thickness(14, 10)
    };

    public static Button Secondary(string text, ICommand? command = null) => new()
    {
        Text = text,
        Command = command,
        BackgroundColor = AccentLight,
        TextColor = Accent,
        CornerRadius = 8,
        Padding = new Thickness(14, 10)
    };

    public static Border Card(View content) => new()
    {
        Stroke = Line,
        StrokeThickness = 1,
        BackgroundColor = Surface,
        Padding = 16,
        StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 12 },
        Content = content
    };

    public static ScrollView PageContent(View content) => new()
    {
        BackgroundColor = Canvas,
        Content = new VerticalStackLayout
        {
            Padding = new Thickness(24),
            Spacing = 18,
            Children = { content }
        }
    };
}
