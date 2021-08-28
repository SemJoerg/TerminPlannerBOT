using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace TerminPlannerBOT
{
    static public class ServerHandler
    {
        public static string SavePath { get; private set; }

        public static List<Server> servers;

        static XmlSerializer serverSerializer;

        public static void SetupServerHandler(string savePath)
        {
            SavePath = savePath;
            servers = new List<Server>();
            serverSerializer = new XmlSerializer(typeof(Server));
        }

        static public Server LoadServer(ulong id)
        {
            foreach (Server server in servers)
            {
                if (server.Id == id)
                {
                    return server;
                }
            }


            string filePath = $"{SavePath}{id}.xml";

            if (File.Exists(filePath))
            {
                try
                {
                    using (StreamReader reader = new StreamReader(filePath))
                    {
                        servers.Add(serverSerializer.Deserialize(reader) as Server);
                        reader.Close();
                    }
                    return servers[servers.Count - 1];
                }
                catch (Exception ex)
                {
                    Program.Log(ex);
                }

            }

            servers.Add(new Server());
            servers[servers.Count - 1].Id = id;

            return servers[servers.Count - 1];
        }

        static public bool SaveServer(Server server)
        {
            if (!Directory.Exists(SavePath))
                Directory.CreateDirectory(SavePath);

            try
            {
                using (StreamWriter writer = new StreamWriter($"{SavePath}{server.Id}.xml"))
                {
                    serverSerializer.Serialize(writer, server);
                }
                return true;
            }
            catch(Exception ex)
            {
                Program.Log("\n!!!Unable to save server!!!\n", ConsoleColor.Red);
                Program.Log(ex);
            }

            return false;
        }
    }
}
