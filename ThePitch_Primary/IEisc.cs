using System;
using ThePitch_Primary;

namespace TSI.HelperClasses
{
    public interface IEisc
    {
        bool Online { get; }

        event EventHandler<EiscEventArgs> _eiscEvent;

        void CheckControlSystemType(ControlSystem cs);
        ushort GetAnalog(uint Join);
        bool GetDigital(uint Join);
        string GetSerial(uint Join);
        void SetAnalog(uint Join, ushort Value);
        void SetDigital(uint Join, bool value);
        void SetSerial(uint Join, string Value);
    }
}