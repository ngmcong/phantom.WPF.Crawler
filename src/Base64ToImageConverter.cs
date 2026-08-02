using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace Crawler
{
    internal class Base64ToImageConverter : IValueConverter
    {
        // Hàm phụ trợ nhận diện chuỗi Base64
        private bool IsBase64String(string str)
        {
            if (str.StartsWith("data:image", StringComparison.OrdinalIgnoreCase)) return true;
            // Kiểm tra xem chuỗi có phải Base64 hợp lệ không
            if (str.Length % 4 != 0) return false;
            return System.Text.RegularExpressions.Regex.IsMatch(str, @"^[a-zA-Z0-9\+/]*={0,2}$");
        }
        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var input = value as string;
            if (string.IsNullOrWhiteSpace(input)) return null;
            input = input.Trim();
            try
            {
                // 1. TRƯỜNG HỢP: Chuỗi Base64
                if (IsBase64String(input))
                {
                    string base64Data = input;
                    if (base64Data.Contains(","))
                    {
                        base64Data = base64Data.Split(',')[1];
                    }

                    byte[] binaryData = System.Convert.FromBase64String(base64Data);
                    using (MemoryStream stream = new MemoryStream(binaryData))
                    {
                        BitmapImage image = new BitmapImage();
                        image.BeginInit();
                        image.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
                        image.CacheOption = BitmapCacheOption.OnLoad;
                        image.StreamSource = stream;
                        image.EndInit();
                        image.Freeze(); // Tối ưu bộ nhớ
                        return image;
                    }
                }

                // 2. TRƯỜNG HỢP: URL Web hoặc Đường dẫn File (Path)
                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(input, UriKind.RelativeOrAbsolute);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                return bitmap;
            }
            catch
            {
                // Nếu parse lỗi -> Trả về null để FallbackValue/TargetNullValue trong XAML tự kích hoạt
                return null;
            }
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}