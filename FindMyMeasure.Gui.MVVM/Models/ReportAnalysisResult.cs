using FindMyMeasure.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FindMyMeasure.Gui.MVVM.Models
{
    public class ReportAnalysisResult
    {
        private readonly HashSet<SemanticModel> semanticModels;
        private readonly IEnumerable<ReportAnalysisConfiguration> reportAnalysisConfigurations;
        private readonly HashSet<DataGridUsageRecord> dataGridUsageRecords;

        public HashSet<SemanticModel> SemanticModels { get { return semanticModels; } }
        public IEnumerable<ReportAnalysisConfiguration> ReportAnalysisConfigurations { get { return reportAnalysisConfigurations; } }
        public HashSet<DataGridUsageRecord> DataGridUsageRecords {  get { return dataGridUsageRecords; } }

        public ReportAnalysisResult(HashSet<SemanticModel> semanticModels, IEnumerable<ReportAnalysisConfiguration> reportAnalysisConfigurations, HashSet<DataGridUsageRecord> usageRecords)
        {
            this.semanticModels = semanticModels;
            this.reportAnalysisConfigurations = reportAnalysisConfigurations;
            this.dataGridUsageRecords = usageRecords;
        }
    }
}
