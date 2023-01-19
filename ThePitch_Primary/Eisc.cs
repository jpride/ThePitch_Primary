using System;
using Crestron.SimplSharp;
using Crestron.SimplSharpPro;
using Crestron.SimplSharp.CrestronIO;
using Crestron.SimplSharpPro.DeviceSupport;
using Crestron.SimplSharpPro.EthernetCommunication;
using ThePitch_Primary;

namespace TSI.HelperClasses
{
    public class Eisc : IEisc
    {

        private uint _ID;
        private string _ipAddress;
        private bool controlSystemIsVirtual = false;

        //use EthernetIntersystemCommunications for 4 series appliances, but not VC4
        //EthernetIntersystemCommunications _eisc;

        //use EISCServer for VC4 platforms
       readonly EISCServer _eisc;

        public bool Online
        {
            get { return _eisc.IsOnline; }
        }

        public event EventHandler<EiscEventArgs> _eiscEvent;
        

        public Eisc(uint ID, string IPaddress, ControlSystem cs, string Description)
        {
            //4 Series Appliances
            //_eisc = new EthernetIntersystemCommunications(ID, IPaddress, cs);

            //VC4
            _eisc = new EISCServer(ID, cs);


            _eisc.Description = Description;

            _eisc.Register();
            _eisc.OnlineStatusChange += eisc_OnlineStatusChange;
            _eisc.SigChange += eisc_SigChange;

            if (_eisc.IsOnline) CrestronConsole.PrintLine($"{Description}(EISC): Online");



            _ID = ID; //store the ID we were set to
            _ipAddress = IPaddress; //store the IPaddress we used

            CrestronConsole.PrintLine($"EISC Created: {_ID} @ {_ipAddress}");
        }

        //not used here but should explore its potential later
        public void CheckControlSystemType(ControlSystem cs)
        {
            string rootDir = Directory.GetApplicationRootDirectory();

            if (String.IsNullOrEmpty(rootDir))
            {
                controlSystemIsVirtual = true;
            }
        }




        public void SetDigital(uint Join, bool value)
        {
            _eisc.BooleanInput[Join].BoolValue = value;
        }

        public bool GetDigital(uint Join)
        {
            return _eisc.BooleanOutput[Join].BoolValue;
        }

        public void SetAnalog(uint Join, ushort Value)
        {
            _eisc.UShortInput[Join].UShortValue = Value;
        }

        public ushort GetAnalog(uint Join)
        {
            return _eisc.UShortOutput[Join].UShortValue;
        }

        public void SetSerial(uint Join, string Value)
        {
            _eisc.StringInput[Join].StringValue = Value;
        }
        public string GetSerial(uint Join)
        {
            return _eisc.StringOutput[Join].StringValue;
        }




        private void eisc_SigChange(BasicTriList currentDevice, SigEventArgs args)
        {
            OnRaiseEvent(new EiscEventArgs("Signal", args));
        }

        private void eisc_OnlineStatusChange(GenericBase currentDevice, OnlineOfflineEventArgs args)
        {
            CrestronConsole.PrintLine($"EISC is {(args.DeviceOnLine ? "online" : "Offline")}");
        }

        protected virtual void OnRaiseEvent(EiscEventArgs e)
        {
            //EventHandler<EiscEventArgs> raiseEvent = _eiscEvent;

            if (_eiscEvent != null)
            {
                e.Online = Online;
                e.ID = _ID;
                e.IpAddress = _ipAddress;

                _eiscEvent(this, e);
            }
        }
    }

    public class EiscEventArgs
    {
        public string Message { get; set; }

        public SigEventArgs Args { get; set; }

        public bool Online { get; set; }

        public uint ID { get; set; }

        public string IpAddress { get; set; }


        public EiscEventArgs(string message)
        {
            Message = message;
        }

        public EiscEventArgs(string message, SigEventArgs args)
        {
            Message = message;
            Args = args;
        }
    }
}
