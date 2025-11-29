using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FindMyMeasure.WarningClasses
{
    public class AnalysisWarning
    {
        private string _warningMessage;
        public string WarningMessage => this._warningMessage;
        public AnalysisWarning(string warningMessage)
        {
            this._warningMessage = warningMessage;
        }
    }
}
