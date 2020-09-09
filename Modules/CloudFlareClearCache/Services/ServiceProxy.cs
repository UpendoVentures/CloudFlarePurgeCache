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
using DotNetNuke.Instrumentation;
using Upendo.Modules.CloudFlareClearCache.Models;

namespace Upendo.Modules.CloudFlareClearCache.Services
{
    public class ServiceProxy : ServiceProxyBase
    {
        private static readonly ILog Logger = LoggerSource.Instance.GetLogger(typeof(ServiceProxy));

        public ServiceProxy(string baseWebSiteUri = "")
        {
            baseUri = baseWebSiteUri;

            if (!baseUri.EndsWith("/"))
            {
                baseUri += "/";
            }

            // used for internal service calls on previous modules
            //fullApiUri = System.IO.Path.Combine(baseUri, "DesktopModules/CodeCamp/API/Event/");
            fullApiUri = baseUri;
        }

        public ServiceResponse<CloudFlareResponse> PurgeCache(string zoneId, Dictionary<string, string> customHeaders = null)
        {
            try
            {
                var fullRequestUri = fullApiUri + zoneId + "/purge_cache";
                var result = new ServiceResponse<CloudFlareResponse>();

                Logger.Debug($"Calling: {fullRequestUri}");

                //result = ServiceHelper.PostRequest<ServiceResponse<CloudFlareResponse>>(fullRequestUri, "{ \"purge_everything\" : true }", "application/json", customHeaders);
                result = ServiceHelper.PostRequest(fullRequestUri, "{ \"purge_everything\" : true }", "application/json", customHeaders);

                return result;
            }
            catch (Exception e)
            {
                LogError(e);
                throw;
            }
        }

        private void LogError(Exception exc)
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