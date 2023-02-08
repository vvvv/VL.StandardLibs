using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using System.Net;
using NetWebRequest = System.Net.WebRequest;
using VL.Core;
using VL.Lib.Basics.Resources;
using VL.Lib.Collections;

namespace VL.Lib.IO.WebRequest
{
    public static class Webrequest
    {
        public static HttpWebRequest AddHeaders(this NetWebRequest webRequest, IEnumerable<string> headers)
        {
            var httpWebRequest = (HttpWebRequest)webRequest;

            foreach (var header in headers)
                httpWebRequest.Headers.Add(header);

            return httpWebRequest;
        }

        public static HttpWebRequest SetProxy(this NetWebRequest webRequest, ICredentials credentials, string uri, int port = 8080)
        {
            var httpWebRequest = (HttpWebRequest)webRequest;
            Uri theUri;
            IPAddress ipAddress;
            if (Uri.TryCreate(uri, UriKind.Absolute, out theUri) || IPAddress.TryParse(uri, out ipAddress))
            {
                httpWebRequest.Proxy = new WebProxy(uri, port);
                httpWebRequest.Proxy.Credentials = credentials;
            }
            return httpWebRequest;
        }

        public static IResourceProvider<Stream> GetRequestStream(this NetWebRequest webRequest)
        {
            return ResourceProvider.New(() => { return (webRequest == null) ? Stream.Null : webRequest.GetRequestStream(); });
        }

        public static IResourceProvider<WebResponse> GetResponse(this NetWebRequest webRequest)
        {
            return ResourceProvider.New(() => { return (webRequest == null) ? null : webRequest.GetResponse(); }).ShareSerially(200, null);
        }

        public static IResourceProvider<HttpWebResponse> GetHttpResponse(this NetWebRequest webRequest, out string status)
        {
            //http-status needs to be returned here since it is part of the WebException

            IResourceProvider<HttpWebResponse> result = null;
            try
            {
                result = ResourceProvider.New(() => { return (webRequest == null) ? null : (HttpWebResponse)webRequest.GetResponse(); }).ShareSerially(200, null);
                using (var h = result.GetHandle())
                {
                    var httpWebResponse = (HttpWebResponse)h.Resource;
                    status = httpWebResponse.StatusDescription;
                }
                return result;
            }
            catch (WebException e)
            {
                var httpWebResponse = ((HttpWebResponse)e.Response);
                if (httpWebResponse != null)
                    status = httpWebResponse.StatusDescription;
                else
                    status = e.Message;

                return null;
            }
        }

        public static IResourceProvider<Stream> GetResponseStream(this IResourceProvider<WebResponse> webResponse)
        {
            return webResponse.BindNew(wr => { return (wr == null) ? null : wr.GetResponseStream(); }).ShareSerially(200, null);
        }

        public static IResourceProvider<WebResponse> GetHeaders(this IResourceProvider<WebResponse> webResponse, out Spread<string> headers)
        {
            var sb = new SpreadBuilder<string>();
            if (webResponse != null)
                using (var h = webResponse.GetHandle())
                foreach (var k in h.Resource.Headers.Keys)
                    foreach (var v in h.Resource.Headers.GetValues(k.ToString()))
                        sb.Add(k.ToString()+","+v);

            headers = sb.ToSpread();
            return webResponse;
        }

        public static IResourceProvider<WebResponse> Split(this IResourceProvider<WebResponse> webResponse,
            out bool isFromCache,
            out bool isMutuallyAuthenticated,
            out long contentLength,
            out string contentType,
            out string responseUri,
            out WebHeaderCollection headers,
            out bool supportsHeaders)
        {
            using (var h = webResponse.GetHandle())
            {
                isFromCache = h.Resource.IsFromCache;
                isMutuallyAuthenticated = h.Resource.IsMutuallyAuthenticated;
                contentLength = h.Resource.ContentLength;
                contentType = h.Resource.ContentType;
                responseUri = h.Resource.ResponseUri.AbsoluteUri;
                headers = h.Resource.Headers;
                supportsHeaders = h.Resource.SupportsHeaders;
            }
            return webResponse;
        }
    }
}
