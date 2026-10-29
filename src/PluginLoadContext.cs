using System.Collections.Concurrent;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using Oxide.Core;

namespace Oxide.Plugins
{
    internal sealed class PluginLoadContext : AssemblyLoadContext
    {
        private static readonly ConcurrentDictionary<string, Assembly> plugins = new ConcurrentDictionary<string, Assembly>();

        private string? assemblyName;

        private PluginLoadContext(string name, bool unloadable) : base(name, unloadable)
        {
        }

        internal static PluginLoadContext Load(string name, byte[] assembly, byte[]? symbols, out Assembly loaded)
        {
            PluginLoadContext context = new PluginLoadContext(name, Interface.Oxide.Config.Compiler.UnloadAssemblies ?? true);
            using MemoryStream assemblyStream = new MemoryStream(assembly, false);
            using MemoryStream? symbolStream = symbols is { Length: > 0 } ? new MemoryStream(symbols, false) : null;
            loaded = context.LoadFromStream(assemblyStream, symbolStream);
            context.assemblyName = loaded.GetName().Name;
            if (context.assemblyName != null)
            {
                plugins[context.assemblyName] = loaded;
            }

            return context;
        }

        protected override Assembly? Load(AssemblyName name) =>
            name.Name != null && plugins.TryGetValue(name.Name, out Assembly? plugin) ? plugin : null;

        internal void Release()
        {
            if (assemblyName != null)
            {
                plugins.TryRemove(assemblyName, out _);
                assemblyName = null;
            }

            if (IsCollectible)
            {
                Unload();
            }
        }
    }
}
