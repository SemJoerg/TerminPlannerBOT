using System;
using System.Collections.Generic;
using System.Text;
using Discord.WebSocket;

namespace TerminPlannerBOT
{
    [Serializable]
    public class Termin
    {
        private int id = 0;
        public int Id 
        {
            get { return id; }
            set
            {
                if (id != 0)
                    return;

                id = value;
            }
        }

        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime Date { get; set; }

        public List<SocketUser> AcceptedUserList { get; set; }
        public List<SocketUser> declinedUserList { get; set; }

        public bool ConvertToDateTime(string date, string time)
        {
            if (String.IsNullOrEmpty(date) && String.IsNullOrEmpty(time))
            {
                return false;
            }
            try
            {
                if (!String.IsNullOrEmpty(date) && String.IsNullOrEmpty(time))
                    Date = Convert.ToDateTime(date + Date.ToString(" HH:mm:ss"));

                else if (!String.IsNullOrEmpty(time) && String.IsNullOrEmpty(date))
                    Date = Convert.ToDateTime(Date.ToString("dd.MM.yyyy ") + time);

                else
                    Date = Convert.ToDateTime($"{date} {time}");

                return true;
            }
            catch (Exception ex)
            {
                Program.Log(ex);
            }
            return false;
        }
    }
}
