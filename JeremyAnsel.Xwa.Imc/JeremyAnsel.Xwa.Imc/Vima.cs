using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JeremyAnsel.Xwa.Imc
{
    public static class Vima
    {
        private static readonly ushort[] StepTable =
        {
            7, 8, 9, 10, 11, 12, 13, 14, 16, 17,
            19, 21, 23, 25, 28, 31, 34, 37, 41, 45,
            50, 55, 60, 66, 73, 80, 88, 97, 107, 118,
            130, 143, 157, 173, 190, 209, 230, 253, 279, 307,
            337, 371, 408, 449, 494, 544, 598, 658, 724, 796,
            876, 963, 1060, 1166, 1282, 1411, 1552, 1707, 1878, 2066,
            2272, 2499, 2749, 3024, 3327, 3660, 4026, 4428, 4871, 5358,
            5894, 6484, 7132, 7845, 8630, 9493, 10442, 11487, 12635, 13899,
            15289, 16818, 18500, 20350, 22385, 24623, 27086, 29794, 32767
        };

        private static ushort[] VimaPredictTable = BuildPredictTable();

        private static readonly byte[] SizeTable =
        {
            4, 4, 4, 4, 4, 4, 4, 4, 4, 4,
            4, 4, 4, 4, 4, 4, 4, 4, 4, 4,
            4, 4, 4, 4, 4, 4, 4, 4, 4, 4,
            4, 4, 4, 4, 4, 4, 4, 4, 4, 4,
            4, 4, 4, 4, 4, 5, 5, 5, 5, 5,
            5, 5, 5, 5, 5, 5, 5, 5, 5, 6,
            6, 6, 6, 6, 6, 6, 6, 6, 6, 6,
            6, 6, 6, 6, 7, 7, 7, 7, 7, 7,
            7, 7, 7, 7, 7, 7, 7, 7, 7
        };

        private static readonly sbyte[] IndexTable2 =
        {
            -1, 4, -1, 4
        };

        private static readonly sbyte[] IndexTable3 =
        {
            -1, -1, 2, 6, -1, -1, 2, 6
        };

        private static readonly sbyte[] IndexTable4 =
        {
            -1, -1, -1, -1, 1, 2, 4, 6, -1, -1,
            -1, -1, 1, 2, 4, 6
        };

        private static readonly sbyte[] IndexTable5 =
        {
            -1, -1, -1, -1, -1, -1, -1, -1, 1, 1,
            1, 2, 2, 4, 5, 6, -1, -1, -1, -1,
            -1, -1, -1, -1, 1, 1, 1, 2, 2, 4,
            5, 6
        };

        private static readonly sbyte[] IndexTable6 =
        {
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, 1, 1, 1, 1,
            1, 2, 2, 2, 2, 4, 4, 4, 5, 5,
            6, 6, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, 1, 1,
            1, 1, 1, 2, 2, 2, 2, 4, 4, 4,
            5, 5, 6, 6
        };

        private static readonly sbyte[] IndexTable7 =
        {
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, 1, 1, 1, 1, 1, 1, 1, 1,
            1, 1, 2, 2, 2, 2, 2, 2, 2, 2,
            4, 4, 4, 4, 4, 4, 5, 5, 5, 5,
            6, 6, 6, 6, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, 1, 1, 1, 1,
            1, 1, 1, 1, 1, 1, 2, 2, 2, 2,
            2, 2, 2, 2, 4, 4, 4, 4, 4, 4,
            5, 5, 5, 5, 6, 6, 6, 6,
        };

        private static readonly sbyte[][] IndexTables =
        {
            null,
            null,
            IndexTable2,
            IndexTable3,
            IndexTable4,
            IndexTable5,
            IndexTable6,
            IndexTable7
        };

        private static ushort[] BuildPredictTable()
        {
            ushort[] predictTable = new ushort[5696];

            for (int pos = 0; pos < 0x40; pos++)
            {
                for (int stepIndex = 0; stepIndex < 0x59; stepIndex++)
                {
                    ushort step = StepTable[stepIndex];

                    ushort mask = 0x20;
                    ushort predict = 0;

                    while (mask != 0)
                    {
                        if ((pos & mask) != 0)
                        {
                            predict += step;
                        }

                        mask >>= 1;
                        step >>= 1;
                    }

                    predictTable[stepIndex * 0x40 + pos] = predict;
                }
            }

            return predictTable;
        }

        public static byte[] Decompress(byte[] input, int decompressedSize)
        {
            if (input == null)
            {
                throw new ArgumentNullException("input");
            }

            if (decompressedSize <= 0)
            {
                throw new ArgumentOutOfRangeException("decompressedSize");
            }

            int inputIndex = 0;

            byte[] destBuffer = new byte[decompressedSize];

            byte[] headerBytes = new byte[2];
            short[] headerWords = new short[2];

            int channelsCount = 1;

            headerBytes[0] = input[inputIndex++];
            headerWords[0] = (short)((input[inputIndex++] << 8) | input[inputIndex++]);

            if (headerBytes[0] >= 0x80)
            {
                headerBytes[0] = (byte)~headerBytes[0];
                channelsCount++;
            }

            if (channelsCount > 1)
            {
                headerBytes[1] = input[inputIndex++];
                headerWords[1] = (short)((input[inputIndex++] << 8) | input[inputIndex++]);
            }

            int samplesCount = decompressedSize / (channelsCount * 2);

            int bitPtr = 0;
            uint bits = input[inputIndex++];

            for (int channel = 0; channel < channelsCount; channel++)
            {
                int destIndex = channel * 2;
                int currTablePos = headerBytes[channel];
                int outputWord = headerWords[channel];

                for (int sample = 0; sample < samplesCount; sample++)
                {
                    int numBits = SizeTable[currTablePos];

                    ushort highBit = (ushort)(1U << (numBits - 1));
                    ushort lowBits = (ushort)(highBit - 1);

                    bitPtr += numBits;

                    if (bitPtr > 8)
                    {
                        bits = (bits << 8) | input[inputIndex++];
                        bitPtr -= 8;
                    }

                    ushort val = (ushort)((bits >> (8 - bitPtr)) & (highBit | lowBits));

                    if ((highBit & val) != 0)
                    {
                        val ^= highBit;
                    }
                    else
                    {
                        highBit = 0;
                    }

                    if (val == lowBits)
                    {
                        bits = (bits << 8) | input[inputIndex++];
                        bits = (bits << 8) | input[inputIndex++];

                        outputWord = (short)(bits >> (8 - bitPtr));
                    }
                    else
                    {
                        int index = (val << (7 - numBits)) | (currTablePos << 6);

                        ushort delta = VimaPredictTable[index];

                        if (val != 0)
                        {
                            delta += (ushort)(StepTable[currTablePos] >> (numBits - 1));
                        }

                        if (highBit != 0)
                        {
                            outputWord -= delta;

                            if (outputWord < -0x8000)
                            {
                                outputWord = -0x8000;
                            }
                        }
                        else
                        {
                            outputWord += delta;

                            if (outputWord > 0x7fff)
                            {
                                outputWord = 0x7fff;
                            }
                        }
                    }

                    destBuffer[destIndex] = (byte)outputWord;
                    destBuffer[destIndex + 1] = (byte)(outputWord >> 8);

                    destIndex += channelsCount * 2;

                    currTablePos += IndexTables[numBits][val];

                    if (currTablePos < 0)
                    {
                        currTablePos = 0;
                    }
                    else if (currTablePos > 88)
                    {
                        currTablePos = 88;
                    }
                }
            }

            return destBuffer;
        }

        public static byte[] Compress(byte[] input, int channelsCount)
        {
            if (input == null)
            {
                throw new ArgumentNullException("input");
            }

            if (channelsCount < 1 || channelsCount > 2)
            {
                throw new ArgumentOutOfRangeException("channelsCount");
            }

            byte[] destBuffer = new byte[input.Length + channelsCount * 3];
            int destIndex = 0;

            if (channelsCount > 1)
            {
                destBuffer[destIndex++] = 255;
            }
            else
            {
                destBuffer[destIndex++] = 0;
            }

            destBuffer[destIndex++] = 0;
            destBuffer[destIndex++] = 0;

            if (channelsCount > 1)
            {
                destBuffer[destIndex++] = 0;
                destBuffer[destIndex++] = 0;
                destBuffer[destIndex++] = 0;
            }

            int samplesCount = input.Length / (channelsCount * 2);

            int bitPtr = 0;
            uint bits = 0;

            for (int channel = 0; channel < channelsCount; channel++)
            {
                int inputIndex = channel * 2;
                int currTablePos = 0;
                int currentWord = 0;

                for (int sample = 0; sample < samplesCount; sample++)
                {
                    int step = StepTable[currTablePos];

                    int index = BitConverter.ToInt16(input, inputIndex) - currentWord;

                    int numBits = SizeTable[currTablePos];

                    uint highBit = 1U << (numBits - 1);
                    uint lowBits = highBit - 1;

                    uint high = highBit;

                    if (index < 0)
                    {
                        index = -index;
                    }
                    else
                    {
                        highBit = 0;
                    }

                    uint val = 0;
                    int delta = 0;

                    for (int i = 0; i < numBits - 1; i++)
                    {
                        high >>= 1;

                        if (index >= step)
                        {
                            val |= high;

                            index -= step;
                            delta += step;
                        }

                        step >>= 1;
                    }

                    if (val != 0)
                    {
                        delta += step;
                    }

                    int bitsLeft = 8 - bitPtr;

                    bits = (bits << numBits) | (val | highBit);
                    bitPtr += numBits;
                    bitPtr &= 7;

                    if (numBits >= bitsLeft)
                    {
                        destBuffer[destIndex++] = (byte)(bits >> (numBits - bitsLeft));
                    }

                    if (val == lowBits)
                    {
                        currentWord = BitConverter.ToInt16(input, inputIndex);

                        bits = (bits << 8) | (((uint)currentWord >> 8) & 0xff);

                        destBuffer[destIndex++] = (byte)(bits >> bitPtr);

                        bits = (bits << 8) | ((uint)currentWord & 0xff);

                        destBuffer[destIndex++] = (byte)(bits >> bitPtr);
                    }
                    else
                    {
                        if (highBit != 0)
                        {
                            currentWord -= delta;
                        }
                        else
                        {
                            currentWord += delta;
                        }

                        if (currentWord < -0x8000)
                        {
                            currentWord = -0x8000;
                        }
                        else if (currentWord > 0x7fff)
                        {
                            currentWord = 0x7fff;
                        }
                    }

                    currTablePos += IndexTables[numBits][val];

                    if (currTablePos < 0)
                    {
                        currTablePos = 0;
                    }
                    else if (currTablePos > 88)
                    {
                        currTablePos = 88;
                    }

                    inputIndex += channelsCount * 2;
                }
            }

            if (bitPtr != 0)
            {
                bits <<= 8 - bitPtr;

                destBuffer[destIndex++] = (byte)bits;
            }

            byte[] buffer = new byte[destIndex];
            Array.Copy(destBuffer, buffer, destIndex);

            return buffer;
        }
    }
}
