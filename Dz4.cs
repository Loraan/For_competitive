using System;
using System.Collections.Generic;
using System.Threading;

namespace CustomThreadPool
{
    public interface IThreadPool
    {
        void EnqueueAction(Action action);
        long GetTasksProcessedCount();
    }

    public class CustomThreadPoolWrapper : IThreadPool
    {
        private long procTask = 0L;
        public void EnqueueAction(Action action)
        {
            CustomThreadPool.AddAction(delegate
            {
                action.Invoke();
                Interlocked.Increment(ref procTask);
            });
        }

        public long GetTasksProcessedCount() => procTask;
    }

    public static class CustomThreadPool
    {
        private static Queue<Action> queue = new Queue<Action>();
        private static Dictionary<int, WorkStealingQueue<Action>> actions = new Dictionary<int, WorkStealingQueue<Action>>();
        static CustomThreadPool()
        {
            void Worker()
            {
                while (true)
                {
                    Action currentAction = delegate { };
                    while (actions[Thread.CurrentThread.ManagedThreadId].LocalPop(ref currentAction))
                        currentAction.Invoke();
                    var flag = TryDequeueAndFindFlag(true);

                    if (!flag)
                        flag = TryStealActionPool(flag);

                    if (!flag)
                        TryDequeueElseWait();
                }
            }

            RunBackgroundThreads(Worker, 16);
        }

        private static bool TryDequeueAndFindFlag(bool flag)
        {
            lock (queue)
            {
                if (queue.TryDequeue(out var action))
                    actions[Thread.CurrentThread.ManagedThreadId].LocalPush(action);
                else
                    flag = false;
            }
            return flag;
        }

        private static bool TryStealActionPool(bool flag)
        {
            foreach (var threadPool in actions)
            {
                Action action = delegate { };
                if (!threadPool.Value.TrySteal(ref action)) continue;
                actions[Thread.CurrentThread.ManagedThreadId].LocalPush(action);
                flag = true;
                break;
            }
            return flag;
        }

        private static void TryDequeueElseWait()
        {
            lock (queue)
            {
                if (queue.TryDequeue(out var action))
                    actions[Thread.CurrentThread.ManagedThreadId].LocalPush(action);
                else
                    Monitor.Wait(queue);
            }
        }

        public static void AddAction(Action action)
        {
            lock (queue)
            {
                queue.Enqueue(action);
                Monitor.Pulse(queue);
            }
        }

        private static Thread[] RunBackgroundThreads(Action action, int count)
        {
            var threads = new List<Thread>();
            for (var i = 0; i < count; i++)
                threads.Add(RunBackgroundThread(action));
            
            return threads.ToArray();
        }

        private static Thread RunBackgroundThread(Action action)
        {
            var thread = new Thread(() => action()) { IsBackground = true };
            actions[thread.ManagedThreadId] = new WorkStealingQueue<Action>();
            thread.Start();
            return thread;
        }
    }
}