using FindMyMeasure.Database;
using FindMyMeasure.Enums;
using FindMyMeasure.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace FindMyMeasure.Gui.MVVM
{
    public class DataGridUsageRecord
    {
        static private int Next_id = 0;
        private int _id;
        public IDataInput DataInput { get; }
        public string Type { get; }
        public string Name { get; }
        public string Model { get; }
        public string Table { get; }
        public int NbOfUsage { get; }
        public UsageState UsageState { get; }

        public DataGridUsageRecord(IDataInput dataInput, string SemanticModeName)
        {
            _id = Next_id++;
            DataInput = dataInput;
            Model = SemanticModeName;
            Type = this.DataInput.Type;
            Name = this.DataInput.Name;
            Table = this.DataInput.ParentTable.Name;
            NbOfUsage = this.DataInput.GetDependents().Count;
            UsageState = this.DataInput.GetUsageState();
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;
            if (obj is DataGridUsageRecord dataRecord)
            {
                return dataRecord._id == this._id;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return this._id.GetHashCode();
        }
    }
}
