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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using DotNetNuke.Common.Utilities;
using DotNetNuke.Data;
using DotNetNuke.Entities.Modules;
using DotNetNuke.Entities.Modules.Definitions;
using DotNetNuke.Entities.Portals;
using DotNetNuke.Entities.Tabs;
using DotNetNuke.Entities.Users;
using DotNetNuke.Instrumentation;
using DotNetNuke.Security.Permissions;
using DotNetNuke.Services.Log.EventLog;
using DotNetNuke.Services.Upgrade;

namespace Upendo.Modules.CloudFlareClearCache.Components
{
    public class FeatureController : IUpgradeable
    {
        private static readonly ILog Logger = LoggerSource.Instance.GetLogger(typeof(FeatureController));

        public const string SETTING_APITOKEN = "Upendo.Modules.CloudFlareClearCache_ApiToken";
        public const string SETTING_EMAIL = "Upendo.Modules.CloudFlareClearCache_Email";
        public const string SETTING_ZONEID = "Upendo.Modules.CloudFlareClearCache_ZoneID";

        public const string SETTING_STATUS = "uvm_CloudFlareClearCache_Status";
        public const string LOGTYPEKEY = "uvm_CloudFlarePurgeCache";

        public string UpgradeModule(string Version)
        {
            try
            {
                switch (Version)
                {
                    case "01.00.00":
                        AddAdminPageToAllPortals();
                        EnsureLogTypeExists();
                        break;
                }

                return "Success";
            }
            catch (Exception e)
            {
                LogError(e);
                return "Failure";
            }
        }

        private void AddAdminPageToAllPortals()
        {
            var tabCtl = new TabController();
            var portals = new PortalController().GetPortals().OfType<PortalInfo>();

            foreach (var portal in portals)
            {
                AddAdminPage(tabCtl, portal);
            }
        }

        internal static void AddAdminPage(TabController tabCtl, PortalInfo portal)
        {
            try
            {
                var desktopModule = DesktopModuleController.GetDesktopModuleByFriendlyName("CloudFlareClearCache Module");
                var pageName = "CloudFlare Cache";
                var tabPath = string.Format("//{0}//{1}", portal.PortalID == Null.NullInteger ? "Host" : "Admin", pageName);
                var tabId = TabController.GetTabByTabPath(portal.PortalID, tabPath, Null.NullString);
                var existTab = TabController.Instance.GetTab(tabId, portal.PortalID);

                if (existTab == null || existTab.TabID == Null.NullInteger)
                {
                    existTab = Upgrade.AddAdminPage(
                        PortalController.Instance.GetPortal(portal.PortalID),
                        pageName,
                        pageName,
                        "~/Icons/Sigma/Configuration_16X16_Standard.png",
                        "~/Icons/Sigma/Configuration_32X32_Standard.png",
                        true);
                }

                AddModuleToPage(desktopModule, existTab);
            }
            catch (Exception e)
            {
                LogError(e);
                throw;
            }
        }

        internal static void AddModuleToPage(DesktopModuleInfo desktopModule, TabInfo tab)
        {
            try
            {
                if (tab.PortalID != Null.NullInteger)
                {
                    AddDesktopModuleToPortal(tab.PortalID, desktopModule.DesktopModuleID, !desktopModule.IsAdmin, false);
                }

                var moduleDefinitions = ModuleDefinitionController.GetModuleDefinitionsByDesktopModuleID(desktopModule.DesktopModuleID).Values;
                var tabModules = ModuleController.Instance.GetTabModules(tab.TabID).Values;
                foreach (var moduleDefinition in moduleDefinitions)
                {
                    if (tabModules.All(m => m.ModuleDefinition.ModuleDefID != moduleDefinition.ModuleDefID))
                    {
                        Upgrade.AddModuleToPage(
                            tab,
                            moduleDefinition.ModuleDefID,
                            desktopModule.FriendlyName,
                            "~/Icons/Sigma/Configuration_16X16_Standard.png",
                            true);
                    }
                }
            }
            catch (Exception e)
            {
                LogError(e);
                throw;
            }
        }

