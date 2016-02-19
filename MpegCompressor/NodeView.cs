﻿using System;
using System.Windows.Forms;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace MpegCompressor {
    public partial class NodeView : TransformPanel {
        public event EventHandler eSelectionChanged;

        private Node selectedNode;
        private LinkedList<Node> selectedNodes;
        private static Pen linePen = new Pen(Color.Black, 3);
        private static Font nodeFont = new Font("Tahoma", 11.0f);
        private static Font nodeExtraFont = new Font("Tahoma", 11.0f, FontStyle.Italic);
        private static Font nodeTitleFont = new Font("Tahoma", 13.0f, FontStyle.Bold);
        private LinkedList<Node> nodes;
        private Point mdown;
        private bool bDragging;

        public NodeView() {
            InitializeComponent();
            init();
        }

        public NodeView(IContainer container) {
            container.Add(this);

            InitializeComponent();
            init();
        }

        private void init() {
            this.SetStyle(ControlStyles.Selectable, true);
            this.TabStop = true;
            nodes = new LinkedList<Node>();
            mdown = new Point();
            selectedNodes = new LinkedList<Node>();
        }

        public void clearNodes() {
            SuspendLayout();
            Controls.Clear();
            nodes.Clear();
            ResumeLayout();
        }

        public void addNode(Node n) {
            nodes.AddLast(n);
            recalcFocus();
        }

        private void recalcFocus() {
            int left = int.MaxValue, right = int.MinValue, top = int.MaxValue, bottom = int.MinValue;
            foreach (Node d in nodes) {
                if (d.pos.X < left) left = d.pos.X;
                if (d.pos.X > right) right = d.pos.X;
                if (d.pos.Y < top) top = d.pos.Y;
                if (d.pos.Y > bottom) bottom = d.pos.Y;
            }
            setFocusRect(left, top, right - left + 100, bottom - top + 50);
        }

        private void select(Node sel, bool toggle) {
            if (toggle) {
                if (sel != null)
                    selectedNodes.AddLast(sel);
            } else {
                selectedNodes.Clear();
                if (sel != null)
                    selectedNodes.AddLast(sel);
            }
            if (selectedNodes.Count() == 1) {
                selectedNode = sel;
                EventHandler handler = eSelectionChanged;
                if (handler != null) {
                    handler(this, new EventArgs());
                }
            }
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e) {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            foreach (Node n in nodes) {
                foreach (Node.Address a in n.getInputs().Values) {
                    if (a == null) {
                        continue;
                    }
                    drawLink(g, a.node, n);
                }
            }
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.Default;

            foreach (Node n in nodes) {
                drawNode(g, n);
            }
        }

        //box including: title, extra, properties (bubble left and/or right)
        private void drawNode(Graphics g, Node n) {
            int width = Math.Max(100, (int)g.MeasureString(n.getName(), nodeTitleFont).Width);
            int titleHeight = nodeTitleFont.Height;
            int textHeight = nodeFont.Height;
            Rectangle r = new Rectangle(n.pos.X, n.pos.Y, width, titleHeight);

            //count the lines to cover with text
            if (n.getExtra() != null) {
                r.Height += textHeight;
            }
            r.Height += textHeight * n.getProperties().Count;
            
            //draw background
            if (selectedNodes.Contains(n)) {
                g.FillRectangle(Brushes.Wheat, r);
            } else {
                g.FillRectangle(Brushes.CadetBlue, r);
            }
            g.DrawRectangle(Pens.Black, r);

            //draw title
            g.DrawString(n.getName(), nodeTitleFont, Brushes.Black, r.Location);
            g.DrawLine(Pens.Black, r.Left, r.Top + nodeTitleFont.Height, r.Right, r.Top + nodeTitleFont.Height);
            r.Offset(0, nodeTitleFont.Height);


            //draw extra
            if (n.getExtra() != null) {
                g.DrawString(n.getExtra(), nodeExtraFont, Brushes.Black, r.Location);
                g.DrawLine(Pens.Black, r.Left, r.Top + nodeExtraFont.Height, r.Right, r.Top + nodeExtraFont.Height);
                r.Offset(0, nodeFont.Height);
            }

            foreach (var kvp in n.getProperties()) {
                g.DrawString(kvp.Key, nodeFont, Brushes.Black, r.Location);
                r.Offset(0, nodeFont.Height);
            }
        }

        private void drawLink(Graphics g, Node a, Node b) {
            Point p1 = a.pos;
            Point p2 = b.pos;
            p1.Offset(100, 25);
            p2.Offset(0, 25);
            g.DrawLine(linePen, p1, p2);
        }

        public Node getSelection() {
            return selectedNode;
        }

        protected override void OnMouseEnter(EventArgs e) {
            this.Focus();
            base.OnMouseEnter(e);
        }
        
        protected override void OnMouseDown(MouseEventArgs e) {
            Node n;

            mdown = e.Location;

            //if the mouse is over a node, selected it and begin dragging. otherwise do base.
            //  if shift is selected, toggle selection instead of replacing
            if ((n = hitTest(e.X, e.Y)) != null) {
                select(n, Control.ModifierKeys == Keys.Shift);
                bDragging = true;
                ScreenToCanvas(ref mdown);
            } else {
                base.OnMouseDown(e);
            }
        }

        protected override void OnMouseMove(MouseEventArgs e) {
            if (bDragging) {
                //mdown is in canvas coordinates
                Point newPos = e.Location;
                ScreenToCanvas(ref newPos);
                foreach (Node n in selectedNodes) {
                    n.pos.Offset(newPos.X - mdown.X, newPos.Y - mdown.Y);
                }
                mdown = newPos;
                Invalidate();
            } else {
                base.OnMouseMove(e);
            }
        }

        protected override void OnMouseUp(MouseEventArgs e) {
            if (mdown.X == e.X && mdown.Y == e.Y) {
                select(hitTest(e.X, e.Y), Control.ModifierKeys == Keys.Shift);
            }

            if (bDragging) {
                bDragging = false;
                recalcFocus();
                return;
            }
            base.OnMouseUp(e);
        }

        private Node hitTest(int x, int y) {
            //x and y are in screen coordinates where 
            //  (0, 0) is the top left of the panel
            ScreenToCanvas(ref x, ref y);
            Point pos;

            foreach (Node n in nodes) {
                pos = n.pos;
                if (x > pos.X && y > pos.Y && x < (pos.X + 100) && y < (pos.Y + 50)) {
                    return n;
                }
            }
            return null;
        }
    }
}
