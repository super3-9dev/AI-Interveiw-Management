using InterviewBot.Models;

namespace InterviewBot.Services
{
    public interface IInterviewNoteService
    {
        Task<IEnumerable<InterviewNote>> GetInterviewNotesAsync(int interviewId);
        Task<InterviewNote?> GetInterviewNoteByIdAsync(int noteId);
        Task<InterviewNote> CreateInterviewNoteAsync(InterviewNote note);
        Task<InterviewNote> UpdateInterviewNoteAsync(InterviewNote note);
        Task<bool> DeleteInterviewNoteAsync(int noteId);
        Task<bool> DeleteAllInterviewNotesAsync(int interviewId);
    }
}
