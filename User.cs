using System.ComponentModel.DataAnnotations;

namespace MinhaApiCrud
{
    public class User
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;
        
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
        
        public int Age { get; set; }
        
        public DateTime CreatedAt { get; set; }
    }
}