namespace Client
{
    class Client
    {
        static void Main(string[] args)
        {
            if (args.Length != 3)
            {
                PrintHelp();
                return;
            }

            var identifier = args[0];
            var script = args[1];
            var serverURLs = args[2];

            Console.WriteLine($"I am a client with identifier: {identifier}");
            Console.WriteLine($"Running script: {script}");
            Console.WriteLine($"Transaction Manager URLs: {serverURLs}");

            Console.ReadLine();
        }

        private static void PrintHelp()
        {
            Console.WriteLine("Usage: Client.exe <identifier> <script> <server_urls>");
        }
    }
}
