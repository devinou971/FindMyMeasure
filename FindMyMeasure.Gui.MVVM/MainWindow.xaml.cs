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
        private IEnumerable<ReportAnalysisConfiguration> _reportAnalysisConfigurations;
        private MainWindowViewModel _viewModel;

        public MainWindow(HashSet<SemanticModel> semanticModels, IEnumerable<ReportAnalysisConfiguration> reportAnalysisConfigurations, HashSet<DataGridUsageRecord> usageRecords)
        {
            this._reportAnalysisConfigurations = reportAnalysisConfigurations;

            InitializeComponent();
            this._viewModel = new MainWindowViewModel(semanticModels, usageRecords);
            this.DataContext = _viewModel;

            this.Resources.MergedDictionaries.Add(Utils.GetLanguageDictionary());
        }

        private void dgUsageRecords_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            
            spDetailsPanel.Visibility = Visibility.Visible;

            if (dgUsageRecords.SelectedCells.Count == 0)
                return;

            DataGridUsageRecord selectedRecord = (DataGridUsageRecord)dgUsageRecords.SelectedCells[0].Item;

            this._viewModel.SelectedArtifact = selectedRecord;

            //lbSelectedElementName.Content = $"{selectedRecord.Type} : {selectedRecord.Name} - {selectedRecord.UsageState}({selectedRecord.NbOfUsage})";
            
        }

        private void bReturnToReportSelection_Click(object sender, RoutedEventArgs e)
        {
            ReportSelectionWindow reportSelectionWindow = new ReportSelectionWindow(this._reportAnalysisConfigurations);
            reportSelectionWindow.Show();
            this.Close();
        }

    }
}
