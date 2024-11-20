namespace AceLand.WebRequest.Core
{
    internal class JsonBody : RequestBody
    {
        protected override void DisposeManagedResources()
        {
            base.DisposeManagedResources();
            Body = string.Empty;
        }

        public string Body = string.Empty;

        public override string BodyText()
        {
            return $">>>>>> {Body}";
        }
    }
}