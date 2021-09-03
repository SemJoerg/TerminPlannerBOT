using System;
using System.Collections.Generic;
using System.Text;
using Discord.WebSocket;

namespace TerminPlannerBOT
{
    [Serializable]
    public class Server
    {
        private ulong? _id;
        public ulong Id
        {
            get { return _id ?? 0; }
            set 
            {
                if (_id != null) return;

                _id = value;
            }
        }

        public char Prefix { get; set; }

        public ulong terminChannelId;

        public List<Termin> Termins { get; set; }

        public Server()
        {
            Prefix = Program.defaultPrefix;
            Termins = new List<Termin>();
            
        }

        //returns false when termin must be inserted
        private bool GetNextId(out int id, out int insertIndex) 
        {
            int index;

            for(index = 0; index < Termins.Count; index++)
            {
                if(Termins[index] == null)
                {
                    insertIndex = index;
                    id = index + 1;
                    return false;
                }
                
                int currentID = Termins[index].Id;
                if (currentID - index > 1)
                {
                    insertIndex = index;
                    id = index + 1;
                    return false;
                }
            }

            id = index + 1;
            insertIndex = -1;

            return true;
        }
        
        public bool AddTermin(Termin termin) //termin id has to be 0 (dont set id)
        {
            if (termin.Id != 0 || Termins.Count >= 20)
                return false;

            int id, insertIndex;

            
            if (GetNextId(out id, out insertIndex))
            {
                termin.Id = id;
                Termins.Add(termin);
            }
            else
            {
                termin.Id = id;
                Termins.Insert(insertIndex, termin);
            }

            return true;
        }

        public bool GetTermin(int id, out Termin terminOutput)
        {
            foreach(Termin termin in Termins)
            {
                if(termin.Id == id)
                {
                    terminOutput = termin;
                    return true;
                }
            }
            terminOutput = new Termin();
            return false;
        }
        
        public bool RemoveTermin(int id)
        {
            for(int index = 0; index < Termins.Count; index++)
            {
                if(Termins[index].Id == id)
                {
                    Termins.RemoveAt(index);
                    return true;
                }
            }

            return false;
        }

        public SocketTextChannel GetTerminChannel()
        {
            return Program._client.GetChannel(terminChannelId) as SocketTextChannel;
        }
        
        public void SetTerminChannel(SocketTextChannel channel)
        {
            terminChannelId = channel.Id;
        }
    }
}
