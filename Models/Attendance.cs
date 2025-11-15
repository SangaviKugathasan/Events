using System;

namespace EventZax.Models
{
    public class Attendance
    {
        public int Id { get; set; }
        public int EventId { get; set; }
        public Event? Event { get; set; }
        public string? UserId { get; set; }
        public ApplicationUser? User { get; set; }
        public DateTime? CheckInTime { get; set; }
        public bool IsCheckedIn { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Tel { get; set; } = string.Empty;
    }
}
