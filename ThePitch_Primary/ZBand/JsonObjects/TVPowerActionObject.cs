using System.Collections.Generic;


namespace ZBand_EZTV
{
    public class TVPowerActionObject
    {
        public string action { get; set; }
        public string actionCluster { get; set; }
        public List<ActionParameter> actionParameters { get; set; }
        public List<ActionTarget> actionTargets { get; set; }
    }
}
