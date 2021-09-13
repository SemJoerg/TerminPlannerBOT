using System;
using System.Xml;
using System.Threading.Tasks;
using Discord;
using System.IO;
using Discord.WebSocket;
using Discord.Rest;

namespace TerminPlannerBOT
{
    class Program
    {
        static void Main(string[] args) => new Program().MainAsync().GetAwaiter().GetResult();

        DiscordSocketConfig clientConfig;
        Discord.Commands.CommandServiceConfig commandConfig;

        public CommandHandler commandHandler;
        static public DiscordSocketClient _client;
        static public DiscordRestClient restClient;

        public string token;
        static public char defaultPrefix;
        static public Color defaultColor = Color.DarkRed;
        public string savePath;
        static public string websiteLinkName;
        static public string websiteUrl;

        public async Task MainAsync()
        {
            if (!LoadConfig(ref token, ref savePath, ref websiteLinkName, ref websiteUrl))
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
            _client.ReactionAdded += Termin.AddedReaction;
            _client.ReactionRemoved += Termin.RemovedReaction;
            _client.MessageUpdated += Termin.MessageUpdated;
            commandHandler = new CommandHandler(_client, new Discord.Commands.CommandService(commandConfig));

            restClient = new DiscordRestClient();
            await restClient.LoginAsync(TokenType.Bot, token);

            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();
            await commandHandler.InstallCommandsAsync();

            while (_client.ConnectionState != ConnectionState.Connected) //Waits for the Client to connect
            {
                await Task.Delay(100);
            }

            UpdateAllTerminMessages();
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
            _client.SetGameAsync($"@{_client.CurrentUser.Username} info");
            Log("Set Bot Status");
        }

        private void UpdateAllTerminMessages()//Updates Terminmessages
        {
            foreach (SocketGuild guild in _client.Guilds)
            {
                Server server = ServerHandler.LoadServer(guild.Id);
                foreach (Termin termin in server.Termins)
                {
                    termin.UpdateTerminQuery(server, server.GetTerminChannel() );
                }
            }
        }

        private bool LoadConfig(ref string _token, ref string _savePath, ref string _websiteLinkName, ref string _websiteUrl)
        {
            try
            {
                XmlDocument configFile = new XmlDocument();
                string filePath = AppContext.BaseDirectory + "config.xml";
                configFile.Load(filePath);

                //token
                XmlNodeList tokenList = configFile.GetElementsByTagName("token");
                _token = tokenList.Item(0).InnerText;
                Log($"Set Token: {_token}");

                //default Prefix
                XmlNodeList prefixList = configFile.GetElementsByTagName("defaultPrefix");
                defaultPrefix = Convert.ToChar(prefixList.Item(0).InnerText);
                Log($"Default-Prefix: {defaultPrefix}");

                //save Path
                _savePath = "";
                XmlAttributeCollection savePathAttributes = configFile.GetElementsByTagName("savePath").Item(0).Attributes;
                if (savePathAttributes.GetNamedItem("relativePath").InnerText == "true")
                {
                    _savePath = AppContext.BaseDirectory;
                }

                _savePath += savePathAttributes.GetNamedItem("path").InnerText;
                Log($"Save-Path: {_savePath}");


                //Website
                XmlAttributeCollection websiteAttributes = configFile.GetElementsByTagName("website").Item(0).Attributes;
                _websiteLinkName = websiteAttributes.GetNamedItem("name").InnerText;
                _websiteUrl = websiteAttributes.GetNamedItem("url").InnerText;

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

        static public Embed BuildSimpleEmbed(string title, string heading = "\u200B", string description = "\u200B", string foot = null)
        {
            EmbedBuilder embedBuilder = new EmbedBuilder();
            embedBuilder.Color = Program.defaultColor;
            embedBuilder.Title = title;
            if (!String.IsNullOrEmpty(foot))
                embedBuilder.WithFooter(foot);

            if(heading != "\u200B" || description != "\u200B") 
                embedBuilder.AddField(heading, description);

            return embedBuilder.Build();
        }

    }

}
