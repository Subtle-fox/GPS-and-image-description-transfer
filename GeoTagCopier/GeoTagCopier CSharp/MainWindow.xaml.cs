using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Forms;
using MessageBox = System.Windows.Forms.MessageBox;

namespace GeoTagCopier_CSharp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string _sourcePath;
        private string _destPath;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Button_Click_SourceFolder(object sender, RoutedEventArgs e)
        {
            var dlg = new FolderBrowserDialog { SelectedPath = _sourcePath };
            var res = dlg.ShowDialog();
            if (res == System.Windows.Forms.DialogResult.OK)
            {
                _sourcePath = dlg.SelectedPath;
                SourcePath.Text = _sourcePath;
            }
        }

        private void Button_Click_DestFiles(object sender, RoutedEventArgs e)
        {
            var dlg = new FolderBrowserDialog {SelectedPath = _sourcePath};
            var res = dlg.ShowDialog();
            if (res == System.Windows.Forms.DialogResult.OK)
            {
                _destPath = dlg.SelectedPath;
                DestPath.Text = _destPath;
            }
        }

        private void Button_Click_Convert(object sender, RoutedEventArgs e)
        {
            //if (Directory.GetFiles(_destPath).Any())
            //{
            //    MessageBox.Show("Select empty folder or delete all files in destination",
            //        "Destination folder is not empty",
            //        MessageBoxButtons.OK, MessageBoxIcon.Warning);
            //    return;
            //}

            Progress.Visibility = Visibility.Visible;
            Total.Visibility = Errors.Visibility = Visibility.Collapsed;

            var worker = new BackgroundWorker {WorkerReportsProgress = true};
            var copier = new DescriptionAndGeoTagCopier(_sourcePath, _destPath);
            worker.DoWork += copier.Run;
            worker.ProgressChanged += WorkerOnProgressChanged;
            worker.RunWorkerAsync();
            worker.RunWorkerCompleted += WorkerOnRunWorkerCompleted;
        }

        private void WorkerOnRunWorkerCompleted(object sender, RunWorkerCompletedEventArgs runWorkerCompletedEventArgs)
        {
            if (runWorkerCompletedEventArgs.Error == null)
                Progress.Content = string.Format("Done :-)");
            else
                Progress.Content = string.Format("Error :-(");

            Total.Visibility = Errors.Visibility = Visibility.Visible;
            var res = runWorkerCompletedEventArgs.Result as Tuple<List<String>, List<String>>;
            Total.Content = "Total jpeg files: " + res.Item1.Count;
            Errors.Content = "Including errors: " + res.Item2.Count;
        }

        private void WorkerOnProgressChanged(object sender, ProgressChangedEventArgs progressChangedEventArgs)
        {
            Progress.Content = string.Format("{0}%", progressChangedEventArgs.ProgressPercentage);
        }
    }
}
