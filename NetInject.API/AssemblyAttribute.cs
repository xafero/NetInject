using System;

namespace NetInject.API
{
    public sealed class AssemblyAttribute : Attribute
    {
        public string FileName { get; }

        public AssemblyAttribute(string fileName)
        {
            FileName = fileName;
        }
    }
}