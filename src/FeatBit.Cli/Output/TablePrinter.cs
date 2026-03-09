namespace FeatBit.Cli.Output;

public static class TablePrinter
{
    public static void Print(TextWriter writer, IReadOnlyList<string> headers, IReadOnlyList<IReadOnlyList<string>> rows)
    {
        if (headers.Count == 0)
        {
            return;
        }

        var widths = new int[headers.Count];
        for (var i = 0; i < headers.Count; i++)
        {
            widths[i] = headers[i].Length;
        }

        foreach (var row in rows)
        {
            for (var i = 0; i < headers.Count && i < row.Count; i++)
            {
                widths[i] = Math.Max(widths[i], row[i].Length);
            }
        }

        WriteRow(writer, headers, widths);
        WriteSeparator(writer, widths);

        foreach (var row in rows)
        {
            WriteRow(writer, row, widths);
        }
    }

    private static void WriteRow(TextWriter writer, IReadOnlyList<string> columns, IReadOnlyList<int> widths)
    {
        for (var i = 0; i < widths.Count; i++)
        {
            var value = i < columns.Count ? columns[i] : string.Empty;
            writer.Write(value.PadRight(widths[i]));
            if (i < widths.Count - 1)
            {
                writer.Write(" | ");
            }
        }

        writer.WriteLine();
    }

    private static void WriteSeparator(TextWriter writer, IReadOnlyList<int> widths)
    {
        for (var i = 0; i < widths.Count; i++)
        {
            writer.Write(new string('-', widths[i]));
            if (i < widths.Count - 1)
            {
                writer.Write("-+-");
            }
        }

        writer.WriteLine();
    }
}
