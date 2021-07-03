using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using Microsoft.WindowsAPICodePack.Dialogs;
using SharpAdbClient;
using SharpAdbClient.DeviceCommands;

namespace EnableGPlayWithPC
{
    public partial class Main : Form
    {
        public Main()
        {
            InitializeComponent();

            var appDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            // それぞれのTextBoxにデフォルトのパスを入れておく
            FileSelector_Vending.Init(Path.Combine(appDir, Apks.Vending));
            FileSelector_GMS.Init(Path.Combine(appDir, Apks.GMS));
            FileSelector_GSF.Init(Path.Combine(appDir, Apks.GSF));
            FileSelector_GSFLogin.Init(Path.Combine(appDir, Apks.GSFLogin));
        }

        private async void Button_Process_Click(object sender, EventArgs e)
        {
            var progressBarDialog = new Progress();
            progressBarDialog.Title = "処理中";
            progressBarDialog.Message = "初期化中";
            progressBarDialog.Value = 0;
            progressBarDialog.Show();

            foreach (var path in GetSelectedPath())
            {
                if (!File.Exists(path))
                {
                    Dialog.Error(string.Format(Properties.Resources.Dialog_404_Inst, path),
                        string.Format(Properties.Resources.Dialog_404_Desc, path), Handle);
                    return;
                }
                progressBarDialog.Value = progressBarDialog.Value + 1;
            }

            var appDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            progressBarDialog.Value = progressBarDialog.Value + 1;

            var adb = new AdbServer();
            progressBarDialog.Value = progressBarDialog.Value + 1;

            try
            {
                var result = adb.StartServer(Path.Combine(appDir, Properties.Resources.AdbPath), true);
                progressBarDialog.Value = progressBarDialog.Value + 1;
            }
            catch (Exception)
            {
                Dialog.Error(Properties.Resources.Dialog_Adb404_Inst,
                    Properties.Resources.Dialog_Adb404_Desc, Handle);
                return;
            }

#if !DEBUG
            try
            {
#endif
                var device = AdbClient.Instance.GetDevices().First();
                progressBarDialog.Value = progressBarDialog.Value + 1;
                
                if (AdbClient.Instance.GetDevices().Count > 1)
                {
                    Dialog.Error(Properties.Resources.Dialog_TooManyDevices_Inst,
                        Properties.Resources.Dialog_TooManyDevices_Desc, Handle);
                    return;
                }
                progressBarDialog.Value = progressBarDialog.Value + 1;

                {
                    var receiver = new ConsoleOutputReceiver();
                    progressBarDialog.Value = progressBarDialog.Value + 1;

                    AdbClient.Instance.ExecuteRemoteCommand($"getprop ro.build.product", device, receiver);
                    var product = receiver.ToString();
                    product = product.Substring(0, product.Length - 2); // 余計な改行は入れさせない
                    progressBarDialog.Value = progressBarDialog.Value + 1;

                    Console.WriteLine(product.Length);

                    if (!BenesseTabs.Names.Contains(product))
                    { // 出力が名前にあるか確認
                        var result =
                            Dialog.ShowQuestion(Properties.Resources.Dialog_Not_Benesse_Tab_Inst,
                            string.Format(Properties.Resources.Dialog_Not_Benesse_Tab_Desc, product), Handle);

                        if (result != TaskDialogResult.Ok) return;
                    }
                    progressBarDialog.Value = progressBarDialog.Value + 1;
                }

                var packageManager = new PackageManager(device);
                progressBarDialog.Value = progressBarDialog.Value + 1;

                // それぞれアンインストール
                foreach (var pkg in Packages.PackageNames)
                {
                    try
                    {
                        progressBarDialog.Message = pkg + "をアンインストール中";
                        packageManager.UninstallPackage(pkg);
                        progressBarDialog.Value = progressBarDialog.Value + 10;
                    }
                    catch (Exception)
                    {

                    }
                }

                // パスを取得
                var apks = GetSelectedPath();
                progressBarDialog.Value = progressBarDialog.Value + 10;

                // それぞれインストール
                var ip = 1;
                progressBarDialog.Message = "インストール中 (" +ip+ "/4)";
                await Task.Delay(1000);
                Array.ForEach(apks, apk => {
                    packageManager.InstallPackage(apk, false);
                    ip++;
                    progressBarDialog.Value = progressBarDialog.Value + 10;
                });

                // Play ストアに権限付与
                progressBarDialog.Message = "Google Playに権限を付与中";
                {
                    var result = AndroidDebugBridgeUtils.GrantPermissions(Packages.Vending,
                            Permissions.Vending,
                            device,
                            Handle);
                    if (!result)
                    {
                        return;
                    }
                }
                progressBarDialog.Value = progressBarDialog.Value + 10;

                // GooglePlay開発者サービスに権限付与
                progressBarDialog.Message = "GMSに権限を付与中";
                {
                    var result = AndroidDebugBridgeUtils.GrantPermissions(Packages.GMS,
                            Permissions.GMS,
                            device,
                            Handle);
                    if (!result)
                    {
                        return;
                    }
                }
                progressBarDialog.Value = progressBarDialog.Value + 4;

                // Google Service Frameworkに権限付与。
                progressBarDialog.Message = "GFSに権限を付与中";
                {
                    var result = AndroidDebugBridgeUtils.GrantPermissions(Packages.GSF,
                            Permissions.GSF,
                            device,
                            Handle);
                    if (!result)
                    {
                        return;
                    }
                }
                progressBarDialog.Value = progressBarDialog.Value + 4;

                // もういちどGMSをインストール。
                progressBarDialog.Message = "最終処理中";
                packageManager.InstallPackage(FileSelector_GMS.GetPath(), true);
                progressBarDialog.Value = 100;

                progressBarDialog.Close();

#if !DEBUG
            }
            catch (Exception)
            {
                Dialog.Error(Properties.Resources.Dialog_UnableToConnect_Inst,
                    Properties.Resources.Dialog_UnableToConnect_Desc, this.Handle);
                return;
            }
#endif

        var dialog = new TaskDialog();
                dialog.Caption = "Enable GPlay With PC";
                dialog.InstructionText = Properties.Resources.Dialog_Successed_Inst;
                dialog.Text = Properties.Resources.Dialog_Successed_Desc;
                dialog.Icon = TaskDialogStandardIcon.Information;
                dialog.OwnerWindowHandle = Handle;
                dialog.Show();
        }

        private string[] GetSelectedPath()
        {
            var files = new FileSelector[] { FileSelector_GMS, FileSelector_GSF, FileSelector_GSFLogin, FileSelector_Vending };
            return files.Select(f => f.GetPath()).ToArray();
        }

        private void LinkLabel_Repo_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start(@"https://github.com/AioiLight/EnableGPlayWithPC");
        }
    }
}
