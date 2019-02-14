using System;
using System.Threading.Tasks;

namespace ValentineBot
{
    class Program
    {
        static void Main(string[] args)
        {
            Config config = null;
            try
            {
                config = Newtonsoft.Json.JsonConvert.DeserializeObject<Config>(System.IO.File.ReadAllText(".\\config.json"));
            }
            catch (Exception)
            {
                Console.WriteLine("ERROR: Config File not found.");
                return;
            }
            Console.Title = $"{config.Name}Bot";
            var bot = new Bot(config, new System.Threading.CancellationTokenSource());
            Task.Run(bot.Start).Wait();
        }
    }
}
