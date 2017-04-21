using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace Palindromes.ConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            // 
            // Create members of the Pipeline
            //

            // Download the requested resource as a string

            var downloadString = new TransformBlock<string, string>
                ( url =>
                    {
                        Console.WriteLine($"Downloading from {url}...");
                        string result = null;
                        using (var client = new HttpClient())
                        {
                            // Perform a synchronous call by calling .Result
                            var response = client.GetAsync(url).Result;

                            if (response.IsSuccessStatusCode)
                            {
                                var responseContent = response.Content;

                                // read result synchronously by calling .Result 
                                result = responseContent.ReadAsStringAsync().Result;
                                if (!string.IsNullOrEmpty(result))
                                    Console.WriteLine($"Downloaded {result.Length} characters...");

                            }
                        }
                        return result;
                    }
                );

            // Process "The Adventurous Life of a Versatile Artist: Houdini" by by Harry Houdini.
            downloadString.Post("http://www.gutenberg.org/cache/epub/45370/pg45370.txt");

            downloadString.Complete();

            Console.WriteLine("Press a key to exit:");
            Console.ReadKey();
        }
    }
}
