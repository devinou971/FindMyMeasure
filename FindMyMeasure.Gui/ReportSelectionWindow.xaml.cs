using FindMyMeasure.Database;
using FindMyMeasure.Gui.Exceptions;
using FindMyMeasure.PowerBI;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using static FindMyMeasure.Gui.ReportAnalysisConfiguration;


namespace FindMyMeasure.Gui
{
    /// <summary>
    /// Logique d'interaction pour MainWindow.xaml
    /// </summary>

    public partial class ReportSelectionWindow : Window
    {

        private bool preventCascadeAction = false;

        public ObservableCollection<ReportAnalysisConfiguration> reportConfigList;

        public ReportSelectionWindow()
        {
            this.Resources.MergedDictionaries.Add(Utils.GetLanguageDictionary());
            InitializeComponent();
            reportConfigList = new ObservableCollection<ReportAnalysisConfiguration>();
            dgReportList.ItemsSource = reportConfigList;

            this.LoadLatestRun();
        }

        public ReportSelectionWindow(IEnumerable<ReportAnalysisConfiguration> reportConfigs)
        {
            InitializeComponent();
            reportConfigList = new ObservableCollection<ReportAnalysisConfiguration>();
            dgReportList.ItemsSource = reportConfigList;

            foreach (var reportConfig in reportConfigs)
            {
                this.reportConfigList.Add(reportConfig);
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
                    AddReportToSelectionList(pbiFilepath);
            }
        }

        private void bRemoveReport(object sender, RoutedEventArgs e)
        {
            reportConfigList.Remove(reportConfigList.First(r => r.ReportId == (int)((Button)sender).Tag));
        }

        private async void bStartAnalysis(object sender, RoutedEventArgs e)
        {
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
            }
        }

        private void AddReportToSelectionList(string pbiFilepath)
        {
            string connectionsFileContent = null;
            using (ZipArchive pbixFile = ZipFile.OpenRead(pbiFilepath))
            {
                ZipArchiveEntry connectionsFile = pbixFile.GetEntry("Connections");
                if (connectionsFile != null)
                {
                    StreamReader streamReader = new StreamReader(connectionsFile.Open());
                    connectionsFileContent = streamReader.ReadToEnd();
                }
            }
            string connectionString = null;
            string modelName = null;
            ModelConnectionType modelType = ModelConnectionType.Local;
            string reportName = pbiFilepath.Split(System.IO.Path.DirectorySeparatorChar).Last();

            if (connectionsFileContent != null)
            {
                JsonObject connectionsNode = (JsonObject)JsonNode.Parse(connectionsFileContent) ?? throw new Exception("Unable to parse Connections of pbix file into json format");
                if (connectionsNode.ContainsKey("Connections"))
                {
                    JsonArray connectionsArray = connectionsNode["Connections"].AsArray();
                    if (connectionsArray.Count > 0)
                    {
                        connectionString = connectionsArray[0]["ConnectionString"].GetValue<String>();
                        modelName = connectionString.Split(new string[] { "Initial Catalog=" }, StringSplitOptions.None).Last().Split(';').First() + " (remote model)";
                        modelType = ModelConnectionType.Remote;
                    }
                }
                else
                {
                    modelName = reportName.Remove(reportName.Length - 5) + " (Local model)";
                    modelType = ModelConnectionType.Local;
                }
            }
            else
            {
                modelName = reportName.Remove(reportName.Length - 5) + " (Local model)";
                modelType = ModelConnectionType.Local;
            }

            var report = new ReportAnalysisConfiguration(reportName, pbiFilepath, modelName, connectionString, Properties.Settings.Default.AnalyseHiddenVisuals, Properties.Settings.Default.AnalyseHiddenPages, modelType);
            if (!reportConfigList.Contains(report))
                reportConfigList.Add(report);
        }

        private HashSet<DataGridUsageRecord> ProcessUsageRecords(HashSet<SemanticModel> semanticModels)
        {
            HashSet<DataGridUsageRecord> usageRecords = new HashSet<DataGridUsageRecord>();
            foreach (var semanticModel in semanticModels)
            {
                foreach (var measure in semanticModel.GetMeasures())
                {
                    UsageState usageState = measure.GetUsageState();
                    usageRecords.Add(new DataGridUsageRecord(measure, semanticModel.Name));
                }
                foreach (var column in semanticModel.GetColumns())
                {
                    UsageState usageState = column.GetUsageState();
                    usageRecords.Add(new DataGridUsageRecord(column, semanticModel.Name));
                }
            }
            return usageRecords;
        }
        
