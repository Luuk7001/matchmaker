using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using MySql.Data;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;

namespace MatchMaker
{
    class Program
    {
        static void Main(string[] args)
        {
            println("Welcome to MatchMaker!~", ConsoleColor.Yellow);
            println("");
            println("Reading settings...");

            Settings settings = new Settings();
            if (File.Exists(AppDomain.CurrentDomain.BaseDirectory + "settings.json"))
            {
                settings = JsonConvert.DeserializeObject<Settings>(File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + "settings.json"));
                println("Okay.", ConsoleColor.Green);
            }
            else
            {
                println("Couldn't find settings file, creating new settings file...", ConsoleColor.Red);
                File.WriteAllText((AppDomain.CurrentDomain.BaseDirectory + "settings.json"), JsonConvert.SerializeObject(settings));
                println("Okay.", ConsoleColor.Green);
            }
            println("");
            println("Reading Database credentials...");

            DatabaseInfo dbInfo = new DatabaseInfo();
            if (File.Exists(AppDomain.CurrentDomain.BaseDirectory + "db.json"))
            {
                dbInfo = JsonConvert.DeserializeObject<DatabaseInfo>(File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + "db.json"));
                println("Okay.", ConsoleColor.Green);
            }
            else
            {
                println("Couldn't find Database credentials file, creating new file...", ConsoleColor.Red);
                File.WriteAllText((AppDomain.CurrentDomain.BaseDirectory + "db.json"), JsonConvert.SerializeObject(dbInfo));
                println("Okay.", ConsoleColor.Green);

                println("Newly created Database credentials file is with dummy credentials, please fill it with your real credentials.");
                println("Press Y to continue anyway or press N to exit the program so you can edit that file.", ConsoleColor.Yellow);
                if(Console.ReadKey().Key == ConsoleKey.Y)
                {
                    Environment.Exit(0);
                }
                else
                {
                    println("Okay, here we go then");
                }
            }
            println("");
            println("Testing Bash with sudo privilages");
            try
            {
                println("sudo whoami".Bash());
                println("Do you want to run port check? (y/n)", ConsoleColor.Yellow);

                if (Console.ReadKey().Key == ConsoleKey.Y)
                {
                    println("");
                    println("checking the ports", ConsoleColor.Yellow);
                    int workingPorts = 0;
                    for (int i = settings.minPort; i < settings.maxPort; i++)
                    {
                        if (!portOpen(i))
                        {
                            println("port " + i + " is not available", ConsoleColor.Red);
                        }
                        else
                        {
                            workingPorts++;
                        }
                    }

                    println(workingPorts + " of " + (settings.maxPort - settings.minPort) + " ports are available", ConsoleColor.Green);
                }
                println("");
                println(getRandomPortAvailable(settings.minPort, settings.maxPort) + " is a random open port that we can use");
                /*
                println("allowing and denying port " + settings.minPort);
                println(("cmd: sudo ufw allow " + settings.minPort));
                println(("sudo ufw allow " + settings.minPort).Bash(), ConsoleColor.Green);
                println(("cmd: sudo ufw status"));
                println(("sudo ufw status").Bash(), ConsoleColor.Green);
                println(("cmd: sudo ufw deny " + settings.minPort));
                println(("sudo ufw deny " + settings.minPort).Bash(), ConsoleColor.Green);*/
                println("Okay");
            }
            catch
            {
                throw new Exception("Please run in sudo");
            }

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Establishing Connection with MYSQL Db");
            Console.ForegroundColor = ConsoleColor.White;
            MySqlConnectionStringBuilder connBuilder = new MySqlConnectionStringBuilder();

            connBuilder.Add("Database", dbInfo.database);
            connBuilder.Add("Data Source", dbInfo.host);
            connBuilder.Add("User Id", dbInfo.user);
            connBuilder.Add("Password", dbInfo.password);

            MySqlConnection conn = new MySqlConnection(connBuilder.ConnectionString);

