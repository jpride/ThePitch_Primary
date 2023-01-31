using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace ZBand_EZTV
{
    public class EPGChannelItem
    {
        public int id { get; set; }
        public int number { get; set; }
        public string name { get; set; }
        public object encryptionKey { get; set; }
        public string encryptionType { get; set; }
        public string encryptionKeyType { get; set; }
        public bool hasNoPrograms { get; set; }
        public int? prgSvcId { get; set; }
        public string type { get; set; }
        public List<Program> programs { get; set; }
        public bool isRecording { get; set; }
        public bool recordable { get; set; }
        public object link { get; set; }
    }

    public class Program
    {
        public string description { get; set; }
        public DateTime start { get; set; }
        public DateTime end { get; set; }
        public string episodeDescription { get; set; }
        public string episodeNumber { get; set; }
        public string episodeSeason { get; set; }
        public string genre { get; set; }
        public string id { get; set; }
        public string name { get; set; }
        public string originalGenre { get; set; }
        public string scheduleId { get; set; }
    }

    public class EpgAllJsonObject
    {
        public Link link { get; set; }
        public int offset { get; set; }
        public int limit { get; set; }
        public int total { get; set; }
        public List<EPGChannelItem> items { get; set; }
        public FirstRef firstRef { get; set; }
        public NextRef nextRef { get; set; }
        public PrevRef prevRef { get; set; }
        public LastRef lastRef { get; set; }
    }




}
