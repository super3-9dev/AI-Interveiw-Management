using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using InterviewBot.Services;
using InterviewBot.Models;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.Extensions.Logging;

namespace InterviewBot.Pages
{
    [Authorize]
    public class NewAnalysisModel : PageModel
    {
        private readonly IProfileService _profileService;

        [BindProperty]
        public IFormFile? ResumeFile { get; set; }

        [BindProperty]
        public string? BriefIntroduction { get; set; }

        [BindProperty]
        public string? FutureCareerGoals { get; set; }

        [BindProperty]
        public string? CurrentActivity { get; set; }

        [BindProperty]
        public string? Motivations { get; set; }

        public string? ErrorMessage { get; set; }
        public string? SuccessMessage { get; set; }
        public bool HasExistingProfile { get; set; } = false;
        public string? ApiResult { get; set; }

        private readonly IExternalAPIService _externalAPIService;
        private readonly IInterviewCatalogService _interviewCatalogService;
        private readonly ILogger<NewAnalysisModel> _logger;

        public NewAnalysisModel(IProfileService profileService, IExternalAPIService externalAPIService, IInterviewCatalogService interviewCatalogService, ILogger<NewAnalysisModel> logger)
        {
            _profileService = profileService;
            _externalAPIService = externalAPIService;
            _interviewCatalogService = interviewCatalogService;
            _logger = logger;
        }

