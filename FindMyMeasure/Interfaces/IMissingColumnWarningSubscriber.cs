using FindMyMeasure.WarningClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FindMyMeasure.Interfaces
{
    public interface IMissingColumnWarningSubscriber
    {
        void OnWarningReceived(MissingColumnWarning warning);
    }
}
