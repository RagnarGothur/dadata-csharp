using Dadata.Model;
using RequestLimiter;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace Dadata
{
    public abstract class ClientBase
    {
        protected const uint defaultMaxRequestsPerSecond = 20;
        protected IRequestExecutor requestExecutor
        { get; set; }
        protected uint maxRequestsPerSecond
        { get; set; }
        protected string token
        { get; set; }
        protected string baseUrl
        { get; set; }
        protected JsonSerializer serializer
        { get; set; }

        static ClientBase()
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls;
        }

        public ClientBase(string token, string baseUrl, uint maxRequestsPerSecond = defaultMaxRequestsPerSecond, IRequestExecutor requestExecutor = null)
        {
            this.token = token;
            this.baseUrl = baseUrl;
            this.maxRequestsPerSecond = maxRequestsPerSecond;

            if (requestExecutor == null)
            {
                requestExecutor = new DefaultExecutor();
            }

            this.requestExecutor = requestExecutor;
            this.serializer = new JsonSerializer();
        }

        protected async Task<T> ExecuteGet<T>(string method, string entity, NameValueCollection parameters)
        {
            var queryString = SerializeParameters(parameters);
            var httpRequest = CreateHttpRequest(verb: "GET", method: method, entity: entity, queryString: queryString);
            var httpResponse = this.requestExecutor.ExecuteWithMaxReqPerSecond(httpRequest, this.maxRequestsPerSecond);
            return await Deserialize<T>((HttpWebResponse)httpResponse);
        }

        protected async Task<T> ExecutePost<T>(string method, string entity, IDadataRequest request)
        {
            var httpRequest = CreateHttpRequest(verb: "POST", method: method, entity: entity);
            httpRequest = SerializeRequest(httpRequest, request);
            var httpResponse = this.requestExecutor.ExecuteWithMaxReqPerSecond(httpRequest, this.maxRequestsPerSecond);
            return await Deserialize<T>((HttpWebResponse)httpResponse);
        }

        protected HttpWebRequest CreateHttpRequest(string verb, string method, string entity, string queryString = null)
        {
            var url = String.Format("{0}/{1}/{2}", baseUrl, method, entity);
            if (queryString != null)
            {
                url += "?" + queryString;
            }
            return CreateHttpRequest(verb, url);
        }

        protected HttpWebRequest CreateHttpRequest(string verb, string url)
        {
            var request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = verb;
            request.ContentType = "application/json";
            request.Headers.Add("Authorization", "Token " + this.token);
            return request;
        }

        protected string SerializeParameters(NameValueCollection parameters)
        {
            List<string> parts = new List<string>();
            foreach (String key in parameters.AllKeys)
                parts.Add(String.Format("{0}={1}", key, parameters[key]));
            return String.Join("&", parts);
        }

        protected HttpWebRequest SerializeRequest(HttpWebRequest httpRequest, IDadataRequest request)
        {
            using (var w = new StreamWriter(httpRequest.GetRequestStream()))
            {
                using (JsonWriter writer = new JsonTextWriter(w))
                {
                    this.serializer.Serialize(writer, request);
                }
            }
            return httpRequest;
        }

        protected virtual async Task<T> Deserialize<T>(HttpWebResponse httpResponse)
        {
            using (var r = new StreamReader(httpResponse.GetResponseStream()))
            {
                string responseText = await r.ReadToEndAsync();
                return JsonConvert.DeserializeObject<T>(responseText);
            }
        }
    }
}
