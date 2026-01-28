using FindMyMeasure.Database;
using FindMyMeasure.Gui.MVVM.ViewModels;
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

namespace FindMyMeasure.Gui.MVVM
{
    /// <summary>
    /// Logique d'interaction pour MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
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

            InitializeComponent();
            var viewModel = new MainWindowViewModel(semanticModels, reportAnalysisConfigurations, usageRecords);
            this.DataContext = viewModel;

            this.Resources.MergedDictionaries.Add(Utils.GetLanguageDictionary());
        }

        private void dgUsageRecords_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            // TODO : Split this method into smaller methods
            spDetailsPanel.Visibility = Visibility.Visible;

            if (dgUsageRecords.SelectedCells.Count == 0)
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
                        Name = (dependent is IDataInput) ? $"{dependent.Name} ({((IDataInput)dependent).GetUsageState()})" : dependent.Name,
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

    }
}
