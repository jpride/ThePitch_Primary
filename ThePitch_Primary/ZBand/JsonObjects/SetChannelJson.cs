using System.Collections.Generic;


namespace ZBand_EZTV
{
    public class ActionParameter
    {
        public string name { get; set; }
        public string value { get; set; }
    }

    public class ActionTarget
    {
        public string id { get; set; }
        public string cmdTargetType { get; set; }
    }

    public class SetChannelJsonObject
    {
        public string action { get; set; }
        public string actionCluster { get; set; }
        public List<ActionParameter> actionParameters { get; set; }
        public List<ActionTarget> actionTargets { get; set; }
    }
}
