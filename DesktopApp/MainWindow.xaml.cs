using System.Diagnostics; // Add this line at the top of your code
using LiveCharts.Wpf;
using ArquivosDoDisco.UseCase;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using LiveCharts;
using LiveCharts.Defaults;
using ArquivosDoDisco.Entities;
using Newtonsoft.Json;

namespace DesktopApp
{
    public partial class MainWindow : Window
    {
        public MyFolderEntity structure = new();
        public string CurrentDrive = string.Empty;
        string CurrentPath = "";

        public MainWindow()
        {
            InitializeComponent();

            var DriveList = DriverFind.GetDrives();

            // Populate the list of labels with disk drives
            foreach (var drive in DriveList)
            {
                var label = new Label();
                label.Content = drive;
                label.Margin = new Thickness(10);
                label.FontSize = 20;
                label.HorizontalAlignment = HorizontalAlignment.Center;
                label.VerticalAlignment = VerticalAlignment.Center;
                label.MouseLeftButtonDown += Label_MouseLeftButtonDown;

                DriveListPanel.Children.Add(label);
            }
        }

        private async void Label_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var label = sender as Label;
            var content = label.Content as string;
            CurrentDrive = label.Content as string;

            structure = await FileManager.ListFoldersAndFilesAsync(content);
            CurrentPath = "";

            fillGraph(structure);
        }

        private void PieChart_DataClick(object sender, ChartPoint chartPoint)
        {
            var selectedSeries = (PieSeries)chartPoint.SeriesView;

            CurrentPath =  System.IO.Path.Combine(CurrentPath, selectedSeries.Title);

            var foundFolder = DriverFind.FindFolder(structure, CurrentPath);
            foundFolder.SortFoldersBySize();
            foundFolder.SortFilesBySize();

            fillGraph(foundFolder);
        }

        private void fillGraph(MyFolderEntity folderData)
        {
            // Update the label with the folder's full path
            PathLabel.Content = folderData.FullPath;

            // Update pie chart with folder and file data
            var seriesCollection = new SeriesCollection();

            // Add series for folders
            foreach (var folder in folderData.Folders)
            {
                var values = new ChartValues<ObservableValue> { new ObservableValue(folder.TotalSize) };
                seriesCollection.Add(new PieSeries { Title = folder.Name, Values = values, DataLabels = true });
            }

            // Add series for files
            foreach (var file in folderData.Files)
            {
                var values = new ChartValues<ObservableValue> { new ObservableValue(file.Size) };
                seriesCollection.Add(new PieSeries { Title = file.Name, Values = values, DataLabels = true });
            }

            PieChart.Series = seriesCollection;
        }

        private void PathLabel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(System.IO.Path.Combine(CurrentDrive, CurrentPath)))
            {
                Process.Start("explorer.exe", System.IO.Path.Combine(CurrentDrive, CurrentPath));
            }
        }

    }
}
