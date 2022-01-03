using SpreadsheetUtilities;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace SS
{
    /// <summary>
    /// Class representation of a spreadsheet
    /// Author : Kevin Xue
    /// </summary>
    public class Spreadsheet : AbstractSpreadsheet
    {
        //keeps track of the cells
        private Dictionary<string, Cell> cells;
        //keeps track fo dependencies (for recalculation)
        private DependencyGraph dg;

        //calls the 3 parameter contructor
        public Spreadsheet() : this(s => true, s => s, "default") { }

        //Main constructor
        public Spreadsheet(Func<string, bool> isValid, Func<string, string> normalize, string version) : base(isValid, normalize, version)
        {
            cells = new Dictionary<string, Cell>();
            dg = new DependencyGraph();
            Version = version;
        }

        /// <summary>
        /// Calls the 3 parameter constructor but fills the cells/dg with xml file values
        /// </summary>
        /// <param name="filepath"></param> xml filepath
        /// <param name="isValid"></param> func to see if variable is valid
        /// <param name="normalize"></param> func to normalize all variables
        /// <param name="version"></param> string version info
        public Spreadsheet(string filepath, Func<string, bool> isValid, Func<string, string> normalize, string version) : this(isValid, normalize, version)
        {
            cells = new Dictionary<string, Cell>();
            dg = new DependencyGraph();
            string temp = GetSavedVersion(filepath);
            if (temp != version)
                throw new SpreadsheetReadWriteException("versions do not match");
            OpenFile(filepath);
        }

        /// <summary>
        /// Private helper method to open xml files
        /// </summary>
        /// <param name="filepath"></param> string filepath containing xml file
        private void OpenFile(string filepath)
        {
            try
            {
                string name = "";
                string content = "";
                XmlReaderSettings settings = new XmlReaderSettings();
                settings.IgnoreWhitespace = true;
                using (XmlReader reader = XmlReader.Create(filepath, settings))
                {
                    while (reader.Read())
                    {
                        if (reader.IsStartElement())
                        {
                            switch (reader.Name)
                            {
                                case "name":
                                    reader.Read();
                                    name = reader.Value;
                                    break;
                                case "contents":
                                    reader.Read();
                                    content = reader.Value;
                                    //If name value pair does not exist throw exception
                                    if (name == "" || content == "")
                                        throw new Exception();
                                    SetContentsOfCell(name, content);
                                    name = "";
                                    content = "";
                                    break;
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                throw new SpreadsheetReadWriteException("Issue opening filepath");
            }
        }

        //keeps track of whether spreadsheet was changed "safely"
        public override bool Changed { get; protected set; }

        /// <summary>
        /// Gets the content of a cell
        /// </summary>
        /// <param name="name"></param> name of the cell
        /// <returns></returns> content of the cell if cell exists, otherwise empty string
        public override object GetCellContents(string name)
        {
            if (name == null || !ValidName(name))
                throw new InvalidNameException();
            if (cells.ContainsKey(Normalize(name)))
                return cells[Normalize(name)].GetContent();
            return "";
        }

        /// <summary>
        /// returns the value for each cell, can only be string or double
        /// </summary>
        /// <param name="name"></param> cell name
        /// <returns></returns> object representation of cell value
        public override object GetCellValue(string name)
        {
            if (name is null || !ValidName(name))
                throw new InvalidNameException();
            object val = "";
            if (cells.ContainsKey(Normalize(name)))
                val = cells[Normalize(name)].getValue();
            return val;
        }

        /// <summary>
        /// Enumerates the names of all the non-empty cells in the spreadsheet.
        /// </summary>
        public override IEnumerable<string> GetNamesOfAllNonemptyCells()
        {
            return new List<string>(cells.Keys);
        }

        /// <summary>
        /// gets the version information of the saved spreadsheet (used to compare to current spreadsheet)
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public override string GetSavedVersion(string filename)
        {
            try
            {
                using (XmlReader reader = XmlReader.Create(filename))
                {
                    while (reader.Read())
                    {
                        if (reader.Name == "spreadsheet")
                        {
                            return reader["version"];
                        }
                    }
                }
            }
            catch (Exception)
            {
                throw new SpreadsheetReadWriteException("Error occurred when reading file version");
            }
            return "";
        }

        /// <summary>
        /// Saves the current spreadsheet to an xml file
        /// </summary>
        /// <param name="filename"></param> filename spreadsheet will be saved under
        public override void Save(string filename)
        {
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;
            settings.IndentChars = "  ";

            try
            {
                using (XmlWriter writer = XmlWriter.Create(filename, settings))
                {
                    writer.WriteStartDocument();
                    writer.WriteStartElement("spreadsheet");
                    writer.WriteAttributeString("version", Version);

                    foreach (string s in GetNamesOfAllNonemptyCells())
                    {
                        writer.WriteStartElement("cell");
                        writer.WriteElementString("name", s);
                        if (cells[s].GetContent() is Formula)
                            writer.WriteElementString("contents", "=" + cells[s].GetContent().ToString());
                        else
                            writer.WriteElementString("contents", cells[s].GetContent().ToString());
                        writer.WriteEndElement();
                    }
                    writer.WriteEndElement();
                    writer.WriteEndDocument();
                }
            }
            catch (Exception)
            {
                throw new SpreadsheetReadWriteException("Error occurred when saving file");
            }
            Changed = false;
        }

        /// <summary>
        /// Sets the content of a cell to a double, throws invalid name exception if error with name
        /// if cell is contained, replaces dependees and changes content of cell
        /// if cell is not contained, creates a new cell and adds to dictionary
        /// </summary>
        /// <param name="name"></param> cell name string
        /// <param name="number"></param> cell content double
        /// <returns></returns> list of cell names that depend on this cell
        protected override IList<string> SetCellContents(string name, double number)
        {
            if (name == null || !ValidName(name))
                throw new InvalidNameException();
            if (cells.ContainsKey(name))
            {
                dg.ReplaceDependees(name, new HashSet<string>());
                cells[name].ChangeContent(number);
            }
            else
            {
                cells.Add(name, new Cell(number));
            }
            Recalculate(name);
            Changed = true;
            return new List<string>(GetCellsToRecalculate(name));
        }

        /// <summary>
        /// Sets the content of a cell to a string, throws invalid name exception if error with name
        /// throws ArgumentNullException if null string
        /// if cell is contained, replaces dependees and changes content of cell
        /// if cell is not contained, creates a new cell and adds to dictionary
        /// </summary>
        /// <param name="name"></param> cell name string
        /// <param name="number"></param> cell content string
        /// <returns></returns> list of cell names that depend on this cell
        protected override IList<string> SetCellContents(string name, string text)
        {
            if (text == "")
                return new List<string>();
            if (text == null)
                throw new ArgumentNullException();
            if (name == null || !ValidName(name))
                throw new InvalidNameException();
            if (cells.ContainsKey(name))
            {
                dg.ReplaceDependees(name, new HashSet<string>());
                cells[name].ChangeContent(text);
            }
            else
                cells.Add(name, new Cell(text));
            Recalculate(name);
            Changed = true;
            return new List<string>(GetCellsToRecalculate(name));
        }

        /// <summary>
        /// Sets the content of a cell to a string, throws invalid name exception if error with name
        /// throws ArgumentNullException if null formula
        /// if cell is contained, replaces dependees and changes content of cell
        /// if cell is not contained, creates a new cell and adds to dictionary
        /// Then checks if circular exception is thrown, if thrown resets the value, if cell does not have previous value it is removed
        /// </summary>
        /// <param name="name"></param> cell name string
        /// <param name="number"></param> cell content formula
        /// <returns></returns> list of cell names that depend on this cell
        protected override IList<string> SetCellContents(string name, Formula formula)
        {
            if (formula == null)
                throw new ArgumentNullException();
            if (name == null || !ValidName(name))
                throw new InvalidNameException();
            Object last = null;
            if (cells.ContainsKey(name))
            {
                last = cells[name].GetContent();
                cells[name].ChangeContent(formula);
            }
            else
                cells.Add(name, new Cell(formula));
            dg.ReplaceDependees(name, new HashSet<string>(formula.GetVariables()));
            try
            {
                GetCellsToRecalculate(name);
            }
            catch (CircularException)
            {
                if (last == null)
                    cells.Remove(name);
                else
                    cells[name].ChangeContent(last);
            }
            Recalculate(name);
            Changed = true;
            return new List<string>(GetCellsToRecalculate(name));
        }

        //helper method to recalculate values of all cells that depend on this one (including current cell)
        private void Recalculate(string name)
        {
            foreach (string s in GetCellsToRecalculate(name))
            {
                if (cells[s].GetContent() is Formula)
                {
                    Formula f = (Formula)cells[s].GetContent();
                    cells[s].setValue(f.Evaluate(lookup));
                }
                else
                {
                    cells[s].setValue(cells[s].GetContent());
                }
            }
        }

        /// <summary>
        /// driver method for the other cell content setters
        /// </summary>
        /// <param name="name"></param> cell name
        /// <param name="content"></param> string representation of cell content
        /// <returns></returns>
        public override IList<string> SetContentsOfCell(string name, string content)
        {
            if (content is null)
                throw new ArgumentNullException();
            if (!ValidName(name) || name is null)
                throw new InvalidNameException();
            double temp = 0;
            if (content.Length < 1)
                return SetCellContents(Normalize(name), content);
            if (Double.TryParse(content, out temp))
                return SetCellContents(Normalize(name), temp);
            if (content[0] == '=')
            {
                Formula f = new Formula(content.Substring(1), Normalize, IsValid);
                return SetCellContents(Normalize(name), f);
            }
            else
                return SetCellContents(Normalize(name), content);
        }

        /// <summary>
        /// lookup function that is passed when using evaluate
        /// returns value if is of type double, else throws exception
        /// </summary>
        /// <param name="name"></param> name of cell that needs to be looked ip
        /// <returns></returns> double representation of cell value
        private double lookup(string name)
        {
            if (!cells.ContainsKey(Normalize(name)))
                throw new ArgumentException("Variable value not found");
            if (cells[Normalize(name)].getValue() is Double)
                return (double)cells[Normalize(name)].getValue();
            else
                throw new ArgumentException("Variable value not of correct format");
        }


        /// <summary>
        /// Gets the direct dependents a cell
        /// </summary>
        /// <param name="name"></param> string cell name
        /// <returns></returns> list of cells that are related to this one
        protected override IEnumerable<string> GetDirectDependents(string name)
        {
            return dg.GetDependents(name);
        }

        /// <summary>
        /// checks if string is valid variable
        /// </summary>
        /// <param name="s"></param> string s to be checked
        /// <returns></returns> true if variable, else false
        private bool ValidName(string s)
        {
            if (!IsValid(Normalize(s)))
                return false;
            if (s.Length < 1)
                return false;
            if (!Char.IsLetter(s[0]) && s[0] != '_')
                return false;
            if (!Char.IsDigit(s[s.Length - 1]) && !Char.IsLetter(s[s.Length - 1]) && s[s.Length - 1] != '_')
                return false;
            return true;
        }
    }

    /// <summary>
    /// Cell class representation
    /// keeps track of the content of a cell
    /// Author: Kevin Xue
    /// </summary>
    class Cell
    {
        //keeps track of the cell content
        private object content;
        //keeps track of the cell value
        private object value;

        //constructor for cells
        public Cell(object content_)
        {
            content = content_;
        }

        //sets the value of a cell (used in recalculate)
        public void setValue(object val)
        {
            value = val;
        }

        //gets the value of a cell
        public object getValue()
        {
            return value;
        }
        /// <summary>
        /// changes the content of a cell to a new object
        /// </summary>
        /// <param name="content_"></param> object new content
        public void ChangeContent(object content_)
        {
            content = content_;
        }

        /// <summary>
        /// returns the content of this cell
        /// </summary>
        /// <returns></returns> object representation of content
        public object GetContent()
        {
            return content;
        }
    }
}
