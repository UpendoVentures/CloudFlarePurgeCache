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
using DotNetNuke.Collections;
using DotNetNuke.Security;
using DotNetNuke.Web.Mvc.Framework.ActionFilters;
using DotNetNuke.Web.Mvc.Framework.Controllers;
using System.Web.Mvc;
using DotNetNuke.Common.Utilities;
using Upendo.Modules.CloudFlareClearCache.Components;

namespace Upendo.Modules.CloudFlareClearCache.Controllers
{
    [DnnModuleAuthorize(AccessLevel = SecurityAccessLevel.Edit)]
    [DnnHandleError]
    public class SettingsController : DnnController
    {
        /// <summary>
        /// Settings Get: Gets the module settings.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public ActionResult Settings()
        {
            var settings = new Models.Settings();

            var email = ModuleContext.Configuration.ModuleSettings.GetValueOrDefault(FeatureController.SETTING_EMAIL, string.Empty);
            var zoneId = ModuleContext.Configuration.ModuleSettings.GetValueOrDefault(FeatureController.SETTING_ZONEID, string.Empty);

            if (!string.IsNullOrEmpty(zoneId))
            {
                zoneId = FIPSCompliant.DecryptAES(zoneId, Config.GetDecryptionkey(), PortalSettings.GUID.ToString());
            }

            settings.Email = email;
            settings.ZoneID = zoneId;

            return View(settings);
        }

        /// <summary>
        /// Settings Post: Saves the module settings.
        /// </summary>
        /// <param name="settings"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateInput(false)]
        [DotNetNuke.Web.Mvc.Framework.ActionFilters.ValidateAntiForgeryToken]
        public ActionResult Settings(Models.Settings settings)
        {
			var security = new PortalSecurity();

            if (!string.IsNullOrEmpty(settings.ZoneID))
            {
                settings.ZoneID = security.InputFilter(settings.ZoneID.Trim(), PortalSecurity.FilterFlag.NoMarkup);
                settings.ZoneID = FIPSCompliant.EncryptAES(settings.ZoneID, Config.GetDecryptionkey(), PortalSettings.GUID.ToString());
                ModuleContext.Configuration.ModuleSettings[FeatureController.SETTING_ZONEID] = settings.ZoneID;
            }
            else
            {
                ModuleContext.Configuration.ModuleSettings[FeatureController.SETTING_ZONEID] = string.Empty;
            }

            if (string.Equals(settings.ApiToken, "clear", StringComparison.OrdinalIgnoreCase))
            {
                ModuleContext.Configuration.ModuleSettings[FeatureController.SETTING_APITOKEN] = string.Empty;
            }
            else if (!string.IsNullOrEmpty(settings.ApiToken))
            {
                settings.ApiToken = security.InputFilter(settings.ApiToken.Trim(), PortalSecurity.FilterFlag.NoMarkup);
                settings.ApiToken = FIPSCompliant.EncryptAES(settings.ApiToken, Config.GetDecryptionkey(), PortalSettings.GUID.ToString());
                ModuleContext.Configuration.ModuleSettings[FeatureController.SETTING_APITOKEN] = settings.ApiToken;
            }

            if (!string.IsNullOrEmpty(settings.Email))
            {
                settings.Email = security.InputFilter(settings.Email.Trim(), PortalSecurity.FilterFlag.NoMarkup);
                ModuleContext.Configuration.ModuleSettings[FeatureController.SETTING_EMAIL] = settings.Email;
            }
            else
            {
                ModuleContext.Configuration.ModuleSettings[FeatureController.SETTING_EMAIL] = string.Empty;
            }

            return RedirectToDefaultRoute();
        }
    }
}