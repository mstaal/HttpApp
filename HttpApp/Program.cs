using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
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

                    handler = GetClientHandler(certificate, password);
                }

                using (var client = handler != null ? new HttpClient(handler, true) : new HttpClient())
                {
                    AddHeaders(client, headers);

                    var httpContent = new StringContent(request.ToString(), Encoding.UTF8, "application/xml");

                    Console.WriteLine("Response from service:");
                    Console.WriteLine("----------------------");
                    var response = await client.PostAsync(endpoint, httpContent);

                    var responseString = await response.Content.ReadAsStringAsync();

                    Console.WriteLine(responseString);
                    Console.WriteLine("----------------------");
                    Console.WriteLine("----------------------");

                    if (!(input?.Count > 1)) continue;
                    try
                    {
                        await File.WriteAllTextAsync(input[1], responseString);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Could not complete writing file successfully");
                    }
                }
            }
        }


        private static XElement GetElementByName(List<XElement> elements, string name)
        {
            return elements.Find(it => it.Name == name);
        }

        private static void AddHeaders(HttpClient client, XDocument headerDoc)
        {
            var headers = headerDoc.Root?.Value.Split(";").ToList().FindAll(it => it.Contains(":"));
            foreach (var header in headers ?? new List<string>())
            {
                var split = header.Trim().Split(":");
                client.DefaultRequestHeaders.Add(split[0], split[1]);
            }
        }

        private static HttpClientHandler GetClientHandler(string certificate, string password)
        {
            var clientHandler = new HttpClientHandler
            {
                SslProtocols = SslProtocols.Tls12,
                ClientCertificateOptions = ClientCertificateOption.Manual
            };
            try
            {
                var certPfx = new X509Certificate2(certificate, password);
                clientHandler.ClientCertificates.Add(certPfx);
            }
            catch (CryptographicException e)
            {
                Console.WriteLine($"Error occured while trying to load certificate: {e.Message}");
            }
            clientHandler.ServerCertificateCustomValidationCallback +=
                (HttpRequestMessage req, X509Certificate2 cert2, X509Chain chain, SslPolicyErrors err) => true;
            return clientHandler;
        }

        private static string GetFileFromPrompt(string typeOfSpecification)
        {
            var message = string.Format("Please specify {0}:", typeOfSpecification);
            Console.WriteLine(message);
            var line = Console.ReadLine();
            while (!File.Exists(line))
            {
                Console.WriteLine(message);
                line = Console.ReadLine();
            }

            return line;
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
