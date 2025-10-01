using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace VL.CoreLib.Windows
{
    public static class DragDropExtensions
    {
        public static DragDropEffects StartDragString(this Control control, string payload, 
            DragDropEffects allowedEffects = DragDropEffects.Copy | DragDropEffects.Move)
        {
            var dataObject = new DataObject();
            dataObject.SetData(DataFormats.Text, payload);
            dataObject.SetData(DataFormats.UnicodeText, payload);
            dataObject.SetData(DataFormats.StringFormat, payload);
            
            return control.DoDragDrop(dataObject, allowedEffects);
        }
    }
}