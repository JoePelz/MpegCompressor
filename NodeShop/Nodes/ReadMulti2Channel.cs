﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NodeShop.NodeProperties;

namespace NodeShop.Nodes {
    class ReadMulti2Channel : Node {
        private const byte rleToken = 128;
        private string inPath;
        DataBlob C1, C2, V2;

        public ReadMulti2Channel() : base() { }
        public ReadMulti2Channel(NodeView graph) : base(graph) { }
        public ReadMulti2Channel(NodeView graph, int posX, int posY) : base(graph, posX, posY) { }

        protected override void init() {
            base.init();
            rename("Read Multi");
            setExtra("2x Channels");
        }

        protected override void createProperties() {
            base.createProperties();

            Property p = new PropertyString("", "Image path to save");
            p.eValueChanged += pathChanged;
            properties.Add("path", p);

            p = new PropertyButton("Read", "open image from file");
            p.eValueChanged += open;
            properties.Add("save", p);

            properties.Add("outChannels1", new PropertyChannels(false, true));
            properties.Add("outVectors2", new PropertyVectors(false, true));
            properties.Add("outChannels2", new PropertyChannels(false, true));
        }

        public void setPath(string path) {
            inPath = path;
            properties["path"].sValue = path;

            int lastSlash = path.LastIndexOf('\\') + 1;
            lastSlash = lastSlash == -1 ? 0 : lastSlash;

            setExtra(path.Substring(lastSlash));
        }

        private void pathChanged(object sender, EventArgs e) {
            setPath(properties["path"].sValue);
        }

        private void open(object sender, EventArgs e) {
            soil();
            clean();
            Stream stream = null;
            try {
                stream = new FileStream(inPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                using (BinaryReader reader = new BinaryReader(stream, Encoding.Default)) {
                    DataBlob metadata = readHeader(reader);
                    setupBlobs(metadata);
                    readChannels(reader, C1);
                    readChannels(reader, V2);
                    readChannels(reader, C2);
                }
            } catch (FileNotFoundException) {
                //silently fail.
            } finally {
                if (stream != null) {
                    stream.Dispose();
                }
            }
        }

        private void readChannel(BinaryReader reader, byte[] channel, Chunker c) {
            byte[] data = new byte[64];
            byte count, val;
            var indexer = Chunker.zigZag8Index();

            for (int iChunk = 0; iChunk < c.getNumChunks(); iChunk++) {
                for (int iPixel = 0; iPixel < 64;) {
                    val = reader.ReadByte();
                    if (val != rleToken) {
                        data[iPixel++] = val;
                    } else {
                        count = reader.ReadByte();
                        val = reader.ReadByte();
                        while (count > 0) {
                            data[iPixel++] = val;
                            count--;
                        }
                    }
                }
                //set the data into the channel
                c.setZigZag8Block(channel, data, iChunk);
            }
        }

        private void readChannels(BinaryReader reader, DataBlob ch) {
            Chunker c = new Chunker(8, ch.channelWidth, ch.channelHeight, ch.channelWidth, 1);
            readChannel(reader, ch.channels[0], c);
            Size s = Subsample.deduceCbCrSize(ch);
            c = new Chunker(8, s.Width, s.Height, s.Width, 1);
            readChannel(reader, ch.channels[1], c);
            readChannel(reader, ch.channels[2], c);
        }

        private void setupBlobs(DataBlob metadata) {
            C1 = new DataBlob();
            C2 = new DataBlob();
            V2 = new DataBlob();
            C1.type = C2.type = DataBlob.Type.Channels;
            V2.type = DataBlob.Type.Vectors;

            //import metadata onto channels
            C1.imageWidth = C2.imageWidth = metadata.imageWidth;
            C1.imageHeight = C2.imageHeight = metadata.imageHeight;
            C1.channelWidth = C2.channelWidth = metadata.channelWidth;
            C1.channelHeight = C2.channelHeight = metadata.channelHeight;
            C1.quantizeQuality = C2.quantizeQuality = metadata.quantizeQuality;
            C1.samplingMode = C2.samplingMode = metadata.samplingMode;

            Chunker c = new Chunker(8, metadata.channelWidth, metadata.channelHeight, metadata.channelWidth, 1);
            V2.imageWidth = metadata.imageWidth;
            V2.imageHeight = metadata.imageHeight;
            V2.channelWidth = c.getChunksWide();
            V2.channelHeight = c.getChunksHigh();
            V2.quantizeQuality = metadata.quantizeQuality;
            V2.samplingMode = metadata.samplingMode;

            //Allocate space for incoming data
            C1.channels = new byte[3][];
            C2.channels = new byte[3][];
            V2.channels = new byte[3][];

            int cMajor = C1.channelWidth * C1.channelHeight;
            Size sizeMinor = Subsample.getPaddedCbCrSize(new Size(C1.channelWidth, C1.channelHeight), C1.samplingMode);
            int cMinor = sizeMinor.Width * sizeMinor.Height;
            C1.channels[0] = new byte[cMajor];
            C2.channels[0] = new byte[cMajor];
            C1.channels[1] = new byte[cMinor];
            C2.channels[1] = new byte[cMinor];
            C1.channels[2] = new byte[cMinor];
            C2.channels[2] = new byte[cMinor];
            cMajor = V2.channelWidth * V2.channelHeight;
            sizeMinor = Subsample.getCbCrSize(new Size(V2.channelWidth, V2.channelHeight), V2.samplingMode);
            cMinor = sizeMinor.Width * sizeMinor.Height;
            V2.channels[0] = new byte[cMajor];
            V2.channels[1] = new byte[cMinor];
            V2.channels[2] = new byte[cMinor];
        }

        private DataBlob readHeader(BinaryReader reader) {
            DataBlob metadata = new DataBlob();
            metadata.imageWidth = reader.ReadUInt16();
            metadata.imageHeight = reader.ReadUInt16();
            metadata.channelWidth = reader.ReadUInt16();
            metadata.channelHeight = reader.ReadUInt16();
            metadata.quantizeQuality = reader.ReadByte();
            metadata.samplingMode = (DataBlob.Samples)reader.ReadByte();
            return metadata;
        }

        public override DataBlob getData(string port) {
            switch (port) {
                case "outChannels1":
                    return C1;
                case "outVectors2":
                    return V2;
                case "outChannels2":
                    return C2;
                default:
                    return null;
            }
        }
    }
}
