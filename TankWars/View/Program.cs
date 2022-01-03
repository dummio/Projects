using System;
using System.Windows.Forms;

//@authors: Kevin Xue & Griffin Zody
// Fall 2021, CS 3500
namespace TankWars
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1(new GameController()));
        }
    }
}
