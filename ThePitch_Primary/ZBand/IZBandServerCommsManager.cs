using System;

namespace ZBand_EZTV
{
    public interface IZBandServerCommsManager
    {
        string Password { get; set; }
        string ServerIP { get; set; }
        string UserName { get; set; }

        event EventHandler<LoginEventArgs> LoginEvent;

        void GetAllEnabledChannels();
        void GetAllEndpointswithLimits();
        void GetAllGroups();
        void GetEPGFull();
        void InitializeCommsManager();
        void LoginRequest();
        void PreAuthorize();
        void RenewToken();
        void SetChannel(string requestBody);
        bool VerifyToken();
    }
}