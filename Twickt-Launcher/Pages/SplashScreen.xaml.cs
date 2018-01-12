﻿// Copyright (c) 2016 Twickt / Ceschia Davide
//Application idea, code and time are given by Davide Ceschia / Twickt
//You may use them according to the GNU GPL v.3 Licence
//GITHUB Project: https://github.com/killpowa/Twickt-Launcher

/*Splashscreen*/
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Resources;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Twickt_Launcher.Classes;

namespace Twickt_Launcher.Pages
{
    /// <summary>
    /// Logica di interazione per SplashScreen.xaml
    /// </summary>
    public partial class SplashScreen : Page
    {
        private CancellationTokenSource _cancellationTokenSource;
        public static Stopwatch sw = new Stopwatch();
        public string forgeJson = null;
        public static SplashScreen singleton;
        public ResourceManager manager = Properties.Resources.ResourceManager;

        public SplashScreen()
        {
            InitializeComponent();
            firstlabelprogress.Visibility = Visibility.Visible;
            //Window1.singleton.MenuToggleButton.IsEnabled = false;
            Window1.singleton.popupbox.IsEnabled = false;
            singleton = this;
            
            CultureInfo culture;
           /* if (Thread.CurrentThread.CurrentCulture.Name == "it-IT")
                culture = CultureInfo.CreateSpecificCulture("en-US");
            else*/
            culture = CultureInfo.CreateSpecificCulture("en");

            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;

            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;

            Assembly resourceAssembly = Assembly.Load("GDLauncher");
            string manifest = "Twickt_Launcher.lang.lang";
            manager = new ResourceManager(manifest, resourceAssembly);

            /*string greeting = String.Format("The current culture is {0}.\n{1}",
                                                     Thread.CurrentThread.CurrentUICulture.Name,
                                                     SplashScreen.singleton.manager.GetString("HelloString"));*/
            
           // MessageBox.Show(greeting);
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if(Properties.Settings.Default.eula == false)
                await MaterialDesignThemes.Wpf.DialogHost.Show(new Dialogs.Eula(), "RootDialog");

            Windows.DebugOutputConsole console = new Windows.DebugOutputConsole();
            firstlabelprogress.Visibility = Visibility.Hidden;
            if (Window1.singleton.started == false)
            {
                Window1.singleton.started = true;


                string update = await AutoUpdater.CheckVersion();
                await AutoUpdater.Download(update);


                //SETTING UP JAVA
                if (await JAVAInstaller.isJavaInstalled() == false)
                {
                    try
                    {
                        await JAVAInstaller.DownloadJava();
                    }
                    catch
                    {
                        MessageBox.Show("Error setting up java. If this error keeps showing up, consider contacting GorillaDevs on our website: https://gorilladevs.com");
                    }
                }

                //CHECKING FORGE JSON
                HttpWebRequest request = (HttpWebRequest)System.Net.WebRequest.Create("http://files.minecraftforge.net/maven/net/minecraftforge/forge/json");
                request.Method = "HEAD";
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                string disposition = response.Headers["Content-Length"];


                sw.Start();
                var client = new WebClient();
                client.DownloadProgressChanged += (s, ee) =>
                {
                    downloadingForgeJSONProgress.Value = ee.ProgressPercentage;
                    forgeJSONSpeed.Content = string.Format("{0} kb/s", (ee.BytesReceived / 1024d / sw.Elapsed.TotalSeconds).ToString("0.00"));
                    forgeJSONMbToDownload.Content = string.Format("{0} MB / {1} MB",
                    (ee.BytesReceived / 1024d / 1024d).ToString("0"),
                    (ee.TotalBytesToReceive / 1024d / 1024d).ToString("0"));
                };
                //VERIFICA SE IL JSON DELLE VERSIONI DI FORGE ESISTE ED E' AGGIORNATO
                if (!File.Exists(config.M_F_P + "forgeVersions.json") || Properties.Settings.Default.forgeJSONContentLength != disposition)
                {
                    downloadingForgeJSONProgress.Visibility = Visibility.Visible;
                    downloadingForgeJSON.Visibility = Visibility.Visible;
                    forgeJSONSpeed.Visibility = Visibility.Visible;
                    forgeJSONMbToDownload.Visibility = Visibility.Visible;
                    await client.DownloadFileTaskAsync("http://files.minecraftforge.net/maven/net/minecraftforge/forge/json", config.M_F_P + "forgeVersions.json");
                    Properties.Settings.Default.forgeJSONContentLength = disposition;
                    Properties.Settings.Default.Save();
                }
                using (StreamReader r = new StreamReader(config.M_F_P + "forgeVersions.json"))
                {
                    forgeJson = await r.ReadToEndAsync();
                }
                try
                {
                    dynamic json = JsonConvert.DeserializeObject(Pages.SplashScreen.singleton.forgeJson);
                }
                catch
                {
                    downloadingForgeJSONProgress.Visibility = Visibility.Visible;
                    downloadingForgeJSON.Visibility = Visibility.Visible;
                    forgeJSONSpeed.Visibility = Visibility.Visible;
                    forgeJSONMbToDownload.Visibility = Visibility.Visible;
                    await client.DownloadFileTaskAsync("http://files.minecraftforge.net/maven/net/minecraftforge/forge/json", config.M_F_P + "forgeVersions.json");
                    Properties.Settings.Default.forgeJSONContentLength = disposition;
                    Properties.Settings.Default.Save();
                }
                transition.SelectedIndex = 1;
                await Task.Delay(350);
                Window1.singleton.MainPage.Navigate(new Pages.Login());

            }
        }
    }
}
