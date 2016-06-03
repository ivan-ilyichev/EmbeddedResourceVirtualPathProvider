using System.Collections.Generic;
using System.Web.Hosting;
using EmbeddedResourceVirtualPathProvider;
using TestResourceLibrary;
using System.Reflection;

namespace TestMvc
{
    public class RegisterVirtualPathProvider
    {
        public static void AppInitialize()
        {
            var vpp = new EmbeddedResourceVirtualPathProvider.Vpp(new Dictionary<Assembly, string>()
            {
                { typeof (Marker).Assembly, @"..\TestResourceLibrary" }
            });
            vpp.UseLocalIfAvailable = r => true;

            HostingEnvironment.RegisterVirtualPathProvider(vpp);
        }
    }
}