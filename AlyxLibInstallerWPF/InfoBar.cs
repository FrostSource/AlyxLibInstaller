using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace AlyxLibInstallerWPF
{
    public enum InfoBarSeverity
    {
        Informational,
        Success,
        Warning,
        Error
    }

    public class InfoBar : ContentControl
    {
        static InfoBar()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(InfoBar),
                new FrameworkPropertyMetadata(typeof(InfoBar)));
        }

        public static readonly DependencyProperty IsOpenProperty =
            DependencyProperty.Register(nameof(IsOpen), typeof(bool), typeof(InfoBar),
                new PropertyMetadata(true, OnIsOpenChanged));

        public static readonly DependencyProperty SeverityProperty =
            DependencyProperty.Register(nameof(Severity), typeof(InfoBarSeverity), typeof(InfoBar),
                new PropertyMetadata(InfoBarSeverity.Informational));

        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(nameof(Title), typeof(string), typeof(InfoBar),
                new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty MessageProperty =
            DependencyProperty.Register(nameof(Message), typeof(string), typeof(InfoBar),
                new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty IsClosableProperty =
            DependencyProperty.Register(nameof(IsClosable), typeof(bool), typeof(InfoBar),
                new PropertyMetadata(true));

        public static readonly DependencyProperty IconSourceProperty =
            DependencyProperty.Register(nameof(IconSource), typeof(Geometry), typeof(InfoBar),
                new PropertyMetadata(null));

        public static readonly DependencyProperty ActionButtonProperty =
            DependencyProperty.Register(nameof(ActionButton), typeof(ButtonBase), typeof(InfoBar),
                new PropertyMetadata(null));

        public bool IsOpen
        {
            get => (bool)GetValue(IsOpenProperty);
            set => SetValue(IsOpenProperty, value);
        }

        public InfoBarSeverity Severity
        {
            get => (InfoBarSeverity)GetValue(SeverityProperty);
            set => SetValue(SeverityProperty, value);
        }

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public string Message
        {
            get => (string)GetValue(MessageProperty);
            set => SetValue(MessageProperty, value);
        }

        public bool IsClosable
        {
            get => (bool)GetValue(IsClosableProperty);
            set => SetValue(IsClosableProperty, value);
        }

        public Geometry IconSource
        {
            get => (Geometry)GetValue(IconSourceProperty);
            set => SetValue(IconSourceProperty, value);
        }

        public ButtonBase ActionButton
        {
            get => (ButtonBase)GetValue(ActionButtonProperty);
            set => SetValue(ActionButtonProperty, value);
        }

        public static readonly RoutedEvent CloseButtonClickEvent =
            EventManager.RegisterRoutedEvent(nameof(CloseButtonClick), RoutingStrategy.Bubble,
                typeof(RoutedEventHandler), typeof(InfoBar));

        public static readonly RoutedEvent ClosedEvent =
            EventManager.RegisterRoutedEvent(nameof(Closed), RoutingStrategy.Bubble,
                typeof(RoutedEventHandler), typeof(InfoBar));

        public static readonly RoutedEvent ClosingEvent =
            EventManager.RegisterRoutedEvent(nameof(Closing), RoutingStrategy.Bubble,
                typeof(RoutedEventHandler), typeof(InfoBar));

        public event RoutedEventHandler CloseButtonClick
        {
            add => AddHandler(CloseButtonClickEvent, value);
            remove => RemoveHandler(CloseButtonClickEvent, value);
        }

        public event RoutedEventHandler Closed
        {
            add => AddHandler(ClosedEvent, value);
            remove => RemoveHandler(ClosedEvent, value);
        }

        public event RoutedEventHandler Closing
        {
            add => AddHandler(ClosingEvent, value);
            remove => RemoveHandler(ClosingEvent, value);
        }

        private Button _closeButton;

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            if (_closeButton != null)
                _closeButton.Click -= OnCloseButtonClick;

            _closeButton = GetTemplateChild("PART_CloseButton") as Button;

            if (_closeButton != null)
                _closeButton.Click += OnCloseButtonClick;
        }

        private void OnCloseButtonClick(object sender, RoutedEventArgs e)
        {
            var args = new RoutedEventArgs(CloseButtonClickEvent);
            RaiseEvent(args);

            var closingArgs = new RoutedEventArgs(ClosingEvent);
            RaiseEvent(closingArgs);

            IsOpen = false;

            var closedArgs = new RoutedEventArgs(ClosedEvent);
            RaiseEvent(closedArgs);
        }

        private static void OnIsOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var infoBar = (InfoBar)d;
            infoBar.Visibility = (bool)e.NewValue ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}
