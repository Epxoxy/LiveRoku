using System;
using System.Collections.Generic;
using System.IO;
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

namespace LiveRoku {
    /// <summary>
    /// Interaction logic for LocationSettings.xaml
    /// </summary>
    public partial class LocationSettings : Window {
        private string mFolder;
        private string mFileName;
        public string Folder {
            get { return folderBox == null ? mFolder : folderBox.Text; }
            set {
                mFolder = value;
                if (folderBox != null)
                    folderBox.Text = mFolder;
            }
        }
        public string FileName {
            get { return fileNameBox == null ? mFileName : fileNameBox.Text; }
            set {
                mFileName = value;
                if (fileNameBox != null)
                    fileNameBox.Text = mFileName;
            }
        }
        private TextBox folderBox => folderTBox;
        private TextBox fileNameBox => fileNameTBox;
        public LocationSettings () {
            InitializeComponent ();
            this.Loaded += onLoaded;
        }

        private void onLoaded (object sender, RoutedEventArgs e) {
            this.Loaded -= onLoaded;
            folderBox.Text = mFolder;
            fileNameBox.Text = mFileName;
        }

        private void changeBtnClick (object sender, RoutedEventArgs e) {
            var dialog = new Microsoft.Win32.SaveFileDialog () {
                InitialDirectory = Folder,
                FileName = "sample.flv"
            };
            if (dialog.ShowDialog (this) == true) {
                Folder = Path.GetDirectoryName (dialog.FileName);
            }
        }

        private void onDragMoveMouseDown (object sender, MouseButtonEventArgs e) {
            if (e.LeftButton == MouseButtonState.Pressed)
                this.DragMove ();
        }

        private void okBtnClick (object sender, RoutedEventArgs e) {
            if (string.IsNullOrEmpty (Folder)) {
                tips ("You must special a folder", "BiliRoku");
                return;
            }
            if (!Directory.Exists (Folder) && !tipsOk ("This folder is not exist, create one?", "Sure?"))
                return;
            if (string.IsNullOrEmpty (FileName)) {
                tips ("File name can't be empty", "BiliRoku");
                return;
            }
            if (!Path.HasExtension (FileName)) {
                if (tipsOk ("File name not contain the video extension of '.flv', add it?", "Sure?")) {
                    FileName = FileName + ".flv";
                } else return;
            }
            DialogResult = true;
            Close ();
        }

        private void tips (string text, string title) {
            MessageBox.Show (this, text, title);
        }

        private bool tipsOk (string text, string title) {
            return MessageBoxResult.OK == MessageBox.Show (this, text, title, MessageBoxButton.OKCancel);
        }

        private void cancelBtnClick (object sender, RoutedEventArgs e) {
            Close ();
        }

    }
}