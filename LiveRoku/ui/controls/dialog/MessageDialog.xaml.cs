using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace LiveRoku.UI {
    public partial class MessageDialog : Window {
        public double StrokeThickness {
            get { return (double) GetValue (StrokeThicknessProperty); }
            set { SetValue (StrokeThicknessProperty, value); }
        }
        public string TypeTitle {
            get { return (string) GetValue (TypeTitleProperty); }
            set { SetValue (TypeTitleProperty, value); }
        }
        public string PrimaryButtonText {
            get { return (string) GetValue (PrimaryButtonTextProperty); }
            set { SetValue (PrimaryButtonTextProperty, value); }
        }
        public string SecondaryButtonText {
            get { return (string) GetValue (SecondaryButtonTextProperty); }
            set { SetValue (SecondaryButtonTextProperty, value); }
        }
        public new DialogResult DialogResult {
            get { return (DialogResult) GetValue (DialogResultProperty); }
            set { SetValue (DialogResultProperty, value); }
        }
        public DialogResult ResultOnPressEnter {
            get { return (DialogResult) GetValue (ResultOnPressEnterProperty); }
            set { SetValue (ResultOnPressEnterProperty, value); }
        }
        public Visibility CloseButtonVisibility {
            get { return (Visibility) GetValue (CloseButtonVisibilityProperty); }
            set { SetValue (CloseButtonVisibilityProperty, value); }
        }

        public static readonly DependencyProperty StrokeThicknessProperty =
            DependencyProperty.Register ("StrokeThickness", typeof (double), typeof (MessageDialog), new PropertyMetadata (0d));
        public static readonly DependencyProperty TypeTitleProperty =
            DependencyProperty.Register ("TypeTitle", typeof (string), typeof (MessageDialog), new PropertyMetadata ("Message"));
        public static readonly DependencyProperty PrimaryButtonTextProperty =
            DependencyProperty.Register ("PrimaryButtonText", typeof (string), typeof (MessageDialog), new PropertyMetadata ("OK"));
        public static readonly DependencyProperty SecondaryButtonTextProperty =
            DependencyProperty.Register ("SecondaryButtonText", typeof (string), typeof (MessageDialog), new PropertyMetadata ("Cancel"));
        public static readonly DependencyProperty DialogResultProperty =
            DependencyProperty.Register ("DialogResult", typeof (DialogResult), typeof (MessageDialog), new PropertyMetadata (DialogResult.Dismiss));
        public static readonly DependencyProperty ResultOnPressEnterProperty =
            DependencyProperty.Register ("ResultOnPressEnter", typeof (DialogResult), typeof (MessageDialog), new PropertyMetadata (DialogResult.Primary));
        public static readonly DependencyProperty CloseButtonVisibilityProperty =
            DependencyProperty.Register ("CloseButtonVisibility", typeof (Visibility), typeof (MessageDialog), new PropertyMetadata (Visibility.Collapsed));

        private Label title;
        private Button closeButton;
        private Button primaryButton;
        private Button secondaryButton;
        private FrameworkElement templateRoot;
        private System.Windows.Shapes.Rectangle border;

        public MessageDialog (Window win) {
            InitializeComponent ();
            if (win != null) {
                this.Owner = win;
            }
            this.Loaded += onLoaded;
        }
        public MessageDialog () : this (Application.Current.MainWindow) { }

        //Why need this? If show string with new line("\n")
        //Use ContentPresenter will case "adjustSize" method
        //can't get a right size for adapt window
        //But when add textblock by ourselves
        //Problem solved.
        public DialogResult ShowText (string title, string message) {
            this.Title = title;
            this.Content = new TextBlock () {
                Text = message,
                TextWrapping = TextWrapping.WrapWithOverflow
            };
            this.ShowDialog ();
            return DialogResult;
        }

        public override void OnApplyTemplate () {
            base.OnApplyTemplate ();
            //ensure unsubscribe handlers
            this.unsubscribeHandlers ();
            //Get template children
            this.title = GetTemplateChild ("PART_Title") as Label;
            this.closeButton = GetTemplateChild ("PART_CloseButton") as Button;
            this.primaryButton = GetTemplateChild ("PART_PrimaryButton") as Button;
            this.secondaryButton = GetTemplateChild ("PART_SecondaryButton") as Button;
            this.templateRoot = GetTemplateChild ("PART_TemplateRoot") as FrameworkElement;
            this.border = GetTemplateChild ("PART_Border") as System.Windows.Shapes.Rectangle;

            this.adjustSize ();
            //subscribe handlers
            this.subscribeHandler ();
        }

        protected override void OnContentChanged (object oldContent, object newContent) {
            base.OnContentChanged (oldContent, newContent);
            this.adjustSize ();
        }

        protected override void OnClosing (System.ComponentModel.CancelEventArgs e) {
            this.unsubscribeHandlers ();
            Keyboard.ClearFocus ();
            base.OnClosing (e);
        }

        protected override void OnKeyDown (KeyEventArgs e) {
            base.OnKeyDown (e);
            if (e.Key == Key.Enter) {
                if (ResultOnPressEnter == DialogResult.Primary) {
                    if (primaryButton != null)
                        primaryButton.RaiseEvent (new RoutedEventArgs (ButtonBase.ClickEvent, primaryButton));
                    else onPrimaryButtonClick (this, new RoutedEventArgs ());
                } else if (ResultOnPressEnter == DialogResult.Secondary) {
                    if (secondaryButton != null)
                        secondaryButton.RaiseEvent (new RoutedEventArgs (ButtonBase.ClickEvent, secondaryButton));
                    else onSecondaryButtonClick (this, new RoutedEventArgs ());
                } else {
                    if (closeButton != null)
                        closeButton.RaiseEvent (new RoutedEventArgs (ButtonBase.ClickEvent, closeButton));
                    else onCloseBtnClick (this, new RoutedEventArgs ());
                }
                e.Handled = true;
            } else if (e.Key == Key.Escape || e.Key == Key.Back) {
                if (closeButton != null)
                    closeButton.RaiseEvent (new RoutedEventArgs (ButtonBase.ClickEvent, closeButton));
                else onCloseBtnClick (this, new RoutedEventArgs ());
                e.Handled = true;
            }
        }

        protected override void OnActivated (EventArgs e) {
            base.OnActivated (e);
            if (title != null) title.Foreground = Brushes.Black;
            if (border != null) border.Stroke = SystemParameters.WindowGlassBrush;
        }

        protected override void OnDeactivated (EventArgs e) {
            base.OnDeactivated (e);
            if (title != null) title.Foreground = Brushes.LightGray;
            if (border != null) border.Stroke = Brushes.LightGray;
        }

        private void onLoaded (object sender, RoutedEventArgs e) {
            this.Loaded -= onLoaded;
            Keyboard.Focus (this);
        }

        private void adjustSize () {
            if (templateRoot != null) {
                templateRoot.Measure (new Size (this.MaxWidth, this.MaxHeight));
                Size desiredSize = templateRoot.DesiredSize;
                this.Width = desiredSize.Width;
                this.Height = desiredSize.Height;
                adjustWindowLocation ();
            }
        }

        //For some reason, use sizetocontent case problem
        //so calculate by self
        private void adjustWindowLocation () {
            if (this.Owner != null) {
                if (WindowStartupLocation != WindowStartupLocation.Manual)
                    WindowStartupLocation = WindowStartupLocation.Manual;
                this.Top = (this.Owner.ActualHeight - this.ActualHeight) / 2 + this.Owner.Top;
                this.Left = (this.Owner.ActualWidth - this.ActualWidth) / 2 + this.Owner.Left;
            }
        }

        private void subscribeHandler () {
            if (border != null) border.MouseDown += onTitleAreaDrag;
            if (closeButton != null) closeButton.Click += onCloseBtnClick;
            if (primaryButton != null) primaryButton.Click += onPrimaryButtonClick;
            if (secondaryButton != null) secondaryButton.Click += onSecondaryButtonClick;
        }

        private void unsubscribeHandlers () {
            if (border != null) border.MouseDown -= onTitleAreaDrag;
            if (closeButton != null) closeButton.Click -= onCloseBtnClick;
            if (primaryButton != null) primaryButton.Click -= onPrimaryButtonClick;
            if (secondaryButton != null) secondaryButton.Click -= onSecondaryButtonClick;
        }

        private void onTitleAreaDrag (object sender, MouseButtonEventArgs e) {
            if (e.ChangedButton == MouseButton.Left && e.ButtonState == MouseButtonState.Pressed) {
                this.DragMove ();
            }
        }

        private void onCloseBtnClick (object sender, RoutedEventArgs e) {
            DialogResult = DialogResult.Dismiss;
            this.Close ();
        }

        private void onPrimaryButtonClick (object sender, RoutedEventArgs e) {
            DialogResult = DialogResult.Primary;
            this.Close ();
        }

        private void onSecondaryButtonClick (object sender, RoutedEventArgs e) {
            DialogResult = DialogResult.Secondary;
            this.Close ();
        }
    }
}