using Newtonsoft.Json.Linq;
using Ookii.CommandLine;
using System;

namespace ovh.api
{
    class Arguments
    {
        [CommandLineArgument(DefaultValue = "%ovh-ak%", IsRequired = false)]
        public string AK { get; set; }
        [CommandLineArgument(DefaultValue = "%ovh-as%", IsRequired = false)]
        public string AS { get; set; }
        [CommandLineArgument(DefaultValue = "%ovh-ck%", IsRequired = false)]
        public string CK { get; set; }
    }
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                CommandLineParser parser = new CommandLineParser(typeof(Arguments));
                Arguments a = (Arguments)parser.Parse(args);

                a.AK = Environment.ExpandEnvironmentVariables(a.AK);
                a.AS = Environment.ExpandEnvironmentVariables(a.AS);
                a.CK = Environment.ExpandEnvironmentVariables(a.CK);

                Rest r = new Rest(a.AK, a.AS, a.CK);
                dynamic lineState = r.GetAsync<JObject>("https://eu.api.ovh.com/1.0/telephony/rt6-ovh-7/line/0033132465798/options").Result;
                if ((bool)lineState.forwardNoReply)
                    Console.WriteLine($"Redirect if no reply {lineState.forwardNoReplyNumber} {lineState.forwardNoReplyDelay}(s)");
                else if ((bool)lineState.forwardUnconditional)
                    Console.WriteLine($"Unconditional forward {lineState.forwardUnconditionalNumber}");

                lineState.forwardUnconditionalNumber = "0033123456789";
                lineState.forwardUnconditional = true;
                lineState = r.PutAsync<JObject>("https://eu.api.ovh.com/1.0/telephony/rt6-ovh-7/line/0033123456789/options", lineState).Result;

                // Second possible syntax
                r.PutAsync<JObject>("https://eu.api.ovh.com/1.0/telephony/rt6-ovh-7/line/0033123456789/options",
                    new { forwardUnconditional = true, forwardUnconditionalNumber = "0033123456789" }
                    ).Wait();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
