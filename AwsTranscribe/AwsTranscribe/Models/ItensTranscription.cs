using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AwsTranscribe.Models
{
    class ItensTranscription
    {
        public string Start_time { get; set; }
        public string End_time { get; set; }
        public List<Alternative> ALternatives { get; set; }
        public string Type { get; set; }
    }
}
