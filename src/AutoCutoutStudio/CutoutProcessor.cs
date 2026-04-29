using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;

namespace AutoCutoutStudio;

internal sealed record CutoutOptions(int Tolerance, int Feather, int EdgeCleanup, bool KeepShadow);

internal static class CutoutProcessor
{
    public static Bitmap RemoveBackground(Bitmap source, CutoutOptions options)
    {
        using var work = EnsureArgb(source);
        int width = work.Width;
        int height = work.Height;
        var background = EstimateBackground(work);
        var alpha = BuildConnectedMask(work, background, options.Tolerance);

        if (options.EdgeCleanup > 0)
        {
            alpha = GrowTransparentArea(alpha, width, height, options.EdgeCleanup);
        }

        if (options.Feather > 0)
        {
            alpha = Feather(alpha, width, height, options.Feather);
        }

        if (options.KeepShadow)
        {
            RestoreSoftShadow(work, alpha, background, options.Tolerance);
        }

        var output = new Bitmap(width, height, PixelFormat.Format32bppArgb);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Color c = work.GetPixel(x, y);
                output.SetPixel(x, y, Color.FromArgb(alpha[y * width + x], c.R, c.G, c.B));
            }
        }

        return output;
    }

    private static Bitmap EnsureArgb(Bitmap source)
    {
        var copy = new Bitmap(source.Width, source.Height, PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(copy);
        g.DrawImage(source, 0, 0, source.Width, source.Height);
        return copy;
    }

    private static Color EstimateBackground(Bitmap image)
    {
        int width = image.Width;
        int height = image.Height;
        int step = Math.Max(1, Math.Min(width, height) / 80);
        long r = 0;
        long g = 0;
        long b = 0;
        int count = 0;

        for (int x = 0; x < width; x += step)
        {
            Add(image.GetPixel(x, 0));
            Add(image.GetPixel(x, height - 1));
        }

        for (int y = 0; y < height; y += step)
        {
            Add(image.GetPixel(0, y));
            Add(image.GetPixel(width - 1, y));
        }

        return Color.FromArgb((int)(r / count), (int)(g / count), (int)(b / count));

        void Add(Color color)
        {
            r += color.R;
            g += color.G;
            b += color.B;
            count++;
        }
    }

    private static byte[] BuildConnectedMask(Bitmap image, Color background, int tolerance)
    {
        int width = image.Width;
        int height = image.Height;
        var alpha = new byte[width * height];
        Array.Fill<byte>(alpha, 255);

        var visited = new bool[width * height];
        var queue = new Queue<Point>(width + height);
        EnqueueBorder();

        int threshold = tolerance * tolerance * 3;
        while (queue.Count > 0)
        {
            Point p = queue.Dequeue();
            int index = p.Y * width + p.X;
            if (visited[index])
            {
                continue;
            }

            visited[index] = true;
            Color c = image.GetPixel(p.X, p.Y);
            if (ColorDistanceSquared(c, background) > threshold)
            {
                continue;
            }

            alpha[index] = 0;
            Enqueue(p.X - 1, p.Y);
            Enqueue(p.X + 1, p.Y);
            Enqueue(p.X, p.Y - 1);
            Enqueue(p.X, p.Y + 1);
        }

        return alpha;

        void EnqueueBorder()
        {
            for (int x = 0; x < width; x++)
            {
                Enqueue(x, 0);
                Enqueue(x, height - 1);
            }

            for (int y = 1; y < height - 1; y++)
            {
                Enqueue(0, y);
                Enqueue(width - 1, y);
            }
        }

        void Enqueue(int x, int y)
        {
            if (x < 0 || y < 0 || x >= width || y >= height)
            {
                return;
            }

            int index = y * width + x;
            if (!visited[index])
            {
                queue.Enqueue(new Point(x, y));
            }
        }
    }

    private static byte[] GrowTransparentArea(byte[] alpha, int width, int height, int iterations)
    {
        var current = alpha;
        for (int pass = 0; pass < iterations; pass++)
        {
            var next = (byte[])current.Clone();
            for (int y = 1; y < height - 1; y++)
            {
                for (int x = 1; x < width - 1; x++)
                {
                    int index = y * width + x;
                    if (current[index] == 0)
                    {
                        continue;
                    }

                    if (current[index - 1] == 0 || current[index + 1] == 0 ||
                        current[index - width] == 0 || current[index + width] == 0)
                    {
                        next[index] = 0;
                    }
                }
            }

            current = next;
        }

        return current;
    }

    private static byte[] Feather(byte[] alpha, int width, int height, int radius)
    {
        var next = (byte[])alpha.Clone();
        for (int y = radius; y < height - radius; y++)
        {
            for (int x = radius; x < width - radius; x++)
            {
                int index = y * width + x;
                if (alpha[index] != 255)
                {
                    continue;
                }

                int transparentNeighbors = 0;
                int samples = 0;
                for (int yy = -radius; yy <= radius; yy++)
                {
                    for (int xx = -radius; xx <= radius; xx++)
                    {
                        samples++;
                        if (alpha[(y + yy) * width + x + xx] == 0)
                        {
                            transparentNeighbors++;
                        }
                    }
                }

                if (transparentNeighbors > 0)
                {
                    double ratio = transparentNeighbors / (double)samples;
                    next[index] = (byte)Math.Clamp(255 - (ratio * 220), 35, 255);
                }
            }
        }

        return next;
    }

    private static void RestoreSoftShadow(Bitmap image, byte[] alpha, Color background, int tolerance)
    {
        int threshold = tolerance * tolerance * 3;
        for (int y = 0; y < image.Height; y++)
        {
            for (int x = 0; x < image.Width; x++)
            {
                int index = y * image.Width + x;
                if (alpha[index] != 0)
                {
                    continue;
                }

                Color c = image.GetPixel(x, y);
                int distance = ColorDistanceSquared(c, background);
                if (distance > threshold / 3 && distance <= threshold)
                {
                    alpha[index] = (byte)Math.Clamp(Math.Sqrt(distance) * 1.8, 18, 105);
                }
            }
        }
    }

    private static int ColorDistanceSquared(Color a, Color b)
    {
        int dr = a.R - b.R;
        int dg = a.G - b.G;
        int db = a.B - b.B;
        return dr * dr + dg * dg + db * db;
    }
}
