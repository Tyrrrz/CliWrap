using System.Diagnostics;

namespace CliWrap.Internal
{
    internal class ProcessEx : Process
    {
        public bool TryKill()
        {
            try
            {
                Kill();
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}