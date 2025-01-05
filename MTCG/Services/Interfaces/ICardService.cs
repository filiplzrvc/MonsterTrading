using MTCG.Models;

namespace MTCG.Services.Interfaces
{
    public interface ICardService
    {
        string ConfigureDeck(int userId, List<Guid> cardIds);
        void DeleteAllCards();
        List<Card> GetAllCards();
        List<Card> GetCardsByUser(int userId);
        List<Card> GetDeck(int userId);
        string InsertCard(Card card);
    }
}