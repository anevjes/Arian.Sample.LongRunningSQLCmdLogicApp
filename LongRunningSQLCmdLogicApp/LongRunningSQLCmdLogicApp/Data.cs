using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LongRunningSQLCmdLogicApp
{

    public class Param
    {
        public string paramName { get; set; }
        public string value { get; set; }
    }

    public class Root
    {
        public string connectionString { get; set; }
        public string storedProcName { get; set; }
        public List<Param> @params { get; set; }
    }


}
