using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZBand_EZTV
{
    public class ChannelItem
    {
        public int id { get; set; }
        public int number { get; set; }
        public string name { get; set; }
        public string description { get; set; }
        public string iPAddress { get; set; }
        public int port { get; set; }
        public string type { get; set; }
        public string encryptionType { get; set; }
        public string encryptionKey { get; set; }
        public string encryptionKeyType { get; set; }
    }



    public class ChannelJsonObject
    {
        public Link link { get; set; }
        public int offset { get; set; }
        public int limit { get; set; }
        public int total { get; set; }
        public List<ChannelItem> items { get; set; }
        public FirstRef firstRef { get; set; }
        public NextRef nextRef { get; set; }
        public PrevRef prevRef { get; set; }
        public LastRef lastRef { get; set; }
    }


}
