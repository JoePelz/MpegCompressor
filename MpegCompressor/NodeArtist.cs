﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MpegCompressor {
    static class NodeArtist {
        public static Font nodeFont = new Font("Tahoma", 11.0f);
        public static Font nodeExtraFont = new Font("Tahoma", 11.0f, FontStyle.Italic);
        public static Font nodeTitleFont = new Font("Tahoma", 13.0f, FontStyle.Bold);
        public static int ballSize = nodeFont.Height / 2;
        public static int ballOffset = (nodeFont.Height - ballSize) / 2;
        
        public static Rectangle getGraphRect(Nodes.Node n) {
            int titleWidth = System.Windows.Forms.TextRenderer.MeasureText(n.getName(), nodeTitleFont).Width;
            Rectangle nodeRect = new Rectangle();
            nodeRect.Location = n.getPos();
            nodeRect.Width = Math.Max(100, titleWidth);
            nodeRect.Height = nodeTitleFont.Height;

            //count the lines to cover with text
            if (n.getExtra() != null) {
                nodeRect.Height += nodeExtraFont.Height;
            }
            foreach (var kvp in n.getProperties()) {
                if (kvp.Value.getType() == Property.Type.NONE) {
                    nodeRect.Height += nodeFont.Height;
                }
            }
            return nodeRect;
        }

        public static Point getJointPos(Nodes.Node n, string port, bool input) {
            Rectangle nodeRect = getGraphRect(n);
            Point result = new Point(nodeRect.X, nodeRect.Y);
            result.Y += nodeTitleFont.Height;
            if (n.getExtra() != null) {
                result.Y += nodeExtraFont.Height;
            }

            foreach (var kvp in n.getProperties()) {
                if (kvp.Key == port) {
                    break;
                }
                if (kvp.Value.getType() == Property.Type.NONE) {
                    result.Y += nodeFont.Height;
                }
            }
            if (!input) {
                result.X += nodeRect.Width;
            }
            result.Y += nodeFont.Height / 2 - ballSize / 2;
            return result;
        }

        public static void drawGraphNode(Graphics g, Nodes.Node n, bool isSelected) {
            Rectangle nodeRect = getGraphRect(n);

            //draw background
            if (isSelected) {
                g.FillRectangle(Brushes.Wheat, nodeRect);
            } else {
                g.FillRectangle(Brushes.CadetBlue, nodeRect);
            }
            g.DrawRectangle(Pens.Black, nodeRect);

            //draw title
            g.DrawString(n.getName(), nodeTitleFont, Brushes.Black, nodeRect.X + (nodeRect.Width - System.Windows.Forms.TextRenderer.MeasureText(n.getName(), nodeTitleFont).Width) / 2, nodeRect.Y);
            nodeRect.Y += nodeFont.Height;
            g.DrawLine(Pens.Black, nodeRect.Left, nodeRect.Y, nodeRect.Right, nodeRect.Y);


            //draw extra
            if (n.getExtra() != null) {
                g.DrawString(n.getExtra(), nodeExtraFont, Brushes.Black, nodeRect.Location);
                nodeRect.Y += nodeFont.Height;
                g.DrawLine(Pens.Black, nodeRect.Left, nodeRect.Y, nodeRect.Right, nodeRect.Y);
            }

            //draw properties
            foreach (var kvp in n.getProperties()) {
                if (kvp.Value.getType() == Property.Type.NONE) {
                    //draw bubbles
                    if (kvp.Value.isInput) {
                        g.DrawString(kvp.Key, nodeFont, Brushes.Black, nodeRect.Left + ballSize / 2, nodeRect.Y);
                        if (kvp.Value.input != null) {
                            g.FillEllipse(Brushes.Black, nodeRect.Left - ballSize / 2, nodeRect.Y + ballOffset, ballSize, ballSize);
                        } else {
                            g.DrawEllipse(Pens.Black, nodeRect.Left - ballSize / 2, nodeRect.Y + ballOffset, ballSize, ballSize);
                        }
                    } else if (kvp.Value.isOutput) {
                        g.DrawString(kvp.Key, nodeFont, Brushes.Black, nodeRect.Left + (nodeRect.Width - g.MeasureString(kvp.Key, nodeFont).Width), nodeRect.Y);
                        if (kvp.Value.output.Any()) {
                            g.FillEllipse(Brushes.Black, nodeRect.Right - ballSize / 2, nodeRect.Y + ballOffset, ballSize, ballSize);
                        } else {
                            g.DrawEllipse(Pens.Black, nodeRect.Right - ballSize / 2, nodeRect.Y + ballOffset, ballSize, ballSize);
                        }
                    } else {
                        g.DrawString(kvp.Key, nodeFont, Brushes.Black, nodeRect.Left + (nodeRect.Width - g.MeasureString(kvp.Key, nodeFont).Width) / 2, nodeRect.Y);
                    }
                    nodeRect.Y += nodeFont.Height;
                }
            }
        }

        public static bool hitTest(int x, int y, Nodes.Node n) {
            Rectangle r = getGraphRect(n);
            return x >= r.Left && x < r.Right && y >= r.Top && y < r.Bottom;
        }
    }
}