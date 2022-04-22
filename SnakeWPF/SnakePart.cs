using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SnakeWPF
{
    class SnakePart
    {
        public UIElement UIElement { get; set; }
        public Point Position { get; set; }
        public bool IsHead { get; set; }



    }
}
