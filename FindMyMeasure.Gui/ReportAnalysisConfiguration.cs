using System;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace FindMyMeasure.Gui
{
    public class ReportAnalysisConfiguration : IEquatable<ReportAnalysisConfiguration>, INotifyPropertyChanged
    {
        public enum ModelConnectionType
        {
            Local,
            Remote
        }

        [JsonIgnore]
        private static int NextReportId = 1;

        [JsonConstructor]
        public ReportAnalysisConfiguration(string reportName, string reportPath, string modelName, string modelConnectionString, bool analyseHiddenVisuals, bool analyseHiddenPages, ModelConnectionType modelType)
        {
            this._intReportId = NextReportId;
            NextReportId++;

            this._reportName = reportName;
            this._reportPath = reportPath;
            this._modelName = modelName;
            this._modelConnectionString = modelConnectionString;
            this._modelType = modelType;
            this._analyseHiddenVisuals = analyseHiddenVisuals;
            this._analyseHiddenPages = analyseHiddenPages;
        }

        [JsonIgnore]
        private int _intReportId;

        [JsonIgnore]
        private bool _analyseHiddenPages;

        [JsonIgnore]
        private bool _analyseHiddenVisuals;

        [JsonIgnore]
        private string _reportPath;

        [JsonIgnore]
        private string _reportName;

        [JsonIgnore]
        private string _modelName;

        [JsonIgnore]
        private string _modelConnectionString;

        [JsonIgnore]
        private ModelConnectionType _modelType;


        [JsonIgnore]
        public int ReportId { get => this._intReportId ; }

        [JsonPropertyName("ReportName")]
        [JsonInclude]
        public string ReportName { get => this._reportName; }

        [JsonPropertyName("ReportPath")]
        [JsonInclude]
        public string ReportPath { get => this._reportPath; }

        [JsonPropertyName("ModelName")]
        [JsonInclude]
        public string ModelName { get => this._modelName; }

        [JsonPropertyName("ModelConnectionString")]
        [JsonInclude]
        public string ModelConnectionString { get => this._modelConnectionString; }

        [JsonPropertyName("AnalyseHiddenPages")]
        [JsonInclude]
        public bool AnalyseHiddenPages { 
            get =>  this._analyseHiddenPages;
            set{
                if(this._analyseHiddenPages != value){
                    this._analyseHiddenPages = value;
                    OnPropertyChanged(nameof(AnalyseHiddenPages));
                }
            } 
        }

        [JsonPropertyName("AnalyseHiddenVisuals")]
        [JsonInclude]
        public bool AnalyseHiddenVisuals { 
            get => this._analyseHiddenVisuals; 
            set {
                if(this._analyseHiddenVisuals != value){
                    this._analyseHiddenVisuals = value;
                    OnPropertyChanged(nameof(AnalyseHiddenVisuals));
                }
            } 
        }

        [JsonPropertyName("ModelType")]
        [JsonInclude]
        public ModelConnectionType ModelType { get => this._modelType; }

        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
        }

        public bool Equals(ReportAnalysisConfiguration other)
        {
            return ReportName == other.ReportName && this.ReportPath == other.ReportPath && this.ModelName == other.ModelName && this.ModelConnectionString == other.ModelConnectionString;
        }
    }
}
