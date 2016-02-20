﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;

namespace MpegCompressor {
    public class ReadImage : Node {
        private string filepath;

        public ReadImage() {
            setPath("C:\\temp\\uv.jpg");
            rename("ReadImage");
        }

        protected override void createProperties() {
            base.createProperties();

            //create filepath property
            Property p = new Property(false, false);
            p.createString("", "Image file to open");
            p.eValueChanged += OnPathChange;
            properties.Add("path", p);

            properties.Add("outColor", new Property(false, true));
        }

        public void setPath(string path) {
            filepath = path;
            properties["path"].setString(path);

            int lastSlash = path.LastIndexOf('\\') + 1;
            lastSlash = lastSlash == -1 ? 0 : lastSlash;

            setExtra(path.Substring(lastSlash));
            soil();
        }

        private void OnPathChange(object sender, EventArgs e) {
            setPath(properties["path"].getString());
        }

        protected override void clean() {
            base.clean();
            //load image from path, if it exists
            state = new DataBlob();

            try {
                state.bmp = new Bitmap(filepath);
                state.bmp = state.bmp.Clone(new Rectangle(0, 0, state.bmp.Width, state.bmp.Height), System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                state.imageWidth = state.bmp.Width;
                state.imageHeight = state.bmp.Height;
            } catch (Exception) {
                state = null;
            }
        }
    }
}