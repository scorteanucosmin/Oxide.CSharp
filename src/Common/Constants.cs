using System.Text.RegularExpressions;
using Oxide.CSharp.CompilerStream;
using Oxide.CSharp.Interfaces;

namespace Oxide.CSharp.Common
{
    internal static class Constants
    {
        //TODO: Move to Oxide.Common
        internal static readonly ISerializer Serializer = new Serializer();

        internal const string CompilerDownloadUrl = "https://downloads.oxidemod.com/artifacts/Oxide.Compiler/{0}/";
        internal const string CompilerBasicArguments = "-unsafe true --setting:Force true -ms true";

        internal static readonly Regex FileErrorRegex = new Regex(@"^\[(?'Severity'\S+)\]\[(?'Code'\S+)\]\[(?'File'\S+)\] (?'Message'.+)$",
            RegexOptions.Compiled);

        internal static readonly Regex SymbolEscapeRegex = new Regex(@"[^\w\d]", RegexOptions.Compiled);
    }
}
