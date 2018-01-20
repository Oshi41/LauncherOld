﻿using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using System.Windows;
using Universal_Launcher.Models.Singleton;

namespace Universal_Launcher.Models.Installers
{
    internal class LauncherInstaller
    {
        private const string UpdaterLink = "https://getfile.dokpub.com/yandex/get/https://yadi.sk/d/kbzF2VrR3Q2uUT";
        private const string LauncherLink = "https://getfile.dokpub.com/yandex/get/https://yadi.sk/d/qvBLvXBo3NJXB5";
        private readonly INameService _nameService;
        private readonly IShowMessage _showMessageService;

        private string _origin;
        private readonly string _tempDir;

        private readonly string _tempLaunName = "temp.zip";
        private readonly string _tempUpdName = "upd.zip";

        public LauncherInstaller()
        {
            _nameService = IoCContainer.Instanse.Resolve<INameService>();
            _showMessageService = IoCContainer.Instanse.Resolve<IShowMessage>();

            _origin = AppDomain.CurrentDomain.FriendlyName;
            _tempDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                _nameService.ProjectName, "TempUpdate");

            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, true);

            Directory.CreateDirectory(_tempDir);
        }


        public async Task Begin()
        {
            var unpackedUpdName = Path.Combine(_tempDir, _tempUpdName);
            var unpackedLaunName = Path.Combine(_tempDir, _tempLaunName);

            // delete old files
            DeleteFiles(unpackedLaunName, unpackedUpdName);

            // download all needed
            DownloadingViewModel download;

            download = new DownloadingViewModel(UpdaterLink, unpackedUpdName);
            await _showMessageService.ShowWorkerAsync(download, false);

            download = new DownloadingViewModel(LauncherLink, unpackedLaunName);
            await _showMessageService.ShowWorkerAsync(download, false);

            // extract all of it
            string launName;
            string updName;

            using (var zip = ZipFile.Open(unpackedLaunName, ZipArchiveMode.Read))
            {
                launName = Path.Combine(
                    Path.GetDirectoryName(unpackedLaunName),
                    zip.Entries.FirstOrDefault().Name);
            }

            using (var zip = ZipFile.Open(unpackedUpdName, ZipArchiveMode.Read))
            {
                updName = Path.Combine(
                    Path.GetDirectoryName(unpackedUpdName),
                    zip.Entries.FirstOrDefault().Name);
            }

            // delete old exe's
            DeleteFiles(launName, updName);

            // unpack all arch
            ZipFile.ExtractToDirectory(unpackedLaunName, Path.GetDirectoryName(unpackedLaunName));
            ZipFile.ExtractToDirectory(unpackedUpdName, Path.GetDirectoryName(unpackedUpdName));

            // delete arch
            DeleteFiles(unpackedLaunName, unpackedUpdName);
            var curentName = "Universal Launcher.exe";

            // start update
            Process.Start(updName, $"\"{curentName}\" \"{launName}\"");
            Process.GetCurrentProcess().Kill();
            Application.Current.Shutdown(1);
        }

        private void DeleteFiles(params string[] files)
        {
            foreach (var file in files)
                if (File.Exists(file))
                    File.Delete(file);
        }
    }
}