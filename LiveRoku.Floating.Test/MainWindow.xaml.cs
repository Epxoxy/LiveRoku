using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using LiveRoku.Plugin;


namespace Plugin.Test {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        private FloatingPlugin plugin;
        public MainWindow () {
            InitializeComponent ();
            this.Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded (object sender, RoutedEventArgs e) {
            plugin = new FloatingPlugin ();
            plugin.onInitialize (null);
            plugin.onAttach (null);
        }

        int count;
        private void Button_Click (object sender, RoutedEventArgs e) {
            int num = new Random ().Next (1, 200);
            for (int i = 0; i < num; i++)
                plugin.onDanmaku (new LiveRoku.Base.DanmakuModel () { MsgType = LiveRoku.Base.MsgTypeEnum.Comment, UserName = "Test only " + count++, CommentText = "Hello world!" });
        }
    }
}