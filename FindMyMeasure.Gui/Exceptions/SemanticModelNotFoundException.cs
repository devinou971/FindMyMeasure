using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FindMyMeasure.Gui.Exceptions
{
    internal class SemanticModelNotFoundException : Exception
    {
        public SemanticModelNotFoundException(string message) : base(message)
        {
        }
    }
}
