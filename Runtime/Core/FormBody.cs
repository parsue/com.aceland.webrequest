using System.Collections.Generic;

namespace AceLand.WebRequest.Core
{
    internal class FormBody : RequestBody
    {
        protected override void DisposeManagedResources()
        {
            base.DisposeManagedResources();
            Body.Clear();
        }

        public readonly List<FormData> Body = new();

        public override string BodyText()
        {
            var text = string.Empty;
            foreach (var data in Body)
                text += $">>>>>> {data.Key} : {data.Value}\n";
            text = text.TrimEnd('\n');
            return text;
        }

    }
}