using System;
using Crestron.SimplSharp;
using System.Net;
using Newtonsoft.Json;
using TSI.WebRequestUtilities;
using TSI.DebugUtilities;



namespace ZBand_EZTV
{
    public class ZBandServerCommsManager : IZBandServerCommsManager
    {
        //class-wide vars and classes 

        //a class to make generic webrequests
        WebRequests wr = new WebRequests();

        //server vars that will be initialized in the initializer method
        private static string _apiRootPath = "/eztv/api/";
        public static string serverAddressPath; //initialized in the InitializeCommsManager method

        //an object that will hold channel info
        public ChannelJsonObject ChannelLineUp;
        public EndpointJsonObject EndpointList;
        public EpgAllJsonObject EPGLineUp;
        public GroupJsonObject GroupList;

        //eventhandlers
        public event EventHandler<LoginEventArgs> LoginEvent;


        //private prop fields
        private string _serverIP;
        private string _username;
        private string _password;

        //properties
        public string ServerIP
        {
            get { return _serverIP; }
            set { _serverIP = value; }
        }

        public string UserName
        {
            get { return _username; }
            set { _username = value; }
        }

        public string Password
        {
            get { return _password; }
            set { _password = value; }
        }


        //api Token 
        public static string apiToken;

        //logged in status
        public bool isLoggedIn;


        //*************************************************             Init          ****************************************************************//
        public void InitializeCommsManager()
        {
            serverAddressPath = _serverIP + _apiRootPath;

            if (Debug.ZBandDebugEnabled)
            {
                ErrorLog.Notice($"ServerAddressPath: {serverAddressPath}");
                ErrorLog.Notice($"U/P: {_username}:{_password}");
            }

            try
            {
                LoginRequest();
            }
            catch (Exception ex)
            {
                ErrorLog.Error($"Error in InitializeCommsManager: LoginRequest() failed");
            }
        }


        //*************************************************       Login and renew     *******************************************************//
        public void LoginRequest()
        {
            try
            {
                string apiPath = "login";

                //create requestBody for Login reqest
                LoginRequestBody requestBodyObject = new LoginRequestBody
                {
                    username = _username,
                    password = _password
                };

                //serialize requestBody
                string requestBody = JsonConvert.SerializeObject(requestBodyObject);

                if (Debug.SystemDebugEnabled) ErrorLog.Notice($"Login Request Body: {requestBody}");

                //create request
                HttpResponseObject rsp = new HttpResponseObject();
                rsp = wr.CreateWebRequestWithRequestBodyOnly(apiPath, "POST", requestBody);


                //create login event args
                LoginEventArgs args = new LoginEventArgs();


                if (rsp.statusCode != HttpStatusCode.OK)
                {
                    if (Debug.ZBandDebugEnabled) ErrorLog.Error($"Login Unsuccessful!");
                    if (Debug.ErrorLogEnabled) ErrorLog.Error($"Login Unusccessful!");
                    isLoggedIn = false;
                }
                else
                {
                    LoginJsonObject loginRsp = JsonConvert.DeserializeObject<LoginJsonObject>(rsp.responseBody);
                    apiToken = loginRsp.token;

                    if (Debug.SystemDebugEnabled)
                    {
                        ErrorLog.Notice($"Login Success. Token Received.");
                    }

                    isLoggedIn = true;
                }

                args.isLoggedIn = isLoggedIn;
                LoginEvent?.Invoke(this, args);

            }
            catch (WebException we) when (we.Status == WebExceptionStatus.Timeout)
            {
                ErrorLog.Error($"WebException in LoginRequest. The operation timed out");
            }
            catch (Exception e)
            {
                ErrorLog.Error($"Exception in LoginRequest: {e.Message}");
            }
        }

        public void RenewToken()
        {
            try
            {
                HttpResponseObject rsp = new HttpResponseObject();
                rsp = wr.CreateWebRequestWithApiToken("renew", "POST");

                //create login event args
                LoginEventArgs args = new LoginEventArgs();

                //test status code for success
                if (rsp.statusCode != HttpStatusCode.OK)
                {
                    if (Debug.ZBandDebugEnabled) ErrorLog.Error($"Renew Unsucessful.");
                    isLoggedIn = false;
                }
                else
                {
                    LoginJsonObject loginRsp = JsonConvert.DeserializeObject<LoginJsonObject>(rsp.responseBody);
                    apiToken = loginRsp.token;

                    if (Debug.ZBandDebugEnabled) ErrorLog.Notice($"Successfully Renewed Token.");
                    isLoggedIn = true;
                }

                args.isLoggedIn = isLoggedIn;
                LoginEvent?.Invoke(this, args);
            }
            catch (Exception e)
            {
                ErrorLog.Error($"Exception in RenewToken: {e.Message}");
            }
        }

