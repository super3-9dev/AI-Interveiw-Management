using Microsoft.EntityFrameworkCore;
using InterviewBot.Data;
using InterviewBot.Models;
using Microsoft.AspNetCore.Http;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

namespace InterviewBot.Services
{
    public interface IProfileService
    {
        Task<Profile> UploadAndAnalyzeResumeAsync(IFormFile file, int userId);
        Task<Profile> CreateProfileFromDescriptionAsync(string briefIntroduction, string careerGoals, string currentActivity, string motivations, int userId);
        Task<Profile?> GetProfileAsync(int id, int userId);
        Task<IEnumerable<Profile>> GetUserProfilesAsync(int userId);
        Task<bool> DeleteProfileAsync(int id, int userId);
        Task<bool> RetryAnalysisAsync(int analysisId, int userId);
        Task<string> GetAnalysisStatusAsync(int analysisId, int userId);
        Task<int> GetAnalysisProgressAsync(int analysisId, int userId);
        Task<bool> UpdateProfileAsync(Profile profile);
        Task<Profile> CreateProfileAsync(Profile profile);
        Task<User?> GetUserAsync(int userId);
        Task<bool> UpdateUserAsync(User user, string? newPassword = null);
        Task<bool> HasCompletedProfileAsync(int userId);
        Task<(bool Success, string? ApiResponse, string? ErrorMessage)> CallResumeAnalysisAPIAsync(IFormFile file);
        Task<Profile> CreateProfileFromApiResponseAsync(string apiResponse, int userId, bool isFallback = false);
        Task<Profile> UpdateProfileFromApiResponseAsync(Profile existingProfile, string apiResponse, bool isFallback = false);
    }
}
