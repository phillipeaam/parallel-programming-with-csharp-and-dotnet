using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Samples
{
    internal static class Program
    {
        public static void Main()
        {
            Test9();

            Console.WriteLine("Main program done.");
            Console.ReadKey();
        }
        
        #region Write using lambda

        private static void Test1()
        {
            var char1 = 'A';
            var char2 = 'B';

            Task.Factory.StartNew(() => Write(char1));

            var soloTask = new Task(() => Write(char2));
            soloTask.Start();
        }

        private static void Write(char c)
        {
            var i = 1000;

            while (i -- > 0)
            {
                Console.Write(c);
            }
        }

        #endregion
        
        #region Write using object overload
        
        private static void Test2()
        {
            var user1 = new User { Name = "Phi" };
            var user2 = new User { Name = "Ju" };
        
            Task.Factory.StartNew(Write, user1);
        
            var soloTask = new Task(Write, user2);
            soloTask.Start();
        }
        
        private static void Write(object obj)
        {
            var i = 1000;
        
            var user = obj as User;
        
            while (i -- > 0)
            {
                Console.Write(user?.Name);
            }
        }
        
        #endregion
        
        #region Write using object overload and getting function return 
        
        private static void Test3()
        {
            var text1 = new User { Name = "Phillipe Augusto de Araújo Mendonça" };
            var text2 = new User { Name = "JuDeAmor" };
            
            var factoryTask = Task.Factory.StartNew(GetLength, text1);
            Console.WriteLine($"Length of text {text1}: {factoryTask.Result}");
        
            var soloTask = new Task<int>(GetLength, text2);
            soloTask.Start();
            Console.WriteLine($"Length of text {text2}: {soloTask.Result}");
        }
        
        private static int GetLength(object obj)
        {
            return obj is User user ? user.Name.Length : 0;
        }
        
        #endregion

        #region Cancellable Write using lambda, CancellationTokenSource

        private static void Test4()
        {
            var user = new User { Name = "Phillipe" };
            var cts = new CancellationTokenSource();
            var token = cts.Token;

            var task = new Task(() =>
            {
                while (true)
                {
                    token.ThrowIfCancellationRequested();
                    Console.Write(user.Name);
                }
            }, token);
            task.Start();

            Console.ReadKey();
            cts.Cancel();
        }

        #endregion
        
        #region Cancellable Write using lambda, CancellationTokenSource.CreateLinkedTokenSource with status on sources

        private static void Test5()
        {
            var planned = new CancellationTokenSource();
            planned.Token.Register(() => Console.Write("Planned cancellation. \t"));
            
            var preventative = new CancellationTokenSource();
            preventative.Token.Register(() => Console.Write("Preventative cancellation. \t"));
            
            var emergency = new CancellationTokenSource();
            emergency.Token.Register(() => Console.Write("Emergency cancellation. \t"));
            
            var paranoid = CancellationTokenSource.CreateLinkedTokenSource(planned.Token, preventative.Token, emergency.Token);
            var token = paranoid.Token;

            var task = new Task(() =>
            {
                int i = 0;
                
                while (true)
                {
                    token.ThrowIfCancellationRequested();
                    Console.Write($"{i++}\t");
                }
            }, token);
            task.Start();

            Task.Factory.StartNew(() =>
            {
                planned.Token.WaitHandle.WaitOne();
                Console.Write("WaitHandle released, cancellation requested. \t");
            }, planned.Token);

            Console.ReadKey();
            planned.Cancel();
            // preventative.Cancel();
            // emergency.Cancel();
        }

        #endregion
        
        #region Difuse bomb before it's too late, CancellationTokenSource, token.WaitHandle.WaitOne(), Timed and using return value

        private static void Test6()
        {
            var cts = new CancellationTokenSource();
            var token = cts.Token;

            Task.Factory.StartNew(() =>
            {
                Console.WriteLine("Press any key to diffuse the bomb. You have 5 seconds");
                var isCancelled = token.WaitHandle.WaitOne(5000);
                Console.WriteLine(isCancelled ? "The bomb has been diffused." : "BOOOM!!!");
            }, token);

            Console.ReadKey();
            cts.Cancel();
        }

        #endregion
        
        #region Wait tasks finish to continue execution

        private static void Test7()
        {
            var cts = new CancellationTokenSource();
            var token = cts.Token;

            var task1 = Task.Factory.StartNew(() =>
            {
                for (int i = 0; i < 5; i++)
                {
                    token.ThrowIfCancellationRequested();
                    Thread.Sleep(1000);
                }
                Console.WriteLine("The Task1 is finished.");
            }, token);
            
            var task2 = Task.Factory.StartNew(() =>
            {
                Thread.Sleep(3000);
                Console.WriteLine("The Task2 is finished.");
            }, token);
            
            Task.WaitAny(task1, task2);
            // Task.WaitAll(task1, task2);
            // Task.WaitAll(new []{task1, task2}, 4000);
            // task1.Wait();
            // task1.Wait(4000);
            // task1.Wait(token);
            
            Console.WriteLine($"Task1 Status: {task1.Status}");
            Console.WriteLine($"Task2 Status: {task2.Status}");
        }

        #endregion
        
        #region Manage task exception

        private static void Test8()
        {
            try
            {
                TaskExceptionTestBody();
            }
            catch (AggregateException ae)
            {
                foreach (var innerException in ae.InnerExceptions)
                {
                    Console.WriteLine($"It's not a managed exception. | Type: {innerException.GetType()}  | Message: {innerException.Message}");
                }
            }
        }

        private static void TaskExceptionTestBody()
        {
            var cts = new CancellationTokenSource();
            var token = cts.Token;

            var task1 = Task.Factory.StartNew(() => throw new InvalidOperationException("Can't do this") { Source = "task1" });

            var task2 = Task.Factory.StartNew(() => { token.ThrowIfCancellationRequested(); }, token);

            var task3 = Task.Factory.StartNew(() => throw new ArgumentException("POKASEaoksep") { Source = "task3" });

            cts.Cancel();

            try
            {
                Task.WaitAll(task1, task2, task3);
            }
            catch (AggregateException ae)
            {
                ae.Handle(HandleTaskException);
            }

            Console.WriteLine($"Sample finished!");
        }
        
        private static bool HandleTaskException(Exception e)
        {
            switch (e)
            {
                case InvalidOperationException x:
                    Console.WriteLine($"Invalid operation. | Origin: {x.Source} | Type: {x.GetType()} | Message: {x.Message}");
                    return true;
                case TaskCanceledException x:
                    Console.WriteLine($"Task Cancelled. | Type: {x.GetType()} | Message: {x.Message}");
                    return true;
                default:
                    return false;
            }
        }

        #endregion

        #region Manage critical section

        private static void Test9()
        {
            var ba = new BankAccount();
            var tasks = new List<Task>();

            for (int i = 0; i < 10; i++)
            {
                tasks.Add(Task.Factory.StartNew(() =>
                {
                    for (int x = 0; x < 1000; x++)
                    {
                        ba.Deposit(100);
                    }
                }));
                
                tasks.Add(Task.Factory.StartNew(() =>
                {
                    for (int x = 0; x < 1000; x++)
                    {
                        ba.Withdraw(100);
                    }
                }));
            }

            Task.WaitAll(tasks.ToArray());
            
            Console.WriteLine($"The balance is: {ba.Balance}");
        }

        #endregion
    }
}