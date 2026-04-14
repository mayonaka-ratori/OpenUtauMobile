using Android.Content;
using Android.OS;
using Android.Provider;
using OpenUtauMobile.Utils.Permission;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Environment = Android.OS.Environment;
using Application = Android.App.Application;

namespace OpenUtauMobile.Platforms.Android.Utils.Permission
{
    /// <summary>
    /// Checks and requests MANAGE_EXTERNAL_STORAGE permission on Android.
    /// </summary>
    public class ExternalStorageService : IExternalStorageService
    {
        public async Task<bool> HasManageExternalStoragePermissionAsync()
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.R) // Android 11 (API 30) and above
            {
                return Environment.IsExternalStorageManager;
            }
            else // Android 10 (API 29) and below
            {
                var storageReadStatus = await Permissions.CheckStatusAsync<Permissions.StorageRead>(); // read
                if (storageReadStatus != PermissionStatus.Granted)
                {
                    storageReadStatus = await Permissions.RequestAsync<Permissions.StorageRead>();
                }
                var storageWriteStatus = await Permissions.CheckStatusAsync<Permissions.StorageWrite>(); // write
                if (storageWriteStatus != PermissionStatus.Granted)
                {
                    storageWriteStatus = await Permissions.RequestAsync<Permissions.StorageWrite>();
                }
                return storageReadStatus == PermissionStatus.Granted && storageWriteStatus == PermissionStatus.Granted; // true if both read and write are granted
            }
        }

        public void RequestManageExternalStoragePermission()
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.R && !Environment.IsExternalStorageManager)
            {
                var intent = new Intent(Settings.ActionManageAllFilesAccessPermission);
                intent.AddFlags(ActivityFlags.NewTask);
                Application.Context.StartActivity(intent);
            }
        }
    }
}
