using System.Collections.Generic;

namespace AceLand.WebRequest.Core
{
    internal class MultipartBody : RequestBody
    {
        protected override void DisposeManagedResources()
        {
            base.DisposeManagedResources();
            Body.Clear();
        }

        public readonly List<FormData> Body = new();
        public readonly List<StreamData> StreamData = new();
        
        public override string BodyText()
        {
            var text = string.Empty;
            foreach (var data in Body)
                text += $">>>>>> {data.Key} : {data.Value}\n";
            foreach (var data in StreamData)
                text += $">>>>>> {data.Key} : {data.FileName}\n";
            text = text.TrimEnd('\n');
            return text;
        }

    }
}