using FindMyMeasure.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FindMyMeasure.WarningClasses
{
    public class MissingHierarchyWarning : AnalysisWarning
    {
        private string _hierarchyName;
        private string _tableName;
        private IPowerBILeafNode _sender;
        public string HierarchyName => _hierarchyName;
        public string TableName => _tableName;
        public IPowerBILeafNode Sender => _sender;

        public MissingHierarchyWarning(IPowerBILeafNode sender, string hierarchyName, string tableName) : base($"The hierarchy {hierarchyName} from table {tableName} used in {sender.Name} doesn't exist in semantic model.")
        {
            this._sender = sender;
            this._hierarchyName = hierarchyName;
            this._tableName = tableName;
        }
    }
}
