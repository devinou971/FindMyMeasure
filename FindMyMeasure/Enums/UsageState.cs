using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FindMyMeasure.Enums
{
    public enum UsageState
    {
        /// <summary>The measure/column is not used anywhere.</summary>
        Unused,
        /// <summary>The measure/column is used by other measures/columns, but all of those are Unused.</summary>
        UsedByUnused,
        /// <summary>The measure/column is used directly or indirectly in a report or relationship.</summary>
        Used
        
    }
}
