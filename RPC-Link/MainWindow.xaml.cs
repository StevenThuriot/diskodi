using DiscordRPC;
using MahApps.Metro.Controls;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Timers;
using System.Windows;
using System.Windows.Controls;

namespace RPC_Link
{

    public partial class MainWindow : MetroWindow
    {
        const string ApplicationID = "381938792329904129";

        readonly Timer tUpdate;

        public DiskodiViewModel ViewModel { get; } = new DiskodiViewModel();
        public DiscordRpcClient DiscordRpcClient { get; set; } = new DiscordRpcClient(ApplicationID);


        public MainWindow()
        {
            DataContext = ViewModel;

            InitializeComponent();

            tUpdate = new Timer(10000);

            tUpdate.Elapsed += HandleTimer;
            tUpdate.AutoReset = true;
            tUpdate.Start();
        }

        protected override void OnStateChanged(EventArgs e)
        {
            if (WindowState == WindowState.Minimized)
                Hide();

            base.OnStateChanged(e);
        }

        void OnPasswordChanged(object sender, RoutedEventArgs e)
        {
            ViewModel.Password = ((PasswordBox)sender).SecurePassword;
        }

        void Save_Settings(object sender, RoutedEventArgs e)
        {
            ViewModel.Save();
        }

        void Show_Window(object sender, RoutedEventArgs e)
        {
            Show();
            Activate();
        }


        bool bEnable;
        void HandleTimer(Object source, ElapsedEventArgs e)
        {
            if (Process.GetProcessesByName("kodi").Length > 0)
            {
                Debug.WriteLine("Seen Kodi");
                bEnable = true;
            }
            else
            {
                if (bEnable)
                {
                    Debug.WriteLine("Shutting down Discord RPC");
                    DiscordRpcClient.ClearPresence();
                    DiscordRpcClient.Invoke();
                }

                bEnable = false;
            }

            if (bEnable)
            {
                if (!DiscordRpcClient.IsInitialized)
                {
                    Debug.WriteLine("Starting up Discord RPC");
                    DiscordRpcClient.Initialize();
                }

                var (title, episodeName, current, total) = ViewModel.ResolveStatus();

                Timestamps timestamps;

                if (current == TimeSpan.Zero || total == TimeSpan.Zero)
                {
                    timestamps = null;
                }
                else
                {
                    var start = DateTime.UtcNow.Subtract(current);
                    var end = start.Add(total);
                    timestamps = new Timestamps
                    {
                        Start = start,
                        End = end
                    };
                }

                var presence = new RichPresence
                {
                    Details = title,
                    State = episodeName,
                    Assets = new Assets
                    {
                        LargeImageKey = "diskodi_logo"
                    },
                    Timestamps = timestamps
                };

                DiscordRpcClient.SetPresence(presence);                
                DiscordRpcClient.Invoke();
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            DiscordRpcClient.ClearPresence();
            DiscordRpcClient.Dispose();

            base.OnClosing(e);
        }
    }
}


