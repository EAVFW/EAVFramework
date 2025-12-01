using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;

namespace EAVFramework.Endpoints.Query
{
    public class QueryContext
    {
        public DynamicContext Context { get; set; }
        public Type Type { get; set; }
        public HttpRequest Request { get; set; }


    }
    public class QueryContext<TContext> : QueryContext
        where TContext : DynamicContext
    {


        public Dictionary<IQueryExtender<TContext>, bool> SkipQueryExtenders { get; set; } = new Dictionary<IQueryExtender<TContext>, bool>();
    }
}