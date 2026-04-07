// This fucking sucks. the text doesn't work, the uninstall shit barely works and it's just so scuffed overall
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using System.Security.Principal;

namespace BubbaBloxLauncher
{
    public partial class Launcher : Form
    {
        private readonly string placeId;
        private readonly string ticket;
        private readonly string clientlocation;
        private readonly string Client2018Path;
        private readonly string Client2020Path;
        private readonly string ClientZIP2018;
        private readonly string ClientZIP2020;
        private bool Installing = false;
        private string TargetYear;
        private bool Use2020Menu = false;
        private string TargetEXEPath;
        private readonly string tbexe;
        private readonly string cfgpath;
        private readonly string launcherpath;
        private bool FirstRun = false;
        private bool NeedsRegistration = false;

        private ProgressBar progress;
        private Label status;

        public Launcher(string placeId, string ticket, string year, bool Use2020Menu = false)
        {
            this.placeId = placeId;
            this.ticket = ticket;
            this.TargetYear = year
            this.Use2020Menu = Use2020Menu;

            clientlocation = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Throback");
            Client2018Path = Path.Combine(clientlocation, "2018M");
            Client2020Path = Path.Combine(clientlocation, "2020L");
            ClientZIP2018 = Path.Combine(clientlocation, "client2018.zip");
            ClientZIP2020 = Path.Combine(clientlocation, "client2020.zip");
            tbexe = Path.Combine(clientlocation, "TBPlayerBeta.exe");
            cfgpath = Path.Combine(clientlocation, "launcher.cfg");
            launcherpath = Path.Combine(clientlocation, "ThrobackLauncher.exe");

            if (TargetYear == "2020")
            {
                TargetEXEPath = Path.Combine(Client2020Path, "TBPlayerBeta.exe");
            }
            else (TargetYear == "2018")
            {
                TargetEXEPath = Path.Combine(Client2018Path, "TBPlayerBeta.exe");
            }

            IsFirstRun();
            CheckProtocolRegistration();
            Init();
            Start();
        }

        private void IsFirstRun()
        {
            if (!Directory.Exists(clientlocation))
            {
                Directory.CreateDirectory(clientlocation);
                FirstRun = true;
                return;
            }

            if (!File.Exists(cfgpath))
            {
                FirstRun = true;
            }
            else
            {
                try
                {
                    var content = File.ReadAllText(cfgpath);
                    FirstRun = !content.Contains("installed=true");
                }
                catch
                {
                    FirstRun = true;
                }
            }
        }

        private void CheckProtocolRegistration()
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Classes\tbclient"))
            {
                NeedsRegistration = key == null;
            }
        }

        private void Init()
        {
            this.Text = "Throback Launcher";
            this.ClientSize = new System.Drawing.Size(400, 200);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = System.Drawing.Color.White;
            this.StartPosition = FormStartPosition.CenterScreen;

            var logo = new PictureBox
            {
                SizeMode = PictureBoxSizeMode.Zoom,
                Width = 200,
                Height = 100,
                Top = 20,
                Left = (this.ClientSize.Width - 200) / 2,
                Image = Throback.Properties.Resources.tblogo
            };
            this.Controls.Add(logo);

            progress = new ProgressBar
            {
                Top = 145,
                Left = 20,
                Width = this.ClientSize.Width - 40,
                Height = 20,
                Style = ProgressBarStyle.Continuous
            };
            this.Controls.Add(progress);

            status = new Label
            {
                Top = 100,
                Left = 20,
                Width = this.ClientSize.Width - 40,
                Height = 60,
                TextAlign = System.Drawing.ContentAlignment.MiddleCenter,
                Text = FirstRun ? "Installing Throback..." : "Launching Throback..."
            };
            this.Controls.Add(status);

            var cancel = new Button
            {
                Text = "Cancel",
                Top = 170,
                Left = (this.ClientSize.Width - 80) / 2,
                Width = 80,
                Height = 25
            };
            cancel.Click += (s, e) => 
            {
                if (Installing)
                    Cancel();
                else
                    this.Close();
            };
            this.Controls.Add(cancel);
        }

