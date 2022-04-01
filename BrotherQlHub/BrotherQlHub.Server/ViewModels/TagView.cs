namespace BrotherQlHub.Server.ViewModels
{
    public interface ITagView
    {
        int Id { get; }
        string Name { get; }
    }

    public class TagView : ITagView
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

}