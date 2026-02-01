using FindMyMeasure.Enums;
using FindMyMeasure.Interfaces;
using System;

namespace FindMyMeasure.Gui.MVVM
{
    public class DataGridUsageRecord
    {
        static private int Next_id = 0;
        private int _id;
        public IDataInput DataInput { get; }
        public string Type { get => this.DataInput.Type; }
        public string Name { get => this.DataInput.Name; }
        public string Model { get; }
        public string Table { get => this.DataInput.ParentTable.Name; }
        public int NbOfUsage { get; }
        public UsageState UsageState { get; }
        public String Expression { get => DataInput.Expression; }
        public bool HasExpression { get => !string.IsNullOrEmpty(this.Expression); }

        public DataGridUsageRecord(IDataInput dataInput, string SemanticModeName)
        {
            _id = Next_id++;
            DataInput = dataInput;
            Model = SemanticModeName;
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
