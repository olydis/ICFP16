using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ICFP16
{
    class ranking
    {
        public double resemblance;
        public int solution_size;
    }
    class problem
    {
        public ranking[] ranking;
        public long publish_time;
        public int solution_size;
        public int problem_id;
        public string owner;
        public int problem_size;
        public string problem_spec_hash;
    }
    class snapshot
    {
        public problem[] problems;
    }
}
