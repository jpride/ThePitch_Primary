using System;
using Crestron.SimplSharp;                          	// For Basic SIMPL# Classes
using Crestron.SimplSharpPro;                       	// For Basic SIMPL#Pro classes
using System.Threading;
using TSI.HelperClasses;
using ZBand_EZTV;
using System.Collections.Generic;
using Newtonsoft.Json;
using TSI.DebugUtilities;

namespace ThePitch_Primary 
{
    //CHANGE EISCS BEFORE DEPLOYMENT
    public class ControlSystem : CrestronControlSystem
    {
        //var to keep track of when EISCs are online and data is ready for manipulating
        private bool _systemReadyForRouting = false;

        //eisc to send data SimplWindows
        Eisc digitalEisc;
        Eisc endpointEisc;
        Eisc channelEisc;
        Eisc epgEisc;


        //enum for Digital Joins on digitalEisc
        public enum Joins 
        {
            SystemReadyForRouting = 1,
            GetEndpoints = 2,
            GetChannels = 3,
            GetEPG = 4,
            SetSystemDebugging = 5,
            SetZBandDebugging = 6,
        }

        public enum PowerJoinsPerDisplay
        { 
            Bar1PowerOn = 10,
            Bar1PowerOff = 11,
            Bar2PowerOn = 12,
            Bar2PowerOff = 13,
            Bar3PowerOn = 14,
            Bar3PowerOff = 15,
            Bar4PowerOn = 16,
            Bar4PowerOff = 17,
            NorthLounge1PowerOn = 18,
            NorthLounge1PowerOff = 19,
            NorthLounge2PowerOn = 20,
            NorthLounge2PowerOff = 21,
            NorthLounge3PowerOn = 22,
            NorthLounge3PowerOff = 23,
            WestLounge1PowerOn = 24,
            WestLounge1PowerOff = 25,
            WestLounge2PowerOn = 26,
            WestLounge2PowerOff = 27,
            SouthLounge1PowerOn = 28,
            SouthLounge1PowerOff = 29,
            SouthLounge2PowerOn = 30,
            SouthLounge2PowerOff = 31,
            SouthLounge3PowerOn = 32,
            SouthLounge3PowerOff = 33,
            SouthLounge4PowerOn = 34,
            WestLounge4PowerOff = 35,
            PrivatePowerOn = 36,
            PrivatePowerOff = 37,
            Patio1PowerOn = 38,
            Patio1PowerOff = 39,
            Patio2PowerOn = 40,
            Patio2PowerOff = 41,
            Patio3PowerOn = 42,
            Patio3PowerOff = 43,
        }

        public enum PowerEventJoins
        { 
            AllPowerOn = 70,
            AllPowerOff = 71,
            BarPowerOn = 72,
            BarPowerOff = 73,
            NorthPowerOn = 74,
            NorthPowerOff = 75,
            WestPowerOn = 76,
            WestPowerOff = 77,
            SouthPowerOn = 78,
            SouthPowerOff = 79,
            PrivatePowerOn = 80,
            PrivatePowerOff = 81,
            PatioPowerOn = 82,
            PatioPowerOff = 83
        }

        //Hardcoded Event IDs for Display Power Control
        public string AllPwrOnEventID = "63d990d55b8e4e1c541f67fa";
        public string AllPwrOffEventID = "63d990c55b8e4e1c541f67f8";

        public string BarPwrOnEventID = "63d973ae5b8e4e1c541f674f";
        public string BarPwrOffEventID = "63d973345b8e4e1c541f6741";

        public string NorthPwrOnEventID = "63d975905b8e4e1c541f6783";
        public string NorthPwrOffEventID = "63d9754f5b8e4e1c541f6776";

        public string WestPwrOnEventID = "63d975ea5b8e4e1c541f679d";
        public string WestPwrOffEventID = "63d975bb5b8e4e1c541f6790";

