﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MpegCompressor {
    class DCT : Node {
        private static int chunkSize = 8;
        private byte[][] channels;
        private int width, height;
        private Subsample.Samples samples;
        private bool isInverse;
        /// <summary>
        /// Luminance quantization table. 
        /// Table 9.2, pg 284, Fundamentals of Multimedia textbook.
        /// </summary>
        private static int[,] quantizationY = 
            {
            {16, 11, 10, 16, 24, 40, 51, 61},
            {12, 12, 14, 19, 26, 58, 60, 55},
            {14, 13, 16, 24, 40, 57, 69, 56},
            {14, 17, 22, 29, 51, 87, 80, 62},
            {18, 22, 37, 56, 68, 109, 103, 77},
            {24, 35, 55, 64, 81, 104, 113, 92},
            {49, 64, 78, 87, 103, 121, 120, 101},
            {72, 92, 95, 98, 112, 100, 103, 99}
            };
        /// <summary>
        /// Chrominance quantization table. 
        /// Table 9.2, pg 284, Fundamentals of Multimedia textbook.
        /// </summary>
        private static int[,] quantizationC =
            {
            {17, 18, 24, 47, 99, 99, 99, 99},
            {18, 21, 26, 66, 99, 99, 99, 99},
            {24, 26, 56, 99, 99, 99, 99, 99},
            {47, 66, 99, 99, 99, 99, 99, 99},
            {99, 99, 99, 99, 99, 99, 99, 99},
            {99, 99, 99, 99, 99, 99, 99, 99},
            {99, 99, 99, 99, 99, 99, 99, 99},
            {99, 99, 99, 99, 99, 99, 99, 99}
            };

        public DCT() {
            rename("DCT");
        }

        protected override void createInputs() {
            inputs.Add("inChannels", null);
        }

        protected override void createOutputs() {
            outputs.Add("outChannels", new HashSet<Address>());
        }

        protected override void createProperties() {
            Property p = new Property();
            p.createCheckbox("Inverse");
            p.eValueChanged += P_eValueChanged; ;
            properties["isInverse"] = p;
        }

        private void P_eValueChanged(object sender, EventArgs e) {
            isInverse = properties["isInverse"].getChecked();
            soil();
        }

        public override DataBlob getData(string port) {
            base.getData(port);
            DataBlob d = new DataBlob();
            d.type = DataBlob.Type.Channels;
            d.channels = channels;
            d.width = width;
            d.height = height;
            d.samplingMode = samples;
            return d;
        }

        protected override void clean() {
            base.clean();
            
            //Acquire source
            Address upstream = inputs["inChannels"];
            if (upstream == null) {
                return;
            }
            DataBlob dataIn = upstream.node.getData(upstream.port);
            if (dataIn == null) {
                return;
            }

            //Acquire data from source
            if (dataIn.type == DataBlob.Type.Channels) {
                if (dataIn.channels == null)
                    return;
                //copy channels to local arrays
                channels = new byte[dataIn.channels.Length][];
                samples = dataIn.samplingMode;
                width = dataIn.width;
                height = dataIn.height;
                for (int channel = 0; channel < channels.Length; channel++) {
                    channels[channel] = (byte[])dataIn.channels[channel].Clone();
                }
            } else {
                return;
            }

            //process the channels
            //DCT the Y channel
            //for each chunk
            //for v = 0 to size
            //for u = 0 to size
            //System.Collections.IEnumerable iterator
            //int accumulator
            //for y = 0 to size
            //for x = 0 to size
            //pixel = iterator.getNext()
            //accumulator += cos * cos * y[pixel]
            //DCT_y


            //for each chunk
            //  for each bin from the chunk
            //    bin = 0
            //    for each pixel in the chunk
            //      bin += DCTfail
            //    bin *= ratio
            //    bin /= quantization

            /*
            Chunker c = new Chunker(chunkSize, width, height, width, 1);
            int u, v, i, j;
            double bin;
            System.Collections.IEnumerator iterBin, iterImg;
            foreach (System.Collections.IEnumerable iterator in c.getIterators()) {
                iterImg = iterator.GetEnumerator();
                iterBin = iterator.GetEnumerator();
                for (v = 0; v < chunkSize; v++) {
                    for (u = 0; u < chunkSize; u++) {
                        bin = 0;
                        iterImg.Reset();
                        for (j = 0; j < chunkSize; j++) {
                            for (i = 0; i < chunkSize; i++) {
                                iterImg.MoveNext();
                                if ((int)iterImg.Current == -1) {
                                    continue; //off the end, treat as if black. (0)
                                }
                                bin += Math.Cos(((2 * i + 1) * u * Math.PI) / (2 * chunkSize))
                                    * Math.Cos(((2 * i + 1) * v * Math.PI) / (2 * chunkSize))
                                    * channels[0][(int)iterImg.Current];
                            }
                        }
                        bin *= 2 * (u == 0 ? 1 / Math.Sqrt(2) : 1) * (v == 0 ? 1 / Math.Sqrt(2) : 1) / chunkSize;
                        bin /= quantizationY[v, u];
                        iterBin.MoveNext();
                        DCT_y[(int)iterBin.Current] = (byte)bin;
                    }
                }
            }
            */

            Chunker c = new Chunker(chunkSize, width, height, width, 1);
            byte[] data;
            for (int i = 0; i < c.getNumChunks(); i++) {
                data = c.getBlock(channels[0], i);
                data = doDCT(data);
                c.setBlock(channels[0], i, data);
            }

            //with 4:2:0 the width of the Cr/b channel is half that of the Y channel, rounded up
            c = new Chunker(chunkSize, (width+1) / 2, (height+1) / 2, (width+1) / 2, 1);
            for (int i = 0; i < c.getNumChunks(); i++) {
                data = c.getBlock(channels[1], i);
                data = doDCT(data);
                c.setBlock(channels[1], i, data);
                data = c.getBlock(channels[2], i);
                data = doDCT(data);
                c.setBlock(channels[2], i, data);
            }
        }

        private byte[] doDCT(byte[] data) {
            byte[] result = new byte[data.Length];
            double bin;
            for (int v = 0; v < chunkSize; v++) {
                for (int u = 0; u < chunkSize; u++) {
                    bin = 0;
                    for (int j = 0; j < chunkSize; j++) {
                        for (int i = 0; i < chunkSize; i++) {
                            bin += Math.Cos(((2 * i + 1) * u * Math.PI) / (2 * chunkSize))
                                    * Math.Cos(((2 * j + 1) * v * Math.PI) / (2 * chunkSize))
                                    * data[j*chunkSize + i];
                        }
                    }
                    bin *= 2 * (u == 0 ? 1 / Math.Sqrt(2) : 1) * (v == 0 ? 1 / Math.Sqrt(2) : 1) / chunkSize;
                    bin /= quantizationY[v, u];
                    result[v * chunkSize + u] = (byte)bin;
                }
            }
            return result;
        }
    }
}
