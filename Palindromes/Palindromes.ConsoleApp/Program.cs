using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
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

      // Step 1 - Download the requested resource as a string

      var downloadString = new TransformBlock<string, string>
        ( url =>
          {
            Console.WriteLine($"STEP 1 - Downloading from {url}...");
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
                  Console.WriteLine($"STEP 1 - Downloaded {result.Length} characters...");

              }
            }
            return result;
          }
        );

      // Step 2 - Separate the specified text into an array of words

      var createWordList = new TransformBlock<string, List<string>>
        ( text =>
          {
            Console.WriteLine("STEP 2 - Creating word list...");

            char[] tokens = text.ToCharArray();

            // replace non-letter chars like punctuations with space char.
            for (int i = 0; i < tokens.Length; i++)
            {
              if (!char.IsLetter(tokens[i]))
                tokens[i] = ' ';
            }
            text = new string(tokens);
            var stringList = text.Split(new char[] {' '}, StringSplitOptions.RemoveEmptyEntries).ToList();

            Console.WriteLine($"STEP 2 - Found {stringList.Count} words...");

            return stringList;
          }
        );

      downloadString.LinkTo(createWordList);

      downloadString.Completion.ContinueWith(t =>
      {
        if (t.IsFaulted) ((IDataflowBlock)createWordList).Fault(t.Exception);
        else createWordList.Complete();
      });

      // Process "The Adventurous Life of a Versatile Artist: Houdini" 
      //         by Harry Houdini.
      downloadString.Post("http://www.gutenberg.org/cache/epub/45370/pg45370.txt");
      downloadString.Complete();

      createWordList.Completion.Wait();

      Console.WriteLine("Press a key to exit:");
      Console.ReadKey();
    }
  }
}
