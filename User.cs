using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization; // 🔥 NOVO - para ignore JSON

namespace MinhaApiCrud
{
    public class User
    {
        public int Id { get; set; }
        
        [Required(ErrorMessage = "O nome é obrigatório")] // 🔥 MELHORADO
        [StringLength(100, ErrorMessage = "O nome deve ter no máximo 100 caracteres")] // 🔥 MELHORADO
        public string Name { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "O email é obrigatório")] // 🔥 MELHORADO
        [EmailAddress(ErrorMessage = "Formato de email inválido")] // 🔥 MELHORADO
        public string Email { get; set; } = string.Empty;
        
        [Range(0, 150, ErrorMessage = "A idade deve estar entre 0 e 150 anos")] // 🔥 MELHORADO
        public int Age { get; set; }
        
        public DateTime CreatedAt { get; set; }
        
        // 🔥🔥🔥 PROPRIEDADES NOVAS (OPCIONAIS - adicione se quiser)
        
        [StringLength(20, ErrorMessage = "O telefone deve ter no máximo 20 caracteres")]
        public string? Phone { get; set; } // 🔥 NOVO - campo opcional
        
        [JsonIgnore] // 🔥 NOVO - não aparece no JSON de resposta
        public bool IsActive { get; set; } = true; // 🔥 NOVO - para soft delete
        
        public DateTime? UpdatedAt { get; set; } // 🔥 NOVO - timestamp de atualização
        
        // 🔥🔥🔥 MÉTODOS ÚTEIS
        public void UpdateTimestamp()
        {
            UpdatedAt = DateTime.UtcNow;
        }
        
        public void Deactivate()
        {
            IsActive = false;
            UpdateTimestamp();
        }
        
        public void Activate()
        {
            IsActive = true;
            UpdateTimestamp();
        }
        
        // 🔥🔥🔥 MÉTODO DE VALIDAÇÃO PERSONALIZADO
        public bool IsValid()
        {
            return !string.IsNullOrEmpty(Name) && 
                   !string.IsNullOrEmpty(Email) && 
                   new EmailAddressAttribute().IsValid(Email) &&
                   Age >= 0 && Age <= 150;
        }
        
        // 🔥🔥🔥 OVERRIDE DO ToString() PARA DEBUG
        public override string ToString()
        {
            return $"User [Id={Id}, Name={Name}, Email={Email}, Age={Age}, CreatedAt={CreatedAt:yyyy-MM-dd}]";
        }
    }
    
    // 🔥🔥🔥 CLASSE DE DTO PARA CRIAÇÃO (OPCIONAL - para separar concerns)
    public class CreateUserRequest
    {
        [Required(ErrorMessage = "O nome é obrigatório")]
        [StringLength(100, ErrorMessage = "O nome deve ter no máximo 100 caracteres")]
        public string Name { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "O email é obrigatório")]
        [EmailAddress(ErrorMessage = "Formato de email inválido")]
        public string Email { get; set; } = string.Empty;
        
        [Range(0, 150, ErrorMessage = "A idade deve estar entre 0 e 150 anos")]
        public int Age { get; set; }
        
        [StringLength(20, ErrorMessage = "O telefone deve ter no máximo 20 caracteres")]
        public string? Phone { get; set; }
    }
    
    // 🔥🔥🔥 CLASSE DE DTO PARA ATUALIZAÇÃO (OPCIONAL)
    public class UpdateUserRequest
    {
        [StringLength(100, ErrorMessage = "O nome deve ter no máximo 100 caracteres")]
        public string? Name { get; set; }
        
        [EmailAddress(ErrorMessage = "Formato de email inválido")]
        public string? Email { get; set; }
        
        [Range(0, 150, ErrorMessage = "A idade deve estar entre 0 e 150 anos")]
        public int? Age { get; set; }
        
        [StringLength(20, ErrorMessage = "O telefone deve ter no máximo 20 caracteres")]
        public string? Phone { get; set; }
    }
    
    // 🔥🔥🔥 CLASSE DE RESPOSTA PADRONIZADA (OPCIONAL)
    public class UserResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int Age { get; set; }
        public string? Phone { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        
        public static UserResponse FromUser(User user)
        {
            return new UserResponse
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                Age = user.Age,
                Phone = user.Phone,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt
            };
        }
    }
}