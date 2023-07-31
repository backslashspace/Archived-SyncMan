using NetTools;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Threading;
using System.Windows;
using System.IO;
using System.Xml;

namespace SyncMan
{
    public partial class MainWindow
    {
        private void SetLocalAlias(Object sender, RoutedEventArgs e)
        {
            String Alias = Microsoft.VisualBasic.Interaction.InputBox("",
                               "Select Profile",
                               "",
                               0,
                               0);

            if (Alias == "")
            {
                return;
            }

            LocalDevice.Alias = Alias;

            GLabel.Content = "Alias: " + TrimDevInfo();

            Registry.SetValue("HKEY_LOCAL_MACHINE\\SOFTWARE\\SyncMan", "Alias", Alias, RegistryValueKind.String);

            LogBoxAdd("\nSet and saved new Alias: " + Alias, Brushes.LightBlue, FontWeights.Bold);
        }

        private void Download(Object sender, RoutedEventArgs e)
        {
            Worker("d");
        }

        private void Upload(Object sender, RoutedEventArgs e)
        {
            Worker("u");
        }

        //#################################################

        private async void Worker(String CurrentActionType)
        {
            LogBoxAdd("\n============================================", Brushes.LightGray, FontWeights.Normal);

            //read roboconfig
            LogBoxAdd("\nReading exchange config", Brushes.LightGray, FontWeights.Normal);

            GetCopyConf();

            //test if resource is on net folder 
            // -> mount 

            Boolean IsNetResource = false;
            String MountPath = null;

            if (RoboConfig.DBPath.Contains("\\\\") || RoboConfig.UploadCMD.Contains("\\\\") || RoboConfig.DownloadCMD.Contains("\\\\"))
            {
                IsNetResource = true;

                MountPath = RoboConfig.DBPath.Substring(0, RoboConfig.DBPath.Length - 7);

                await Task.Run(() => Execute.EXE("cmd.exe", "/c net use \"" + MountPath + "\"", WaitForExit: true));

                if (CurrentActionType == "u")
                {
                    LogBoxAdd("\nAttempting to connect and upload", Brushes.CadetBlue, FontWeights.Normal);
                }
                else
                {
                    LogBoxAdd("\nAttempting to connect and download", Brushes.CadetBlue, FontWeights.Normal);
                }

                LogBoxAdd("\nReading data from " + RoboConfig.DBPath, Brushes.LightGray, FontWeights.Normal);

                await Task.Run(() => ReadXML());

                LogBoxAdd(" ☑", Brushes.LightGreen, FontWeights.Normal);
            }
            else
            {
                if (CurrentActionType == "u")
                {
                    LogBoxAdd("\nAttempting to upload", Brushes.CadetBlue, FontWeights.Normal);
                }
                else
                {
                    LogBoxAdd("\nAttempting to download", Brushes.CadetBlue, FontWeights.Normal);
                }

                LogBoxAdd("\nReading data from " + RoboConfig.DBPath, Brushes.LightGray, FontWeights.Normal);

                await Task.Run(() => ReadXML());
            }

            //check status last device
            Boolean LastActionIsUpload;
            if (LastDevice.LastAction == "u")
            {
                LastActionIsUpload = true;
            }
            else
            {
                LastActionIsUpload = false;
            }

            String Dev;

            if (LastDevice.Alias != "null")
            {
                Dev = LastDevice.Alias + " (" + LastDevice.GUID + ")";
            }
            else
            {
                Dev = LastDevice.GUID;
            }

            switch (LastDevice.LastState)
            {
                case "error":
                    if (CurrentActionType == "u")
                    {
                        if (LastActionIsUpload)
                        {
                            //will upload but other is uploading
                            LogBoxAdd("\n\n[Error] Last upload from " + Dev + " finished with errors", Brushes.OrangeRed, FontWeights.Normal);
                            LogBoxAdd("\nError time: " + LastDevice.LastAccess, Brushes.OrangeRed, FontWeights.Normal);

                            ContinuePrompt("[Error] Last upload from " + Dev + " finished with errors.\nContinue uploading?", "Ätänschen", MessageBoxIcon.Warning);

                            LogBoxAdd("\n\nContinuing", Brushes.NavajoWhite, FontWeights.Normal);
                        }
                        else
                        {
                            //will upload but other is downloading
                            LogBoxAdd("\n[Info] Last download on " + Dev + " finished with errors", Brushes.Gray, FontWeights.Normal);
                        }
                    }
                    else
                    {
                        if (LastActionIsUpload)
                        {
                            //will download but other is uploading
                            LogBoxAdd("\n\n[Error] Last upload from " + Dev + " finished with errors", Brushes.OrangeRed, FontWeights.Normal);

                            ContinuePrompt("[Error] Last upload from " + Dev + " finished with errors.\nContinue downloading?", "Ätänschen", MessageBoxIcon.Warning);

                            LogBoxAdd("\n\nContinuing", Brushes.NavajoWhite, FontWeights.Normal);
                        }
                        else
                        {
                            //will download and other is dowmloading
                            LogBoxAdd("\n[Info] Last download on " + Dev + " finished with errors", Brushes.Gray, FontWeights.Normal);
                        }
                    }

                    break;

                case "running":
                    if (CurrentActionType == "u")
                    {
                        if (LastActionIsUpload)
                        {
                            //will upload but other is uploading
                            LogBoxAdd("\n\n[Important] Unfinished upload from " + Dev, Brushes.OrangeRed, FontWeights.Normal);
                            LogBoxAdd("\nStart time: " + LastDevice.LastAccess, Brushes.OrangeRed, FontWeights.Normal);

                            if (!ContinuePrompt("Unfinished or aborted upload from " + Dev + ".\nContinue?", "Ätänschen", MessageBoxIcon.Warning))
                            {
                                //update db

                                await Task.Run(() => SaveXMLFile("success"));

                                Environment.Exit(1);
                            }

                            LogBoxAdd("\n\nContinuing", Brushes.NavajoWhite, FontWeights.Normal);
                        }
                        else
                        {
                            //will upload but other is downloading
                            LogBoxAdd("\n[Info] " + Dev + " is currently downloading", Brushes.Yellow, FontWeights.Normal);

                            if (!ContinuePrompt("[Important] " + Dev + " is currently downloading.\nContinue?", "Ätänschen", MessageBoxIcon.Warning))
                            {
                                //update db

                                await Task.Run(() => SaveXMLFile("success"));

                                Environment.Exit(1);
                            }

                            LogBoxAdd("\nContinuing", Brushes.NavajoWhite, FontWeights.Normal);
                        }
                    }
                    else
                    {
                        if (LastActionIsUpload)
                        {
                            //will download but other is uploading
                            LogBoxAdd("\n[Warning] " + Dev + " is currently uploading", Brushes.Yellow, FontWeights.Normal);

                            if (!ContinuePrompt("[Important] " + Dev + " is currently uploading.\nContinue?", "Ätänschen", MessageBoxIcon.Warning))
                            {
                                //update db

                                await Task.Run(() => SaveXMLFile("success"));

                                Environment.Exit(1);
                            }

                            LogBoxAdd("\nContinuing", Brushes.NavajoWhite, FontWeights.Normal);
                        }
                        else
                        {
                            //will download and other is dowmloading
                            LogBoxAdd("\n[Info] " + Dev + " is currently downloading", Brushes.Gray, FontWeights.Normal);
                        }
                    }

                    break;
            }

            //check Int32-tegreitiy previous devices
            if (LastActionIsUpload)
            {
                if (CurrentActionType == "u" && (LastDevice.GUID != LocalDevice.GUID))
                {
                    //warn
                    //there might be a newer version on LastDevice.GUID

                    LogBoxAdd("\n[Warn] There might be a newer version on " + Dev + "\n{last action was upload, current action upload}", Brushes.Yellow, FontWeights.Normal);

                    DialogResult R = System.Windows.Forms.MessageBox.Show(
                    "[Warn] There might be a newer version on \" + Dev + \"\\n{last action was upload, current action upload}",
                    "",
                    MessageBoxButtons.OKCancel,
                    MessageBoxIcon.Warning);

                    if (R == System.Windows.Forms.DialogResult.Cancel)
                    {
                        Thread.Sleep(1000);

                        System.Environment.Exit(0);
                    }
                }
            }
            else
            {
                if (CurrentActionType == "d" && (LastDevice.GUID != LocalDevice.GUID))
                {
                    //warn
                    //Last action was download at lastdev.time
                    //cure action == download -> continue?

                    LogBoxAdd("\n[Warn] There might be a newer version on " + Dev + "\n{last action was download, current action download}", Brushes.Yellow, FontWeights.Normal);

                    DialogResult R = System.Windows.Forms.MessageBox.Show(
                    "[Warn] There might be a newer version on \" + Dev + \"\\n{last action was download, current action download}",
                    "",
                    MessageBoxButtons.OKCancel,
                    MessageBoxIcon.Warning);

                    if (R == System.Windows.Forms.DialogResult.Cancel)
                    {
                        Thread.Sleep(1000);

                        System.Environment.Exit(0);
                    }
                }
            }

            //update (status + start)
            LogBoxAdd("\nUpdating " + RoboConfig.DBPath, Brushes.LightGray, FontWeights.Normal);

            await Task.Run(() => SaveXMLFile("running"));

            LogBoxAdd("\nStarting transfer", Brushes.NavajoWhite, FontWeights.SemiBold);

            //start progress updater
            CancellationTokenSource tokenSource = new();
            Boolean CanContinue = false;
            Status(tokenSource.Token);

            if (IsNetResource)
            {
                if (CurrentActionType == "u")
                {
                    Process myProcess = new();

                    await Task.Run(() =>
                    {
                        myProcess = Process.Start("robocopy.exe", RoboConfig.UploadCMD);
                        while (!myProcess.WaitForExit(500)) ;
                    });

                    //stop status updater
                    tokenSource.Cancel();
                    while (!CanContinue)
                    {
                        await Task.Delay(500);
                    }
                    tokenSource.Dispose();

                    if (ExitcodeToString(myProcess.ExitCode))
                    {
                        LogBoxAdd("\nUpdating " + RoboConfig.DBPath, Brushes.LightGray, FontWeights.Normal);

                        await Task.Run(() => SaveXMLFile("success"));
                    }
                    else
                    {
                        LogBoxAdd("\nUpdating " + RoboConfig.DBPath, Brushes.LightGray, FontWeights.Normal);

                        await Task.Run(() => SaveXMLFile("error"));
                    }

                    await Task.Run(() => Execute.EXE("net.exe", Args: "use \"" + MountPath + "\" /d", WaitForExit: true));
                }
                else
                {
                    Process myProcess = new();

                    await Task.Run(() =>
                    {
                        using Process myProcess = Process.Start("robocopy.exe", RoboConfig.DownloadCMD);
                        while (!myProcess.WaitForExit(500)) ;
                    });

                    //stop status updater
                    tokenSource.Cancel();
                    while (!CanContinue)
                    {
                        await Task.Delay(500);
                    }
                    tokenSource.Dispose();

                    if (ExitcodeToString(myProcess.ExitCode))
                    {
                        LogBoxAdd("\nUpdating " + RoboConfig.DBPath, Brushes.LightGray, FontWeights.Normal);

                        await Task.Run(() => SaveXMLFile("success"));
                    }
                    else
                    {
                        LogBoxAdd("\nUpdating " + RoboConfig.DBPath, Brushes.LightGray, FontWeights.Normal);

                        await Task.Run(() => SaveXMLFile("error"));
                    }

                    await Task.Run(() => Execute.EXE("net.exe", Args: "use \"" + MountPath + "\" /d", WaitForExit: true));
                }
            }
            else
            {
                if (CurrentActionType == "u")
                {
                    Int32 RBEXC = await Task.Run(() =>
                    {
                        Process myProcess = Process.Start("robocopy.exe", RoboConfig.UploadCMD);
                        while (!myProcess.WaitForExit(500)) ;

                        return myProcess.ExitCode;
                    });

                    //stop status updater
                    tokenSource.Cancel();
                    while (!CanContinue)
                    {
                        await Task.Delay(500);
                    }
                    tokenSource.Dispose();

                    if (ExitcodeToString(RBEXC))
                    {
                        LogBoxAdd("\nUpdating " + RoboConfig.DBPath, Brushes.LightGray, FontWeights.Normal);

                        await Task.Run(() => SaveXMLFile("success"));
                    }
                    else
                    {
                        LogBoxAdd("\nUpdating " + RoboConfig.DBPath, Brushes.LightGray, FontWeights.Normal);

                        await Task.Run(() => SaveXMLFile("error"));
                    }
                }
                else
                {
                    Int32 RBEXC = await Task.Run(() =>
                    {
                        Process myProcess = Process.Start("robocopy.exe", RoboConfig.UploadCMD);
                        while (!myProcess.WaitForExit(500)) ;

                        return myProcess.ExitCode;
                    });

                    //stop status updater
                    tokenSource.Cancel();
                    while (!CanContinue)
                    {
                        await Task.Delay(500);
                    }
                    tokenSource.Dispose();

                    if (ExitcodeToString(RBEXC))
                    {
                        LogBoxAdd("\nUpdating " + RoboConfig.DBPath, Brushes.LightGray, FontWeights.Normal);

                        await Task.Run(() => SaveXMLFile("success"));
                    }
                    else
                    {
                        LogBoxAdd("\nUpdating " + RoboConfig.DBPath, Brushes.LightGray, FontWeights.Normal);

                        await Task.Run(() => SaveXMLFile("error"));
                    }
                }
            }

            //#### fanktions ####

            async void Status(CancellationToken ct)
            {
                await Task.Delay(500);

                Byte B = 0;

                LogBoxAdd("\n", Brushes.LightGreen, FontWeights.Normal);

                do
                {
                    if (B == 13)
                    {
                        B = 0;

                        LogBoxRemoveLine();

                        LogBoxAdd("\n#", Brushes.LightGreen, FontWeights.Normal);
                    }
                    else
                    {
                        LogBoxAdd("#", Brushes.LightGreen, FontWeights.Normal);
                    }

                    await Task.Delay(500);

                    B++;
                }
                while (!ct.IsCancellationRequested);

                LogBoxRemoveLine();

                CanContinue = true;
            }

            Boolean ExitcodeToString(Int32 Exitcode)
            {
                switch (Exitcode)
                {
                    case 0:
                        LogBoxAdd("\nSuccess [0]\nThe source and destination directory trees are completely synchronized.", Brushes.LightGreen, FontWeights.Normal);
                        return true;
                    case 1:
                        LogBoxAdd("\nSuccess [1]\nOne or more files were copied successfully (that is, new files have arrived).", Brushes.LightGreen, FontWeights.Normal);
                        return true;
                    case 2:
                        LogBoxAdd("\nSuccess [2]\nThe source and destination directory trees are synchronized.\n(Excluding excluded files)", Brushes.LightGreen, FontWeights.Normal);
                        return true;
                    case 3:
                        LogBoxAdd("\nSuccess [3 (2+1)]\nSome files were copied. Additional files were present (excluded files?).\nNo failure was encountered.", Brushes.LightGreen, FontWeights.Normal);
                        return true;
                    case 4:
                        LogBoxAdd("\n[4] Some Mismatched files or directories were detected.", Brushes.Yellow, FontWeights.Normal);
                        return true;
                    case 5:
                        LogBoxAdd("\n[5 (4+1)] Some files were copied. Some files were mismatched. No failure was encountered.", Brushes.Yellow, FontWeights.Normal);
                        return true;
                    case 6:
                        LogBoxAdd("\n[6 (4+2)] Additional files and mismatched files exist. No files were copied and no failures were encountered.This means that the files already exist in the destination directory.", Brushes.Yellow, FontWeights.Normal);
                        return true;
                    case 7:
                        LogBoxAdd("\n[7 (4+1+2)] Files were copied, a file mismatch was present, and additional files were present.", Brushes.Yellow, FontWeights.Normal);
                        return true;
                    case 8:
                        LogBoxAdd("\n[8] Some files or directories could not be copied (copy errors occurred and the retry limit was exceeded).", Brushes.Yellow, FontWeights.Normal);
                        return false;
                    case 16:
                        LogBoxAdd("\n[16] Some files or directories could not be copied (copy errors occurred and the retry limit was exceeded).", Brushes.OrangeRed, FontWeights.Normal);
                        return false;
                    default:
                        LogBoxAdd("\nUnknown exit code: " + Exitcode, Brushes.OrangeRed, FontWeights.Normal);
                        return false;
                }
            }

            Boolean ContinuePrompt(String Message, String Title, MessageBoxIcon Icon)
            {
                DialogResult R = System.Windows.Forms.MessageBox.Show(
                        Message,
                        Title,
                        MessageBoxButtons.YesNo,
                        Icon);

                if (R == System.Windows.Forms.DialogResult.Yes)
                {
                    return true;
                }

                LogBoxAdd("\n\nExiting", Brushes.Yellow, FontWeights.Normal);

                for (Byte b = 0; b < 3; b++)
                {
                    LogBoxAdd(".", Brushes.LightGray, FontWeights.Normal);
                }

                return false;
            }

            void ReadXML()
            {
                PreAction(CurrentActionType);
            }
        }

