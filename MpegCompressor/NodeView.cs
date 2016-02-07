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
        private Pen linePen;
        private Font nodeFont;
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
            nodeFont = new Font("Tahoma", 11.0f);
            linePen = new Pen(Color.Black, 3);
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

        private void select(Node sel) {
            if (Control.ModifierKeys == Keys.Shift) {
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
            Rectangle r = new Rectangle();
            r.Width = 100;
            r.Height = 50;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            foreach (Node n in nodes) {
                foreach (Node.Address a in n.getInputs().Values) {
                    drawLink(g, a.node, n);
                }
            }
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.Default;

            foreach (Node n in nodes) {
                r.Location = n.pos;
                if (selectedNodes.Contains(n)) {
                    g.FillRectangle(Brushes.Wheat, r);
                } else {
                    g.FillRectangle(Brushes.CadetBlue, r);
                }
                g.DrawRectangle(Pens.Black, r);

                g.DrawString(n.getName(), nodeFont, Brushes.Black, r.Location);
                r.Offset(0, nodeFont.Height);
                g.DrawString(n.getExtra(), nodeFont, Brushes.Black, r.Location);
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

        private void onNodeClicked(object sender, MouseEventArgs e) {
            select(sender as Node);
        }

        protected override void OnMouseEnter(EventArgs e) {
            this.Focus();
            base.OnMouseEnter(e);
        }
        
        protected override void OnMouseDown(MouseEventArgs e) {
            Node n;
            //if (Control.ModifierKeys == Keys.Alt) {
            if (e.Button == MouseButtons.Middle) {
                bDragging = true;
                if ((n = hitTest(e.X, e.Y)) != null && !selectedNodes.Contains(n)) {
                    select(n);
                }
                mdown = e.Location;
                ScreenToCanvas(ref mdown);
            } else {
                base.OnMouseDown(e);
                mdown = e.Location;
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
            if (bDragging) {
                bDragging = false;
                recalcFocus();
                return;
            }
            base.OnMouseUp(e);
            if (mdown.X - e.X == 0 && mdown.Y - e.Y == 0) {
                select(hitTest(e.X, e.Y));
            }
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
