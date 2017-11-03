using ConsoleApp.Launcher.MyNuget;
using ConsoleApp.Launcher.MyNuget.Models;
using ConsoleApp.Launcher.MyNuget.Utils;
using NuGet;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Net;
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

namespace ConsoleApp.Launcher
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private WebClient _webClient;
        private PackageManager _packageManager;
        private IPackageRepository _serverRepository;
        private NugetRepository _nugetRepository;
        private string _downloadingFileName;

        //Obs: Do not change if you want to run the example
        private const string NugetServer = "http://localhost:28770/nuget";
        private const string PackageId = "Wellington.ConsoleApp";

        public MainWindow()
        {
            InitializeComponent();
            this.ContentRendered += Window_ContentRendered;

            _webClient = new WebClient();
            _webClient.DownloadProgressChanged += DownloadProgressChanged;
            _webClient.DownloadFileCompleted += DownloadFileCompleted;

            _packageManager = new PackageManager(GetCacheRepository(), GetInstallDirectory());

            _nugetRepository = new NugetRepository(GetInstallDirectory());

            _serverRepository = PackageRepositoryFactory.Default.CreateRepository(NugetServer);
        }

        /// <summary>
        /// Reports progress to progress bar
        /// </summary>
        private void PackageProgress(ProgressReport report)
        {
            string msg = string.Empty;
            if (report.Operation == (int)PackageOperationType.Moving)
            {
                msg = $"{report.Progress}% Moving files....";
            }
            else if (report.Operation == (int)PackageOperationType.Installing)
            {
                msg = $"{report.Progress}% Installing files....";
            }
            else
            {
                msg = $"{report.Progress}% Deleting Files....";
            }

            this.infoTextBlock.Text = msg;
            this.packageProgreessBar.Value = report.Progress;
        }

        private void DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            //check if the package was actually downloaded
            if (File.Exists(_downloadingFileName))
            {
                this.infoTextBlock.Text = "Preparing to install the package....";
                DoEvents();
                InstallPackageAsync();
            }
            else
            {
                MessageBox.Show("There was an error downloading the packagee");
            }
        }

        private void DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            this.infoTextBlock.Text = $"{e.ProgressPercentage}% Downloading package....";
            this.packageProgreessBar.Value = e.ProgressPercentage;
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            this.infoTextBlock.Text = "Checking package version....";
            DoEvents();
            CheckPackageUpdates();
        }

        /// <summary>
        /// Checks for package updates to be downloaded
        /// </summary>
        private void CheckPackageUpdates()
        {
            var lastServerPackage = _serverRepository.FindPackagesById(PackageId).Where(x => x.IsLatestVersion).SingleOrDefault();
            var installedPackage = _packageManager.LocalRepository.FindPackagesById(PackageId).Where(x => x.IsLatestVersion).SingleOrDefault();

            if (installedPackage == null)
            {
                this.infoTextBlock.Text = "Searching for packages....";
                DoEvents();
                DownloadPackage(lastServerPackage);
            }
            else
            {
                if(lastServerPackage != null)
                {
                    if (lastServerPackage.Version > installedPackage.Version)
                    {
                        this.infoTextBlock.Text = "Updating package....";
                        DoEvents();
                        DownloadPackage(lastServerPackage);
                    }
                    else
                    {
                        StartApplication(_packageManager, installedPackage);
                    }
                }
                else
                {
                    MessageBox.Show("Could not find a valid package on server.", "", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            
        }

        /// <summary>
        /// Download a package from the server
        /// </summary>
        /// <param name="package">Package to be downloaded</param>
        private void DownloadPackage(IPackage package)
        {

            if (!(package is DataServicePackage))
            {
                MessageBox.Show("The package found is not valid.", "", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var packageDownloadUrl = ((DataServicePackage)package).DownloadUrl;

            _downloadingFileName = System.IO.Path.Combine(GetCacheDirectory(), package.GetFullName().Replace(" ", ".") + ".nupkg"); 
            _webClient.DownloadFileTaskAsync(packageDownloadUrl, _downloadingFileName);
        }

        /// <summary>
        /// Install downloaded package using <see cref="NugetRepository"/>
        /// </summary>
        private async void InstallPackageAsync()
        {
            var zipPackage = new OptimizedZipPackage(_downloadingFileName);

            //Install the zipPackage in installFolder
            var progressIndicator = new Progress<ProgressReport>(PackageProgress);
            bool result = await _nugetRepository.InstallPackageAsync(zipPackage, progressIndicator);

            if (result)
            {
                //Confirm that the package was installed as expected by NuGet
                var localPackage = _packageManager.LocalRepository
                    .GetPackages().Where(x => x.Id == zipPackage.Id).SingleOrDefault();

                if(localPackage != null)
                {
                    //Delete zipPackage from the cache Folder
                    File.Delete(_downloadingFileName);
                    StartApplication(_packageManager, localPackage);
                }
            }
        }

        /// <summary>
        /// Starts the application inside the package and closes the launcher
        /// </summary>
        /// <param name="packageManager">NuGet Package Manager</param>
        /// <param name="localPackage">Installed local package</param>
        private void StartApplication(PackageManager packageManager, IPackage localPackage)
        {
            this.infoTextBlock.Text = "Starting application....";
            packageProgreessBar.Value = 50;
            DoEvents();

            //Create a resolver path to manage the paths of the installed package
            var resolver = new DefaultPackagePathResolver(packageManager.FileSystem);
            
            var packageInstallPath = resolver.GetInstallPath(localPackage);

            //Get the path to the folder that contains the compilation files inside the package
            var appPackagePath = System.IO.Path.Combine(packageInstallPath, "bin");
            
            //Get executable from the package directory
            string executableFileName = Directory.GetFiles(appPackagePath, "*.exe").FirstOrDefault();

            if(!string.IsNullOrEmpty(executableFileName))
            {
                Process.Start(executableFileName);
            }

            packageProgreessBar.Value = 100;
            DoEvents();
            Thread.Sleep(100);
            Application.Current.Shutdown();
        }

        /// <summary>
        /// Get a repository for the cache folder
        /// </summary>
        private IPackageRepository GetCacheRepository()
        {
            string cacheDirectory = GetCacheDirectory();

            if (!Directory.Exists(cacheDirectory))
            {
                Directory.CreateDirectory(cacheDirectory);
            }

            return PackageRepositoryFactory.Default.CreateRepository(cacheDirectory);
        }

        /// <summary>
        /// Get the cache directory
        /// </summary>
        /// <returns>Path to directory</returns>
        private string GetCacheDirectory()
        {
            return System.IO.Path.Combine(Environment.CurrentDirectory, "Cache");
        }

        /// <summary>
        /// Get the directory where packages can be installed
        /// </summary>
        /// <returns>Path to directory</returns>
        private string GetInstallDirectory()
        {
            return System.IO.Path.Combine(Environment.CurrentDirectory, "AppPackages");
        }

        /// <summary>
        /// Method not recommended to update XAML. 
        /// Consider using the MVVM pattern in real scenario
        /// </summary>
        public static void DoEvents()
        {
           Dispatcher.CurrentDispatcher.Invoke(DispatcherPriority.Background, new Action(delegate { }));
        }
    }
}
