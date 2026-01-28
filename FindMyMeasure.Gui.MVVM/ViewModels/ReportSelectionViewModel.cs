using FindMyMeasure.Gui.MVVM.Commands;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Windows;
using System.Windows.Input;
using static FindMyMeasure.Gui.MVVM.ReportAnalysisConfiguration;

namespace FindMyMeasure.Gui.MVVM.ViewModels
{
    public class ReportSelectionViewModel : ViewModelBase
    {

        private bool? analyseHiddenPages;
        private bool? analyseHiddenVisuals;
        private ObservableCollection<ReportAnalysisConfiguration> reportConfigList = new ObservableCollection<ReportAnalysisConfiguration>();

        public ICommand UpdateReportListCommand { get; }
        public ICommand RemoveReport { get ; }

        public ICommand MyCommand { get; } = new RelayCommand(_ => { Console.WriteLine("Button clicked"); });


        public ReportSelectionViewModel() : this(new List<ReportAnalysisConfiguration>())
        {
        }

        public ReportSelectionViewModel(IEnumerable<ReportAnalysisConfiguration> reportConfigs)
        {
            UpdateReportListCommand = new RelayCommand(action => updateReportsAdvancedSettings());
            RemoveReport = new RelayCommand(action => 
            { 
                if(action is int reportId)
                    removeReport(reportId); 
            });
             
            foreach (var reportConfig in reportConfigs)
            {
                this.reportConfigList.Add(reportConfig);
            }
        }


        public bool? AnalyseHiddenPages
        {
            get => this.analyseHiddenPages;
            set
            {
                if (this.analyseHiddenPages == value)
                    return;
                this.analyseHiddenPages = value;
                OnPropertyChanged();
                updateReportsAdvancedSettings();
            }
        }

        public bool? AnalyseHiddenVisuals
        {
            get => this.analyseHiddenVisuals;
            set
            {
                if(this.analyseHiddenVisuals == value)
                    return;
                this.analyseHiddenVisuals = value;
                OnPropertyChanged();
                updateReportsAdvancedSettings();
            }
        }

        public ObservableCollection<ReportAnalysisConfiguration> ReportConfigList
        {
            get => this.reportConfigList;
        }

        public void updateReportsAdvancedSettings()
        {
            Console.WriteLine("Testing");
            foreach(var report in this.reportConfigList)
            {
                if (!analyseHiddenPages != null)
                    report.AnalyseHiddenPages = this.AnalyseHiddenPages ?? false;

                if (!analyseHiddenVisuals != null)
                    report.AnalyseHiddenVisuals= this.AnalyseHiddenVisuals ?? false;
            }
            OnPropertyChanged(nameof(this.ReportConfigList));
        }

        public void AddReportToSelectionList(ReportAnalysisConfiguration reportAnalysisConfiguration)
        {
            this.reportConfigList.Add(reportAnalysisConfiguration);
        }

        public void AddReportToSelectionList(string pbiFilepath)
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
            OnPropertyChanged(nameof(this.ReportConfigList));
        }

        private void removeReport(int reportId)
        {
            reportConfigList.Remove(reportConfigList.First(r => r.ReportId == reportId));
        }

        public void LoadLatestRun()
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


    }
}
