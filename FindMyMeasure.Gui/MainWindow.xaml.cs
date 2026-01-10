using FindMyMeasure.Database;
using FindMyMeasure.Interfaces;
using FindMyMeasure.PowerBI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media.TextFormatting;
using System.Windows.Threading;

namespace FindMyMeasure.Gui
{
    /// <summary>
    /// Logique d'interaction pour MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        public ICollectionView UsageRecordsView { get; }

        private CancellationTokenSource _filterCts;
        private IEnumerable<DataGridUsageRecord> _filteredRecords;

        private HashSet<SemanticModel> _semanticModels;
        private IEnumerable<ReportAnalysisConfiguration> _reportAnalysisConfigurations;
        private IEnumerable<DataGridUsageRecord> _usageRecords;

        public MainWindow(HashSet<SemanticModel> semanticModels, IEnumerable<ReportAnalysisConfiguration> reportAnalysisConfigurations, HashSet<DataGridUsageRecord> usageRecords)
        {
            this._semanticModels = semanticModels;
            this._reportAnalysisConfigurations = reportAnalysisConfigurations;
            this._usageRecords = usageRecords;
            this._filteredRecords = usageRecords;
            ObservableCollection<string> dataGridSementicModelNames = new ObservableCollection<string>(semanticModels.Select(x => x.Name));

            InitializeComponent();

            this.UsageRecordsView = CollectionViewSource.GetDefaultView(usageRecords);
            this.UsageRecordsView.Filter = FilterUsageRecords;

            dgUsageRecords.ItemsSource = this.UsageRecordsView;

            cbSementicModelFilter.ItemsSource = dataGridSementicModelNames;
            if (dataGridSementicModelNames.Count > 0)
            {
                cbSementicModelFilter.SelectedIndex = 0;
            }

            this.Resources.MergedDictionaries.Add(Utils.GetLanguageDictionary());
        }

        public bool FilterUsageRecords(object obj)
        {
            if (obj is DataGridUsageRecord record)
            {
                if (cbSementicModelFilter.SelectedItem == null)
                {
                    return true;
                }
                return _filteredRecords.Contains(record);
            }
            return false;
        }

