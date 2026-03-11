using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LibraryAutomation.Models
{
    public class User
    {
        [Key] public int Id { get; set; }
        public string Username { get; set; }
       public string Status { get; set; } = "Aktif"; // Aktif veya Banlı
        public bool IsBanned { get; set; } = false;

    }

    [Table("Authors")]
    public class Author
    {
        [Key] public int Id { get; set; }
        public string Name { get; set; }
        public string Biography { get; set; }
        public virtual ICollection<Book> Books { get; set; }
    }

    public class Genre
    {
        [Key] public int Id { get; set; }
        public string Name { get; set; }
    }

    public class Book
    {

        [Key] public int Id { get; set; }
        public bool IsFavorite { get; set; }
        public bool IsExtended { get; set; } = false;
        public bool WasBorrowedBefore { get; set; }
        public DateTime? BorrowDate { get; set; } 
        public string ShelfInfo { get; set; } 
        public string AISummary { get; set; }
        public string Title { get; set; }
        public int AuthorId { get; set; }
        public virtual Author Author { get; set; }
        public int GenreId { get; set; }
        public virtual Genre Genre { get; set; }
        public string Description { get; set; }
        public bool IsAvailable { get; set; } = true;
        public string ImageUrl { get; set; }
        public bool IsBorrowed { get; internal set; }
        public string BorrowedBy { get; internal set; }
    }
    public class BorrowRecord
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int BookId { get; set; }
        public DateTime BorrowDate { get; set; }
        public DateTime ReturnDate { get; set; } 
        public bool IsReturned { get; set; }
    }
}