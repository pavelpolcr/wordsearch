using CrossroadsCZ.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using System.IO;
using iText.Kernel.Font;
using iText.IO.Font;

namespace CrossroadsCZ
{
    public partial class MainWindow : Form
    {
        public List<string> Nouns = new List<string>();
        public List<string> UsableNouns = new List<string>();
        public List<string> UsedNouns = new List<string>();
        public List<CrossRoadField> fields = new List<CrossRoadField>();
        public List<CrossRoadField> emptyFields = new List<CrossRoadField>(); // all puzzle fields not filled already
        public List<CrossRoadField> wordfield = new List<CrossRoadField>();// list containing currently edited fields/currently added word from nouns
        string[,] crossRoadGrid;
        public int PuzzleDimension { get; set; }

        public enum Directions {left,right,up,down,DlUr,UrDl,UlDr,DrUl};


        public MainWindow()
        {
            InitializeComponent();
        }

        private void MainWindow_Load(object sender, EventArgs e)
        {
            PrepareVocabulary();
            PuzzleDimension = 10;
            numericUpDown1.Value = 10;
            
            PdfButton.Visible = false;
        }
        /// <summary>
        /// Prepade list Nouns containing all czech nouns formatted and ready to use. 
        /// </summary>
        private void PrepareVocabulary()
        {
            string temp = Resources.czechNouns.ToString();  //load all nouns to list Nouns
            string[] temp2 = temp.Split('\n');
            for (int i = 0; i < temp2.Length; i++)
            {
                string newit = temp2[i].Remove(temp2[i].Length - 1);
                temp2[i] = newit;
            }
            Nouns.AddRange(temp2);
        }

