using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThePitch_Primary.ZBand.JsonObjects
{
    public class TVPowerActionObject
    {
        public string action { get; set; }
        public string actionCluster { get; set; }
        public List<ActionParameter> actionParameters { get; set; }
        public List<ActionTarget> actionTargets { get; set; }
    }
}
