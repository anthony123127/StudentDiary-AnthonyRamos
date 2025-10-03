using Microsoft.AspNetCore.Mvc;
using StudentDiary.Presentation.Attributes;
using StudentDiary.Services.DTOs;
using StudentDiary.Services.Interfaces;

namespace StudentDiary.Presentation.Controllers
{
    [SessionAuthentication]
    public class ProfileController : Controller
    {
        private readonly IAuthService _authService;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public ProfileController(IAuthService authService, IWebHostEnvironment webHostEnvironment)
        {
            _authService = authService;
            _webHostEnvironment = webHostEnvironment;
        }

        private int GetCurrentUserId()
        {
            return HttpContext.Session.GetInt32("UserId") ?? 0;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = GetCurrentUserId();
            var userProfile = await _authService.GetUserProfileAsync(userId);

            if (userProfile == null)
            {
                TempData["ErrorMessage"] = "User profile not found.";
                return RedirectToAction("Index", "Diary");
            }

            return View(userProfile);
        }

        [HttpGet]
        public async Task<IActionResult> Edit()
        {
            var userId = GetCurrentUserId();
            var userProfile = await _authService.GetUserProfileAsync(userId);

            if (userProfile == null)
            {
                TempData["ErrorMessage"] = "User profile not found.";
                return RedirectToAction("Index", "Diary");
            }

            var updateDto = new UpdateProfileDto
            {
                FirstName = userProfile.FirstName,
                LastName = userProfile.LastName,
                Email = userProfile.Email
            };

            return View(updateDto);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(UpdateProfileDto updateProfileDto)
        {
            if (!ModelState.IsValid)
            {
                return View(updateProfileDto);
            }

            var userId = GetCurrentUserId();
            var result = await _authService.UpdateProfileAsync(userId, updateProfileDto);

            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
                return RedirectToAction("Index");
            }

            ModelState.AddModelError(string.Empty, result.Message);
            return View(updateProfileDto);
        }

        [HttpPost]
        public async Task<IActionResult> UploadProfilePicture(IFormFile profilePicture)
        {
            if (profilePicture == null || profilePicture.Length == 0)
            {
                TempData["ErrorMessage"] = "Please select a valid image file.";
                return RedirectToAction("Index");
            }

            // Validate file type
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            var fileExtension = Path.GetExtension(profilePicture.FileName).ToLowerInvariant();
            
            if (!allowedExtensions.Contains(fileExtension))
            {
                TempData["ErrorMessage"] = "Only image files (.jpg, .jpeg, .png, .gif) are allowed.";
                return RedirectToAction("Index");
            }

            // Validate file size (max 5MB)
            if (profilePicture.Length > 5 * 1024 * 1024)
            {
                TempData["ErrorMessage"] = "File size must be less than 5MB.";
                return RedirectToAction("Index");
            }

            try
            {
                var userId = GetCurrentUserId();
                var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "profile-pictures");
                
                // Create directory if it doesn't exist
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                // Generate unique filename
                var fileName = $"{userId}_{Guid.NewGuid()}{fileExtension}";
                var filePath = Path.Combine(uploadsFolder, fileName);

                // Save file
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await profilePicture.CopyToAsync(fileStream);
                }

                // Update database with relative path
                var relativePath = $"/uploads/profile-pictures/{fileName}";
                var result = await _authService.UpdateProfilePictureAsync(userId, relativePath);

                if (result.Success)
                {
                    TempData["SuccessMessage"] = result.Message;
                }
                else
                {
                    // Clean up file if database update failed
                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }
                    TempData["ErrorMessage"] = result.Message;
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "An error occurred while uploading the profile picture.";
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> RemoveProfilePicture()
        {
            var userId = GetCurrentUserId();
            var userProfile = await _authService.GetUserProfileAsync(userId);

            if (!string.IsNullOrEmpty(userProfile?.ProfilePicturePath))
            {
                // Remove file from filesystem
                var fullPath = Path.Combine(_webHostEnvironment.WebRootPath, userProfile.ProfilePicturePath.TrimStart('/'));
                if (System.IO.File.Exists(fullPath))
                {
                    System.IO.File.Delete(fullPath);
                }

                // Update database
                var result = await _authService.UpdateProfilePictureAsync(userId, null);
                
                if (result.Success)
                {
                    TempData["SuccessMessage"] = "Profile picture removed successfully.";
                }
                else
                {
                    TempData["ErrorMessage"] = result.Message;
                }
            }

            return RedirectToAction("Index");
        }
    }
}
