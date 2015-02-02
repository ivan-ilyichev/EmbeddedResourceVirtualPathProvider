using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Web;
using System.Web.Caching;

namespace EmbeddedResourceVirtualPathProvider
{
    public class EmbeddedResource
    {
        public EmbeddedResource(Assembly assembly, string resourcePath, string projectSourcePath)
        {
            this.AssemblyName = assembly.GetName().Name;
            System.IO.FileInfo fileInfo = new System.IO.FileInfo(assembly.Location);
            AssemblyLastModified = fileInfo.LastWriteTime;
            this.ResourcePath = resourcePath;
            if (!string.IsNullOrWhiteSpace(projectSourcePath))
            {
                var filename = GetFileNameFromProjectSourceDirectory(assembly, resourcePath, projectSourcePath);

                if (filename != null) //means that the source file was found, or a copy was in the web apps folders
                {
                    GetCacheDependency = (utcStart) => new CacheDependency(filename, utcStart);
                    GetStream = () => File.OpenRead(filename);
                    return;
                }
            }
            GetCacheDependency = (utcStart) => new CacheDependency(assembly.Location);
            GetStream = () => assembly.GetManifestResourceStream(resourcePath);
        }

        public DateTime AssemblyLastModified { get; private set; }

        public string ResourcePath { get; private set; }

        public Func<Stream> GetStream { get; private set; }
        public Func<DateTime, CacheDependency> GetCacheDependency { get; private set; }

        public string AssemblyName { get; private set; }

        string GetFileNameFromProjectSourceDirectory(Assembly assembly, string resourcePath, string projectSourcePath)
        {
            try
            {
                if (!Path.IsPathRooted(projectSourcePath))
                {
                    projectSourcePath =
                        new DirectoryInfo((Path.Combine(HttpRuntime.AppDomainAppPath, projectSourcePath))).FullName;
                }

                var path = resourcePath.Substring(assembly.GetName().Name.Length + 1).Replace('.', '\\');
                var fileName = EmbeddedResourcePathHelper.GetPath(projectSourcePath, path);

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
    }
}