        internal static int AddDesktopModuleToPortal(int portalId, int desktopModuleId, bool addPermissions, bool clearCache)
        {
            try
            {
                int portalDesktopModuleID;
                PortalDesktopModuleInfo portalDesktopModule = GetPortalDesktopModule(portalId, desktopModuleId);
                if (portalDesktopModule == null)
                {
                    portalDesktopModuleID = DataProvider.Instance().AddPortalDesktopModule(portalId, desktopModuleId, UserController.Instance.GetCurrentUserInfo().UserID);
                    EventLogController.Instance.AddLog(
                        "PortalDesktopModuleID",
                        portalDesktopModuleID.ToString(),
                        PortalController.Instance.GetCurrentPortalSettings(),
                        UserController.Instance.GetCurrentUserInfo().UserID,
                        EventLogController.EventLogType.PORTALDESKTOPMODULE_CREATED);
                    if (addPermissions)
                    {
                        ArrayList permissions = PermissionController.GetPermissionsByPortalDesktopModule();
                        if (permissions.Count > 0)
                        {
                            var permission = permissions[0] as PermissionInfo;
                            PortalInfo objPortal = PortalController.Instance.GetPortal(portalId);
                            if (permission != null && objPortal != null)
                            {
                                var desktopModulePermission = new DesktopModulePermissionInfo(permission) { RoleID = objPortal.AdministratorRoleId, AllowAccess = true, PortalDesktopModuleID = portalDesktopModuleID };
                                DesktopModulePermissionController.AddDesktopModulePermission(desktopModulePermission);
                            }
                        }
                    }
                }
                else
                {
                    portalDesktopModuleID = portalDesktopModule.PortalDesktopModuleID;
                }

                if (clearCache)
                {
                    DataCache.ClearPortalCache(portalId, true);
                }

                return portalDesktopModuleID;
            }
            catch (Exception e)
            {
                LogError(e);
                throw;
            }
        }

        internal static PortalDesktopModuleInfo GetPortalDesktopModule(int portalId, int desktopModuleId)
        {
            return CBO.FillObject<PortalDesktopModuleInfo>(DataProvider.Instance().GetPortalDesktopModules(portalId, desktopModuleId));
        }

        private void EnsureLogTypeExists()
        {
            if (DoesLogTypeExist(LOGTYPEKEY))
            {
                return;
            }

            var logTypeInfo = new LogTypeInfo
            {
                LogTypeKey = LOGTYPEKEY,
                LogTypeFriendlyName = "CloudFlare: Purge Cache",
                LogTypeDescription = string.Empty,
                LogTypeCSSClass = "GeneralAdminOperation",
                LogTypeOwner = "Upendo.Logging.PurgeCloudFlareCacheLogType"
            };
            LogController.Instance.AddLogType(logTypeInfo);

            // Add LogType
            var logTypeConf = new LogTypeConfigInfo
            {
                LoggingIsActive = true,
                LogTypeKey = LOGTYPEKEY,
                KeepMostRecent = "50",
                NotificationThreshold = 1,
                NotificationThresholdTime = 1,
                NotificationThresholdTimeType = LogTypeConfigInfo.NotificationThresholdTimeTypes.Seconds,
                MailFromAddress = Null.NullString,
                MailToAddress = Null.NullString,
                LogTypePortalID = "*"
            };
            LogController.Instance.AddLogTypeConfigInfo(logTypeConf);
        }

        private bool DoesLogTypeExist(string logTypeKey)
        {
            var logTypeDictionary = LogController.Instance.GetLogTypeInfoDictionary();
            logTypeDictionary.TryGetValue(logTypeKey, out var logType);

            return logType != null;
        }

        internal static void LogError(Exception ex)
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