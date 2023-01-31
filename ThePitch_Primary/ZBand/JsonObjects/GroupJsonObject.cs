using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZBand_EZTV
{
    public class GroupItem
    {
        public string id { get; set; }
        public string name { get; set; }
        public string description { get; set; }
        public bool permittedToAll { get; set; }
        public string gType { get; set; }
        public object linkedObjects { get; set; }
        public LinkedObjectsLink linkedObjectsLink { get; set; }
        public string customData01 { get; set; }
        public string customData02 { get; set; }
        public Link link { get; set; }
    }



    public class LinkedObjectsLink
    {
        public string type { get; set; }
        public string @ref { get; set; }
    }



    public class GroupJsonObject
    {
        public Link link { get; set; }
        public int offset { get; set; }
        public int limit { get; set; }
        public int total { get; set; }
        public List<GroupItem> items { get; set; }
        public FirstRef firstRef { get; set; }
        public NextRef nextRef { get; set; }
        public PrevRef prevRef { get; set; }
        public LastRef lastRef { get; set; }
    }

}
