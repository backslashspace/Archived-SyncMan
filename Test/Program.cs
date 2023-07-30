using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Test
{
    internal class Program
    {
        private static void DoFileStuffStatus(CancellationToken token)
        {
            while (!token.IsCancellationRequested) // Check if the caller requested cancellation. 
            {
                Thread.Sleep(1000);

                Console.WriteLine("worker");
            }
        }

        static void Main()
        {
            Console.WriteLine("main");

            CancellationTokenSource tokenSource = new CancellationTokenSource(); // Create a token source.
            Thread thread = new Thread(() => DoFileStuffStatus(tokenSource.Token)); // Pass the token to the thread you want to stop.
            thread.Start();

            Thread.Sleep(10000);

            tokenSource.Cancel(); // Request cancellation. 
            thread.Join(); // If you want to wait for cancellation, `Join` blocks the calling thread until the thread represented by this instance terminates.
            tokenSource.Dispose(); // Dispose the token source.

            Console.WriteLine("done-main");
        }

        
    }
}
