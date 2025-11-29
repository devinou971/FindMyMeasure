using FindMyMeasure.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FindMyMeasure.WarningClasses
{
    public class MissingColumnWarning : AnalysisWarning
    {
        private string _columnName;
        private string _tableName;
        private IPowerBILeafNode _sender;
        public string ColumnName => this._columnName;
        public string TableName => this._tableName;
        public IPowerBILeafNode Sender => this._sender;

        public MissingColumnWarning(IPowerBILeafNode sender, string columnName, string tableName) : base($"The column {columnName} from table {tableName} used in {sender.Name} doesn't exist in semantic model.")
        {
            this._sender = sender;
            this._columnName = columnName;
            this._tableName = tableName;
        }
    }
}
