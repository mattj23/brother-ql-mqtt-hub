using System.Collections.Generic;

namespace BrotherQlMqttHub.ViewModels
{
    public interface ICategoryView
    {
        int Id { get; }
        string Name { get; }
        string Description { get; }
        IReadOnlyList<ITagView> Tags { get; }
    }

    public class CategoryView : ICategoryView
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public string Description { get; set; }

        public IReadOnlyList<ITagView> Tags { get; set; }
    }

}