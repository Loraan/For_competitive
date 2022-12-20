using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace CustomThreadPool
{
    public static class Program
    {
        public static void Main()
        {
            ThreadPoolTests.Run<CustomThreadPool>();
            ThreadPoolTests.Run<DotNetThreadPoolWrapper>();
        }
    }
    
    public class CustomThreadPool : IThreadPool, IDisposable
    {
        private readonly IReadOnlyList<Worker> _works;
        private long _processedTask;
        private volatile int _threadsWaitingCount;
        private readonly Queue<Action> _gQueue = new Queue<Action>();
        
        private CustomThreadPool(int concurrencyLevel)
        {
            if (concurrencyLevel <= 0)
                throw new ArgumentOutOfRangeException(nameof(concurrencyLevel));

            _works = Enumerable
                .Range(0, concurrencyLevel)
                .Select(x => Worker.CreateAndStart(WorkersLoop))
                .ToArray();
        }
        
        public CustomThreadPool() : this(Environment.ProcessorCount) {}

        public void EnqueueAction(Action action)
        {
            if (Worker.CurrentThreadIsWorker)
                Worker.GetCurrentThreadWorker().LocQueue.LocalPush(action);
            else
                lock(_gQueue)
                    _gQueue.Enqueue(action);
            ResumeWorker();
        }

        public long GetTasksProcessedCount() => _processedTask;

        private void WorkersLoop(Worker worker)
        {
            while (true)
            {
                GetTask().Invoke();
                Interlocked.Increment(ref _processedTask);
            }

            Action GetTask()
            {
                if (TryGetTaskFromLocalQueue(out var task))
                    return task;
                while (true)
                {
                    if (TryGetTaskFromGlobalQueue(out task))
                        return task;
                    if (TryStealTask(out task))
                        return task;
                    WaitForNewTask();
                }
            }

            bool TryGetTaskFromLocalQueue(out Action task)
            {
                task = null;
                return worker.LocQueue.LocalPop(ref task);
            }

            bool TryGetTaskFromGlobalQueue(out Action task)
            {
                lock (_gQueue)
                    return _gQueue.TryDequeue(out task);
            }

            bool TryStealTask(out Action task)
            {
                task = null;
                foreach (var anotherWorker in _works?.Where(w => w != worker) ?? Enumerable.Empty<Worker>())
                    if (anotherWorker.LocQueue.TrySteal(ref task))
                        return true;
                return false;
            }
        }
        
        private void ResumeWorker()
        {
            if (_threadsWaitingCount <= 0)
                return;
            
            lock(_gQueue)
                Monitor.Pulse(_gQueue);
        }

        private void WaitForNewTask()
        {
            lock (_gQueue)
            {
                _threadsWaitingCount++;
                try
                {
                    Monitor.Wait(_gQueue);
                }
                finally
                {
                    _threadsWaitingCount--;
                }
            }
        }

        public void Dispose()
        {
            foreach (var worker in _works)
                worker.Dispose();
        }
    }
    
    public class Worker : IDisposable
    {
        private static readonly ThreadLocal<Worker> CurWorker = new ThreadLocal<Worker>();
        public WorkStealingQueue<Action> LocQueue { get; } = new WorkStealingQueue<Action>();
        private Thread Thread { get; }
        
        private Worker(Action<Worker> workerLoop)
        {
            Thread = new Thread(SetWorkerAndRunLoop) {IsBackground = true};

            void SetWorkerAndRunLoop()
            {
                CurWorker.Value = this;
                workerLoop(this);
            }
        }

        public static Worker CreateAndStart(Action<Worker> workerLoop)
        {
            var worker = new Worker(workerLoop);
            worker.Thread.Start();
            return worker;
        }

        public static bool CurrentThreadIsWorker => CurWorker.Value != null;

        public static Worker GetCurrentThreadWorker()
        {
            return CurrentThreadIsWorker ? CurWorker.Value : throw new InvalidOperationException("Current thread is not a worker");
        }
        
        public void Dispose() => CurWorker.Dispose();
    }
}
