using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BuildMonitor.Discord.Tasks
{
    public static class TaskManager
    {
        private static ConcurrentDictionary<string, IRepeatedTask> repeatedTasks = new ConcurrentDictionary<string, IRepeatedTask>();

        /// <summary>
        /// Add all tasks to <see cref="ConcurrentDictionary{string, IRepeatedTask}"/>
        /// </summary>
        public static void GetAllTasks()
        {
            var repeatedTaskType = typeof(IRepeatedTask);
            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(p => repeatedTaskType.IsAssignableFrom(p) && !p.IsInterface);

            foreach (var type in types)
                repeatedTasks.TryAdd(type.Name, (IRepeatedTask)Activator.CreateInstance(type));
        }

        /// <summary>
        /// Run all tasks from <see cref="ConcurrentDictionary{string, IRepeatedTask}"/>
        /// </summary>
        public static void RunAllTasks()
        {
            foreach (var repeatableTask in repeatedTasks)
            {
                var token = new CancellationTokenSource();

                new Thread(() =>
                {
                    Console.WriteLine($"[BOT]: Starting {repeatableTask.Key}");

                    while (!token.IsCancellationRequested)
                    {
                        if (!repeatableTask.Value.Run(DiscordManager.Bot.GetClient()).IsCompleted)
                            throw new Exception("Something went wrong!");

                        Task.Delay(repeatableTask.Value.Interval, token.Token);
                    }
                }).Start();
            }
        }
    }
}
