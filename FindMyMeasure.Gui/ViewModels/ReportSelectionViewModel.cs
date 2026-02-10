using FindMyMeasure.Gui.Commands;
using FindMyMeasure.Gui.Models;
using FindMyMeasure.Gui.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using static FindMyMeasure.Gui.Models.ReportAnalysisConfiguration;

namespace FindMyMeasure.Gui.ViewModels
{
    public class ReportSelectionViewModel : ViewModelBase
    {

        private bool? _analyseHiddenPagesAllChecked;
        private bool? _analyseHiddenVisualsAllChecked;
        private ObservableCollection<ReportAnalysisConfiguration> _reportConfigList = new ObservableCollection<ReportAnalysisConfiguration>();
        private bool _isBusy = false;
        private bool blockReportConfigChangedEvent = false;
        private double _analysisProgressValue = 0.0;
        private ObservableCollection<String> _analysisProgressMessages = new ObservableCollection<String>();
        private ReportAnalysisResult _analysisResult;

        public event EventHandler<string> ErrorOccured;

        public ICommand RemoveReportCommand { get ; }
        public ICommand StartAnalysisCommand { get; }

        /// <summary>
        /// Initializes a new instance of the view model and populates the internal
        /// report configuration list with the provided items. Also wires up collection
        /// and property changed handlers and recalculates the aggregate tri-state
        /// checkbox values.
        /// </summary>
        public ReportSelectionViewModel() : this(new List<ReportAnalysisConfiguration>())
        {
        }

        /// <summary>
        /// Initializes a new instance of the view model and populates the internal
        /// report configuration list with the provided items. Also wires up collection
        /// and property changed handlers and recalculates the aggregate tri-state
        /// checkbox values.
        /// </summary>
        public ReportSelectionViewModel(IEnumerable<ReportAnalysisConfiguration> reportConfigs)
        {
            this.AnalyseHiddenPagesAllChecked = true;
            RemoveReportCommand = new RelayCommand(action => 
            { 
                if(action is int reportId)
                    RemoveReport(reportId); 
            });

            StartAnalysisCommand = new RelayCommand(action =>
            {
                StartAnalysisAsync();
            }, (object _) => { return !this.IsBusy; } );

            foreach (var reportConfig in reportConfigs)
                this._reportConfigList.Add(reportConfig);

            this._reportConfigList.CollectionChanged += OnReportListChanged;
            foreach (var report in this._reportConfigList)
                report.PropertyChanged += OnReportConfigChanged;

            RecalculateAnalyseHiddenPagesAllChecked();
            RecalculateAnalyseHiddenVisualsAllChecked();
        }


        public bool? AnalyseHiddenPagesAllChecked
        {
            get => this._analyseHiddenPagesAllChecked;
            set
            {
                if (this._analyseHiddenPagesAllChecked == value)
                    return;
                this._analyseHiddenPagesAllChecked = value;
                OnPropertyChanged();
                if (this._analyseHiddenPagesAllChecked != null)
                {
                    blockReportConfigChangedEvent = true;
                    SetAllReportsToAnalyseHiddenPages(this._analyseHiddenPagesAllChecked ?? true);
                    blockReportConfigChangedEvent = false;
                }
            }
        }

        public bool? AnalyseHiddenVisualsAllChecked
        {
            get => this._analyseHiddenVisualsAllChecked;
            set
            {
                if(this._analyseHiddenVisualsAllChecked == value)
                    return;
                this._analyseHiddenVisualsAllChecked = value;
                OnPropertyChanged();
                if (this._analyseHiddenVisualsAllChecked != null)
                {
                    this.blockReportConfigChangedEvent = true;
                    this. SetAllReportsToAnalyseHiddenVisuals(this._analyseHiddenVisualsAllChecked ?? true);
                    this.blockReportConfigChangedEvent = false;
                }
            }
        }

        public bool IsBusy { get => this._isBusy; set { 
                this._isBusy = value;
                OnPropertyChanged();
            }
        }

