using FindMyMeasure.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FindMyMeasure.WarningClasses
{
    public class MissingHierarchyWarning : MissingArtifactWarning // TODO : Deprecate this to replace with MissingArtifactWarning
    {
        private string _hierarchyName;
        public string HierarchyName => _hierarchyName;

        public MissingHierarchyWarning(IPowerBILeafNode sender, string hierarchyName, string tableName) : base(sender, "Hierarchy", hierarchyName, tableName)
        {
            this._hierarchyName = hierarchyName;
        }
    }
}
