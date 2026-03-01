using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using System.Collections.Specialized;
using System.Net;

namespace ValSharp;

public static class HttpUtils
{
    internal static ILogger? _logger;

    public static IRestResponse? SendRequest(string url, RequestMethod method = RequestMethod.GET, NameValueCollection? headers = null, object? body = null,
                                            NameValueCollection? cookies = null, bool followRedirects = true, HttpStatusCode? acceptOnly = null)
    {
        try
        {
            var _method = Map(method);

            RestClient client = new RestClient(url);
            RestRequest request = new RestRequest(_method);
            
            client.RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;

            if (headers != null)
            {
                foreach (string key in headers)
                {
                    var header = headers.Get(key);

                    if (header != null)
                        request.AddHeader(key, header);
                }
            }

            if (cookies != null)
            {
                client.CookieContainer = new CookieContainer();
                foreach (string cookie in cookies)
                {
                    if (cookie != null)
                        client.CookieContainer.Add(new Cookie(cookie, cookies.Get(cookie)));
                }
            }

            if (body != null)
            {
                if (body is string str)
                    request.AddBody(str);
                else
                    request.AddJsonBody(body);
            }

            client.FollowRedirects = followRedirects;

            var response = client.Execute(request, _method);

            if (acceptOnly != null && response.StatusCode != acceptOnly)
                return null;

            if (!response.IsSuccessful)
                return null;

            return response;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, string.Empty);
            return new RestResponse() { ErrorException = ex };
        }
    }

    public static TResult? SendRequest<TResult>(string url, RequestMethod method = RequestMethod.GET, NameValueCollection? headers = null, object? body = null,
                                        NameValueCollection? cookies = null, bool followRedirects = true, HttpStatusCode? acceptOnly = null, string? innerObj = null)
        where TResult : class
    {
        try
        {
            var response = SendRequest(url, method, headers, body, cookies, followRedirects, acceptOnly);

            if (response == null)
                return default;

            return JsonConvert.DeserializeObject<TResult>(response.Content);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, string.Empty);
            return null;
        }
    }

    private static Method Map(RequestMethod method)
    {
        switch (method)
        {
            case RequestMethod.GET:
                return Method.GET;

            case RequestMethod.POST:
                return Method.POST;

            case RequestMethod.PUT:
                return Method.PUT;

            case RequestMethod.OPTIONS:
                return Method.OPTIONS;

            case RequestMethod.DELETE:
                return Method.DELETE;
        }

        return Method.GET;
    }
}

public enum RequestMethod
{
    GET,
    POST,
    PUT,
    DELETE,
    OPTIONS
}
