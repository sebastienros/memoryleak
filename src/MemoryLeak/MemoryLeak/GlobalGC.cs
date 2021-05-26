using System.Runtime;

namespace MemoryLeak
{
    public static class GlobalGC
    {
        public static string GC = (GCSettings.IsServerGC == true) ? "Server" : "Workstation";
    }
}
