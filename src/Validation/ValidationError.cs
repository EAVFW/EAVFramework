﻿namespace EAVFramework.Validation
{
    public class ValidationError
    {
        public string Error { get; set; }
        public string Code { get; set; }
        public object[] ErrorArgs { get; set; }
        public string AttributeSchemaName { get; set; }
        public string EntityCollectionSchemaName { get;  set; }
    }
}