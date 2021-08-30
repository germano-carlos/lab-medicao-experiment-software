using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Graphs
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            chart1.Series["s1"].Points.AddXY(0, 3, 8, 4, 7);
            chart1.Series["s1"].Points.AddXY(0, 3, 8, 4, 7);
            chart1.Series["s1"].Points.AddXY(0, 3, 8, 4, 7);
        }
    }
}
