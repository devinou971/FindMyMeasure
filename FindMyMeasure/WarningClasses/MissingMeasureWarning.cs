using FindMyMeasure.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FindMyMeasure.WarningClasses
{
    public class MissingMeasureWarning : AnalysisWarning
    {
        private string _measureName;
        private string _tableName;
        private IPowerBILeafNode _sender;
        public string MeasureName => _measureName;
        public string TableName => _tableName;
        public IPowerBILeafNode Sender => _sender;

        public MissingMeasureWarning(IPowerBILeafNode sender, string measureName, string tableName) : base($"The measure {measureName} from table {tableName} used in {sender.Name} doesn't exist in semantic model.")
        {
            this._sender = sender;
            this._measureName = measureName;
            this._tableName = tableName;
        }
    }
}
