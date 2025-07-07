using System;
using System.IO;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace wxl2png;

class Program
{
    static void Main(string[] args)
    {
        if (args.Length <= 0)
        {
            Console.WriteLine("Usage: wxl2png <file.wxl>");
            return;
        }

        string file = args[0];

        if (File.Exists(file))
        {
            Console.WriteLine("File not found: {0}", file);
            return;
        }

        FileInfo fi = new FileInfo(file);
        string? fileLocation = Path.GetDirectoryName(file);
        if (fileLocation == null)
        {
            Console.WriteLine("File location not found: {0}", file);
            return;
        }
        
        string fileWithoutExt = Path.GetFileNameWithoutExtension(file);
        string outputFile = Path.Combine(fileLocation, fileWithoutExt + ".png");

        Console.WriteLine("Input File = {0}", file);

        // Open file as stream
        using FileStream fs = File.OpenRead(file);

        // Move to start of the stream
        fs.Seek(0, SeekOrigin.Begin);

        // Start reading header
        int width = 0, height = 0;
        byte[] buffer = new byte[4];
        int index = 0;
        bool hasWidth = false, hasHeight = false;

        while (!hasWidth || !hasHeight)
        {
            int b = fs.ReadByte();

            // Reached the end of the value
            if (b == 0x20)
            {
                // found the width, 
                if (!hasWidth)
                {
                    hasWidth = true;
                    width = int.Parse(Encoding.ASCII.GetString(buffer));
                    // reset the buffer
                    buffer = new byte[4];
                    index = 0;
                    continue;
                }

                // found the height
                if (!hasHeight)
                {
                    hasHeight = true;
                    height = int.Parse(Encoding.ASCII.GetString(buffer));
                    // reset the buffer, break out since we're done
                    break;
                }
            }

            // Add byte to the buffer
            buffer[index++] = (byte)b;
        } // done parsing header

        if (width == 0 || height == 0)
        {
            Console.WriteLine("Unable to find width/height. Are you sure this is a wxl file?");
            return;
        }

        Console.WriteLine("Width = {0}", width);
        Console.WriteLine("Height = {0}", height);

        // Start creating bitmap
        Bitmap bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);
        BitmapData bitmapData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.WriteOnly, bmp.PixelFormat);

        // Seek to start of image data
        fs.Seek(256, SeekOrigin.Begin);

        // Get final file size, minus 256 byte header
        long fileSize = fi.Length - 256;

        // Read data from stream into a byte array
        byte[] fileBuffer = new byte[fileSize];
        fs.Read(fileBuffer, 0, (int)fileSize);

        // Convert RGBA to ARGB
        for (int i = 0; i < fileBuffer.Length; i += 4)
        {
            byte R = fileBuffer[i];
            byte G = fileBuffer[i + 1];
            byte B = fileBuffer[i + 2];
            byte A = fileBuffer[i + 3];

            fileBuffer[i] = B;
            fileBuffer[i + 1] = G;
            fileBuffer[i + 2] = R;
            fileBuffer[i + 3] = A;
        }

        // Copy byte array into the bitmap
        Marshal.Copy(fileBuffer, 0, bitmapData.Scan0, (int)fileSize);

        // Finalize and save png
        bmp.UnlockBits(bitmapData);
        bmp.RotateFlip(RotateFlipType.RotateNoneFlipY);
        bmp.Save(outputFile, ImageFormat.Png);

        Console.WriteLine("File saved: {0}", outputFile);
    }
}