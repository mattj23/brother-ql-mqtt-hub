using System.Collections.Generic;
using System.Linq;
using BrotherQlMqttHub.Data;

namespace BrotherQlMqttHub.ViewModels
{
    public static class Extensions
    {
        public static ITagView ToView(this Tag tag)
        {
            return new TagView {Id = tag.Id, Name = tag.Name};
        }

        public static ICategoryView ToView(this TagCategory category)
        {
            return new CategoryView
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description,
                Tags = category.Tags.Select(x => x.ToView()).ToList()
            };
        }

        public static List<ITagView> ToOptions(this ICategoryView category)
        {
            var options = new List<ITagView> {new TagView {Id = -1, Name = "(No Selection)"}};
            options.AddRange(category.Tags);
            return options;
        }
    }
}