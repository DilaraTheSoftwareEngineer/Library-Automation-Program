using DevExpress.Xpf.Core;
using LibraryAutomation.Data;
using LibraryAutomation.Models;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging; // BitmapImage için gerekli olabilir

namespace LibraryAutomationn
{
    public partial class MainWindow : ThemedWindow
    {
        public string CurrentUser { get; set; }
        public string BorrowedBy { get; set; }

        public MainWindow(string userName)
        {
            InitializeComponent();
            this.CurrentUser = userName;
            txtWelcome.Text = $"Hoş Geldin, {CurrentUser}";
            LoadData();
        }

        private void LoadData()
        {
            using (var db = new LibraryDbContext())
            {
                // db.Database.EnsureDeleted(); // Gerekirse açılabilir
                db.Database.EnsureCreated();

                if (!db.Books.Any())
                {
                    var g1 = new Genre { Name = "Fantastik" };
                    var g2 = new Genre { Name = "Klasik" };
                    var g3 = new Genre { Name = "Bilim Kurgu" };
                    var g4 = new Genre { Name = "Distopya" };
                    db.Genres.AddRange(g1, g2, g3, g4);
                    db.SaveChanges();

                    var a1 = new Author { Name = "J.R.R. Tolkien", Biography = "Yüzüklerin Efendisi yazarı." };
                    var a2 = new Author { Name = "George Orwell", Biography = "1984 yazarı." };
                    db.Authors.AddRange(a1, a2);
                    db.SaveChanges();

                    db.Books.AddRange(new List<Book> {
                        new Book { Title = "Yüzük Kardeşliği", GenreId = g1.Id, AuthorId = a1.Id, ImageUrl = "https://images.isbndb.com/covers/92/23/9789753421923.jpg", IsBorrowed = false, ShelfInfo = "A-1" },
                        new Book { Title = "1984", GenreId = g4.Id, AuthorId = a2.Id, ImageUrl = "https://i.dr.com.tr/cache/600x600-0/originals/0000000064038-1.jpg", IsBorrowed = false, ShelfInfo = "B-2" }
                    });
                    db.SaveChanges();
                }
                var allGenres = db.Genres.ToList();
                var allAuthors = db.Authors.ToList();
                GenresList.ItemsSource = allGenres;
                AuthorsList.ItemsSource = allAuthors;
                var allBooks = db.Books.Include(b => b.Author).Include(b => b.Genre).ToList();
                BooksList.ItemsSource = allBooks;
                PastBooksList.ItemsSource = db.Books
                .Include(b => b.Author).Include(b => b.Genre)
                .Where(b => b.WasBorrowedBefore == true && b.IsBorrowed == false)
                .ToList();
            }
        }

