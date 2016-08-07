using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Serialization;

namespace ICFP16
{
    public partial class Form1 : Form
    {
        public static Control RenderTarget;
        public static Control RenderTarget2;
        public static int SleepBetweenSteps = 0;

        public Form1()
        {
            InitializeComponent();
            RenderTarget = panel1;
            RenderTarget2 = panel2;
        }

        int uid = 113;
        State state = new State();
        XmlSerializer ser = new XmlSerializer(typeof(State));

        private void button1_Click(object sender, EventArgs e)
        {
            state.Update();
            Save();
        }

        private void HandleProblem(ProblemState p)
        {
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine("LOOKING AT: " + p.ID);
            Console.WriteLine("BEST SCORE: " + p.BestSolutionScore);
            Console.WriteLine("BEST SIZE: " + p.BestSolutionSize);

            // CACHE CHECK
            if(Constants.EnableCache)
            {
                var cachedSol = state.SearchForSolutionFor(p);
                if (cachedSol != null)
                {
                    var res = p.SubmitSolution(cachedSol);
                    Console.WriteLine("CACHE HIT :)");
                    return;
                }
            }


            var prob = p.GetProblemDescription();
            if (prob == null)
                return;
            prob.Render();

            var g = prob.CreateGraph();
            var sols = g.FullSearch();
            
            var sw = Stopwatch.StartNew();
            string bestSol = null;
            foreach (var solx in sols)
            {
                solx.Render();
                var sol = solx.ToString();
                if (bestSol == null || bestSol.Length > sol.Length)
                {
                    Console.WriteLine("found better sol, size: " + sol.Length);
                    bestSol = sol;
                }


                if (!Constants.ReOptimization)
                    break;
                if (sw.ElapsedMilliseconds > Constants.MaxReOptimizationTimeMS)
                {
                    Console.WriteLine("TIMEOUT ==> no more optimization tries");
                    break;
                }
            }

            if (bestSol == null)
            {
                Console.WriteLine("NO SOLUTION FOUND!");
                return;
            }

            if (bestSol.Length > 5000)
            {
                Console.WriteLine("TOO LARGE!!!");
                return;
            }
            if (bestSol.Length >= p.MyBestSolution.Size && p.SolvedFullScore())
                return;

            //if (prob.Facets.Count != 1)
            //    continue;
            //if (prob.Edges.Count != 4)
            //    continue;
            //if (!prob.Edges.All(ee => ee.GetLengthSquared().IsOne()))
            //    continue;

            //var sol = solBase +
            //    string.Join("\n", prob.Facets[0].Outline.Select(x => x.ToString()));

            try
            {
                var sizePre = p.MyBestSolution.Size;
                var scorePre = p.MyBestSolution.Score;
                var res = p.SubmitSolution(bestSol);
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("PROBLEM: " + p.ID);
                Console.WriteLine("SCORE: " + scorePre + " ==> " + res.Score);
                Console.WriteLine("SIZE: " + sizePre + " ==> " + res.Size);
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine();
            }
            catch (Exception exx)
            {
                Console.WriteLine(exx.GetType());
            }
            Save();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            SleepBetweenSteps = (int)numericUpDown1.Value;

            Func<string, bool> failedBefore = hash =>
            {
                return state.Problems.Any(p => p.hash == hash && p.Description != null && p.MyBestSolution.Score == 0);
            };
            Func<string, bool> succeededBefore = hash =>
            {
                return state.Problems.Any(p => p.hash == hash && p.MyBestSolution.Score == 1);
            };
            Func<string, bool> ensuredBefore = hash =>
            {
                return state.Problems.Any(p => p.hash == hash && p.Description != null);
            };

            var i = 0;
            foreach (var p in state.Problems)
            {
                i++;
                if (ensuredBefore(p.hash))
                    continue;
                p.Ensure();
                Save();
                Console.WriteLine(i);
            }

            Constants.MaxFacetsCount = 1000;
            //Constants.TryWithoutEdges = true;
            var ps = state.Problems.AsEnumerable()
                .Where(x => x.Owner != uid)
                .Where(x => !x.SolvedFullScore())
                .Where(x => x.BestSolutionScore == 1)
                .Where(x => !failedBefore(x.hash) || succeededBefore(x.hash))
                //.OrderBy(x => x.SolutionSize)
                //.OrderBy(x => x.ProblemSize)
                .AsParallel();

            //Parallel.ForEach(ps, p =>
            //{
            //    HandleProblem(p);
            //});

            //ps = state.Problems
            //    .Where(x => x.Owner != uid)
            //    .Where(x => x.GetProblemDescription().CreateGraph().RotationSymmetric90())
            //    .AsParallel();

            foreach (var p in ps)
                HandleProblem(p);
        }

        private ProblemState GetBestie()
        {
            var p = state.Problems.AsEnumerable()
                .Where(x => x.Owner != uid)
                .Where(x => !x.SolvedFullScore())
                .ElementAt(0);

            //p = state.Problems.AsEnumerable()
            //    .First(x => x.GetProblemDescription().CreateGraph().RotationSymmetric90());
            p = state.Problems.First(x => x.ID == 19);
            //p = state.Problems.First(x => x.ID == 3201);
            //p = state.Problems.First(x => x.ID == 687);
            p = state.Problems.First(x => x.ID == 101);
            //p = state.Problems.First(x => x.ID == 688);
            p = state.Problems.First(x => x.ID == 523);
            p = state.Problems.First(x => x.ID == 4551);
            p = state.Problems.First(x => x.ID == 3409);
            p = state.Problems.AsEnumerable()
                .Where(x => x.Owner != uid)
                .Where(x => !x.SolvedFullScore())
                .Where(x => x.BestSolutionScore == 1)
                //.OrderBy(x => x.SolutionSize)
                .OrderByDescending(x => x.ProblemSize)
                .First();

            return p;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            SleepBetweenSteps = (int)numericUpDown1.Value;

            Constants.MaxFacetsCount = 200;
            //Constants.TryWithoutEdges = true;
            HandleProblem(GetBestie());
        }

        private void button4_Click(object sender, EventArgs e)
        {
            foreach (var ft in GetBestie()
                .GetProblemDescription()
                .CreateGraph()
                .EnumFaceTheories())
                Console.WriteLine("GOT");
            Console.WriteLine("DONE");
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            try
            {
                using (var stream = File.OpenRead("state.xml"))
                    state = ser.Deserialize(stream) as State;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                Debug.WriteLine("==> fresh state");
            }
            Save();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            Save();
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        private void Save()
        {
            using (var stream = File.Create("state.xml"))
                ser.Serialize(stream, state);
            string currFN = "state" + DateTime.Now.Day + "_" + DateTime.Now.Hour + "_" + DateTime.Now.Minute + ".xml";
            if (!File.Exists(currFN))
                File.Copy("state.xml", currFN);

            this.richTextBox1.Text = state.ToString();
            this.Text = state.Problems.Count(x => x.SolvedFullScore()) + "/" +
                state.Problems.Count;
        }
    }

}
