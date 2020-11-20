using LoginServer.Application;
using System;
using System.Globalization;
using System.Threading;

namespace LoginServer
{
    class Program
    {
        private static void Header()
        {
            Console.ForegroundColor = ConsoleColor.Cyan;

            string str = @"----------------------------------------------------------------------------";
            Console.SetCursorPosition((Console.WindowWidth - str.Length) / 2, Console.CursorTop);
            Console.WriteLine(str);

            str = @" __         ______     ______     __     __   __    ";
            Console.SetCursorPosition((Console.WindowWidth - str.Length) / 2, Console.CursorTop);
            Console.WriteLine(str);

            str = @"/\ \       /\  __ \   /\  ___\   /\ \   /\ ""-.\ \  ";
            Console.SetCursorPosition((Console.WindowWidth - str.Length) / 2, Console.CursorTop);
            Console.WriteLine(str);

            str = @"\ \ \____  \ \ \/\ \  \ \ \__ \  \ \ \  \ \ \-.  \  ";
            Console.SetCursorPosition((Console.WindowWidth - str.Length) / 2, Console.CursorTop);
            Console.WriteLine(str);

            str = @" \ \_____\  \ \_____\  \ \_____\  \ \_\  \ \_\\""\_\ ";
            Console.SetCursorPosition((Console.WindowWidth - str.Length) / 2, Console.CursorTop);
            Console.WriteLine(str);

            str = @"  \/_____/   \/_____/   \/_____/   \/_/   \/_/ \/_/ ";
            Console.SetCursorPosition((Console.WindowWidth - str.Length) / 2, Console.CursorTop);
            Console.WriteLine(str);

            Console.WriteLine(string.Empty);

            Console.ForegroundColor = ConsoleColor.DarkCyan;

            str = $"Server started at {DateTime.Now.ToString(CultureInfo.InvariantCulture)}";
            Console.SetCursorPosition((Console.WindowWidth - str.Length) / 2, Console.CursorTop);
            Console.WriteLine(str);

            Console.ForegroundColor = ConsoleColor.Cyan;

            str = @"----------------------------------------------------------------------------";
            Console.SetCursorPosition((Console.WindowWidth - str.Length) / 2, Console.CursorTop);
            Console.WriteLine(str);

            Console.WriteLine(string.Empty);
            Console.ResetColor();
        }

        private static void Main()
        {
            // Set Culture
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

            Header();

            // Server application.
            LoginServerApp app = new LoginServerApp();

            ManualResetEvent exitEvent = new ManualResetEvent(false);

            // Ctrl + C handler.
            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                eventArgs.Cancel = true;
                exitEvent.Set();
                //app.Close();
            };

            // Initialize the server.
            app.Start();

            // Run the server..
            app.Run();

            exitEvent.WaitOne();

            // Shutdown the server.
            app.Shutdown();
        }
    }
}
