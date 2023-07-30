using System;
using System.Collections.Generic;

namespace SyncMan
{
    public partial class MainWindow
    {
        public class Device
        {
            public String GUID { get; set; }
            public String Alias { get; set; }
            public String LastAccess { get; set; }
            public String LastAction { get; set; }
        }

        public static List<Device> XMLDevices { get; set; }

        public static Int32 TotalXMLDevices { get; set; }

        public static Int32 IndexInXMLFile { get; set; }

        public static String ExecTime { get; set; }

        public static class RoboConfig
        {
            public static String DBPath { get; set; }
            public static String DownloadCMD { get; set; }
            public static String UploadCMD { get; set; }
        }

        public static class LastDevice
        {
            public static String GUID { get; set; }
            public static String Alias { get; set; }
            public static String LastAccess { get; set; }
            public static String LastAction { get; set; }
            public static String LastState { get; set; }
        }

        public static class LocalDevice
        {
            public static String GUID { get; set; }
            public static String Alias { get; set; }
        }
    }
}
