using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace AlyxLibInstallerWPF;
public class TextBoxHelper
{
    //public static string GetPlaceholder(DependencyObject obj)
    //{
    //    return (string)obj.GetValue(PlaceholderProperty);
    //}

    //public static void SetPlaceholder(DependencyObject obj, string value)
    //{
    //    obj.SetValue(PlaceholderProperty, value);
    //}

    //public static readonly DependencyProperty PlaceholderProperty =
    //    DependencyProperty.RegisterAttached(
    //        "Placeholder",
    //        typeof(string),
    //        typeof(TextBoxHelper),
    //        new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty PlaceholderProperty =
        DependencyProperty.RegisterAttached(
            "Placeholder",
            typeof(string),
            typeof(TextBoxHelper),
            new FrameworkPropertyMetadata(string.Empty, OnPlaceholderChanged));

    public static void SetPlaceholder(DependencyObject element, string value) =>
        element.SetValue(PlaceholderProperty, value);

    public static string GetPlaceholder(DependencyObject element) =>
        (string)element.GetValue(PlaceholderProperty);

    private static void OnPlaceholderChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TextBox textBox)
        {
            //textBox.Loaded += (s, _) =>
            //{
            //    var layer = AdornerLayer.GetAdornerLayer(textBox);
            //    if (layer != null)
            //    {
            //        layer.Add(new PlaceholderAdorner(textBox, e.NewValue?.ToString() ?? string.Empty));
            //    }
            //};
            void ApplyAdorner()
            {
                var layer = AdornerLayer.GetAdornerLayer(textBox);
                if (layer != null)
                {
                    layer.Add(new PlaceholderAdorner(textBox, e.NewValue?.ToString() ?? string.Empty));
                }
            }

            if (textBox.IsLoaded)
            {
                ApplyAdorner();
            }
            else
            {
                textBox.Loaded += (s, _) => ApplyAdorner();
            }

        }
    }

}