        public string SouthPwrOnEventID = "63d9752f5b8e4e1c541f6769";
        public string SouthPwrOffEventID = "63d975085b8e4e1c541f675c";

        public string PrivatePwrOnEventID = "63d976645b8e4e1c541f67ac";
        public string PrivatePwrOffEventID = "63d976505b8e4e1c541f67aa";

        public string PatioPwrOnEventID = "63d976c95b8e4e1c541f67c7";
        public string PatioPwrOffEventID = "63d976b85b8e4e1c541f67c4";


        private uint endpointNowPlayingNameJoinOffset = 30;
        private uint endpointNowPlayingNumberJoinOffset = 60;
        private uint endpointNowPlayingProgramName = 100;

        
        //Zband Server Class Init and field setting
        ZBandServerCommsManager commMgr;

        private string ZBand_Username = "admin@dss-internal";
        private string ZBand_Password = "1qaz!QAZ";
        private string ZBand_ServerIP = "192.168.25.50";

        

        //********************************      Control System and Init         ******************************//
        public ControlSystem()  : base()
        {
            try
            {
                Crestron.SimplSharpPro.CrestronThread.Thread.MaxNumberOfUserThreads = 20;

                //create Eiscs at IPIDs
                endpointEisc = new Eisc(0x03, "127.0.0.2", this, "Endpoint EISC");
                channelEisc = new Eisc(0x04, "127.0.0.2", this, "Channel EISC");
                epgEisc = new Eisc(0x05, "127.0.0.2", this, "EPG EISC");
                digitalEisc = new Eisc(0x06, "127.0.0.2", this, "Digital Triggers EISC");
                

                //tie event to each EISC
                endpointEisc._eiscEvent += Eisc_event;
                channelEisc._eiscEvent += Eisc_event;
                epgEisc._eiscEvent += Eisc_event;
                digitalEisc._eiscEvent += Eisc_event;

                


                //instantiate ZBandServerCommsManager class
                commMgr = new ZBandServerCommsManager();

                //Subscribe to the controller events (System, Program, and Ethernet)
                CrestronEnvironment.EthernetEventHandler += new EthernetEventHandler(_ControllerEthernetEventHandler);
            }
            catch (Exception e)
            {
                ErrorLog.Error("Error in the constructor: {0}", e.Message);
            }
        }

        public override void InitializeSystem()
        {
            try
            {
                var programThread = new Thread(() =>
                    {
                        ErrorLog.Notice("Creating program thread");

                        //set important vars for commMgr
                        commMgr.UserName = ZBand_Username;
                        commMgr.Password = ZBand_Password;
                        commMgr.ServerIP = ZBand_ServerIP;

                        //subscribe to LoginEvents
                        commMgr.LoginEvent += CommMgr_LoginEvent;
 
                        //initialize commMgr
                        commMgr.InitializeCommsManager();
                    });

                programThread.Start();

                //Get Server data at startup
                GetEndpoints(null);
                GetChannels(null);
                GetEPG(null);
                
            }
            catch (Exception e)
            {
                ErrorLog.Error("Error in InitializeSystem: {0}", e.Message);
            }
        }

        private void SetSystemDebugging(string cmdParameters)
        {
            if (cmdParameters.ToLower() == "on")
            {
                Debug.SystemDebugEnabled = true;
            }
            else
            {
                Debug.SystemDebugEnabled = false;
            }

            string state = Debug.SystemDebugEnabled ? "on" : "off";
            ErrorLog.Notice($"System Debugging is now: {state}");
        }

        private void SetZBandDebugging(string cmdParameters)
        {
            if (cmdParameters.ToLower() == "on")
            {
                Debug.ZBandDebugEnabled = true;
            }
            else
            {
                Debug.ZBandDebugEnabled = false;
            }

            string state = Debug.ZBandDebugEnabled ? "on" : "off";
            ErrorLog.Notice($"Zband Debugging is now: {state}");
        }

