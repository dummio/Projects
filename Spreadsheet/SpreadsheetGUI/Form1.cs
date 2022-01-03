using System;
using SS;
using SpreadsheetUtilities;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Text.RegularExpressions;

namespace SpreadsheetGUI
{
    /// <summary>
    /// Spreadsheet GUI representation
    /// @author: Kevin Xue
    /// </summary>
    public partial class Form1 : Form
    {
        private Spreadsheet sheet;
        private bool dark = false;

        /// <summary>
        /// Constructor for the Spreadsheet GUI
        /// </summary>
        public Form1()
        {
            InitializeComponent();
            sheet = new Spreadsheet(s => Regex.IsMatch(s, @"[A-Z][1-9][0-9]?"), s => s.ToUpper(), "PS6");
            spreadsheetPanel1.SelectionChanged += displaySelection;
            spreadsheetPanel1.SetSelection(0, 0);
            int row, col;
            spreadsheetPanel1.GetSelection(out col, out row);
            CellName.Text = "" + Convert.ToChar(65 + col) + (1 + row); // printe cell name into cell name box
        }

        /// <summary>
        /// Selection changed delegate 
        /// if cell value is not empty, prints the value/content to the cell Value and Content boxes
        /// </summary>
        /// <param name="ss"></param>
        private void displaySelection(SpreadsheetPanel ss)
        {
            int row, col;
            String value;
            ss.GetSelection(out col, out row);
            ss.GetValue(col, row, out value);
            string name = "" + Convert.ToChar(65 + col) + (1 + row);
            CellName.Text = name;
            if (sheet.GetCellValue(name).ToString() != "")
            {
                if(sheet.GetCellContents(name) is Formula || sheet.GetCellValue(name) is FormulaError)
                {
                    if (sheet.GetCellValue(name) is FormulaError)
                        CellValue.Text = "Formula Error";
                    if (sheet.GetCellContents(name) is Formula)
                        CellContent.Text = "=" + sheet.GetCellContents(name).ToString();
                }
                else
                    CellContent.Text = sheet.GetCellContents(name).ToString();
            }
            else
            {
                CellContent.Text = "";
            }
            //string st;
            //ss.GetValue(col, row, out st);
        }

        /// <summary>
        /// Helper method to refill the grid with the values of all cells
        /// if exception is detected, a message box describing the issue is displayed
        /// used in cellContent_keyDown
        /// </summary>
        /// <param name="name"></param>
        private void ViewHelper(string name)
        {
            int row, col;
            spreadsheetPanel1.GetSelection(out col, out row);
            try
            {
                List<string> names = new List<string>(sheet.SetContentsOfCell(CellName.Text, CellContent.Text));
                if (sheet.GetCellValue(CellName.Text) is FormulaError)
                {
                    spreadsheetPanel1.SetValue(col, row, "Formula Error");
                    CellValue.Text = "FormulaError";
                }
                else { 
                    spreadsheetPanel1.SetValue(col, row, sheet.GetCellValue(CellName.Text).ToString());
                    CellValue.Text = sheet.GetCellValue(CellName.Text).ToString();
                }
                foreach (string s in names)
                {
                    if(sheet.GetCellValue(s) is FormulaError)
                        spreadsheetPanel1.SetValue(s[0] - 65, int.Parse(s.Substring(1)) - 1, "Formula Error");
                    else 
                        spreadsheetPanel1.SetValue(s[0] - 65, int.Parse(s.Substring(1)) - 1, sheet.GetCellValue(s).ToString());
                }
            }
            catch (Exception ex)
            {
                if (ex is CircularException)
                    MessageBox.Show("Circular Exception Detected");
                else if (ex is InvalidNameException)
                    MessageBox.Show("Invalid Name");
                else if (ex is FormulaFormatException)
                    MessageBox.Show("Formula Format Exception");
                else
                    MessageBox.Show(ex.Message);
            }
        }

        /// <summary>
        /// driver method that detects when an enter key is pressed (for the cell content box)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CellContent_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyValue == 13)
            {
                int row, col;
                spreadsheetPanel1.GetSelection(out col, out row);
                ViewHelper(CellName.Text);
            }
        }

