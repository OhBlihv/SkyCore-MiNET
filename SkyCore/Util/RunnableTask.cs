using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SkyCore.Util
{

	public class RunnableTask
	{
		
		public static ConcurrentDictionary<int, RunnableTask> ActiveRunnables = new ConcurrentDictionary<int, RunnableTask>();
		
		private static int _nextId = 1;

		private static volatile object _runnableIdLock = new object();
		private static int GetNextId()
		{
			lock (_runnableIdLock)
			{
				return _nextId++;
			}
		}
		
		private static void CancelRunnable(RunnableTask runnable)
		{
			CancelRunnable(runnable.RunnableId);
		}

		private static void CancelRunnable(int runnableId)
		{
			if (ActiveRunnables.TryRemove(runnableId, out var runnableTask))
			{
				runnableTask.Cancelled = true;
			}
		}
		
		//

		public static RunnableTask RunTask(Action runnable)
		{
			RunnableTask runnableTask = new RunnableTask(runnable);

			_RunTask(runnableTask, 0);

			return runnableTask;
		}

		public static RunnableTask RunTaskLater(Action runnable, int delayMillis)
		{
			RunnableTask runnableTask = new RunnableTask(runnable);

			_RunTask(runnableTask, delayMillis);

			return runnableTask;
		}

		public static RunnableTask RunTaskIndefinitely(Action runnable, int timerMillis)
		{
			return RunTaskTimer(runnable, timerMillis, -1);
		}

		public static RunnableTask RunTaskTimer(Action runnable, int timerMillis, int executionCount)
		{
			int executionTimes = -1;

			RunnableTask runnableTask = new RunnableTask(null);

			runnableTask.Runnable = () =>
			{
				//Allow infinite executions with a count of -1
				while (!runnableTask.Cancelled && (executionCount == -1 || ++executionTimes < executionCount))
				{
					Thread.Sleep(timerMillis);

					runnable.Invoke();
				}
			};
			
			_RunTask(runnableTask, timerMillis);

			return runnableTask;
		}

		private static void _RunTask(RunnableTask runnable, int delayMillis)
		{
			var runnableId = GetNextId();

			runnable.RunnableId = runnableId;

			if (delayMillis > 0)
			{
				//Dodgy Override.
				runnable.Runnable = () =>
				{
					Action internalRunnable = runnable.Runnable;
					if (delayMillis > 0)
					{
						Thread.Sleep(delayMillis);
					}

					internalRunnable.Invoke();

					CancelRunnable(runnableId);
				};
			}
			
			ActiveRunnables.TryAdd(runnableId, runnable);
			
			ThreadPool.QueueUserWorkItem(runnable.ThreadPoolCallback);
		}

		//

		protected Action Runnable;

		public int RunnableId { get; protected set; }
		
		public bool Cancelled { get; set; }

		public RunnableTask(Action runnable)
		{
			Runnable = runnable;
		}

		public void ThreadPoolCallback(Object threadContext)
		{
			try
			{
				if (Cancelled)
				{
					return;
				}
				
				Runnable.Invoke();
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
			}
		}

	}

}