        //method to update digital EISC with LoggedIn status
        private void CommMgr_LoginEvent(object sender, LoginEventArgs e)
        {
            digitalEisc.SetDigital(1, e.isLoggedIn);
        }

        void _ControllerEthernetEventHandler(EthernetEventArgs ethernetEventArgs)
        {
            switch (ethernetEventArgs.EthernetEventType)
            {//Determine the event type Link Up or Link Down
                case (eEthernetEventType.LinkDown):
                    //Next need to determine which adapter the event is for. 
                    //LAN is the adapter is the port connected to external networks.
                    if (ethernetEventArgs.EthernetAdapter == EthernetAdapterType.EthernetLANAdapter)
                    {
                        //
                    }
                    break;
                case (eEthernetEventType.LinkUp):
                    if (ethernetEventArgs.EthernetAdapter == EthernetAdapterType.EthernetLANAdapter)
                    {

                    }
                    break;
            }
        }


        //********************************      ZBand Server Operations     ****************************//
        public void GetEndpoints(string parms)
        {
            commMgr.GetAllEndpointswithLimits();

            uint i = 1;
            foreach (var item in commMgr.EndpointList.items)
            {
                if (Debug.ZBandDebugEnabled)
                {
                    ErrorLog.Notice($"Endpoint: {item.name}");
                    ErrorLog.Notice($"Endpoint Now Playing: {item.playingChannel1Number} | {item.playingChannel1Name}");
                    ErrorLog.Notice($"\n");
                }

                endpointEisc.SetSerial(i, item.name);
                endpointEisc.SetSerial(i + endpointNowPlayingNameJoinOffset, item.playingChannel1Name == "N/A" ? "" : item.playingChannel1Name);
                endpointEisc.SetSerial(i + endpointNowPlayingNumberJoinOffset, item.playingChannel1Number);

                i++;
            }
        }

        public void GetChannels(string parms)
        {
            //get all enabled channels
            commMgr.GetAllEnabledChannels();

            //send channel count to eisc
            channelEisc.SetAnalog(1, (ushort)commMgr.ChannelLineUp.items.Count);

            //loop thru channels and print / send to eisc
            uint i = 1;
            foreach (var item in commMgr.ChannelLineUp.items)
            {
                if (Debug.ZBandDebugEnabled)
                {
                    ErrorLog.Notice($"Channel: {item.name}");
                    ErrorLog.Notice($"\n");
                }

                channelEisc.SetSerial(i, item.name); //update UI Slot with Channel Name
                i++;
            }
        }

        public void GetEPG(string parms)
        {
            commMgr.GetEPGFull();

            try
            {
                uint i = 1; //need to look at this. needs to be channel number based indexing
                foreach (var item in commMgr.EPGLineUp.items)
                {
                    if (Debug.ZBandDebugEnabled)
                    {
                        ErrorLog.Notice($"*** EPG Data ***");
                        ErrorLog.Notice($"Channel: {item.name}");
                        ErrorLog.Notice($"Channel ID: {item.id}");
                        ErrorLog.Notice($"Channel number: {item.number}");
                        ErrorLog.Notice($"Program Name: {item.programs[0].name}");
                        ErrorLog.Notice($"Program Episode: {item.programs[0].episodeDescription}");
                        ErrorLog.Notice($"\n");
                    }

                    //send to eisc
                    //uint channelNumber = (uint)item.number; this is not necessary when we sort EPG by channel ID
                    epgEisc.SetSerial(i, item.programs[0].name == "N/A" ? "" : item.programs[0].name);
                    epgEisc.SetSerial(i + 1, item.programs[0].episodeDescription);

                    i += 2;
                }

                //attempt to put a name to the current program showing on each endpoint and send it to EISC
                GetCurrentlyPlayingOnEndpoints();
            }
            catch (Exception e)
            {
                ErrorLog.Error($"Exception in GetEPG in ControlSystem.cs: {e.StackTrace}");
                ErrorLog.Error($"Exception: {e.Message}");
            }

        }

