namespace FlightManager.Data
{
    public interface IDb <T,K>
    {
        Task CreateAsync(T item);
        Task<T> ReadAsync(K key);
        Task<IEnumerable<T>> ReadAllAsync();
        Task UpdateAsync(T item);
        Task DeleteAsync(K key);
    }
}
