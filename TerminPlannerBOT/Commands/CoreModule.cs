using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Discord;
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

            foreach(CommandInfo command in commands)
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
                        summary = command.Summary.Substring(i + 1);
                        break;
                    }
                }

                if (!parameterFound)
                    summary = command.Summary;


                string commandGroup = "";
                if (command.Module.Group != null)
                    commandGroup = command.Module.Group + " ";

                string commandHeadder = $"{_server.Prefix}{commandGroup}{command.Name} {parameters}";
                string commandSummary = summary ?? "No Description Available"; ;

                embedOutput.AddField(commandHeadder, commandSummary);
            }

            ReplyAsync($"**Current Prefix: {_server.Prefix}**", false, embedOutput.Build());

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
    }
}
