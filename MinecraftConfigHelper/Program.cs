using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace MinecraftConfigHelper
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            bool stringReplaced = false;

            string fileType = GetArg(args, 0).ToLower();

            string filePath = GetArg(args, 1);

            if (!string.IsNullOrEmpty(filePath) && !File.Exists(filePath)) File.Create(filePath).Close();

            //string key = GetArg(args, 2);

            string value = GetValue(args, 2).Replace("\\\"", "\"").Replace("\\\\", "\\");

            if (fileType == "casual")
            {
                StringBuilder output = new StringBuilder();

                foreach (string line in File.ReadAllText(filePath).Split('\n'))
                {
                    if (string.IsNullOrWhiteSpace(line.Trim())) continue;

                    if (line.Trim().StartsWith(value.Split(':')[0]))
                    {
                        if (!stringReplaced) output.AppendLine(value);

                        stringReplaced = true;

                        continue;
                    }

                    output.AppendLine(line.Trim());
                }

                if (!stringReplaced) output.AppendLine(value);

                File.WriteAllText(filePath, output.ToString(), new UTF8Encoding(false));

                return;
            }
            else if (fileType == "setprofile")
            {
                string spacenName = args[2];

                string key = args[3];
                value = args[4];

                JObject node = JObject.Parse(File.ReadAllText(filePath));

                foreach (var Element in (dynamic)node["profiles"])
                {
                    try
                    {
                        if (node["profiles"][Element.key]["lastVersionId"].ToString() == spacenName)
                        {
                            stringReplaced = true;
                            node["profiles"][Element.key][key] = value;
                            break;
                        }
                    }
                    catch { }
                }

                if (!stringReplaced)
                {
                    Console.WriteLine("Error finding namespace :C");
                    return;
                }

                File.WriteAllText(filePath, ReplaceUnicode(node.ToString()));
                return;
            }
            else if (fileType == "deleteprofile")
            {
                string spacenName = args[2];
                string json = File.ReadAllText(filePath);

                JObject node = JObject.Parse(json);

                string ProfileToDelete = null;

                foreach (var Element in (dynamic)node["profiles"])
                {
                    try
                    {
                        if (node["profiles"][Element.key]["lastVersionId"].ToString() == spacenName)
                        {
                            ProfileToDelete = Element.key;
                        }
                    }
                    catch { }
                }

                if (ProfileToDelete == null) return;

                ((dynamic)node["profiles"]).Remove(ProfileToDelete);

                File.WriteAllText(filePath, ReplaceUnicode(node.ToString()), new UTF8Encoding(false));
                return;
            }
            else if (fileType == "getfabric")
            {
                JArray json = JArray.Parse(new WebClient().DownloadString("https://meta.fabricmc.net/v2/versions/loader"));

                Dictionary<string, bool> versions = new Dictionary<string, bool>();

                foreach (var Element in (dynamic)json)
                {
                    try
                    {
                        versions.Add(Element["version"].ToString(), bool.Parse(Element["stable"].ToString()));
                    }
                    catch { }
                }

                versions.OrderBy(ver => ver);

                Console.Write(versions.Where(ver => ver.Value).First().Key);
                return;
            }

            Console.WriteLine("Invalid arguments ._.");
        }

        private static string ReplaceUnicode(string input) => Regex.Replace(input, @"\\u([0-9a-fA-F]{4})", m => char.ToString((char)ushort.Parse(m.Groups[1].Value, NumberStyles.HexNumber)));

        private static string GetArg(string[] args, int index)
        {
            if (args.Length > index) return args[index];

            return null;
        }

        private static string GetValue(string[] args, int index = 2)
        {
            if (args.Length <= index) return "";

            StringBuilder output = new StringBuilder();

            for (int i = index; i < args.Length; i++)
            {
                output.Append(args[i] + " ");
            }

            return output.ToString().Trim();
        }
    }
}