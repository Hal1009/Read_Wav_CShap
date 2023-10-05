using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Media;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WpfApp1_wav
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        class WavHeader
        {
            public byte[] RIFFHeader { get; set; } = new byte[4];
            public byte[] WavFileSize { get; set; } = new byte[4];
            public byte[] WaveHeader { get; set; } = new byte[4];
        }

        class WavFmtChunkData
        {
            public byte[] Chunk { get; set; } = new byte[4];
            public byte[] ChunkSize { get; set; } = new byte[4];
            public byte[] FmtTag { get; set; } = new byte[2];
            public byte[] Channels { get; set; } = new byte[2];
            public byte[] SampleRate { get; set; } = new byte[4];
            public byte[] BitsRate { get; set; } = new byte[4];
            public byte[] BlockAlign { get; set; } = new byte[2];
            public byte[] BitsPerSecond { get; set; }
        }

        class WavDataChunkData
        {
            public byte[] Chunk { get; set; }
            public byte[] ChunkSize { get; set; }
            public byte[] SampleData { get; set; }

            public int PlayTimeMicroSecond { get; set; }

        }

        class WavData
        {
            public WavHeader Header { get; set; } = new WavHeader();
            public WavFmtChunkData FmtChunkData { get; set; } = new WavFmtChunkData();
            public WavDataChunkData WavDataChunkData { get; set; } = new WavDataChunkData();

            public bool IsReadConpleted { get; set; } = false;
        }

        public List<string> RLogs { get; set; } = new List<string>();
        public List<string> LLogs { get; set; } = new List<string>();


        public MainWindow()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            string Filename = @"D:\projects\cshap\maou_bgm_piano40.wav";
            string Bin = @"D:\\projects\\cshap\\WpfApp1_wav\\Music.wav";

            SoundPlayer player = new SoundPlayer(Bin);
            //player.Play();
            ReadWav(Bin, out WavData wavData);

            if (wavData.IsReadConpleted)
            {
                ConvertWavData(wavData);

                Play(wavData);
            }
        }

        private void ReadWav(string filename, out WavData wavData)
        {
            wavData = new WavData { IsReadConpleted = false, };

            using (FileStream stream = new FileStream(filename, FileMode.Open))
            {
                BinaryReader reader = new BinaryReader(stream);

                // RIFF
                WavHeader wavHeader = new WavHeader
                {
                    RIFFHeader = reader.ReadBytes(4),
                    WavFileSize = reader.ReadBytes(4),
                    WaveHeader = reader.ReadBytes(4)
                };
                wavData.Header = wavHeader;

                WavFmtChunkData wavFmtChunkData = new WavFmtChunkData();
                WavDataChunkData wavDataChunkData = new WavDataChunkData();

                bool readFmtChunk = false;
                bool readDataChunk = false;

                try
                {
                    while (!readFmtChunk || !readDataChunk)
                    {
                        byte[] chunk = reader.ReadBytes(4);

                        if (chunk.SequenceEqual(Encoding.UTF8.GetBytes("fmt ")))
                        {
                            wavFmtChunkData.Chunk = chunk;
                            wavFmtChunkData.ChunkSize = reader.ReadBytes(4);
                            wavFmtChunkData.FmtTag = reader.ReadBytes(2);
                            wavFmtChunkData.Channels = reader.ReadBytes(2);
                            wavFmtChunkData.SampleRate = reader.ReadBytes(4);
                            wavFmtChunkData.BitsRate = reader.ReadBytes(4);
                            wavFmtChunkData.BlockAlign = reader.ReadBytes(2);
                            wavFmtChunkData.BitsPerSecond = reader.ReadBytes(2);

                            readFmtChunk = true;
                        }
                        else if (chunk.SequenceEqual(Encoding.UTF8.GetBytes("data")))
                        {
                            wavDataChunkData.Chunk = chunk;
                            wavDataChunkData.ChunkSize = reader.ReadBytes(4);
                            wavDataChunkData.SampleData = reader.ReadBytes(BitConverter.ToInt32(wavDataChunkData.ChunkSize));

                            // 再生時間
                            //int bytesPerSecond = BitConverter.ToInt32(wavFmtChunkData.SampleRate) * BitConverter.ToInt32(wavFmtChunkData.BlockAlign);
                            //wavDataChunkData.PlayTimeMicroSecond = (int)(((double)BitConverter.ToInt32(wavDataChunkData.ChunkSize) / (double)bytesPerSecond) * 1000);

                            readDataChunk = true;
                        }
                        else
                        {
                            // 不要なデータは読み捨て
                            int size = BitConverter.ToInt32(reader.ReadBytes(4));
                            if (0 < size)
                            {
                                reader.ReadBytes(size);
                            }
                        }
                    }
                    wavData.FmtChunkData = wavFmtChunkData;
                    wavData.WavDataChunkData = wavDataChunkData;
                    wavData.IsReadConpleted = true;
                }
                catch
                {
                    reader.Close();
                }
            }
        }

        private void Play(WavData wavData)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                try 
                {
                    stream.Write(wavData.Header.RIFFHeader, 0, 4);
                    stream.Write(wavData.Header.WavFileSize, 0, 4);
                    stream.Write(wavData.Header.WaveHeader, 0, 4);

                    stream.Write(wavData.FmtChunkData.Chunk, 0, 4);
                    stream.Write(wavData.FmtChunkData.ChunkSize, 0, 4);
                    stream.Write(wavData.FmtChunkData.FmtTag, 0, 2);
                    stream.Write(wavData.FmtChunkData.Channels, 0, 2);
                    stream.Write(wavData.FmtChunkData.SampleRate, 0, 4);
                    stream.Write(wavData.FmtChunkData.BitsRate, 0, 4);
                    stream.Write(wavData.FmtChunkData.BlockAlign, 0, 2);
                    stream.Write(wavData.FmtChunkData.BitsPerSecond, 0, 2);

                    stream.Write(wavData.WavDataChunkData.Chunk, 0, 4);
                    stream.Write(wavData.WavDataChunkData.ChunkSize, 0, 4);
                    stream.Write(wavData.WavDataChunkData.SampleData, 0, BitConverter.ToInt32(wavData.WavDataChunkData.ChunkSize));

                    stream.Seek(0, SeekOrigin.Begin);
                    using (FileStream fs = new FileStream("D:\\projects\\cshap\\WpfApp1_wav\\Music.wav", FileMode.Create))
                    {
                        stream.WriteTo(fs);
                    }
                    SoundPlayer player = new SoundPlayer(stream);
                    player.Play();


                }
                catch
                {
                    stream.Close();
                }
            }
        }

        private void ConvertWavData(WavData wavData)
        {
            int dataChunkSize = BitConverter.ToInt32(wavData.WavDataChunkData.ChunkSize);
            int channels = BitConverter.ToInt16(wavData.FmtChunkData.Channels);
            int bitsPerSecond = BitConverter.ToInt16(wavData.FmtChunkData.BitsPerSecond);
            int[] R = new int[(dataChunkSize / channels) / (bitsPerSecond / 8)];
            int[] L = new int[(dataChunkSize / channels) / (bitsPerSecond / 8)];

            int frameIndex = 0;
            int channelIndex = 0;
            int size = dataChunkSize / (bitsPerSecond / 8);
            for (int i =0;i<dataChunkSize / (bitsPerSecond / 8);i++)
            {
                byte[] data = new byte[2];
                int work = 0;

                switch (bitsPerSecond)
                {
                    case 8:
                        work = (int)wavData.WavDataChunkData.SampleData[frameIndex];
                        frameIndex += 1;
                        break;
                    case 16:
                        Array.Copy(wavData.WavDataChunkData.SampleData, frameIndex, data, 0, 2);
                        work = (int)BitConverter.ToInt16(data, 0);
                        frameIndex += 2;
                        break;
                }

                if(channels == 1) 
                {
                    R[i] = work;
                }
                else
                {
                    if(channelIndex == 0) 
                    {
                        channelIndex = 1;
                        R[i / 2] = work;
                        RLogs.Add(work.ToString());
                    }
                    else
                    {
                        channelIndex = 0;
                        L[i/2] = work;
                        LLogs.Add(work.ToString());
                    }
                }
            }
        }

    }
}
