using InterviewBot.Data;
using InterviewBot.Models;
using Microsoft.EntityFrameworkCore;

namespace InterviewBot.Services
{
    public class InterviewNoteService : IInterviewNoteService
    {
        private readonly AppDbContext _context;

        public InterviewNoteService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<InterviewNote>> GetInterviewNotesAsync(int interviewId)
        {
            return await _context.InterviewNotes
                .Where(n => n.InterviewId == interviewId)
                .OrderByDescending(n => n.Date)
                .ToListAsync();
        }

        public async Task<InterviewNote?> GetInterviewNoteByIdAsync(int noteId)
        {
            return await _context.InterviewNotes
                .FirstOrDefaultAsync(n => n.Id == noteId);
        }

        public async Task<InterviewNote> CreateInterviewNoteAsync(InterviewNote note)
        {
            note.CreatedAt = DateTime.UtcNow;
            _context.InterviewNotes.Add(note);
            await _context.SaveChangesAsync();
            return note;
        }

        public async Task<InterviewNote> UpdateInterviewNoteAsync(InterviewNote note)
        {
            note.UpdatedAt = DateTime.UtcNow;
            _context.InterviewNotes.Update(note);
            await _context.SaveChangesAsync();
            return note;
        }

        public async Task<bool> DeleteInterviewNoteAsync(int noteId)
        {
            var note = await _context.InterviewNotes.FindAsync(noteId);
            if (note == null)
                return false;

            _context.InterviewNotes.Remove(note);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAllInterviewNotesAsync(int interviewId)
        {
            var notes = await _context.InterviewNotes
                .Where(n => n.InterviewId == interviewId)
                .ToListAsync();

            if (!notes.Any())
                return false;

            _context.InterviewNotes.RemoveRange(notes);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