        public ReportAnalysisResult ReportAnalysisResult { get => this._analysisResult; 
            set
            {
                this._analysisResult = value;
                OnPropertyChanged();
            } 
        }


        public ObservableCollection<ReportAnalysisConfiguration> ReportConfigList
        {
            get => this._reportConfigList;
        }

        public double AnalysisProgressValue
        {
            get => this._analysisProgressValue;
            set { 
                this._analysisProgressValue = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<string> AnalysisProgressMessages
        {
            get => this._analysisProgressMessages;
        }

        /// <summary>
        /// Sets the `AnalyseHiddenPages` flag on every report in the list.
        /// </summary>
        /// <param name="analyseHiddenPages">Value to assign to each report's flag.</param>
        public void SetAllReportsToAnalyseHiddenPages(bool analyseHiddenPages)
        {
            foreach (var report in this._reportConfigList)
                report.AnalyseHiddenPages = analyseHiddenPages;
        }

        /// <summary>
        /// Sets the `AnalyseHiddenVisuals` flag on every report in the list.
        /// </summary>
        /// <param name="analyseHiddenVisuals">Value to assign to each report's flag.</param>
        public void SetAllReportsToAnalyseHiddenVisuals(bool analyseHiddenVisuals)
        {
            foreach (var report in this._reportConfigList)
                report.AnalyseHiddenVisuals = analyseHiddenVisuals;
        }

        /// <summary>
        /// Extracts metadata from the specified PBIX file and creates a
        /// corresponding `ReportAnalysisConfiguration`. The configuration is
        /// added to the list if it does not already exist.
        /// </summary>
        /// <param name="pbiFilepath">Path to a PBIX file.</param>
        public void AddReportConfigToList(string pbiFilepath)
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
            if (!_reportConfigList.Contains(report))
                _reportConfigList.Add(report);
        }

        private void RemoveReport(int reportId)
        {
            _reportConfigList.Remove(_reportConfigList.First(r => r.ReportId == reportId));
        }

        /// <summary>
        /// Starts the analysis process on a background thread and updates progress
        /// and result properties. Exceptions are reported via `ErrorOccured` event handler.
        /// </summary>
        private async Task StartAnalysisAsync()
        {
            this.IsBusy = true;
            try
            {
                var analysisService = new ReportAnalysisService(this.ReportConfigList.ToList()); // We send a copy in case of collection modified exception
                Progress<double> progressValue = new Progress<double>(p => this.AnalysisProgressValue = p);
                Progress<string> progressMessage = new Progress<string>(m => this.AnalysisProgressMessages.Add(m));
                this._analysisResult = await Task.Run(() => { return analysisService.RunAsync(progressValue, progressMessage); });
                SaveReportsList();
            } catch (Exception e)
            {
                this.ErrorOccured?.Invoke(this, e.Message);
            }
            finally
            {
                this.IsBusy = false;
            }
        }

        /// <summary>
        /// Handles changes to the report collection by wiring/unwiring property
        /// change handlers for added/removed items and recalculating the aggregate
        /// tri-state checkbox values.
        /// </summary>
        private void OnReportListChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            
            if(e.NewItems != null)
            {
                foreach (ReportAnalysisConfiguration item in e.NewItems)
                    item.PropertyChanged += OnReportConfigChanged;
            }
            if(e.OldItems != null)
            {
                foreach (ReportAnalysisConfiguration item in e.OldItems)
                    item.PropertyChanged -= OnReportConfigChanged;
            }
            RecalculateAnalyseHiddenPagesAllChecked();
            RecalculateAnalyseHiddenVisualsAllChecked();
        }

