using FindMyMeasure.Enums;
using FindMyMeasure.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FindMyMeasure.Database
{
    public class Hierarchy : DatabaseArtifact
    {
        public Hierarchy(ulong hierarchyId, string name, Table table) 
            : base(hierarchyId, name, "Hierarchy", table) 
        { }
    }
}