        public void GetCurrentlyPlayingOnEndpoints()
        {
            
            uint i = 0;
            foreach (var item in commMgr.EndpointList.items)
            {
                try
                {
                    //find the channel index for the endpoints currently playing number
                    //Convert Endpoint playingChannel1Number to int
                    bool convertChanSuccess = Int32.TryParse(item.playingChannel1Number, out int searchNum);

                    //if the value is sucessfully converted to int, find the index of the channel with that 'number'
                    if (convertChanSuccess)
                    {
                        //get index in channel list of channel with number == searchNum
                        var chanIndex = commMgr.ChannelLineUp.items.FindIndex(a => a.number.Equals(searchNum));

                        //output to debug if necessary
                        if (Debug.ZBandDebugEnabled)
                        {
                            ErrorLog.Notice($"Endpoint: {item.name}");
                            ErrorLog.Notice($"Endpoint Current Channel: {item.playingChannel1Number}");
                            ErrorLog.Notice($"Searching for current channel number in Channel List...");
                            ErrorLog.Notice($"Found current channel for endpoint {i}:{item.name} at list Index: {chanIndex}");
                            ErrorLog.Notice($"\n");
                        }   

                        //if the chanIndex is not -1, its been found in the list, set serial to that program's name to keep track of current program on each endpoint
                        if (chanIndex != -1)
                        {   //send to EISC
                            endpointEisc.SetSerial(i + endpointNowPlayingProgramName + 1, commMgr.EPGLineUp.items[chanIndex].programs[0].name == "N/A" ? "" : commMgr.EPGLineUp.items[chanIndex].programs[0].name); //use '0' index because we only want whats playing at this second
                        }
                    }
                }
                catch (Exception e)
                {
                    ErrorLog.Error($"Error in GetEPG of ControlSystem.cs: {e.Message}");
                }

                i++;
            }
        }

        private string BuildSetChannelBody( uint endpointNumber, ushort endpointRouteValue)
        {
            string requestBody = "";

            //test if the incoming channel selection falls within the amount of channel on offer currently
            if (endpointRouteValue <= commMgr.ChannelLineUp.items.Count - 1)
            {
                //send the currently selected channel's program info (name) to serial (100 + endpoint number) and the channel name and number to serials endpointNumber + 1 and +2
                endpointEisc.SetSerial(endpointNumber + endpointNowPlayingNameJoinOffset, commMgr.ChannelLineUp.items[endpointRouteValue].name);
                endpointEisc.SetSerial(endpointNumber + endpointNowPlayingNumberJoinOffset, commMgr.ChannelLineUp.items[endpointRouteValue].number.ToString());
                endpointEisc.SetSerial(endpointNumber + endpointNowPlayingProgramName, commMgr.EPGLineUp.items[endpointRouteValue].programs[0].name);

                //debug print
                if (Debug.SystemDebugEnabled)
                    ErrorLog.Notice($"Incoming Route Change. Endpoint: {endpointNumber} | Channel: {endpointRouteValue}");

                //create actionparams for request body json 
                List<ActionParameter> actionparams = new List<ActionParameter>
                {
                    new ActionParameter { name = "Emergency", value = "false"},
                    new ActionParameter { name = "Fullscreen", value = "false"},
                    new ActionParameter { name = "Type", value = commMgr.ChannelLineUp.items[endpointRouteValue].type},
                    new ActionParameter { name = "IP", value = commMgr.ChannelLineUp.items[endpointRouteValue].iPAddress},
                    new ActionParameter { name = "Port", value = commMgr.ChannelLineUp.items[endpointRouteValue].port.ToString()},
                    new ActionParameter { name = "EncryptionType", value = commMgr.ChannelLineUp.items[endpointRouteValue].encryptionType},
                    new ActionParameter { name = "EncryptionKey", value = commMgr.ChannelLineUp.items[endpointRouteValue].encryptionKey ?? ""},
                    new ActionParameter { name = "DecoderIndex", value = "0"},
                    new ActionParameter { name = "Id", value = commMgr.ChannelLineUp.items[endpointRouteValue].id.ToString()} //id is not listed in API as required but it is
                };

                //more params for request body
                List<ActionTarget> targetList = new List<ActionTarget>
                {
                    new ActionTarget { id =commMgr.EndpointList.items[(int)endpointNumber - 1].id, cmdTargetType = "Endpoint" }
                };

                //request body proper (JSON) by creating a JSON object to serialze and attach to the request (body)
                SetChannelJsonObject setChannelRequestBody = new SetChannelJsonObject
                {
                    action = "LoadChannel",
                    actionCluster = "DSS",
                    actionParameters = actionparams,
                    actionTargets = targetList,

                };

                //serialize request body JSON into string
                requestBody = JsonConvert.SerializeObject(setChannelRequestBody);


                if (Debug.ZBandDebugEnabled)
                    ErrorLog.Notice($"Set Channel Request Body: {requestBody}");
            }
            else
            {
                if (Debug.ZBandDebugEnabled) ErrorLog.Warn($"Channel index requested is not within the enabled channel list.");
            }

            //Make request
            return requestBody;

        }