        public bool VerifyToken()
        {
            try
            {
                HttpResponseObject rsp = new HttpResponseObject();
                rsp = wr.CreateWebRequestWithApiToken("verify", "GET");

                //create login event args
                LoginEventArgs args = new LoginEventArgs();

                //test status code for success
                if (rsp.statusCode == HttpStatusCode.Unauthorized)
                {
                    if (Debug.ZBandDebugEnabled) ErrorLog.Error($"Token Expired.");
                    isLoggedIn = false;
                }
                else if (rsp.statusCode == HttpStatusCode.OK)
                {
                    if (Debug.ZBandDebugEnabled) ErrorLog.Notice($"Token Verified.");
                    isLoggedIn = true;
                }

                args.isLoggedIn = isLoggedIn;
                LoginEvent?.Invoke(this, args);
            }
            catch (Exception e)
            {
                ErrorLog.Error($"Exception in VerifyToken: {e.Message}");
            }

            return isLoggedIn;
        }

        public void PreAuthorize()
        {
            if (VerifyToken())
            {
                RenewToken();
            }
            else
            {
                LoginRequest();
            }
        }

        //**************************************************    Endpoint Operations     ****************************************************//

        /// <summary>
        /// Requests a list of all endpoints with a hard limit of 100 (editable). Will only request certain pertenant info such as id, endpointiId, name, playingChannel1Name, playingChannel1Number. Only 'endpointId' is needed for the LoadChannel activity.
        /// </summary>
        public void GetAllEndpointswithLimits()
        {
            try
            {
                //check token and take appropraite action
                PreAuthorize();

                //build out params of request
                string offset = "offset=0";
                string limit = "limit=100";
                string includedFields = "includedFields=id,endpointId,name,playingChannel1Name,playingChannel1Number"; //added last field, not tested

                //construct apiPath with Params
                string apiPath = "endpoints?" + offset + "&" + limit + "&" + includedFields;


                HttpResponseObject rsp = new HttpResponseObject();
                rsp = wr.CreateWebRequestWithApiToken(apiPath, "GET");

                //test status code for success
                if (rsp.statusCode != HttpStatusCode.OK)
                {
                    if (Debug.ZBandDebugEnabled) ErrorLog.Error($"ZBandServerCommsManager:GetAllEndpointswithLimits Request Failed");
                    if (Debug.SystemDebugEnabled) ErrorLog.Error($"ZBandServerCommsManager:GetAllEndpointswithLimits Request Failed with code: {rsp.statusCode}");
                    return;
                }
                else
                {
                    //place data in container for later parsing
                    EndpointList = JsonConvert.DeserializeObject<EndpointJsonObject>(rsp.responseBody);
                }

            }
            catch (Exception e)
            {
                ErrorLog.Error($"Exception in ZBandServerCommsManager:GetAllEndpointswithLimits: {e.Message}");
                ErrorLog.Error($"Exception in ZBandServerCommsManager:GetAllEndpointswithLimits: {e.Message}");
            }
        }

        /// <summary>
        /// Sends request to change channel of endpoint defined inside requestbody
        /// </summary>
        /// <param name="requestBody">JSON payload containing info about the channel desired and the targets of the action</param>
        public void SetChannel(string requestBody)
        {
            try
            {
                //check token and take appropraite action
                PreAuthorize();

                //construct apiPath with Params
                string apiPath = "events/immediate/activities";

                //create request
                HttpResponseObject rsp = new HttpResponseObject();
                rsp = wr.CreateWebRequestWithApiTokenandRequestBody(apiPath, "POST", requestBody);

                //test status code for success
                if (rsp.statusCode != HttpStatusCode.Created)
                {
                    ErrorLog.Error($"Set Channel Request Failed with Status Code: {rsp.statusCode}");
                    if (Debug.ErrorLogEnabled) ErrorLog.Warn($"Set Channel Request Failed. Status Code: {rsp.statusCode}");
                    PreAuthorize();
                    return;
                }
                else
                {
                    if (Debug.ZBandDebugEnabled)
                    {
                        ErrorLog.Notice($"Successfully sent Set Channel request. Response: {rsp.responseBody}");
                        if (Debug.ErrorLogEnabled) ErrorLog.Notice($"Successfully sent Set Channel request. Repsonse: {rsp.responseBody}");
                    }
                }
            }
            catch (Exception e)
            {
                ErrorLog.Error($"Exception in SetChannel: {e.Message}");
                if (Debug.ErrorLogEnabled) ErrorLog.Error($"Exception in SetChannel: {e.Message}");
            }
        }

