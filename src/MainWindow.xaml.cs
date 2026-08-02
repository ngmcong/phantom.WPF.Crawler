using phantom.WPF.Crawler;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media.Imaging;

namespace Crawler
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Properties
        private MainWindowModel _currentContext = new MainWindowModel();
        private List<SourcePath> _sourcePaths = new List<SourcePath>
        {
            new SourcePath
            {
                name = "xvideos.com",
                crawlJquery = $@"(function() {{
                const dataArrayString = document.querySelectorAll(""div.frame-block.thumb-block"");
                const datas = Array.from(dataArrayString).map(item => ({{
                    href: item.querySelector(""div.thumb-under"").querySelector(""a"").href,
                    image: item.querySelector(""div.thumb-inside"").querySelector(""img"").getAttribute(""src""),
                    duration: item.querySelector(""div.thumb-under"").querySelector(""p.metadata"").querySelector(""span.duration"").innerHTML,
                    title: item.querySelector(""div.thumb-under"").querySelector(""p.title"").querySelector(""a"").textContent,
                }}));
                return JSON.stringify(datas);
                }})();",
                nextPageJquery = $@"(function() {{
                const element = document.querySelector(""div.pagination "").querySelector(""a.next-page"");
                if (element) {{
                    return element.href;
                }}
                return '';
                }})();"
            },
            new SourcePath
            {
                name = "porntrex.com",
                crawlJquery = $@"(function() {{
                const dataArrayString = document.querySelectorAll(""div.video-preview-screen.video-item.thumb-item"");
                const datas = Array.from(dataArrayString).map(item => ({{
                    href: item.querySelector(""a"").href,
                    image: ""https:"" + item.querySelector(""a"").querySelector(""ul.screenshots-list"").querySelector(""li.screenshot-item.active"").getAttribute(""data-src""),
                    duration: item.querySelector(""div.durations"").querySelector(""i"").innerText,
                    title: item.querySelector(""p.inf"").querySelector(""a"").title,
                }}));
                return JSON.stringify(datas);
                }})();",
                nextPageJquery = $@"(function() {{
                const element = document.querySelector(""li.next"").querySelector(""a"");
                if (element) {{
                    return element.href;
                }}
                return '';
                }})();"
            },
            new SourcePath
            {
                name = "avtoday.io",
                crawlJquery = $@"
                (function() {{
                    const dataArrayString = document.querySelectorAll(""div.thumbnail"");
                    const datas = Array.from(dataArrayString).map(item => {{
                        const videoEl = item.querySelector(""div.video-card"").querySelector(""a"").querySelector(""video"");
                        let imageUrl = """";
                        if (videoEl) {{
                        const style = videoEl.style.background;
                        imageUrl = ""https://avtoday.io/"" + style.slice(style.indexOf('url(""') + 5, style.lastIndexOf('"")'));
                        }}
                        return {{
                        href: item.querySelector(""div.video-card"").querySelector(""a"").href,
                        image: imageUrl,
                        duration: """",
                        title: item.querySelector(""div.video-title"").querySelector(""a"").textContent,
                        }};
                    }});
                    return JSON.stringify(datas);
                }})();
                ",
                nextPageJquery = $@"
                (function() {{
                    const elements = document.querySelectorAll(""ul.pagination li.page-item a.page-link"");
                    if (elements.length > 0) {{
                        // Lấy phần tử cuối cùng trong danh sách (thường là nút Next hoặc trang cuối)
                        const lastItem = elements[elements.length - 1];
                        if (lastItem.classList.contains('active')) {{
                        return '';
                        }}
                        // Trả về href tuyệt đối
                        return lastItem.href;
                    }}
                    return '';
                }})();
                "
            },
            new SourcePath
            {
                name = "xhamster.com",
                crawlJquery = $@"
                (function() {{
                    function wait(ms) {{
                        const start = Date.now();
                        while (Date.now() - start < ms) {{}}
                    }}

                    function waitForData(selector, maxTry) {{
                        for (let i = 0; i < maxTry; i++) {{
                        const items = document.querySelectorAll(selector);
                        if (items.length > 0) {{
                            // Lấy item đầu tiên để kiểm tra
                            const firstItem = items[0];
                            // Tìm thẻ <a>, sau đó tìm <img> bên trong <a>
                            const linkInItem = firstItem.querySelector(""a"");
                            const imgInLink = linkInItem ? linkInItem.querySelector(""img"") : null;

                            // Điều kiện: Có ảnh và src phải là link thật (http...)
                            if (imgInLink && imgInLink.src && imgInLink.src.startsWith('http')) {{
                            return true;
                            }}
                        }}
                        // Sync wait 200ms
                        const start = Date.now();
                        while (Date.now() - start < 200) {{}}
                        }}
                        return false;
                    }}

                    // Đợi cho đến khi các item video xuất hiện và có ảnh thật
                    waitForData(""div.thumb-list__item.video-thumb"", 50);

                    const items = document.querySelectorAll(
                        ""div.thumb-list__item.video-thumb.video-thumb--type-video""
                    );

                    const data = [];

                    for (let i = 0; i < items.length; i++) {{
                        const item = items[i];
                        const link = item.querySelector(""a"");
                        let img = link ? link.querySelector(""img"") : null;
                        // let retry = 0;
                        // while (!img && retry < 5) {{
                        //   wait(200); 
                        //   img = link ? link.querySelector(""img"") : null;
                        //   retry++;
                        // }}
                        const titleEl = item.querySelector(""a.video-thumb-info__name"");

                        data.push({{
                        href: link ? link.href : """",
                        image: img ? img.src : """",
                        duration: """",
                        title: titleEl
                            ? titleEl.textContent.trim()
                            : (link ? link.getAttribute(""aria-label"") : """")
                        }});
                    }}
                    return JSON.stringify(data);
                    }})();
                ",
                nextPageJquery = $@"
                (function() {{
                    const element = document.querySelector(""div.main-wrap"").querySelector(""a.prev-next-list-link.prev-next-list-link--next"");
                    if (element) {{
                        return element.getAttribute(""href"");
                    }}
                    return '';
                }})();
                "
            },
            new SourcePath
            {
                name = "supjav.com",
                crawlJquery = $@"
                (function() {{
                    const dataArrayString = document.querySelectorAll(""div.post"");
                    const datas = Array.from(dataArrayString).map(item => ({{
                        href: item.querySelector(""a"").href,
                        image: item.querySelector(""a"").querySelector(""img"").src,
                        duration: """",
                        title: item.querySelector(""a"").getAttribute(""title""),
                    }}));
                    return JSON.stringify(datas);
                }})();
                ",
                nextPageJquery = $@"
                (function() {{
                    const element = document.querySelector(""li.next-page"").querySelector(""a"");
                    if (element) {{
                        return element.href;
                    }}
                    return '';
                }})();
                "
            },
        };
        #endregion

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
                var url = webBrowser.Source?.AbsoluteUri ?? string.Empty;
                string crawlScript = $@"(function() {{
                const dataArrayString = document.querySelectorAll(""li.pcVideoListItem.js-pop.videoblock"");
                const datas = Array.from(dataArrayString).map(item => ({{
                    href: item.querySelector(""div.wrap"").querySelector(""div.phimage"").querySelector(""a"").href,
                    image: item.querySelector(""div.wrap"").querySelector(""div.phimage"").querySelector(""a"").querySelector(""img"").src,
                    duration: item.querySelector(""div.wrap"").querySelector(""div.phimage"").querySelector(""a"").querySelector(""var"").innerText,
                    title: item.querySelector(""div.wrap"").querySelector(""div.thumbnail-info-wrapper"").querySelector(""span.title"").querySelector(""a"").title,
                }}));
                return JSON.stringify(datas);
                }})()";
                var nextUrlScript = $@"(function() {{
                const element = document.querySelectorAll(""li.page_next"");
                if (element) {{
                    return element[element.length - 1].querySelector(""a"").href;
                }}
                return '';
                }})();";
                var sourcePath = _sourcePaths.FirstOrDefault(x => url.Contains(x.name));
                if (sourcePath != null)
                {
                    crawlScript = sourcePath.crawlJquery ?? crawlScript;
                    nextUrlScript = sourcePath.nextPageJquery ?? nextUrlScript;
                }
                var result = await webBrowser.CoreWebView2.CallDevToolsProtocolMethodAsync(
                    "Runtime.evaluate",
                    JsonSerializer.Serialize(new
                    {
                        expression = crawlScript,
                        returnByValue = true
                    }));

                var jsonResult = await webBrowser.ExecuteScriptAsync(crawlScript);
                if (string.IsNullOrEmpty(jsonResult)) return (null, null);
                jsonResult = JsonSerializer.Deserialize<string>(jsonResult);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                var crawlItems = JsonSerializer.Deserialize<IEnumerable<CrawlItem>>(jsonResult!, options);
                crawlItems = from cr in crawlItems
                             join iv in _currentContext.InvisibleItems.GroupBy(x => x).Select(x => x.Key) on cr.Href equals iv into leftIV
                             from iv in leftIV.DefaultIfEmpty()
                             where iv == null
                             select cr;
                string correctContent(string content) => content?.TrimStart('\"').TrimEnd('\"') ?? string.Empty;
                var nextUrl = await webBrowser.ExecuteScriptAsync(nextUrlScript);
                nextUrl = JsonSerializer.Deserialize<string>(nextUrl);
                return (crawlItems, correctContent(nextUrl!));
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
            if (_currentContext.CrawlItems?.Count() > 1000) return;
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
        private void Image_ImageFailed(object sender, ExceptionRoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Image image && image.DataContext is CrawlItem failedItem)
            {
                failedItem.IsImageError = true;
            }
        }
        private void Image_TargetUpdated(object sender, DataTransferEventArgs e)
        {
            if (sender is System.Windows.Controls.Image image && image.DataContext is CrawlItem failedItem)
            {
                if (image.Source is BitmapImage bitmap)
                {
                    string uriString = bitmap.UriSource?.ToString() ?? "";
                    // 2. Kiểm tra xem URI có chứa tên ảnh lỗi không
                    if (uriString.Contains("ErrorImage.png")) failedItem.IsImageError = true;
                }
            }
        }
        private void HideErrorsButton_Clicked(object sender, RoutedEventArgs e)
        {
            if (_currentContext?.CrawlItems == null) return;
            foreach (var item in _currentContext.CrawlItems)
            {
                item.IsInvisible = item.IsImageError || string.IsNullOrEmpty(item.Image);
            }
            _currentContext.ItemsView.Refresh();
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
        private bool _isImageError = false;
        public bool IsImageError
        {
            get { return _isImageError; }
            set
            {
                _isImageError = value;
                OnPropertyChanged(nameof(IsImageError));
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
        private readonly string invisiblePath = "F:\\Downloads\\invisible.txt";
        #endregion

        #region Events
        public MainWindowModel()
        {
            if (File.Exists(invisiblePath))
            {
                using StreamReader sr = new StreamReader(invisiblePath);
                string? json = sr.ReadToEnd();
                if (string.IsNullOrEmpty(json) == false)
                {
                    InvisibleItems = System.Text.Json.JsonSerializer.Deserialize<List<string>>(json)!;
                }
                sr.Close();
                sr.Dispose();
            }
            CrawlItems = new ObservableCollection<CrawlItem>();
            ItemsView = CollectionViewSource.GetDefaultView(CrawlItems);
            ItemsView.Filter = FilterItems;
        }
        public async void ExecuteUnfollowLink(CrawlItem model)
        {
            if (model == null) return;
            InvisibleItems.Add(model.Href!);
            await File.WriteAllTextAsync(invisiblePath, System.Text.Json.JsonSerializer.Serialize(InvisibleItems));
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