using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace MinecraftConfigHelper
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            bool StringReplaced = false;

            string FilePath = GetArg(args, 0);

            if (!string.IsNullOrEmpty(FilePath)) if (!File.Exists(FilePath))
                {
                    File.Create(FilePath).Close();
                }

            string FileType = GetArg(args, 1);

            string Key = GetArg(args, 2);

            string Value = GetValue(args).Replace("\\\"", "\"").Replace("\\\\", "\\");

            if (FileType.ToLower() == "casual")
            {
                StringBuilder Output = new StringBuilder();

                foreach (string Line in File.ReadAllText(FilePath).Split('\n'))
                {
                    if (string.IsNullOrWhiteSpace(Line.Trim())) continue;

                    if (Line.Trim().StartsWith(Key))
                    {
                        if (!StringReplaced) Output.AppendLine(Key + ":" + Value);

                        StringReplaced = true;

                        continue;
                    }

                    Output.AppendLine(Line.Trim());
                }

                if (!StringReplaced) Output.AppendLine(Key + ":" + Value);

                File.WriteAllText(FilePath, Output.ToString(), new UTF8Encoding(false));

                return;
            }

            if (FileType.ToLower() == "setprofile")
            {
                string SpacenName = args[2];
                Key = args[3];
                Value = args[4];

                string Json = File.ReadAllText(FilePath);
                JsonNode Node = JsonNode.Parse(Json);

                foreach (var Element in (dynamic)Node["profiles"])
                {
                    try
                    {
                        if (Node["profiles"][Element.Key]["lastVersionId"].ToString() == SpacenName)
                        {
                            StringReplaced = true;
                            Node["profiles"][Element.Key][Key] = Value;
                            break;
                        }
                    }
                    catch { }
                }

                if (!StringReplaced)
                {
                    Console.WriteLine("Error finding namespace :C");
                    return;
                }

                File.WriteAllText(FilePath, ReplaceUnicode(Node.ToString()));
                return;
            }

            if (FileType.ToLower() == "deleteprofile")
            {
                string SpacenName = args[2];
                string Json = File.ReadAllText(FilePath);

                JsonNode Node = JsonNode.Parse(Json);
                string ProfileToDelete = null;

                foreach (var Element in (dynamic)Node["profiles"])
                {
                    try
                    {
                        if (Node["profiles"][Element.Key]["lastVersionId"].ToString() == SpacenName)
                        {
                            ProfileToDelete = Element.Key;
                        }
                    }
                    catch { }
                }

                if (ProfileToDelete == null) return;

                ((dynamic)(Node["profiles"])).Remove(ProfileToDelete);

                File.WriteAllText(FilePath, ReplaceUnicode(Node.ToString()), new UTF8Encoding(false));
                return;
            }

            if (FileType.ToLower() == "getfabric")
            {
                JsonNode Json = JsonNode.Parse(new WebClient().DownloadString("https://meta.fabricmc.net/v2/versions/loader"));

                Dictionary<string, bool> Versions = new Dictionary<string, bool>();
                foreach (var Element in (dynamic)Json)
                {
                    try
                    {
                        Versions.Add(Element["version"].ToString(), bool.Parse(Element["stable"].ToString()));
                    }
                    catch { }
                }
                Versions.OrderBy(Ver => Ver);

                Console.Write(Versions.Where(Ver => Ver.Value).First().Key);
                return;
            }

            Console.WriteLine("Invalid arguments ._.");
        }

        private static string ReplaceUnicode(string Input) => Regex.Replace(Input, @"\\u([0-9a-fA-F]{4})", m => char.ToString((char)ushort.Parse(m.Groups[1].Value, NumberStyles.HexNumber)));

        private static string GetArg(string[] args, int Index)
        {
            if (args.Length > Index)
            {
                return args[Index];
            }
            return null;
        }

        private static string GetValue(string[] args)
        {
            if (args.Length <= 3) return "";

            StringBuilder Output = new StringBuilder();

            for (int i = 3; i < args.Length; i++)
            {
                Output.Append(args[i] + " ");
            }

            return Output.ToString().Trim();
        }
    }
}