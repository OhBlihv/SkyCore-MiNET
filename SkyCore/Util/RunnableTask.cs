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
			_RunTask(runnable, 0);
		}

		public static void RunTaskLater(Action runnable, int delayMillis)
		{
			_RunTask(runnable, delayMillis);
		}

		public static void RunTaskTimer(Action runnable, int timerMillis, int executionCount)
		{
			int executionTimes = -1;

			_RunTask(() =>
			{
				while (++executionTimes < executionCount)
				{
					Thread.Sleep(timerMillis);

					runnable.Invoke();
				}
			}, timerMillis);
		}

		private static void _RunTask(Action runnable, int delayMillis)
		{
			ThreadPool.QueueUserWorkItem(new RunnableTask(() =>
			{
				if (delayMillis > 0)
				{
					Thread.Sleep(delayMillis);
				}

				runnable.Invoke();
			}).ThreadPoolCallback);
		}

		//

		private readonly Action _runnable;

		public RunnableTask(Action runnable)
		{
			_runnable = runnable;
		}

		public void ThreadPoolCallback(Object threadContext)
		{
			try
			{
				_runnable.Invoke();
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
			}
		}

	}

}
