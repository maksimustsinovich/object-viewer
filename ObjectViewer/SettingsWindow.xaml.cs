using System.Windows;
using System.Windows.Media;
using ObjectViewer.Enum;
using Color = System.Drawing.Color;

namespace ObjectViewer;

public partial class SettingsWindow : Window
{
    public Color BackgroundColor { get; private set; }
    public Color GridColor { get; private set; }
    public Color ModelColor { get; private set; }
    public RenderType SelectedRenderType { get; private set; }

    public SettingsWindow(Color backgroundColor, Color gridColor, Color modelColor, RenderType renderType)
    {
        InitializeComponent();

        BackgroundColor = backgroundColor;
        GridColor = gridColor;
        ModelColor = modelColor;
        SelectedRenderType = renderType;

        BackgroundColorPicker.SelectedColor = ToMediaColor(backgroundColor);
        GridColorPicker.SelectedColor = ToMediaColor(gridColor);
        ModelColorPicker.SelectedColor = ToMediaColor(modelColor);
        RenderTypeComboBox.ItemsSource = System.Enum.GetValues<RenderType>();
        
        RenderTypeComboBox.SelectedItem = renderType;
        
    }

    private void ColorPicker_SelectedColorChanged(object sender,
        RoutedPropertyChangedEventArgs<System.Windows.Media.Color?> routedPropertyChangedEventArgs)
    {
    }

    private void ApplyButton_Click(object sender, RoutedEventArgs e)
    {
        BackgroundColor = ToDrawingColor(BackgroundColorPicker.SelectedColor ?? Colors.LightGray);
        GridColor = ToDrawingColor(GridColorPicker.SelectedColor ?? Colors.DarkGray);
        ModelColor = ToDrawingColor(ModelColorPicker.SelectedColor ?? Colors.Red);

        SelectedRenderType = RenderTypeComboBox.SelectedItem is RenderType type ? type : RenderType.Wireframe;

        DialogResult = true;
        Close();
    }

    private static System.Windows.Media.Color ToMediaColor(Color drawingColor)
    {
        return System.Windows.Media.Color.FromArgb(drawingColor.A, drawingColor.R, drawingColor.G, drawingColor.B);
    }

    private static Color ToDrawingColor(System.Windows.Media.Color mediaColor)
    {
        return Color.FromArgb(mediaColor.A, mediaColor.R, mediaColor.G, mediaColor.B);
    }
}