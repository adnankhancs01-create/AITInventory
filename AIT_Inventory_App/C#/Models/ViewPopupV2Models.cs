using System;
using System.Collections.Generic;
using System.Text;

namespace AIT_Inventory_App.C_.Models
{
    public class ViewSection
    {
        public string Title { get; set; } = "";
        public List<ViewField> Fields { get; set; } = new();
    }

    public class ViewField
    {
        public string Label { get; set; } = "";
        public string Value { get; set; } = "";
    }
}
