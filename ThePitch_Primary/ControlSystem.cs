using System;
using Crestron.SimplSharp;                          	// For Basic SIMPL# Classes
using Crestron.SimplSharpPro;                       	// For Basic SIMPL#Pro classes
using System.Threading;
using TSI.HelperClasses;
using ZBand_EZTV;
using System.Collections.Generic;
using Newtonsoft.Json;
using TSI.DebugUtilities;

namespace ThePitch_Primary //CHANGE EISCS BEFORE DEPLOYMENT
{
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
        }


        //Zband Server Class Init and field setting
        ZBandServerCommsManager commMgr;

        private string ZBand_Username = "admin@dss-internal";
        private string ZBand_Password = "1qaz!QAZ";
        private string ZBand_ServerIP = "10.14.1.10";

        

        //********************************      Control System and Init         ******************************//
        public ControlSystem()
            : base()
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
                        CrestronConsole.PrintLine("Creating program thread");

                        //set important vars for commMgr
                        commMgr.UserName = ZBand_Username;
                        commMgr.Password = ZBand_Password;
                        commMgr.ServerIP = ZBand_ServerIP;

                        //subscribe to LoginEvents
                        commMgr.LoginEvent += CommMgr_LoginEvent;

                        //initialize commMgr
                        commMgr.InitializeCommsManager();

                        //create console commands for testing
                        CrestronConsole.AddNewConsoleCommand(
                            GetEndpoints,
                            "GetEndpoints",
                            "Requests List of Endpoints from Zband Server",
                            ConsoleAccessLevelEnum.AccessOperator);

                        CrestronConsole.AddNewConsoleCommand(
                            GetChannels,
                            "GetChannels",
                            "Requests List of Enabled Channels from Zband Server",
                            ConsoleAccessLevelEnum.AccessOperator);

                        CrestronConsole.AddNewConsoleCommand(
                            GetEPG,
                            "GetEPG",
                            "Requests List of Current Shows for Enabled Channels from Zband Server",
                            ConsoleAccessLevelEnum.AccessOperator);
                    });

                programThread.Start();

                //Get Server data at startup
                commMgr.GetAllEndpointswithLimits();
                commMgr.GetAllEnabledChannels();
                commMgr.GetEPGFull();
                
            }
            catch (Exception e)
            {
                ErrorLog.Error("Error in InitializeSystem: {0}", e.Message);
            }
        }

        //method to set digital EISC for LoggedIn status
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
                if (Debug.SystemDebugEnabled)
                {
                    CrestronConsole.PrintLine($"Endpoint: {item.name}");
                    CrestronConsole.PrintLine($"Endpoint Now Playing: {item.playingChannel1Number} | {item.playingChannel1Name}");
                }

                endpointEisc.SetSerial(i, item.name);
                endpointEisc.SetSerial(i + 1, item.playingChannel1Name);
                endpointEisc.SetSerial(i + 2, item.playingChannel1Number);

                i+=4;
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
                if (Debug.SystemDebugEnabled) CrestronConsole.PrintLine($"Channel: {item.name}");
                channelEisc.SetSerial(i, item.name);
                i++;
            }
        }

        public void GetEPG(string parms)
        {
            commMgr.GetEPGFull();

            try
            {
                uint i = 1;
                foreach (var item in commMgr.EPGLineUp.items)
                {
                    if (Debug.SystemDebugEnabled)
                    {
                        CrestronConsole.PrintLine($"Channel: {item.name}");
                        CrestronConsole.PrintLine($"Program Name: {item.programs[0].name}");
                        CrestronConsole.PrintLine($"Program Episode: {item.programs[0].episodeDescription}");
                    }

                    //send to eisc
                    epgEisc.SetSerial(i, item.programs[0].name);
                    epgEisc.SetSerial(i + 1, item.programs[0].episodeDescription);

                    i += 2;

                }
            }
            catch (Exception e)
            {
                CrestronConsole.PrintLine($"Exception in GetEPG in ControlSystem.cs: {e.StackTrace}");
                CrestronConsole.PrintLine($"Exception: {e.Message}");
            }

        }


        //********************************      EISC Event Handlers     ******************************//
        private void Eisc_event(object sender, EiscEventArgs e)
        {
            if (Debug.SystemDebugEnabled) CrestronConsole.PrintLine($"Eisc Event Fired {e.Message}");

            switch (e.Args.Sig.Type)
            {
                case eSigType.Bool:
                    {
                        if (Debug.SystemDebugEnabled) CrestronConsole.PrintLine($"EISC Digital {e.Args.Sig.Number} changed to {e.Args.Sig.BoolValue}");
                        
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

                            //send the currently selected channel number to the feedback side of the EISC for Fb per endpoint
                            endpointEisc.SetAnalog(endpointNumber, endpointRouteValue);


                            //test if the incoming channel selection falls within the amount of channel on offer currently
                            if (endpointRouteValue <= commMgr.ChannelLineUp.items.Count - 1) //needs testing
                            {
                                //send the currently selected channel's program info (name) to serial (100 + endpoint number) 
                                endpointEisc.SetSerial(endpointNumber + 100, commMgr.EPGLineUp.items[endpointRouteValue].programs[0].name);

                                //debug print
                                if (Debug.SystemDebugEnabled)
                                    CrestronConsole.PrintLine($"Incoming Route Change. Endpoint: {endpointNumber} | Channel: {endpointRouteValue}");



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
                                var requestBody = JsonConvert.SerializeObject(setChannelRequestBody);


                                if (Debug.ZBandDebugEnabled)
                                    CrestronConsole.PrintLine($"Request Body: {requestBody}");

                                //Make request
                                commMgr.SetChannel(requestBody);
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