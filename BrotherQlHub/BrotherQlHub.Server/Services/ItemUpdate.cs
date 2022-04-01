namespace BrotherQlHub.Server.Services
{
    public class ItemUpdate<T>
    {
        public ItemUpdate(T item, bool isDelete)
        {
            Item = item;
            IsDelete = isDelete;
        }

        public T Item { get; }
        public bool IsDelete { get; }
        
    }
}