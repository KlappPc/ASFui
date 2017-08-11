﻿using System;
using ASFui.Properties;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.ServiceModel;
using System.Windows.Forms;

namespace ASFui
{
    internal static class Util
    {
		private static readonly NetTcpBinding Binding = new NetTcpBinding { SendTimeout = new TimeSpan(0, 30, 0), Security = { Mode = SecurityMode.None } };

        public static bool CheckBinary()
        {
            return File.Exists(Settings.Default.ASFBinary) || !Settings.Default.IsLocal;
        }

        public static string SendCommand(string command)
        {
			string adress;
			if ((adress = GetEndpointAddress()).StartsWith("net")){
				using (var asfClient = new Client(Binding, new EndpointAddress(adress))) {
					return asfClient.HandleCommand(command);
				}
			}

			using (WebClient webclient = new WebClient()) {
				return webclient.DownloadString(adress +"?command="+command);
			}
		}

        public static string GenerateCommand(string command, string user, string args = "")
        {
            return command + " " + user + " " + args;
        }

        public static string MultiToOne(string[] text)
        {
            string command = null;
            text = text.Where(x => !string.IsNullOrEmpty(x) && !string.IsNullOrWhiteSpace(x)).ToArray();
            command += string.Join(",", text);

            return command;
        }

        public static string GetEndpointAddress()
        {
            if (!Settings.Default.IsLocal) return Settings.Default.RemoteURL;
            var json =
                JObject.Parse(
                    File.ReadAllText(Path.GetDirectoryName(Settings.Default.ASFBinary) + @"/config/ASF.json"));

			var hostname = "127.0.0.1";
			var port = "1242";
			//newversion:
			if (null==json["WCFHost"] && json["WCFPort"]==null && json["WCFHostname"] == null) {
				if (null != json["IPCHostname"])
					hostname = json["IPCHostname"].ToString();
				if (null != json["IPCPort"])
					port = json["IPCPort"].ToString();
				return "http://" + hostname + ":" + port + "/IPC";
			}
			if (null != json["WCFHost"])
				hostname = json["WCFHost"].ToString();
			if (null != json["WCFHostname"])
				hostname = json["WCFHostname"].ToString();
			if (null != json["WCFPort"])
				port = json["WCFPort"].ToString();

            return "net.tcp://" + hostname + ":" + port + "/ASF";
        }

        public static bool CheckIfAsfIsRunning()
        {
            return Process.GetProcessesByName("ASF").Length > 0 ||
                   Process.GetProcessesByName("ArchiSteamFarm").Length > 0 ||
                   Process.GetProcessesByName(Path.GetFileNameWithoutExtension(Settings.Default.ASFBinary)).Length > 0;
        }

        public static void CheckVersion()
        {
            string currentVersion;
            using (var web = new WebClient())
            {
                currentVersion =
                    web.DownloadString("https://raw.githubusercontent.com/alvr/ASFui/master/version.txt");
            }

            if (new Version(Application.ProductVersion).CompareTo(new Version(currentVersion)) >= 0) return;
            var option = MessageBox.Show(@"A new version (" + currentVersion + @") is available, download now?",
                @"New version", MessageBoxButtons.YesNo,
                MessageBoxIcon.Information);
            if (option == DialogResult.Yes)
            {
                Process.Start("https://github.com/alvr/ASFui/releases/latest");
            }
        }
		
		public static void UpgradeSettings()
		{
            if (!Settings.Default.UpdateSettings) return;
            Settings.Default.Upgrade();
            Settings.Default.UpdateSettings = false;
            Settings.Default.Save();
            Logging.Info("ASFui updated to " + new Version(Application.ProductVersion));
        }

        public static bool IsOnScreen(Rectangle rec, double minPercentOnScreen = 0.2)
        {
            var pixelsVisible = Screen.AllScreens.Select(scrn => Rectangle.Intersect(rec, scrn.WorkingArea))
                .Where(r => r.Width != 0 & r.Height != 0)
                .Aggregate<Rectangle, double>(0, (current, r) => current + (r.Width*r.Height));

            return pixelsVisible >= (rec.Width * rec.Height) * minPercentOnScreen;
        }

        public static bool CheckUrl(string url)
        {

            return url.ToLower().StartsWith("net.tcp://");
            /* Do an advanced check here if the URL fits or something is currently listening.
            try
            {
                var client = new MetadataExchangeClient(Binding);
                client.GetMetadata();
                return true;
            }
            catch (Exception ex)
            {
                Logging.Exception(ex, "Invalid remote URL.");
                return false;
            }
            */
        }
    }
}