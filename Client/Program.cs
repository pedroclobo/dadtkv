namespace Client
{
    class Client
    {
        static void Main(string[] args)
        {
            if (args.Length != 2)
            {
                PrintHelp();
                return;
            }

            var identifier = args[0];
            var script = args[1];

            Console.WriteLine($"I am a client with identifier: {identifier}");
            Console.WriteLine($"Running script: {script}");

            Console.ReadLine();
        }

        private static void PrintHelp()
        {
            Console.WriteLine("Usage: Client.exe <identifier> <script>");
        }
    }
}