        /// <summary>
        /// Responds to individual report configuration property changes and
        /// recalculates aggregate checkbox values unless updates are currently
        /// blocked.
        /// </summary>
        private void OnReportConfigChanged(object sender, PropertyChangedEventArgs e)
        {
            if (blockReportConfigChangedEvent)
                return;
            if (e.PropertyName == nameof(ReportAnalysisConfiguration.AnalyseHiddenPages))
                RecalculateAnalyseHiddenPagesAllChecked();
            else if (e.PropertyName == nameof(ReportAnalysisConfiguration.AnalyseHiddenVisuals))
                RecalculateAnalyseHiddenVisualsAllChecked();
        }

        
        /// <summary>
        /// Recalculates the tri-state value of `AnalyseHiddenPagesAllChecked` based on the
        /// current `ReportConfigList` values. Sets the backing field to:
        /// - true:  if all reports have `AnalyseHiddenPages` enabled
        /// - false: if all reports have `AnalyseHiddenPages` disabled
        /// - null:  if there is a mix of enabled and disabled values
        /// </summary>
        private void RecalculateAnalyseHiddenPagesAllChecked()
        {
            if (this._reportConfigList.All(x => x.AnalyseHiddenPages))
                this._analyseHiddenPagesAllChecked = true;
            else if (this._reportConfigList.All(x => !x.AnalyseHiddenPages))
                this._analyseHiddenPagesAllChecked = false;
            else 
                this._analyseHiddenPagesAllChecked = null;
            OnPropertyChanged(nameof(this.AnalyseHiddenPagesAllChecked));
        }

        /// <summary>
        /// Recalculates the tri-state value of `AnalyseHiddenVisualsAllChecked` based on the
        /// current `ReportConfigList` values. Sets the backing field to:
        /// - true:  if all reports have `AnalyseHiddenVisuals` enabled
        /// - false: if all reports have `AnalyseHiddenVisuals` disabled
        /// - null:  if there is a mix of enabled and disabled values
        /// </summary>
        private void RecalculateAnalyseHiddenVisualsAllChecked()
        {
            if (this._reportConfigList.All(x => x.AnalyseHiddenVisuals))
                this._analyseHiddenVisualsAllChecked = true;
            else if (this._reportConfigList.All(x => !x.AnalyseHiddenVisuals))
                this._analyseHiddenVisualsAllChecked = false;
            else
                this._analyseHiddenVisualsAllChecked = null;
            OnPropertyChanged(nameof(this.AnalyseHiddenVisualsAllChecked));
        }

        /// <summary>
        /// Reads the content of last config file, and adds all the reports to the _reportConfigList. 
        /// The path of the file is set in : Properties.Settings.Default.LastRunSavePath
        /// </summary>
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
                            if (!_reportConfigList.Contains(reportConfig))
                            {
                                _reportConfigList.Add(reportConfig);
                            }
                        }
                    }
                }
            }
            catch (System.IO.IOException e)
            {
                this.ErrorOccured?.Invoke(this, $"Could not open the file {Properties.Settings.Default.LastRunSavePath}. Error details : {e.Message}");
            }
            catch (JsonException e)
            {
                this.ErrorOccured?.Invoke(this, $"Could not deserialyse file {Properties.Settings.Default.LastRunSavePath}. Here are error details : {e.Message}");
            }
        }

        /// <summary>
        /// Save the ReportConfigList into a json file. 
        /// The path to that JSON is set in Properties.Settings.Default.LastRunSavePath.
        /// </summary>
        private void SaveReportsList()
        {
            IEnumerable<string> reportPaths = this._reportConfigList.Select(x => x.ReportPath);

            var options = new JsonSerializerOptions { WriteIndented = true };
            string jsonString = JsonSerializer.Serialize(this._reportConfigList.ToList(), options);

            try
            {
                string savePath = Properties.Settings.Default.LastRunSavePath;

                using (StreamWriter writer = new StreamWriter(savePath, false))
                    writer.Write(jsonString);
            }
            catch (System.IO.IOException e)
            {
                this.ErrorOccured?.Invoke(this, $"Could not write the file {Properties.Settings.Default.LastRunSavePath}. Error details : {e.Message}");
            }
            catch (JsonException e)
            {
                this.ErrorOccured?.Invoke(this, $"Could not serialize file {Properties.Settings.Default.LastRunSavePath}. Here are error details : {e.Message}");
            }
        }

    }
}