        private async void Start()
        {
            try
            {
                this.Text = "Throback Installer";
                this.Show();
                this.Refresh();
                await Task.Delay(100);

                bool Needs2018Install = !File.Exists(Path.Combine(Client2018Path, "TBPlayerBeta.exe")) || FirstRun;
                bool Needs2020Install = !File.Exists(Path.Combine(Client2020Path, "TBPlayerBeta.exe")) || FirstRun;

                Installing = Needs2018Install || Needs2020Install;

                if (Needs2018Install || Needs2020Install)
                {
                    if (Needs2018Install)
                    {
                        status.Text = "Downloading Throback (2018)...";
                        this.Refresh();
                        await DownloadVersion("2018");
                    }

                    if (Needs2020Install)
                    {
                        status.Text = "Downloading Throback (2020)...";
                        this.Refresh();
                        await DownloadVersion("2020");
                    }

                    if (!Directory.Exists(clientlocation))
                    {
                        Directory.CreateDirectory(clientlocation);
                    }

                    File.WriteAllText(cfgpath, "installed=true");

                    if (FirstRun)
                    {
                        ShowCompleteMsg();
                    }
                }

                if (!string.IsNullOrEmpty(placeId) && !string.IsNullOrEmpty(ticket))
                {
                    Launch();
                }
                else if (!FirstRun && !(Needs2018Install || Needs2020Install))
                {
                    ShowInstalledMessage();
                }
                else
                {
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
                this.Close();
            }
        }

        private void ShowCompleteMsg()
        {
            this.Invoke((MethodInvoker)(() =>
            {
                progress.Style = ProgressBarStyle.Continuous;
                progress.Value = 100;
            }));

            status.Text = "";
            MessageBox.Show("Throback installed! You can now join games.",
                          "Throback",
                          MessageBoxButtons.OK,
                          MessageBoxIcon.Information);
        }

        private void ShowInstalledMessage()
        {
            var result = MessageBox.Show("Would you like to uninstall Throback?",
                                       "Throback",
                                       MessageBoxButtons.YesNo,
                                       MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                Task.Run(() =>
                {
                    Uninstall();
                });
            }
            else
            {
                this.Close();
            }
        }
        private void Cancel()
        {
            try
            {
                var processes = Process.GetProcesses();
                int currentId = Process.GetCurrentProcess().Id;
                foreach (var proc in processes)
                {
                    try
                    {
                        string processName = proc.ProcessName.ToLower();

                        if (processName == "tbplayerbeta" || processName == "throback")
                        {
                            if (proc.Id != currentId)
                            {
                                proc.Kill();
                            }
                            Thread.Sleep(100);
                        }
                    }
                    catch
                    {
                    }
                }

                try
                {
                    Registry.CurrentUser.DeleteSubKeyTree(@"Software\Classes\tbclient", false);
                }
                catch { }

                string localapppath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string tbfolder = Path.Combine(localapppath, "Throback");

                if (Directory.Exists(tbfolder))
                {
                    for (int i = 0; i < 5; i++)
                    {
                        try
                        {
                            Directory.Delete(tbfolder, true);
                            break;
                        }
                        catch
                        {
                            Thread.Sleep(500);
                            foreach (var proc in Process.GetProcessesByName("TBPlayerBeta"))
                            {
                                try { proc.Kill(); } catch { }
                            }
                            foreach (var proc in Process.GetProcessesByName("Throback"))
                            {
                                try { proc.Kill(); } catch { }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
            }

            Application.Exit();
        }

        private void Uninstall()
        {
            try
            {
                this.Invoke((MethodInvoker)(() =>
                {
                    progress.Style = ProgressBarStyle.Marquee;
                    progress.MarqueeAnimationSpeed = 30;
                    status.Text = $"Uninstalling Throback...";
                }));

                var processes = Process.GetProcesses();
                int currentId = Process.GetCurrentProcess().Id;
                foreach (var proc in processes)
                {
                    try
                    {
                        string processName = proc.ProcessName.ToLower();

                        if (processName == "tbplayerbeta" || processName == "throback")
                        {
                            if (proc.Id != currentId)
                            {
                                proc.Kill();
                            }
                            Thread.Sleep(100);
                        }
                    }
                    catch
                    {
                    }
                }

                try
                {
                    Registry.CurrentUser.DeleteSubKeyTree(@"Software\Classes\tbclient", false);
                }
                catch { }

                string localapppath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string tbfolder = Path.Combine(localapppath, "Throback");

                if (Directory.Exists(tbfolder))
                {
                    for (int i = 0; i < 5; i++)
                    {
                        try
                        {
                            Directory.Delete(tbfolder, true);
                            break;
                        }
                        catch
                        {
                            Thread.Sleep(500);
                            foreach (var proc in Process.GetProcessesByName("TBPlayerBeta"))
                            {
                                try { proc.Kill(); } catch { }
                            }
                            foreach (var proc in Process.GetProcessesByName("Throback"))
                            {
                                try { proc.Kill(); } catch { }
                            }
                        }
                    }
                }

                MessageBox.Show("Throback has been uninstalled successfully.",
                            "Throback",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to uninstall: {ex.Message}",
                              "Throback - Error",
                              MessageBoxButtons.OK,
                              MessageBoxIcon.Error);
            }

            Application.Exit();
        }

        // does this need admin?
        private void RegisterProtocol()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(@"Software\Classes\tbclient"))
                {
                    key.SetValue("", "URL:Throback Protocol");
                    key.SetValue("URL Protocol", "");

                    using (RegistryKey defaultIcon = key.CreateSubKey("DefaultIcon"))
                    {
                        defaultIcon.SetValue("", launcherpath + ",1");
                    }

                    using (RegistryKey commandKey = key.CreateSubKey(@"shell\open\command"))
                    {
                        commandKey.SetValue("", "\"" + launcherpath + "\" \"%1\"");
                    }
                }

                NeedsRegistration = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to register protocol: {ex.Message}\n\nYou may need to make tbclient:// links work or try running the installer again.",
                              "Throback - Error",
                              MessageBoxButtons.OK,
                              MessageBoxIcon.Error);
            }
        }

        private async Task DownloadVersion(string version)
        {
            using (var client = new WebClient())
            {
                string ZipURL = version == "2020" ?
                    "https://zawg.ca/assets/ecsr/bubbaclient2020.zip?v=15" :
                    version == "2018" ?
                    "https://zawg.ca/assets/ecsr/bubbaclient2018.zip?v=15" :

                string Target = version == "2020" ? ClientZIP2020 :
                                   version == "2018" ? ClientZIP2018 :

                string Extract = version == "2020" ? Client2020Path :
                                     version == "2018" ? Client2018Path :

                this.Invoke((MethodInvoker)(() =>
                {
                    progress.Style = ProgressBarStyle.Continuous;
                    progress.Minimum = 0;
                    progress.Maximum = 100;
                    progress.Value = 0;
                }));

                client.DownloadProgressChanged += (s, e) =>
                {
                    this.Invoke((MethodInvoker)(() =>
                    {
                        progress.Value = Math.Min(e.ProgressPercentage, 100);
                        status.Text = $"Downloading Throback ({version})... {e.ProgressPercentage}%";
                    }));
                };

                await client.DownloadFileTaskAsync(ZipURL, Target);

                this.Invoke((MethodInvoker)(() =>
                {
                    progress.Value = 100;
                }));


                this.Invoke((MethodInvoker)(() =>
                {
                    progress.Style = ProgressBarStyle.Marquee;
                    progress.MarqueeAnimationSpeed = 30;
                    status.Text = $"Extracting Throback ({version})...";
                }));

                try
                {
                    if (File.Exists(Target))
                    {
                        if (!Directory.Exists(Extract))
                        {
                            Directory.CreateDirectory(Extract);
                        }

                        await Task.Run(() =>
                        {
                            ZipFile.ExtractToDirectory(Target, Extract, true);
                        });
                        File.Delete(Target);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Extraction failed for {version}: {ex.Message}");
                    throw;
                }
            }
        }

        private void Launch()
        {
            try
            {
                if (!File.Exists(TargetEXEPath))
                {
                    MessageBox.Show($"Client not found at: {TargetEXEPath}", "Throback - Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                progress.Style = ProgressBarStyle.Marquee;
                progress.MarqueeAnimationSpeed = 30;
                status.Text = "Starting Throback...";
                this.Refresh();
                // we should make this dynamic in the future, like in the bbclient:// thing should have  the url
                string joinUrl = TargetYear == "2020" ?
                    $"http://thrbck.lol/game/PlaceLauncher.ashx?placeid={placeId}&ticket={ticket}&2020=true" :
                    TargetYear == "2018" ?
                    $"http://thrbck.lol/game/PlaceLauncher.ashx?placeid={placeId}&ticket={ticket}&2018=true" :
                    $"http://thrbck.lol/game/PlaceLauncher.ashx?placeid={placeId}&ticket={ticket}";

                string arguments = $"-a \"http://thrbck.lol/Login/Negotiate.ashx\" -j \"{joinUrl}\" -t \"{ticket}\"";

                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = TargetEXEPath,
                    Arguments = arguments,
                    UseShellExecute = true,
                    WorkingDirectory = Path.GetDirectoryName(TargetEXEPath)
                };

                var process = Process.Start(startInfo);

                if (process != null)
                {
                    Console.WriteLine("Joined game");

                    Task.Run(() =>
                    {
                        process.WaitForInputIdle(3000);
                        FocusGameWindow(process);

                        Thread.Sleep(1000);
                        this.Invoke((MethodInvoker)delegate
                        {
                            if (!this.IsDisposed)
                                this.Close();
                        });
                    });
                }
                else
                {
                    MessageBox.Show($"Failed to launch Throback (Process returned nothing)", "Throback - Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to launch Throback at: {TargetEXEPath}\n\nError: {ex.Message}", "Throback - Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.Close();
            }
        }

        private void FocusGameWindow(Process gameProcess)
        {
            try
            {
                int attempts = 0;
                while (attempts < 10)
                {
                    if (gameProcess.HasExited)
                        return;

                    gameProcess.Refresh();
                    if (gameProcess.MainWindowHandle != IntPtr.Zero)
                        break;

                    Thread.Sleep(100);
                    attempts++;
                }

                if (gameProcess.MainWindowHandle != IntPtr.Zero)
                {
                    NativeMethods.SetForegroundWindow(gameProcess.MainWindowHandle);
                }
                else
                {
                    FocusAnyGameWindow();
                }
            }
            catch
            {
                // ignore
            }
        }

        private void FocusAnyGameWindow()
        {
            try
            {
                var processes = Process.GetProcesses();
                foreach (var proc in processes)
                {
                    try
                    {
                        string processName = proc.ProcessName.ToLower();
                        if ((processName == "TBPlayerBeta" || processName == "throback") &&
                            proc.MainWindowHandle != IntPtr.Zero)
                        {
                            NativeMethods.SetForegroundWindow(proc.MainWindowHandle);
                            break;
                        }
                    }
                    catch
                    {
                        // ignore
                    }
                }
            }
            catch
            {
                // ignore
            }
        }

        internal static class NativeMethods
        {
            [DllImport("user32.dll")]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool SetForegroundWindow(IntPtr hWnd);
        }

        static class Program
        {
            [STAThread]
            static void Main(string[] args)
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                EnsureExistence();

                string placeId = null;
                string ticket = null;
                string year;
                bool Use2020Menu = false;

                if (args.Length > 0)
                {
                    string uri = args[0];
                    if (uri.StartsWith("tbclient://"))
                    {
                        var query = new Uri(uri).Query;
                        if (!string.IsNullOrEmpty(query))
                        {
                            var parameters = System.Web.HttpUtility.ParseQueryString(query);
                            placeId = parameters["place"];
                            ticket = parameters["ticket"];
                            year = parameters["year"];

                            string menuParam = parameters["2020menu"];
                            if (!string.IsNullOrEmpty(menuParam))
                            {
                                Use2020Menu = menuParam.ToLower() == "true";
                            }
                        }
                    }
                }

                Application.Run(new Launcher(placeId, ticket, year, Use2020Menu));
            }

            private static void EnsureExistence()
            {
                string appdata = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Throback");
                string launcher = Path.Combine(appdata, "ThrobackLauncher.exe");

                if (!Directory.Exists(appdata))
                {
                    Directory.CreateDirectory(appdata);
                }

                string EXE = Process.GetCurrentProcess().MainModule.FileName;
                if (!File.Exists(launcher) || File.GetLastWriteTime(EXE) > File.GetLastWriteTime(launcher))
                {
                    try
                    {
                        File.Copy(EXE, launcher, true);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"failed to copy launcher: {ex.Message}");
                    }
                }
            }
        }
    }
}