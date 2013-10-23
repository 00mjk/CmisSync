using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

using log4net;

namespace CmisSync.Lib.Events
{
    public class SyncEventQueue : IDisposable {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(SyncEventQueue));

        private BlockingCollection<ISyncEvent> queue = new BlockingCollection<ISyncEvent>();
        private SyncEventManager manager;
        private Task consumer;
        
        private static void Listen(BlockingCollection<ISyncEvent> queue, SyncEventManager manager){
            Logger.Debug("Starting to listen on SyncEventQueue");
            while (!queue.IsCompleted)
            {

                ISyncEvent syncEvent = null;
                // Blocks if number.Count == 0 
                // IOE means that Take() was called on a completed collection. 
                // Some other thread can call CompleteAdding after we pass the 
                // IsCompleted check but before we call Take.  
                // In this example, we can simply catch the exception since the  
                // loop will break on the next iteration. 
                try
                {
                    syncEvent = queue.Take();
                }
                catch (InvalidOperationException) { }

                if (syncEvent != null)
                {
                    manager.handle(syncEvent);
                }
            }
            Logger.Debug("Stopping to listen on SyncEventQueue");
        }

        public SyncEventQueue(SyncEventManager manager) {
            this.manager = manager;
        }

        public void StartListener() {
            if(this.consumer != null) {
                throw new InvalidOperationException("Listener is not restartable");
            }
            this.consumer = new Task(() => Listen(this.queue, this.manager));
            this.consumer.Start();
        }

        public void StopListener() {
            this.queue.CompleteAdding();
        }            
        
        public bool IsStopped {
            get { 
                if(this.consumer == null){
                    Logger.Debug("consumer null");
                    return true;
                }
                Logger.Debug(this.consumer.Status);
                return this.consumer.IsCompleted; 
            }
        }

        public void Dispose() {
            if(!IsStopped){
                Logger.Error("Disposing a not yet stopped SyncEventQueue - implementation error");
            }
            if(this.consumer != null){
                this.consumer.Dispose();
            }
            this.queue.Dispose();
        }
    }
}
