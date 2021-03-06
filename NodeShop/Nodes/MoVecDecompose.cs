﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NodeShop.Nodes;
using NodeShop.NodeProperties;

namespace NodeShop.Nodes {
    public class MoVecDecompose : Node {
        private int[] testData = new int[256];
        private const int chunkSize = 8;
        private DataBlob vState;

        public MoVecDecompose(): base() { }
        public MoVecDecompose(NodeView graph) : base(graph) { }
        public MoVecDecompose(NodeView graph, int posX, int posY) : base(graph, posX, posY) { }


        protected override void init() {
            base.init();
            rename("Motion Vectors");
        }

        protected override void createProperties() {
            base.createProperties();

            Property p = new PropertyInt(7, 0, 7, "Vector search radius");
            p.eValueChanged += (sender, e) => { soil(); };
            properties.Add("radius", p);

            properties.Add("inChannelsPast", new PropertyChannels(true, false));
            properties.Add("inChannelsNow",  new PropertyChannels(true, false));
            properties.Add("outVectors",    new PropertyVectors(false, true));
            properties.Add("outChannels",   new PropertyChannels(false, true));
        }

        protected override void clean() {
            base.clean();

            state = null;
            vState = null;

            Address upstreamNow = properties["inChannelsNow"].input;
            if (upstreamNow == null) {
                return;
            }
            Address upstreamPast = properties["inChannelsPast"].input;
            if (upstreamPast == null) {
                return;
            }

            DataBlob stateNow = upstreamNow.node.getData(upstreamNow.port);
            if (stateNow == null) {
                return;
            }
            DataBlob statePast = upstreamPast.node.getData(upstreamPast.port);
            if (statePast == null) {
                return;
            }

            if (stateNow.type != DataBlob.Type.Channels || stateNow.channels == null) {
                return;
            }
            if (statePast.type != DataBlob.Type.Channels || statePast.channels == null) {
                return;
            }

            //check resolutions match
            if (stateNow.channels[0].Length != statePast.channels[0].Length
                || stateNow.channels[1].Length != statePast.channels[1].Length
                || stateNow.channels[2].Length != statePast.channels[2].Length) {
                return;
            }

            state = stateNow.clone();
            vState = stateNow.clone();

            //create copy of channels for local use.
            byte[][] diffChannels = new byte[state.channels.Length][];
            diffChannels[0] = new byte[state.channels[0].Length];
            diffChannels[1] = new byte[state.channels[1].Length];
            diffChannels[2] = new byte[state.channels[2].Length];
            byte[][] vectors = new byte[3][];
            state.channels = diffChannels;
            vState.channels = vectors;
            state.bmp = null;
            vState.bmp = null;
            state.type = DataBlob.Type.Channels;
            vState.type = DataBlob.Type.Vectors;

            calcMoVec(statePast.channels, stateNow.channels);
        }

        private void calcMoVec(byte[][] chOld, byte[][] chNew) {
            //for each channel
            //chunk state.channels into 8x8 blocks
            //compare each block with blocks surrounding them in the arg channels 
            //over x = [-7,7] (range 15 values)
            //and  y = [-7,7] (range 15 values)


            //Do the first channel
            Chunker c = new Chunker(chunkSize, state.channelWidth, state.channelHeight, state.channelWidth, 1);
            int pixelTL;
            byte offset;
            //need to set vState.channelWidth and vState.channelHeight correctly, I think....
            vState.channels[0] = new byte[c.getNumChunks()];
            vState.channelWidth = c.getChunksWide();
            vState.channelHeight = c.getChunksHigh();
            
            for (int i = 0; i < c.getNumChunks(); i++) {
                pixelTL = c.chunkIndexToPixelIndex(i);
                //find best match given search area
                offset = findOffsetVector(chNew[0], chOld[0], pixelTL, state.channelWidth);
                //save best match vector
                vState.channels[0][i] = offset;
                //update channels to be difference.
                if ( i == 20 ) {
                    i = 20;
                }
                setDiff(state.channels[0], chNew[0], chOld[0], pixelTL, offset, state.channelWidth);
            }

            //Do the second two channels
            Size smaller = Subsample.deduceCbCrSize(state);
            c = new Chunker(chunkSize, smaller.Width, smaller.Height, smaller.Width, 1);
            vState.channels[1] = new byte[c.getNumChunks()];
            vState.channels[2] = new byte[c.getNumChunks()];
            for (int i = 0; i < c.getNumChunks(); i++) {
                pixelTL = c.chunkIndexToPixelIndex(i);
                offset = findOffsetVector(chNew[1], chOld[1], pixelTL, smaller.Width);
                vState.channels[1][i] = offset;
                setDiff(state.channels[1], chNew[1], chOld[1], pixelTL, offset, smaller.Width);
                //offset = findOffsetVector(state.channels[2], channels[2], pixelTL, state.channelWidth);
                //Just use the same vectors for channel 3 as channel 2. Probably okay.
                vState.channels[2][i] = offset;
                setDiff(state.channels[2], chNew[2], chOld[2], pixelTL, offset, smaller.Width);
            }
        }

