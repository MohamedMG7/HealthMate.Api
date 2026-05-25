using HealthMate.Infrastructure.Enums;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace HealthMate.Infrastructure.DTO.AccountDto
{
	public class AccountRegisterDto
	{
        [Required]
        public string First_Name { get; set; }
        [Required]
        public string Last_Name { get; set; }

		[Required(ErrorMessage = "Email is required.")]
		[EmailAddress(ErrorMessage = "Invalid email format.")]
		public string Email { get; set; }

        [Required(ErrorMessage = "Password Is Required")]
        [DataType(DataType.Password)]
        [RegularExpression(@"^(?=.*[A-Za-z])(?=.*\d)(?=.*[@$!%*#?&])[A-Za-z\d@$!%*#?&]{8,}$",ErrorMessage = "Password Must consist of 8 character at least and Have At Least one letter, one number and one special Chracter")]
        public string Password { get; set; }
        [Required]
		[DataType(DataType.Password)]
        [Compare("Password", ErrorMessage ="The Password And Confiramtion Password does not match.")]
		public string PasswordConfirmed { get; set; }

        public UserType UserType { get; set; }
        [DataType(DataType.Upload)]
        [FromForm]
        public IFormFile Image { get; set; }
        public string PhoneNumber { get; set; }

    }
}