        /// <summary>
        /// Send request to send Power On/Off command to endpoint defined inside request body
        /// </summary>
        /// <param name="requestBody"></param>
        public void RunImmediateEvent(string requestBody)
        {
            try
            {
                //check token and take appropraite action
                PreAuthorize();

                //construct apiPath with Params
                string apiPath = "events/immediate/activities";

                //create request
                HttpResponseObject rsp = new HttpResponseObject();
                rsp = wr.CreateWebRequestWithApiTokenandRequestBody(apiPath, "POST", requestBody);

                //test status code for success
                if (rsp.statusCode != HttpStatusCode.Created)
                {
                    ErrorLog.Error($"Endpoint TV Power Request Failed with Status Code: {rsp.statusCode}");
                    if (Debug.ErrorLogEnabled) ErrorLog.Warn($"Endpoint TV Power Request Failed. Status Code: {rsp.statusCode}");
                    PreAuthorize();
                    return;
                }
                else
                {
                    if (Debug.ZBandDebugEnabled)
                    {
                        ErrorLog.Notice($"Successfully sent Set Endpoint TV Power request. Response: {rsp.responseBody}");
                        if (Debug.ErrorLogEnabled) ErrorLog.Notice($"Successfully sent Set Endpoint TV Power request. Repsonse: {rsp.responseBody}");
                    }
                }
            }
            catch (Exception e)
            {
                ErrorLog.Error($"Exception in SetPower: {e.Message}");
                if (Debug.ErrorLogEnabled) ErrorLog.Error($"Exception in SetPower: {e.Message}");
            }
        }

        public void RunEvent(string eventID)
        {
            try
            {
                //check token and take appropraite action
                PreAuthorize();

                //construct apiPath with Params
                string apiPath = String.Format($"events/{eventID}/start_now");

                //create request
                HttpResponseObject rsp = new HttpResponseObject();
                rsp = wr.CreateWebRequestWithApiToken(apiPath, "POST");

                //test status code for success
                if (rsp.statusCode != HttpStatusCode.OK)
                {
                    ErrorLog.Error($"EventCallbyID Request Failed with Status Code: {rsp.statusCode}");
                    if (Debug.ErrorLogEnabled) ErrorLog.Warn($"EventCallbyID Request Failed. Status Code: {rsp.statusCode}");
                    PreAuthorize();
                    return;
                }
                else
                {
                    if (Debug.ZBandDebugEnabled)
                    {
                        ErrorLog.Notice($"Successfully sent EventCallbyID request. Response: {rsp.responseBody}");
                        if (Debug.ErrorLogEnabled) ErrorLog.Notice($"Successfully sent EventCallbyID request. Repsonse: {rsp.responseBody}");
                    }
                }
            }
            catch (Exception e)
            {
                ErrorLog.Error($"Exception in RunEvent: {e.Message}");
                if (Debug.ErrorLogEnabled) ErrorLog.Error($"Exception in RunEvent: {e.Message}");
            }
        }

        //**************************************************    Channel Operations      ***************************************************//
        /// <summary>
        /// Requests a list of all enabled channels. Hard limit of 100 (editable). Only requests info needed for making LoadChannel requests
        /// </summary>
        public void GetAllEnabledChannels()
        {
            //check token and take appropriate action
            PreAuthorize();

            //build out params of request
            string offset = "offset=0";
            string limit = "limit=100";
            string targetuseragent = "targetuseragent=PC_Mac";
            string filter = "filter=enabled::true";
            string includedFields = "includedFields=id,number,name,description,iPAddress,port,type,encryptionType,encryptionKey,encryptionKeyType";

            //construct apiPath with Params
            string apiPath = "channels?" + offset + "&" + limit + "&" + targetuseragent + "&" + filter + "&" + includedFields;

            try
            {
                //create request 
                HttpResponseObject rsp = new HttpResponseObject();
                rsp = wr.CreateWebRequestWithApiToken(apiPath, "GET");

                //check status code
                if (rsp.statusCode != HttpStatusCode.OK)
                {
                    if (Debug.ZBandDebugEnabled) ErrorLog.Error($"GetAllEnabledChannel Request Failed");

                    if (Debug.ErrorLogEnabled) ErrorLog.Error($"ZBandServerCommsManager:GetAllEnabledChannel Request Failed with code: {rsp.statusCode}");
                    return;
                }
                else
                {
                    //place data in a container for later parsing
                    ChannelLineUp = JsonConvert.DeserializeObject<ChannelJsonObject>(rsp.responseBody);
                }
            }
            catch (Exception e)
            {
                ErrorLog.Error($"Exception in ZBandServerCommsManager:GetAllEnabledChannels: {e.Message}");
                if (Debug.ErrorLogEnabled) ErrorLog.Error($"Exception in ZBandServerCommsManager:GetAllEnabledChannels: {e.Message}");
            }

        }

