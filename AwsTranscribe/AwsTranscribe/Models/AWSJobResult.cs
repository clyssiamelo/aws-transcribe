using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AwsTranscribe.Models
{
    class AWSJobResult
    {
        public List<TranscriptionDetail> Transcripts { get; set; }
        public List<ItensTranscription> Items { get; set; }
    }
}
