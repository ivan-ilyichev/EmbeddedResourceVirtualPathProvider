using System.IO;
using System.Reflection;

namespace EmbeddedResourceVirtualPathProvider.FileSystem
{
    /// <summary>
    /// IEmbeddedResourceItem
    /// Defines an embedded resource file.
    /// </summary>
    public interface IResourceFile
    {
        /// <summary>
        /// Gets the assembly.
        /// </summary>
        /// <value>
        /// The assembly.
        /// </value>
        Assembly Assembly { get; }
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
        /// Gets the name of the file.
        /// </summary>
        /// <value>
        /// The name of the file.
        /// </value>
        string FileName { get; }
        /// <summary>
        /// Gets or sets the project path.
        /// 
        /// Used by the virtual path provider to identify where the files came from at run time.
        /// That way you can keep making local changes to the files and not have to recompile every time
        /// 
        /// </summary>
        /// <value>
        /// The project path.
        /// </value>
        string ProjectPath { get; set; }

        /// <summary>
        /// Gets the resource string.
        /// </summary>
        /// <returns></returns>
        string GetResourceString();
        /// <summary>
        /// Gets the resource stream.
        /// </summary>
        /// <returns></returns>
        Stream GetResourceStream();
    }

    class ResourceFile : IResourceFile
    {
        public Assembly Assembly { get; set; }
        public string ResourcePath { get; set; }
        public string VirtualPath { get; set; }
        public string FileName { get; set; }

        /// <summary>
        /// Gets or sets the project path.
        /// </summary>
        /// <value>
        /// The project path.
        /// </value>
        public string ProjectPath { get; set; }

        public Stream GetResourceStream()
        {
            var result = this.Assembly.GetManifestResourceStream(this.ResourcePath);
            return result;
        }

        private string _resourceString;
        public string GetResourceString()
        {
            if (this._resourceString == null)
            {
                using (var stream = this.GetResourceStream())
                {
                    if (stream != null)
                    {
                        using (var reader = new StreamReader(stream))
                        {
                            this._resourceString = reader.ReadToEnd();
                        }
                    }
                }
            }
            return this._resourceString;
        }
    }
}