        /// <summary>
        /// opens a .sprd file and fills the current spreadsheet with the values in the saved spreadsheet
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void openToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            OpenFileDialog open = new OpenFileDialog();
            open.Filter = "sprd files (*.sprd)|*.sprd|All files (*.*)|*.*";
            if(open.ShowDialog() == DialogResult.OK)
            {
                sheet = new Spreadsheet(open.FileName, s => Regex.IsMatch(s, @"[A-Z][1-9][0-9]?"), s => s.ToUpper(), "PS6");
                foreach(string s in sheet.GetNamesOfAllNonemptyCells())
                {
                    spreadsheetPanel1.SetValue(s[0] - 65, int.Parse(s.Substring(1)) - 1, sheet.GetCellValue(s).ToString());
                }
                MessageBox.Show("File successfully opened");
            }
            else
            {
                MessageBox.Show("No File Selected");
            }
        }

        /// <summary>
        /// opens a new instance of the spreadsheet
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void newToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Tell the application context to run the form on the same
            // thread as the other forms.
            DemoApplicationContext.getAppContext().RunForm(new Form1());
        }

        /// <summary>
        /// saves the current spreadsheet to the computer's local files as a .sprd file
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog save = new SaveFileDialog();
            save.Filter = "sprd files (*.sprd)|*.sprd|All files (*.*)|*.*";
            if (save.ShowDialog() == DialogResult.OK)
            {
                sheet.Save(save.FileName);
                MessageBox.Show("File successfully saved");
            }
        }

        /// <summary>
        /// overrides the 'x' button that closes the spreadsheet
        /// if changes were made to the spreadsheet, prompts the user to save the spreadsheet
        /// </summary>
        /// <param name="e"></param>
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            if (sheet.Changed)
            {
               DialogResult result = MessageBox.Show("Changes occurred to spreadsheet", "Would you like to save?", MessageBoxButtons.YesNo);
                if(result == DialogResult.Yes)
                {
                    saveToolStripMenuItem.PerformClick();
                }
                else
                {
                    MessageBox.Show("Spreadsheet closed without saving");
                }
            }
            return;
        }

        /// <summary>
        /// closes the spreadsheet similarily to the 'x' button
        /// prompts the user to save the spreadsheet if changes were made to the current spreadsheet
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void closeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (sheet.Changed)
            {
                DialogResult result = MessageBox.Show("Changes occurred to spreadsheet", "Would you like to save?", MessageBoxButtons.YesNo);
                if (result == DialogResult.Yes)
                {
                    saveToolStripMenuItem.PerformClick();
                }
                else
                {
                    MessageBox.Show("Spreadsheet closed without saving");
                }
            }
            Close();
        }

        /// <summary>
        /// darkmode changes the color of the spreadsheet
        /// when darkmode is enabled, changes the button name to "lightmode"
        /// lightmode (default) makes the background white
        /// changes the lightmode button to "darkmode"
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void darkModeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if(!dark)
            {
                this.BackColor = Color.DarkGray;
                spreadsheetPanel1.BackColor = Color.DarkGray;
                menuStrip1.BackColor = Color.DarkGray;
                dark = true;
                darkModeToolStripMenuItem.Text = "Light Mode";
            }
            else
            {
                this.BackColor = Color.White;
                spreadsheetPanel1.BackColor = Color.White;
                menuStrip1.BackColor = Color.White;
                dark = false;
                darkModeToolStripMenuItem.Text = "Dark Mode";
            }
        }

        /// <summary>
        /// Help menu for the TA's to use to learn the spreadsheet's functions
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void helpToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("To select a cell, click on the cell \n" + 
                "To change the content of a cell, simply enter a string into the cell content box at the top left \n" + 
                "The menu tab contains Open, New, Close, Save, Help, Darkmode/Lightmode \n" + 
                "Open : opens an existing spreadsheet \n" + "Close : closes the spreadsheet \n" + 
                "Save : saves the current spreadsheet as a .sprd file \n" +
                "Help : opens the help menu (You're here now!) \n" +
                "Darkmode/Lightmode : turns on dark/light mode for the spreadsheet (additional feature)");
        }
    }
}
