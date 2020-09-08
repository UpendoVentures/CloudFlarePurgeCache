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
using DotNetNuke.Entities.Portals;
using DotNetNuke.Instrumentation;
using DotNetNuke.Services.Log.EventLog;
using Upendo.Modules.CloudFlareClearCache.Components;
using Upendo.Modules.CloudFlareClearCache.Models;

namespace Upendo.Modules.CloudFlareClearCache.Services
{
    public class CloudFlareController
    {
        private static readonly ILog Logger = LoggerSource.Instance.GetLogger(typeof(CloudFlareController));

        public bool PurgeCache(Settings moduleSettings, int portalId)
        {
            var service = new ServiceProxy("https://api.cloudflare.com/client/v4/zones/");

            var result = service.PurgeCache(moduleSettings.ZoneID, GetCustomHeaders(moduleSettings));

            Logger.Debug(result.ToString());

            if (result.Errors != null && result.Errors.Count > 0)
            {
                foreach (var error in result.Errors)
                {
                    LogError(new Exception($"{error.Code}: {error.Description}"));
                }
            }

            if (result.Content != null)
            {
                var resCloudFlare = result.Content;

                if (resCloudFlare.Errors != null && resCloudFlare.Errors.Count > 0)
                {
                    foreach (var error in resCloudFlare.Errors)
                    {
                        LogError(new Exception($"{error.Code}: {error.Message}"));
                    }
                }

                if (resCloudFlare.Messages != null && resCloudFlare.Messages.Count > 0)
                {
                    foreach (var msg in resCloudFlare.Messages)
                    {
                        Logger.Debug(msg);
                    }
                }

                PortalController.UpdatePortalSetting(portalId, FeatureController.SETTING_STATUS, $"{resCloudFlare.Success}", true);

                var ctlLog = new EventLogController();
                var log = new LogInfo { LogTypeKey = FeatureController.LOGTYPEKEY };
                ctlLog.AddLog(log);

                return resCloudFlare.Success;
            }

            return false;
        }

        private Dictionary<string, string> GetCustomHeaders(Settings moduleSettings)
        {
            var newHeaders = new Dictionary<string, string>
            {
                {"X-Auth-Key", moduleSettings.ApiToken}, 
                {"X-Auth-Email", moduleSettings.Email}
            };

            return newHeaders;
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