using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace NullSoftware.ToolKit.CustomRender
{
    internal class ModernColorTable : ProfessionalColorTable
    {
        public override Color MenuItemBorder
        {
            get { return Color.WhiteSmoke; }
        }
        public override Color MenuItemSelected
        {
            get { return Color.FromArgb(145, 201, 247); }
        }
        public override Color ToolStripDropDownBackground
        {
            get { return SystemColors.MenuBar; }
        }
        public override Color ImageMarginGradientBegin
        {
            get { return SystemColors.MenuBar; }
        }
        public override Color ImageMarginGradientMiddle
        {
            get { return SystemColors.MenuBar; }
        }
        public override Color ImageMarginGradientEnd
        {
            get { return SystemColors.MenuBar; }
        }

        public override Color SeparatorLight => Color.LightGray;

        public override Color SeparatorDark => Color.LightGray;
    }
}
