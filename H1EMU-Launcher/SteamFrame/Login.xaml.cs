﻿using H1EMU_Launcher.Resources;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace H1EMU_Launcher
{
    /// <summary>
    /// Interaction logic for Login.xaml
    /// </summary>

    public partial class Login : Page
    {
        public static CancellationTokenSource tokenSource = new CancellationTokenSource();
        static CancellationToken token;

        public static string gameInfo { get; set; } = "NULL";

        public Login()
        {
            InitializeComponent();

            //Adds the correct language file to the resource dictionary and then load it.
            Resources.MergedDictionaries.Add(SetLanguageFile.LoadFile());
        }

        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (!NullLoginCheck())
                {
                    return;
                }
                else
                {
                    loadingGif.Visibility = Visibility.Visible;
                    loginEnterButton.Visibility = Visibility.Hidden;

                    new Thread(() =>
                    {
                        TryLoginDownload();

                    }).Start();
                }
            }
        }

        private void LoginButton(object sender, RoutedEventArgs e)
        {
            if (!NullLoginCheck())
            {
                return;
            }
            else
            {
                loadingGif.Visibility = Visibility.Visible;
                loginEnterButton.Visibility = Visibility.Hidden;

                new Thread(() => 
                {
                    TryLoginDownload();

                }).Start();
            }
        }

        public bool NullLoginCheck()
        {
            if (string.IsNullOrEmpty(usernameBox.Text) || string.IsNullOrEmpty(passwordBox.Password))
            {
                CustomMessageBox.Show(FindResource("item36").ToString());
                return false;
            }

            return true;
        }

        public void UpdateLang()
        {
            Resources.MergedDictionaries.Clear();

            //Adds the correct language file to the resource dictionary and then load it.
            Resources.MergedDictionaries.Add(SetLanguageFile.LoadFile());
        }

        public static string version = "";

        public async void TryLoginDownload()
        {
            string username = "";
            string password = "";
            
            string[] args = gameInfo.Split(' ');

            Dispatcher.Invoke((System.Windows.Forms.MethodInvoker)delegate 
            {
                username = usernameBox.Text;
                password = passwordBox.Password;
            });

            try
            {
                AccountSettingsStore.LoadFromFile("account.config");
            }
            catch (Exception er)
            {
                Debug.WriteLine(er.Message);
            }

            ContentDownloader.Config.RememberPassword = HasParameter(args, "-remember-password");
            ContentDownloader.Config.DownloadManifestOnly = HasParameter(args, "-manifest-only");

            int cellId = GetParameter<int>(args, "-cellid", -1);
            if (cellId == -1) { cellId = 0; }

            ContentDownloader.Config.CellID = cellId;
            string fileList = GetParameter<string>(args, "-filelist");
            string[] files = null;
            if (fileList != null)
            {
                try
                {
                    string fileListData = File.ReadAllText(fileList);
                    files = fileListData.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

                    ContentDownloader.Config.UsingFileList = true;
                    ContentDownloader.Config.FilesToDownload = new List<string>();
                    ContentDownloader.Config.FilesToDownloadRegex = new List<Regex>();

                    var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
                    foreach (var fileEnry in files)
                    {
                        try
                        {
                            string fileEntryProcessed;

                            if (isWindows) { fileEntryProcessed = fileEnry.Replace("/", "[\\\\|/]"); }
                            else { fileEntryProcessed = fileEnry; }

                            Regex rgx = new Regex(fileEntryProcessed, RegexOptions.Compiled | RegexOptions.IgnoreCase);
                            ContentDownloader.Config.FilesToDownloadRegex.Add(rgx);
                        }
                        catch
                        {
                            if (isWindows)
                            {
                                 ContentDownloader.Config.FilesToDownload.Add(fileEnry.Replace("/", "\\"));
                            }
                            ContentDownloader.Config.FilesToDownload.Add(fileEnry);
                        }
                    }
                    Debug.WriteLine("Using filelist: " + fileList);
                }
                catch (Exception ex) { Debug.WriteLine("Warning: Unable to load filelist: " + ex.ToString()); }
            }

            ContentDownloader.Config.InstallDirectory = GetParameter<string>(args, "-dir");

            ContentDownloader.Config.VerifyAll = HasParameter(args, "-verify-all") || HasParameter(args, "-verify_all") || HasParameter(args, "-validate");
            ContentDownloader.Config.MaxServers = GetParameter<int>(args, "-max-servers", 20);
            ContentDownloader.Config.MaxDownloads = GetParameter<int>(args, "-max-downloads", 8);
            ContentDownloader.Config.MaxServers = Math.Max(ContentDownloader.Config.MaxServers, ContentDownloader.Config.MaxDownloads);
            ContentDownloader.Config.LoginID = HasParameter(args, "-loginid") ? (uint?)GetParameter<uint>(args, "-loginid") : null;

            ulong pubFile = GetParameter<ulong>(args, "-pubfile", ContentDownloader.INVALID_MANIFEST_ID);
            ulong ugcId = GetParameter<ulong>(args, "-ugc", ContentDownloader.INVALID_MANIFEST_ID);

            #region App downloading

            string branch = GetParameter<string>(args, "-branch") ?? GetParameter<string>(args, "-beta") ?? ContentDownloader.DEFAULT_BRANCH;
            ContentDownloader.Config.BetaPassword = GetParameter<string>(args, "-betapassword");

            ContentDownloader.Config.DownloadAllPlatforms = HasParameter(args, "-all-platforms");
            string os = GetParameter<string>(args, "-os", null);

            if (ContentDownloader.Config.DownloadAllPlatforms && !String.IsNullOrEmpty(os))
            {
                Debug.WriteLine("Error: Cannot specify -os when -all-platforms is specified.");
            }

            string arch = GetParameter<string>(args, "-osarch", null);

            ContentDownloader.Config.DownloadAllLanguages = HasParameter(args, "-all-languages");
            string language = GetParameter<string>(args, "-language", null);

            if (ContentDownloader.Config.DownloadAllLanguages && !String.IsNullOrEmpty(language))
            {
                Debug.WriteLine("Error: Cannot specify -language when -all-languages is specified.");
            }

            bool lv = HasParameter(args, "-lowviolence");

            List<(uint, ulong)> depotManifestIds = new List<(uint, ulong)>();
            bool isUGC = false;

            if (InitializeSteam(username, password))
            {
                try
                {
                    Dispatcher.Invoke((System.Windows.Forms.MethodInvoker)delegate
                    {
                        Launcher.lncher.SteamFrame.Navigate(new Uri("..\\SteamFrame\\GameInfo.xaml", UriKind.Relative));
                    });

                    bool result = true;

                SelectLocation:
                    tokenSource.Dispose();
                    tokenSource = new CancellationTokenSource();
                    token = tokenSource.Token;

                    token.WaitHandle.WaitOne();

                    Dispatcher.Invoke((System.Windows.Forms.MethodInvoker)delegate
                    {
                        System.Windows.Forms.FolderBrowserDialog folderBrowser = new System.Windows.Forms.FolderBrowserDialog();

                        if (folderBrowser.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                        {
                            if (Directory.GetFileSystemEntries(folderBrowser.SelectedPath).Length != 0)
                            {
                                UpdateLang();

                                System.Windows.Forms.DialogResult dr = CustomMessageBox.ShowResult(FindResource("item89").ToString().Replace("\\" + "n" + "\\" + "n", Environment.NewLine + Environment.NewLine));
                                if (dr == System.Windows.Forms.DialogResult.Yes)
                                {
                                    ContentDownloader.DEFAULT_DOWNLOAD_DIR = folderBrowser.SelectedPath;
                                    result = true;
                                }
                                else
                                {
                                    result = false;
                                }
                            }
                            else
                            {
                                ContentDownloader.DEFAULT_DOWNLOAD_DIR = folderBrowser.SelectedPath;
                                result = true;
                            }
                        }
                        else
                        {
                            result = false;
                        }
                    });

                    if (!result) { goto SelectLocation; }

                    args = gameInfo.Split(' ');

                    List<uint> depotIdList = GetParameterList<uint>(args, "-depot");
                    List<ulong> manifestIdList = GetParameterList<ulong>(args, "-manifest");
                    if (manifestIdList.Count > 0)
                    {
                        if (depotIdList.Count != manifestIdList.Count)
                        {
                            Debug.WriteLine("Error: -manifest requires one id for every -depot specified");
                        }

                        var zippedDepotManifest = depotIdList.Zip(manifestIdList, (depotId, manifestId) => (depotId, manifestId));
                        depotManifestIds.AddRange(zippedDepotManifest);
                    }
                    else
                    {
                        depotManifestIds.AddRange(depotIdList.Select(depotId => (depotId, ContentDownloader.INVALID_MANIFEST_ID)));
                    }

                    uint appId = GetParameter<uint>(args, "-app", ContentDownloader.INVALID_APP_ID);
                    if (appId == ContentDownloader.INVALID_APP_ID)
                    {
                        Debug.WriteLine("Error: -app not specified!");
                    }

                    Dispatcher.Invoke((System.Windows.Forms.MethodInvoker)delegate
                    {
                        Launcher.lncher.SteamFrame.Navigate(new Uri("..\\SteamFrame\\DownloadStatus.xaml", UriKind.Relative));
                    });

                    foreach (ulong manifestVersion in manifestIdList)
                    {
                        if (manifestVersion == 1930886153446950288)
                        {
                            version = "2015";

                            Dispatcher.Invoke((System.Windows.Forms.MethodInvoker)delegate
                            {
                                DownloadStatus.downStatus.gameDownloadText.Text = $"{version}:";
                                DownloadStatus.downStatus.downloadProgressText.Text = "Processing...";
                            });
                        }
                        else
                        {
                            version = "2016";

                            Dispatcher.Invoke((System.Windows.Forms.MethodInvoker)delegate
                            {
                                DownloadStatus.downStatus.gameDownloadText.Text = $"{version}:";
                                DownloadStatus.downStatus.downloadProgressText.Text = "Processing...";
                            });
                        }
                    }

                    await ContentDownloader.DownloadAppAsync(appId, depotManifestIds, branch, os, arch, language, lv, isUGC).ConfigureAwait(false);

                    Dispatcher.Invoke((System.Windows.Forms.MethodInvoker)delegate
                    {
                        UpdateLang();
                        Launcher.lncher.SteamFrame.Navigate(new Uri("..\\SteamFrame\\DownloadComplete.xaml", UriKind.Relative));
                        loadingGif.Visibility = Visibility.Hidden;
                        loginEnterButton.Visibility = Visibility.Visible;
                        CustomMessageBox.Show(FindResource("item37").ToString() + $" {version}.");
                    });
                }
                catch (Exception ph) when (ph is TaskCanceledException)
                {
                    Dispatcher.Invoke((System.Windows.Forms.MethodInvoker)delegate
                    {
                        UpdateLang();
                        Launcher.lncher.SteamFrame.Navigate(new Uri("..\\SteamFrame\\Login.xaml", UriKind.Relative));
                        loadingGif.Visibility = Visibility.Hidden;
                        loginEnterButton.Visibility = Visibility.Visible;
                        CustomMessageBox.Show(FindResource("item38").ToString() + $" {version}.");
                    });

                    return;
                }
                catch (Exception ex) when (ex is ContentDownloaderException || ex is OperationCanceledException)
                {
                    Dispatcher.Invoke((System.Windows.Forms.MethodInvoker)delegate
                    {
                        UpdateLang();
                        Launcher.lncher.SteamFrame.Navigate(new Uri("..\\SteamFrame\\Login.xaml", UriKind.Relative));
                        loadingGif.Visibility = Visibility.Hidden;
                        loginEnterButton.Visibility = Visibility.Visible;
                        CustomMessageBox.Show(FindResource("item39").ToString() + $" \"{ex.Message}\".");
                    });

                    return;
                }
                catch (Exception er)
                {
                    Dispatcher.Invoke((System.Windows.Forms.MethodInvoker)delegate
                    {
                        UpdateLang();
                        Launcher.lncher.SteamFrame.Navigate(new Uri("..\\SteamFrame\\Login.xaml", UriKind.Relative));
                        loadingGif.Visibility = Visibility.Hidden;
                        loginEnterButton.Visibility = Visibility.Visible;
                        CustomMessageBox.Show(FindResource("item40").ToString() + $" \"{er.Message}\".");
                    });

                    return;
                }
                finally
                {
                    ContentDownloader.ShutdownSteam3();
                }
            }
            else
            {
                Debug.WriteLine("Error: InitializeSteam failed.");

                Dispatcher.Invoke((System.Windows.Forms.MethodInvoker)delegate
                {
                    loadingGif.Visibility = Visibility.Hidden;
                    loginEnterButton.Visibility = Visibility.Visible;
                });
            }

            username = "";
            password = "";

            #endregion
        }

        static bool InitializeSteam(string username, string password)
        {
            if (username != null && password == null && (!ContentDownloader.Config.RememberPassword || !AccountSettingsStore.Instance.LoginKeys.ContainsKey(username)))
            {
                do
                {
                    Console.Write("Enter account password for \"{0}\": ", username);
                    if (Console.IsInputRedirected)
                    {
                        password = Console.ReadLine();
                    }
                    else
                    {
                        // Avoid console echoing of password
                        password = Util.ReadPassword();
                    }
                    Console.WriteLine();
                } while (String.Empty == password);
            }
            else if (username == null)
            {
                Console.WriteLine("No username given. Using anonymous account with dedicated server subscription.");
            }

            // capture the supplied password in case we need to re-use it after checking the login key
            ContentDownloader.Config.SuppliedPassword = password;

            return ContentDownloader.InitializeSteam3(username, password);
        }

        static int IndexOfParam(string[] args, string param)
        {
            for (int x = 0; x < args.Length; ++x)
            {
                if (args[x].Equals(param, StringComparison.OrdinalIgnoreCase))
                    return x;
            }
            return -1;
        }

        static bool HasParameter(string[] args, string param)
        {
            return IndexOfParam(args, param) > -1;
        }

        static T GetParameter<T>(string[] args, string param, T defaultValue = default(T))
        {
            int index = IndexOfParam(args, param);

            if (index == -1 || index == (args.Length - 1))
                return defaultValue;

            string strParam = args[index + 1];

            var converter = TypeDescriptor.GetConverter(typeof(T));
            if (converter != null)
            {
                return (T)converter.ConvertFromString(strParam);
            }

            return default(T);
        }

        static List<T> GetParameterList<T>(string[] args, string param)
        {
            List<T> list = new List<T>();
            int index = IndexOfParam(args, param);

            if (index == -1 || index == (args.Length - 1))
                return list;

            index++;

            while (index < args.Length)
            {
                string strParam = args[index];

                if (strParam[0] == '-') break;

                var converter = TypeDescriptor.GetConverter(typeof(T));
                if (converter != null)
                {
                    list.Add((T)converter.ConvertFromString(strParam));
                }

                index++;
            }

            return list;
        }

        private void UsernameBoxGotFocus(object sender, RoutedEventArgs e)
        {
            usernameHint.Visibility = Visibility.Hidden;
        }

        private void UsernameBoxLostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(usernameBox.Text)) { usernameHint.Visibility = Visibility.Visible; }
        }

        private void PasswordBoxGotFocus(object sender, RoutedEventArgs e)
        {
            passwordHint.Visibility = Visibility.Hidden;
        }

        private void PasswordBoxLostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(passwordBox.Password)) { passwordHint.Visibility = Visibility.Visible; }
        }
    }
}
