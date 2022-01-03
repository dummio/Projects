using SS;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using SpreadsheetUtilities;
using System.Linq;
using System.Xml;

namespace SpreadsheetTests
{
    [TestClass()]
    public class SpreadsheetTest
    {
        [TestMethod()]
        public void TestSetEntry()
        {
            Spreadsheet sheet = new Spreadsheet();
            sheet.SetContentsOfCell("A1", "9");
            Assert.AreEqual(1, sheet.GetNamesOfAllNonemptyCells().Count());
        }

        [TestMethod()]
        public void TestSetGetEntry()
        {
            Spreadsheet sheet = new Spreadsheet();
            sheet.SetContentsOfCell("A1", "9");
            Assert.AreEqual(9.0, sheet.GetCellContents("A1"));
        }

        [TestMethod()]
        public void TestGetnonEntry()
        {
            Spreadsheet sheet = new Spreadsheet();
            Assert.AreEqual("", sheet.GetCellContents("A1"));
        }

        [TestMethod()]
        [ExpectedException(typeof(InvalidNameException))]
        public void TestGetInvalidEntry()
        {
            Spreadsheet sheet = new Spreadsheet();
            sheet.GetCellContents("1A1");
        }

        [TestMethod()]
        [ExpectedException(typeof(InvalidNameException))]
        public void TestSetInvalidEntryDouble()
        {
            Spreadsheet sheet = new Spreadsheet();
            sheet.SetContentsOfCell("1A1", "9");
        }

        [TestMethod()]
        [ExpectedException(typeof(InvalidNameException))]
        public void TestSetInvalidEntryString()
        {
            Spreadsheet sheet = new Spreadsheet();
            sheet.SetContentsOfCell("1A1", "0");
        }

        [TestMethod()]
        [ExpectedException(typeof(ArgumentNullException))]
        public void TestSetInvalidEntryStringValue()
        {
            Spreadsheet sheet = new Spreadsheet();
            string s = null;
            sheet.SetContentsOfCell("1A1", s);
        }

        [TestMethod()]
        [ExpectedException(typeof(InvalidNameException))]
        public void TestSetInvalidEntryFormula()
        {
            Spreadsheet sheet = new Spreadsheet();
            sheet.SetContentsOfCell("1A1", "=9+10");
        }

        [TestMethod()]
        [ExpectedException(typeof(FormulaFormatException))]
        public void TestSetInvalidEntryFormulaValue()
        {
            Spreadsheet sheet = new Spreadsheet();
            sheet.SetContentsOfCell("A1", "=");
        }

        [TestMethod()]
        [ExpectedException(typeof(CircularException))]
        public void TestCircularFormula()
        {
            Spreadsheet sheet = new Spreadsheet();
            sheet.SetContentsOfCell("A1", "=A2 + 3");
            sheet.SetContentsOfCell("A2", "=A3 + 3");
            sheet.SetContentsOfCell("A3", "=A4 + 3");
            sheet.SetContentsOfCell("A4", "=A5 + 3");
            sheet.SetContentsOfCell("A5", "=A1 + 3");
        }

        [TestMethod()]
        public void TestResetCellValueDouble()
        {
            Spreadsheet sheet = new Spreadsheet();
            sheet.SetContentsOfCell("A1", "=A2 + 3");
            sheet.SetContentsOfCell("A1", "3");
            Assert.AreEqual(3.0, sheet.GetCellContents("A1"));
        }

        [TestMethod()]
        public void TestResetCellValueString()
        {
            Spreadsheet sheet = new Spreadsheet();
            sheet.SetContentsOfCell("A1", "=A2 + 3");
            sheet.SetContentsOfCell("A1", "hi");
            Assert.AreEqual("hi", sheet.GetCellContents("A1"));
        }

