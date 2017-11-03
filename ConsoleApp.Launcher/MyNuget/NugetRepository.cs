using ConsoleApp.Launcher.MyNuget.Models;
using ConsoleApp.Launcher.MyNuget.Utils;
using NuGet;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ConsoleApp.Launcher.MyNuget
{
    /// <summary>
    /// Class responsible for implementing alternative methods for managing NuGet Packages
    /// </summary>
    public class NugetRepository
    {
        private string _installDirectory;

        // paths to exclude
        private static readonly string[] ExcludePaths = new[] { "_rels", "package" };

        public NugetRepository(string installPath)
        {
            _installDirectory = installPath;
            EnsureDirectory(_installDirectory);
        }


        /// <summary>
        /// Installs a new <see cref="OptimizedZipPackage"/> that is already installed or not and reports the progress of all operations performed.
        /// </summary>
        /// <param name="package">Package to be installed</param>
        /// <param name="progress">Provider for progress updates</param>
        /// <returns>Returns a Boolean when operations are finalized</returns>
        public async Task<bool> InstallPackageAsync(OptimizedZipPackage package, IProgress<ProgressReport> progress)
        {
            var result = await Task.Run(() =>
            {
                string packageDirectory = Path.Combine(_installDirectory, CorrectPackageFullName(package.GetFullName()));

                DeleteFilesWithProgress(packageDirectory, progress);

                string packagePathMoved = MovePakageFileWithProgress(package, packageDirectory, progress);

                ExtractContentPackageWithProgress(new OptimizedZipPackage(packagePathMoved), packageDirectory, progress);

                return true;
            });

            return result;
        }

        /// <summary>
        /// Uninstall an existing package and report progress.
        /// </summary>
        /// <param name="package">Package to be uninstalled</param>
        /// <param name="progress">Provider for progress updates</param>
        /// <returns>Returns a Boolean when operations are finalized</returns>
        public async Task<bool> UnistallPackageAsync(IPackage package, IProgress<ProgressReport> progress)
        {
            var result = await Task.Run(() =>
            {
                string packageDirectory = Path.Combine(_installDirectory, CorrectPackageFullName(package.GetFullName()));

                DeleteFilesWithProgress(packageDirectory, progress);

                return true;
            });

            return result;
        }

        /// <summary>
        /// Delete all files and directories from the package installation directory.
        /// </summary>
        /// <param name="packageDirectory">Directory where the package is installed</param>
        /// <param name="progress">Provider for progress updates</param>
        private void DeleteFilesWithProgress(string packageDirectory, IProgress<ProgressReport> progress)
        {
            var directory = new DirectoryInfo(packageDirectory);

            if (directory.Exists)
            {
                var files = directory.GetFiles("*", SearchOption.AllDirectories);

                //Delete all files
                for (int i = 0; i < files.Length; i++)
                {
                    progress.Report(new ProgressReport()
                    {
                        Operation = (int)PackageOperationType.Deleting,
                        Progress = Convert.ToInt32(((i + 1) / Convert.ToDouble(files.Length)) * 100)
                    });

                    files[i].Delete();
                    Thread.Sleep(1000);
                }

                //Delete all directories
                directory.Delete(true);
            }
        }

        /// <summary>
        /// Extracts all the contained files inside the <see cref="OptimizedZipPackage"/>
        /// </summary>
        /// <param name="nugetPackage">Package to be extracted</param>
        /// <param name="extractPath">Path to where files will be extracted</param>
        /// <param name="progress">Provider for progress updates</param>
        private void ExtractContentPackageWithProgress(OptimizedZipPackage nugetPackage, string extractPath, IProgress<ProgressReport> progress)
        {
            Package package = Package.Open(nugetPackage.GetStream());
            var packageId = NuGet.ZipPackage.GetPackageIdentifier(package);

            foreach (PackagePart part in package.GetParts()
                    .Where(p => IsPackageFile(p, packageId)))
            {
                var relativePath = UriUtility.GetPath(part.Uri);

                var targetPath = Path.Combine(extractPath, relativePath);

                WriteFileWithProgress(part.GetStream(), targetPath, PackageOperationType.Installing, progress);
            }
        }

        /// <summary>
        /// Move the <see cref="OptimizedZipPackage"/> to the installation directory as used by NuGet
        /// </summary>
        /// <param name="package">Package to be moved</param>
        /// <param name="packageDirectory">Directory to which the package will be moved, as in the example "InstallPath\PackageName.1.0.0"</param>
        /// <param name="progress">Provider for progress updates</param>
        /// <returns>Returns the path of the moved package</returns>
        private string MovePakageFileWithProgress(OptimizedZipPackage package, string packageDirectory, IProgress<ProgressReport> progress)
        {
            string fileFullName = CorrectPackageFullName(package.GetFullName());
            string desPackageFilePath = Path.Combine(packageDirectory, fileFullName + ".nupkg");

            WriteFileWithProgress(package.GetStream(), desPackageFilePath, PackageOperationType.Moving, progress);

            return desPackageFilePath;
        }

        /// <summary>
        /// Write a file reporting the progress
        /// </summary>
        /// <param name="stream">Stream the file to be written</param>
        /// <param name="destFilePath">File destination path</param>
        /// <param name="operation">Type of operation that requested the file to be written</param>
        /// <param name="progress">Provider for progress updates</param>
        private void WriteFileWithProgress(Stream stream, string destFilePath, PackageOperationType operation, IProgress<ProgressReport> progress)
        {

            byte[] buffer = new byte[1024 * 1024]; // 1MB buffer

            long fileLength = stream.Length;

            EnsureDirectory(Path.GetDirectoryName(destFilePath));

            using (Stream outputStream = File.Create(destFilePath))
            {
                long totalBytes = 0;
                int currentBlockSize = 0;

                while ((currentBlockSize = stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    totalBytes += currentBlockSize;
                    double percentage = (double)totalBytes * 100.0 / fileLength;

                    outputStream.Write(buffer, 0, currentBlockSize);

                    progress.Report(new ProgressReport()
                    {
                        Operation = (int)operation,
                        Progress = Convert.ToInt32(percentage)
                    });
                }
            }

        }

        private void EnsureDirectory(string path)
        {
            Directory.CreateDirectory(path);
        }

        private string CorrectPackageFullName(string fullName)
        {
            return fullName.Replace(" ", ".");
        }

        private bool IsPackageFile(PackagePart part, string packageId)
        {
            string path = UriUtility.GetPath(part.Uri);
            string directory = Path.GetDirectoryName(path);

            // We exclude any opc files and the auto-generated package manifest file ({packageId}.nuspec)
            return !ExcludePaths.Any(p => directory.StartsWith(p, StringComparison.OrdinalIgnoreCase)) &&
                   !PackageHelper.IsPackageManifest(path, packageId);
        }
    }

}
