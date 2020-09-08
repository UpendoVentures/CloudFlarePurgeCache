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
            var apiToken = security.InputFilter(settings.ApiToken.Trim(), PortalSecurity.FilterFlag.NoMarkup);
            var email = security.InputFilter(settings.Email.Trim(), PortalSecurity.FilterFlag.NoMarkup);
            var zoneId = security.InputFilter(settings.ZoneID.Trim(), PortalSecurity.FilterFlag.NoMarkup);

            if (!string.IsNullOrEmpty(apiToken))
            {
                apiToken = FIPSCompliant.EncryptAES(apiToken, Config.GetDecryptionkey(), PortalSettings.GUID.ToString());
            }

            if (!string.IsNullOrEmpty(zoneId))
            {
                zoneId = FIPSCompliant.EncryptAES(zoneId, Config.GetDecryptionkey(), PortalSettings.GUID.ToString());
            }

            ModuleContext.Configuration.ModuleSettings[FeatureController.SETTING_APITOKEN] = apiToken;
            ModuleContext.Configuration.ModuleSettings[FeatureController.SETTING_EMAIL] = email;
            ModuleContext.Configuration.ModuleSettings[FeatureController.SETTING_ZONEID] = zoneId;

            return RedirectToDefaultRoute();
        }
    }
}