        private void cbSementicModelFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.UsageRecordsView == null)
                return;

            var selectedSemanticModel = this._semanticModels.First(x => x.Name == this.cbSementicModelFilter.SelectedItem.ToString());
            cbTypeFilter.SelectedIndex = 0;
            cbUsageFilter.SelectedIndex = 0;

            this.UsageRecordsView.Refresh();

            lbStats.Items.Clear();
            lbStats.Items.Add($"Number of Tables: {selectedSemanticModel.GetTables().Count}");
            lbStats.Items.Add($"Number of Measures: {selectedSemanticModel.GetMeasures().Count}");
            lbStats.Items.Add($"Number of Columns: {selectedSemanticModel.GetColumns().Count}");
            lbStats.Items.Add($"Number of Relationships: {selectedSemanticModel.GetRelationships().Count}");
        }

        private void dgUsageRecords_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            // TODO : Split this method into smaller methods
            spDetailsPanel.Visibility = Visibility.Visible;

            if(dgUsageRecords.SelectedCells.Count == 0)
                return;

            DataGridUsageRecord selectedRecord = (DataGridUsageRecord)dgUsageRecords.SelectedCells[0].Item;
            
            lbSelectedElementName.Content = $"{selectedRecord.Type} : {selectedRecord.Name} - {selectedRecord.UsageState}({selectedRecord.NbOfUsage})";
            
            tbExpression.Text = selectedRecord.DataInput.Expression;
            tbExpression.Visibility = string.IsNullOrEmpty(selectedRecord.DataInput.Expression) ? Visibility.Collapsed : Visibility.Visible;

            var dependents = selectedRecord.DataInput.GetDependents();
            dgSementicModelDependents.Items.Clear();
            dgReportDependents.Items.Clear();
            foreach (var dependent in dependents)
            {
                if (dependent is IDataInput || dependent is Table || dependent is Relationship)
                {
                    dgSementicModelDependents.Items.Add(new 
                    { 
                        Type = dependent.Type,
                        Name = (dependent is IDataInput) ? $"{dependent.Name} ({((IDataInput)dependent).GetUsageState()})"  : dependent.Name,
                        TableName = (dependent is IDataInput) ? ((IDataInput)dependent).ParentTable.Name :
                                (dependent is Relationship) ? ((Relationship)dependent).FromColumn.ParentTable.Name + " -> " + ((Relationship)dependent).ToColumn.ParentTable.Name :
                                ""
                    });
                }
                else if (dependent is FindMyMeasure.Interfaces.IPowerBILeafNode)
                {
                    string reportName = "";
                    string pageName = "";
                    if (dependent is Visual)
                    {
                        pageName = ((Visual)dependent).GetReportPage().Name;
                        reportName = ((Visual)dependent).GetReportPage().GetPowerBIReport().Name;
                    } else if (dependent is Filter)
                    {
                        var filterParent = ((Filter)dependent).GetParent();
                        if (filterParent is Visual)
                        {
                            pageName = ((Visual)filterParent).GetReportPage().Name;
                            reportName = ((Visual)filterParent).GetReportPage().GetPowerBIReport().Name;
                        } else if (filterParent is ReportPage)
                        {
                            pageName = ((ReportPage)filterParent).Name;
                            reportName = ((ReportPage)filterParent).GetPowerBIReport().Name;
                        } else if (filterParent is PowerBIReport)
                        {
                            reportName = ((PowerBIReport)filterParent).Name;
                        }
                    }
                    dgReportDependents.Items.Add(new 
                    { 
                        Type = dependent.Type,
                        Name = ((FindMyMeasure.Interfaces.IPowerBILeafNode)dependent).Name,
                        ReportName = reportName,
                        PageName = pageName
                    });
                }
            }
        }

        private void bReturnToReportSelection_Click(object sender, RoutedEventArgs e)
        {

            ReportSelectionWindow reportSelectionWindow = new ReportSelectionWindow(this._reportAnalysisConfigurations);
            reportSelectionWindow.Show();
            this.Close();
        }

        private void cbTypeFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RunFilterAsync();
        }

        private void cbUsageFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RunFilterAsync();
        }

        private void tbArtifactNameSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            RunFilterAsync();
        }

        private void tbTableNameSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            RunFilterAsync();
        }

        private async void RunFilterAsync()
        {
            if (_filterCts != null)
                _filterCts.Cancel();
            _filterCts = new CancellationTokenSource();
            var token = _filterCts.Token;
            try
            {
                await Task.Delay(300, token); // debounce

                string modelFilter = cbSementicModelFilter.SelectedValue.ToString();
                string typeFilter = cbTypeFilter.SelectedValue.ToString();
                int typeFilterId = cbTypeFilter.SelectedIndex;
                string usageFilter = cbUsageFilter.SelectedValue.ToString();
                int usageFilterId = cbUsageFilter.SelectedIndex;
                string artifactNameFilter = tbArtifactNameSearch.Text.ToLower().Trim();
                string tableNameFilter = tbTableNameSearch.Text.ToLower().Trim();

                this._filteredRecords = await Task.Run(() =>
                {
                    List<DataGridUsageRecord> filteredRecords = new List<DataGridUsageRecord>();
                    foreach (var record in this._usageRecords)
                    {
                        bool matchUsageState = record.Model == modelFilter;
                        matchUsageState &= typeFilterId == 0 || record.Type == typeFilter;
                        matchUsageState &= usageFilterId == 0 || usageFilter == record.UsageState.ToString();
                        matchUsageState &= artifactNameFilter.Length == 0 || record.DataInput.Name.ToLower().Contains(artifactNameFilter);
                        matchUsageState &= tableNameFilter.Length == 0 || record.DataInput.ParentTable.Name.ToLower().Contains(tableNameFilter.ToLower());
                        if (matchUsageState)
                        {
                            filteredRecords.Add(record);
                        }
                    }
                    return filteredRecords;
                }, token);

                this.UsageRecordsView.Refresh();
            }
            catch (TaskCanceledException)
            {
            }

        }
    }
}
