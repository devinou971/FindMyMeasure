using FindMyMeasure.Database;
using FindMyMeasure.Enums;
using FindMyMeasure.Gui.MVVM.Commands;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;

namespace FindMyMeasure.Gui.MVVM.ViewModels
{
    

    internal class MainWindowViewModel : ViewModelBase
    {

        private CancellationTokenSource _filterCts;
        private IEnumerable<DataGridUsageRecord> _filteredRecords;

        private HashSet<SemanticModel> _semanticModels;
        private IEnumerable<ReportAnalysisConfiguration> _reportAnalysisConfigurations;
        private IEnumerable<DataGridUsageRecord> _usageRecords;

        public ICommand FilterArtifactsBySemanticModel;

        public CollectionViewSource UsageRecordsView { get; }

        public ObservableCollection<SemanticModel> SemanticModels { get; }

        private SemanticModel _semanticModelFilter;
        public SemanticModel SemanticModelFilter { 
            get => this._semanticModelFilter;
            set
            {
                _semanticModelFilter = value;
                calculateStats();
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

        public MainWindowViewModel(HashSet<SemanticModel> semanticModels, IEnumerable<ReportAnalysisConfiguration> reportAnalysisConfigurations, IEnumerable<DataGridUsageRecord> usageRecords)
        {
            this._semanticModels = semanticModels;
            this._reportAnalysisConfigurations = reportAnalysisConfigurations;
            this._usageRecords = usageRecords;
            this._filteredRecords = usageRecords;


            this.SemanticModels = new ObservableCollection<SemanticModel>(this._semanticModels);
            this.Stats = new ObservableCollection<string>();
            this.SemanticModelFilter = this._semanticModels.FirstOrDefault();

            //this.UsageRecordsView = CollectionViewSource.GetDefaultView(usageRecords);
            this.UsageRecordsView = new CollectionViewSource { Source = usageRecords };
            this.UsageRecordsView.Filter += FilterUsageRecords;

            this.FilterArtifactsBySemanticModel = new RelayCommand(semanticModelName => filterArtifactsBySemanticModel(semanticModelName));

        }

        private void calculateStats()
        {
            this.Stats.Clear();
            this.Stats.Add($"Number of Tables: {this.SemanticModelFilter.GetTables().Count}");
            this.Stats.Add($"Number of Measures: {this.SemanticModelFilter.GetMeasures().Count}");
            this.Stats.Add($"Number of Columns: {this.SemanticModelFilter.GetColumns().Count}");
            this.Stats.Add($"Number of Relationships: {this.SemanticModelFilter.GetRelationships().Count}");
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


        private void filterArtifactsBySemanticModel(object semanticModel)
        {
            if(semanticModel is string semanticModelName)
            {

            }
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
                string typeFilter = this.ArtifactTypeFilter; //cbTypeFilter.SelectedValue.ToString();
                //int typeFilterId = cbTypeFilter.SelectedIndex;
                string usageFilter = this.UsageStateFilter; // cbUsageFilter.SelectedValue.ToString();
                //int usageFilterId = cbUsageFilter.SelectedIndex;
                //string artifactNameFilter = tbArtifactNameSearch.Text.ToLower().Trim();
                //string tableNameFilter = tbTableNameSearch.Text.ToLower().Trim();

                this._filteredRecords = await Task.Run(() =>
                {
                    List<DataGridUsageRecord> filteredRecords = new List<DataGridUsageRecord>();
                    foreach (var record in this._usageRecords)
                    {
                        bool matchUsageState = record.Model == modelFilter;
                        matchUsageState &= typeFilter == "All" || record.Type == typeFilter; // typeFilterId == 0 || record.Type == typeFilter;
                        matchUsageState &= usageFilter == "All" || usageFilter == record.UsageState.ToString(); //usageFilterId == 0 || usageFilter == record.UsageState.ToString();
                        //matchUsageState &= artifactNameFilter.Length == 0 || record.DataInput.Name.ToLower().Contains(artifactNameFilter);
                        //matchUsageState &= tableNameFilter.Length == 0 || record.DataInput.ParentTable.Name.ToLower().Contains(tableNameFilter.ToLower());
                        if (matchUsageState)
                        {
                            filteredRecords.Add(record);
                        }
                    }
                    return filteredRecords;
                }, token);

                this.UsageRecordsView.View.Refresh();
                OnPropertyChanged("UsageRecordsView.View");
            }
            catch (TaskCanceledException)
            {
            }

        }

    }
}
