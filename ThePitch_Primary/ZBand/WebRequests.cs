using System.IO;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using ZBand_EZTV;

namespace TSI.WebRequestUtilities
{
    public class WebRequests : IWebRequests
    {
        public HttpResponseObject CreateWebRequestWithApiToken(string apiPath, string requestMethod)
        {
            HttpWebRequest request = WebRequest.Create("https://" + ZBandServerCommsManager.serverAddressPath + apiPath) as HttpWebRequest;
            request.Proxy = null;
            request.Method = requestMethod;
            request.ContentLength = 0;
            request.Headers.Add("Authorization", "Bearer " + ZBandServerCommsManager.apiToken);
            request.Accept = "*/*";


            //allows for validation of SSL certificates 
            ServicePointManager.ServerCertificateValidationCallback += new System.Net.Security.RemoteCertificateValidationCallback(ValidateServerCertificate);

            HttpWebResponse response = request.GetResponse() as HttpWebResponse;
            Stream dataStream = response.GetResponseStream();
            StreamReader reader = new StreamReader(dataStream);
            string responseFromServer = reader.ReadToEnd();

            HttpResponseObject rsp = new HttpResponseObject
            {
                responseBody = responseFromServer,
                statusCode = response.StatusCode
            };

            reader.Close();
            dataStream.Close();
            response.Close();

            return rsp;

        }

        public HttpResponseObject CreateWebRequestWithApiTokenandRequestBody(string apiPath, string requestMethod, string requestBody)
        {
            HttpWebRequest request = WebRequest.Create("https://" + ZBandServerCommsManager.serverAddressPath + apiPath) as HttpWebRequest;
            request.Proxy = null;
            request.Method = requestMethod;
            request.Headers.Add("Authorization", "Bearer " + ZBandServerCommsManager.apiToken);
            request.Accept = "*/*";

            //write request body
            ASCIIEncoding encoding = new ASCIIEncoding();
            byte[] reqByte = encoding.GetBytes(requestBody);
            request.ContentLength = reqByte.Length;
            request.ContentType = "application/json";
            Stream newStream = request.GetRequestStream();
            newStream.Write(reqByte, 0, reqByte.Length);
            newStream.Close();



            //allows for validation of SSL certificates 
            ServicePointManager.ServerCertificateValidationCallback += new System.Net.Security.RemoteCertificateValidationCallback(ValidateServerCertificate);

            HttpWebResponse response = request.GetResponse() as HttpWebResponse;
            Stream dataStream = response.GetResponseStream();
            StreamReader reader = new StreamReader(dataStream);
            string responseFromServer = reader.ReadToEnd();

            HttpResponseObject rsp = new HttpResponseObject
            {
                responseBody = responseFromServer,
                statusCode = response.StatusCode
            };

            reader.Close();
            dataStream.Close();
            response.Close();

            return rsp;

        }

        public HttpResponseObject CreateWebRequestWithRequestBodyOnly(string apiPath, string requestMethod, string requestBody)
        {
            HttpWebRequest request = WebRequest.Create("https://" + ZBandServerCommsManager.serverAddressPath + apiPath) as HttpWebRequest;
            request.Proxy = null;
            request.Method = requestMethod;
            //request.Headers.Add("Authorization", "Bearer " + ZBandServerCommsManager.apiToken);
            request.Accept = "*/*";

            //write request body
            ASCIIEncoding encoding = new ASCIIEncoding();
            byte[] reqByte = encoding.GetBytes(requestBody);
            request.ContentLength = reqByte.Length;
            request.ContentType = "application/json";
            Stream newStream = request.GetRequestStream();
            newStream.Write(reqByte, 0, reqByte.Length);
            newStream.Close();

            //allows for validation of SSL certificates 
            ServicePointManager.ServerCertificateValidationCallback += new System.Net.Security.RemoteCertificateValidationCallback(ValidateServerCertificate);

            HttpWebResponse response = request.GetResponse() as HttpWebResponse;
            Stream dataStream = response.GetResponseStream();
            StreamReader reader = new StreamReader(dataStream);
            string responseFromServer = reader.ReadToEnd();

            HttpResponseObject rsp = new HttpResponseObject
            {
                responseBody = responseFromServer,
                statusCode = response.StatusCode
            };

            reader.Close();
            dataStream.Close();
            response.Close();

            return rsp;

        }


        public static bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }

    }

    public class HttpResponseObject
    {
        public string responseBody { get; set; }
        public HttpStatusCode statusCode { get; set; } 
    }


        
}
