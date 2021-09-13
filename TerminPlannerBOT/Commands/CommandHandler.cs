using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace TerminPlannerBOT
{
    class CommandHandler
    {
        private readonly DiscordSocketClient _client;
        private readonly CommandService _commandService;
        private IServiceProvider _service;
        private Server server;

        public CommandHandler(DiscordSocketClient client, CommandService commandService)
        {
            _commandService = commandService;
            _client = client;
            server = new Server();
        }

        //private Methods
        private bool SendInfo(SocketUserMessage message)
        {
            //Sends Bot/Server Info
            if (message.Content == $"<@!{_client.CurrentUser.Id}> info")
            {
                EmbedBuilder embed = new EmbedBuilder();
                embed.Color = Color.DarkRed;
                SocketTextChannel terminChannel = server.GetTerminChannel();
                string terminChannelName;
                if (terminChannel != null)
                    terminChannelName = terminChannel.Name;
                else
                    terminChannelName = "None";

                embed.AddField($"**Guild Info**", $"**Prefix:** `{server.Prefix}`\n**Termin channel:** `{terminChannelName}`");
                embed.AddField($"**Bot Info**", $"**Name:** `{_client.CurrentUser.ToString()}`\n**Website: ** [{Program.websiteLinkName}]({Program.websiteUrl})");
                message.Channel.SendMessageAsync(embed: embed.Build());
                return true;
            }
            return false;
        }

        private IServiceProvider updateDependencyInjection()
        {
            return new ServiceCollection()
                .AddSingleton(_client)
                .AddSingleton(_commandService)
                .AddSingleton(server)
                .BuildServiceProvider();
        }

        //Sendet eine Fehlermeldung wenn der Command nicht ausgeführt werden konnte
        private Task OnCommandExecuted(Optional<CommandInfo> command, ICommandContext context, IResult result)
        {
            if (!string.IsNullOrEmpty(result?.ErrorReason))
            {
                string errorMessage = result.ErrorReason;
                string fieldContent = "\u200B";
                if (errorMessage == "Unknown command.")
                    fieldContent += $"\nType **{server.Prefix}help** for a list of commands";
                context.Channel.SendMessageAsync(embed: Program.BuildSimpleEmbed(":x: **Error**", errorMessage, fieldContent));
            }
            
            return Task.CompletedTask;
        }

        //public Methods
        public async Task InstallCommandsAsync()
        {
            _client.MessageReceived += HandleCommandAsync;
            _commandService.CommandExecuted += OnCommandExecuted;
            _service = updateDependencyInjection();
            
            await _commandService.AddModulesAsync(Assembly.GetEntryAssembly(), _service);
        }

        public async Task HandleCommandAsync(SocketMessage messageParam)
        {
            SocketUserMessage message = messageParam as SocketUserMessage;
            if (message == null) return;
            
            SocketGuildChannel channel = message.Channel as SocketGuildChannel;

            server = ServerHandler.LoadServer(channel.Guild.Id);

            if(SendInfo(message)) return;

            //Check ob Command ausgeführt werden soll
            int argPos = 0; // Create a number to track where the prefix ends and the command begins
            if (!(message.HasCharPrefix(server.Prefix, ref argPos) ||
                message.HasMentionPrefix(_client.CurrentUser, ref argPos)) ||
                message.Author.IsBot)
                return;

            //Start Command
            SocketCommandContext context = new SocketCommandContext(_client, message);
            _service = updateDependencyInjection();
            await _commandService.ExecuteAsync(context,argPos, _service);
        }
        
    }
}