        //###################################################################################

        static String TrimDevInfo()
        {
            String Out;

            if (LocalDevice.Alias.Length > 36)
            {
                Out = LocalDevice.Alias.Substring(0, 36) + "...";
            }
            else
            {
                Out = LocalDevice.Alias;
            }

            return Out;
        }

        //

        private void CreateConfigTemplate()
        {
            try
            {
                using StreamWriter writer = new("config.txt");
                writer.WriteLine("DatabaseFilePath=\\\\localhost\\Master\\4. Repository\\db.xml");
                writer.WriteLine("#robocopy");
                writer.WriteLine("UploadCommand=\"C:\\Users\\dev0\\source\\repos\" \"\\\\localhost\\master\\4. Repository\" *.* /MIR /xo /w:3 /np /v /sl /e /b /MT:12 /compress");
                writer.WriteLine("DownloadCommand=\"\\\\localhost\\master\\4. Repository\" \"C:\\Users\\dev0\\source\\repos\" *.* /MIR /xo /w:3 /np /v /sl /e /b /MT:12 /compress");

                writer.Close();
                writer.Dispose();
            }
            catch
            { }
        }

        private void GetConfig()
        {
            //check if roboconfig exists
            if (!File.Exists("config.txt"))
            {
                CreateConfigTemplate();

                if (!File.Exists("config.txt"))
                {
                    LogBoxAdd("Could not create config file\n\nExiting...", Brushes.OrangeRed, FontWeights.Bold);

                    System.Environment.Exit(1);
                }

                LogBoxAdd("[Info] Generated config file (config.txt)\n", Brushes.LightBlue, FontWeights.Bold);
            }

            //try to load device info
            LocalDevice.GUID = RegistryIO.GetValue("HKEY_LOCAL_MACHINE\\SOFTWARE\\SyncMan", "GUID", RegistryValueKind.String, true);
            LocalDevice.Alias = RegistryIO.GetValue("HKEY_LOCAL_MACHINE\\SOFTWARE\\SyncMan", "Alias", RegistryValueKind.String, true);
            LocalDevice.Alias ??= "null";

            //get or gen guid
            if (!Guid.TryParse(LocalDevice.GUID, out _))
            {
                LocalDevice.GUID = Guid.NewGuid().ToString();

                Registry.SetValue("HKEY_LOCAL_MACHINE\\SOFTWARE\\SyncMan", "GUID", LocalDevice.GUID, RegistryValueKind.String);

                LogBoxAdd("[Info] Generated and saved new local guid", Brushes.LightBlue, FontWeights.Bold);
            }
            else
            {
                LogBoxAdd("Loaded GUID: " + LocalDevice.GUID, Brushes.LightGreen, FontWeights.Bold);
            }

            //set guid in gui
            if (LocalDevice.Alias != "" && LocalDevice.Alias != "null")
            {
                GLabel.Content = "Alias: " + TrimDevInfo();
            }
            else
            {
                GLabel.Content = "GUID: " + LocalDevice.GUID;
            }
        }

