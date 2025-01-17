﻿using H1EMU_Launcher.Resources;
using System;
using System.Windows;
using System.Windows.Controls;

namespace H1EMU_Launcher
{
    /// <summary>
    /// Interaction logic for DownloadComplete.xaml
    /// </summary>

    public partial class DownloadComplete : Page
    {
        public DownloadComplete()
        {
            InitializeComponent();

            Resources.MergedDictionaries.Clear();

            //Adds the correct language file to the resource dictionary and then load it.
            Resources.MergedDictionaries.Add(SetLanguageFile.LoadFile());
        }

        private void BackToLoginPage(object sender, RoutedEventArgs e)
        {
            Launcher.lncher.SteamFrame.Navigate(new Uri("..\\SteamFrame\\Login.xaml", UriKind.Relative));
        }
    }
}
