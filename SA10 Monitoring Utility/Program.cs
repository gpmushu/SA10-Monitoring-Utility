using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net.NetworkInformation;
using System.Diagnostics;
using System.ServiceProcess;
using Microsoft.Win32;
using Newtonsoft.Json;

namespace SA10_Monitoring_Utility
{

    //class to hold the AssetStatus data
    public class AssetStatusOutput
    {
        public string assetID { get; set; }
        public string dateTime { get; set; }
        public bool onlineStatus { get; set; }
        public double netSpeed { get; set; }
        public double netQuality { get; set; }
        public bool dfStatus { get; set; }
        public bool cmStatus { get; set; }
        public bool gkStatus { get; set; }
        public bool regkeyStatus { get; set; }
    }


    class Program
    {
        static void Main(string[] args)
        {
            //format filename as log file
            string path = "T:\\SA10 Monitoring Utility\\" + AssetIDmethod() + " " + DateTime.Now.ToString("M-dd-yyyy") + ".log";

            //Run methods to get Asset Status data
            AssetStatusOutput output = new AssetStatusOutput
            {
                assetID = AssetIDmethod(),
                dateTime = DateTime.Now.ToString(),
                onlineStatus = OnlineStatusMethod(),
                netSpeed = NetSpeedMethod(),
                netQuality = NetQualityMethod(),
                dfStatus = DFStatusMethod(),
                cmStatus = CMStatusMethod(),
                gkStatus = GKStatusMethod(),
                regkeyStatus = RegKeyStatusMethod()
            };

            //format data as json
            string json = JsonConvert.SerializeObject(output);

            //write data to log
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(path, true))
            {
                file.WriteLine(json);
            } 
        }

        //Returns the AssetID from SA10 config.JSON file
        private static string AssetIDmethod()
        {
            try
            {
                string source = System.IO.File.ReadAllText("T:\\ClientViewer\\Config.json");
                string[] substrings = source.Split(',');
                string assetID = substrings[1].Substring(11, 15);
                return assetID;
            }
            catch (FileNotFoundException)
            {
                string assetID = "000000000000000";
                return assetID;
            }
        }

        //Pings 8.8.8.8 and returns success or failure
        private static bool OnlineStatusMethod()
        {
            try
            {
                Ping ping = new Ping();
                PingReply reply = ping.Send("8.8.8.8", 1000);
                if (reply != null)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch
            {
                return false;
            }
        }

        //Calculates and returns network speed in mbps
        private static double NetSpeedMethod()
        {
            System.Net.WebClient wc = new System.Net.WebClient();

            DateTime dt1 = DateTime.Now;

            wc.DownloadFile("https://gpmushustorage.blob.core.windows.net/storage1/5MB.zip", @"T:\\SA10 Monitoring Utility\\speedtest.txt");

            DateTime dt2 = DateTime.Now;

            double netSpeed = Math.Round((40) / (dt2 - dt1).TotalSeconds, 2);

            System.IO.File.Delete(@"T:\\SA10 Monitoring Utility\\speedtest.txt");

            return netSpeed;
        }
        
        //Calculates and returns packet loss as percent
        private static double NetQualityMethod()
        {
            Ping pktloss = new Ping();
            PingOptions options = new PingOptions();

            options.DontFragment = true;

            string data = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
            byte[] buffer = Encoding.ASCII.GetBytes(data);
            int timeout = 120;
            double failed = 0;

            for (int i = 0; i < 50; i++)
            {
                PingReply reply = pktloss.Send("8.8.8.8", timeout, buffer, options);
                if (reply.Status != IPStatus.Success)
                {
                    failed += 1;
                }
            }
            double percent = ((50 - failed) / 50) * 100;
            return percent;
        }

        //Returns Deep Freeze frozen/thawed status
        private static bool DFStatusMethod()
        {
            Process proc = new Process();
            string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.SystemX86), "dfc.exe");
            proc.StartInfo.FileName = path;
            proc.StartInfo.Arguments = "get /isfrozen";
            proc.StartInfo.RedirectStandardOutput = true;
            proc.StartInfo.CreateNoWindow = true;
            proc.StartInfo.UseShellExecute = false;
            proc.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            proc.Start();
            proc.WaitForExit();
            if (proc.ExitCode == 1 )
            {
                return true;
            }
            else
            {
                return false;
            }
            
        }

        //Returns status of ClientManager.exe process
        private static bool CMStatusMethod()
        {
            Process[] clientManager = Process.GetProcessesByName("ClientManager");
            if (clientManager.Length == 0)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        //Returns status of GlobalKiosk Service
        private static bool GKStatusMethod()
        {
            ServiceController sc = new ServiceController
            {
                ServiceName = "GlobalKiosk Service"
            };

            if (sc.Status == ServiceControllerStatus.Running)
            {
                return true; 
            }
            else
            {
                return false;
            }
        }

        //Returns status of ServicesPipeTimeout regkey
        private static bool RegKeyStatusMethod()
        {
            string keyName = @"HKEY_LOCAL_MACHINE\System\CurrentControlSet\Control";
            string valueName = "ServicesPipeTimeout";

            if (Registry.GetValue(keyName, valueName, null) == null)
            {
                return false;
            }
            else
            {
                return true;
            }

        }
    }

}
