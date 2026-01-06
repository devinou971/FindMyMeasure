using FindMyMeasure.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FindMyMeasure.WarningClasses
{
    public class MissingColumnWarning : MissingArtifactWarning // TODO : Deprecate this to replace with MissingArtifactWarning
    {
        private string _columnName;
        public string ColumnName => this._columnName;

        public MissingColumnWarning(IPowerBILeafNode sender, string columnName, string tableName) : base(sender, "Column", columnName, tableName)
        {
            this._columnName = columnName;
        }
    }
}
