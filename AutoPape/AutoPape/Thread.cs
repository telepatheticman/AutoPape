using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AutoPape
{
    class Thread
    {
        string url;
        string threadId;
        Regex rxImages = new Regex(@"(i\.4cdn|is2\.4chan)\.org\/wg\/[0-9]+s?\.(?i)(jpg|png|jpeg)");
        HttpClient client;

        Thread(string board, string threadId)
        {
            this.threadId = threadId;
            url = $"https://boards.4chan.org/{board}/thread/{threadId}";
        }
    }
}
