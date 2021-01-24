using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace MatchMaker
{
    [Serializable]
    class Settings
    {
        public string filePath { get; set; }
        public int minPort { get; set; }
        public int maxPort { get; set; }
        public int minimumPlayersToStart { get; set; }
        public int maxPlayersPerMatch { get; set; }

        public Settings()
        {
            minPort = 5000;
            maxPort = 6000;
            filePath = "/home/folder1/folder2/file";
            minimumPlayersToStart = 0;
            maxPlayersPerMatch = 100;
        }

    }

    [Serializable]
    class DatabaseInfo
    {
        public string host { get; set; }
        public string database { get; set; }
        public string user { get; set; }
        public string password { get; set; }

        public DatabaseInfo()
        {
            host = "localhost";
            database = "database_name_here";
            user = "username_for_db";
            password = "****_you_know_what_that_means";
        }
    }

    public static class ShellHelper
    {
        public static string Bash(this string cmd)
        {
            var escapedArgs = cmd.Replace("\"", "\\\"");

            var process = new Process()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "/bin/bash",
                    Arguments = $"-c \"{escapedArgs}\"",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };
            process.Start();
            string result = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            return result;
        }
    }

    public class matchData
    {
        public int port { get; set; }
        public int matchType { get; set; }
        public List<int> playersJoined { get; set; }
        // public List<int> playersConnected { get; set; }

        public matchData(int _port, int _matchType)
        {
            matchType = _matchType; port = _port; playersJoined = new List<int>();// playersConnected = new List<int>(); 
        }

        public void addPlayerId(int playerId) { playersJoined.Add(playerId); }

        //   public void connectPlayer(int playerId) { playersJoined.Remove(playerId); playersConnected.Add(playerId); }
    }

    public class matchRequest
    {
        public int playerId { get; set; }
        public int match_type { get; set; }

        public matchRequest()
        {
            playerId = 0;
            match_type = 0;
        }

        public matchRequest(int pid, int mid)
        {
            playerId = pid;
            match_type = mid;
        }
    }
}
