using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Web.Caching;

namespace EmbeddedResourceVirtualPathProvider
{
    public class EmbeddedResource
    {
        public EmbeddedResource(VppAssemblyInfo assemblyInfo, string resourcePath)
        {
            AssemblyName = assemblyInfo.Assembly.GetName().Name;
            AssemblyInfo = assemblyInfo;

            var fileInfo = new FileInfo(assemblyInfo.Assembly.Location);
            AssemblyLastModified = fileInfo.LastWriteTime;
            ResourcePath = resourcePath;

            if (!string.IsNullOrWhiteSpace(assemblyInfo.ProjectSourcePath))
            {
                FileName = GetFileNameFromProjectSourceDirectory();

                if (FileName != null) //means that the source file was found, or a copy was in the web apps folders
                {
                    GetCacheDependency = (utcStart) => new CacheDependency(FileName, utcStart);
                    GetStream = () => File.OpenRead(FileName);
                    return;
                }
            }
            GetCacheDependency = (utcStart) => new CacheDependency(assemblyInfo.Assembly.Location);
            GetStream = () => assemblyInfo.Assembly.GetManifestResourceStream(resourcePath);
        }

        public VppAssemblyInfo AssemblyInfo { get; private set; }

        public DateTime AssemblyLastModified { get; private set; }

        public string ResourcePath { get; private set; }

        public string FileName { get; private set; }

        public Func<Stream> GetStream { get; private set; }
        public Func<DateTime, CacheDependency> GetCacheDependency { get; private set; }

        public string AssemblyName { get; private set; }

        private string GetFileNameFromProjectSourceDirectory()
        {
            try
            {
                var resourceName = AssemblyInfo.ProjectSourcePath.Substring(AssemblyInfo.Assembly.GetName().Name.Length + 1).Replace('.', '\\');

                if (!Directory.Exists(AssemblyInfo.ProjectSourcePath))
                    return null;

                // search all subdirectories with dashes
                var dottedDirectories = AssemblyInfo.ScannedSources
                    .Select(d => d.Substring(AssemblyInfo.ProjectSourcePath.Length + 1))
                    .Where(d => d.Contains("."))
                    .ToList();

                foreach (var dir in dottedDirectories)
                {
                    var slashedDir = dir.Replace('.', '\\');
                    // replace dashed path 
                    if (resourceName.StartsWith(slashedDir, true, CultureInfo.InvariantCulture))
                    {
                        resourceName = dir + resourceName.Substring(slashedDir.Length);
                        break;
                    }
                }
                var fileName = Path.Combine(AssemblyInfo.ProjectSourcePath, resourceName);

                fileName = GetFileName(fileName);

                return fileName;


                return fileName;
            }
            catch (Exception ex)
            {
#if DEBUG
                throw;
#else
                Logger.LogWarning("Error reading source files", ex);
                return null;
#endif
            }
        }

        private static string GetFileName(string possibleFileName)
        {
            var indexOfLastSlash = possibleFileName.LastIndexOf('\\');
            while (indexOfLastSlash > -1)
            {
                if (File.Exists(possibleFileName))
                    return possibleFileName;
                possibleFileName = ReplaceChar(possibleFileName, indexOfLastSlash, '.');
                indexOfLastSlash = possibleFileName.LastIndexOf('\\');
            }
            return null;
        }

        private static string ReplaceChar(string text, int index, char charToUse)
        {
            char[] buffer = text.ToCharArray();
            buffer[index] = charToUse;
            return new string(buffer);
        }
    }
}