            try
            {
                conn.Open();
                println("Connection Established!", ConsoleColor.Green);
                println("");

                //   List<matchData> matches = new List<matchData>();
                //forever loop to check players
                List<int> offlinePlayers = new List<int>();
                while (true)
                {
                    MySqlCommand cmd = new MySqlCommand("SELECT * FROM Users", conn);
                    MySqlDataReader reader = cmd.ExecuteReader();

                    //itereates thru each player
                    List<matchRequest> totalRequests = new List<matchRequest>();
                    while (reader.Read())
                    {
                        int matchType = reader.GetInt32("match_type");
                        //collect all match requests
                        if (matchType > 0)
                        {
                            if (DateTime.Parse(reader.GetString("last_seen")) >= DateTime.Now.AddSeconds(-3))
                            {
                                totalRequests.Add(new matchRequest(reader.GetInt32("id"), matchType));
                            }
                            else
                            {
                                //inactive player
                                offlinePlayers.Add(reader.GetInt32("id"));
                            }
                        }
                        #region oldCode
                        /*
                        foreach(matchData match in matches)
                        {
                            match.playersJoined = new List<int>();
                        }
                        if (matchType > 0)
                        {
                           //println(reader.GetString("username") + " is looking for a match of matchType:" + matchType, ConsoleColor.Cyan);

                            bool playerAlreadyJoined = false;
                            for (int i = 0; i < matches.Count; i++)
                            {
                             //   if(matches[i].playersJoined.Contains(reader.GetInt32("id")) || matches[i].playersConnected.Contains(reader.GetInt32("id"))) { playerAlreadyJoined = true; break; }

                                if(matches[i].playersJoined.Count < settings.maxPlayersPerMatch)
                                {
                                    matches[i].addPlayerId(reader.GetInt32("id"));
                                  //  foundExistingMatch = true;
                                    break;
                                }
                            }

                            if (!playerAlreadyJoined)
                            {
                                matches.Add(new matchData(getRandomPortAvailable(settings.minPort, settings.maxPort)));
                            }
                        }
                    }
                    reader.Close();

                    foreach (matchData match in matches)
                    {
                        Thread.Sleep(1000);
                        if (match.playersJoined.Count <= settings.minimumPlayersToStart) { continue; }
                        if (!portOpen(match.port)) { println("port is in use", ConsoleColor.Red); continue; }
                        println("Starting a match with port : " + match.port + " for "+(match.playersConnected.Count+ match.playersJoined.Count).ToString() + " Players");
                        openInstance(settings.filePath, match.port);
                        Thread.Sleep(5000);
                        List<int> players = new List<int>();
                        players.AddRange(match.playersJoined);

                        foreach (int userId in players)
                        {
                            MySqlCommand updatePlayerPort = new MySqlCommand("UPDATE Users SET connected_port=" + match.port + ", match_type=0 WHERE id="+userId, conn);
                            MySqlDataReader updateReader = updatePlayerPort.ExecuteReader();
                            while (updateReader.Read())
                            {

                            }

                            updateReader.Close();
                            match.connectPlayer(userId);
                        }*/
                        #endregion
                    }
                    reader.Close();
                    //done collecting requests

                    //send offline players to ranch! just kidding, let's delete their matchmake requests
                    foreach(int id in offlinePlayers)
                    {
                        MySqlCommand updatePlayerPort = new MySqlCommand("UPDATE Users SET match_type=0 WHERE id=" + id, conn);
                        MySqlDataReader updateReader = updatePlayerPort.ExecuteReader();
                        while (updateReader.Read())
                        {
                        }

                        updateReader.Close();
                    }


                    //categorize them
                    List<matchData> matches = new List<matchData>();
                    List<matchData> openedMatches = new List<matchData>();
                    //Add AvailableMatches to matches
                    MySqlCommand availableSql = new MySqlCommand("SELECT * FROM `AvailableMatches`", conn);
                    MySqlDataReader match_reader = availableSql.ExecuteReader();

                    while (match_reader.Read())
                    {
                        openedMatches.Add(new matchData(match_reader.GetInt32("port"), match_reader.GetInt32("match_type")));
                    }
                    match_reader.Close();

                    foreach (matchRequest request in totalRequests)
                    {
                        bool joinedMatch = false;
                        foreach(matchData match in openedMatches)
                        {
                            if(match.matchType == request.match_type)
                            {
                                MySqlCommand updatePlayerPort = new MySqlCommand("UPDATE Users SET connected_port=" + match.port + ", match_type=0 WHERE id=" + request.playerId, conn);
                                MySqlDataReader updateReader = updatePlayerPort.ExecuteReader();
                                while (updateReader.Read())
                                {
                                }

                                updateReader.Close();
                                println(request.playerId.ToString(), ConsoleColor.Blue);
                                joinedMatch = true;
                            }
                        }
                        if (joinedMatch) { continue; }

                        bool foundMatch = false;
                        foreach (matchData match in matches)
                        {
                            if (match.matchType == request.match_type && match.playersJoined.Count < settings.maxPlayersPerMatch)
                            {
                                //found a match to join
                                //do not connect yet, let's see how many players get here at the end
                                match.addPlayerId(request.playerId);
                                foundMatch = true;
                                break;
                            }
                        }

                        if (!foundMatch)
                        {
                            //wait what??? No match for you? Let's get you a match bud.
                            matchData newMatchData = new matchData(getRandomPortAvailable(settings.minPort, settings.maxPort), request.match_type);
                            newMatchData.addPlayerId(request.playerId);
                            matches.Add(newMatchData);
                        }
                    }

                    //okay now let's check if any match can start
                    for (int i = 0; i < matches.Count; i++)
                    {
                        matchData match = matches[i];
                        if (match.playersJoined.Count > settings.minimumPlayersToStart)
                        {
                            println("Okay let's go, Starting new instance with portNumber:" + match.port, ConsoleColor.Green);

                            if (!portOpen(match.port))
                            {
                                println("Port's not available. When I was assigning this port to this match, it was open. So I must have opened another instance using same port, Let my father know this incident!!! contact:sewmina7@gmail.com", ConsoleColor.Red);
                                println("!!!  Aborting this match  !!!", ConsoleColor.Red);
                                continue;
                            }
                            openInstance(settings.filePath, match.port);
                            println("Sending back the new match details to players");

                            foreach (int playerId in match.playersJoined)
                            {
                                MySqlCommand updatePlayerPort = new MySqlCommand("UPDATE Users SET connected_port=" + match.port + ", match_type=0 WHERE id=" + playerId, conn);
                                MySqlDataReader updateReader = updatePlayerPort.ExecuteReader();
                                while (updateReader.Read())
                                {
                                }

                                updateReader.Close();
                                println(playerId.ToString(), ConsoleColor.Blue);
                            }
                        }
                        else
                        {
                            //damn, we really have no players playing this game, :(
                            println("NOT ENOUGH PLAYERS TO START MATCH", ConsoleColor.Red);
                            println("Players Available for this match : " + match.playersJoined.Count + " , Players need to start match : " + settings.minimumPlayersToStart, ConsoleColor.Gray);
                        }
                    }

                    Thread.Sleep(2000);
                }
                conn.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine("Error : " + e.Message);
            }
            Console.WriteLine("Press any key to exit...");
            Console.ReadLine();
        }

        #region helperMethods
        public static void println()
        {
            Console.WriteLine("");
        }

        public static void println(string txt)
        {
            Console.WriteLine(txt);
        }

        public static void println(string txt, ConsoleColor color)
        {
            ConsoleColor col = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.WriteLine(txt);
            Console.ForegroundColor = col;
        }



        public static bool portOpen(int portNumber)
        {
            string bashOut = ("sudo ss -tulpn | grep :" + portNumber.ToString()).Bash();
            return !(bashOut.ToLower().Contains("tcp") || bashOut.ToLower().Contains("udp"));
        }

        public static int getRandomPortAvailable(int minPort, int maxPort)
        {
            Random rand = new Random();
            bool portAvailable = false;
            int port = 0;

            while (!portAvailable)
            {
                port = rand.Next(minPort, maxPort);
                portAvailable = portOpen(port);
            }

            return port;
        }

        public static bool openInstance(string fileName, int port)
        {
            bool success = false;
            try
            {
                Process p = Process.Start(new ProcessStartInfo
                {
                    FileName = fileName, // File to execute
                    Arguments = "-port " + port.ToString(), // arguments to use
                    UseShellExecute = false, // use process creation semantics
                    RedirectStandardOutput = true, // redirect standard output to this Process object
                    CreateNoWindow = true, // if this is a terminal app, don't show it
                    WindowStyle = ProcessWindowStyle.Hidden // if this is a terminal app, don't show it
                });
                println("Success: " + p.StartInfo.FileName + p.StartInfo.Arguments);
                success = true;
            }
            catch (Exception e)
            {
                println("Error : " + e.Message, ConsoleColor.Red);
            }

            return success;
        }
        #endregion

    }
    static class Extensions
    {
        public static IList<T> Clone<T>(this IList<T> listToClone) where T : ICloneable
        {
            return listToClone.Select(item => (T)item.Clone()).ToList();
        }
    }
}