        private HashSet<SemanticModel> LoadSemanticModels(List<ReportAnalysisConfiguration> dataGridReports, IProgress<string> progressMessage, IProgress<double> progressValue)
        {
            HashSet<SemanticModel> semanticModels = new HashSet<SemanticModel>();

            progressMessage.Report("Retrieving all connection strings ...");

            // First resolve local semantic models
            if (dataGridReports.Where(x => x.ModelType == ModelConnectionType.Local).Count() > 0)
            {
                List<SemanticModel> localSemanticModels = Utils.ListAllLocalSemanticModels(); // TODO : Make this async ?
                foreach (var report in dataGridReports.Where(x => x.ModelType == ModelConnectionType.Local))
                {
                    SemanticModel correspondingSemanticModel = localSemanticModels.FirstOrDefault<SemanticModel>(x => x.Name.Equals(report.ReportName.Remove(report.ReportName.Length - 5)));
                    if (correspondingSemanticModel == null)
                        throw new SemanticModelNotFoundException($"The report \"{report.ReportName}\" seems to use a local model but no local semantic model matching its name could be found. Did you open the report in PowerBI Desktop ?");
                    semanticModels.Add(correspondingSemanticModel);
                }
            }
            progressValue.Report(5);

            // Then resolve remote semantic models
            foreach (var report in dataGridReports.Where(x => x.ModelType == ModelConnectionType.Remote))
            {
                SemanticModel semanticModel = new SemanticModel(report.ModelName, report.ModelConnectionString);
                semanticModels.Add(semanticModel);
            }
            progressValue.Report(10);

            progressMessage.Report("Loading all semantic models ...");
            // Load all semantic models
            foreach (var obj in semanticModels.Select((semanticModel, index) => new { semanticModel, index }))
            {
                try
                {
                    
                    progressMessage.Report($"Loading semantic model : {obj.semanticModel.Name} ...");
                    obj.semanticModel.LoadFullModel();
                    double progressPercent = 10 + (obj.index + 1.0) / semanticModels.Count * 35;
                    progressValue.Report(progressPercent);
                }
                catch (Exception ex)
                {
                    throw new Exception($"An error occured while loading the semantic model \"{obj.semanticModel.Name}\". Error message: {ex.Message}");
                }
            }

            return semanticModels;
        }

        private HashSet<PowerBIReport> LoadReports(List<ReportAnalysisConfiguration> dataGridReports, HashSet<SemanticModel> semanticModels, IProgress<string> progressMessage, IProgress<double> progressValue)
        {
            HashSet<PowerBIReport> powerBIReports = new HashSet<PowerBIReport>();

            // Finally load all PowerBI reports
            progressMessage.Report("Loading all PowerBI reports ...");
            foreach (var obj in dataGridReports.Select((report, index) => new { report, index }))
            {
                try
                {
                    SemanticModel semanticModel = null;
                    if (obj.report.ModelType == ModelConnectionType.Local)
                        semanticModel = semanticModels.FirstOrDefault<SemanticModel>(x => x.Name.Equals(obj.report.ReportName.Remove(obj.report.ReportName.Length - 5)));
                    else
                        semanticModel = semanticModels.FirstOrDefault<SemanticModel>(x => x.ConnectionString.Equals(obj.report.ModelConnectionString));
                    progressMessage.Report($"Loading PowerBI report : {obj.report.ReportName} ...");
                    PowerBIReport powerBIReport = PowerBIReport.LoadFromPbix(obj.report.ReportPath, semanticModel, obj.report.AnalyseHiddenPages, obj.report.AnalyseHiddenVisuals); // TODO : Make this async ?
                    powerBIReports.Add(powerBIReport);
                    double progressPercent = 45 + (obj.index + 1.0) / dataGridReports.Count * 55;
                    progressValue.Report(progressPercent);
                }
                catch (Exception ex)
                {
                    throw new Exception($"An error occured while loading the PowerBI report \"{obj.report.ReportName}\". Error message: {ex.Message}");
                }
            }

            return powerBIReports;
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

        private void LoadLatestRun()
        {
            try
            {
                if (!string.IsNullOrEmpty(Properties.Settings.Default.LastRunSavePath) && File.Exists(Properties.Settings.Default.LastRunSavePath))
                {
                    string lastRunPath = Properties.Settings.Default.LastRunSavePath;
                    using (StreamReader reader = new StreamReader(lastRunPath))
                    {
                        string lastRunValue = reader.ReadToEnd();
                        var options = new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        };
                        var reportConfigs = JsonSerializer.Deserialize<List<ReportAnalysisConfiguration>>(lastRunValue, options);
                        foreach (var reportConfig in reportConfigs)
                        {
                            if (!reportConfigList.Contains(reportConfig))
                            {
                                reportConfigList.Add(reportConfig);
                            }
                        }
                    }
                }
            }
            catch (System.IO.IOException e)
            {
                MessageBox.Show($"Could not open the file {Properties.Settings.Default.LastRunSavePath}. Error details : {e.Message}");
            }
            catch (JsonException e)
            {
                MessageBox.Show($"Could not deserialyse file {Properties.Settings.Default.LastRunSavePath}. Here are error details : {e.Message}");
            }
        }

