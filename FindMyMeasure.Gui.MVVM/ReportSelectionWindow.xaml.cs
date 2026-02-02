using FindMyMeasure.Database;
using FindMyMeasure.Enums;
using FindMyMeasure.Gui.MVVM.Exceptions;
using FindMyMeasure.Gui.MVVM.ViewModels;
using FindMyMeasure.PowerBI;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using static FindMyMeasure.Gui.MVVM.ReportAnalysisConfiguration;


namespace FindMyMeasure.Gui.MVVM
{
    /// <summary>
    /// Logique d'interaction pour MainWindow.xaml
    /// </summary>

    public partial class ReportSelectionWindow : Window
    {

        private LoadingProgressWindow loadingProgressWindow;

        public ObservableCollection<ReportAnalysisConfiguration> reportConfigList;
        private ReportSelectionViewModel viewModel;

        public ReportSelectionWindow() : this(new List<ReportAnalysisConfiguration>())
        {
        }

        
        public ReportSelectionWindow(IEnumerable<ReportAnalysisConfiguration> reportConfigs)
        {
            this.Resources.MergedDictionaries.Add(Utils.GetLanguageDictionary());
            InitializeComponent();
            this.viewModel = new ReportSelectionViewModel(reportConfigs);
            this.viewModel.ErrorOccured += OnAnalysisError;
            this.DataContext = viewModel;
            this.viewModel.LoadLatestRun();

            viewModel.PropertyChanged += OnReportAnalysisStarted;

        }

        private void OnAnalysisError(object sender, string errorMessage)
        {
            MessageBox.Show(errorMessage, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void OnReportAnalysisStarted(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ReportSelectionViewModel.IsBusy))
            {
                if (this.viewModel.IsBusy)
                {
                    this.loadingProgressWindow = new LoadingProgressWindow(this.viewModel);
                    this.loadingProgressWindow.Owner = this;
                    this.IsEnabled = false;
                    this.loadingProgressWindow.Show();
                }
                else
                {
                    loadingProgressWindow?.Close();
                    this.IsEnabled = true;
                    if (this.viewModel.ReportAnalysisResult != null)
                    {
                        MainWindow mainWindow = new MainWindow(this.viewModel.ReportAnalysisResult);
                        mainWindow.Show();
                        this.Close();
                    }
                }
            }
        }


        private void bImportReports_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.DefaultExt = ".pbix";
            dialog.Multiselect = true;
            dialog.Filter = "Document(s) pbix|*.pbix";

            bool? result = dialog.ShowDialog();
            if (result is null || result == false)
                return;
            foreach (string pbiFilepath in dialog.FileNames)
            {
                if (File.Exists(pbiFilepath))
                {
                    this.viewModel.AddReportConfigToList(pbiFilepath);
                }
            }
        }
    }
}
