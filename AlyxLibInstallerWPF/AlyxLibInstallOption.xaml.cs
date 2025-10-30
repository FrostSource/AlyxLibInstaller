using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace AlyxLibInstallerWPF;
/// <summary>
/// Interaction logic for AlyxLibInstallOption.xaml
/// </summary>
public partial class AlyxLibInstallOption : UserControl
{

    public AlyxLibInstallOption()
    {
        this.InitializeComponent();

        CheckBoxControl.Click += CheckBoxControl_Click;
        CheckBoxControl.Checked += CheckBoxControl_Click;
    }

    public event RoutedEventHandler Click;

    private void CheckBoxControl_Click(object sender, RoutedEventArgs e)
    {
        IsChecked = CheckBoxControl.IsChecked;
        Click?.Invoke(this, e);
    }

    // Add this event handler for the Border's PointerPressed event
    private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        // Prevent toggling if the original source is the CheckBox itself or its children
        if (e.OriginalSource is FrameworkElement fe &&
            (fe == CheckBoxControl || IsDescendantOf(fe, CheckBoxControl)))
        {
            return;
        }

        // Toggle the checkbox
        if (CheckBoxControl.IsChecked == true)
            CheckBoxControl.IsChecked = false;
        else
            CheckBoxControl.IsChecked = true;

        // Raise the Click event
        Click?.Invoke(this, new RoutedEventArgs());
    }

    // Helper to check if an element is a descendant of another
    private bool IsDescendantOf(FrameworkElement child, FrameworkElement parent)
    {
        DependencyObject current = child;
        while (current != null)
        {
            if (current == parent)
                return true;
            current = VisualTreeHelper.GetParent(current);
        }
        return false;
    }

    public static readonly DependencyProperty IsCheckedProperty =
    DependencyProperty.Register(
        nameof(IsChecked),
        typeof(bool?),
        typeof(AlyxLibInstallOption),
        new FrameworkPropertyMetadata(
            false,
            FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
            OnIsCheckedChanged
        )
    );

    public bool? IsChecked
    {
        //get { return CheckBoxControl.IsChecked; }
        //set { CheckBoxControl.IsChecked = value; }
        get => (bool?)GetValue(IsCheckedProperty);
        set => SetValue(IsCheckedProperty, value);
    }

    // Sync the inner CheckBox whenever the dependency property changes
    private static void OnIsCheckedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is AlyxLibInstallOption control)
        {
            control.CheckBoxControl.IsChecked = (bool?)e.NewValue;
        }
    }

    public void ShowInfoBar(InfoBarSeverity severity, string text)
    {
        OptionInfoBar.Severity = severity;
        OptionInfoBar.Message = text;
        OptionInfoBar.IsOpen = true;
    }
    public void HideInfoBar()
    {
        OptionInfoBar.IsOpen = false;
    }

    public string CheckboxContent
    {
        get { return (string)GetValue(CheckboxContentProperty); }
        set { SetValue(CheckboxContentProperty, value); }
    }

    public static readonly DependencyProperty CheckboxContentProperty =
        DependencyProperty.Register("CheckboxContent", typeof(string), typeof(AlyxLibInstallOption), new PropertyMetadata(string.Empty));
    // Dependency property for Tooltip text
    public string TooltipText
    {
        get { return (string)GetValue(TooltipTextProperty); }
        set
        {
            if (string.IsNullOrEmpty(value))
            {
                SetValue(TooltipTextProperty, null);
            }
            else
            {
                SetValue(TooltipTextProperty, value);
            }
        }
    }

    public static readonly DependencyProperty TooltipTextProperty =
        DependencyProperty.Register("TooltipText", typeof(string), typeof(AlyxLibInstallOption), new PropertyMetadata(string.Empty));

    // Dependency property for Description text
    public string DescriptionText
    {
        get { return (string)GetValue(DescriptionTextProperty); }
        set { SetValue(DescriptionTextProperty, value); }
    }

    public static readonly DependencyProperty DescriptionTextProperty =
        DependencyProperty.Register("DescriptionText", typeof(string), typeof(AlyxLibInstallOption), new PropertyMetadata(string.Empty));

}
