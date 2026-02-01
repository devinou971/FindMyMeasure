using FindMyMeasure.Database;
using FindMyMeasure.Gui.MVVM.Models;
using FindMyMeasure.Gui.MVVM.ViewModels;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace FindMyMeasure.Gui.MVVM
{
    /// <summary>
    /// Logique d'interaction pour MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private IEnumerable<ReportAnalysisConfiguration> _reportAnalysisConfigurations;
        private MainWindowViewModel _viewModel;

        public MainWindow(ReportAnalysisResult analysisResult)
        {
            this._reportAnalysisConfigurations = analysisResult.ReportAnalysisConfigurations;

            InitializeComponent();
            this._viewModel = new MainWindowViewModel(analysisResult.SemanticModels, analysisResult.DataGridUsageRecords);
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
        }

        private void bReturnToReportSelection_Click(object sender, RoutedEventArgs e)
        {
            ReportSelectionWindow reportSelectionWindow = new ReportSelectionWindow(this._reportAnalysisConfigurations);
            reportSelectionWindow.Show();
            this.Close();
        }

    }
}