        private void Menu_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            if (btn == null) return;
            string tag = btn.Tag.ToString();
            if (tag == "Dash")
            {
                DashPanel.Visibility = Visibility.Visible;
                txtWelcome.Text = "Dashboard";
                LoadData();
            }
            else if (tag == "MyBooks")
            {
                SwitchPanel("MyBooks");
                txtWelcome.Text = "Kişisel Kitaplığım";
                using (var db = new LibraryDbContext())
                {
                    var all = db.Books.Include(b => b.Author).Include(b => b.Genre).ToList();
                    CurrentBorrowedList.ItemsSource = all.Where(b => b.BorrowedBy == CurrentUser && b.IsBorrowed).ToList();
                    FavoritesList.ItemsSource = all.Where(b => b.IsFavorite).ToList();
                    PastBooksList.ItemsSource = all.Where(b => b.WasBorrowedBefore && !b.IsBorrowed).ToList();
                }
            }
            else if (tag == "Badges")
            {
                SwitchPanel("Badges");
                txtWelcome.Text = "Başarılarım & Rozetler";
            }
            else if (tag == "Discovery")
            {
                SwitchPanel("Discovery");
                txtWelcome.Text = "Keşfet";
            }
            else if (tag == "Admin") // Admin butonu kontrolü
            {
                if (CurrentUser.ToLower() == "admin")
                {
                    SwitchPanel("Admin");
                    RefreshAdminStats();
                    LoadAdminResources();
                    txtWelcome.Text = "Yönetim Paneli";
                }
                else
                {
                    MessageBox.Show("Bu alana erişim yetkiniz yok!");
                }
            }
        }
        private void SwitchPanel(string panelName)
        {
           
            // BÜTÜN ana panelleri buraya ekle ki hepsi kapansın
            DashPanel.Visibility = Visibility.Collapsed;
            BookDetailPanel.Visibility = Visibility.Collapsed;
            MyBooksPanel.Visibility = Visibility.Collapsed;
            BadgesPanel.Visibility = Visibility.Collapsed;
            DiscoveryPanel.Visibility = Visibility.Collapsed;
            AuthorDetailPanel.Visibility = Visibility.Collapsed;
            GenreDetailPanel.Visibility = Visibility.Collapsed;
            AdminPanel.Visibility = Visibility.Collapsed;

            // Sadece hedef paneli aç
            switch (panelName)
            {
                case "Dash": DashPanel.Visibility = Visibility.Visible; 
                    break;
                case "Admin": AdminPanel.Visibility = Visibility.Visible;
                    break;
                case "Book": BookDetailPanel.Visibility = Visibility.Visible; 
                    break;
                case "MyBooks": MyBooksPanel.Visibility = Visibility.Visible; 
                    break;
                case "Badges": BadgesPanel.Visibility = Visibility.Visible;
                    UpdateBadges();
                    break;
                case "Discovery": DiscoveryPanel.Visibility = Visibility.Visible;
                    LoadDiscovery();
                    break;
            }
        
        }

