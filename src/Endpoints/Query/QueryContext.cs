using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;

namespace EAVFramework.Endpoints.Query
{
    public class QueryContext<TContext> where TContext : DynamicContext
    {
        public DynamicContext Context { get; set; }
        public Type Type { get; set; }
       public HttpRequest Request { get; set; }

        public Dictionary<IQueryExtender<TContext>, bool> SkipQueryExtenders { get; set; } = new Dictionary<IQueryExtender<TContext>, bool>();
    }
}
