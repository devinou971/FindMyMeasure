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

        private bool preventCascadeAction = false;
        private LoadingProgressWindow loadingProgressWindow;

        public ObservableCollection<ReportAnalysisConfiguration> reportConfigList;
        private ReportSelectionViewModel viewModel;

        public ReportSelectionWindow()
        {
            this.Resources.MergedDictionaries.Add(Utils.GetLanguageDictionary());
            InitializeComponent();
            this.viewModel = new ReportSelectionViewModel();
            this.DataContext = viewModel;
            this.viewModel.LoadLatestRun();

            viewModel.PropertyChanged += OnReportAnalysisStarted;
        }

        private void OnReportAnalysisStarted(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if(e.PropertyName == nameof(ReportSelectionViewModel.IsBusy))
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
                    MainWindow mainWindow = new MainWindow(this.viewModel.ReportAnalysisResult);
                    mainWindow.Show();
                    this.Close();
                }
            }
        }

        public ReportSelectionWindow(IEnumerable<ReportAnalysisConfiguration> reportConfigs)
        {
            this.Resources.MergedDictionaries.Add(Utils.GetLanguageDictionary());
            InitializeComponent();
            this.viewModel = new ReportSelectionViewModel(reportConfigs);
            this.DataContext = viewModel;
            this.viewModel.LoadLatestRun();

            viewModel.PropertyChanged += OnReportAnalysisStarted;

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
                    //AddReportToSelectionList(pbiFilepath);
                }
            }
        }

        private async void bStartAnalysis(object sender, RoutedEventArgs e)
        {
            /*
            var reportsSnapshot = reportConfigList.ToList(); // To avoid collection modified exception
            LoadingProgressWindow loadingProgressWindow = new LoadingProgressWindow();
            loadingProgressWindow.Owner = this;
            this.IsEnabled = false;
            loadingProgressWindow.Show();

            IProgress<string> progressMessage = new Progress<string>(s => { loadingProgressWindow.AddLog(s); });
            IProgress<double> progressValue = new Progress<double>(val => { loadingProgressWindow.SetProgress(val); });

            progressMessage.Report("Starting the loading of semantic models and reports ...");
            try
            {
                var result = await Task.Run(() =>
                {
                    HashSet<SemanticModel> semanticModels = LoadSemanticModels(reportsSnapshot, progressMessage, progressValue);
                    progressValue.Report(45);
                    HashSet<PowerBIReport> powerBIReports = LoadReports(reportsSnapshot, semanticModels, progressMessage, progressValue);
                    progressValue.Report(90);
                    HashSet<DataGridUsageRecord> usageRecords = ProcessUsageRecords(semanticModels);
                    return new
                    {
                        SemanticModels = semanticModels,
                        PowerBIReports = powerBIReports,
                        UsageRecords = usageRecords
                    };
                });

                progressValue.Report(100);
                loadingProgressWindow.MarkAsCompleted();

                this.SaveReportsList("lastRun.json");

                MainWindow mainWindow = new MainWindow(result.SemanticModels, this.reportConfigList, result.UsageRecords);
                mainWindow.Show();
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An unexpected error occured during the loading of reports and semantic models. Error message: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                this.IsEnabled = true;
                loadingProgressWindow.Close();
                return;
            }*/
        }

        private void SaveReportsList(string savePath)
        {
            IEnumerable<string> reportPaths = this.reportConfigList.Select(x => x.ReportPath);

            var options = new JsonSerializerOptions { WriteIndented = true };
            string jsonString = JsonSerializer.Serialize(this.reportConfigList.ToList(), options);

            try
            {
                Properties.Settings.Default.LastRunSavePath = savePath;

                if (string.IsNullOrEmpty(savePath))
                {
                    savePath = Properties.Settings.Default.LastRunSavePath;
                } else
                {
                    Properties.Settings.Default.LastRunSavePath = savePath;
                }

                using (StreamWriter writer = new StreamWriter(savePath, false))
                    writer.Write(jsonString);

            } catch (System.IO.IOException e)
            {
                MessageBox.Show($"Could not write the file {Properties.Settings.Default.LastRunSavePath}. Error details : {e.Message}");
            } catch (JsonException e)
            {
                MessageBox.Show($"Could not serialize file {Properties.Settings.Default.LastRunSavePath}. Here are error details : {e.Message}");
            }
        }

    }
}
