using System;
using System.Collections.Generic;
using System.Text;

namespace NullSoftware.ToolKit
{
    /// <summary>
    /// Context menu generation mode.
    /// </summary>
    public enum ContextMenuVariation : byte
    {
        /// <summary>
        /// Use <see cref="System.Windows.Forms.ContextMenu"/>.
        /// </summary>
        ContextMenu,

        /// <summary>
        /// Use <see cref="System.Windows.Forms.ContextMenuStrip"/>.
        /// </summary>
        ContextMenuStrip
    }
}
