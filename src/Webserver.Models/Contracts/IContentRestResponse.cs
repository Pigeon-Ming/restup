namespace Restup.Webserver.Models.Contracts
{
    public interface IContentSerializer : IRestResponse
    {
        object ContentData { get; }
    }
}
