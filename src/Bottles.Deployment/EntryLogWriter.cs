﻿using System;
using System.Collections.Generic;
using System.Linq;
using Bottles.Environment;
using FubuCore;
using HtmlTags;

namespace Bottles.Deployment
{
    public static class EntryLogWriter
    {
        public static HtmlDocument Write(IEnumerable<LogEntry> entries, string title)
        {
            var tags = createTags(entries);

            throw new NotImplementedException();
            //return DiagnosticHtml.BuildDocument(null, title, tags.ToArray());
        }
        
        private static IEnumerable<HtmlTag> createTags(IEnumerable<LogEntry> entries)
        {
            foreach (LogEntry log in entries)
            {
                var text = "{0} in {1} milliseconds".ToFormat(log.Description, log.TimeInMilliseconds);
                if (!log.Success)
                {
                    text += " -- Failed!";
                }

                var headerTag = new HtmlTag("h4").Text(text).AddClass("log");

                yield return headerTag;

                if (log.TraceText.IsNotEmpty())
                {
                    var traceTag = new HtmlTag("pre").AddClass("log").Text(log.TraceText);
                    if (!log.Success)
                    {
                        traceTag.AddClass("failure");
                    }

                    yield return traceTag;
                }

                yield return new HtmlTag("hr");
            }
        }
    }
}