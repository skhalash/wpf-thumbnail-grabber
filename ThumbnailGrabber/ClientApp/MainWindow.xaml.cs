using System.IO;
using System.Windows;
using Microsoft.Win32;
using ThumbnailGrabber;

namespace ClientApp
{
    public partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void OnSelectFileClicked(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog {Multiselect = false};
            if (openFileDialog.ShowDialog() != true)
            {
                return;
            }

            var filePath = openFileDialog.FileName;
            var fileBytes = File.ReadAllBytes(filePath);
            var bitmap = VideoThumbnailGrabber.Grab(fileBytes);
            ThumbnailImage.Source = bitmap;
        }
    }
}