        private void cbAnalyseHiddenPages_Click(object sender, RoutedEventArgs e)
        {
            if (this.preventCascadeAction || cbAnalyseHiddenPages.IsChecked is null)
                return;

            this.preventCascadeAction = true;
            foreach (var reportConfig in reportConfigList)
            {
                reportConfig.AnalyseHiddenPages = cbAnalyseHiddenPages.IsChecked ?? false;
            }
            this.preventCascadeAction = false;
        }

        private void cbAnalyseHiddenVisuals_Click(object sender, RoutedEventArgs e)
        {
            if (this.preventCascadeAction || cbAnalyseHiddenVisuals.IsChecked is null)
                return;
            this.preventCascadeAction = true;
            foreach (var reportConfig in reportConfigList)
            {
                reportConfig.AnalyseHiddenVisuals = cbAnalyseHiddenVisuals.IsChecked ?? false;
            }
            this.preventCascadeAction = false;
        }

        private void cbAnalyseHiddenPagesForRow_Click(object sender, RoutedEventArgs e)
        {
            var reportConfig = reportConfigList.First(r => r.ReportId == (int)((CheckBox)sender).Tag);
            reportConfig.AnalyseHiddenPages = ((CheckBox)sender).IsChecked ?? false;

            if (this.preventCascadeAction)
                return;

            this.preventCascadeAction = true;

            if (reportConfigList.All(r => r.AnalyseHiddenPages == true))
                cbAnalyseHiddenPages.IsChecked = true;
            else if(reportConfigList.All(r => r.AnalyseHiddenPages == false))
                cbAnalyseHiddenPages.IsChecked = false;
            else
                cbAnalyseHiddenPages.IsChecked = null;

            this.preventCascadeAction = false;
        }

        private void cbAnalyseHiddenVisualsForRow_Click(object sender, RoutedEventArgs e)
        {
            var reportConfig = reportConfigList.First(r => r.ReportId == (int)((CheckBox)sender).Tag);
            reportConfig.AnalyseHiddenVisuals = ((CheckBox)sender).IsChecked ?? false;

            if (this.preventCascadeAction)
                return;

            this.preventCascadeAction = true;
            if (reportConfigList.All(r => r.AnalyseHiddenVisuals == true))
                cbAnalyseHiddenVisuals.IsChecked = true;
            else if (reportConfigList.All(r => r.AnalyseHiddenVisuals == false))
                cbAnalyseHiddenVisuals.IsChecked = false;
            else
                cbAnalyseHiddenVisuals.IsChecked = null;

            this.preventCascadeAction = false;
        }

        private void cbShowAdvancedOptions_Click(object sender, RoutedEventArgs e)
        {
            colAnalyseHiddenPages.Visibility = cbShowAdvancedOptions.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
            colAnalyseHiddenVisuals.Visibility = cbShowAdvancedOptions.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}
