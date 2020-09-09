/*
MIT License

Copyright (c) Upendo Ventures, LLC

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using DotNetNuke.Instrumentation;
using Upendo.Modules.CloudFlareClearCache.Components;
using Upendo.Modules.CloudFlareClearCache.Models;

namespace Upendo.Modules.CloudFlareClearCache.Services
{
    public class ServiceHelper
    {
        private static readonly ILog Logger = LoggerSource.Instance.GetLogger(typeof(ServiceHelper));

        public static T GetRequest<T>(string uri)
            where T : class, new()
        {
            var result = SendGet<T>(uri);
            return result;
        }

        public static T PostRequest<T>(string uri, string data, string contentType = "", Dictionary<string, string> customHeaders = null)
            where T : class, new()
        {
            var result = SendWithData<T>(uri, "POST", data, contentType, customHeaders);
            return result;
        }

        public static ServiceResponse<CloudFlareResponse> PostRequest(string uri, string data, string contentType = "", Dictionary<string, string> customHeaders = null)
        {
            var result = SendWithData(uri, "POST", data, contentType, customHeaders);
            return result;
        }

        public static T PutRequest<T>(string uri, string data)
            where T : class, new()
        {
            var result = SendWithData<T>(uri, "PUT", data);
            return result;
        }

        public static T DeleteRequest<T>(string uri, string data)
            where T : class, new()
        {
            var result = SendWithData<T>(uri, "DELETE", data);
            return result;
        }

        private const int DefaultTimeout = 100000;

        private static T SendGet<T>(string uri)
            where T : class, new()
        {
            try
            {
                Logger.Debug($"Service URL: {uri}");

                var response = SendRequest(uri, "GET", null);

                Logger.Debug(response.ToString());
                
                var result = JsonHelper.ObjectFromJson<T>(response);

                return result;
            }
            catch (Exception ex)
            {
                LogError(ex);

                var result = new T();
                var apiResponse = result as IServiceResponse;

                if (apiResponse != null)
                {
                    apiResponse.Errors.Add(new ServiceError("EXCEPTION", ex.Message + " | " + ex.StackTrace));
                }

                return result;
            }
        }

        private static T SendWithData<T>(string uri, string method, string data, string contentType = "", Dictionary<string, string> customHeaders = null)
            where T : class, new()
        {
            try
            {
                var response = SendRequest(uri, method, data, contentType, customHeaders);

                Logger.Debug(response.ToString());
                
                var result = JsonHelper.ObjectFromJson<T>(response);
                //var result = DotNetNuke.Common.Utilities.Json.Deserialize<T>(response);
                
                return result;
            }
            catch (Exception ex)
            {
                LogError(ex);

                var result = new T();
                var apiResponse = result as IServiceResponse;

                if (apiResponse != null)
                {
                    apiResponse.Errors.Add(new ServiceError("EXCEPTION", ex.Message + " | " + ex.StackTrace));
                }

                return result;
            }
        }

        private static ServiceResponse<CloudFlareResponse> SendWithData(string uri, string method, string data, string contentType = "", Dictionary<string, string> customHeaders = null)
        {
            try
            {
                var strResponse = SendRequest(uri, method, data, contentType, customHeaders);

                var oResult = new ServiceResponse<CloudFlareResponse>();

                Logger.Debug(strResponse.ToString());

                oResult.Content= JsonHelper.ObjectFromJson<CloudFlareResponse>(strResponse);

                return oResult;
            }
            catch (Exception ex)
            {
                LogError(ex);

                var result = new ServiceResponse<CloudFlareResponse>();
                var apiResponse = result as IServiceResponse;

                if (apiResponse != null)
                {
                    apiResponse.Errors.Add(new ServiceError("EXCEPTION", ex.Message + " | " + ex.StackTrace));
                }

                return result;
            }
        }

        private static string SendRequest(string serviceUrl, string method, string data, string contentType = "", Dictionary<string,string> customHeaders = null, WebProxy proxy = null, int timeout = DefaultTimeout)
        {
            WebResponse objResp;
            WebRequest objReq;
            var strResp = string.Empty;
            byte[] byteReq;

            try
            {
                Logger.Debug($"Service URL: {serviceUrl}");
                Logger.Debug($"Content-Type: {contentType}");
                Logger.Debug($"Method: {method}");
                Logger.Debug($"Data: {data}");
                if (customHeaders != null)
                {
                    foreach (var header in customHeaders)
                    {
                        Logger.Debug($"Header Key: {header.Key}");
                        Logger.Debug($"Header Value: {header.Value}");
                    }
                }

                byteReq = data == null ? null : Encoding.UTF8.GetBytes(data);
                objReq = WebRequest.Create(serviceUrl);
                objReq.Method = method.ToUpperInvariant();

                if (byteReq != null)
                {
                    objReq.ContentLength = byteReq.Length;
                    objReq.ContentType = "application/x-www-form-urlencoded";
                }

                if (!string.IsNullOrEmpty(contentType))
                {
                    // override the content type for certain API calls that require it
                    objReq.ContentType = contentType;
                }

                if (customHeaders != null && customHeaders.Count > 0)
                {
                    foreach (var header in customHeaders)
                    {
                        objReq.Headers[header.Key] = header.Value;
                    }
                }

                objReq.Timeout = timeout;

                if (proxy != null)
                {
                    objReq.Proxy = proxy;
                }

                Logger.Debug($"Request RequestUri: {objReq.RequestUri}");
                Logger.Debug($"Request ContentType: {objReq.ContentType}");
                Logger.Debug($"Request Method: {objReq.Method}");
                for (int i = 0; i < objReq.Headers.Count; ++i)
                {
                    string header = objReq.Headers.GetKey(i);
                    foreach (var value in objReq.Headers.GetValues(i))
                    {
                        Logger.Debug($"Request Header Key: {header}");
                        Logger.Debug($"Request Header Value: {value}");
                    }
                }

                if (byteReq != null)
                {
                    var OutStream = objReq.GetRequestStream();

                    OutStream.Write(byteReq, 0, byteReq.Length);
                    OutStream.Close();
                }

                objResp = objReq.GetResponse();

                var sr = new StreamReader(objResp.GetResponseStream(), Encoding.UTF8, true);

                strResp += sr.ReadToEnd();
                sr.Close();
            }
            catch (Exception ex)
            {
                LogError(ex);
                throw new ArgumentException("Error SendRequest: " + ex.Message + " " + ex.Source);
            }

            return strResp;
        }

        private static void LogError(Exception exc)
        {
            try
            {
                if (exc != null)
                {
                    Logger.Error(exc.Message, exc);

                    if (exc.InnerException != null)
                    {
                        LogError(exc.InnerException);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex.Message, ex);
            }
        }
    }
}