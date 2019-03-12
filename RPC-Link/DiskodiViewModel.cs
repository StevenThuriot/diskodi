using IniParser;
using IniParser.Model;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Security;

namespace RPC_Link
{
    public class DiskodiViewModel : INotifyPropertyChanged
    {
        public string Address { get; set; }
        public int Port { get; set; }
        public string User { get; set; }
        public SecureString Password { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        string KodiUrl => @"http://" + Address + ":" + Port + "/jsonrpc?request={%22jsonrpc%22:%20%222.0%22,%20%22method%22:%20%22Player.GetItem%22,%20%22params%22:%20{%20%22properties%22:%20[%22title%22,%20%22album%22,%20%22artist%22,%20%22season%22,%20%22episode%22,%20%22duration%22,%20%22showtitle%22,%20%22tvshowid%22,%20%22thumbnail%22,%20%22file%22,%20%22fanart%22,%20%22streamdetails%22],%20%22playerid%22:%201%20},%20%22id%22:%20%22VideoGetItem%22}";
        string KodiPropsUrl => @"http://" + Address + ":" + Port + "/jsonrpc?request={%22jsonrpc%22: %222.0%22, %22method%22: %22Player.GetProperties%22, %22params%22: { %22playerid%22: 1, %22properties%22: [%22totaltime%22,%22time%22] }, %22id%22: 1}";

        public (string title, string episodeName, TimeSpan current, TimeSpan total) ResolveStatus()
        {
            string title = "";
            string episodeName = "";

            var current = TimeSpan.Zero;
            var total = TimeSpan.Zero;

            try
            {
                using (var wc = new WebClient())
                {
                    if (Password != null)
                    {
                        wc.Credentials = new NetworkCredential(User, Password);
                    }

                    var json = wc.DownloadString(KodiUrl);
                    dynamic parsedInfo = Newtonsoft.Json.JsonConvert.DeserializeObject(json);

                    title = parsedInfo.result.item.showtitle;
                    episodeName = parsedInfo.result.item.title;

                    if ((title == "") && (episodeName == ""))
                    {
                        title = "In Menus";
                    }
                    else
                    {
                        var propsJson = wc.DownloadString(KodiPropsUrl);
                        dynamic parsedProps = Newtonsoft.Json.JsonConvert.DeserializeObject(propsJson);

                        int hours = parsedProps.result.time.hours.ToObject<int>();
                        int minutes = parsedProps.result.time.minutes.ToObject<int>();
                        int seconds = parsedProps.result.time.seconds.ToObject<int>();

                        current = new TimeSpan(hours, minutes, seconds);

                        hours = parsedProps.result.totaltime.hours.ToObject<int>();
                        minutes = parsedProps.result.totaltime.minutes.ToObject<int>();
                        seconds = parsedProps.result.totaltime.seconds.ToObject<int>();

                        total = new TimeSpan(hours, minutes, seconds);
                    }
                }
            }
            catch
            {
                Debug.WriteLine("Unable to Connect to Remote Server");
            }

            return (title, episodeName, current, total);
        }

        public DiskodiViewModel()
        {
            var iniParser = new FileIniDataParser();
            var iniData = new IniData();

            Directory.CreateDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RPC-Link"));

            try
            {
                iniData = iniParser.ReadFile(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RPC-Link", "settings.ini"));

                Address = iniData["Settings"]["tbKodiAddr"] ?? "localhost";
                User = iniData["Settings"]["tbKodiUser"] ?? "kodi";
                if (int.TryParse(iniData["Settings"]["tbKodiPort"], out var port))
                {
                    Port = port;
                }
                else
                {
                    Port = 8080;
                }
            }
            catch (Exception ex)
            {
                Debug.Write(ex);
            }
        }


        public void Save()
        {
            var iniParser = new FileIniDataParser();
            var iniData = new IniData();

            try
            {
                iniData = iniParser.ReadFile(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RPC-Link", "settings.ini"));
            }
            catch (Exception ex)
            {
                Debug.Write(ex);
            }

            iniData["Settings"]["tbKodiAddr"] = Address;
            iniData["Settings"]["tbKodiPort"] = Port.ToString();
            iniData["Settings"]["tbKodiUser"] = User;

            iniParser.WriteFile(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RPC-Link", "settings.ini"), iniData);
        }
    }
}