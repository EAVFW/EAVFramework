using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;



namespace System.ComponentModel.DataAnnotations.Schema
{
    public class ForeignKeyAttribute : Attribute
    {
        public ForeignKeyAttribute(string name)
        {

        }
    }

    public class InversePropertyAttribute : Attribute
    {
        public InversePropertyAttribute(string name)
        {

        }
    }

}