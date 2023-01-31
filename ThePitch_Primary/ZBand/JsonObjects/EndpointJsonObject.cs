using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZBand_EZTV
{
    
    public class EndpointItem
    {
        public string id { get; set; }
        public string sn { get; set; }
        public int endpointId { get; set; }
        public string name { get; set; }
        public string ipAddress { get; set; }
        public string description { get; set; }
        public string mac { get; set; }
        public string playingChannel1Number { get; set; }
        public string playingChannel1Name { get; set; }
    }


    public class EndpointJsonObject
    {
        public Link link { get; set; }
        public int offset { get; set; }
        public int limit { get; set; }
        public int total { get; set; }
        public List<EndpointItem> items { get; set; }
        public FirstRef firstref { get; set; }
        public NextRef nextref { get; set; }
        public PrevRef prevref { get; set; }
        public LastRef lastref { get; set; }
    }




}
