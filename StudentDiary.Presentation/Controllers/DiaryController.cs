using Microsoft.AspNetCore.Mvc;
using StudentDiary.Presentation.Attributes;
using StudentDiary.Services.DTOs;
using StudentDiary.Services.Interfaces;

namespace StudentDiary.Presentation.Controllers
{
    [SessionAuthentication]
    public class DiaryController : Controller
    {
        private readonly IDiaryService _diaryService;

        public DiaryController(IDiaryService diaryService)
        {
            _diaryService = diaryService;
        }

        private int GetCurrentUserId()
        {
            return HttpContext.Session.GetInt32("UserId") ?? 0;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = GetCurrentUserId();
            var entries = await _diaryService.GetUserEntriesAsync(userId);
            return View(entries);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateDiaryEntryDto createDto)
        {
            if (!ModelState.IsValid)
            {
                return View(createDto);
            }

            var userId = GetCurrentUserId();
            var result = await _diaryService.CreateEntryAsync(userId, createDto);

            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
                return RedirectToAction("Index");
            }

            ModelState.AddModelError(string.Empty, result.Message);
            return View(createDto);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var userId = GetCurrentUserId();
            var entry = await _diaryService.GetEntryByIdAsync(id, userId);

            if (entry == null)
            {
                TempData["ErrorMessage"] = "Diary entry not found or you don't have permission to view it.";
                return RedirectToAction("Index");
            }

            return View(entry);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var userId = GetCurrentUserId();
            var entry = await _diaryService.GetEntryByIdAsync(id, userId);

            if (entry == null)
            {
                TempData["ErrorMessage"] = "Diary entry not found or you don't have permission to edit it.";
                return RedirectToAction("Index");
            }

            var updateDto = new UpdateDiaryEntryDto
            {
                Id = entry.Id,
                Title = entry.Title,
                Content = entry.Content
            };

            return View(updateDto);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(UpdateDiaryEntryDto updateDto)
        {
            if (!ModelState.IsValid)
            {
                return View(updateDto);
            }

            var userId = GetCurrentUserId();
            var result = await _diaryService.UpdateEntryAsync(userId, updateDto);

            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
                return RedirectToAction("Details", new { id = updateDto.Id });
            }

            ModelState.AddModelError(string.Empty, result.Message);
            return View(updateDto);
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = GetCurrentUserId();
            var entry = await _diaryService.GetEntryByIdAsync(id, userId);

            if (entry == null)
            {
                TempData["ErrorMessage"] = "Diary entry not found or you don't have permission to delete it.";
                return RedirectToAction("Index");
            }

            return View(entry);
        }

        [HttpPost]
        [ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var userId = GetCurrentUserId();
            var result = await _diaryService.DeleteEntryAsync(id, userId);

            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
            }
            else
            {
                TempData["ErrorMessage"] = result.Message;
            }

            return RedirectToAction("Index");
        }
    }
}
