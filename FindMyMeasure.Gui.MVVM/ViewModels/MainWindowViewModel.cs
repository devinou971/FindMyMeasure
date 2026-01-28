using FindMyMeasure.Database;
using FindMyMeasure.Interfaces;
using FindMyMeasure.PowerBI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;

namespace FindMyMeasure.Gui.MVVM.ViewModels
{
    internal class MainWindowViewModel : ViewModelBase
    {
        private CancellationTokenSource _filterCts;
        private IEnumerable<DataGridUsageRecord> _filteredRecords;

        private HashSet<SemanticModel> _semanticModels;
        private IEnumerable<DataGridUsageRecord> _usageRecords;

        public CollectionViewSource UsageRecordsView { get; }

        public ObservableCollection<SemanticModel> SemanticModels { get; }

        private SemanticModel _semanticModelFilter;
        public SemanticModel SemanticModelFilter { 
            get => this._semanticModelFilter;
            set
            {
                _semanticModelFilter = value;
                CalculateStats();
                FilterRows(0);
                OnPropertyChanged();
            } 
        }

        public ObservableCollection<string> Stats { get; }

        private string _usageStateFilter = "All";
        public string UsageStateFilter { get => _usageStateFilter; 
            set {
                this._usageStateFilter = value;
                FilterRows(0);
                OnPropertyChanged();
            }
        }

        private string _artifactTypeFilter = "All";
        public string ArtifactTypeFilter { get => this._artifactTypeFilter; 
            set { 
                this._artifactTypeFilter = value;
                FilterRows(0);
                OnPropertyChanged();
            } 
        }

        private string _artifactNameFilter = "";
        public string ArtifactNameFilter { 
            get => this._artifactNameFilter;
            set {
                this._artifactNameFilter = value;
                FilterRows(300);
                OnPropertyChanged();
            }
        }

        private string _tableNameFilter = "";
        public string TableNameFilter
        {
            get => this._tableNameFilter;
            set {
                this._tableNameFilter = value;
                FilterRows(300);
                OnPropertyChanged();
            }
        }

        private DataGridUsageRecord _selectedArtifact;
        public DataGridUsageRecord SelectedArtifact { get => this._selectedArtifact; set { 
                this._selectedArtifact = value;
                UpdateDependents();
                OnPropertyChanged();
                OnPropertyChanged(nameof(SelectedArtifactLabel));
            } }

        public string SelectedArtifactLabel { get
            {
                if (_selectedArtifact == null)
                {
                    var defaultText = Utils.GetLanguageDictionary()["MainWindow.Label.SelectionDetail.Content"];
                    return defaultText == null ? "Selection details :" : defaultText.ToString();
                }
                return $"{this.SelectedArtifact.Type} : {this.SelectedArtifact.Name} - {this.SelectedArtifact.UsageState}({this.SelectedArtifact.NbOfUsage})";
            } 
        }

        public ObservableCollection<object> SemanticModelDependents { get; private set; }
        public ObservableCollection<object> ReportDependents { get; private set; }

        public MainWindowViewModel(HashSet<SemanticModel> semanticModels, IEnumerable<DataGridUsageRecord> usageRecords)
        {
            this._semanticModels = semanticModels;
            this._usageRecords = usageRecords;
            this._filteredRecords = usageRecords;

            this.SemanticModelDependents = new ObservableCollection<object>();
            this.ReportDependents = new ObservableCollection<object>();

            this.SemanticModels = new ObservableCollection<SemanticModel>(this._semanticModels);
            this.Stats = new ObservableCollection<string>();
            this.SemanticModelFilter = this._semanticModels.FirstOrDefault();

            this.UsageRecordsView = new CollectionViewSource { Source = usageRecords };
            this.UsageRecordsView.Filter += FilterUsageRecords;
        }

        private void CalculateStats()
        {
            this.Stats.Clear();
            this.Stats.Add($"Number of Tables: {this.SemanticModelFilter.GetTables().Count}");
            this.Stats.Add($"Number of Measures: {this.SemanticModelFilter.GetMeasures().Count}");
            this.Stats.Add($"Number of Columns: {this.SemanticModelFilter.GetColumns().Count}");
            this.Stats.Add($"Number of Relationships: {this.SemanticModelFilter.GetRelationships().Count}");
        }