        private void FirePowerEventRequests(uint join)
        {
            if (join >= (uint)PowerEventJoins.AllPowerOn & join <= (uint)PowerEventJoins.PatioPowerOff)
            {
                switch (join)
                {
                    case (uint)PowerEventJoins.AllPowerOn:
                        commMgr.RunEvent(AllPwrOnEventID);
                        break;
                    case (uint)PowerEventJoins.AllPowerOff:
                        commMgr.RunEvent(AllPwrOffEventID);
                        break;
                    case (uint)PowerEventJoins.BarPowerOn:
                        commMgr.RunEvent(BarPwrOnEventID);
                        break;
                    case (uint)PowerEventJoins.BarPowerOff:
                        commMgr.RunEvent(BarPwrOffEventID);
                        break;
                    case (uint)PowerEventJoins.NorthPowerOn:
                        commMgr.RunEvent(NorthPwrOnEventID);
                        break;
                    case (uint)PowerEventJoins.NorthPowerOff:
                        commMgr.RunEvent(NorthPwrOffEventID);
                        break;
                    case (uint)PowerEventJoins.WestPowerOn:
                        commMgr.RunEvent(WestPwrOnEventID);
                        break;
                    case (uint)PowerEventJoins.WestPowerOff:
                        commMgr.RunEvent(WestPwrOffEventID);
                        break;
                    case (uint)PowerEventJoins.SouthPowerOn:
                        commMgr.RunEvent(SouthPwrOnEventID);
                        break;
                    case (uint)PowerEventJoins.SouthPowerOff:
                        commMgr.RunEvent(SouthPwrOffEventID);
                        break;
                    case (uint)PowerEventJoins.PrivatePowerOn:
                        commMgr.RunEvent(PrivatePwrOnEventID);
                        break;
                    case (uint)PowerEventJoins.PrivatePowerOff:
                        commMgr.RunEvent(PrivatePwrOffEventID);
                        break;
                    case (uint)PowerEventJoins.PatioPowerOn:
                        commMgr.RunEvent(PatioPwrOnEventID);
                        break;
                    case (uint)PowerEventJoins.PatioPowerOff:
                        commMgr.RunEvent(PatioPwrOffEventID);
                        break;
                }
            }
        
        }


