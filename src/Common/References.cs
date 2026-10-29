using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Oxide.Core;
using Oxide.CSharp.CompilerStream;

namespace Oxide.CSharp.Common
{
    internal static class References
    {
        internal static readonly string RuntimeDirectory = Path.GetDirectoryName(typeof(object).Assembly.Location) is { Length: > 0 } directory
            ? directory
            : RuntimeEnvironment.GetRuntimeDirectory();

        private static readonly string[] NotAccessible = { "0Harmony", "System", "Microsoft", "mscorlib", "Unity", "Mono", "netstandard", "Oxide", "MySql.Data" };

        internal static string? FindDirectory(string fileName)
        {
            if (File.Exists(Path.Combine(Interface.Oxide.ExtensionDirectory, fileName)))
            {
                return Interface.Oxide.ExtensionDirectory;
            }

            return File.Exists(Path.Combine(RuntimeDirectory, fileName)) ? RuntimeDirectory : null;
        }

        internal static string? Find(string fileName) => FindDirectory(fileName) is string directory ? Path.Combine(directory, fileName) : null;

        internal static string[] AccessibleAssemblies(IEnumerable<CompilerFile> references)
        {
            if (!(Interface.Oxide.Config.Compiler.Publicize ?? false))
            {
                return Array.Empty<string>();
            }

            List<string> ignored = Interface.Oxide.Config.Compiler.IgnoredPublicizerReferences;
            return references
                .Where(file => file.Name.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                .Select(file => Path.GetFileNameWithoutExtension(file.Name))
                .Where(name => !NotAccessible.Any(prefix => name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                               && !ignored.Any(prefix => name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
                .Distinct()
                .ToArray();
        }
    }
}
