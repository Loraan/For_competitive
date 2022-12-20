using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;


namespace CustomThreadPool
{
    public class MyThreadPool : IThreadPool
    {
        private List<Thread> _works = new List<Thread>();
        private Dictionary<int, WorkStealingQueue<Action>> _lQueues = new Dictionary<int, WorkStealingQueue<Action>>();
        private Queue<Action> _gQueue = new Queue<Action>();
        private long _processedTaskCount;
        
        public long GetTasksProcessedCount()
            => _processedTaskCount;

        public MyThreadPool()
        {
            for (var i = 0; i < Environment.ProcessorCount * 2; i++)
                _works.Add(new Thread(CreateWorker) { IsBackground = true });
            
            foreach (var worker in _works)
                _lQueues[worker.ManagedThreadId] = new WorkStealingQueue<Action>();
            
            foreach (var worker in _works)
                worker.Start();
        }

        public void EnqueueAction(Action action)
        {
            var id = Thread.CurrentThread.ManagedThreadId;
            if (_lQueues.ContainsKey(id))
                _lQueues[id].LocalPush(action);
            else
            {
                lock (_gQueue)
                {
                    _gQueue.Enqueue(action);
                    Monitor.Pulse(_gQueue);
                }
            }
        }


        private void CreateWorker()
        {
            var id = Thread.CurrentThread.ManagedThreadId;
            while (true)
            {
                Action task = null;
                if (_lQueues[id].LocalPop(ref task))
                {
                    task();
                    Interlocked.Increment(ref _processedTaskCount);
                }
                else
                {
                    lock (_gQueue)
                    {
                        if (_gQueue.TryDequeue(out task))
                            _lQueues[id].LocalPush(task);
                        else if (!_lQueues.Any(worker => worker.Key != id && !worker.Value.IsEmpty))
                            Monitor.Wait(_gQueue);
                    }

                    if (task is not null) continue;
                    
                    var stealingQueue = _lQueues
                        .Where(worker => worker.Key != id && !worker.Value.IsEmpty)
                        .Select(worker => worker.Value).FirstOrDefault();
                    
                    if (stealingQueue is null || !stealingQueue.TrySteal(ref task))
                        continue;
                    _lQueues[id].LocalPush(task);
                    
                }
            }
        }
    }
}
