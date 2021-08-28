using System;
using System.Xml;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace TerminPlannerBOT
{
    class Program
    {
        static void Main(string[] args) => new Program().MainAsync().GetAwaiter().GetResult();

        DiscordSocketConfig clientConfig;
        Discord.Commands.CommandServiceConfig commandConfig;

        public CommandHandler commandHandler;
        DiscordSocketClient _client;
        public string token;
        public static char defaultPrefix;
        public static Color defaultColor = Color.DarkRed;
        public string savePath;

        public async Task MainAsync()
        {
            if (!LoadConfig(ref token, ref savePath))
            {
                Console.WriteLine("\nPress any Key to exit");
                Console.ReadKey();
                return;
            }

            ServerHandler.SetupServerHandler(savePath);

            clientConfig = new DiscordSocketConfig();
            clientConfig.LogLevel = LogSeverity.Verbose;

            commandConfig = new Discord.Commands.CommandServiceConfig();
            commandConfig.CaseSensitiveCommands = false;
            commandConfig.DefaultRunMode = Discord.Commands.RunMode.Async;

            _client = new DiscordSocketClient(clientConfig);
            _client.Log += Log;
            commandHandler = new CommandHandler(_client, new Discord.Commands.CommandService(commandConfig));

            
            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();
            await commandHandler.InstallCommandsAsync();
            SetStatus();
            await Task.Delay(-1);
        }

        private Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }

        private async Task SetStatus()
        {
            while (_client.ConnectionState != ConnectionState.Connected)
            {
                await Task.Delay(1000);
            }
            await _client.SetGameAsync($"forgot prefix? type: @{_client.CurrentUser.Username} info");
            Log("Set Bot Status");
        }

        private bool LoadConfig(ref string _token, ref string _savePath)
        {
            try
            {
                XmlDocument configFile = new XmlDocument();
                string filePath = AppContext.BaseDirectory + "config.xml";
                configFile.Load(filePath);

                XmlNodeList tokenList = configFile.GetElementsByTagName("token");
                _token = tokenList.Item(0).InnerText;
                Log($"Set Token: {_token}");

                XmlNodeList prefixList = configFile.GetElementsByTagName("defaultPrefix");
                defaultPrefix = Convert.ToChar(prefixList.Item(0).InnerText);
                Log($"Default-Prefix: {defaultPrefix}");

                _savePath = "";
                XmlAttributeCollection savePathAttributes = configFile.GetElementsByTagName("savePath").Item(0).Attributes;
                if (savePathAttributes.GetNamedItem("relativePath").InnerText == "true")
                {
                    _savePath = AppContext.BaseDirectory;
                }

                _savePath += savePathAttributes.GetNamedItem("path").InnerText;
                Log($"Save-Path: {_savePath}");


                return true;
            }
            catch (Exception ex)
            {
                Log(ex);
                return false;
            }
        }


        //static public
        static public void Log(string msg, ConsoleColor color = ConsoleColor.DarkGreen)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(DateTime.Now.ToString("HH:mm:ss ") + msg);
            Console.ResetColor();
        }

        static public void Log(Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(ex.ToString());
            Console.ResetColor();
        }

        static public Embed BuildSimpleEmbed(string heading, string description = null, string foot = null)
        {
            EmbedBuilder embedBuilder = new EmbedBuilder();
            embedBuilder.Color = Program.defaultColor;
            if (!String.IsNullOrEmpty(foot))
                embedBuilder.WithFooter(foot, "https://www.google.com/");

            embedBuilder.AddField(heading, description);

            return embedBuilder.Build();
        }
    }

}
