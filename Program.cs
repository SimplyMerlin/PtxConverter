using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;

namespace PtxConverter {
        class Program {
        static void Main(string[] args) {
            foreach(String arg in args) {
                var allBytes = new List<byte>();
                Console.WriteLine("Attempting to read the hex data of " + arg + "...");
                FileStream fs = new FileStream(arg, FileMode.Open);
                //header code
                fs.Seek(-16, SeekOrigin.End);
                byte[] header = new byte[16];
                fs.Read(header, 0, 16);
                int width = BitConverter.ToInt32(SubArray(header, 0, 4), 0);
                int height = BitConverter.ToInt32(SubArray(header, 4, 4), 0);
                int widthRounded = width - (width % 4) + (width % 4 == 0 ? 0 : 4);
                int heightRounded = height - (height % 4) + (height % 4 == 0 ? 0 : 4);
                int rowSize = BitConverter.ToInt32(SubArray(header, 8, 4), 0);
                Console.WriteLine("Detected width of " + width + " and a height of " + height + ".");
                Console.WriteLine("The rowsize is " + rowSize);

                fs.Seek(0, SeekOrigin.Begin);
                int hexIn;
                Boolean swapMode = false;
                var swapPool = new List<byte>();
                for (int i = 0; (hexIn = fs.ReadByte()) != -1; i++){
                    if ((i + (((rowSize / 16) - (widthRounded / 4)) * 16)) % rowSize == 0 && i != 0 && (((rowSize / 16) - (widthRounded / 4)) * 16) != 0) {
                        fs.Seek((((rowSize / 16) - (widthRounded / 4)) * 16 - 1), SeekOrigin.Current);
                        i += (((rowSize / 16) - (widthRounded / 4)) * 16 - 1);
                        continue;
                    }
                    if (i % 2 == 0 || swapMode) {
                        swapMode = true;
                        swapPool.Add((byte)hexIn);
                        if (swapPool.Count >= 2) {
                            allBytes.Add(swapPool[1]);
                            allBytes.Add(swapPool[0]);
                            swapPool.Clear();
                        }
                    } else {
                        allBytes.Add((byte)hexIn);
                    }
                }
                Console.WriteLine("Successfully swapped the bytes...");
                var allBytesArray = allBytes.ToArray();
                Console.WriteLine("Converted Byte list to array...");
                Directory.CreateDirectory("converted");
                var s = File.OpenWrite(@"converted\" + Path.GetFileName(arg));
                var bw = new BinaryWriter(s);
                bw.Write(allBytesArray);
                bw.Close();
                fs.Close();
                s.Close();
                String startArgument = "\"" + s.Name + "\" BC3 0 " + widthRounded + " " + heightRounded;
                Console.WriteLine("Starting rawtex with the startargument: " + startArgument);
                Process process = new Process();
                process.StartInfo.FileName = "RawtexCmd.exe";
                process.StartInfo.Arguments = startArgument;
                process.Start();
                process.WaitForExit();
                Console.WriteLine("Rawtex finished!");
                if (height != heightRounded || width != widthRounded) {
                    Console.WriteLine("Opening file " + Path.ChangeExtension(s.Name, "dds"));
                    using (FileStream ddsFile = new FileStream(Path.ChangeExtension(s.Name, "dds"), FileMode.Open)) {
                        ddsFile.Seek(12, SeekOrigin.Begin);
                        ddsFile.Write(BitConverter.GetBytes((UInt16)height), 0, 2);
                        ddsFile.Seek(16, SeekOrigin.Begin);
                        ddsFile.Write(BitConverter.GetBytes((UInt16)width), 0, 2);
                        ddsFile.Close();
                    }
                }
                File.Delete(s.Name);
                Console.WriteLine("Success!");
                Console.WriteLine("----------");
            }
            Console.WriteLine("Thank you. Converter made by SimplyMerlin, special thanks to LolHacksRule for getting me on the right path.");
            Console.ReadKey();
        }
        public static Byte[] SubArray(byte[] data, int index, int length) {
            byte[] result = new byte[length];
            Array.Copy(data, index, result, 0, length);
            Array.Reverse(result);
            return result;
        }
    }
}