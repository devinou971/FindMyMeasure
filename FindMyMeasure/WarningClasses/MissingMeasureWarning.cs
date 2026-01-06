using FindMyMeasure.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FindMyMeasure.WarningClasses
{
    public class MissingMeasureWarning : MissingArtifactWarning // TODO : Deprecate this to replace with MissingArtifactWarning
    {
        private string _measureName;
        public string MeasureName => _measureName;

        public MissingMeasureWarning(IPowerBILeafNode sender, string measureName, string tableName) : base(sender, "Measure", measureName, tableName)
        {
            this._measureName = measureName;
        }
    }
}