        [TestMethod()]
        public void TestResetCellValueFormula()
        {
            Spreadsheet sheet = new Spreadsheet();
            sheet.SetContentsOfCell("A1", "=A2 + 3");
            sheet.SetContentsOfCell("A1", "=1 + 4");
            Assert.AreEqual(new Formula("1 + 4"), sheet.GetCellContents("A1"));
        }

        [TestMethod()]
        public void SaveSpreadsheet()
        {
            Spreadsheet sheet = new Spreadsheet(s => true, s => s, "1.0");
            sheet.SetContentsOfCell("A1", "=5+3");
            sheet.SetContentsOfCell("A2", "9.3");
            sheet.SetContentsOfCell("A3", "=A1 + A2");
            sheet.SetContentsOfCell("A4", "=9 * A3");
            sheet.Save("TestText.xml");
        }

        [TestMethod()]
        public void TestOpenExistingSpreadsheet()
        {
            using (XmlWriter writer = XmlWriter.Create("save.txt")) // NOTICE the file with no path
            {
                writer.WriteStartDocument();
                writer.WriteStartElement("spreadsheet");
                writer.WriteAttributeString("version", "");

                writer.WriteStartElement("cell");
                writer.WriteElementString("name", "A1");
                writer.WriteElementString("contents", "hello");
                writer.WriteEndElement();

                writer.WriteEndElement();
                writer.WriteEndDocument();
            }

            Spreadsheet s = new Spreadsheet("save.txt", s => true, s => s, "");
            Assert.AreEqual("hello", s.GetCellValue("A1"));
        }

        [TestMethod()]
        public void TestOpenExistingSpreadsheet2()
        {
            Spreadsheet s = new Spreadsheet("TestText.xml", s => true, s => s, "1.0");
            Assert.AreEqual(4, s.GetNamesOfAllNonemptyCells().Count());
        }

        [TestMethod()]
        public void TestOpenExistingSpreadsheetAndGetValue()
        {
            Spreadsheet s = new Spreadsheet("TestText.xml", s => true, s => s.ToUpper(), "1.0");
            Assert.AreEqual(155.7, (double) s.GetCellValue("a4"), 1e-9);
            Assert.IsTrue(s.Changed);
        }

        [TestMethod()]
        [ExpectedException(typeof(InvalidNameException))]
        public void testEnterBadVariable()
        {
            Spreadsheet s = new Spreadsheet();
            s.SetContentsOfCell("A?", "5");
        }

        [TestMethod()]
        [ExpectedException(typeof(InvalidNameException))]
        public void testEnterEmptyVariable()
        {
            Spreadsheet s = new Spreadsheet();
            s.SetContentsOfCell("", "5");
        }

        [TestMethod()]
        [ExpectedException(typeof(CircularException))]
        public void testReplaceToCircularFormula()
        {
            Spreadsheet s = new Spreadsheet();
            s.SetContentsOfCell("A1", "=5 + 3");
            s.SetContentsOfCell("A2", "=5 + A1");
            s.SetContentsOfCell("A3", "=5 + A2");
            s.SetContentsOfCell("A4", "=5 + A3");
            s.SetContentsOfCell("A5", "=5 + A4");
            s.SetContentsOfCell("A1", "=5 + A5");
        }

        [TestMethod()]
        [ExpectedException(typeof(SpreadsheetReadWriteException))]
        public void openBadFilePath()
        {
            Spreadsheet s = new Spreadsheet("emptytext.xml", s => true, s => s, "");
        }

        [TestMethod()]
        [ExpectedException(typeof(SpreadsheetReadWriteException))]
        public void openMismatchVersions()
        {
            Spreadsheet s = new Spreadsheet("TestText.xml", s => true, s => s, "");
        }

        [TestMethod()]
        [ExpectedException(typeof(InvalidNameException))]
        public void testIsValidFail()
        {
            Spreadsheet s = new Spreadsheet(s => false, s => s, "1.0");
            s.SetContentsOfCell("A1", "5");
        }
    }
}

       
