namespace Epam.CmeMdp3Handler.Samples
{
    internal static class Program
    {
        static void Main(string[] args)
        {
            // Run Sample1: low-level listener (all instruments, raw packets)
            Sample1_LowLevelListener.Run(args);

            // Run Sample2: print all security definitions
            // Sample2_PrintAllSecurities.Run(args);
        }
    }
}
