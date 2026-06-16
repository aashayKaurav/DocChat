using System;
using System.Collections.Generic;

namespace DocChat.Consumer.Services;

public class TextChunkerService
{
    private const int MaxChunkSize = 500;  // characters (rough proxy for tokens)
    private const int Overlap = 50;        // overlap between chunks for context continuity

    public List<string> ChunkText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return new List<string>();

        var chunks = new List<string>();
        var start = 0;

        while (start < text.Length)
        {
            var length = Math.Min(MaxChunkSize, text.Length - start);
            var chunk = text.Substring(start, length);

            // Try to break at a sentence or paragraph boundary
            if (start + length < text.Length)
            {
                var lastPeriod = chunk.LastIndexOf('.');
                var lastNewline = chunk.LastIndexOf('\n');
                var breakPoint = Math.Max(lastPeriod, lastNewline);

                if (breakPoint > MaxChunkSize / 2)
                {
                    chunk = chunk.Substring(0, breakPoint + 1);
                    length = breakPoint + 1;
                }
            }

            chunks.Add(chunk.Trim());
            start += Math.Max(length - Overlap, 1);
        }

        return chunks.Where(c => !string.IsNullOrWhiteSpace(c)).ToList();
    }
}