        private void RefreshAdminStats()
        {
            using (var db = new LibraryDbContext())
            {
                // 1. Toplam Kitap Sayısı
                int totalBooks = db.Books.Count();
                txtStatTotalBooks.Text = totalBooks.ToString();

                // 2. Ödünçteki Kitap Sayısı
                int borrowedBooks = db.Books.Count(b => b.IsBorrowed);
                txtStatBorrowedBooks.Text = borrowedBooks.ToString();

                // 3. Toplam Üye Sayısı
                int totalUsers = db.Users.Count();
                txtStatTotalUsers.Text = totalUsers.ToString();

                // 4. En Popüler Türü Bulma (En çok kitabı olan tür)
                var popularGenre = db.Books
                    .Include(b => b.Genre)
                    .GroupBy(b => b.Genre.Name)
                        .OrderByDescending(g => g.Count())
                        .Select(g => g.Key)
                        .FirstOrDefault();

                txtStatPopularGenre.Text = popularGenre ?? "Tanımsız";
            }
        }
         private void SetImageSource(Image imgElement, string url)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(url) || !url.StartsWith("http"))
                {
                    
                    imgElement.Source = new BitmapImage(new Uri("https://via.placeholder.com/150"));
                }
                else
                {
                    imgElement.Source = new BitmapImage(new Uri(url));
                }
            }
            catch
            {
                imgElement.Source = new BitmapImage(new Uri("https://via.placeholder.com/150"));
            }
        }

        // 2. Yazar ekleme (Biyografi ile)
        private void AdminAddAuthor_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtQuickAuthorName.Text))
            {
                MessageBox.Show("Lütfen yazar ismini giriniz.", "Uyarı");
                return;
            }
            using (var db = new LibraryDbContext())
            {
                var author = new Author
                {
                    Name = txtQuickAuthorName.Text,
                    Biography = txtAuthorBio.Text
                };
                db.Authors.Add(author);
                db.SaveChanges();
                MessageBox.Show("Yazar ve Biyografisi eklendi!");
                LoadAdminResources();
            }
        }

        // 3. Kitap ekleme (Raf Konumu ile)
        private void AdminAddBook_Click(object sender, RoutedEventArgs e)
        {
            var author = cbAddAuthor.SelectedItem as Author;
            if (author == null) return;

            using (var db = new LibraryDbContext())
            {
                var book = new Book
                {
                    Title = txtAddTitle.Text,
                    AuthorId = author.Id,
                    GenreId = (cbAddGenre.SelectedItem as Genre)?.Id ?? 1,
                    ShelfInfo = txtAddShelf.Text, // Raf Konumu
                    ImageUrl = txtAddImageUrl.Text,
                    IsBorrowed = false
                };
                db.Books.Add(book);
                db.SaveChanges();
            }
            LoadAdminResources();
            MessageBox.Show("Kitap Raf Bilgisiyle Eklendi!");
        }

        // 4. Dashboard Butonu Fix (SwitchPanel'e AdminPanel'i mutlaka ekle!)
      

        private void CardBorder_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.DataContext is Book listItem)
            {
                using (var db = new LibraryDbContext())
                {
                    var selectedBook = db.Books.Include(b => b.Author).Include(b => b.Genre).FirstOrDefault(b => b.Id == listItem.Id);
                    if (selectedBook == null) return;

                    // Panellerin ayarlanması
                    DashPanel.Visibility = Visibility.Collapsed;
                    MyBooksPanel.Visibility = Visibility.Collapsed;
                    BookDetailPanel.Visibility = Visibility.Visible;

                    // Verilerin doldurulması
                    imgDetay.Source = new BitmapImage(new Uri(selectedBook.ImageUrl ?? ""));
                    txtDetayBaslik.Text = selectedBook.Title;
                    txtDetayYazar.Text = selectedBook.Author?.Name;
                    txtDetayYazar.Tag = selectedBook.Author;
                    txtDetayTur.Text = "Kategori: " + (selectedBook.Genre?.Name ?? "Türsüz");
                    txtDetayRaf.Text = "Konum: " + selectedBook.ShelfInfo;
                    txtDetayAI.Text = selectedBook.AISummary;

                    // MANTIK: Kitap Rafta mı, Kullanıcıda mı, yoksa Başkasında mı?
                    bool bendeMi = selectedBook.IsBorrowed && string.Equals(selectedBook.BorrowedBy?.Trim(), CurrentUser?.Trim(), StringComparison.OrdinalIgnoreCase);

                    if (!selectedBook.IsBorrowed)
                    {
                        // DURUM 1: KİTAP RAFTA
                        brdStatus.Background = Brushes.Green;
                        txtStatus.Text = "RAFTA";
                        btnBorrowAction.Content = "Ödünç Al";
                        btnBorrowAction.IsEnabled = true;
                        btnBorrowAction.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF00E5FF"));
                    }
                    else if (bendeMi)
                    {
                        // DURUM 2: KİTAP KULLANICININ KENDİSİNDE
                        int toplamSure = selectedBook.IsExtended ? 25 : 15;
                        int gecenGun = selectedBook.BorrowDate.HasValue ? (DateTime.Now - selectedBook.BorrowDate.Value).Days : 0;
                        int kalanGun = toplamSure - gecenGun;

                        brdStatus.Background = Brushes.Orange;
                        txtStatus.Text = $"ÜZERİNİZDE ({kalanGun} Gün Kaldı)";
                        btnBorrowAction.Content = "İade Et";
                        btnBorrowAction.IsEnabled = true;
                        btnBorrowAction.Background = Brushes.Orange;

                        // SÜRE UZATMA MANTIĞI: Son 5 gün kala ve daha önce uzatılmadıysa
                        if (kalanGun <= 5 && kalanGun > 0 && !selectedBook.IsExtended)
                        {
                            var cevap = MessageBox.Show("Kitap süresinin dolmasına az kaldı. Süreyi 10 gün uzatmak ister misiniz?", "Süre Uzatma", MessageBoxButton.YesNo);
                            if (cevap == MessageBoxResult.Yes)
                            {
                                selectedBook.IsExtended = true;
                                db.SaveChanges();
                                MessageBox.Show("Süreniz başarıyla 10 gün uzatıldı! Yeni iade süreniz toplam 25 gün oldu.");
                                txtStatus.Text = $"ÜZERİNİZDE ({kalanGun + 10} Gün Kaldı)";
                            }
                        }
                    }
                    else
                    {
                        // DURUM 3: KİTAP BAŞKASINDA
                        int toplamSure = selectedBook.IsExtended ? 25 : 15;
                        int gecenGun = selectedBook.BorrowDate.HasValue ? (DateTime.Now - selectedBook.BorrowDate.Value).Days : 0;
                        int kalanGun = toplamSure - gecenGun;

                        string adminBilgisi = (CurrentUser == "admin" || CurrentUser == "Admin") ? $" [{selectedBook.BorrowedBy}]" : "";

                        brdStatus.Background = Brushes.Red;
                        txtStatus.Text = $"ÖDÜNÇTE - Kalan: {kalanGun} Gün{adminBilgisi}";
                        btnBorrowAction.Content = "Mevcut Değil";
                        btnBorrowAction.IsEnabled = false; // Başkasının kitabını ödünç alamaz/iade edemez
                        btnBorrowAction.Background = Brushes.Gray;
                    }

                    // AI Analizini başlat
                    _ = GetRealAIAnalysis(selectedBook.Title, selectedBook.Author?.Name);
                }
            }
        }
        private void AuthorCard_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement fe)
            {
                string authorName = fe.Tag?.ToString(); // XAML'de Tag'e {Binding Name} vermiştik

                using (var db = new LibraryDbContext())
                {
                    var filtered = db.Books.Include(b => b.Author).Include(b => b.Genre)
                                     .Where(b => b.Author.Name == authorName).ToList();

                    BooksList.ItemsSource = filtered;
                    txtWelcome.Text = $"{authorName} Kitapları";
                }
            }
        }
       

        // Yazar Detayı Metodu (Kitap Detayından veya Karttan Tıklanınca Çalışır)
        private void AuthorDetail_Click(object sender, MouseButtonEventArgs e)
        {
            string targetAuthorName = "";

            if (sender is TextBlock tb)
            {
                // Eğer detay sayfasındaki yazar adına tıklandıysa
                targetAuthorName = tb.Text;
            }
            else if (sender is FrameworkElement fe && fe.DataContext is Book b)
            {
                // Eğer kitap kartındaki yazar adına tıklandıysa
                targetAuthorName = b.Author?.Name;
            }

            if (string.IsNullOrEmpty(targetAuthorName)) return;

            using (var db = new LibraryDbContext())
            {
                var author = db.Authors.FirstOrDefault(a => a.Name == targetAuthorName);
                if (author != null)
                {
                    DashPanel.Visibility = Visibility.Collapsed;
                    BookDetailPanel.Visibility = Visibility.Collapsed;
                    AuthorDetailPanel.Visibility = Visibility.Visible;

                    txtAuthorName.Text = author.Name;
                    txtAuthorBioDetail.Text = author.Biography ?? "Biyografi bulunamadı.";

                    // Yazarın diğer kitaplarını da altına yükle
                    AuthorBooksList.ItemsSource = db.Books.Include(b => b.Author).Include(b => b.Genre)
                                                    .Where(b => b.AuthorId == author.Id).ToList();
                }
            }
        }
        private void UpdateBadges()
        {
            using (var db = new LibraryDbContext())
            {
                var readCount = db.Books.Count(b => b.WasBorrowedBefore == true );
                txtReadingStats.Text = $"Toplam {readCount} Kitap Okundu";
                txtProfileName.Text = $"Hoşgeldin, {CurrentUser}!";

                 if (readCount >= 2)
                {
                    badgeBookWorm.Opacity = 1.0;
                    badgeBookWorm.Background = Brushes.Gold;
                }

                if (readCount >= 10)
                {
                    badgeSpeedReader.Opacity = 1.0;
                    badgeSpeedReader.Background = Brushes.Cyan;
                }

                if (readCount >= 15)
                {
                    badgeNightOwl.Opacity = 1.0;
                    badgeNightOwl.Background = Brushes.MediumPurple;
                }
            }
        }
        private void LoadDiscovery()
        {
            using (var db = new LibraryDbContext())
            {
                // Önceki hatalı satır yerine bunu yapıştır:
                
                var allBooks = db.Books.Include(b => b.Author).Include(b => b.Genre).ToList();
                if (allBooks.Count == 0) return;

                // 1. Günün Kitabını Rastgele Seç
                Random rnd = new Random();
                var todayBook = allBooks[rnd.Next(allBooks.Count)];
                if (!string.IsNullOrEmpty(todayBook.ImageUrl) && todayBook.ImageUrl.StartsWith("http"))
                {
                    imgDiscoveryToday.Source = new BitmapImage(new Uri(todayBook.ImageUrl));
                }
                else
                {
                    // Resim yoksa veya hatalıysa varsayılan bir görsel yükle
                    imgDiscoveryToday.Source = new BitmapImage(new Uri("https://via.placeholder.com/300x450?text=Resim+Yok"));
                }

                imgDiscoveryToday.Source = new BitmapImage(new Uri(todayBook.ImageUrl ?? ""));
                txtDiscoveryTitle.Text = todayBook.Title;
                txtDiscoveryAuthor.Text = todayBook.Author?.Name;
                // Günün kitabı butonu için tag'e ID gömüyoruz
                txtDiscoveryTitle.Tag = todayBook.Id;

                // 2. AI Önerileri (Rastgele 4 kitap)
                DiscoverySuggestionsList.ItemsSource = allBooks.OrderBy(x => rnd.Next()).Take(4).ToList();
            }
        }

        private void DiscoveryReview_Click(object sender, RoutedEventArgs e)
        {
            if (txtDiscoveryTitle.Tag is int bookId)
            {
                using (var db = new LibraryDbContext())
                {
                    // Veritabanından ilişkileriyle (Author, Genre) kitabı bul
                    var book = db.Books
                        .Include(b => b.Author)
                        .Include(b => b.Genre)
                        .FirstOrDefault(b => b.Id == bookId);

                    if (book != null)
                    {
     
                        SwitchPanel("Book"); // Önce paneli aç

                        // Detay verilerini doldur
                        imgDetay.Source = new BitmapImage(new Uri(book.ImageUrl ?? ""));
                        txtDetayBaslik.Text = book.Title;
                        txtDetayYazar.Text = book.Author?.Name;
                        txtDetayTur.Text = "Kategori: " + (book.Genre?.Name ?? "Türsüz");
                        txtDetayRaf.Text = "Konum: " + book.ShelfInfo;
                        txtDetayAI.Text = book.AISummary;

                        UpdateBorrowButtonState(book); // Bu logic'i bir metoda toplamanı öneririm

                        _ = GetRealAIAnalysis(book.Title, book.Author?.Name);
                   }
                }
            }
        }

        private void UpdateBorrowButtonState(Book book)
        {
            // Kullanıcı kontrolü
            bool bendeMi = book.IsBorrowed &&
                           string.Equals(book.BorrowedBy?.Trim(), CurrentUser?.Trim(), StringComparison.OrdinalIgnoreCase);

            if (!book.IsBorrowed)
            {
                brdStatus.Background = Brushes.Green;
                txtStatus.Text = "RAFTA";
                btnBorrowAction.Content = "Ödünç Al";
                btnBorrowAction.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF00E5FF"));
            }
            else if (bendeMi)
            {
                brdStatus.Background = Brushes.Orange;
                txtStatus.Text = "ÜZERİNİZDE";
                btnBorrowAction.Content = "İade Et";
                btnBorrowAction.Background = Brushes.Orange;
            }
            else
            {
                brdStatus.Background = Brushes.Red;
                txtStatus.Text = $"ÖDÜNÇTE ({book.BorrowedBy})";
                btnBorrowAction.Content = "Mevcut Değil";
                btnBorrowAction.IsEnabled = false;
            }
        }

        private void GenreFilter_Click(object sender, MouseButtonEventArgs e)
        {
            GenreCard_Click(sender, new RoutedEventArgs());
        }
        private void GenreCard_Click(object sender, RoutedEventArgs e)
        {
            string selectedGenreName = "";

            // 1. Tür adını farklı kaynaklardan hatasız çekme
            if (sender is Button btn)
                selectedGenreName = btn.Content.ToString();
            else if (sender is TextBlock tb)
            {
                // "Kategori: Roman" yazıyorsa sadece "Roman" kısmını alalım
                selectedGenreName = tb.Text.Replace("Kategori: ", "").Trim();
            }
            else if (sender is FrameworkElement fe && fe.DataContext is Book b)
            {
                selectedGenreName = b.Genre?.Name;
            }

            if (string.IsNullOrEmpty(selectedGenreName)) return;

            // 2. Panelleri ayarla
            DashPanel.Visibility = Visibility.Collapsed;
            BookDetailPanel.Visibility = Visibility.Collapsed;
            AuthorDetailPanel.Visibility = Visibility.Collapsed;
            GenreDetailPanel.Visibility = Visibility.Visible;

            txtGenreDetailName.Text = selectedGenreName;

            // 3. Veritabanı sorgusu (Kritik: Include eklemeyi unutma)
            using (var db = new LibraryDbContext())
            {
                var booksInGenre = db.Books
                    .Include(b => b.Author) // Yazar ismi görünmesi için
                    .Include(b => b.Genre)  // Kategori ismi görünmesi için
                    .Where(b => b.Genre.Name.ToLower() == selectedGenreName.ToLower())
                    .ToList();

                GenreBooksList.ItemsSource = booksInGenre;
            }
        }
        // 1. Yazarları ve Türleri ComboBox'a Yükleme (LoadData içine ekle)
        private void LoadAdminResources()
        {
            using (var db = new LibraryDbContext())
            {
                dgAdminBooks.ItemsSource = db.Books.Include(b => b.Author).ToList();
                cbAddAuthor.ItemsSource = db.Authors.ToList();
                cbAddGenre.ItemsSource = db.Genres.ToList();
                dgUsers.ItemsSource = db.Users.ToList();
                dgBorrowLogs.ItemsSource = db.Books.Where(b => b.BorrowDate != null)
                    .Select(b => new { b.Title, b.BorrowedBy, b.BorrowDate, Status = b.IsBorrowed ? "Ödünçte" : "İade Edildi" }).ToList();
            }
        }

        private void AdminBanUser_Click(object sender, RoutedEventArgs e)
        {
            if (dgUsers.SelectedItem is User selectedUser)
            {
                using (var db = new LibraryDbContext())
                {
                    var user = db.Users.FirstOrDefault(u => u.Id == selectedUser.Id);
                    if (user != null)
                    {
                        user.IsBanned = true;
                        user.Status = "BANLANDI";
                        db.SaveChanges();
                        MessageBox.Show($"{user.Username} sistemden banlandı!");
                        LoadAdminResources(); // Tabloyu tazele
                    }
                }
            }
        }
        // 1. Hızlı Yazar Ekleme Metodu


        private void AdminDeleteBook_Click(object sender, RoutedEventArgs e)
        {
            // 1. Tablodan seçili olan kitabı al
            var selectedBook = dgAdminBooks.SelectedItem as Book;

            if (selectedBook == null)
            {
                MessageBox.Show("Lütfen önce silmek istediğiniz kitabı tablodan seçin!", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 2. Kullanıcıya onay sor (Yanlışlıkla silmeyi önlemek için)
            var result = MessageBox.Show($"'{selectedBook.Title}' kitabını sistemden tamamen çıkarmak istediğinize emin misiniz?",
                                         "Kitap Silme Onayı",
                                         MessageBoxButton.YesNo,
                                         MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                using (var db = new LibraryDbContext())
                {
                    // 3. Veritabanından kitabı bul ve sil
                    var bookInDb = db.Books.FirstOrDefault(b => b.Id == selectedBook.Id);
                    if (bookInDb != null)
                    {
                        db.Books.Remove(bookInDb);
                        db.SaveChanges();

                        MessageBox.Show("Kitap başarıyla silindi.", "Başarılı");

                        // 4. Tabloyu ve istatistikleri yenile
                        LoadAdminResources();
                        RefreshAdminStats();
                    }
                }
            }
        }


        private void Borrow_Click(object sender, RoutedEventArgs e)
        {
            using (var db = new LibraryDbContext())
            {
                var currentTitle = txtDetayBaslik.Text;
                // Kitabı veritabanından en güncel haliyle çekiyoruz
                var book = db.Books.FirstOrDefault(b => b.Title == currentTitle);

                if (book != null)
                {
                    // Eşleşme hatasını önlemek için isimleri temizleyip (Trim) küçük harfe (ToLower) çeviriyoruz
                    bool bendeMi = book.IsBorrowed &&
                                   string.Equals(book.BorrowedBy?.Trim(), CurrentUser?.Trim(), StringComparison.OrdinalIgnoreCase);

                    if (!book.IsBorrowed)
                    {
                        // ÖDÜNÇ ALMA
                        book.IsBorrowed = true;
                        book.BorrowedBy = CurrentUser.Trim(); // İsmi temiz kaydediyoruz
                        book.BorrowDate = DateTime.Now;
                        book.WasBorrowedBefore = true; // Rozetler için önemli
                        db.SaveChanges();
                        MessageBox.Show($"'{book.Title}' başarıyla ödünç alındı.", "Bilgi");
                    }
                    else if (bendeMi)
                    {
                        // İADE ETME
                        book.IsBorrowed = false;
                        book.BorrowedBy = null;
                        db.SaveChanges();
                        MessageBox.Show("Kitap iade edildi.", "Bilgi");
                    }
                    else
                    {
                        MessageBox.Show("Bu kitap zaten başkasında!");
                        return;
                    }

                    // --- KRİTİK NOKTA: ARAYÜZÜ YENİLE ---
                    LoadData(); // Ana sayfadaki listeleri tazeler
                    UpdateBorrowButtonState(book); // Detay sayfasındaki butonu ve yazıları günceller

                    // Eğer MyBooks sayfasındaysak orayı da tazele
                    if (MyBooksPanel.Visibility == Visibility.Visible)
                    {
                        var all = db.Books.Include(b => b.Author).Include(b => b.Genre).ToList();
                        CurrentBorrowedList.ItemsSource = all.Where(b => b.BorrowedBy == CurrentUser && b.IsBorrowed).ToList();
                    }
                }
            }
        }

        private void Favorite_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.CommandParameter is Book b)
            {
                using (var db = new LibraryDbContext())
                {
                    var dbBook = db.Books.Find(b.Id);
                    if (dbBook != null)
                    {
                        dbBook.IsFavorite = !dbBook.IsFavorite;
                        db.SaveChanges();
                        LoadData();
                    }
                }
            }
           
        }

        private async System.Threading.Tasks.Task GetRealAIAnalysis(string title, string author)
        {
            txtDetayAI.Text = "AI Analizi Yapılıyor... 🚀  ";
            try
            {
                string apiKey = " AIzaSyD55hli0Z63Ul9UKnZ2JEOwxevDCujv8jA";
                string url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent?key={apiKey}";
                using var client = new System.Net.Http.HttpClient();
                var req = new { contents = new[] { new { parts = new[] { new { text = $"{author} - {title} kitabını fütüristik özetle." } } } } };
                var res = await client.PostAsync(url, new System.Net.Http.StringContent(JsonConvert.SerializeObject(req), System.Text.Encoding.UTF8, "application/json"));
                var data = JsonConvert.DeserializeObject<dynamic>(await res.Content.ReadAsStringAsync());
                txtDetayAI.Text = data.candidates[0].content.parts[0].text;
            }
            catch { txtDetayAI.Text = "Analiz yüklenemedi."; }
        }

        private void BackToDash_Click(object sender, RoutedEventArgs e)
        {
            SwitchPanel("Dash");
        }
        private void BackToBook_Click(object sender, RoutedEventArgs e) { AuthorDetailPanel.Visibility = Visibility.Collapsed; BookDetailPanel.Visibility = Visibility.Visible; }
        private void Logout_Click(object sender, RoutedEventArgs e) { new LoginView().Show(); this.Close(); }
        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string search = SearchBox.Text.ToLower();
            using (var db = new LibraryDbContext())
            {
                var filtered = db.Books.Include(b => b.Author).Include(b => b.Genre)
                    .Where(b => b.Title.ToLower().Contains(search) || b.Author.Name.ToLower().Contains(search))
                    .ToList();
                BooksList.ItemsSource = filtered;
            }
        }
       
    }

    public class StatusColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            bool isBorrowed = (bool)value;
            return isBorrowed ? Brushes.Red : Brushes.Green;
        }
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) => throw new NotImplementedException();
    }

    public class FavoriteIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            bool isFavorite = (bool)value;
            return isFavorite ? "❤️" : "🤍";
        }
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) => throw new NotImplementedException();
    }
}