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
using System.Data;
using System.Linq;
using DotNetNuke.Web.Mvc.Framework.ActionFilters;
using DotNetNuke.Web.Mvc.Framework.Controllers;
using System.Web.Mvc;
using DotNetNuke.Collections;
using DotNetNuke.Common.Utilities;
using DotNetNuke.Data;
using DotNetNuke.Framework.JavaScriptLibraries;
using DotNetNuke.Instrumentation;
using DotNetNuke.Security;
using Upendo.Modules.CloudFlareClearCache.Components;
using Upendo.Modules.CloudFlareClearCache.Models;
using Upendo.Modules.CloudFlareClearCache.Services;

namespace Upendo.Modules.CloudFlareClearCache.Controllers
{
    [DnnHandleError]
    public class ClearCacheController : DnnController
    {
        private static readonly ILog Logger = LoggerSource.Instance.GetLogger(typeof(ClearCacheController));

        #region Properties
        private Settings moduleSettings;
        private Settings ModuleSettings
        {
            get
            {
                if (moduleSettings != null)
                {
                    return moduleSettings;
                }

                moduleSettings = new Settings
                {
                    ApiToken = ModuleContext.Configuration.ModuleSettings.GetValueOrDefault(FeatureController.SETTING_APITOKEN, string.Empty),
                    Email = ModuleContext.Configuration.ModuleSettings.GetValueOrDefault(FeatureController.SETTING_EMAIL, string.Empty),
                    ZoneID = ModuleContext.Configuration.ModuleSettings.GetValueOrDefault(FeatureController.SETTING_ZONEID, string.Empty)
                };

                if (!string.IsNullOrEmpty(moduleSettings.ApiToken))
                {
                    moduleSettings.ApiToken = FIPSCompliant.DecryptAES(moduleSettings.ApiToken, Config.GetDecryptionkey(), PortalSettings.GUID.ToString());
                }

                if (!string.IsNullOrEmpty(moduleSettings.ZoneID))
                {
                    moduleSettings.ZoneID = FIPSCompliant.DecryptAES(moduleSettings.ZoneID, Config.GetDecryptionkey(), PortalSettings.GUID.ToString());
                }

                return moduleSettings;
            }
        }
        #endregion

        [HttpPost]
        [DotNetNuke.Web.Mvc.Framework.ActionFilters.ValidateAntiForgeryToken]
        public ActionResult Index(ClearCacheInfo model)
        {
            try
            {
                if (model.Config == null)
                {
                    model.Config = ModuleSettings;
                }

                ViewBag.ReadyToUse = IsReadyToUse(model);

                if (!ViewBag.ReadyToUse)
                {
                    ViewBag.Status = "failure";
                }
                else
                {
                    var originalStatus = GetPortalSetting(PortalSettings.PortalId);

                    var ctlCf = new CloudFlareController();
                    var result = ctlCf.PurgeCache(ModuleSettings, PortalSettings.PortalId);

                    var currentStatus = GetPortalSetting(PortalSettings.PortalId);

                    //if (originalStatus.LastModifiedOnDate != currentStatus.LastModifiedOnDate)
                    if (result)
                    {
                        if (currentStatus.SettingValue == "true")
                        {
                            ViewBag.Status = "success";
                        }
                        else
                        {
                            ViewBag.Status = "failure";
                        }
                    }
                    else
                    {
                        ViewBag.Status = "failure";
                    }
                }
            }
            catch (Exception e)
            {
                LogError(e);
                throw;
            }

            return View(model);
        }

        [HttpGet]
        public ActionResult Index()
        {
            DotNetNuke.Framework.JavaScriptLibraries.JavaScript.RequestRegistration(CommonJs.jQuery);

            var model = new ClearCacheInfo();

            try
            {
                ViewBag.ReadyToUse = false;

                model.Config = ModuleSettings;

                if (IsReadyToUse(model))
                {
                    model.Action = ActionType.ClearCache;
                    ViewBag.ReadyToUse = true;
                }

                var url = PortalSettings.PortalAlias.HTTPAlias;
                if (Request.IsSecureConnection)
                {
                    url = string.Concat("https://", url);
                }
                else
                {
                    url = string.Concat("http://", url);
                }

                model.UrlPattern = url;
            }
            catch (Exception e)
            {
                LogError(e);
                throw;
            }

            return View(model);
        }

        private bool IsReadyToUse(ClearCacheInfo model)
        {
            if (model == null) return false;

            return (!string.IsNullOrEmpty(model.Config.ApiToken) && 
                    !string.IsNullOrEmpty(model.Config.Email) &&
                    !string.IsNullOrEmpty(model.Config.ZoneID));
        }

        private PortalSetting GetPortalSetting(int portalId)
        {
            var settings = new List<PortalSetting>();

            using (IDataContext ctx = DataContext.Instance())
            {
                settings = ctx.ExecuteQuery<PortalSetting>(CommandType.Text,
                    "SELECT [PortalSettingID], [PortalID], [SettingName], [SettingValue], [CreatedByUserID], [CreatedOnDate], [LastModifiedByUserID], [LastModifiedOnDate], [CultureCode], [IsSecure] FROM {databaseOwner}[{objectQualifier}PortalSettings] ORDER BY [LastModifiedOnDate] DESC;").ToList();
            }

            return settings.FirstOrDefault(setting => setting.SettingName == FeatureController.SETTING_STATUS);
        }

        private void LogError(Exception ex)
        {
            if (ex != null)
            {
                Logger.Error(ex.Message, ex);
                if (ex.InnerException != null)
                {
                    Logger.Error(ex.InnerException.Message, ex.InnerException);
                }
            }
        }
    }
}
