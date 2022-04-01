using System.Collections.Concurrent;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using BrotherQlHub.Data;
using BrotherQlHub.Server.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace BrotherQlHub.Server.Services
{
    public class CategoryManager
    {
        private readonly IServiceScopeFactory _scopeFactory;

        private readonly ConcurrentDictionary<int, ICategoryView> _categories;

        private readonly Subject<ItemUpdate<ICategoryView>> _categorySubject;

        public CategoryManager(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
            _categories = new ConcurrentDictionary<int, ICategoryView>();
            _categorySubject = new Subject<ItemUpdate<ICategoryView>>(); 
        }

        public IObservable<ItemUpdate<ICategoryView>> CategoryUpdates => _categorySubject.AsObservable();

        public async Task<ICategoryView[]> GetCategories()
        {
            if (!_categories.Any())
            {
                await LoadFromDatabase();
            }

            return _categories.Values.ToArray();
        }

        public async Task AddCategory()
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetService<HubContext>();
            var newCategory = new TagCategory
            {
                Name = "New Category",
                Description = "New category description",
                Tags = new List<Tag>()
            };
            await context.Categories.AddAsync(newCategory);
            await context.SaveChangesAsync();

            var view = newCategory.ToView();
            _categories[view.Id] = view;
            _categorySubject.OnNext(new ItemUpdate<ICategoryView>(view, false));
        }

        public async Task UpdateCategory(int id, string name, string description)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetService<HubContext>();
            var target = await context.Categories
                .Include(c => c.Tags)
                .FirstAsync(c => c.Id == id);
            target.Name = name;
            target.Description = description;

            await context.SaveChangesAsync();

            var view = target.ToView();
            _categories[view.Id] = view;
            _categorySubject.OnNext(new ItemUpdate<ICategoryView>(view, false));
        }

        public async Task DeleteCategory(int id)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetService<HubContext>();
            var target = await context.Categories
                .Include(c => c.Tags)
                .FirstAsync(c => c.Id == id);
            context.Categories.Remove(target);

            await context.SaveChangesAsync();

            _categories.TryRemove(target.Id, out var value);
            _categorySubject.OnNext(new ItemUpdate<ICategoryView>(value, true));
        }

        public async Task AddTag(int categoryId)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetService<HubContext>();
            var target = await context.Categories
                .Include(c => c.Tags)
                .FirstAsync(c => c.Id == categoryId);

            target.Tags.Add(new Tag {Name = "New Tag"});
            await context.SaveChangesAsync();

            var view = target.ToView();
            _categories[view.Id] = view;
            _categorySubject.OnNext(new ItemUpdate<ICategoryView>(view, false));
        }

        public async Task UpdateTag(int tagId, string name) 
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetService<HubContext>();
            var target = await context.Tags
                .Include(t => t.Category)
                .FirstAsync(t => t.Id == tagId);

            target.Name = name;

            await context.SaveChangesAsync();

            var cat = await context.Categories
                .Include(c => c.Tags)
                .FirstAsync(c => c.Id == target.Category.Id);

            var view = cat.ToView();
            _categories[view.Id] = view;
            _categorySubject.OnNext(new ItemUpdate<ICategoryView>(view, false));
        }


        public async Task DeleteTag(int tagId) 
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetService<HubContext>();
            var target = await context.Tags
                .Include(t => t.Category)
                .FirstAsync(t => t.Id == tagId);

            context.Tags.Remove(target);

            await context.SaveChangesAsync();

            var cat = await context.Categories
                .Include(c => c.Tags)
                .FirstAsync(c => c.Id == target.Category.Id);

            var view = cat.ToView();
            _categories[view.Id] = view;
            _categorySubject.OnNext(new ItemUpdate<ICategoryView>(view, false));
        }



        private async Task LoadFromDatabase()
        {
            using var factory = _scopeFactory.CreateScope();
            var context = factory.ServiceProvider.GetService<HubContext>();

            var data = await context.Categories
                .Include(c => c.Tags)
                .ToArrayAsync();

            _categories.Clear();
            foreach (var tagCategory in data)
            {
                _categories[tagCategory.Id] = tagCategory.ToView();
            }
        }
    }
}