        //**************************************************    EPG Operations      *******************************************************//
        /// <summary>
        /// Requests EPG Data for all "allowed" channels for now to 15 min from now. 
        /// </summary>
        public void GetEPGFull()
        {
            //check token and take appropriate action
            PreAuthorize();
            
            //get current DateTime and 15 min from now as well
            DateTime now = DateTime.UtcNow; //this api requires time in UTC for some reason
            string nowString = now.ToString("yyyy-MM-ddTHH:mm:ss");

            DateTime fifteenMinFromNow = now.AddMinutes(15);
            string fifteenMinFromNowString = fifteenMinFromNow.ToString("yyyy-MM-ddTHH:mm:ss");

            //set other request params
            string offset = "offset=0";
            string limit = "limit=100"; //ZBAND IS LIMITED TO 100 CHANNELS
            string targetuseragent = "targetuseragent=PC_Mac";
            string sort = "sort=id"; //sort EPG data by Channel to keep the list in an order we expect THIS IS CRITICAL AS IT SORTS THE LIST SUCH THAT THE ORDER IS ALWAYS MAINTAINED FOR UIs
            string filter = "filter=enabled::true"; //only request info for channels that are enabled THIS IS CRITICAL AS IT OMITS DISABLED CHANNELS

            //create an api path with the time frame included
            string apiPath = "epg?" + offset + "&" + limit + "&from=" + nowString + "&to=" + fifteenMinFromNowString + "&" + targetuseragent + "&" + sort + "&" + filter;

            
            try
            {
                //create request 
                HttpResponseObject rsp = new HttpResponseObject();
                rsp = wr.CreateWebRequestWithApiToken(apiPath, "GET");

                //check status code
                if (rsp.statusCode != HttpStatusCode.OK)
                {
                    if (Debug.ZBandDebugEnabled) ErrorLog.Error($"GetEPGFull Request Failed");
                    if (Debug.ErrorLogEnabled) ErrorLog.Error($"ZBandServerCommsManager:GetEPGFull Request Failed with code:  {rsp.statusCode}");
                    return;
                }
                else
                {
                    if (Debug.ZBandDebugEnabled)
                    {
                        ErrorLog.Notice($"GetEPGFull Request Success");
                        //ErrorLog.Notice($"EPG Data: {rsp.responseBody}"); //uncomment this only for debugging purposes. In production, this will be a super large chunk of data
                    }

                    //place data in container for later parsing
                    EPGLineUp = JsonConvert.DeserializeObject<EpgAllJsonObject>(rsp.responseBody);
                    
                }
            }
            catch (Exception ex)
            {
                ErrorLog.Error($"Exception in ZBandServerCommsManager:GetEPGFull: {ex.Message}");
                if (Debug.ErrorLogEnabled) ErrorLog.Error($"Exception in ZBandServerCommsManager:GetEPGFull: {ex.Message}");
            }

        }

        //**************************************************    Groups Operations      *******************************************************//
        /// <summary>
        /// Requests List of Groups on the server
        /// </summary>
        public void GetAllGroups()
        {
            //check token and take appropriate action
            PreAuthorize();

            //build out params of request
            string offset = "offset=0";
            string limit = "limit=20";

            //construct apiPath with Params
            string apiPath = "groups?" + offset + "&" + limit;

            try
            {
                //create request 
                HttpResponseObject rsp = new HttpResponseObject();
                rsp = wr.CreateWebRequestWithApiToken(apiPath, "GET");

                //check status code
                if (rsp.statusCode != HttpStatusCode.OK)
                {
                    if (Debug.ZBandDebugEnabled) ErrorLog.Error($"GetAllGroups Request Failed");

                    if (Debug.ErrorLogEnabled) ErrorLog.Error($"ZBandServerCommsManager:GetAllGroups Request Failed with code: {rsp.statusCode}");
                    return;
                }
                else
                {
                    //place data in a container for later parsing
                    GroupList = JsonConvert.DeserializeObject<GroupJsonObject>(rsp.responseBody);
                }
            }
            catch (Exception e)
            {
                CrestronConsole.PrintLine($"Exception in ZBandServerCommsManager:GetAllGroups: {e.Message}");
                if (Debug.ErrorLogEnabled) ErrorLog.Error($"Exception in ZBandServerCommsManager:GetAllGroups: {e.Message}");
            }
        }

    }
}
