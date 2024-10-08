﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;

namespace JeremyAnsel.Xwa.Imc
{
    public sealed class ImcFile
    {
        public ImcFile()
        {
            this.BitsPerSample = 16;
            this.SampleRate = 22050;
            this.Channels = 1;
        }

        public string? FileName { get; private set; }

        public string? Name
        {
            get
            {
                return Path.GetFileNameWithoutExtension(this.FileName);
            }
        }

        public int BitsPerSample { get; private set; }

        public int SampleRate { get; private set; }

        public int Channels { get; private set; }

        public int DataRawSize { get; private set; }

        public int BlockAlign
        {
            get
            {
                return (this.BitsPerSample / 8) * this.Channels;
            }
        }

        public int AvgBytesPerSec
        {
            get
            {
                return this.SampleRate * this.BlockAlign;
            }
        }

        public int Length
        {
            get
            {
                return this.DataRawSize / this.BlockAlign;
            }
        }

        public double TimeLength
        {
            get
            {
                return (double)this.Length / this.SampleRate;
            }
        }

        public IList<ImcMapItem> Map { get; } = new List<ImcMapItem>();

        private IList<ImcEntry> Entries { get; } = new List<ImcEntry>();

        private IList<string> Codecs { get; } = new List<string>();

        public static ImcFile FromFile(string? fileName)
        {
            if (fileName is null)
            {
                throw new ArgumentNullException(nameof(fileName));
            }

            using var filestream = new FileStream(fileName, FileMode.Open, FileAccess.Read);

            ImcFile imc = FromStream(filestream);
            imc.FileName = fileName;

            return imc;
        }

