using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
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

            // Step 3 - Remove short words, sort alphabetically and remove dupes.

            var filterWordList = new TransformBlock<List<string>, List<string>>
                (wordsList =>
                 {
                     Console.WriteLine("STEP 3 - Filtering word list...");
                     var filteredWordsList = wordsList.Where(word => word.Length > 3).OrderBy(word => word).Distinct().ToList();
                     Console.WriteLine($"STEP 3 - Filtered list down to {filteredWordsList.Count}...");
                     return filteredWordsList;
                 }
                );

            // Step 4 - Find Palindromes.
            var findPalindromeWords = new TransformManyBlock<List<string>, string>
                ( wordsList =>
                    {
                        Console.WriteLine("STEP 4 - Finding Palindrome words...");
                        
                        // Holds palindrome words.
                        var palindromeWords = new ConcurrentQueue<string>();

                        // Add each word in the original collection to the result whose 
                        // reversed word also exists in the collection.
                        Parallel.ForEach(wordsList, word =>
                        {
                        // Reverse the word.
                        string reverse = new string(word.Reverse().ToArray());

                        // Enqueue the word if the reversed version also exists
                        // in the collection.
                        if (word == reverse)
                                palindromeWords.Enqueue(word);
                        });

                        var searchMessage = palindromeWords.Any() ? $"STEP 4 - Found {palindromeWords.Count} palindrome(s)" : $"STEP 4 - Didn't find any palindromes :(";
                        Console.WriteLine(searchMessage);

                        return palindromeWords;
                    });

            // Step 5 - Print Palindromes.
            var printPalindromes = new ActionBlock<string>
                ( palindrome =>
                    {
                        Console.WriteLine($"STEP 5 - Found palindrome: {palindrome} / {new string(palindrome.Reverse().ToArray())}");
                    }
                );

            downloadString.LinkTo(createWordList);
            createWordList.LinkTo(filterWordList);
            filterWordList.LinkTo(findPalindromeWords);
            findPalindromeWords.LinkTo(printPalindromes);

            downloadString.Completion.ContinueWith(t =>
            {
                if (t.IsFaulted) ((IDataflowBlock)createWordList).Fault(t.Exception);
                else createWordList.Complete();
            });

            createWordList.Completion.ContinueWith(t =>
            {
                if (t.IsFaulted) ((IDataflowBlock)filterWordList).Fault(t.Exception);
                else filterWordList.Complete();
            });

            filterWordList.Completion.ContinueWith(t =>
            {
                if (t.IsFaulted) ((IDataflowBlock)findPalindromeWords).Fault(t.Exception);
                else findPalindromeWords.Complete();
            });

            findPalindromeWords.Completion.ContinueWith(t =>
            {
                if (t.IsFaulted) ((IDataflowBlock)printPalindromes).Fault(t.Exception);
                else printPalindromes.Complete();
            });

            // Process "Adventures of Huckleberry Finn" 
            //                 by Mark Twain
            Console.WriteLine("STEP 0 - Processing Mark Twain's Book - Adventures of Huckleberry Finn");

            downloadString.Post("http://www.gutenberg.org/files/76/76-0.txt");

            // Mark the head of the pipeline as complete. The continuation tasks 
            // propagate completion through the pipeline as each part of the 
            // pipeline finishes.
            downloadString.Complete();

            // Wait for the last block in the pipeline to process all messages.
            printPalindromes.Completion.Wait();

            Console.WriteLine("Done!\nPress a key to exit:");
            Console.ReadKey();
        }
    }
}
