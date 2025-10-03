using Microsoft.EntityFrameworkCore;
using StudentDiary.Infrastructure.Data;
using StudentDiary.Infrastructure.Models;
using StudentDiary.Services.DTOs;
using StudentDiary.Services.Interfaces;

namespace StudentDiary.Services.Services
{
    public class DiaryService : IDiaryService
    {
        private readonly StudentDiaryContext _context;

        public DiaryService(StudentDiaryContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<DiaryEntryDto>> GetUserEntriesAsync(int userId)
        {
            var entries = await _context.DiaryEntries
                .Where(d => d.UserId == userId)
                .OrderByDescending(d => d.CreatedDate)
                .Select(d => new DiaryEntryDto
                {
                    Id = d.Id,
                    Title = d.Title,
                    Content = d.Content,
                    CreatedDate = d.CreatedDate,
                    LastModifiedDate = d.LastModifiedDate,
                    UserId = d.UserId
                })
                .ToListAsync();

            return entries;
        }

        public async Task<DiaryEntryDto> GetEntryByIdAsync(int entryId, int userId)
        {
            var entry = await _context.DiaryEntries
                .Where(d => d.Id == entryId && d.UserId == userId)
                .Select(d => new DiaryEntryDto
                {
                    Id = d.Id,
                    Title = d.Title,
                    Content = d.Content,
                    CreatedDate = d.CreatedDate,
                    LastModifiedDate = d.LastModifiedDate,
                    UserId = d.UserId
                })
                .FirstOrDefaultAsync();

            return entry;
        }

        public async Task<(bool Success, string Message, DiaryEntryDto Entry)> CreateEntryAsync(int userId, CreateDiaryEntryDto createDto)
        {
            // Validate that user exists
            var userExists = await _context.Users.AnyAsync(u => u.Id == userId);
            if (!userExists)
            {
                return (false, "User not found.", null);
            }

            var diaryEntry = new DiaryEntry
            {
                Title = createDto.Title,
                Content = createDto.Content,
                UserId = userId,
                CreatedDate = DateTime.UtcNow,
                LastModifiedDate = DateTime.UtcNow
            };

            _context.DiaryEntries.Add(diaryEntry);
            await _context.SaveChangesAsync();

            var entryDto = new DiaryEntryDto
            {
                Id = diaryEntry.Id,
                Title = diaryEntry.Title,
                Content = diaryEntry.Content,
                CreatedDate = diaryEntry.CreatedDate,
                LastModifiedDate = diaryEntry.LastModifiedDate,
                UserId = diaryEntry.UserId
            };

            return (true, "Diary entry created successfully.", entryDto);
        }

        public async Task<(bool Success, string Message, DiaryEntryDto Entry)> UpdateEntryAsync(int userId, UpdateDiaryEntryDto updateDto)
        {
            var entry = await _context.DiaryEntries
                .FirstOrDefaultAsync(d => d.Id == updateDto.Id && d.UserId == userId);

            if (entry == null)
            {
                return (false, "Diary entry not found or you don't have permission to edit it.", null);
            }

            entry.Title = updateDto.Title;
            entry.Content = updateDto.Content;
            entry.LastModifiedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var entryDto = new DiaryEntryDto
            {
                Id = entry.Id,
                Title = entry.Title,
                Content = entry.Content,
                CreatedDate = entry.CreatedDate,
                LastModifiedDate = entry.LastModifiedDate,
                UserId = entry.UserId
            };

            return (true, "Diary entry updated successfully.", entryDto);
        }

        public async Task<(bool Success, string Message)> DeleteEntryAsync(int entryId, int userId)
        {
            var entry = await _context.DiaryEntries
                .FirstOrDefaultAsync(d => d.Id == entryId && d.UserId == userId);

            if (entry == null)
            {
                return (false, "Diary entry not found or you don't have permission to delete it.");
            }

            _context.DiaryEntries.Remove(entry);
            await _context.SaveChangesAsync();

            return (true, "Diary entry deleted successfully.");
        }
    }
}
