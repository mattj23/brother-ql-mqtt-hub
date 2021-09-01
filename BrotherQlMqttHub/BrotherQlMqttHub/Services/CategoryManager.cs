using System.Collections.Concurrent;
using System.Threading.Tasks;
using BrotherQlMqttHub.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BrotherQlMqttHub.Services
{
    public class CategoryManager
    {
        private readonly IServiceScopeFactory _scopeFactory;

        private readonly ConcurrentDictionary<int, TagCategory> _categories;

        public CategoryManager(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
            _categories = new ConcurrentDictionary<int, TagCategory>();
        }


        private void LoadFromDatabase()
        {
            using var factory = _scopeFactory.CreateScope();
            var context = factory.ServiceProvider.GetService<PrinterContext>();

            _categories.Clear();
            foreach (var tagCategory in context.Categories.Include(c => c.Tags))
            {
                _categories[tagCategory.Id] = tagCategory;
            }
        }





    }
}