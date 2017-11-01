using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SkyCore.Util
{

	public class RunnableTask
	{

		public static void RunTask(Action runnable)
		{
			_RunTask(new RunnableTask(runnable), 0);
		}

		public static void RunTaskLater(Action runnable, int delayMillis)
		{
			_RunTask(new RunnableTask(runnable), delayMillis);
		}

		public static void RunTaskIndefinitely(Action runnable, int timerMillis)
		{
			RunTaskTimer(runnable, timerMillis, -1);
		}

		public static void RunTaskTimer(Action runnable, int timerMillis, int executionCount)
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
		}

		private static void _RunTask(RunnableTask runnable, int delayMillis)
		{
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
				};
			}
			
			ThreadPool.QueueUserWorkItem(runnable.ThreadPoolCallback);
		}

		//

		protected Action Runnable;

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
