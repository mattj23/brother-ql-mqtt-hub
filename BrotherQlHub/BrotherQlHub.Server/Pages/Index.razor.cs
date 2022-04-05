
using System.Reactive.Linq;
using BrotherQlHub.Server.Services;
using BrotherQlHub.Server.ViewModels;
using Microsoft.AspNetCore.Components;

namespace BrotherQlHub.Server.Pages;

public partial class Index : ComponentBase, IDisposable
{
    [Inject] public PrinterMonitor Monitor { get; set; } = null!;
    [Inject] public CategoryManager Categories { get; set; } = null!;
    
    private Dictionary<string, PrinterViewModel> _printers;
    private Dictionary<int, List<ITagView>> _tagOptions = new();
    private Dictionary<string, Dictionary<int, SelectionContainer>> _tagSelections = new();
    private Dictionary<int, ICategoryView> _categories;

    private IDisposable? _printerUpdates;
    private IDisposable? _categoryUpdates;

    public void Dispose()
    {
        _printerUpdates?.Dispose();
        _categoryUpdates?.Dispose();
    }
    
    protected override async Task OnInitializedAsync()
    {
        _printers = Monitor.GetPrinters().ToDictionary(p => p.Serial, p => p);
        _printerUpdates = Monitor.PrinterUpdates
            .ObserveOn(SynchronizationContext.Current!)
            .Subscribe(ReceivePrinter);

        _categories = (await Categories.GetCategories()).ToDictionary(c => c.Id, c => c);
        _categoryUpdates = Categories.CategoryUpdates
            .ObserveOn(SynchronizationContext.Current!)
            .Subscribe(ReceiveCategory);

        await UpdateEverything();

    }

    private async void OnChange(string printerSerial, object tagView)
    {
        var tag = tagView as ITagView;

        await Monitor.SetPrinterTag(printerSerial, tag.Id);
    }

    private async void ReceivePrinter(PrinterViewModel update)
    {
        _printers[update.Serial] = update;

        await UpdateEverything();
    }

    private async void ReceiveCategory(ItemUpdate<ICategoryView> update)
    {
        if (update.IsDelete)
        {
            _categories.Remove(update.Item.Id);
        }
        else
        {
            _categories[update.Item.Id] = update.Item;
        }

        await UpdateEverything();
    }

    private async Task UpdateEverything()
    {
        if (_categories is null) return;

        _tagOptions.Clear();
        foreach (var cat in _categories)
        {
            _tagOptions[cat.Value.Id] = cat.Value.ToOptions();
        }

        _tagSelections.Clear();
        foreach (var p in _printers.Values)
        {
            _tagSelections[p.Serial] = new Dictionary<int, SelectionContainer>();

            foreach (var cat in _categories.Values)
            {
                int? selectedTagId = p.Tags.ContainsKey(cat.Id) ? (int?)p.Tags[cat.Id] : null;
                var selectedTag = _tagOptions[cat.Id][0];
                if (selectedTagId is not null)
                {
                    selectedTag = _tagOptions[cat.Id].First(t => t.Id == selectedTagId);
                }

                _tagSelections[p.Serial][cat.Id] = new SelectionContainer(p.Serial, this, selectedTag);
            }
        }

        StateHasChanged();

    }

    private class SelectionContainer
    {
        public SelectionContainer(string serial, Index parent, ITagView selected)
        {
            _selectedTag = selected;
            Parent = parent;
            Serial = serial;
        }
        
        private ITagView _selectedTag;
        public Index Parent { get; }
        public string Serial { get; }

        public ITagView SelectedTag
        {
            get => _selectedTag;
            set
            {
                _selectedTag = value;
                Parent.OnChange(Serial, _selectedTag);
            }
        }
    }
}