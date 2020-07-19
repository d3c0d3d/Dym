using System;

namespace AppLoaderNC
{
    class Program
    {      

        static void Main(string[] args)
        {
            try
            {
                AppShared.Test.SighTesterModules();
                AppShared.Test.LoadTesterModules();

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            Console.WriteLine("Press Q to exit");
            while (Console.ReadKey(true).Key != ConsoleKey.Q) ;
        }   

    }
}
