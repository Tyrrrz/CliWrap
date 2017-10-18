using System.Threading.Tasks;

namespace CliWrap.Tests
{
    public static class Extensions
    {
        public static void Forget(this Task task)
        {
            // Suppress pragma 4014
        }

        public static void Forget<T>(this Task<T> task)
        {
            // Suppress pragma 4014
        }
    }
}