using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Discord.Commands;

namespace TerminPlannerBOT.Commands
{
    public class CoreModule : ModuleBase<SocketCommandContext>
    {
        readonly Discord.WebSocket.DiscordSocketClient _client;
        readonly CommandService _commandService;
        Server _server;

        public CoreModule(Discord.WebSocket.DiscordSocketClient client, CommandService commandService, Server server)
        {
            _client = client;
            _commandService = commandService;
            _server = server;
        }


        [Command("help")]
        [Summary("Prints a list of Commands")]
        public Task Help()
        {
            IEnumerable<CommandInfo> commands = _commandService.Commands;
            EmbedBuilder embedOutput = new EmbedBuilder();
            embedOutput.Color = Program.defaultColor;
            embedOutput.Title = ":interrobang: HELP";
            foreach (CommandInfo command in commands)
            {
                string parameters = "";
                string summary = null;
                bool parameterFound = false;

                for (int i = 0; i < command.Summary.Length; i++)
                {
                    if(command.Summary[i] == '|')
                    {
                        parameterFound = true;
                        parameters = command.Summary.Substring(0, i);
                        summary += command.Summary.Substring(i + 1);
                        break;
                    }
                }

                if (!parameterFound)
                    summary += command.Summary;

                string commandGroup = "";
                if (command.Module.Group != null)
                    commandGroup = command.Module.Group + " ";

                string commandHeadder = $"**{_server.Prefix}{commandGroup}{command.Name} {parameters}**";
                string commandSummary = ">>> ";
                commandSummary += summary ?? "No Description Available";

                embedOutput.AddField(commandHeadder, commandSummary);
            }

            ReplyAsync(embed: embedOutput.Build());

            return Task.CompletedTask;
        }

        [Command("change prefix")]
        [Summary("<prefix>|Changes the Command Prefix for this Server")]
        public Task ChangePrefix(char prefix)
        {
            _server.Prefix = prefix;
            ReplyAsync($"Changed Prefix to **{_server.Prefix}**");
            ServerHandler.SaveServer(_server);

            return Task.CompletedTask;
        }

        private void SetTerminChannel(SocketTextChannel channel)
        {
            _server.SetTerminChannel(channel);
            ServerHandler.SaveServer(_server);
        }

        [Command("set termin channel")]
        [Summary("<id>|Sets the channel where the termin will occour")]
        public Task SetTerminChannel(ulong id)
        {
            SocketTextChannel channel = _client.GetChannel(id) as SocketTextChannel;
            if(channel == null)
            {
                ReplyAsync(embed: Program.BuildSimpleEmbed(":x: Error", "Invalid id"));
                return Task.CompletedTask;
            }

            SetTerminChannel(channel);
            ReplyAsync(embed: Program.BuildSimpleEmbed($"Set Termin channel to `{channel.Name}`"));
            return Task.CompletedTask;
        }

        [Command("set termin channel")]
        [Summary("Sets the (current)channel where the termin will occour")]
        public Task SetTerminChannel()
        {
            SetTerminChannel(Context.Channel as SocketTextChannel);
            ReplyAsync(embed: Program.BuildSimpleEmbed($"Set Termin channel to `{Context.Channel.Name}`"));
            return Task.CompletedTask;
        }
    }
}
