using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Forms;

namespace GeoTagCopier
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
            CheckConditions();
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
            CheckConditions();
        }

        private void CheckConditions()
        {
            RunBtn.IsEnabled = !string.IsNullOrEmpty(_sourcePath) && !string.IsNullOrEmpty(_destPath);
        }

        private void Button_Click_Convert(object sender, RoutedEventArgs e)
        {
            Progress.Visibility = Visibility.Visible;
            Total.Visibility = Errors.Visibility = Skiped.Visibility = Visibility.Collapsed;

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

            Total.Visibility = Errors.Visibility = Skiped.Visibility = Visibility.Visible;
            var res = runWorkerCompletedEventArgs.Result as Tuple<List<String>, List<String>, List<String>>;
            Total.Content = "Total jpeg files: " + res.Item1.Count;
            Skiped.Content = "Skiped files: " + res.Item2.Count;
            Errors.Content = "Including errors: " + res.Item3.Count;
        }

        private void WorkerOnProgressChanged(object sender, ProgressChangedEventArgs progressChangedEventArgs)
        {
            Progress.Content = string.Format("{0}%", progressChangedEventArgs.ProgressPercentage);
        }
    }
}
