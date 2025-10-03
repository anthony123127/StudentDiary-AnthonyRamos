using Microsoft.EntityFrameworkCore;
using StudentDiary.Infrastructure.Data;
using StudentDiary.Infrastructure.Models;
using StudentDiary.Services.DTOs;
using StudentDiary.Services.Interfaces;
using System.Security.Cryptography;
using System.Text;

namespace StudentDiary.Services.Services
{
    public class AuthService : IAuthService
    {
        private readonly StudentDiaryContext _context;

        public AuthService(StudentDiaryContext context)
        {
            _context = context;
        }

        public async Task<(bool Success, string Message)> RegisterAsync(RegisterDto registerDto)
        {
            // Check if username already exists
            if (await _context.Users.AnyAsync(u => u.Username == registerDto.Username))
            {
                return (false, "Username already exists.");
            }

            // Check if email already exists
            if (await _context.Users.AnyAsync(u => u.Email == registerDto.Email))
            {
                return (false, "Email already exists.");
            }

            var user = new User
            {
                Username = registerDto.Username,
                Email = registerDto.Email,
                PasswordHash = HashPassword(registerDto.Password),
                FirstName = registerDto.FirstName,
                LastName = registerDto.LastName,
                DateCreated = DateTime.UtcNow,
                LastLoginDate = DateTime.UtcNow,
                FailedLoginAttempts = 0
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return (true, "Registration successful.");
        }

        public async Task<(bool Success, string Message, UserProfileDto User)> LoginAsync(LoginDto loginDto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == loginDto.Username);

            if (user == null)
            {
                return (false, "Invalid username or password.", null);
            }

            // Check if account is locked
            if (user.LockoutEnd.HasValue && user.LockoutEnd > DateTime.UtcNow)
            {
                return (false, $"Account is locked until {user.LockoutEnd.Value:yyyy-MM-dd HH:mm:ss}.", null);
            }

            if (!VerifyPassword(loginDto.Password, user.PasswordHash))
            {
                // Increment failed attempts
                user.FailedLoginAttempts++;

                // Lock account after 3 failed attempts
                if (user.FailedLoginAttempts >= 3)
                {
                    user.LockoutEnd = DateTime.UtcNow.AddMinutes(15); // Lock for 15 minutes
                    await _context.SaveChangesAsync();
                    return (false, "Account locked due to too many failed login attempts. Try again in 15 minutes.", null);
                }

                await _context.SaveChangesAsync();
                return (false, "Invalid username or password.", null);
            }

            // Reset failed attempts and lockout on successful login
            user.FailedLoginAttempts = 0;
            user.LockoutEnd = null;
            user.LastLoginDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var userProfile = new UserProfileDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                ProfilePicturePath = user.ProfilePicturePath,
                DateCreated = user.DateCreated
            };

            return (true, "Login successful.", userProfile);
        }

        public async Task<(bool Success, string Message)> ForgotPasswordAsync(ForgotPasswordDto forgotPasswordDto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == forgotPasswordDto.Email);

            if (user == null)
            {
                // Don't reveal if email exists for security reasons
                return (true, "If the email exists, a password reset link has been sent.");
            }

            // Generate password reset token
            user.PasswordResetToken = GenerateRandomToken();
            user.PasswordResetTokenExpiry = DateTime.UtcNow.AddHours(1); // Token expires in 1 hour

            await _context.SaveChangesAsync();

            // In a real application, you would send an email here
            // For this demo, we'll just return success
            return (true, "If the email exists, a password reset link has been sent.");
        }

        public async Task<(bool Success, string Message)> ResetPasswordAsync(ResetPasswordDto resetPasswordDto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => 
                u.PasswordResetToken == resetPasswordDto.Token && 
                u.PasswordResetTokenExpiry > DateTime.UtcNow);

            if (user == null)
            {
                return (false, "Invalid or expired reset token.");
            }

            user.PasswordHash = HashPassword(resetPasswordDto.NewPassword);
            user.PasswordResetToken = null;
            user.PasswordResetTokenExpiry = null;
            user.FailedLoginAttempts = 0; // Reset failed attempts
            user.LockoutEnd = null; // Remove any lockout

            await _context.SaveChangesAsync();

            return (true, "Password reset successful.");
        }

        public async Task<UserProfileDto> GetUserProfileAsync(int userId)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return null;

            return new UserProfileDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                ProfilePicturePath = user.ProfilePicturePath,
                DateCreated = user.DateCreated
            };
        }

        public async Task<(bool Success, string Message)> UpdateProfileAsync(int userId, UpdateProfileDto updateProfileDto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return (false, "User not found.");
            }

            // Check if email is already taken by another user
            if (!string.IsNullOrEmpty(updateProfileDto.Email) && updateProfileDto.Email != user.Email)
            {
                if (await _context.Users.AnyAsync(u => u.Email == updateProfileDto.Email && u.Id != userId))
                {
                    return (false, "Email is already in use by another account.");
                }
                user.Email = updateProfileDto.Email;
            }

            if (!string.IsNullOrEmpty(updateProfileDto.FirstName))
                user.FirstName = updateProfileDto.FirstName;

            if (!string.IsNullOrEmpty(updateProfileDto.LastName))
                user.LastName = updateProfileDto.LastName;

            await _context.SaveChangesAsync();

            return (true, "Profile updated successfully.");
        }

        public async Task<(bool Success, string Message)> UpdateProfilePictureAsync(int userId, string imagePath)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return (false, "User not found.");
            }

            user.ProfilePicturePath = imagePath;
            await _context.SaveChangesAsync();

            return (true, "Profile picture updated successfully.");
        }

        public string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                // Add salt for better security
                var saltedPassword = password + "StudentDiary_Salt_2024";
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(saltedPassword));
                return Convert.ToBase64String(hashedBytes);
            }
        }

        public bool VerifyPassword(string password, string hash)
        {
            var hashToCompare = HashPassword(password);
            return hashToCompare == hash;
        }

        private string GenerateRandomToken()
        {
            using (var rng = RandomNumberGenerator.Create())
            {
                var tokenBytes = new byte[32];
                rng.GetBytes(tokenBytes);
                return Convert.ToBase64String(tokenBytes);
            }
        }
    }
}
