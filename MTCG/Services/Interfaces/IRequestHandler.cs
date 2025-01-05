namespace MTCG.Services.Interfaces
{
    public interface IRequestHandler
    {
        void HandleRequest(string method, string path, string body, StreamWriter writer, Dictionary<string, string> headers);
    }
}