        //DEPRECATED METHODS
        private string BuildTVControlRequestBody(uint powerJoin)
        {
            string requestBody = "";
            string actionVar;
            uint endpointIndex = 0;

            //test if the incoming channel selection falls within the amount of channel on offer currently
            if (powerJoin >= (uint)PowerJoinsPerDisplay.Bar1PowerOn & powerJoin <= (uint)PowerJoinsPerDisplay.Patio3PowerOff)
            {
                //create actionparams for request body json 
                List<ActionParameter> actionparams = new List<ActionParameter>();


                //set a local var for TVOn or TVOff
                if (powerJoin % 2 == 0) //if the powerJoin is even, then we know its a "TVOn" request
                {
                    actionVar = "TVOn";
                }
                else
                {
                    actionVar = "TVOff";
                }

                //gather the endpoint ID here with a switch statement
                switch (powerJoin)
                {
                    case (uint)PowerJoinsPerDisplay.Bar1PowerOn | (uint)PowerJoinsPerDisplay.Bar1PowerOff:
                        endpointIndex = 0;
                        break;

                    case (uint)PowerJoinsPerDisplay.Bar2PowerOn | (uint)PowerJoinsPerDisplay.Bar2PowerOff:
                        endpointIndex = 1;
                        break;

                    case (uint)PowerJoinsPerDisplay.Bar3PowerOn | (uint)PowerJoinsPerDisplay.Bar3PowerOff:
                        endpointIndex = 2;
                        break;

                    case (uint)PowerJoinsPerDisplay.Bar4PowerOn | (uint)PowerJoinsPerDisplay.Bar4PowerOff:
                        endpointIndex = 3;
                        break;

                    case (uint)PowerJoinsPerDisplay.NorthLounge1PowerOn | (uint)PowerJoinsPerDisplay.NorthLounge1PowerOff:
                        endpointIndex = 4;
                        break;

                    case (uint)PowerJoinsPerDisplay.NorthLounge2PowerOn | (uint)PowerJoinsPerDisplay.NorthLounge2PowerOff:
                        endpointIndex = 5;
                        break;

                    case (uint)PowerJoinsPerDisplay.NorthLounge3PowerOn | (uint)PowerJoinsPerDisplay.NorthLounge3PowerOff:
                        endpointIndex = 6;
                        break;

                    case (uint)PowerJoinsPerDisplay.WestLounge1PowerOn | (uint)PowerJoinsPerDisplay.WestLounge1PowerOff:
                        endpointIndex = 7;
                        break;

                    case (uint)PowerJoinsPerDisplay.WestLounge2PowerOn | (uint)PowerJoinsPerDisplay.WestLounge2PowerOff:
                        endpointIndex = 8;
                        break;

                    case (uint)PowerJoinsPerDisplay.SouthLounge1PowerOn | (uint)PowerJoinsPerDisplay.SouthLounge1PowerOff:
                        endpointIndex = 9;
                        break;

                    case (uint)PowerJoinsPerDisplay.SouthLounge2PowerOn | (uint)PowerJoinsPerDisplay.SouthLounge2PowerOff:
                        endpointIndex = 10;
                        break;

                    case (uint)PowerJoinsPerDisplay.SouthLounge3PowerOn | (uint)PowerJoinsPerDisplay.SouthLounge3PowerOff:
                        endpointIndex = 11;
                        break;

                    case (uint)PowerJoinsPerDisplay.SouthLounge4PowerOn | (uint)PowerJoinsPerDisplay.SouthLounge3PowerOff:
                        endpointIndex = 12;
                        break;

                    case (uint)PowerJoinsPerDisplay.PrivatePowerOn | (uint)PowerJoinsPerDisplay.PrivatePowerOff:
                        endpointIndex = 13;
                        break;

                    case (uint)PowerJoinsPerDisplay.Patio1PowerOn | (uint)PowerJoinsPerDisplay.Patio1PowerOff:
                        endpointIndex = 14;
                        break;

                    case (uint)PowerJoinsPerDisplay.Patio2PowerOn | (uint)PowerJoinsPerDisplay.Patio2PowerOff:
                        endpointIndex = 15;
                        break;

                    case (uint)PowerJoinsPerDisplay.Patio3PowerOn | (uint)PowerJoinsPerDisplay.Patio3PowerOff:
                        endpointIndex = 16;
                        break;
                }


                //more params for request body
                List<ActionTarget> targetList = new List<ActionTarget>
                {
                    new ActionTarget { id =commMgr.EndpointList.items[(int)endpointIndex].id, cmdTargetType = "Endpoint" }
                };

                //request body proper (JSON) by creating a JSON object to serialze and attach to the request (body)
                TVPowerActionObject tvPowerActionRequestBody = new TVPowerActionObject
                {
                    action = actionVar,
                    actionCluster = "DSS",
                    actionParameters = actionparams,
                    actionTargets = targetList,

                };

                //serialize request body JSON into string
                requestBody = JsonConvert.SerializeObject(tvPowerActionRequestBody);


                if (Debug.ZBandDebugEnabled)
                    ErrorLog.Notice($"TV Power Request Body: {requestBody}");
            }
            else
            {
                if (Debug.ZBandDebugEnabled) ErrorLog.Warn($"TV Power request not within join range.");
            }

            //Make request
            return requestBody;

        }

