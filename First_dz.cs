using System;
using System.Threading;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace ConsoleApp2
{
    static class Program
    {
        static void Main()
        {
            Process.GetCurrentProcess().ProcessorAffinity = (IntPtr)(1 << 0);
            Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.RealTime;
            var thread1 = new Thread(Loop1);
            var thread2 = new Thread(Loop);
            thread1.Start();
            thread2.Start();
            Thread.Sleep(1000000000);
        }

        static void Loop1()
        {
            while (true) ;
        }

        static void Loop()
        {
            var stopWatch = new Stopwatch();
            double prev = 0;
            stopWatch.Start();
            var n = 0;
            double sum = 0;
            while (true)
            {
                var current = stopWatch.Elapsed.TotalMilliseconds;
                var dif = current - prev;
                if (dif > 2)
                {
                    sum += dif;
                    n += 1;
                }
                prev = current;
                if (n > 100)
                    break;
            }
            Console.WriteLine(sum / n);
        }
    }
}