        private void PreAction(String UDAction)
        {
            //init device list
            XMLDevices = new();

            XmlDocument XMLFile = new();

            //get time
            ExecTime = DateTimeOffset.Now.ToUnixTimeSeconds().ToString();

            try
            {
                XMLFile.Load(RoboConfig.DBPath);

                try
                {
                    //get total devices in list
                    Int32.TryParse(XMLFile.DocumentElement.SelectSingleNode("/Devices/Info/TotalDevices").InnerText, out Int32 TD);

                    if (TD == 0)
                    {
                        throw new Exception("xml file: device count not found");
                    }
                    else
                    {
                        TotalXMLDevices = TD;
                    }

                    //get info on last xml device
                    LastDevice.GUID = XMLFile.DocumentElement.SelectSingleNode("/Devices/Info/LastAccessByGUID").InnerText;
                    LastDevice.Alias = XMLFile.DocumentElement.SelectSingleNode("/Devices/Info/LastAccessByAlias").InnerText;
                    try
                    {
                        LastDevice.LastAccess = DateTimeOffset.FromUnixTimeSeconds(Int64.Parse(XMLFile.DocumentElement.SelectSingleNode("/Devices/Info/LastAccessTime").InnerText)).LocalDateTime.ToString();
                    }
                    catch
                    {
                        LastDevice.LastAccess = "null";
                    }
                    LastDevice.LastAction = XMLFile.DocumentElement.SelectSingleNode("/Devices/Info/LastAction").InnerText;
                    LastDevice.LastState = XMLFile.DocumentElement.SelectSingleNode("/Devices/Info/LastState").InnerText;

                    //populate XMLDevices list
                    for (Int32 i = 0; i < TD; i++)
                    {
                        Device temp = new()
                        {
                            GUID = XMLFile.DocumentElement.SelectSingleNode("/Devices/Dev" + i + "/GUID").InnerText,
                            Alias = XMLFile.DocumentElement.SelectSingleNode("/Devices/Dev" + i + "/Alias").InnerText,
                            LastAccess = XMLFile.DocumentElement.SelectSingleNode("/Devices/Dev" + i + "/LastAccess").InnerText,
                            LastAction = XMLFile.DocumentElement.SelectSingleNode("/Devices/Dev" + i + "/LastAction").InnerText
                        };

                        XMLDevices.Add(temp);
                    }

                    //check if this exec device is in list [update]
                    for (Int32 i = 0; i < TD; i++)
                    {
                        if (XMLDevices[i].GUID == LocalDevice.GUID)
                        {
                            IndexInXMLFile = i;

                            XMLDevices[i].Alias = LocalDevice.Alias;
                            XMLDevices[i].LastAccess = ExecTime;
                            XMLDevices[i].LastAction = UDAction;

                            return;
                        }
                    }

                    AddCurrentToMEMxml();

                    TotalXMLDevices++;
                }
                catch
                {
                    System.Windows.Forms.MessageBox.Show(
                        "Invalid XML data.",
                        "Ahh",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);

                    throw new Exception("AHHHHHHHHHH (L~700)");
                }
            }
            catch (System.ArgumentException)
            {
                Exit("Invalid filepath for db.xml");
            }
            catch (FileNotFoundException)
            {
                //when xml not found

                CreateData();
            }
            catch (System.UnauthorizedAccessException)
            {
                //no perms

                Exit("Could not access SYNC path: UnauthorizedAccessException");
            }
            catch (System.IO.DirectoryNotFoundException)
            {
                //when dir not found

                Exit("Could not access SYNC path: DirectoryNotFoundException");
            }
            catch (System.IO.IOException)
            {
                //Path resource not found
                //smb path not accesinble
                //access

                Exit("Could not access resource on path");
            }
            catch (System.Xml.XmlException)
            {
                //not an xml file

                DialogResult R = System.Windows.Forms.MessageBox.Show(
                        "Invalid XML fileformat\nOverwrite file?",
                        "Error",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Error);

                if (R == System.Windows.Forms.DialogResult.Yes)
                {
                    CreateData();
                }
                else
                {
                    Exit();
                }
            }

            void CreateData()
            {
                TotalXMLDevices = 1;

                Device temp = new()
                {
                    GUID = LocalDevice.GUID,
                    Alias = LocalDevice.Alias,
                    LastAccess = ExecTime,
                    LastAction = UDAction
                };

                XMLDevices.Add(temp);

                IndexInXMLFile = 0;
            }

            void Exit(String Message = null)
            {
                if (Message != null)
                {
                    System.Windows.Forms.MessageBox.Show(
                        Message,
                        "Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }

                System.Environment.Exit(1);
            }

            void AddCurrentToMEMxml()
            {
                Device temp = new()
                {
                    GUID = LocalDevice.GUID,
                    Alias = LocalDevice.Alias,
                    LastAccess = ExecTime,
                    LastAction = UDAction
                };

                XMLDevices.Add(temp);

                IndexInXMLFile = TotalXMLDevices;
            }
        }

        private void GetCopyConf()
        {
            Boolean HasFailed = false;
        redo:

            try
            {
                String[] Lines = File.ReadAllLines("config.txt");

                RoboConfig.DBPath = Lines[0].Substring(17);
                RoboConfig.UploadCMD = Lines[2].Substring(14);
                RoboConfig.DownloadCMD = Lines[3].Substring(16);
            }
            catch
            {
                if (HasFailed)
                {
                    Dispatcher.Invoke(new Action(() => { LogBoxAdd("\n\nCould not create file\nExiting..", Brushes.OrangeRed, FontWeights.Normal); }));

                    Thread.Sleep(1000);

                    System.Environment.Exit(0);
                }
                else
                {
                    DialogResult R = System.Windows.Forms.MessageBox.Show(
                        "Error reading config file (config.txt)\nCreate config template?",
                        "Error",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Error);

                    if (R == System.Windows.Forms.DialogResult.Yes)
                    {
                        try
                        {
                            Dispatcher.Invoke(new Action(() => { LogBoxAdd("\nAttempting to delete old config file ", Brushes.LightGray, FontWeights.Normal); }));
                            File.Delete("config.txt");

                            Dispatcher.Invoke(new Action(() => { LogBoxAdd("\nAttempting to create new template", Brushes.LightGray, FontWeights.Normal); }));
                            CreateConfigTemplate();

                            HasFailed = true;
                            goto redo;
                        }
                        catch
                        {
                            Dispatcher.Invoke(new Action(() => { LogBoxAdd("\nAn error occurred while deleting the file\nExiting..", Brushes.DarkRed, FontWeights.SemiBold); }));
                        }
                    }
                    else
                    {
                        Dispatcher.Invoke(new Action(() => { LogBoxAdd("\n\nExiting..", Brushes.OrangeRed, FontWeights.Normal); }));

                        Thread.Sleep(1000);

                        System.Environment.Exit(0);
                    }
                }
            }
        }

        private static void SaveXMLFile(String Status)
        {
            if (Status != "running" && Status != "success" && Status != "error") { throw new Exception("Invalid argument: " + Status); }

            //write xml file
            XmlWriterSettings settings = new()
            {
                Indent = true
            };

            XmlWriter writer = XmlWriter.Create(RoboConfig.DBPath, settings);

            writer.WriteStartDocument();
            writer.WriteStartElement("Devices");

            //Info
            writer.WriteStartElement("Info");
            writer.WriteElementString("TotalDevices", TotalXMLDevices.ToString());
            writer.WriteElementString("LastAccessByGUID", LocalDevice.GUID);
            writer.WriteElementString("LastAccessByAlias", LocalDevice.Alias);
            writer.WriteElementString("LastAccessTime", ExecTime);
            writer.WriteElementString("LastAction", XMLDevices[IndexInXMLFile].LastAction);
            writer.WriteElementString("LastState", Status);
            writer.WriteEndElement();

            //put XMLDevices
            for (Int32 i = 0; i < XMLDevices.Count; i++)
            {
                writer.WriteStartElement("Dev" + i);
                writer.WriteElementString("GUID", XMLDevices[i].GUID);
                writer.WriteElementString("Alias", XMLDevices[i].Alias);
                writer.WriteElementString("LastAccess", XMLDevices[i].LastAccess);
                writer.WriteElementString("LastAction", XMLDevices[i].LastAction);
                writer.WriteEndElement();
            }

            writer.WriteEndElement();
            writer.WriteEndDocument();
            writer.Flush();
            writer.Close();
        }
    }
}