        private string BuildSetChannelByGroupBody(ushort endpointRouteValue, string GroupID)
        {
            string requestBody = "";
            //test if the incoming channel selection falls within the amount of channel on offer currently
            if (endpointRouteValue <= commMgr.ChannelLineUp.items.Count - 1) //needs testing
            {
                //send the currently selected channel's program info (name) to serial (100 + endpoint number) 
                //endpointEisc.SetSerial(endpointNumber + 100, commMgr.EPGLineUp.items[endpointRouteValue].programs[0].name);

                //debug print
                if (Debug.SystemDebugEnabled)
                    CrestronConsole.PrintLine($"Incoming Route Change. Group: {GroupID} | Channel: {endpointRouteValue}");



                //create actionparams for request body json 
                List<ActionParameter> actionparams = new List<ActionParameter>
                {
                    new ActionParameter { name = "Emergency", value = "false"},
                    new ActionParameter { name = "Fullscreen", value = "false"},
                    new ActionParameter { name = "Type", value = commMgr.ChannelLineUp.items[endpointRouteValue].type},
                    new ActionParameter { name = "IP", value = commMgr.ChannelLineUp.items[endpointRouteValue].iPAddress},
                    new ActionParameter { name = "Port", value = commMgr.ChannelLineUp.items[endpointRouteValue].port.ToString()},
                    new ActionParameter { name = "EncryptionType", value = commMgr.ChannelLineUp.items[endpointRouteValue].encryptionType},
                    new ActionParameter { name = "EncryptionKey", value = commMgr.ChannelLineUp.items[endpointRouteValue].encryptionKey ?? ""},
                    new ActionParameter { name = "DecoderIndex", value = "0"},
                };

                //more params for request body
                List<ActionTarget> targetList = new List<ActionTarget>
                {
                    new ActionTarget { id = GroupID, cmdTargetType = "Group" }
                };

                //request body proper (JSON) by creating a JSON object to serialze and attach to the request (body)
                SetChannelJsonObject setChannelRequestBody = new SetChannelJsonObject
                {
                    action = "LoadChannel",
                    actionCluster = "DSS",
                    actionParameters = actionparams,
                    actionTargets = targetList,

                };

                //serialize request body JSON into string
                requestBody = JsonConvert.SerializeObject(setChannelRequestBody);


                if (Debug.ZBandDebugEnabled)
                    CrestronConsole.PrintLine($"Request Body: {requestBody}");
            }
            //Make request
            return requestBody;
        }