        public async Task OnGetAsync()
        {
            // Get current user ID
            var userId = GetCurrentUserId();
            if (userId != null)
            {
                // Always allow resume uploads - set HasExistingProfile to false
                HasExistingProfile = false;

                // Get user's latest profile to populate form fields
                var userProfiles = await _profileService.GetUserProfilesAsync(userId.Value);
                var latestProfile = userProfiles.FirstOrDefault();

                if (latestProfile != null)
                {
                    // Populate form fields with data from the latest profile
                    BriefIntroduction = latestProfile.BriefIntroduction;
                    FutureCareerGoals = latestProfile.FutureCareerGoals;
                    CurrentActivity = latestProfile.CurrentActivities;
                    Motivations = latestProfile.Motivations;
                }
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                // Get current user ID
                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    {
                        return new JsonResult(new { success = false, message = "User not authenticated" });
                    }
                    ErrorMessage = "User not authenticated. Please log in again.";
                    return Page();
                }

                // Check if user already has a completed profile
                var hasExistingProfile = await _profileService.HasCompletedProfileAsync(userId.Value);
                Profile? existingProfile = null;
                if (hasExistingProfile)
                {
                    // Get the existing profile to update it
                    var userProfiles = await _profileService.GetUserProfilesAsync(userId.Value);
                    existingProfile = userProfiles.FirstOrDefault();
                }

                // Validate file if uploaded
                if (ResumeFile != null)
                {
                    // Check file size (10MB limit)
                    if (ResumeFile.Length > 10 * 1024 * 1024)
                    {
                        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                        {
                            return new JsonResult(new { success = false, message = "File size must be less than 10MB." });
                        }
                        ErrorMessage = "File size must be less than 10MB.";
                        return Page();
                    }

                    // Check file type - only PDF for now
                    var allowedExtensions = new[] { ".pdf" };
                    var fileExtension = Path.GetExtension(ResumeFile.FileName).ToLowerInvariant();

                    if (!allowedExtensions.Contains(fileExtension))
                    {
                        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                        {
                            return new JsonResult(new { success = false, message = "Only PDF files are allowed." });
                        }
                        ErrorMessage = "Only PDF files are allowed.";
                        return Page();
                    }

                    // Upload and analyze resume
                    try
                    {
                        // Call the resume analysis API directly
                        var (apiSuccess, apiResponse, apiError) = await _profileService.CallResumeAnalysisAPIAsync(ResumeFile);
                        Profile profile;
                        string responseToStore;
                        bool isFallback = false;

                        if (apiSuccess && !string.IsNullOrWhiteSpace(apiResponse))
                        {
                            // API call successful and returned valid content - use the real response
                            responseToStore = apiResponse!;
                            
                            if (existingProfile != null)
                            {
                                // Update existing profile with new API response
                                profile = await _profileService.UpdateProfileFromApiResponseAsync(existingProfile, apiResponse!, false);
                            }
                            else
                            {
                                // Create new profile
                                profile = await _profileService.CreateProfileFromApiResponseAsync(apiResponse!, userId.Value, false);
                            }
                            
                            // Store the API response in TempData to display it
                            TempData["ApiResult"] = apiResponse;
                        }
                        else
                        {
                            // API call failed or returned empty content - use fallback data
                            var reason = apiSuccess ? "API returned empty response" : $"API call failed: {apiError}";
                            _logger.LogWarning("{Reason}. Using fallback data for user {UserId}", reason, userId.Value);
                            var fallbackResponse = GetFallbackApiResponse();
                            responseToStore = fallbackResponse;
                            
                            if (existingProfile != null)
                            {
                                // Update existing profile with fallback data
                                profile = await _profileService.UpdateProfileFromApiResponseAsync(existingProfile, fallbackResponse, true);
                            }
                            else
                            {
                                // Create new profile with fallback data
                                profile = await _profileService.CreateProfileFromApiResponseAsync(fallbackResponse, userId.Value, true);
                            }
                            isFallback = true;
                            
                            // Store the fallback response in TempData to display it
                            TempData["ApiResult"] = fallbackResponse;
                        }

                        var actionType = existingProfile != null ? "updated" : "created";
                        
                        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                        {
                            return new JsonResult(new
                            {
                                success = true,
                                message = isFallback 
                                    ? $"Resume '{ResumeFile.FileName}' processed using fallback data due to API issues. Profile {actionType} successfully!" 
                                    : $"Resume '{ResumeFile.FileName}' analyzed successfully! Profile {actionType}.",
                                apiResponse = responseToStore,
                                isFallback = isFallback,
                                actionType = actionType
                            });
                        }

                        SuccessMessage = isFallback 
                            ? $"Resume '{ResumeFile.FileName}' processed using fallback data due to API issues. Profile {actionType} successfully with sample data." 
                            : $"Resume '{ResumeFile.FileName}' analyzed successfully! Profile {actionType}.";

                        // Redirect to a results page or dashboard
                        return RedirectToPage("/ResumeAnalysisResults", new { id = profile.Id });
                    }
                    catch (Exception ex)
                    {
                        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                        {
                            return new JsonResult(new { success = false, message = $"Error uploading resume: {ex.Message}" });
                        }
                        ErrorMessage = $"Error uploading resume: {ex.Message}";
                        return Page();
                    }
                }

                // Validate that at least one input method is provided
                if (ResumeFile == null && string.IsNullOrWhiteSpace(BriefIntroduction))
                {
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    {
                        return new JsonResult(new { success = false, message = "Please either upload a resume or provide a description of yourself." });
                    }
                    ErrorMessage = "Please either upload a resume or provide a description of yourself.";
                    return Page();
                }

                // Handle text-based analysis
                if (!string.IsNullOrWhiteSpace(BriefIntroduction))
                {
                    try
                    {
                        var profile = await _profileService.CreateProfileFromDescriptionAsync(
                            BriefIntroduction ?? "",
                            FutureCareerGoals ?? "",
                            CurrentActivity ?? "",
                            Motivations ?? "",
                            userId.Value);

                        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                        {
                            return new JsonResult(new
                            {
                                success = true,
                                message = "Profile created successfully! AI analysis is in progress.",
                                redirectUrl = $"/ResumeAnalysisResults/{profile.Id}"
                            });
                        }

                        SuccessMessage = "Profile created successfully! AI analysis is in progress.";

                        // Redirect to results page
                        return RedirectToPage("/ResumeAnalysisResults", new { id = profile.Id });
                    }
                    catch (Exception ex)
                    {
                        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                        {
                            return new JsonResult(new { success = false, message = $"Error creating profile: {ex.Message}" });
                        }
                        ErrorMessage = $"Error creating profile: {ex.Message}";
                        return Page();
                    }
                }

                return Page();
            }
            catch (Exception ex)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return new JsonResult(new { success = false, message = $"An error occurred: {ex.Message}" });
                }
                ErrorMessage = $"An error occurred: {ex.Message}";
                return Page();
            }
        }

        public async Task<IActionResult> OnPostDescriptionAnalysisAsync()
        {
            try
            {
                // Get current user ID
                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    return new JsonResult(new { success = false, message = "User not authenticated" });
                }

                // Validate required fields
                if (string.IsNullOrWhiteSpace(BriefIntroduction) || string.IsNullOrWhiteSpace(CurrentActivity))
                {
                    return new JsonResult(new { success = false, message = "Brief Introduction and Current Activities are required" });
                }

                if (string.IsNullOrWhiteSpace(FutureCareerGoals) || string.IsNullOrWhiteSpace(Motivations))
                {
                    return new JsonResult(new { success = false, message = "Please fill in your Career Goals and Motivations" });
                }

                // Prepare API request data
                var interviewCatalogRequest = new
                {
                    briefIntroduction = BriefIntroduction,
                    futureCareerGoals = FutureCareerGoals,
                    currentActivities = CurrentActivity,
                    motivations = Motivations
                };

                // Call the external API
                var apiResponse = await _externalAPIService.SendInterviewCatalogRequestAsync(interviewCatalogRequest);

                if (!apiResponse.Success)
                {
                    return new JsonResult(new { success = false, message = $"API Error: {apiResponse.ErrorMessage}" });
                }

                // Get user's latest profile to get the profile ID
                var userProfiles = await _profileService.GetUserProfilesAsync(userId.Value);
                var latestProfile = userProfiles.FirstOrDefault();

                if (latestProfile == null)
                {
                    return new JsonResult(new { success = false, message = "No profile found. Please create a profile first." });
                }

                // Generate interview catalogs from the API response
                var catalogs = await _interviewCatalogService.GenerateInterviewCatalogsAsync(userId.Value, latestProfile.Id, apiResponse.Data);

                return new JsonResult(new
                {
                    success = true,
                    message = "Interview catalogs generated successfully!",
                    catalogCount = catalogs.Count,
                    redirectUrl = "/Dashboard"
                });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        private int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
            {
                return userId;
            }
            return null;
        }

        private string GetFallbackApiResponse()
        {
            return @"{
                ""possibleJobs"": ""Potential job opportunities for Emrah include positions as a Senior Shopify Developer, eCommerce Consultant, or Front-End Developer, possibly within larger organizations or agencies that focus on delivering comprehensive eCommerce solutions."",
                ""mbaSubjectsToReinforce"": ""To further enhance his career, Emrah could benefit from reinforcing subjects related to Digital Marketing, Project Management, and Business Analytics during an MBA program. Understanding these areas in-depth would provide him with a broader business perspective and enhance his ability to strategize and execute eCommerce initiatives effectively."",
                ""briefIntroduction"": ""Emrah Gunel is a seasoned Shopify Developer with a focus on theme customization, development, app integrations, and eCommerce business creation. With over 7 years of hands-on experience in Shopify and eCommerce development, Emrah possesses a comprehensive skill set that allows him to effectively leverage modern web design trends and standards to build high-performing online stores."",
                ""currentActivities"": ""Currently, Emrah is employed as a Shopify Developer at Mark Anthony Group, where he is responsible for setting up Shopify stores, theme configurations, custom functionalities, and much more. His previous roles include positions at Royal Retailer, Anheuser-Busch, Carian's Bistro Chocolates, and Design Furniture, where he honed his skills in various aspects of both front-end development and eCommerce management."",
                ""motivations"": ""Emrah is motivated by a desire to create intuitive digital experiences that drive customer engagement and revenue growth for online businesses. He is passionate about keeping up with the latest trends in web design and eCommerce, which is showcased through his continuous learning and application of advanced technologies in his work."",
                ""futureCareerGoals"": """",
                ""strengths"": ""Emrah's strengths lie in his extensive experience with Shopify Liquid, custom theme development, site optimization, and strong understanding of front-end technologies including HTML, CSS, and JavaScript. His hands-on experience with various eCommerce platforms, debugging skills, and knowledge of integrations further enhance his capability to deliver high-quality solutions."",
                ""weaknesses"": ""One potential weakness could be his specific focus on Shopify, which may limit his exposure to other eCommerce platforms or technologies that could broaden his expertise. Additionally, while Emrah has experience with a variety of programming languages and tools, his primary proficiency may lead to less experience with certain niche tools that could be beneficial in specific projects."",
                ""potentialCareerPaths"": ""Emrah's career could progress towards roles such as eCommerce Manager or Technical Project Manager, where he can leverage his deep understanding of development and customer relationship dynamics. He may also transition into a consulting role, helping businesses optimize their Shopify and eCommerce strategies.""
            }";
        }
    }
}
