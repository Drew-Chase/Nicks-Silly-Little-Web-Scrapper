// LFInteractive LLC. 2021-2024
﻿

using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace nslws;

public record Link(string source, string target);

internal partial class Scrapper
{
    private readonly string url;
    private readonly int depth;
    private readonly bool performance;
    private readonly string output;

    private readonly HttpClient client;
    private readonly List<string> fetchedUrls;

    public Scrapper(string url, int depth, string output, bool performance)
    {
        this.url = url;
        this.depth = depth;
        this.performance = performance;
        this.output = output;
        this.fetchedUrls = [];
        this.client = new();
    }

    public async Task Run()
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("Nicks Silly Little Web Scrapper");
        Console.ResetColor();

        Stopwatch? watch = null;
        if (performance)
        {
            watch = Stopwatch.StartNew();
        }
        Console.ForegroundColor = ConsoleColor.Blue;
        Console.WriteLine("Finding Links...");
        Console.ResetColor();

        Link[] links = [.. await GetLinks(url, depth)];

        Console.ForegroundColor = ConsoleColor.White;
        Console.Write("Found ");
        Console.ForegroundColor = ConsoleColor.Green;
        Console.Write($"'{links.Length:N0}'");
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine(" Links");
        Console.ResetColor();

        if (performance && watch != null)
        {
            watch.Stop();
            Console.WriteLine($"Get Links took {watch.Elapsed}");
        }
        client.Dispose();
        if (performance)
        {
            watch = Stopwatch.StartNew();
        }

        await Scrape(links);

        if (performance && watch != null)
        {
            watch.Stop();
            Console.WriteLine($"Scrape took {watch.Elapsed}");
        }
    }

    [GeneratedRegex("(?<=('|\"))http(s)?:\\/\\/.*?(?=('|\"))")]
    private static partial Regex LinkMatch();

    private async Task<ConcurrentBag<Link>> GetLinks(string url, int depth)
    {
        if (depth == 0 || fetchedUrls.Contains(url)) return [];
        fetchedUrls.Add(url);

        try
        {
            ConcurrentBag<Link> links = [];
            string html = await client.GetStringAsync(url);

            if (!string.IsNullOrWhiteSpace(html))
            {
                MatchCollection hrefCollection = LinkMatch().Matches(html);

                Parallel.ForEach(hrefCollection, new() { MaxDegreeOfParallelism = 100 }, href =>
                {
                    if (Uri.TryCreate(new Uri(url), href.Value, out Uri fullUrl))
                    {
                        links.Add(new Link(url, fullUrl.ToString()));
                    }
                });

                ConcurrentBag<Link> results = [];

                Parallel.ForEach(links, new() { MaxDegreeOfParallelism = 100 }, link =>
                {
                    results.AddRange(GetLinks(link.target, depth - 1).Result);
                });

                return [.. links, .. results];
            }

            return links;
        }
        catch (Exception)
        {
            return [];
        }
    }

    private async Task Scrape(Link[] links)
    {
        HashSet<object> nodes = [];
        HashSet<object> edges = [];
        foreach (Link link in links)
        {
            nodes.Add(new { id = link.source });
            nodes.Add(new { id = link.target });
            edges.Add(link);
        }
        string json = JsonConvert.SerializeObject(new { nodes, edges }, Formatting.Indented);
        await File.WriteAllTextAsync(output, json);
        Console.ForegroundColor = ConsoleColor.White;
        Console.Write("Saved to ");
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"\"{output}\"");
        Console.ResetColor();

        Console.ForegroundColor = ConsoleColor.White;
        Console.Write("Exporting Nodes ");
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"'{nodes.Count:N0}'");
        Console.ForegroundColor = ConsoleColor.White;
        Console.ResetColor();

        Console.ForegroundColor = ConsoleColor.White;
        Console.Write("Exporting Edges ");
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"'{edges.Count:N0}'");
        Console.ForegroundColor = ConsoleColor.White;
        Console.ResetColor();
    }
}

public static class ConcurrentBagExtensions
{
    public static void AddRange<T>(this ConcurrentBag<T> bag, IEnumerable<T> items)
    {
        foreach (var item in items)
        {
            bag.Add(item);
        }
    }
}