using HealthMate.Application.Abstractions.Enums;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace HealthMate.Infrastructure.Data.Models
{
	public class ApplicationUser : IdentityUser
	{
		// Id, Email, Email Confirmed, PasswordHash, PhoneNumber added by Identity
		public string First_Name { get; set; } = null!;
		public string Last_Name { get; set; } = null!;
		public UserType UserType { get; set; }
        public bool IsActive { get; set; }
        public string? ImageUrl { get; set; }

		// not mapped by EF
		public string FullName => $"{First_Name} {Last_Name}";

        public ICollection<UserFeedback> UserFeedbacks { get; set; } = new HashSet<UserFeedback>(); // application user can add many feedbacks
        public ICollection<UserDiseaseExperience> UserDiseaseExperiences { get; set; } = new HashSet<UserDiseaseExperience>(); // application user can add many disease experinces one on every disease
    }
}
