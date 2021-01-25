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
        public List<CrossRoadField> emptyFields = new List<CrossRoadField>();
        public List<CrossRoadField> wordfield = new List<CrossRoadField>();
        
        string[,] crossRoadGrid;
        int dim;
        
        public enum Directions {left,right,up,down,DlUr,UrDl,UlDr,DrUl};


        public MainWindow()
        {
            InitializeComponent();
        }

        private void MainWindow_Load(object sender, EventArgs e)
        {
            //string temp = Resources.czechNouns.ToString();  //load all nouns to list Nouns
            string temp = Resources.engNouns.ToString();
            string[] temp2 = temp.Split('\n');
            for(int i=0;i<temp2.Length;i++)
            {
                string newit = temp2[i].Remove(temp2[i].Length - 1);
                temp2[i] = newit;
            }
            Nouns.AddRange(temp2);
            dim = 10;
            numericUpDown1.Value = 10;
            OutputButton.Visible = false;
        }

        
        public void PrepareGrid(int dim)
        {
            UsableNouns.Clear();
            UsedNouns.Clear();
            fields.Clear();
            emptyFields.Clear();
            crossRoadGrid = new string[dim, dim]; //arr for final crossroads generation
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
            PrepareGrid(dim);
            BackgroundWorker PopulateOnBg = new BackgroundWorker();

            // this allows our worker to report progress during work
            PopulateOnBg.WorkerReportsProgress = true;

            // what to do in the background thread
            PopulateOnBg.DoWork += Populate;
            

            // what to do when progress changed (update the progress bar for example)
            PopulateOnBg.ProgressChanged += new ProgressChangedEventHandler(
            delegate (object o, ProgressChangedEventArgs args)
            {
                toolStripStatusLabel1.Text = "Generuji, zbyva zaplnit :" + args.ProgressPercentage.ToString() +"/" + (dim*dim).ToString();
            });

            // what to do when worker completes its task (notify the user)
            PopulateOnBg.RunWorkerCompleted += new RunWorkerCompletedEventHandler(
            delegate (object o, RunWorkerCompletedEventArgs args)
            {
                toolStripStatusLabel1.Text = "Finished!";
                OutputButton.Visible = true;
            });

            PopulateOnBg.RunWorkerAsync();
            //button1.Visible= false;
           
            
            textBox2.AppendText("testovani");
           

            
        }
        public void Populate(object o, DoWorkEventArgs e)
        {
            wordfield.Clear();
            BackgroundWorker b = o as BackgroundWorker;
            CrossRoadField actfield;
            Directions actWordDirection;
            while (emptyFields.Count>0) // fill all fields of puzzle
            {
                b.ReportProgress(emptyFields.Count());
                actfield = SelectRandomEmptyField(); 
                actWordDirection = SelectRandomDirection();
                             
                wordfield.Clear();
                
                wordfield.AddRange(MapWordSpace(actfield,actWordDirection)); // checked
                bool wordhavebeenfound = false;
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
                    var filloptions = from w in ValidWords.AsParallel()
                                      where Regex.IsMatch(w, querry)
                                      select w;
                    List<string> filloptionsL = new List<string>();
                    filloptionsL.Clear();
                    filloptionsL.AddRange(filloptions.ToArray());
                    if (filloptionsL.Count() == 0)
                    {
                        if (wordfield.Count > 1)
                        {
                            wordfield.RemoveAt(wordfield.Count - 1);
                            
                        }
                        

                        else
                        {
                            actfield.str = SelectRandomChar();
                            //UsedNouns.Add(actfield.str);
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
                    
                    for (int i = actfield.xcoord; i < dim; i++)
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
                   
                    for (int i = actfield.ycoord; i <dim; i++)
                    {
                        var fieldts = from f in fields.AsParallel()
                                      where f.xcoord == actfield.xcoord && f.ycoord == i
                                      select f;
                       
                        found.AddRange(fieldts);
                    }
                    break;
                case Directions.DlUr: //down left to up right corner diagonal
                     xrest = dim - actfield.xcoord;
                     yrest = actfield.ycoord;
                     x = actfield.xcoord;
                     y = actfield.ycoord;
                    while(x<dim&&y>=0)
                    {
                        var fieldts = from f in fields.AsParallel()
                                      where f.xcoord == x && f.ycoord == y
                                      select f;

                        found.AddRange(fieldts);
                        x += 1;
                        y -= 1;
                    }
                    break;
                case Directions.UrDl: //down left to up right corner diagonal
                    xrest = actfield.xcoord;
                    yrest = dim-actfield.ycoord;
                    x = actfield.xcoord;
                    y = actfield.ycoord;
                    while (x >= 0 && y <dim)
                    {
                        var fieldts = from f in fields.AsParallel()
                                      where f.xcoord == x && f.ycoord == y
                                      select f;

                        found.AddRange(fieldts);
                        x -= 1;
                        y += 1;
                    }
                    break;
                case Directions.UlDr: //down left to up right corner diagonal
                    xrest = dim - actfield.xcoord;
                    yrest = dim - actfield.ycoord;
                    x = actfield.xcoord;
                    y = actfield.ycoord;
                    while (x < dim && y < dim)
                    {
                        var fieldts = from f in fields.AsParallel()
                                      where f.xcoord == x && f.ycoord == y
                                      select f;

                        found.AddRange(fieldts);
                        x += 1;
                        y += 1;
                    }
                    break;
                case Directions.DrUl: //down left to up right corner diagonal
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
            
            for (int i = 0; i < dim; i++)
            {
                textBox1.AppendText(Environment.NewLine);
                textBox1.AppendText(Environment.NewLine);
                textBox1.AppendText(Environment.NewLine);
                for (int j = 0; j < dim; j++)
                {
                    var fiel = from f in fields
                               where f.xcoord == j && f.ycoord == i
                               select f;
                    textBox1.AppendText(fiel.ElementAt(0).str.ToUpper() + "\t");
                    

                }
                

            }
            for (int i = 0; i < UsedNouns.Count; i++)
            {
                textBox2.AppendText(UsedNouns.ElementAt(i));
                textBox2.AppendText(Environment.NewLine);
            }
            
            

            
        }

        private void timer1_Tick_1(object sender, EventArgs e)
        {
            toolStripStatusLabel1.Text = emptyFields.Count().ToString();
        }

        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            NumericUpDown num = (NumericUpDown)sender;
            dim = (int)num.Value;
        }

        private void saveFileDialog1_FileOk(object sender, CancelEventArgs e)
        {

        }

        private void PdfButton_Click(object sender, EventArgs e)
        {
            Table table = new Table(dim);
            
            for (int i = 0; i < dim; i++)
            {

                for (int j = 0; j < dim; j++)
                {
                    var fiel = from f in fields
                               where f.xcoord == j && f.ycoord == i
                               select f;

                    table.AddCell(fiel.ElementAt(0).str.ToUpper());

                }
                table.StartNewRow();
            }

            string temp = "C:\\TEST\\test.pdf";

            FileInfo outPdfFile = new FileInfo(temp);
            outPdfFile.Directory.Create();
            PdfDocument outPdf = new PdfDocument(new PdfWriter(outPdfFile.FullName));
            
            Document doc = new Document(outPdf);
            doc.Add(table);
            doc.Close();

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
