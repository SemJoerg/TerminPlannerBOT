using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text;
using Discord;
using Discord.Commands;

namespace TerminPlannerBOT.Commands
{
    [NamedArgumentType]
    public class OptionalTerminAddArguments
    {
        public string Description { get; set; }
        public uint RepatsDays { get; set; }
    }

    [NamedArgumentType]
    public class OptionalTerminModifyArguments
    {
        public string Name { get; set; }
        public string Date { get; set; }
        public string Time { get; set; }
        public string Description { get; set; }
        public uint RepatsDays { get; set; }
    }

    [Group("termin")]
    public class TerminModule : ModuleBase<SocketCommandContext>
    {
        readonly Discord.WebSocket.DiscordSocketClient _client;
        readonly CommandService _commandService;
        Server _server;

        public TerminModule(Discord.WebSocket.DiscordSocketClient client, CommandService commandService, Server server)
        {
            _client = client;
            _commandService = commandService;
            _server = server;
        }

        private void GetTerminField(EmbedBuilder embedBuilder, Termin termin)
        {
            
            string fieldContent = $"**ID:** {termin.Id}\n**Description:** {termin.Description ?? "No Termin Description available"}\n" +
                $"**Date:** {termin.Date.ToString("dd.MM.yyyy")}\n**Time:** {termin.Date.ToString("HH:mm")}";
            embedBuilder.AddField($"**{termin.Name}**", fieldContent);
        }
        
        [Command("list")]
        [Summary("Lists all termins of the server")]
        public Task List()
        {
            EmbedBuilder embed = new EmbedBuilder();
            embed.Color = Program.defaultColor;

            EmbedFooterBuilder foot = new EmbedFooterBuilder();
            foot.Text = $"{_server.Termins.Count}/20 termins";
            embed.Footer = foot;
            foreach (Termin termin in _server.Termins)
            {
                GetTerminField(embed, termin);
            }
            ReplyAsync(embed: embed.Build());
            return Task.CompletedTask;
        }

        [Command("add")]
        [Summary("<name> <date&time> [__Optional Parameter__]|*description: <description>*\n\nAdds a termin")]
        public Task AddTermin(string name, string date, string time, OptionalTerminAddArguments namedArgs = null)
        {   
            Termin termin = new Termin();

            termin.Name = name;
            if(!termin.ConvertToDateTime(date, time))
            {
                ReplyAsync(embed: Program.BuildSimpleEmbed("Error", "Invalid date or time format."));
                return Task.CompletedTask;
            }

            if(namedArgs != null)
            {
                termin.Description = namedArgs.Description;
            }

            _server.AddTermin(termin);
            ServerHandler.SaveServer(_server);

            EmbedBuilder embed = new EmbedBuilder();
            embed.Color = Program.defaultColor;
            GetTerminField(embed, termin);

            ReplyAsync(embed: embed.Build());
            return Task.CompletedTask;
        }

        [Command("modify")]
        [Summary("<id> [__Optional Parameter__]|*name: <name>\ndescription: <description>\ndateandtime: <date&time>*\n\nModifys a existing termin")]
        public Task ModifyTermin(int id, OptionalTerminModifyArguments namedArgs)
        {
            string replyMessageText = null;
            Termin termin;
            if(!_server.GetTermin(id, out termin))
            {
                ReplyAsync($"There is no termin with id **{id}**");
                return Task.CompletedTask;
            }

            termin.Name = namedArgs.Name ?? termin.Name;
            if (!termin.ConvertToDateTime(namedArgs.Date, namedArgs.Time))
            {
                replyMessageText = "Incorrect date or time format!";
            }
            
            termin.Description = namedArgs.Description ?? termin.Description;

            ServerHandler.SaveServer(_server);
            EmbedBuilder embed = new EmbedBuilder();
            embed.Color = Program.defaultColor;
            GetTerminField(embed, termin);
            ReplyAsync(replyMessageText, embed: embed.Build());
            return Task.CompletedTask;
        }

        [Command("remove")]
        [Summary("<terminId>|removes a termin")]
        public Task RemoveTermin(int terminId)
        {

            if(_server.removeTermin(terminId))
            {
                ServerHandler.SaveServer(_server);
                ReplyAsync($"Deleted termin with id **{terminId}**");
            }
            else
            {
                ReplyAsync($"There is no termin with id **{terminId}**");
            }
            return Task.CompletedTask;
        }

    }
}
