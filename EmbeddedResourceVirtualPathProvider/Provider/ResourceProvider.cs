using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using EmbeddedResourceVirtualPathProvider.FileSystem;
using Newtonsoft.Json;

namespace EmbeddedResourceVirtualPathProvider.Provider
{
    /// <summary>
    /// IEmbeddedResourceProvider
    /// Provides embedded resources through all application and tenant assemblies.
    /// </summary>
    public interface IResourceProvider
    {
        /// <summary>
        /// Gets the name of the manifest resource.
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
        /// <returns></returns>
        string GetManifestResourceName(string fileName);
        /// <summary>
        /// Resources the exists.
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
        /// <returns></returns>
        bool FileExists(string fileName);
        /// <summary>
        /// Gets the resource file.
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
        /// <returns></returns>
        IResourceFile GetResourceFile(string fileName);
        /// <summary>
        /// Folders the exists.
        /// </summary>
        /// <param name="directoryPath">The directory path.</param>
        /// <returns></returns>
        bool FolderExists(string directoryPath);
        /// <summary>
        /// Gets the resource folder.
        /// </summary>
        /// <param name="directoryPath">The directory path.</param>
        /// <returns></returns>
        IResourceFolder GetResourceFolder(string directoryPath);
    }

    /// <summary>
    /// EmbeddedResourceProvider
    /// Defines an interface of embedded resources.
    /// </summary>
    public class ResourceProvider : IResourceProvider
    {
        private readonly ConcurrentDictionary<string, IResourceFile> _embeddedResourceItems = new ConcurrentDictionary<string, IResourceFile>(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<string, IList<IResourceFile>> _embeddedResourceItemCollections = new ConcurrentDictionary<string, IList<IResourceFile>>(StringComparer.OrdinalIgnoreCase);

        private readonly ResourceFolder _rootFolder = new ResourceFolder { Name = "\\" };
        private readonly ConcurrentDictionary<string, string> _assemblyProjectPath = new ConcurrentDictionary<string, string>(); 

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceProvider"/> class.
        /// </summary>
        /// <param name="assemblies">The assemblies.</param>
        public ResourceProvider(IEnumerable<Assembly> assemblies)
        {
            foreach (var assembly in assemblies.Where(x => !x.IsDynamic).Where(x => x.GetName() != null))
            {
                var assemblyName = assembly.GetName().Name;
                var resources = assembly.GetManifestResourceNames();
                // This should work for most cases...
                // unless this assembly has been merged
                // with another assembly, and has inconsitent names.
                var namespaces = assembly.GetTypes().GroupBy(x => x.Namespace).Select(x => x.Key).Where(n => !string.IsNullOrEmpty(n));
                namespaces = GetMostUniquePrefixStrings(namespaces);
                
                this.CacheAssemblyResources(assembly, assemblyName, namespaces, resources);

                var assemblyProjectMetadata = resources.FirstOrDefault(x => x.EndsWith(".rpmetadata.json", StringComparison.OrdinalIgnoreCase));
                if (assemblyProjectMetadata != null)
                {
                    this.BuildMetadataCache(assembly.GetManifestResourceStream(assemblyProjectMetadata), assembly); 
                }
            }
        }

        internal static IEnumerable<string> GetMostUniquePrefixStrings(IEnumerable<string> strings)
        {
            var groups = strings.Select(x => x.Split('.'))
                .GroupBy(x => x.First())
                .Select(x => new
                {
                    Name = x.Key,
                    Prefix = CommonPrefix(x.Select(z => String.Join(".", z)).ToArray()).TrimEnd('.')
                });
            foreach (var group in groups)
            {
                yield return group.Prefix;
            }
        }
        private static string CommonPrefix(string[] ss)
        {
            if (ss.Length == 0)
            {
                return "";
            }

            if (ss.Length == 1)
            {
                return ss[0];
            }

            int prefixLength = 0;

            foreach (char c in ss[0])
            {
                foreach (string s in ss)
                {
                    if (s.Length <= prefixLength || s[prefixLength] != c)
                    {
                        return ss[0].Substring(0, prefixLength);
                    }
                }
                prefixLength++;
            }

            return ss[0]; // all strings identical
        }

        /// <summary>
        /// Builds the project metadata cache.
        /// If the assembly gives us an embedded version of it's csproj we can use that for file metadata.
        /// This lets us know the "true" structure of our embedded resources, so when we want a directory
        /// of resources.
        /// We can return the "true" directory contents.
        /// </summary>
        /// <param name="metadataStream">The project stream.</param>
        /// <param name="assembly">The assembly.</param>
        private void BuildMetadataCache(Stream metadataStream, Assembly assembly)
        {
            dynamic metadata = null;
            using (var reader = new StreamReader(metadataStream))
            {
                metadata = JsonConvert.DeserializeObject(reader.ReadToEnd());
            }

            _assemblyProjectPath.TryAdd(assembly.GetName().Name, metadata.ProjectPath.ToString());

            foreach (var file in metadata.Files)
            {
                var values = new List<string>();
                foreach (var value in file.Value)
                {
                    var parts = value.ToString().Split('\\');
                    this.ProcessFilePart(file.Name, parts, this._rootFolder);
                }
            }
        }

        private static string GetFolderPathDelegate(string value)
        {
            return value.Replace('-', '_');
        }

        private static string GetEmbeddedFileNamePath(string fileName)
        {
            var fileNamePaths = fileName.Split('/');
            var folderPaths = fileNamePaths.Take(fileNamePaths.Length - 1).Select(GetFolderPathDelegate);
            return String.Join("/", String.Join("/", folderPaths), fileNamePaths.Last()).TrimStart('/');
        }

        /// <summary>
        /// Processes the file part.
        /// This helps recursively create our directory structure.
        /// </summary>
        /// <param name="resourceName">Name of the file.</param>
        /// <param name="parts">The parts.</param>
        /// <param name="folder">The folder.</param>
        /// <exception cref="System.Collections.Generic.KeyNotFoundException"></exception>
        private void ProcessFilePart(string resourceName, IEnumerable<string> parts, ResourceFolder folder)
        {
            resourceName = resourceName.Replace("\\", "/");

            if (parts.Count() == 1)
            {
                IResourceFile item = null;

                this._embeddedResourceItems.TryGetValue(GetResourceItemKey(resourceName), out item);

                var embeddedResourceFile = item as ResourceFile;
                if (embeddedResourceFile == null && !resourceName.EndsWith(".resx", StringComparison.OrdinalIgnoreCase))
                {
                    System.Diagnostics.Trace.TraceWarning("Could not find an embedded file {0} yet it was defined in embedded project file.", resourceName);
                }

                if (item != null)
                {
                    folder.AddFile(item);

                    // we can only infer the virtual path once we know the file location.
                    embeddedResourceFile.VirtualPath = "~/" + resourceName;
                    embeddedResourceFile.FileName = parts.First();
                }
            }
            else if (parts.Count() > 1)
            {
                var originalPart = parts.First();
                var firstPart = originalPart
                    .Replace("-", "_"); // this is a MSBuild convention, folder paths cannot contain a -, they are converted to _ at build time.
                // File names can contain dashes on the other hand... go figure.

                var nextParts = parts.Skip(1);

                var childFolder = folder.Folders
                    .OfType<ResourceFolder>()
                    .FirstOrDefault(x => x.Name.Equals(firstPart, StringComparison.OrdinalIgnoreCase));

                if (childFolder == null)
                {
                    var virtualPath = (folder.VirtualPath ?? "~") + "/" + originalPart;

                    childFolder = new ResourceFolder()
                    {
                        Name = firstPart,
                        VirtualPath = virtualPath
                    };

                    folder.AddFolder(childFolder);
                }

                this.ProcessFilePart(resourceName, nextParts, childFolder);
            }
        }

        /// <summary>
        /// Caches the assembly resources.
        /// We also cache a list of all the overrides (there could be more than one!), so they can be accessed by certian code if needed.
        /// </summary>
        /// <param name="assembly">The assembly.</param>
        /// <param name="assemblyName">Name of the assembly.</param>
        /// <param name="namespaces">The namespaces.</param>
        /// <param name="resources">The resources.</param>
        private void CacheAssemblyResources(Assembly assembly, string assemblyName, IEnumerable<string> namespaces, string[] resources)
        {
            foreach (var resource in resources)
            {
                var substringIndex = assemblyName.Length + 1;
                var relativeResourcePath = GetEmbeddedFileNamePath(resource.Substring(substringIndex));
                // Create our item
                IResourceFile item = new ResourceFile()
                {
                    Assembly = assembly,
                    ResourcePath = resource,
                    ProjectPath = _assemblyProjectPath.GetOrAdd(assemblyName, String.Empty)
                };

                // Get list of items.
                IList<IResourceFile> items = null;
                this._embeddedResourceItemCollections.TryGetValue(relativeResourcePath, out items);

                // No items, create list, and this is the first resource, so lets add the list, and also add this item as the default item.
                // This handles the case where we have an override file, but we can also expose the other files, if we need to get access to them for some reason.
                if (items == null)
                {
                    items = new List<IResourceFile>();

                    this._embeddedResourceItemCollections.TryAdd(relativeResourcePath, items);
                    this._embeddedResourceItems.TryAdd(relativeResourcePath, item);
                }
                items.Add(item);
            }
        }

        private static string GetResourceItemKey(string fileName)
        {
            return GetEmbeddedFileNamePath(fileName.TrimStart('\\', '/', '~')).Replace("\\", ".").Replace("/", ".");
        }

        private IResourceFile GetResourceItem(string fileName)
        {
            IResourceFile resourceItem = null;
            this._embeddedResourceItems.TryGetValue(GetResourceItemKey(fileName), out resourceItem);
            return resourceItem;
        }

        /// <summary>
        /// Gets the name of the resource.
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
        /// <returns></returns>
        public string GetManifestResourceName(string fileName)
        {
            var resourceItem = this.GetResourceItem(fileName);
            if (resourceItem != null)
                return resourceItem.ResourcePath;
            return string.Empty;
        }

        /// <summary>
        /// Resources the exists.
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
        /// <returns></returns>
        public bool FileExists(string fileName)
        {
            return this.GetResourceFile(fileName) != null;
        }

        /// <summary>
        /// Gets the resource file.
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public IResourceFile GetResourceFile(string fileName)
        {
            var folderIndex = fileName.LastIndexOfAny(new[] { '\\', '/' });
            string folder = String.Empty;
            string fName = fileName;
            IResourceFolder resourceFolder = null;
            if (folderIndex > -1)
            {
                folder = fileName.Substring(0, folderIndex);
                fName = fileName.Substring(folderIndex + 1);
            }

            resourceFolder = this._rootFolder.FindFolder(folder);

            if (resourceFolder == this._rootFolder || resourceFolder == null)
            {
                // We cannot find the folder, we could be
                // using an assembly that does not have any metadata
                var file = this.GetResourceItem(fileName);
                if (file != null && String.IsNullOrWhiteSpace(file.FileName))
                    return file;
            }

            if (resourceFolder == null)
                return null;

            return resourceFolder.Files.FirstOrDefault(x => x.FileName.Equals(fName));
        }

        /// <summary>
        /// Resources the exists.
        /// </summary>
        /// <param name="directoryPath">The directory path.</param>
        /// <returns></returns>
        public bool FolderExists(string directoryPath)
        {
            return this.GetResourceFolder(directoryPath) != null;
        }

        /// <summary>
        /// Gets the resource folder.
        /// </summary>
        /// <param name="directoryPath">The directory path.</param>
        /// <returns></returns>
        public IResourceFolder GetResourceFolder(string directoryPath)
        {
            if (directoryPath == null)
                throw new ArgumentNullException("directoryPath");

            if (directoryPath.EndsWith("\\", StringComparison.OrdinalIgnoreCase) || directoryPath.EndsWith("/", StringComparison.OrdinalIgnoreCase))
                directoryPath = directoryPath.Substring(0, directoryPath.Length - 1);

            return this._rootFolder.FindFolder(directoryPath);
        }

    }
}
