using System;
using System.Collections.Generic;
using System.Linq;

namespace EmbeddedResourceVirtualPathProvider.FileSystem
{
    /// <summary>
    /// IEmbeddedResourceFolder
    /// Defines an embedded resource folder
    /// </summary>
    public interface IResourceFolder
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        string Name { get; }

        /// <summary>
        /// Gets the resource path.
        /// </summary>
        /// <value>
        /// The resource path.
        /// </value>
        string ResourcePath { get; }

        /// <summary>
        /// Gets the virtual path.
        /// </summary>
        /// <value>
        /// The virtual path.
        /// </value>
        string VirtualPath { get; }

        /// <summary>
        /// Gets the folders.
        /// </summary>
        /// <value>
        /// The folders.
        /// </value>
        IEnumerable<IResourceFolder> Folders { get; }
        /// <summary>
        /// Gets the files.
        /// </summary>
        /// <value>
        /// The files.
        /// </value>
        IEnumerable<IResourceFile> Files { get; }
        /// <summary>
        /// Finds the folder.
        /// </summary>
        /// <param name="directoryPath">The directory path.</param>
        /// <returns></returns>
        IResourceFolder FindFolder(string directoryPath);
    }

    class ResourceFolder : IResourceFolder
    {
        public ResourceFolder()
        {
            this._folders = new List<IResourceFolder>();
            this._files = new List<IResourceFile>();
        }

        public string Name { get; set; }
        public string VirtualPath { get; set; }

        public string ResourcePath { get; set; }

        private readonly ICollection<IResourceFolder> _folders;
        public IEnumerable<IResourceFolder> Folders
        {
            get
            {
                return this._folders;
            }
        }

        private readonly ICollection<IResourceFile> _files;
        public IEnumerable<IResourceFile> Files
        {
            get
            {
                return this._files;
            }
        }

        public void AddFolder(IResourceFolder folder)
        {
            this._folders.Add(folder);
        }

        public void AddFile(IResourceFile file)
        {
            this._files.Add(file);
        }

        public IResourceFolder FindFolder(string directoryPath)
        {
            if (directoryPath == null)
                throw new ArgumentNullException("directoryPath");

            var parts = directoryPath.TrimStart('~', '/', '\\').Split('\\', '/').AsEnumerable();

            if (parts.Count() == 1 && String.IsNullOrWhiteSpace(parts.First()))
                return this;

            IResourceFolder currentFolder = this;
            while (parts.Count() > 0)
            {
                var part = parts.First();
                currentFolder = currentFolder.Folders.FirstOrDefault(x => x.Name.Equals(part, StringComparison.OrdinalIgnoreCase));
                if (currentFolder == null)
                    return null;

                parts = parts.Skip(1);
            }

            return currentFolder;
        }
    }
}
