using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZBand_EZTV
{
    public class LoginEventArgs : EventArgs
    { 
        public bool isLoggedIn { get; set; }
    
    }

    public class ChannelInfoEventArgs : EventArgs
    {
        public ushort[] id { get; set; }
        public ushort[] number { get; set; }
        public string[] name { get; set; }
        public string[] description { get; set; }

        public ushort channelListCount { get; set; }
    }


    public class EndpointInfoEventArgs : EventArgs
    {
        public string[] id { get; set; }
        public string[] name { get; set; }
        public ushort[] endpointId { get; set; }
        public string[] ipAddress { get; set; }
        public string[] description { get; set; }
        public string[] sn { get; set; }
        public string[] mac { get; set; }

        public ushort endpointListCount { get; set; }
    }


    public class EPGInfoEventArgs : EventArgs
    {
        public string[] id { get; set; }
        public string[] name { get; set; }
        public string[] description { get; set; }
        public string[] startTime { get; set; }
        public string[] endTime { get; set; }
        public string[] episodeDescription { get; set; }

        //public ushort programCount { get; set; }
        //public string episodeNumber { get; set; }
        //public string episodeSeason { get; set; }
        //public string genre { get; set; }
        //public string originalGenre { get; set; }
        //public string scheduleId { get; set; }
    }

}
