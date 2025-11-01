using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace AlyxLibInstallerWPF;
public class PlaceholderAdorner : Adorner
{
    private readonly TextBlock _placeholder;

    public string PlaceholderText
    {
        get => _placeholder.Text;
        set => _placeholder.Text = value;
    }

    public PlaceholderAdorner(UIElement adornedElement, string placeholderText)
        : base(adornedElement)
    {
        IsHitTestVisible = false;
        _placeholder = new TextBlock
        {
            Text = placeholderText,
            Foreground = Brushes.Gray,
            FontStyle = FontStyles.Italic,
            Margin = new Thickness(4, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center
        };
        AddVisualChild(_placeholder);

        //(adornedElement as TextBox)!.TextChanged += (s, e) => InvalidateVisual();

        if (adornedElement is TextBox tb)
            tb.TextChanged += (s, e) => UpdateVisibility();

        UpdateVisibility();
    }

    private void UpdateVisibility()
    {
        if (AdornedElement is TextBox tb)
            _placeholder.Visibility = string.IsNullOrEmpty(tb.Text) ? Visibility.Visible : Visibility.Collapsed;
    }

    protected override int VisualChildrenCount => 1;
    protected override Visual GetVisualChild(int index) => _placeholder;

    protected override Size MeasureOverride(Size constraint)
    {
        _placeholder.Measure(constraint);
        return base.MeasureOverride(constraint);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        //_placeholder.Arrange(new Rect(finalSize));
        //return finalSize;
        var textBox = (TextBox)AdornedElement;
        var padding = textBox.Padding;

        // Position inside the padded client area
        var rect = new Rect(
            padding.Left,
            padding.Top,
            Math.Max(0, finalSize.Width - padding.Left - padding.Right),
            Math.Max(0, finalSize.Height - padding.Top - padding.Bottom));

        _placeholder.Arrange(rect);
        return finalSize;

    }

    //protected override void OnRender(DrawingContext drawingContext)
    //{
    //    var textBox = (TextBox)AdornedElement;
    //    _placeholder.Visibility = string.IsNullOrEmpty(textBox.Text)
    //        ? Visibility.Visible
    //        : Visibility.Collapsed;
    //}

    //Style="{StaticResource PlaceholderTextBox}"

}