        private void setDiff(byte[] dest, byte[] goal, byte[] searchArea, int indexTopLeft, byte offset, int stride) {
            int offsetX = ((offset & 0xf0) >> 4) - 7;
            int offsetY = (offset & 0x0f) - 7;
            int x0 = indexTopLeft % stride;
            int y0 = indexTopLeft / stride;
            int xref, yref;
            int yMax = dest.Length / stride;
            int xMax = stride;
            int targetPixel, refPixel;
            int temp;
            for (int y = y0; y < y0 + 8; y++) {
                if (y >= yMax) break;
                yref = y + offsetY;
                for (int x = x0; x < x0 + 8; x++) {
                    if (x >= xMax) break;
                    xref = x + offsetX;
                    targetPixel = y * stride + x;
                    refPixel = yref * stride + xref;
                    if (xref < 0 || xref >= xMax || yref < 0 || yref >= yMax) {
                        temp = (goal[targetPixel] + 127);
                    } else {
                        temp = (goal[targetPixel] - searchArea[refPixel] + 127);
                    }
                    dest[targetPixel] = (byte)(temp > 255 ? 255 : (temp < 0 ? 0 : temp));
                    //dest[targetPixel] = (byte)temp;
                    testData[dest[targetPixel]] += 1;
                }
            }
        }
        
        private byte findOffsetVector(byte[] goal, byte[] searchArea, int indexTopLeft, int stride) {
            int pixel = 0;
            int diff;
            int minDiff = int.MaxValue;
            int offX = 0;
            int offY = 0;
            //Calibrate to center first
            diff = SAD(goal, indexTopLeft, searchArea, indexTopLeft, stride);
            minDiff = diff;
            //Test radius
            int radius = 7;
            radius = properties["radius"].nValue;

            for (int yo = -radius; yo <= radius; yo++) {
                for (int xo = -radius; xo <= radius; xo++) {
                    pixel = indexTopLeft + yo * stride + xo;
                    diff = SAD(goal, indexTopLeft, searchArea, pixel, stride);
                    if (diff < minDiff) {
                        minDiff = diff;
                        offX = xo;
                        offY = yo;
                    }
                }
            }
            offX += 7; //-7..7 => 0..14 (15 values)
            offY += 7;
            return (byte)((offX << 4) | offY);

        }

        private int SAD(byte[] a, int startA, byte[] b, int startB, int stride) {
            int sad = 0, ia, ib, xLimit;
            for (int y = 0; y < 8; y++) {
                xLimit = startA / stride * stride + (y + 1) * stride;
                if (startA / stride + y >= a.Length / stride) {
                    break;
                }
                for (int x = 0; x < 8; x++) {
                    ia = y * stride + x;
                    ib = ia + startB;
                    ia = ia + startA;
                    if (ia < 0 || ia >= xLimit) {
                        continue;
                    } else if (ib < 0 || ib >= b.Length) {
                        sad += a[ia];
                    } else {
                        sad += a[ia] > b[ib] ? a[ia] - b[ib] : b[ib] - a[ia];
                    }
                }
            }
            return sad;
        }
        
        public override void viewExtra(Graphics g) {
            //base.viewExtra(g);
            if (state == null) {
                return;
            }
            Chunker c = new Chunker(8, state.channelWidth, state.channelHeight, state.channelWidth, 1);
            int offsetX, offsetY;
            int y = state.channelHeight - 4;
            int x = 4;

            for (int i = 0; i < vState.channels[0].Length; i++) {
                offsetX = ((vState.channels[0][i] & 0xF0) >> 4) - 7;
                offsetY = (vState.channels[0][i] & 0x0F) - 7;
                if (offsetX == 0 && offsetY == 0) {
                    g.FillRectangle(Brushes.BlanchedAlmond, x-1, y-1, 2, 2);
                } else {
                    g.DrawLine(Pens.BlanchedAlmond, x, y, x + offsetX, y - offsetY);
                }
                x += 8;
                if (x - 4 >= state.channelWidth) {
                    x = 4;
                    y -= 8;
                }
            }
        }

        public override DataBlob getData(string port) {
            base.getData(port);
            if (port == "outChannels") {
                return state;
            } else if (port == "outVectors") {
                return vState;
            }
            return state;
        }
    }
}
