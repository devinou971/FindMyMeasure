using FindMyMeasure.Database;
using FindMyMeasure.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace FindMyMeasure.Gui
{
    public class DataGridUsageRecord
    {
        public IDataInput DataInput { get; }

        public string Type { get; }
        public string Name { get; }
        public string Model { get; }
        public string Table { get; }
        public int NbOfUsage { get; }
        public UsageState UsageState { get; } 

        public DataGridUsageRecord(IDataInput dataInput, string SemanticModeName)
        {
            DataInput = dataInput;
            Model = SemanticModeName;
            Type = this.DataInput.Type;
            Name = this.DataInput.Name;
            Table = this.DataInput.ParentTable.Name;
            NbOfUsage = this.DataInput.GetDependents().Count;
            UsageState = this.DataInput.GetUsageState();

        }
    }
}
