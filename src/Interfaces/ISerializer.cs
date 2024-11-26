namespace Oxide.CSharp.Interfaces
{
    internal interface ISerializer
    {
        byte[] Serialize<T>(T type) where T : class;

        T Deserialize<T>(byte[] data) where T : class;
    }
}
