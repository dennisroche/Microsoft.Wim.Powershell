using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Microsoft.Wim.Powershell.Tests
{
    public static class AssemblyDirectory
    {

        public static string CallingAssemblyDirectory
        {
            get { return GetAssemblyDirectory(Assembly.GetCallingAssembly()); }
        }

        public static string EntryAssemblyDirectory
        {
            get { return GetAssemblyDirectory(Assembly.GetEntryAssembly()); }
        }

        public static string ExecutingAssemblyDirectory
        {
            get { return GetAssemblyDirectory(Assembly.GetExecutingAssembly()); }
        }

        private static string GetAssemblyDirectory(Assembly assembly)
        {
            var codeBase = assembly.CodeBase;
            var uri = new UriBuilder(codeBase);
            var path = Uri.UnescapeDataString(uri.Path);

            return Path.GetDirectoryName(path);
        }

    }
}