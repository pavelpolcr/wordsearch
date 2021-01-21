using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrossroadsCZ
{
    class Program
    {
        public static Random rand = new Random();
        static void Main(string[] args)
        {
            MainWindow f1 = new MainWindow();
            f1.ShowDialog();
        }
    }
}
