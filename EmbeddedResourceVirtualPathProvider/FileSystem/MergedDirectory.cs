using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Hosting;
using EmbeddedResourceVirtualPathProvider.Provider;

namespace EmbeddedResourceVirtualPathProvider.FileSystem
{
    internal class MergedDirectory : VirtualDirectory
    {
        private readonly string _embeddedDir;

        private readonly IEnumerable<VirtualFile> _virtualFiles;

        private readonly IEnumerable<VirtualDirectory> _virtualDirectories;
        /// <summary>
        /// Initializes a new instance of the <see cref="MergedDirectory" /> class.
        /// </summary>
        /// <param name="virtualDir">The virtual dir.</param>
        /// <param name="embeddedDir">The embedded dir.</param>
        /// <param name="">The get relative path.</param>
        /// <param name="resourceProvider">The embedded resource provider.</param>
        public MergedDirectory(string virtualDir, string embeddedDir, IResourceProvider resourceProvider)
            : base(virtualDir)
        {
            this._embeddedDir = embeddedDir;

            // Can't make any assumptions on original directory structure...
            // embedded resources will always load everything that matchs this path, and all diretories under it.
            var item = resourceProvider.GetResourceFolder(embeddedDir);

            if (item != null)
            {
                var embeddedFiles = item.Files;
                var embeddedFolders = item.Folders;

                this._virtualFiles = embeddedFiles
                    .Where(x => x.VirtualPath != null)
                    .Select(x => new EmbeddedFile((x.VirtualPath), x))
                    .OfType<VirtualFile>();

                this._virtualDirectories = embeddedFolders
                    .Select(x => new MergedDirectory((x.VirtualPath), x.VirtualPath , resourceProvider));

                var physicalPath = PathUtils.MapPath(embeddedDir);
                if (Directory.Exists(physicalPath))
                {
                    var directoryInfo = new DirectoryInfo(physicalPath);

                    var realFiles = directoryInfo.EnumerateFiles();
                    var physicalFiles = realFiles.Select(x => new PhysicalFile((PathUtils.VirtualPath(x.FullName)), new FileInfo(x.FullName)));

                    this._virtualFiles = this._virtualFiles
                        .Where(x => !physicalFiles.Any(z => x.VirtualPath.Equals((z.VirtualPath), StringComparison.OrdinalIgnoreCase)))
                        .Union(physicalFiles.OfType<VirtualFile>())
                        .OrderBy(x => x.VirtualPath);

                    var enumerateDirectories = directoryInfo.EnumerateDirectories();
                    var realDirectories = enumerateDirectories
                        .Select(x => PathUtils.VirtualPath(x.FullName))
                        .Except(embeddedFolders.Select(x => x.VirtualPath));

                    this._virtualDirectories = this._virtualDirectories.Union(realDirectories
                        .Select(x => new MergedDirectory((x), x , resourceProvider)))
                        .OrderBy(x => x.VirtualPath);
                }
            }
        }

        private ArrayList _children;
        public override System.Collections.IEnumerable Children
        {
            get
            {
                if (this._children == null)
                {
                    this._children = new ArrayList();

                    this._children.AddRange(this._virtualDirectories.ToArray());
                    this._children.AddRange(this._virtualFiles.ToArray());
                }
                return this._children;
            }
        }

        private ArrayList _directories;
        public override System.Collections.IEnumerable Directories
        {
            get
            {
                if (this._directories == null)
                {
                    this._directories = new ArrayList();

                    var virtualDirectories = this._virtualDirectories.ToArray();
                    this._directories.AddRange(virtualDirectories);
                }
                return this._directories;
            }
        }

        private ArrayList _files;
        public override System.Collections.IEnumerable Files
        {
            get
            {
                if (this._files == null)
                {
                    this._files = new ArrayList();
                    this._files.AddRange(this._virtualFiles.ToArray());
                }
                return this._files;
            }
        }
    }
}