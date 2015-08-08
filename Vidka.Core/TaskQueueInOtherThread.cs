using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Vidka.Core
{
	/// <summary>
	/// A generic class that adds actions to a queue and processes them in another thread
	/// NOTE: to trigger listeners and such, you need to call them from the Actions you queue...
	/// </summary>
	public class TaskQueueInOtherThread
	{
		#region events
		public delegate void CurrentQueueFinishedHandler();
		public event CurrentQueueFinishedHandler CurrentQueueFinished;
		#endregion

		// helpers and private
		private Thread curThread;
		private Queue<Action> queue;

		public TaskQueueInOtherThread()
		{
			queue = new Queue<Action>();
		}

		/// <summary>
		/// Will spawn a new thread if the current one is not in progress
		/// </summary>
		public void QueueThisUpPlease(Action item)
		{
			lock (queue)
			{
				queue.Enqueue(item);
				if (curThread == null)
				{
					curThread = new Thread(() =>
					{
						ProcessQueue();
					});
					curThread.Start();
				}

			}
		}

		private void ProcessQueue()
		{
			Action item = null;
			while ((item = DequeueSynchronizedOrNull()) != null)
			{
				item();
			}
			lock (queue)
			{
				curThread = null;
			}
			if (CurrentQueueFinished != null)
				CurrentQueueFinished();
		}

		/// <summary>
		/// lock (queue) { return queue.Any() ? queue.Dequeue() : null; }
		/// </summary>
		private Action DequeueSynchronizedOrNull()
		{
			lock (queue)
			{
				return queue.Any()
					? queue.Dequeue()
					: null;
			}
		}
	}
}
