namespace TSI.WebRequestUtilities
{
    public interface IWebRequests
    {
        HttpResponseObject CreateWebRequestWithApiToken(string apiPath, string requestMethod);
        HttpResponseObject CreateWebRequestWithApiTokenandRequestBody(string apiPath, string requestMethod, string requestBody);
        HttpResponseObject CreateWebRequestWithRequestBodyOnly(string apiPath, string requestMethod, string requestBody);
    }
}