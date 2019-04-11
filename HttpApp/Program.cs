using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace HttpApp
{
    class Program
    {
        static async Task Main(string[] args)
        {
            while (true)
            {
                var input = GetInputFromPrompt("a valid xml file (and possibly a response name)");

                var document = XDocument.Load(input?.First() ?? "");
                var children = document.Root?.Elements()?.ToList();

                var headers = new XDocument(GetElementByName(children, "Header"));
                var request = new XDocument(GetElementByName(children, "Request").Elements().First());
                var endpoint = GetElementByName(children, "Endpoint")?.Value;
                var auth = new XDocument(GetElementByName(children, "Auth"));

                HttpClientHandler handler = null;
                if (auth?.Root?.Value != null)
                {
                    var login = auth.Root.Elements().ToList();
                    var certificate = GetElementByName(login, "Certificate")?.Value;
                    var password = GetElementByName(login, "Password")?.Value;

                    handler = HttpHelper.GetClientHandler(certificate, password);
                }

                var responseString = await HttpHelper.GetClientResponse(endpoint, request, headers, handler);

                if (input?.Count <= 1 || responseString == null) continue;
                await ResponseToFile(input[1], responseString);
            }
        }

        private static async Task ResponseToFile(string file, string response)
        {
            try
            {
                await File.WriteAllTextAsync(file, response);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Could not successfully complete writing file: {e.Message}");
            }
        }

        private static XElement GetElementByName(List<XElement> elements, string name)
        {
            return elements.Find(it => it.Name == name);
        }

        private static List<string> GetInputFromPrompt(string typeOfSpecification)
        {
            var message = string.Format("Please specify {0}:", typeOfSpecification);
            Console.WriteLine(message);
            var regex = new Regex("[^-\\s](.*\\\\(.+?)|.*/(.*?))\\.([^-\\s]+)");
            var input = Console.ReadLine() ?? "";
            var match = Regex.Match(input, regex.ToString());
            while (!File.Exists(match.Value))
            {
                Console.WriteLine(message);
                input = Console.ReadLine() ?? "";
                match = Regex.Match(input, regex.ToString());
            }

            var response = new List<string> {match.Value};
            input = input.Replace(match.Value, "").Trim();
            if (input.Length > 0)
            {
                var oldEndLength = match.Value.Split("/\\".ToCharArray()).Last().Length;
                var oldPath = match.Value.Substring(0, match.Value.Length - oldEndLength);
                response.Add(oldPath + input + ".xml");
            }

            return response;
        }
    }
}