        [SuppressMessage("Style", "IDE0017:Simplifier l'initialisation des objets", Justification = "Reviewed.")]
        public static ImcFile FromStream(Stream? stream)
        {
            if (stream is null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            var imc = new ImcFile();

            using var file = new BinaryReader(stream, Encoding.ASCII, true);

            if (Encoding.ASCII.GetString(file.ReadBytes(4)) != "MCMP")
            {
                throw new InvalidDataException();
            }

            int entriesCount = file.ReadBigEndianInt16();

            file.BaseStream.Seek(9, SeekOrigin.Current);

            for (int i = 0; i < entriesCount - 1; i++)
            {
                var entry = new ImcEntry();

                entry.Codec = file.ReadByte();
                entry.RawSize = file.ReadBigEndianInt32();
                entry.CompressedSize = file.ReadBigEndianInt32();

                imc.Entries.Add(entry);
            }

            int codecsCount = file.ReadBigEndianInt16() / 5;

            for (int i = 0; i < codecsCount; i++)
            {
                imc.Codecs.Add(Encoding.ASCII.GetString(file.ReadBytes(4)));
                file.ReadByte();
            }

            if (imc.Codecs.Count != 2
                || !string.Equals(imc.Codecs[0], "NULL", StringComparison.Ordinal)
                || !string.Equals(imc.Codecs[1], "VIMA", StringComparison.Ordinal))
            {
                throw new NotSupportedException();
            }

            if (Encoding.ASCII.GetString(file.ReadBytes(4)) != "iMUS")
            {
                throw new InvalidDataException();
            }

            //int imusRawSize = file.ReadInt32();
            file.ReadInt32();

            if (Encoding.ASCII.GetString(file.ReadBytes(4)) != "MAP ")
            {
                throw new InvalidDataException();
            }

            ImcFile.ReadMap(imc, file /*, imusRawSize*/);

            string fourcc = Encoding.ASCII.GetString(file.ReadBytes(4));

            if (fourcc == "RIFF")
            {
                file.BaseStream.Seek(-4, SeekOrigin.Current);

                //var wav = WaveFile.FromStream(file.BaseStream);

                //imc.DataRawSize = wav.Data.Length;

                //var entry = new ImcEntry
                //{
                //    Codec = 0,
                //    RawSize = wav.Data.Length,
                //    CompressedSize = wav.Data.Length,
                //    Data = wav.Data
                //};

                //imc.Entries.Add(entry);

                imc.SetRawDataFromWave(file.BaseStream);
            }
            else if (fourcc == "DATA")
            {
                imc.DataRawSize = file.ReadBigEndianInt32();

                if (imc.DataRawSize != imc.Entries.Sum(t => t.RawSize))
                {
                    throw new InvalidDataException();
                }

                foreach (var entry in imc.Entries)
                {
                    entry.Data = file.ReadBytes(entry.CompressedSize);
                }
            }
            else
            {
                throw new InvalidDataException();
            }

            if (file.BaseStream.Position != file.BaseStream.Length)
            {
                throw new InvalidDataException();
            }

            imc.ComputeEntriesOffsets();

            return imc;
        }

        public void Save(string? fileName)
        {
            if (fileName is null)
            {
                throw new ArgumentNullException(nameof(fileName));
            }

            using var filestream = new FileStream(fileName, FileMode.Create, FileAccess.Write);

            this.Save(filestream);
            this.FileName = fileName;
        }

        public void Save(Stream? stream)
        {
            if (stream is null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            using var file = new BinaryWriter(stream, Encoding.ASCII, true);

            var blocks = this.BuildMapBlocks(out int mapSize);

            file.Write(Encoding.ASCII.GetBytes("MCMP"));

            file.WriteBigEndian((short)(this.Entries.Count + 1));

            file.Write((byte)0);
            file.WriteBigEndian(mapSize + 24);
            file.WriteBigEndian(mapSize + 24);

            foreach (var entry in this.Entries)
            {
                file.Write(entry.Codec);
                file.WriteBigEndian(entry.RawSize);
                file.WriteBigEndian(entry.CompressedSize);
            }

            file.WriteBigEndian((short)(this.Codecs.Count * 5));

            foreach (var codec in this.Codecs)
            {
                file.Write(Encoding.ASCII.GetBytes(codec));
                file.Write((byte)0);
            }

            file.Write(Encoding.ASCII.GetBytes("iMUS"));
            file.Write(this.DataRawSize + mapSize + 16);

            ImcFile.WriteMap(this, file, blocks, mapSize);

            file.Write(Encoding.ASCII.GetBytes("DATA"));
            file.WriteBigEndian(this.DataRawSize);

            foreach (var entry in this.Entries)
            {
                if (entry.Data is null)
                {
                    continue;
                }

                file.Write(entry.Data);
            }
        }

        public void SaveAsWave(string? fileName)
        {
            if (fileName is null)
            {
                throw new ArgumentNullException(nameof(fileName));
            }

            var data = this.RetrieveRawData();

            var wav = new WaveFile
            {
                Channels = this.Channels,
                SampleRate = this.SampleRate,
                BitsPerSample = this.BitsPerSample,
                Data = data
            };

            wav.Save(fileName);
        }

        public void SaveAsWave(Stream? stream)
        {
            if (stream is null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            var data = this.RetrieveRawData();

            var wav = new WaveFile
            {
                Channels = this.Channels,
                SampleRate = this.SampleRate,
                BitsPerSample = this.BitsPerSample,
                Data = data
            };

            wav.Save(stream);
        }

        public byte[] RetrieveRawData()
        {
            byte[] data = new byte[this.DataRawSize];

            this.Entries
                .AsParallel()
                .ForAll(entry =>
                {
                    byte[] buffer;
                    byte[] entryData = entry.Data ?? Array.Empty<byte>();

                    switch (entry.Codec)
                    {
                        case 0:
                            buffer = entryData;
                            break;

                        case 1:
                            buffer = Vima.Decompress(entryData, entry.RawSize);
                            break;

                        default:
                            throw new NotSupportedException();
                    }

                    buffer.CopyTo(data, entry.RawOffset);
                });

            return data;
        }

        public byte[] RetrieveRawData(int start, int end)
        {
            start *= this.BlockAlign;
            end *= this.BlockAlign;

            if (start < 0 || start > this.DataRawSize)
            {
                throw new ArgumentOutOfRangeException(nameof(start));
            }

            if (end < start || end > this.DataRawSize)
            {
                throw new ArgumentOutOfRangeException(nameof(end));
            }

            byte[] data = new byte[end - start];

            this.Entries
                .Where(t => t.RawOffset >= start && t.RawOffset + t.RawSize <= end)
                .AsParallel()
                .ForAll(entry =>
                {
                    byte[] buffer;
                    byte[] entryData = entry.Data ?? Array.Empty<byte>();

                    switch (entry.Codec)
                    {
                        case 0:
                            buffer = entryData;
                            break;

                        case 1:
                            buffer = Vima.Decompress(entryData, entry.RawSize);
                            break;

                        default:
                            throw new NotSupportedException();
                    }

                    int left = Math.Max(entry.RawOffset, start);
                    int right = Math.Min(entry.RawOffset + entry.RawSize, end);

                    Array.Copy(buffer, left - entry.RawOffset, data, left, right - left);
                });

            return data;
        }

        public void SetRawData(byte[]? data, int bitsPerSample, int sampleRate, int channels)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            if (bitsPerSample != 16)
            {
                throw new ArgumentOutOfRangeException(nameof(bitsPerSample));
            }

            if (sampleRate <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(sampleRate));
            }

            if (channels < 1 || channels > 2)
            {
                throw new ArgumentOutOfRangeException(nameof(channels));
            }

            this.Entries.Clear();
            this.Codecs.Clear();
            this.Codecs.Add("NULL");
            this.Codecs.Add("VIMA");

            this.BitsPerSample = bitsPerSample;
            this.SampleRate = sampleRate;
            this.Channels = channels;
            this.DataRawSize = data.Length;

            int remaining = data.Length;

            while (remaining > 0)
            {
                var rawData = new byte[remaining > 8192 ? 8192 : remaining];
                Array.Copy(data, data.Length - remaining, rawData, 0, rawData.Length);

                var compressedData = Vima.Compress(rawData, channels);

                this.Entries.Add(new ImcEntry
                {
                    Codec = 1,
                    RawSize = rawData.Length,
                    CompressedSize = compressedData.Length,
                    Data = compressedData
                });

                remaining -= rawData.Length;
            }

            this.ComputeEntriesOffsets();
        }

        public void SetRawDataFromWave(string? fileName)
        {
            if (fileName is null)
            {
                throw new ArgumentNullException(nameof(fileName));
            }

            var wav = WaveFile.FromFile(fileName);

            this.SetRawData(wav.Data, wav.BitsPerSample, wav.SampleRate, wav.Channels);
        }

        public void SetRawDataFromWave(Stream? stream)
        {
            if (stream is null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            var wav = WaveFile.FromStream(stream);

            this.SetRawData(wav.Data, wav.BitsPerSample, wav.SampleRate, wav.Channels);
        }

        public Stream RetrieveWaveStream()
        {
            return this.RetrieveWaveStream(0, this.Length);
        }

        public Stream RetrieveWaveStream(int start, int end)
        {
            var data = this.RetrieveRawData(start, end);

            var wav = new WaveFile
            {
                Channels = this.Channels,
                SampleRate = this.SampleRate,
                BitsPerSample = this.BitsPerSample,
                Data = data
            };

            var header = wav.BuildHeader();

            var stream = new MemoryStream(header.Length + data.Length);

            try
            {
                stream.Write(header, 0, header.Length);
                stream.Write(data, 0, data.Length);

                stream.Seek(0, SeekOrigin.Begin);
            }
            catch
            {
                stream.Dispose();
                throw;
            }

            return stream;
        }

        private static void ReadMap(ImcFile imc, BinaryReader file /*, int imusRawSize*/)
        {
            int mapSize = file.ReadBigEndianInt32();

            if (Encoding.ASCII.GetString(file.ReadBytes(4)) != "FRMT")
            {
                throw new InvalidDataException();
            }

            int frmtSize = file.ReadBigEndianInt32();

            if (frmtSize != 20)
            {
                throw new InvalidDataException();
            }

            int frmtPosition = file.ReadBigEndianInt32();

            if (mapSize + 24 != frmtPosition)
            {
                throw new InvalidDataException();
            }

            int frmtIsBigEndian = file.ReadBigEndianInt32();

            if (frmtIsBigEndian != 1)
            {
                throw new InvalidDataException();
            }

            imc.BitsPerSample = file.ReadBigEndianInt32();
            imc.SampleRate = file.ReadBigEndianInt32();
            imc.Channels = file.ReadBigEndianInt32();

            int blockAlign = imc.BlockAlign;

            while (true)
            {
                string fourcc = Encoding.ASCII.GetString(file.ReadBytes(4));

                if (string.Equals(fourcc, "STOP", StringComparison.Ordinal))
                {
                    int size = file.ReadBigEndianInt32();

                    if (size != 4)
                    {
                        throw new InvalidDataException();
                    }

                    //int position = file.ReadBigEndianInt32() - frmtPosition;
                    file.ReadBigEndianInt32();

                    // TODO
                    //if (position != imusRawSize - mapSize - 16)
                    //{
                    //    throw new InvalidDataException();
                    //}

                    break;
                }

                switch (fourcc)
                {
                    case "REGN":
                        var regn = new ImcRegnBlock();
                        regn.Read(file, frmtPosition);
                        break;

                    case "TEXT":
                        var text = new ImcTextBlock();
                        text.Read(file, frmtPosition);

                        imc.Map.Add(new ImcText
                        {
                            Position = text.Position / blockAlign,
                            Text = text.Text
                        });
                        break;

                    case "JUMP":
                        var jump = new ImcJumpBlock();
                        jump.Read(file, frmtPosition);

                        imc.Map.Add(new ImcJump
                        {
                            Position = jump.Position / blockAlign,
                            Destination = jump.Destination / blockAlign,
                            HookId = jump.HookId,
                            Delay = jump.Delay
                        });
                        break;

                    default:
                        throw new NotSupportedException("Unknown block " + fourcc);
                }
            }
        }

        private static void WriteMap(ImcFile imc, BinaryWriter file, List<ImcBlock> blocks, int mapSize)
        {
            file.Write(Encoding.ASCII.GetBytes("MAP "));
            file.WriteBigEndian(mapSize);

            file.Write(Encoding.ASCII.GetBytes("FRMT"));
            file.WriteBigEndian(20);
            file.WriteBigEndian(mapSize + 24);
            file.WriteBigEndian(1);
            file.WriteBigEndian(imc.BitsPerSample);
            file.WriteBigEndian(imc.SampleRate);
            file.WriteBigEndian(imc.Channels);

            foreach (var block in blocks)
            {
                block.Write(file, mapSize + 24);
            }

            file.Write(Encoding.ASCII.GetBytes("STOP"));
            file.WriteBigEndian(4);
            file.WriteBigEndian(imc.DataRawSize + mapSize + 24);
        }

        private void ComputeEntriesOffsets()
        {
            int entryRawOffset = 0;
            //int entryCompressedOffset = 0;

            foreach (var entry in this.Entries)
            {
                entry.RawOffset = entryRawOffset;
                //entry.CompressedOffset = entryCompressedOffset;

                entryRawOffset += entry.RawSize;
                //entryCompressedOffset += entry.CompressedSize;
            }
        }

        private List<ImcBlock> BuildMapBlocks(out int mapSize)
        {
            List<ImcMapItem> mapItems = this.Map.OrderBy(t => t.Position).ToList();

            this.Map.Clear();

            foreach (ImcMapItem map in mapItems)
            {
                this.Map.Add(map);
            }

            var blocks = new List<ImcBlock>();

            void fillRegion(int position)
            {
                var lastPosition = blocks.Count != 0 ? blocks.Last().Position : 0;

                if (lastPosition != position)
                {
                    blocks.Add(new ImcRegnBlock
                    {
                        Position = lastPosition,
                        Length = position - lastPosition
                    });
                }
            }

            int blockAlign = this.BlockAlign;

            foreach (var item in this.Map)
            {
                fillRegion(item.Position * blockAlign);

                if (item is ImcText text)
                {
                    blocks.Add(new ImcTextBlock
                    {
                        Position = text.Position * blockAlign,
                        Text = text.Text
                    });
                }
                else if (item is ImcJump jump)
                {
                    blocks.Add(new ImcJumpBlock
                    {
                        Position = jump.Position * blockAlign,
                        Destination = jump.Destination * blockAlign,
                        HookId = jump.HookId,
                        Delay = jump.Delay
                    });
                }
                else
                {
                    throw new InvalidDataException();
                }
            }

            fillRegion(this.DataRawSize);

            mapSize = blocks.Sum(t => t.Size + 8) + 12 + 28;

            return blocks;
        }
    }
}
