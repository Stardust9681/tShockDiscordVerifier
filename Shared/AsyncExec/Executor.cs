using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tShockDiscordVerifier.Shared.AsyncExec
{
    public static class Executor
    {
		static Executor()
		{
			Tasks = new Queue<Task>();
			queueLock = new object();
			Task.Run(async () => {
				while (!TShockAPI.TShock.ShuttingDown)
				{
					bool shouldAwait = false;
					Task? task;
					lock (queueLock)
					{
						shouldAwait = Tasks.TryDequeue(out task);
					}
					if (shouldAwait)
					{
						//await task!.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
						await task!;
					}
				}
			});
		}
		private static Queue<Task> Tasks;
		private static object queueLock;
		public static void Run(Task task)
		{
			lock (queueLock)
			{
				Tasks.Enqueue(task);
			}
		}
		public static void Finish()
		{
			lock (queueLock)
			{
				while (Tasks.TryDequeue(out Task task))
				{
					task.GetAwaiter().GetResult();
				}
			}
		}
    }
}