        private void UpdateDependents()
        {
            // TODO : Split this method into smaller methods
            this.SemanticModelDependents.Clear();
            this.ReportDependents.Clear();
            var dependents = this.SelectedArtifact.DataInput.GetDependents();

            foreach (var dependent in dependents)
            {
                if (dependent is IDataInput || dependent is Table || dependent is Relationship)
                {
                    SemanticModelDependents.Add(new
                    {
                        Type = dependent.Type,
                        Name = (dependent is IDataInput) ? $"{dependent.Name} ({((IDataInput)dependent).GetUsageState()})" : dependent.Name,
                        TableName = (dependent is IDataInput) ? ((IDataInput)dependent).ParentTable.Name :
                                (dependent is Relationship) ? ((Relationship)dependent).FromColumn.ParentTable.Name + " -> " + ((Relationship)dependent).ToColumn.ParentTable.Name :
                                ""
                    });
                }
                else if (dependent is IPowerBILeafNode)
                {
                    string reportName = "";
                    string pageName = "";
                    if (dependent is Visual)
                    {
                        pageName = ((Visual)dependent).GetReportPage().Name;
                        reportName = ((Visual)dependent).GetReportPage().GetPowerBIReport().Name;
                    }
                    else if (dependent is Filter)
                    {
                        var filterParent = ((Filter)dependent).GetParent();
                        if (filterParent is Visual)
                        {
                            pageName = ((Visual)filterParent).GetReportPage().Name;
                            reportName = ((Visual)filterParent).GetReportPage().GetPowerBIReport().Name;
                        }
                        else if (filterParent is ReportPage)
                        {
                            pageName = ((ReportPage)filterParent).Name;
                            reportName = ((ReportPage)filterParent).GetPowerBIReport().Name;
                        }
                        else if (filterParent is PowerBIReport)
                        {
                            reportName = ((PowerBIReport)filterParent).Name;
                        }
                    }
                    this.ReportDependents.Add(new
                    {
                        Type = dependent.Type,
                        Name = ((IPowerBILeafNode)dependent).Name,
                        ReportName = reportName,
                        PageName = pageName
                    });
                }
            }
            OnPropertyChanged(nameof(this.ReportDependents));
            OnPropertyChanged(nameof(this.SemanticModelDependents));
        }

        public void FilterUsageRecords(object send, FilterEventArgs e)
        {
            if (e.Item is DataGridUsageRecord record)
            {
                e.Accepted = _filteredRecords.Contains(record);
                return;
            }
            e.Accepted = false;
        }

        private async void FilterRows(int debounceDelay = 300)
        {
            if (_filterCts != null)
                _filterCts.Cancel();
            _filterCts = new CancellationTokenSource();
            var token = _filterCts.Token;
            try
            {
                await Task.Delay(debounceDelay, token); // debounce

                string modelFilter = this.SemanticModelFilter.Name ;
                string typeFilter = this.ArtifactTypeFilter;
                string usageFilter = this.UsageStateFilter;
                string artifactNameFilter = this.ArtifactNameFilter.ToLower().Trim();
                string tableNameFilter = this.TableNameFilter.ToLower().Trim();

                this._filteredRecords = await Task.Run(() =>
                {
                    List<DataGridUsageRecord> filteredRecords = new List<DataGridUsageRecord>();
                    foreach (var record in this._usageRecords)
                    {
                        bool matchUsageState = record.Model == modelFilter;
                        matchUsageState &= typeFilter == "All" || record.Type == typeFilter;
                        matchUsageState &= usageFilter == "All" || usageFilter == record.UsageState.ToString();
                        matchUsageState &= artifactNameFilter.Length == 0 || record.DataInput.Name.ToLower().Contains(artifactNameFilter);
                        matchUsageState &= tableNameFilter.Length == 0 || record.DataInput.ParentTable.Name.ToLower().Contains(tableNameFilter.ToLower());
                        if (matchUsageState)
                        {
                            filteredRecords.Add(record);
                        }
                    }
                    return filteredRecords;
                }, token);

                this.UsageRecordsView.View.Refresh();
                OnPropertyChanged(nameof(UsageRecordsView));
            }
            catch (TaskCanceledException)
            {
            }

        }

    }
}
