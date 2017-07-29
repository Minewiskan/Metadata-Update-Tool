﻿using System;
using System.Collections.Generic;
using DocoptNet;

namespace mdapply
{
    class Options
    {
        public readonly string usage = @"Usage: mdapply.exe <file> [--bracket | --dash]

Options:
  --bracket -b                Force multiline tags into bracketed lists
  --dash -d                   Force multiline tags into dash lists
  --help -h -?                Show this usage statement
";

        public string ArgFile { get { return null == arguments["<file>"] ? null : arguments["<file>"].ToString(); } }
        public bool OptBracket { get { return arguments["--bracket"].IsTrue; } }
        public bool OptDash { get { return arguments["--dash"].IsTrue; } }

        public Options(string[] args)
        {
            arguments = new Docopt().Apply(usage, args, version: "mdapply 0.1", exit: true);

        }

        public void PrintOptions()
        {
            // debug output
            Console.WriteLine("Recognized options:");
            foreach (var argument in arguments)
            {
                Console.WriteLine("{0} = {1}", argument.Key, argument.Value);
            }
        }

        private IDictionary<string, ValueObject> arguments { get; set; }
    }
}