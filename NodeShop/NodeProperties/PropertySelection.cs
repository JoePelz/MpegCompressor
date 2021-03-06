﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NodeShop.NodeProperties {
    public class PropertySelection : Property{
        private ComboBox comboBox;

        public PropertySelection(string[] choices, int defChoice, string label) : base(false, false, Type.SELECTION) {
            sLabel = label;
            updateLayout(choices);
            nValue = defChoice;
        }

        public override int nValue
        {
            get { return comboBox.SelectedIndex; }
            set { comboBox.SelectedIndex = value; }
        }

        public override string ToString() {
            return comboBox.SelectedIndex.ToString();
        }

        public override void FromString(string data) {
            nValue = int.Parse(data);
        }

        private void updateLayout(string[] choices) {
            SuspendLayout();

            resetLayout();

            comboBox = new ComboBox();
            foreach (string option in choices) {
                comboBox.Items.Add(option);
            }
            comboBox.SelectedIndex = 0;
            comboBox.SelectedIndexChanged += fireEvent;

            Controls.Add(comboBox, 0, 0);

            ResumeLayout();
        }
    }
}
