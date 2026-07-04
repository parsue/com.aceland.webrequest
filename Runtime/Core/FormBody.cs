using System.Collections.Generic;
using ZLinq;

namespace AceLand.WebRequest.Core
{
    internal class FormBody : RequestBody
    {
        public FormBody() : base(DataType.Form) {}
        
        protected override void DisposeManagedResources()
        {
            base.DisposeManagedResources();
            Body.Clear();
        }

        public readonly List<FormData> Body = new();

        public override string BodyText()
        {
            var text = Body.AsValueEnumerable()
                .Aggregate(
                    string.Empty,
                    (current, data) => current + $">>>>>> {data.Key} : {data.Value}\n"
                )
                .TrimEnd('\n');
            
            return text;
        }

    }
}