namespace FlightManager.Data
{
    public interface IQueryDb <T, K>
    {
        bool Exists(K key);
    }
}
