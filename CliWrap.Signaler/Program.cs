namespace CliWrap.Signaler;

public static class Program
{
    public static int Main(string[] args)
    {
        if (args is [var processIdRaw, var signalRaw])
        {
            var processId = int.Parse(processIdRaw);
            var signal = int.Parse(signalRaw);

            return 0;
        }
    }
}