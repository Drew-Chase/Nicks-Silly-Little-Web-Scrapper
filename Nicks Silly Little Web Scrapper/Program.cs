// LFInteractive LLC. 2021-2024
﻿using cclip;

namespace nslws;

internal class Program
{
    private static void Main(string[] args)
    {
        string url;
        int depth = 1;
        bool invalid = false;
        string output;

        OptionsManager manager = new("Nicks Silly Little Web Scrapper");
        manager.Add(new() { ShortName = "u", LongName = "url", HasArgument = true, Required = true, Description = "The starting url." });
        manager.Add(new() { ShortName = "d", LongName = "depth", HasArgument = true, Required = true, Description = "How many layers deep you wanna go." });
        manager.Add(new() { ShortName = "o", LongName = "output", HasArgument = true, Required = false, Description = "The output json file, default is 'output.json'" });
        manager.Add(new() { ShortName = "p", LongName = "performance", HasArgument = false, Required = false, Description = "This will test the performance of the request." });

        OptionsParser parser = manager.Parse(args);
        bool performance = parser.IsPresent("p");
        if (parser.IsPresent("u", out url))
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri) || uri == null)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine($"Invalid url: '{url}'");
                url = "";
                Console.ResetColor();
                invalid = true;
            }
        }
        if (parser.IsPresent("d", out string depthString))
        {
            if (!int.TryParse(depthString, out depth) || depth <= 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine($"Invalid depth: '{depth}'");
                Console.Error.WriteLine("The depth must be a whole number.");
                depth = 1;
                Console.ResetColor();
                invalid = true;
            }
        }

        if (parser.IsPresent("o", out output))
        {
            output = Path.GetFullPath(output);
        }
        else
        {
            output = Path.Combine(Environment.CurrentDirectory, "output.json");
        }

        if (invalid)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine("Exiting application...");
            Console.ResetColor();
            Environment.Exit(1);
            return;
        }

        Scrapper scrapper = new(url, depth, output, performance);
        scrapper.Run().Wait();
    }
}