        public void PrepareGrid(int dim)
        {
            
            UsableNouns.Clear();
            UsedNouns.Clear();
            fields.Clear();
            emptyFields.Clear();
            crossRoadGrid = new string[dim, dim]; //arr for final puzzle generation
            for (int i = 0; i < dim; i++)
            {
                for (int j = 0; j < dim; j++)
                {
                    emptyFields.Add(new CrossRoadField(i, j)); // add empty field 
                    fields.Add(new CrossRoadField(i, j));
                }
            }
            
            
            
            
            var items = from noun in Nouns.AsParallel()
                        where noun.Length <= dim
                        select noun;
            UsableNouns.AddRange(items);
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            button1.Enabled = false;
            numericUpDown1.Enabled = false;
            toolStripStatusLabel1.Text = "Generuji...";
            toolStripProgressBar.Maximum = PuzzleDimension * PuzzleDimension;
            toolStripProgressBar.Step = (PuzzleDimension * PuzzleDimension) / 50;
            toolStripProgressBar.Value = 0;
            toolStripProgressBar.Visible = true;
            PrepareGrid(PuzzleDimension);
            FillPuzzle();
            

        }
        /// <summary>
        /// Uses prepared gris and fill it with words from nouns. 
        /// </summary>
        private void FillPuzzle()
        {
            BackgroundWorker PopulateOnBg = new BackgroundWorker(); // Backgroud worker to async fill of puzzle
            PopulateOnBg.WorkerReportsProgress = true;
            PopulateOnBg.DoWork += Populate;
            PopulateOnBg.ProgressChanged += new ProgressChangedEventHandler(
            delegate (object o, ProgressChangedEventArgs args) //what to do on progress of bg work 
            {
                toolStripStatusLabel1.Text = "Generuji, zbyva zaplnit :" + args.ProgressPercentage.ToString() + "/" + (PuzzleDimension * PuzzleDimension).ToString();
                toolStripProgressBar.Value = (PuzzleDimension * PuzzleDimension) - args.ProgressPercentage;
            });
            PopulateOnBg.RunWorkerCompleted += new RunWorkerCompletedEventHandler( //what to do on work done
            delegate (object o, RunWorkerCompletedEventArgs args)
            {
                toolStripStatusLabel1.Text = "Finished!";
                button1.Enabled = true;
                numericUpDown1.Enabled = true;
                toolStripProgressBar.Visible = false;
                WritePuzzleOutput();
            });

            PopulateOnBg.RunWorkerAsync();
            //button1.Visible= false;
        }
        /// <summary>
        /// Used to actually fill all the wors of puzzle
        /// </summary>
        /// <param name="o"></param>
        /// <param name="e"></param>
        public void Populate(object o, DoWorkEventArgs e)
        {
            wordfield.Clear();
            BackgroundWorker b = o as BackgroundWorker;
            CrossRoadField actfield; // field currently chosen as start of puzzle word composing. 
            Directions actWordDirection;
            while (emptyFields.Count>0) // fill all fields of puzzle
            {
                b.ReportProgress(emptyFields.Count());
                actfield = SelectRandomEmptyField(); 
                actWordDirection = SelectRandomDirection();
                bool wordhavebeenfound = false;
                wordfield.Clear();                
                wordfield.AddRange(MapWordSpace(actfield,actWordDirection)); 
                if (wordfield.Count > 15) // cutoff for speading up big puzzles generation  - we will use only first 15 letters. 
                {
                    wordfield.RemoveRange(15, wordfield.Count - 15);
                }
                while (wordhavebeenfound == false)
                {
                    
                    var ValidWords = from noun in UsableNouns.AsParallel()
                                     where noun.Length <= wordfield.Count
                                     select noun;
                    string querry = "";
                    for (int i = 0; i < wordfield.Count; i++)
                    {
                        if (wordfield.ElementAt(i).str == "")
                        {
                            querry += ".";
                        }
                        else
                        {
                            querry += "["+wordfield.ElementAt(i).str+"]";
                        }

                    }
                    var filloptions = from w in ValidWords.AsParallel() // find all nouns that can match 
                                      where Regex.IsMatch(w, querry)
                                      select w;
                    List<string> filloptionsL = new List<string>();
                    filloptionsL.Clear();
                    filloptionsL.AddRange(filloptions.ToArray());
                    if (filloptionsL.Count() == 0)
                    {
                        if (wordfield.Count > 1)        //delete last character in wordfield and trz again to find shorter word
                        {
                            wordfield.RemoveAt(wordfield.Count - 1);
                            
                        }
                        

                        else
                        {
                            actfield.str = SelectRandomChar();
                            wordhavebeenfound = true;
                            var el = from fld in fields.AsParallel()
                                     where fld.xcoord == actfield.xcoord && fld.ycoord ==actfield.ycoord
                                     select fld;
                            el.ElementAt(0).str = actfield.str;
                            var el2 = from fld in emptyFields.AsParallel()
                                      where fld.xcoord == actfield.xcoord && fld.ycoord == actfield.ycoord
                                      select fld;
                            if (el2.Count() > 0)
                            {
                                emptyFields.Remove(el2.ElementAt(0));
                            }

                        }
                    }
                    else
                    {
                        wordhavebeenfound = true;
                        string wordtofill = filloptionsL.ElementAt(Program.rand.Next(filloptionsL.Count() - 1));
                        UsedNouns.Add(wordtofill.ToUpper());
                        UsableNouns.Remove(wordtofill);
                        for (int i = 0; i < wordfield.Count; i++)
                        {
                            wordfield.ElementAt<CrossRoadField>(i).str = wordtofill.Substring(i, 1);
                            var el = from fld in fields.AsParallel()
                                     where fld.xcoord == wordfield.ElementAt(i).xcoord && fld.ycoord == wordfield.ElementAt(i).ycoord
                                     select fld;
                            el.ElementAt(0).str = wordfield.ElementAt(i).str;
                            var el2 = from fld in emptyFields.AsParallel()
                                      where fld.xcoord == wordfield.ElementAt(i).xcoord && fld.ycoord == wordfield.ElementAt(i).ycoord
                                     select fld;
                            if (el2.Count() > 0)
                            {
                                emptyFields.Remove(el2.ElementAt(0));
                            }
                            
                        }
                    }
                }
                                
            }
            
        }
        public string SelectRandomChar()
        {
            int chrint = Program.rand.Next(65, 91);
            return ((char)chrint).ToString();
        }
        public CrossRoadField SelectRandomEmptyField()
        {

            return emptyFields.ElementAt<CrossRoadField>(Program.rand.Next(emptyFields.Count));
        }
        public Directions SelectRandomDirection()
        {
            int d = Program.rand.Next(8);
            return (Directions)d;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="actfield">chosen free field of puzzle</param>
        /// <param name="dir">chosen direction of word characters</param>
        /// <returns>array of Crossroads fields that combine word</returns>
        public CrossRoadField[] MapWordSpace(CrossRoadField actfield,Directions dir)
        {
            List<CrossRoadField> found = new List<CrossRoadField>();
            int x;
            int y;
            int xrest;
            int yrest;
            
            //found.Add(actfield);
            switch (dir)
            {
                case Directions.left:
                    
                    for (int i = actfield.xcoord; i >=0 ; i--)
                    {
                        var fieldts = from f in fields.AsParallel()
                                      where f.ycoord == actfield.ycoord && f.xcoord == i
                                    select f;

                        found.AddRange(fieldts);
                    }
                    break;
                case Directions.right:
                    
                    for (int i = actfield.xcoord; i < PuzzleDimension; i++)
                    {
                        var fieldts = from f in fields.AsParallel()
                                      where f.ycoord == actfield.ycoord && f.xcoord == i
                                      select f;

                        found.AddRange(fieldts);
                    }
                    break;
                case Directions.up:
                    for (int i = actfield.ycoord; i>=0; i--)
                    {
                        var fieldts = from f in fields.AsParallel()
                                      where f.xcoord == actfield.xcoord && f.ycoord == i
                                      select f;

                        found.AddRange(fieldts);
                    }
                    break;
                case Directions.down:
                   
                    for (int i = actfield.ycoord; i <PuzzleDimension; i++)
                    {
                        var fieldts = from f in fields.AsParallel()
                                      where f.xcoord == actfield.xcoord && f.ycoord == i
                                      select f;
                       
                        found.AddRange(fieldts);
                    }
                    break;
                case Directions.DlUr: //down left to up right corner diagonal
                     xrest = PuzzleDimension - actfield.xcoord;
                     yrest = actfield.ycoord;
                     x = actfield.xcoord;
                     y = actfield.ycoord;
                    while(x<PuzzleDimension&&y>=0)
                    {
                        var fieldts = from f in fields.AsParallel()
                                      where f.xcoord == x && f.ycoord == y
                                      select f;

                        found.AddRange(fieldts);
                        x += 1;
                        y -= 1;
                    }
                    break;
                case Directions.UrDl: //up right to left down corner diagonal 
                    xrest = actfield.xcoord;
                    yrest = PuzzleDimension-actfield.ycoord;
                    x = actfield.xcoord;
                    y = actfield.ycoord;
                    while (x >= 0 && y <PuzzleDimension)
                    {
                        var fieldts = from f in fields.AsParallel()
                                      where f.xcoord == x && f.ycoord == y
                                      select f;

                        found.AddRange(fieldts);
                        x -= 1;
                        y += 1;
                    }
                    break;
                case Directions.UlDr: //up left to down right corner diagonal
                    xrest = PuzzleDimension - actfield.xcoord;
                    yrest = PuzzleDimension - actfield.ycoord;
                    x = actfield.xcoord;
                    y = actfield.ycoord;
                    while (x < PuzzleDimension && y < PuzzleDimension)
                    {
                        var fieldts = from f in fields.AsParallel()
                                      where f.xcoord == x && f.ycoord == y
                                      select f;

                        found.AddRange(fieldts);
                        x += 1;
                        y += 1;
                    }
                    break;
                case Directions.DrUl: //down right to up left corner diagonal
                    xrest = actfield.xcoord;
                    yrest = actfield.ycoord;
                    x = actfield.xcoord;
                    y = actfield.ycoord;
                    while (x >=0 && y >= 0)
                    {
                        var fieldts = from f in fields.AsParallel()
                                      where f.xcoord == x && f.ycoord == y
                                      select f;

                        found.AddRange(fieldts);
                        x -= 1;
                        y -= 1;
                    }
                    break;
                default:
                    break;
            }
            return found.ToArray();
        }
        
        

        private void OutputButton_Click(object sender, EventArgs e)
        {
            WritePuzzleOutput();

        }

        private void WritePuzzleOutput()
        {
            textBox1.Font = new Font(FontFamily.GenericMonospace, textBox1.Font.Size);
            textBox1.Clear();
            textBox2.Clear();
            textBox2.AppendText("Slova ktera lze najit:");
            textBox2.AppendText(Environment.NewLine);
            textBox2.AppendText(Environment.NewLine);
            for (int i = 0; i < PuzzleDimension; i++)
            {
                textBox1.AppendText(Environment.NewLine);
                for (int j = 0; j < PuzzleDimension; j++)
                {
                    var fiel = from f in fields
                               where f.xcoord == j && f.ycoord == i
                               select f;
                    textBox1.AppendText(fiel.ElementAt(0).str.ToUpper() + " ");
                }
            }

            for (int i = 0; i < UsedNouns.Count; i++)
            {
                textBox2.AppendText(UsedNouns.ElementAt(i));
                textBox2.AppendText(Environment.NewLine);
            }
        }

        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            NumericUpDown num = (NumericUpDown)sender;
            PuzzleDimension = (int)num.Value;
        }
        private void PdfButton_Click(object sender, EventArgs e) // currently anused
        {
            Table table = new Table(PuzzleDimension);
            
            
            for (int i = 0; i < PuzzleDimension; i++)
            {

                for (int j = 0; j < PuzzleDimension; j++)
                {
                    var fiel = from f in fields
                               where f.xcoord == j && f.ycoord == i
                               select f;

                    table.AddCell(fiel.ElementAt(0).str.ToUpper());

                }
                table.StartNewRow();
            }

            string temp = "C:\\TEST\\test.pdf";
            if (saveFileDialog1.ShowDialog() == DialogResult.OK) 
            {
                temp = saveFileDialog1.FileName;
                
                FileInfo outPdfFile = new FileInfo(temp);
                outPdfFile.Directory.Create();
                PdfDocument outPdf = new PdfDocument(new PdfWriter(outPdfFile.FullName));

                Document doc = new Document(outPdf);
                doc.Add(table);
                doc.Close();
            }
            

        }
    }
    public class CrossRoadField
    {
        public int xcoord;
        public int ycoord;
        public string str;
        public CrossRoadField(int x,int y)
        {
            this.xcoord = x;
            this.ycoord = y;
            this.str = "";
        }
    }
}
