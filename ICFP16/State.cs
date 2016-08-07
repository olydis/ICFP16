using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ICFP16
{
    public class State
    {
        public State()
        {
            Problems = new List<ProblemState>();
        }

        public List<ProblemState> Problems;

        internal void Update()
        {
            var res = WC.GetInstance().GetRecentSnapshot();
            foreach (var p in res.problems)
            {
                var id = p.problem_id;
                if (!Problems.Any(x => x.ID == id))
                    Problems.Add(new ProblemState { ID = id, hash = p.problem_spec_hash });
                var state = Problems.First(x => x.ID == id);
                if (p.ranking.Length > 0)
                {
                    state.BestSolutionSize = p.ranking.First().solution_size;
                    state.BestSolutionScore = p.ranking.First().resemblance;
                }
                else
                {
                    state.BestSolutionSize = int.MaxValue;
                    state.BestSolutionScore = 0;
                }
                state.SolutionSize = p.solution_size;
                state.ProblemSize = p.problem_size;
                state.Owner = int.Parse(p.owner);
            }
        }

        public override string ToString()
        {
            //return string.Join("\n", Problems
            //    .Where(p => !p.SolvedPerfectly()));
            return string.Join("\n", Problems
                .Where(p => !p.SolvedFullScore()));
        }

        internal string SearchForSolutionFor(ProblemState p)
        {
            // hash match?
            string hash = p.hash;
            var matchHash = this
                .Problems
                .Where(x => x.SolvedFullScore())
                .Where(x => x.hash == p.hash)
                .OrderByDescending(x => x.MyBestSolution.Size)
                .FirstOrDefault();
            if (matchHash != null)
                return matchHash.MyBestSolution.Text;

            p.Ensure();
            var probStmt = p.Description;
            var match = this
                .Problems
                .Where(x => x.SolvedFullScore())
                .Where(x => x.Description == probStmt)
                .OrderByDescending(x => x.MyBestSolution.Size)
                .FirstOrDefault();
            return match == null
                ? null
                : match.MyBestSolution.Text;
        }
    }

    public class ProblemState
    {
        public ProblemState()
        {
            this.MyBestSolution = new Solution
            {
                Score = 0,
                Size = int.MaxValue,
                Text = "",
                hash = null
            };
        }

        public int ID;
        public string hash;
        public string Description;
        public Solution MyBestSolution;

        public double BestSolutionScore;
        public int BestSolutionSize;
        public int ProblemSize;
        public int SolutionSize;
        public int Owner;

        public bool SolvedPerfectly()
        {
            return MyBestSolution.Score == 1
                && MyBestSolution.Size <= BestSolutionSize
                && MyBestSolution.Size <= SolutionSize;
        }
        public bool SolvedFullScore()
        {
            return MyBestSolution.Score == 1;
        }

        public void Ensure()
        {
            if (Description != null)
                return;
            Description = WC.GetInstance().GetProbDesc(hash);
        }
        public Problem GetProblemDescription()
        {
            Ensure();
            return Problem.Parse(Description);
        }

        public Solution SubmitSolution(string spec)
        {
            var res = WC.GetInstance().PostSolution(ID, spec);
            var sol = new Solution
            {
                Text = spec,
                Score = res.Value<double>("resemblance"),
                Size = res.Value<int>("solution_size"),
                hash = res.Value<string>("solution_spec_hash")
            };
            // update
            if (MyBestSolution == null)
                MyBestSolution = sol;
            if (MyBestSolution.Score < sol.Score)
                MyBestSolution = sol;
            if (MyBestSolution.Score == sol.Score && MyBestSolution.Size > sol.Size)
                MyBestSolution = sol;
            return sol;
        }

        public override string ToString()
        {
            return ID.ToString("0000")
                + " - " + MyBestSolution.Score.ToString("0.000000") + "/" + BestSolutionScore.ToString("0.000000")
                + " - " + MyBestSolution.Size.ToString("0000") + "/" + BestSolutionSize.ToString("0000");
        }
    }

    public class Solution
    {
        public string Text;

        public double Score;
        public int Size;
        public string hash;
    }
}