        //********************************      EISC Event Handlers     ******************************//
        private void Eisc_event(object sender, EiscEventArgs e)
        {
            if (Debug.SystemDebugEnabled) ErrorLog.Notice($"Eisc Event Fired. Signal: {e.Args.Sig} from IP-ID: {e.ID}");

            //set var _systemReadyForRouting = to the AND of all eisc online signals
            _systemReadyForRouting = endpointEisc.Online & channelEisc.Online & digitalEisc.Online & epgEisc.Online;
            if (Debug.SystemDebugEnabled) ErrorLog.Notice($"All EISCs online. Ready for Channel Routing");

            switch (e.Args.Sig.Type)
            {
                case eSigType.Bool:
                    {
                        if (Debug.SystemDebugEnabled) ErrorLog.Notice($"EISC Digital {e.Args.Sig.Number} changed to {e.Args.Sig.BoolValue}");

                        //run switch to set debugging values
                        switch (e.Args.Sig.Number)
                        {
                            case (uint)Joins.SetSystemDebugging:
                                if (e.Args.Sig.BoolValue)
                                {
                                    SetSystemDebugging("on");
                                }
                                else
                                {
                                    SetSystemDebugging("off");
                                }
                                break;

                            case (uint)Joins.SetZBandDebugging:
                                if (e.Args.Sig.BoolValue)
                                {
                                    SetZBandDebugging("on");
                                }
                                else
                                {
                                    SetZBandDebugging("off");
                                }
                                break;


                            default: //send power joins to function
                                if (e.Args.Sig.BoolValue)
                                {
                                    FirePowerEventRequests(e.Args.Sig.Number); //send join to method to parse and call appropriate api requests
                                    //string requestBody = BuildTVControlRequestBody(e.Args.Sig.Number);
                                    //commMgr.SetPower(requestBody);
                                }
                                break;
                        }

                        //now run switch for all other digitals
                        if (e.Args.Sig.BoolValue) //this stops things from happening when the EISC first connects unless the signals in SimplWindows are high at logic start
                        {
                            uint join = e.Args.Sig.Number;

                            switch (join)
                            {
                                case (uint)Joins.GetChannels:
                                    GetChannels(null);
                                    break;
                                case (uint)Joins.GetEndpoints:
                                    GetEndpoints(null);
                                    break;
                                case (uint)Joins.GetEPG:
                                    GetEPG(null);
                                    break;
                                case (uint)Joins.SystemReadyForRouting:
                                    _systemReadyForRouting = true;
                                    break;
                                default:
                                    break;
                            }
                        }
                        break;

                        
                    }

                case eSigType.UShort:
                    {
                        if (Debug.SystemDebugEnabled) 
                            CrestronConsole.PrintLine($"EISC Analog {e.Args.Sig.Number} changed to {e.Args.Sig.UShortValue}");

                        //check that simpl windows program is ready to start sending valid info, after that start processing anything that comes from it.
                        if (_systemReadyForRouting)
                        {

                            //create local vars for sig passed in. Dont need to test which EISC this came from because only one is sending in analogs
                            uint endpointNumber = e.Args.Sig.Number;
                            ushort endpointRouteValue = e.Args.Sig.UShortValue;

                            //call BuildSetChannelBody method to generate requestBody string
                            string requestBody = BuildSetChannelBody(endpointNumber, endpointRouteValue); //<--try this instead of everything above

                            if (!String.IsNullOrEmpty(requestBody)) //protects against making requests with empty body, i.e. the channel index was outside the range of the channel list
                            {
                                //Make request
                                commMgr.SetChannel(requestBody);
                                //commMgr.RunImmediateEvent(requestBody);

                                //send the currently selected channel number to the feedback side of the EISC for Fb per endpoint
                                //setting this in this way presumes the setchannel action is successful
                                endpointEisc.SetAnalog(endpointNumber, endpointRouteValue);
                            }
                        }

                        break;
                    }
                case eSigType.String:
                    {
                        if (Debug.SystemDebugEnabled)
                            CrestronConsole.PrintLine($"EISC Serial {e.Args.Sig.Number} changed to {e.Args.Sig.StringValue}");

                        //here is a way to send the server IP into the S# program from the SimplWindows Prgm
                        //uint sjoin = e.Args.Sig.Number;
                        //string serverIP = endpointEisc.GetSerial(sjoin);
                        //if (_systemDebug) CrestronConsole.PrintLine($"Incoming Serial Data: {serverIP}");
                        break;
                    }
            }
        }

        
    }
}