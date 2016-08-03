using System;
using Plugin.Settings;
using Plugin.Settings.Abstractions;

namespace GrocerySync.Helpers
{
    /// <summary>
    /// This is the Settings static class that can be used in your Core solution or in any
    /// of your client applications. All settings are laid out the same exact way with getters
    /// and setters. 
    /// </summary>
    public static class Settings
    {
        private static ISettings AppSettings
        {
            get
            {
                return CrossSettings.Current;
            }
        }

        #region Setting Constants

        private const string SyncUrlKey = "sync_url";

        #endregion


        public static string SyncURL
        {
            get
            {
                return AppSettings.GetValueOrDefault(SyncUrlKey, String.Empty);
            }
            set
            {
                AppSettings.AddOrUpdateValue(SyncUrlKey, value);
            }
        }

    }
}