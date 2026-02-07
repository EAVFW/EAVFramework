using DotNETDevOps.JsonFunctions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace EAVFW.Extensions.Manifest.SDK
{
    public class DefaultManifestReplacementRunner : IManifestReplacmentRunner
    {
        private readonly ManifestEnricherOptions options;
        private readonly ISchemaNameManager schemaNameManager;
        private readonly IManifestPathExtracter manifestPathExtracter;

        public DefaultManifestReplacementRunner(IOptions<ManifestEnricherOptions> options,ISchemaNameManager schemaNameManager, IManifestPathExtracter manifestPathExtracter)
        {
            this.options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            this.schemaNameManager = schemaNameManager ?? throw new ArgumentNullException(nameof(schemaNameManager));
            this.manifestPathExtracter = manifestPathExtracter ?? throw new ArgumentNullException(nameof(manifestPathExtracter));
        }

        public virtual bool ShouldEvaluate(string str)
        {
            return str.StartsWith("[") && str.EndsWith("]") && !str.StartsWith("[[");
        }

        public virtual async Task<JToken> EvaluateAsync(ExpressionParser<JToken> expressionParser, string str)
        {
            try
            {
                var nToken = await expressionParser.EvaluateAsync(str);

                if (nToken == null)
                {
                    return nToken;
                }



                if (nToken.Type == JTokenType.Object)
                {
                    var q = new Queue<JToken>();
                    q.Enqueue(nToken);
                    while (q.Count > 0)
                    {
                        var c = q.Dequeue();
                        if (c is JObject o)
                        {
                            foreach (var p in o.Properties())
                                q.Enqueue(p);

                        }
                        else if (c is JProperty p)
                        {
                            if (p.Name.StartsWith("[["))
                            {
                                var nprop = new JProperty(p.Name.Substring(1, p.Name.Length - 2), p.Value);
                                p.Replace(nprop);
                                q.Enqueue(nprop);
                            }
                            else
                            {
                                q.Enqueue(p.Value);
                            }
                        }
                        else if (c is JArray a)
                        {
                            foreach (var e in a)
                                q.Enqueue(e);
                        }
                        else if (c.Type == JTokenType.String && c.ToString().StartsWith("[["))
                        {
                            //  var ch = await expressionParser.EvaluateAsync(c.ToString().Substring(1, c.ToString().Length - 2));
                            //  c.Replace(ch);
                            //  q.Enqueue(ch);
                            var child = c.ToString().Substring(1, c.ToString().Length - 2);
                            // var childToken = await EvaluateAsync(expressionParser, child);
                            c.Replace(child);
                        }
                    }
                }



                while (nToken.Type == JTokenType.String && ShouldEvaluate(nToken.ToString().Substring(1, nToken.ToString().Length - 2)))
                {
                    nToken = await expressionParser.EvaluateAsync(nToken.ToString().Substring(1, nToken.ToString().Length - 2));
                }



                return nToken;
            }
            catch (Exception ex)
            {
                Console.WriteLine("EvaluateAsync: " + ex.ToString());
                throw;
            }
        }

         

        public async Task RunReplacements(JToken jsonraw, string customizationprefix, ILogger logger, JToken elementToRunReplacementFor = null)
        {
            var entityPath = string.Empty;
            var attributePath = string.Empty;
            JToken currentElement = null;
            JToken localelement = null;
            JToken[] localarguments = null;

            var q = new Queue<JToken>(new[] { elementToRunReplacementFor ?? jsonraw });


            var expressionParser = new ExpressionParser<Newtonsoft.Json.Linq.JToken>(
                Options.Create(new ExpressionParserOptions<Newtonsoft.Json.Linq.JToken>() { Document = jsonraw, ThrowOnError = true }), logger,
                new DefaultExpressionFunctionFactory<Newtonsoft.Json.Linq.JToken>()
                {
                    Functions =
                    {
                        ["data"] = (parser,Document,arguments) => {Console.WriteLine(arguments[0]); var child=JToken.Parse(File.ReadAllText(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(options.Path) ,arguments[0]?.ToString())));// q.Enqueue(child);
                                                                                                                                                                                                                                      return Task.FromResult<JToken>(child); },
                        ["customizationprefix"] =(parser,Document,arguments) => Task.FromResult<JToken>(customizationprefix),
                        ["propertyNames"] = (parser,document,arguments) => Task.FromResult<JToken>((arguments[0] is JObject obj ? JToken.FromObject( obj.Properties().Select(k=>k.Name)):new JArray())),
                        ["indexOf"] =(parser,document,arguments) => Task.FromResult<JToken>(Array.IndexOf( arguments[0].ToArray(),arguments[1])),
                        ["default"] = (parser,document,arguments) => Task.FromResult(arguments[0] == null || arguments[0].Type == JTokenType.Null ? arguments[1]:arguments[0]),
                        ["unfoldsource"] = (parser,document,arguments)=>  Task.FromResult(document.SelectToken(arguments[0]["__unroll__path"].ToString())),
                        ["if"] = (parser,document,arguments) => Task.FromResult(arguments[0].ToObject<bool>() ? arguments[1]:arguments[2]),
                        ["condition"] = (parser,document,arguments) => Task.FromResult(arguments[0].ToObject<bool>() ? arguments[1]:arguments[2]),
                        ["in"] =(parser,document,arguments) => Task.FromResult(JToken.FromObject( arguments[1] != null && ( arguments[1] is JObject obj ? obj.ContainsKey(arguments[0].ToString()) :  arguments[1].Any(a=>arguments[0].Equals(a)))  )),
                        ["variables"] = (parser,document,arguments)=> { localarguments= arguments;  return Task.FromResult(jsonraw.SelectToken($"$.variables.{arguments.First()}")?.DeepClone()); },
                        ["concat"] = (parser,document,arguments)=>Task.FromResult<JToken>(string.Join("",arguments.Select(k=>k.ToString())) ),
                        ["entity"] = (parser, document, arguments) =>
                        {
                            var entity = document.SelectToken(entityPath);

                            return Task.FromResult<JToken>(entity);
                        },
                        ["toLogicalName"] = (parser,document,arguments) => Task.FromResult<JToken>(schemaNameManager.ToSchemaName(arguments[0].ToString()).ToLower()),
                        ["attribute"] = (parser, document, arguments) => Task.FromResult(document.SelectToken(attributePath)),
                        ["attributes"] = (parser, document, arguments) => Task.FromResult(document.SelectToken(entityPath+".attributes")),
                        ["select"] = (parser, document, arguments) => Task.FromResult(arguments.FirstOrDefault(a=>!(a== null || a.Type == JTokenType.Null))),
                        ["propertyName"] = (parser, document, arguments) => Task.FromResult<JToken>( arguments[0].Parent is JProperty prop ? prop.Name : null),
                        ["parent"] =(parser, document, arguments) => Task.FromResult<JToken>(  arguments.Any() ?  (arguments[0].Parent is JProperty prop ? prop.Parent:arguments[0].Parent)  :  (currentElement.Parent is JProperty prop1 ? prop1.Parent:currentElement.Parent)),
                        ["element"]=(parser,document,arguments)=>Task.FromResult(localelement ?? currentElement),
                        ["map"] =async (parser, document, arguments) =>{

                            return JToken.FromObject( await Task.WhenAll( arguments[0].Select(a=>{

                            localelement = a;

                            return parser.EvaluateAsync(arguments[1].ToString());


                            })));

                            }


                    }
                });

            while (q.Count > 0)
            {

                var a = q.Dequeue();
                if (a == null)
                    continue;

                entityPath = manifestPathExtracter.ExtractPath(a, "entities");
                attributePath = manifestPathExtracter.ExtractPath(a, "attributes") ?? manifestPathExtracter.ExtractPath(a, "columns");

                try
                {
                    if (a is JProperty prop)
                    {
                        var value = prop.Value;
                        var str = prop.Name;

                        if (ShouldEvaluate(str))
                        {

                            if (str == "[merge()]")
                            {
                                var parentObj = prop.Parent as JObject;
                                var obj = prop.Value;
                                var mergeIndex = Array.IndexOf(parentObj.Properties().ToArray(), prop);
                                var parentObjKeys = parentObj.Properties().Select(k => k.Name).ToArray();

                                if (obj.Type == JTokenType.String && ShouldEvaluate(obj.ToString()))
                                {
                                    currentElement = obj;
                                    obj = await EvaluateAsync(expressionParser, obj.ToString());
                                }

                                foreach (var childProp in (obj as JObject).Properties().ToArray())
                                {

                                    childProp.Remove();
                                  
                                    if (parentObj.ContainsKey(childProp.Name) && childProp.Value is JObject source && parentObj[childProp.Name] is JObject target)
                                    {
                                        target.Merge(source);
                                        q.Enqueue(parentObj.Property(childProp.Name));
                                        // parentObj[childProp.Name]
                                    }
                                    else if (parentObj.ContainsKey(childProp.Name) && JToken.DeepEquals(childProp.Value, parentObj[childProp.Name]))
                                    {
                                        q.Enqueue(parentObj.Property(childProp.Name));
                                    }
                                    else
                                    {

                                        if (!parentObj.ContainsKey(childProp.Name) || Array.IndexOf(parentObjKeys,childProp.Name) < mergeIndex)
                                        {
                                            parentObj[childProp.Name] = childProp.Value;
                                            q.Enqueue(childProp);
                                        }
                                        
                                    }

                                    // parentObj.Add(childProp.Name, childProp.Value);
                                    
                                }

                                prop.Remove();
                                continue;
                            }
                            currentElement = prop.Value;
                            var nToken = await EvaluateAsync(expressionParser, str);



                            if (nToken.Type == JTokenType.Null || nToken.Type == JTokenType.Undefined)
                            {
                                prop.Remove();
                                continue;
                            }



                            var nProp = new JProperty(nToken.ToString(), value);
                            prop.Replace(nProp);
                            q.Enqueue(nProp);
                        }
                        else
                        {


                            q.Enqueue(value);
                        }
                    }
                    else if (a is JObject obj)
                    {
                        foreach (var p in obj.Properties())
                        {

                            q.Enqueue(p);


                        }

                    }
                    else if (a is JArray array)
                    {
                        foreach (var element in array)
                            q.Enqueue(element);

                    }
                    else if (a.Type == JTokenType.String)
                    {
                        var str = a.ToString();

                        if (ShouldEvaluate(str))
                        {
                            currentElement = a;
                            var t = await EvaluateAsync(expressionParser, str);

                            a.Replace(t);
                            q.Enqueue(t);
                        }
                    }

                }
                catch (Exception)
                {
                    Console.WriteLine($"{entityPath}| {attributePath}");
                    throw;
                }
            }
        }
    }
}