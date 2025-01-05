using MTCG.Models;
using Npgsql;

namespace MTCG.Services.Interfaces
{
    public interface IDatalayer
    {
        NpgsqlConnection GetConnection();
        void InitializeDatabase();
    }
}