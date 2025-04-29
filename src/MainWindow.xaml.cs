using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Data;
using System.Windows.Documents;

namespace Crawler
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Methods
        private async Task<string?> FindElementInnerTextDevToolsAsync(string selector)
        {
            try
            {
                var result = await webBrowser.CoreWebView2.CallDevToolsProtocolMethodAsync(
                    "Runtime.evaluate",
                    JsonSerializer.Serialize(new
                    {
                        //expression = $"document.querySelector('{selector}')?.innerText", // Get innerText directly
                        expression = $"document.querySelector('{selector}')?.innerHTML", // Get innerText directly
                        returnByValue = true
                    }));

                using var jsonDocument = JsonDocument.Parse(result);
                if (jsonDocument.RootElement.TryGetProperty("result", out var resultProperty) &&
                    resultProperty.TryGetProperty("value", out var valueProperty) &&
                    valueProperty.ValueKind == JsonValueKind.String) // Check if it's a string
                {
                    return valueProperty.GetString();
                }
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error using DevTools Protocol: {ex.Message}");
                return null;
            }
        }
        private async Task<(IEnumerable<CrawlItem>? Items, string? NextUrl)> GetAllVideoThumbDivsAsync(string selector)
        {
            try
            {
                string script = $@"document.querySelector('div.main-wrap').querySelector('div.thumb-list.thumb-list--sidebar.thumb-list--middle-line.thumb-list--bigger-with-cube').querySelectorAll('div.thumb-list__item.video-thumb.video-thumb--type-video')";
                var result = await webBrowser.CoreWebView2.CallDevToolsProtocolMethodAsync(
                    "Runtime.evaluate",
                    JsonSerializer.Serialize(new
                    {
                        expression = script,
                        returnByValue = true
                    }));

                string? jsonResult = await webBrowser.ExecuteScriptAsync(script);
                jsonResult = await webBrowser.ExecuteScriptAsync("document.querySelector('div.main-wrap').querySelector('div.thumb-list.thumb-list--sidebar.thumb-list--middle-line.thumb-list--bigger-with-cube').querySelectorAll('div.thumb-list__item.video-thumb.video-thumb--type-video').length");
                if (string.IsNullOrEmpty(jsonResult)) return (null, null);
                var countQty = int.Parse(jsonResult);
                List<CrawlItem> crawlItems = new List<CrawlItem>();
                string correctContent(string content) => content.TrimStart('\"').TrimEnd('\"');
                for (int qty = 0; qty < countQty; qty++)
                {
                    var href = await webBrowser.ExecuteScriptAsync($"document.querySelector('div.main-wrap').querySelector('div.thumb-list.thumb-list--sidebar.thumb-list--middle-line.thumb-list--bigger-with-cube').querySelectorAll('div.thumb-list__item.video-thumb.video-thumb--type-video')[{qty}].querySelector('a.video-thumb__image-container.role-pop.thumb-image-container').getAttribute('href')");
                    var image = await webBrowser.ExecuteScriptAsync($"document.querySelector('div.main-wrap').querySelector('div.thumb-list.thumb-list--sidebar.thumb-list--middle-line.thumb-list--bigger-with-cube').querySelectorAll('div.thumb-list__item.video-thumb.video-thumb--type-video')[{qty}].querySelector('a.video-thumb__image-container.role-pop.thumb-image-container').querySelector('img').getAttribute('src')");
                    var duration = await webBrowser.ExecuteScriptAsync($"document.querySelector('div.main-wrap').querySelector('div.thumb-list.thumb-list--sidebar.thumb-list--middle-line.thumb-list--bigger-with-cube').querySelectorAll('div.thumb-list__item.video-thumb.video-thumb--type-video')[{qty}].querySelector('a.video-thumb__image-container.role-pop.thumb-image-container').querySelector('div[data-role=video-duration]').innerText");
                    var title = await webBrowser.ExecuteScriptAsync($"document.querySelector('div.main-wrap').querySelector('div.thumb-list.thumb-list--sidebar.thumb-list--middle-line.thumb-list--bigger-with-cube').querySelectorAll('div.thumb-list__item.video-thumb.video-thumb--type-video')[{qty}].querySelector('div.video-thumb-info').querySelector('a.root-48288').innerText");
                    var model = new CrawlItem()
                    {
                        Href = correctContent(href),
                        Image = correctContent(image),
                        Title = correctContent(title),
                        Duration = correctContent(duration),
                    };
                    if (_currentContext.InvisibleItems.Contains(model.Href!)) continue;
                    crawlItems.Add(model);
                }
                var nextUrl = await webBrowser.ExecuteScriptAsync($"document.querySelector('div.main-wrap').querySelector('a.prev-next-list-link.prev-next-list-link--next').getAttribute('href')");
                return (crawlItems, correctContent(nextUrl));
            }
            catch (Exception ex)
            {
                throw new Exception($"Error getting all video thumb divs: {ex.Message}");
            }
        }
        private async Task ClickElementAsync(string selector)
        {
            try
            {
                string script = $@"const element = document.querySelector('{selector}');" +
                    $" if (element) {{element.click();}} else {{console.log('Element with selector \"{selector}\" not found.');}};";

                await webBrowser.ExecuteScriptAsync(script);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error clicking element: {ex.Message}");
            }
        }
        #endregion Methods
        #region Properties
        private MainWindowModel _currentContext = new MainWindowModel();
        #endregion
        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = _currentContext;
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            tabControl.SelectedIndex = 1;
        }
        private void StartButton_Clicked(object sender, RoutedEventArgs e)
        {
            string url = txtUrl.Text;
            if (string.IsNullOrEmpty(url))
            {
                MessageBox.Show("Please enter a valid URL.");
                return;
            }
            webBrowser.Source = new Uri(url);
        }
        private async void WebBrowser_NavigationCompleted(object sender, Microsoft.Web.WebView2.Core.CoreWebView2NavigationCompletedEventArgs e)
        {
            string buttonSelector = "button.root-64d24.size-big-64d24.color-brand-64d24.fullWidth-64d24";
            string? innerText = await FindElementInnerTextDevToolsAsync(buttonSelector);
            if (string.IsNullOrEmpty(innerText) == false)
            {
                await ClickElementAsync(buttonSelector);
            }
            string divSelector = "div.thumb-list__item.video-thumb.video-thumb--type-video";
            innerText = await FindElementInnerTextDevToolsAsync(divSelector);
            var retVal = await GetAllVideoThumbDivsAsync(divSelector);
            if (_currentContext.CrawlItems == null) _currentContext.CrawlItems = new ObservableCollection<CrawlItem>();
            foreach (var item in retVal.Items!)
            {
                if (_currentContext.CrawlItems.Contains(item) == false)
                {
                    _currentContext.CrawlItems.Add(item);
                }
            }
            var nextUrl = retVal.NextUrl!;
            if (string.IsNullOrEmpty(nextUrl)) return;
            webBrowser.Source = new Uri(nextUrl);
        }
        private void Hyperlink_ClickInTemplate(object sender, RoutedEventArgs e)
        {
            if (sender is Hyperlink link && link.NavigateUri != null)
            {
                try
                {
                    Process.Start(new ProcessStartInfo(link.NavigateUri.AbsoluteUri) { UseShellExecute = true });
                    e.Handled = true;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Could not open link: {ex.Message}");
                }
            }
        }
        private void ItemButton_Clicked(object sender, RoutedEventArgs e)
        {
            var button = sender as System.Windows.Controls.Button;
            if (button == null) return;
            var model = button.DataContext as CrawlItem;
            if (model == null) return;
            var content = button.Content.ToString();
            if (content == "Unfollow")
            {
                _currentContext.ExecuteUnfollowLink(model);
            }
            else if (content == "Invisible")
            {
                _currentContext.ExecuteInvisibleLink(model);
            }
        }
    }
    public class CrawlItem : INotifyPropertyChanged
    {
        #region Implementation
        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion Implementation
        private string? _href;
        public string? Href
        {
            get { return _href; }
            set
            {
                _href = value;
                OnPropertyChanged(nameof(Href));
            }
        }
        private string? _image;
        public string? Image
        {
            get { return _image; }
            set
            {
                _image = value;
                OnPropertyChanged(nameof(Image));
            }
        }
        private string? _title;
        public string? Title
        {
            get { return _title; }
            set
            {
                _title = value;
                OnPropertyChanged(nameof(Title));
            }
        }
        private string? _duration;
        public string? Duration
        {
            get { return _duration; }
            set
            {
                _duration = value;
                OnPropertyChanged(nameof(Duration));
            }
        }
        public string HashString
        {
            get { return $"{Href}{Image}{Title}{Duration}".ToLower(); }
        }
        public override bool Equals(object? obj)
        {
            if (obj == null || (obj is CrawlItem) == false) return false;
            return this.HashString == (obj as CrawlItem)!.HashString;
        }
        public override int GetHashCode()
        {
            return this.HashString.GetHashCode();
        }
        private bool _isInvisible = false;
        public bool IsInvisible
        {
            get { return _isInvisible; }
            set
            {
                _isInvisible = value;
                OnPropertyChanged(nameof(IsInvisible));
            }
        }
    }
    public class MainWindowModel : INotifyPropertyChanged
    {
        #region Implementation
        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion Implementation
        #region Properties
        private ObservableCollection<CrawlItem>? _crawlItems;
        public ObservableCollection<CrawlItem>? CrawlItems
        {
            get { return _crawlItems; }
            set
            {
                _crawlItems = value;
                OnPropertyChanged(nameof(CrawlItems));
            }
        }
        public List<string> InvisibleItems = new List<string>();
        public ICollectionView ItemsView { get; }
        private string? _filterText;
        public string? FilterText
        {
            get { return _filterText; }
            set
            {
                _filterText = value;
                ItemsView.Refresh(); // Apply the filter again
                OnPropertyChanged(nameof(FilterText));
            }
        }
        private bool FilterItems(object item)
        {
            //if (string.IsNullOrEmpty(FilterText)) return true;
            var dataItem = item as CrawlItem;
            return dataItem!.IsInvisible == false;
            //if (dataItem != null)
            //{
            //    // Define your filtering logic here based on dataItem properties
            //    //return dataItem.SomeProperty.Contains(FilterText, StringComparison.OrdinalIgnoreCase);
            //    return true;
            //}
            //return false;
        }

        #endregion
        #region Events
        public MainWindowModel()
        {
            if (File.Exists("invisible.txt"))
            {
                using StreamReader sr = new StreamReader("invisible.txt");
                string? json = sr.ReadToEnd();
                if (string.IsNullOrEmpty(json) == false)
                {
                    InvisibleItems = System.Text.Json.JsonSerializer.Deserialize<List<string>>(json)!;
                }
            }
            CrawlItems = new ObservableCollection<CrawlItem>();
            ItemsView = CollectionViewSource.GetDefaultView(CrawlItems);
            ItemsView.Filter = FilterItems;
        }
        public void ExecuteUnfollowLink(CrawlItem model)
        {
            if (model == null) return;
            InvisibleItems.Add(model.Href!);
            using StreamWriter sw = new StreamWriter("invisible.txt");
            sw.Write(System.Text.Json.JsonSerializer.Serialize(InvisibleItems));
            CrawlItems!.Remove(model);
        }
        public void ExecuteInvisibleLink(CrawlItem model)
        {
            if (model == null) return;
            model.IsInvisible = true;
            ItemsView.Refresh();
        }
        #endregion Events
    }
}