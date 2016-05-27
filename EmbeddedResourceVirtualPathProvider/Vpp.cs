﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Web;
using System.Web.Caching;
using System.Web.Hosting;

namespace EmbeddedResourceVirtualPathProvider
{
    public class Vpp : VirtualPathProvider, IEnumerable
    {
        readonly IDictionary<string, List<EmbeddedResource>> resources = new Dictionary<string, List<EmbeddedResource>>();

        public Vpp(params Assembly[] assemblies)
        {
            RegisteredAssemblies = new List<VppAssemblyInfo>();
            Array.ForEach(assemblies, a => Add(a));
            UseResource = er => true;
            UseLocalIfAvailable = resource => true;
            CacheControl = er => null;
        }

        public Func<EmbeddedResource, bool> UseResource { get; set; }
        public Func<EmbeddedResource, bool> UseLocalIfAvailable { get; set; }
        public Func<EmbeddedResource, EmbeddedResourceCacheControl> CacheControl { get; set; }
        public List<VppAssemblyInfo> RegisteredAssemblies { get; private set; }

        public override VirtualDirectory GetDirectory(string virtualDir)
        {
            var directory = base.GetDirectory(virtualDir);
            return directory;
        }

        public void Add(Assembly assembly, string projectSourcePath = null)
        {
            // retrieve absolute path if available
            projectSourcePath = string.IsNullOrWhiteSpace(projectSourcePath)
                ? null
                : Path.IsPathRooted(projectSourcePath)
                    ? projectSourcePath
                    : new DirectoryInfo((Path.Combine(HttpRuntime.AppDomainAppPath, projectSourcePath))).FullName;

            // scan folders not to prevent non-optimal disk read in future
            var scannedFolders = string.IsNullOrWhiteSpace(projectSourcePath) || !Directory.Exists(projectSourcePath)
                ? new List<string>()
                : Directory.GetDirectories(projectSourcePath, "*", SearchOption.AllDirectories).ToList();

            var assemblyInfo = new VppAssemblyInfo()
            {
                Assembly = assembly,
                ProjectSourcePath = projectSourcePath,
                ScannedSources = scannedFolders
            };
            RegisteredAssemblies.Add(assemblyInfo);

            var assemblyName = assembly.GetName().Name;

            foreach (var resourcePath in assembly.GetManifestResourceNames().Where(r => r.StartsWith(assemblyName)))
            {
                var key = resourcePath.ToUpperInvariant().Substring(assemblyName.Length).TrimStart('.');
                if (!resources.ContainsKey(key))
                    resources[key] = new List<EmbeddedResource>();
                resources[key].Insert(0, new EmbeddedResource(assemblyInfo, resourcePath));
            }
        }
 
        public override bool FileExists(string virtualPath)
        {
            return (base.FileExists(virtualPath) || GetResourceFromVirtualPath(virtualPath) != null);
        }

        public override VirtualFile GetFile(string virtualPath)
        {
            var resource = GetResourceFromVirtualPath(virtualPath);
            if (resource != null)
                return new EmbeddedResourceVirtualFile(virtualPath, resource, CacheControl(resource));
            return base.GetFile(virtualPath);
        }

        public override string CombineVirtualPaths(string basePath, string relativePath)
        {
            var combineVirtualPaths = base.CombineVirtualPaths(basePath, relativePath);
            return combineVirtualPaths;
        }
        public override string GetFileHash(string virtualPath, IEnumerable virtualPathDependencies)
        {
            var fileHash = base.GetFileHash(virtualPath, virtualPathDependencies);
            return fileHash;
        }

        public override string GetCacheKey(string virtualPath)
        {
            var resource = GetResourceFromVirtualPath(virtualPath);
            if (resource != null)
            {
                return (virtualPath + resource.AssemblyName + resource.AssemblyLastModified.Ticks).GetHashCode().ToString();
            }
            return base.GetCacheKey(virtualPath);
        }
        
        public EmbeddedResource GetResourceFromVirtualPath(string virtualPath)
        {
            var path = VirtualPathUtility.ToAppRelative(virtualPath).TrimStart('~', '/');
            var index = path.LastIndexOf("/", StringComparison.InvariantCultureIgnoreCase);
            if (index != -1)
            {
                var folder = path.Substring(0, index).Replace("-", "_"); //embedded resources with "-"in their folder names are stored as "_".
                path = folder + path.Substring(index);
            }
            var cleanedPath = path.Replace('/', '.');
            var key = (cleanedPath).ToUpperInvariant();
            if (resources.ContainsKey(key))
            {
                var resource = resources[key].FirstOrDefault(UseResource);
                if (resource != null && !ShouldUsePrevious(virtualPath, resource))
                {
                    return resource;
                }
            }
            return null;
        }

        public override CacheDependency GetCacheDependency(string virtualPath, IEnumerable virtualPathDependencies, DateTime utcStart)
        {
            var resource = GetResourceFromVirtualPath(virtualPath);
            if (resource != null)
            {
                return resource.GetCacheDependency(utcStart);
            }

            if (DirectoryExists(virtualPath) || FileExists(virtualPath))
            {
                return base.GetCacheDependency(virtualPath, virtualPathDependencies, utcStart);
            }

            return null;
        }

        private bool ShouldUsePrevious(string virtualPath, EmbeddedResource resource)
        {
            return base.FileExists(virtualPath) && UseLocalIfAvailable(resource);
        }

        public IEnumerator GetEnumerator()
        {
            throw new NotImplementedException("Only got this so that we can use object collection initializer syntax");
        }
    }
}