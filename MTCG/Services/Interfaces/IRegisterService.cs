using MTCG.Models;

namespace MTCG.Services.Interfaces
{
    public interface IRegisterService
    {
        IDatalayer DatabaseConnection { get; }

        User? GetRandomOpponent(User currentUser);
        User GetUserById(int userId);
        User? GetUserByToken(string token);
        User? GetUserByUsername(string username);
        string RegisterUser(string username, string password);
    }
}