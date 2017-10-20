using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace LiveRoku.UI.controls {
    public class ExpendEx : ContentControl {
        private static RoutedCommand changeExpandedCommand;
        public static RoutedCommand ChangeExpandedCommand => changeExpandedCommand;

        private static void initializeCommand () {
            changeExpandedCommand = new RoutedCommand ("ToNextDisplayModeCommand", typeof (ExpendEx));
            CommandManager.RegisterClassCommandBinding (typeof (ExpendEx),
                new CommandBinding (ChangeExpandedCommand, executeChangeExpanded));
        }

        private static void executeChangeExpanded (object sender, ExecutedRoutedEventArgs e) {
            var view = sender as ExpendEx;
            if (view != null) {
                view.IsExpanded = !view.IsExpanded;
            }
        }

        static ExpendEx () {
            DefaultStyleKeyProperty.OverrideMetadata (typeof (ExpendEx), new FrameworkPropertyMetadata (typeof (ExpendEx)));
            KeyboardNavigation.TabNavigationProperty.OverrideMetadata (typeof (ExpendEx), new FrameworkPropertyMetadata (KeyboardNavigationMode.Cycle));
            initializeCommand ();
        }

        public bool IsExpanded {
            get { return (bool) GetValue (IsExpandedProperty); }
            set { SetValue (IsExpandedProperty, value); }
        }
        public static readonly DependencyProperty IsExpandedProperty =
            DependencyProperty.Register ("IsExpanded", typeof (bool), typeof (ExpendEx), new PropertyMetadata (false, OnIsExpandChanged));

        private static void OnIsExpandChanged (DependencyObject obj, DependencyPropertyChangedEventArgs e) {
            ExpendEx expend = obj as ExpendEx;
            expend?.updateState ((bool) e.NewValue);
        }

        private void updateState (bool isExpand) {
            if (isExpand) Keyboard.Focus (this);
            else Keyboard.ClearFocus ();
            VisualStateManager.GoToState (this, isExpand ? "Expanded" : "Collapsed", true);
        }

        public override void OnApplyTemplate () {
            base.OnApplyTemplate ();
            updateState (IsExpanded);
        }
    }
}