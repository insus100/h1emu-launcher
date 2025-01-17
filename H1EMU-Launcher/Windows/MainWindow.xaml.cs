﻿using H1EMU_Launcher.Resources;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Media;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace H1EMU_Launcher
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>

    public partial class MainWindow : Window
    {

#pragma warning disable SYSLIB0014 // Warning saying that WebClient is discontinued and not supported anymore.

        string downloadUrl;
        public static string downloadFileName;

        Splash sp = new Splash();

        public MainWindow()
        {
            InitializeComponent();

            //Adds the correct language file to the resource dictionary and then load it.
            Resources.MergedDictionaries.Add(SetLanguageFile.LoadFile());

            sp.Show();

            CheckVersion();
        }

        public void CheckVersion()
        {
            try
            {
                WebClient wc = new WebClient();
                wc.Headers.Add("User-Agent", "d-fens HttpClient");
                string jsonApp = wc.DownloadString("https://api.github.com/repos/H1emu/h1emu-launcher/releases/latest");

                WebClient wc2 = new WebClient();
                wc2.Headers.Add("User-Agent", "d-fens HttpClient");
                string jsonServer = wc2.DownloadString("https://api.github.com/repos/QuentinGruber/h1z1-server/releases/latest");

                // Get latest release number and date published for app
                var jsonDesApp = JsonConvert.DeserializeObject<dynamic>(jsonApp);
                string raw = jsonDesApp.tag_name;
                string online = raw.Substring(1);
                string local = Assembly.GetExecutingAssembly().GetName().Version.ToString().TrimEnd('0').TrimEnd('.');
                downloadUrl = jsonDesApp.assets[0].browser_download_url;
                downloadFileName = jsonDesApp.assets[0].name;

                // Get latest release number and date published for server
                var jsonDesServer = JsonConvert.DeserializeObject<dynamic>(jsonServer);
                string onlineServer = jsonDesServer.tag_name;
                string dateExactServer = jsonDesServer.published_at;
                string patchNotesServer = jsonDesServer.body;

                // If there is an internet connection set these properties
                Launcher.latestUpdateVersionServer = onlineServer;
                Launcher.recentDateServer = dateExactServer;
                Launcher.patchNotes = patchNotesServer;

                // Store the latest server version, date and patch notes in the case of no internet
                Properties.Settings.Default.latestServerVersion = onlineServer;
                Properties.Settings.Default.publishDate = dateExactServer;
                Properties.Settings.Default.patchNotes = patchNotesServer;
                Properties.Settings.Default.Save();

                if (local == online)
                {
                    sp.Close();

                    this.Topmost = true;
                    this.Close();
                }
                else
                {
                    sp.Close();
                }
            }
            catch (Exception e) when (e.Message == "No such host is known. (api.github.com:443)")
            {
                sp.Close();

                CustomMessageBox.Show(FindResource("item66").ToString() + $" \"{e.Message}\"." + FindResource("item137").ToString().Replace("\\" + "n" + "\\" + "n", Environment.NewLine + Environment.NewLine));

                this.Topmost = true;
                this.Close();
            }
            catch (Exception ex)
            {
                sp.Close();

                CustomMessageBox.Show(FindResource("item66").ToString() + $" \"{ex.Message}\".");

                this.Topmost = true;
                this.Close();
            }
        }

        private void updateButton_Click(object sender, RoutedEventArgs e)
        {
            notNow.Foreground = new SolidColorBrush(Colors.Gray);
            notNow.IsEnabled = false;
            updateButton.IsEnabled = false;
            closeButton.IsEnabled = false;

            new Thread(() => 
            {
                try
                {
                    ManualResetEvent ma = new ManualResetEvent(false);

                    WebClient wc = new WebClient();
                    wc.DownloadProgressChanged += (s, e) =>
                    {
                        Dispatcher.BeginInvoke((System.Windows.Forms.MethodInvoker)delegate
                        {
                            downloadSetupProgress.Value = e.ProgressPercentage;
                        });
                    };
                    wc.DownloadFileCompleted += (s, e) =>
                    {
                        ma.Set();
                    };

                    wc.DownloadFileAsync(new Uri(downloadUrl), $"{Launcher.appDataPath}\\H1EmuLauncher\\{downloadFileName}");
                    ma.WaitOne();

                    Process.Start(new ProcessStartInfo
                    {
                        FileName = $"{Launcher.appDataPath}\\H1EmuLauncher\\{downloadFileName}",
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke((System.Windows.Forms.MethodInvoker)delegate
                    {
                        CustomMessageBox.Show(FindResource("item142").ToString().Replace("{0}", ex.Message));
                    });
                }

                Environment.Exit(69);

            }).Start();
        }

        private void NotNowClick(object sender, RoutedEventArgs e)
        {
            this.Topmost = true;
            this.Close();
        }

        private void CloseUpdater(object sender, RoutedEventArgs e)
        {
            this.Topmost = true;
            this.Close();
        }

        private void MoveWindow(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void MainUpdateWindowLoaded(object sender, RoutedEventArgs e)
        {
            SystemSounds.Beep.Play();
        }

        private void MainUpdateWindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            notNow.Foreground = new SolidColorBrush(Colors.White);
            this.Hide();

            Launcher la = new Launcher();
            la.Show();
        }

        private void NotNowHyperLinkMouseDown(object sender, MouseButtonEventArgs e)
        {
            notNow.Foreground = new SolidColorBrush(Colors.Gray);
        }
    }
}