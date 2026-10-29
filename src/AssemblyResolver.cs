using Oxide.Core;
using Oxide.CSharp.Common;
using Mono.Cecil;

namespace Oxide.CSharp;

internal class AssemblyResolver : DefaultAssemblyResolver
{
    public AssemblyResolver()
    {
        RemoveSearchDirectory(".");
        RemoveSearchDirectory("bin");
        AddSearchDirectory(Interface.Oxide.ExtensionDirectory);
        AddSearchDirectory(References.RuntimeDirectory);
    }
}
