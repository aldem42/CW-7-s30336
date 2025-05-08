using System.ComponentModel.DataAnnotations;

namespace TravelAgency.Models.DTOs
{
    public class ClientDTO
    {
        [Required]
        [MaxLength(120)]
        public string FirstName { get; set; }

        [Required]
        [MaxLength(120)]
        public string LastName { get; set; }

        [Required]
        [EmailAddress]
        [MaxLength(120)]
        public string Email { get; set; }

        [Required]
        [MaxLength(120)]
        public string Telephone { get; set; }

        [Required]
        [MaxLength(120)]
        public string Pesel { get; set; }
    }
}