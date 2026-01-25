using FindMyMeasure.Gui.MVVM.Commands;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace FindMyMeasure.Gui.MVVM.ViewModels
{
    public class ReportSelectionViewModel : ViewModelBase
    {

        private bool? analyseHiddenPages;
        private bool? analyseHiddenVisuals;
        private ObservableCollection<ReportAnalysisConfiguration> reportConfigList = new ObservableCollection<ReportAnalysisConfiguration>();

        public ICommand UpdateReportListCommand { get; } = new RelayCommand(_ => { Console.WriteLine("Testing"); });

        public ReportSelectionViewModel()
        {
            AnalyseHiddenPages = false;
            UpdateReportListCommand = new RelayCommand(action => updateReportsAdvancedSettings());
        }


        public bool? AnalyseHiddenPages
        {
            get => this.analyseHiddenPages;
            set
            {
                if (this.analyseHiddenPages == value)
                    return;
                this.analyseHiddenPages = value;
                OnPropertChanged();
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
                OnPropertChanged();
                updateReportsAdvancedSettings();
            }
        }

        public ObservableCollection<ReportAnalysisConfiguration> ReportConfigList
        {
            get => this.reportConfigList;
        }

        public void updateReportsAdvancedSettings()
        {
            Console.WriteLine("Here");
            foreach(var report in this.reportConfigList)
            {
                if (!analyseHiddenPages is null)
                    report.AnalyseHiddenPages = this.AnalyseHiddenPages ?? false;

                if (!analyseHiddenVisuals is null)
                    report.AnalyseHiddenVisuals= this.AnalyseHiddenVisuals ?? false;
            }
            OnPropertChanged(nameof(this.ReportConfigList));
        }
    }
}
